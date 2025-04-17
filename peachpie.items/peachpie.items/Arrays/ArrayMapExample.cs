using System.Buffers;
using System.Runtime.InteropServices;
using Pchp.Core;

namespace peachpie.items.Arrays;

public class ArrayMapExample
{
    private static readonly IPhpCallable s_mapIdentity = PhpCallback.Create((ctx, args) =>
    {
        var result = new PhpArray(args.Length);

        for (int i = 0; i < args.Length; i++)
        {
            result.Add(args[i].DeepCopy());
        }

        return PhpValue.Create(result);
    });

    /// <summary>
    /// Исходная версия array_map.
    /// </summary>
    public static PhpArray array_map_Original(Context ctx /*, caller*/, IPhpCallable map,
        [In, Out] params PhpArray[] arrays)
    {
        if (map != null && !PhpVariable.IsValidBoundCallback(ctx, map))
        {
            PhpException.InvalidArgument(nameof(map));
            return null;
        }

        //if (!PhpArgument.CheckCallback(map, caller, "map", 0, true)) return null;
        if (arrays == null || arrays.Length == 0)
        {
            PhpException.InvalidArgument(nameof(arrays), "arg_null_or_empty");
            return null;
        }

        // if callback has not been specified uses the default one:
        if (map == null)
        {
            // If only one array is provided, array_map() will return the input array.
            if (arrays.Length == 1)
            {
                return arrays[0].DeepCopy();
            }

            map = s_mapIdentity;
        }

        int count = arrays.Length;
        bool preserve_keys = count == 1;
        PhpArray result;

        var argsbuffer = ArrayPool<PhpValue>.Shared.Rent(count);
        var args = argsbuffer.AsSpan(0, count);
        var iterators = ArrayPool<OrderedDictionary.FastEnumerator>.Shared.Rent(count);

        try
        {
            // initializes iterators and args array, computes length of the longest array:
            int max_count = 0;
            for (int i = 0; i < arrays.Length; i++)
            {
                var array = arrays[i];

                if (array == null)
                {
                    PhpException.Throw(PhpError.Warning, "argument_not_array",
                        (i + 2).ToString()); // +2 (first arg is callback) 
                    return null;
                }

                iterators[i] = array.GetFastEnumerator();
                if (array.Count > max_count) max_count = array.Count;
            }

            // keys are preserved in a case of a single array and re-indexed otherwise:
            result = new PhpArray(arrays[0].Count);

            for (;;)
            {
                bool hasvalid = false;

                // fills args[] with items from arrays:
                for (int i = 0; i < arrays.Length; i++)
                {
                    if (!iterators[i].IsDefault)
                    {
                        if (iterators[i].MoveNext())
                        {
                            hasvalid = true;

                            // note: deep copy is not necessary since a function copies its arguments if needed:
                            args[i] = iterators[i].CurrentValue;
                            // TODO: throws if the CurrentValue is an alias
                        }
                        else
                        {
                            args[i] = PhpValue.Null;
                            iterators[i] = default; // IsDefault, !IsValid
                        }
                    }
                }

                if (!hasvalid) break;

                // invokes callback:
                var return_value = map.Invoke(ctx, args);

                // return value is not deeply copied:
                if (preserve_keys)
                {
                    result.Add(iterators[0].CurrentKey, return_value);
                }
                else
                {
                    result.Add(return_value);
                }

                // loads new values (callback may modify some by ref arguments):
                for (int i = 0; i < arrays.Length; i++)
                {
                    if (iterators[i].IsValid)
                    {
                        var item = iterators[i].CurrentValue;
                        if (item.IsAlias)
                        {
                            item.Alias.Value = args[i].GetValue();
                        }
                        else
                        {
                            iterators[i].CurrentValue = args[i].GetValue();
                        }
                    }
                }
            }
        }
        finally
        {
            ArrayPool<PhpValue>.Shared.Return(argsbuffer);
            ArrayPool<OrderedDictionary.FastEnumerator>.Shared.Return(iterators);
        }

        return result;
    }

    /// <summary>
    /// Оптимизированная версия array_map.
    /// </summary>
    /// <summary>
    /// Applies a callback function to the elements of the given arrays.
    /// </summary>
    /// <param name="ctx">Current runtime context.</param>
    /// <param name="map">A callback function to apply. Can be null for zip-like behavior.</param>
    /// <param name="arrays">Arrays to process.</param>
    /// <returns>An array of results from applying the callback.</returns>
    public static PhpArray array_map_Optimized(Context ctx, IPhpCallable map, params PhpArray[] arrays)
    {
        // Validate input arrays
        if (arrays == null || arrays.Length == 0)
        {
            PhpException.InvalidArgument(nameof(arrays), "arg_null_or_empty");
            return null;
        }

        for (int i = 0; i < arrays.Length; i++)
        {
            if (arrays[i] == null)
            {
                PhpException.Throw(PhpError.Warning, "argument_not_array",
                        (i + 2).ToString()); // +2 (first arg is callback) 
                    return null;
            }
        }

        // If no callback is provided, use default identity callback
        if (map == null)
        {
            if (arrays.Length == 1)
            {
                return arrays[0].DeepCopy();
            }

            map = s_mapIdentity;
        }

        // Check if all arrays have numeric keys
        bool allNumericKeys = true;
        foreach (var array in arrays)
        {
            foreach (var key in array.Keys)
            {
                if (!key.IsInteger)
                {
                    allNumericKeys = false;
                    goto CheckComplete;
                }
            }
        }

        CheckComplete:

        if (allNumericKeys)
        {
            int maxCount = 0;
            for (int i = 0; i < arrays.Length; i++)
            {
                if (arrays[i].Count > maxCount)
                {
                    maxCount = arrays[i].Count;
                }
            }

            var result = new PhpArray(maxCount);

            for (int i = 0; i < maxCount; i++)
            {
                var args = new PhpValue[arrays.Length];
                for (int j = 0; j < arrays.Length; j++)
                {
                    args[j] = i < arrays[j].Count ? arrays[j][i] : PhpValue.Null;
                }

                result.Add(map.Invoke(ctx, args));
            }

            return result;
        }
        else
        {
            var keys = new HashSet<IntStringKey>();
            for (int i = 0; i < arrays.Length; i++)
            {
                foreach (var key in arrays[i].Keys)
                {
                    keys.Add(key);
                }
            }

            var result = new PhpArray(keys.Count);

            foreach (var key in keys)
            {
                var args = new PhpValue[arrays.Length];
                for (int i = 0; i < arrays.Length; i++)
                {
                    args[i] = arrays[i].ContainsKey(key) ? arrays[i][key] : PhpValue.Null;
                }

                var mappedValue = map.Invoke(ctx, args);
                if (key.IsInteger)
                {
                    result.Add(mappedValue);
                }
                else
                {
                    result.Add(key, mappedValue);
                }
            }

            return result;
        }
    }
}