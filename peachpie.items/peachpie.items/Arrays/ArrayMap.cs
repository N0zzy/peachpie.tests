using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Pchp.Core;

namespace peachpie.items.Arrays;

public class ArrayMap : ArrayMapExample
{
    public ArrayMap(int size, int count)
    {
        Console.WriteLine("\n===== Array Map Tests =====");

        var ctx = Context.CreateEmpty();
        int arraySize = size;
        int arrayCount = count;
        var arrays = GenerateTestArrays(arraySize, arrayCount);

        IPhpCallable callback = PhpCallback.Create((ctx, args) =>
        {
            long sum = 0;
            foreach (var arg in args)
            {
                sum += arg.ToLong();
            }

            return PhpValue.Create(sum);
        });

        Console.WriteLine("Testing Original Version...");
        var (timeOriginal, memoryOriginal, resultOriginal) =
            TestFunction(() => array_map_Original(ctx, callback, arrays));

        Console.WriteLine("Testing Optimized Version...");
        var (timeOptimized, memoryOptimized, resultOptimized) =
            TestFunction(() => array_map_Optimized(ctx, callback, arrays));

        bool resultsAreEqual = false;
        if (
            resultOriginal != null &&
            resultOptimized != null
        )
        {
            resultsAreEqual = ComparePhpArrays(resultOriginal, resultOptimized)
                ;
        }
        else
        {
            Console.WriteLine("One or more results are null.");
        }

        // Вывод результатов
        Console.WriteLine("\n===== Results for [" + $"size={arraySize}, count={arrayCount}] =====");
        Console.WriteLine(">>> Original Version:");
        Console.WriteLine($"Time: {timeOriginal} ms");
        Console.WriteLine($"Memory: {memoryOriginal} B");

        Console.WriteLine("\n >>> Optimized Version:");
        Console.WriteLine($"Time: {timeOptimized} ms");
        Console.WriteLine($"Memory: {memoryOptimized} B");

        Console.WriteLine("\nResults are equal: " + resultsAreEqual);
    }

    /// <summary>
    /// Генерирует массивы для тестирования.
    /// </summary>
    static PhpArray[] GenerateTestArrays(int size, int count)
    {
        var arrays = new PhpArray[count];
        for (int i = 0; i < count; i++)
        {
            var array = new PhpArray(size);
            for (int j = 0; j < size; j++)
            {
                array.Add(j + 1);
            }

            arrays[i] = array;
        }

        return arrays;
    }

    /// <summary>
    /// Метод для тестирования времени, памяти и получения результата функции.
    /// </summary>
    static (long time, long memory, PhpArray? result) TestFunction(Func<PhpArray> action)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        Thread.Sleep(200);

        long memoryBefore = GC.GetTotalMemory(true);

        Stopwatch stopwatch = Stopwatch.StartNew();
        PhpArray? result = null;
        try
        {
            result = action();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during function execution: {ex.Message}");
        }

        stopwatch.Stop();

        long memoryAfter = GC.GetTotalMemory(true);

        return (stopwatch.ElapsedMilliseconds, memoryAfter - memoryBefore, result);
    }

    /// <summary>
    /// Сравнивает два PhpArray на равенство.
    /// </summary>
    static bool ComparePhpArrays(PhpArray? array1, PhpArray? array2)
    {
        if (array1 == null || array2 == null)
        {
            return false;
        }

        if (array1.Count != array2.Count)
        {
            return false;
        }

        foreach (var key in array1.Keys)
        {
            if (!array2.ContainsKey(key))
            {
                return false;
            }

            var value1 = array1[key];
            var value2 = array2[key];

            if (!value1.Equals(value2))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Default callback for <see cref="Map"/>.
    /// </summary>
    private static readonly IPhpCallable _mapIdentity = PhpCallback.Create((ctx, args) =>
    {
        var result = new PhpArray(args.Length);

        for (int i = 0; i < args.Length; i++)
        {
            result.Add(args[i].DeepCopy());
        }

        return PhpValue.Create(result);
    });
}

public class ArrayMapExample
{
    public static IPhpCallable? _mapIdentity;

    /// <summary>
    /// Исходная версия array_map.
    /// </summary>
    public static PhpArray array_map_Original(Context ctx /*, caller*/, IPhpCallable map, [In, Out] params PhpArray[] arrays)
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
            map = _mapIdentity;
        }

        int count = arrays.Length;
        bool preserve_keys = count == 1;
        var args = new PhpValue[count];
        var iterators = new OrderedDictionary.FastEnumerator[count];
        PhpArray result;

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

        return result;
    }

    /// <summary>
    /// Оптимизированная версия array_map.
    /// </summary>
    public static PhpArray array_map_Optimized(Context ctx, IPhpCallable map, params PhpArray[] arrays)
    {
        if (arrays == null || arrays.Length == 0)
        {
            PhpException.InvalidArgument(nameof(arrays), "arg_null_or_empty");
            return null;
        }

        if (map != null && !PhpVariable.IsValidBoundCallback(ctx, map))
        {
            PhpException.InvalidArgument(nameof(map));
            return null;
        }

        if (arrays.Length == 1 && map == _mapIdentity)
        {
            return arrays[0].DeepCopy();
        }

        map ??= _mapIdentity ??= CreateIdentityCallback();

        var iterators = new OrderedDictionary.FastEnumerator[arrays.Length];
        int max_count = 0;
        for (int i = 0; i < arrays.Length; i++)
        {
            if (arrays[i] == null)
            {
                PhpException.Throw(PhpError.Warning, "argument_not_array", (i + 2).ToString());
                return null;
            }

            iterators[i] = arrays[i].GetFastEnumerator();
            max_count = Math.Max(max_count, arrays[i].Count);
        }

        // 4. Выбор стратегии обработки
        PhpArray result = new PhpArray(max_count);
        bool preserve_keys = (arrays.Length == 1);

        // Маленькие массивы: создаём новый массив вместо пула
        PhpValue[] args = arrays.Length <= 4
            ? new PhpValue[arrays.Length]
            : ArrayPool<PhpValue>.Shared.Rent(arrays.Length);

        try
        {
            while (true)
            {
                bool has_values = false;
                for (int i = 0; i < arrays.Length; i++)
                {
                    if (iterators[i].MoveNext())
                    {
                        args[i] = iterators[i].CurrentValue;
                        has_values = true;
                    }
                    else
                    {
                        args[i] = PhpValue.Null;
                    }
                }

                if (!has_values) break;

                var return_value = map.Invoke(ctx, args);
                if (preserve_keys)
                    result.Add(iterators[0].CurrentKey, return_value);
                else
                    result.Add(return_value);
            }
        }
        finally
        {
            if (arrays.Length > 4)
                ArrayPool<PhpValue>.Shared.Return(args);
        }

        return result;
    }


    private static IPhpCallable CreateIdentityCallback()
    {
        return PhpCallback.Create((ctx, args) =>
        {
            if (args.Length == 1)
            {
                return args[0].DeepCopy();
            }

            var result = new PhpArray(args.Length);
            foreach (var arg in args)
            {
                result.Add(arg.DeepCopy());
            }

            return result;
        });
    }
}