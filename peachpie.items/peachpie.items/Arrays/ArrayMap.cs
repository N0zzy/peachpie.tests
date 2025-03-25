using System.Diagnostics;
using System.Runtime.InteropServices;
using Pchp.Core;

namespace peachpie.items.Arrays;

public class ArrayMap : ArrayMapExample
{
    public ArrayMap(int size, int count)
    {
        Console.WriteLine("\n===== Array Map Tests =====");

        // Создаем тестовые данные
        var ctx = Context.CreateEmpty();
        int arraySize = size; // Размер массивов
        int arrayCount = count; // Количество массивов
        var arrays = GenerateTestArrays(arraySize, arrayCount);

        // Коллбэк для тестирования
        IPhpCallable callback = PhpCallback.Create((ctx, args) =>
        {
            long sum = 0;
            foreach (var arg in args)
            {
                sum += arg.ToLong(); // Преобразуем PhpValue в long
            }

            return PhpValue.Create(sum);
        });

        // Тестирование исходной версии
        Console.WriteLine("Testing Original Version...");
        var (timeOriginal, memoryOriginal, resultOriginal) =
            TestFunction(() => array_map_Original(ctx, callback, arrays));

        // Тестирование оптимизированной версии
        Console.WriteLine("Testing Optimized Version...");
        var (timeOptimized, memoryOptimized, resultOptimized) =
            TestFunction(() => array_map_Optimized(ctx, callback, arrays));

        // Тестирование версии с использованием структуры
        Console.WriteLine("Testing Optimized With Struct Version...");
        var (timeOptimizedStruct, memoryOptimizedStruct, resultOptimizedStruct) =
            TestFunction(() => array_map_Optimized_With_Struct(ctx, callback, arrays));

        // Тестирование финальной версии
        Console.WriteLine("Testing Optimized Final Version...");
        var (timeOptimizedFinal, memoryOptimizedFinal, resultOptimizedFinal) =
            TestFunction(() => array_map_Optimized_Final(ctx, callback, arrays));

        Console.WriteLine("Testing Optimized Other Version...");
        var (timeOptimizedOther, memoryOptimizedOther, resultOptimizedOther) =
            TestFunction(() => array_map_Optimized_Other(ctx, callback, arrays));


        // Сравнение результатов
        bool resultsAreEqual = false;
        if (
            resultOriginal != null &&
            resultOptimized != null &&
            resultOptimizedStruct != null &&
            resultOptimizedFinal != null &&
            resultOptimizedOther != null
        )
        {
            resultsAreEqual = ComparePhpArrays(resultOriginal, resultOptimized) &&
                              ComparePhpArrays(resultOriginal, resultOptimizedStruct) &&
                              ComparePhpArrays(resultOriginal, resultOptimizedFinal) &&
                              ComparePhpArrays(resultOriginal, resultOptimizedOther);
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

        Console.WriteLine("\n >>> Optimized With Struct Version:");
        Console.WriteLine($"Time: {timeOptimizedStruct} ms");
        Console.WriteLine($"Memory: {memoryOptimizedStruct} B");

        Console.WriteLine("\n >>> Optimized Final Version:");
        Console.WriteLine($"Time: {timeOptimizedFinal} ms");
        Console.WriteLine($"Memory: {memoryOptimizedFinal} B");

        Console.WriteLine("\n >>> Optimized Other Version:");
        Console.WriteLine($"Time: {timeOptimizedOther} ms");
        Console.WriteLine($"Memory: {memoryOptimizedOther} B");

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
                array.Add(j + 1); // Простые числовые значения
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
        GC.Collect(); // Принудительно очищаем память перед тестом
        GC.WaitForPendingFinalizers();
        Thread.Sleep(200); // Задержка для завершения очистки

        long memoryBefore = GC.GetTotalMemory(true); // Память до выполнения

        Stopwatch stopwatch = Stopwatch.StartNew();
        PhpArray? result = null;
        try
        {
            result = action(); // Выполняем тестируемую функцию
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during function execution: {ex.Message}");
        }

        stopwatch.Stop();

        long memoryAfter = GC.GetTotalMemory(true); // Память после выполнения

        return (stopwatch.ElapsedMilliseconds, memoryAfter - memoryBefore, result);
    }

    /// <summary>
    /// Сравнивает два PhpArray на равенство.
    /// </summary>
    static bool ComparePhpArrays(PhpArray? array1, PhpArray? array2)
    {
        if (array1 == null || array2 == null)
        {
            return false; // Если один из массивов null, они не равны
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
    public static PhpArray array_map_Optimized(Context ctx, IPhpCallable map, [In, Out] params PhpArray[] arrays)
    {
        if (map != null && !PhpVariable.IsValidBoundCallback(ctx, map))
        {
            PhpException.InvalidArgument(nameof(map));
            return null;
        }

        if (arrays == null || arrays.Length == 0)
        {
            PhpException.InvalidArgument(nameof(arrays), "arg_null_or_empty");
            return null;
        }

        map ??= _mapIdentity;

        int count = arrays.Length;
        bool preserveKeys = count == 1;
        var args = new PhpValue[count];
        int maxCount = 0;

        foreach (var array in arrays)
        {
            if (array.Count > maxCount) maxCount = array.Count;
        }

        var result = new PhpArray(preserveKeys ? arrays[0].Count : maxCount);

        for (int i = 0; i < maxCount; i++)
        {
            for (int j = 0; j < arrays.Length; j++)
            {
                args[j] = i < arrays[j].Count ? arrays[j][i] : PhpValue.Null;
            }

            var returnValue = map?.Invoke(ctx, args);

            if (preserveKeys)
            {
                result.Add(i, returnValue);
            }
            else
            {
                result.Add(returnValue);
            }
        }

        return result;
    }

    private static PhpValue[] _argsBuffer;

    /// <summary>
    ///  Оптимизированная версия array_map.
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="map"></param>
    /// <param name="arrays"></param>
    /// <returns></returns>
    public static PhpArray array_map_Optimized_With_Struct(Context ctx, IPhpCallable map,
        [In, Out] params PhpArray[] arrays)
    {
        if (_argsBuffer == null || _argsBuffer.Length < arrays.Length)
        {
            _argsBuffer = new PhpValue[arrays.Length];
        }

        int max_count = arrays.Max(a => a.Count);
        var result = new PhpArray(max_count);

        for (int i = 0; i < max_count; i++)
        {
            for (int j = 0; j < arrays.Length; j++)
            {
                _argsBuffer[j] = i < arrays[j].Count ? arrays[j][i] : PhpValue.Null;
            }

            var return_value = map.Invoke(ctx, _argsBuffer);
            result.Add(return_value);
        }

        return result;
    }

    public static PhpArray array_map_Optimized_Final(Context ctx, IPhpCallable map, [In, Out] params PhpArray[] arrays)
    {
        if (map != null && !PhpVariable.IsValidBoundCallback(ctx, map))
        {
            PhpException.InvalidArgument(nameof(map));
            return null;
        }

        if (arrays == null || arrays.Length == 0)
        {
            PhpException.InvalidArgument(nameof(arrays), "arg_null_or_empty");
            return null;
        }

        map ??= _mapIdentity;

        int count = arrays.Length;
        bool preserve_keys = count == 1;
        var args = new PhpValue[count];
        int max_count = arrays.Max(a => a.Count);

        var result = new PhpArray(max_count);

        for (int i = 0; i < max_count; i++)
        {
            for (int j = 0; j < arrays.Length; j++)
            {
                args[j] = i < arrays[j].Count ? arrays[j][i] : PhpValue.Null;
            }

            var return_value = map.Invoke(ctx, args);

            if (preserve_keys)
            {
                result.Add(i, return_value);
            }
            else
            {
                result.Add(return_value);
            }
        }

        return result;
    }

    /// <summary>
    ///   Оптимизированная версия array_map.
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="map"></param>
    /// <param name="arrays"></param>
    /// <returns></returns>
    public static PhpArray array_map_Optimized_Other(Context ctx, IPhpCallable map, [In, Out] params PhpArray[] arrays)
    {
        if (map == null || arrays == null || arrays.Length == 0)
        {
            return null;
        }

        map ??= _mapIdentity;

        int count = arrays.Length;
        bool preserve_keys = count == 1;
        int max_count = arrays.Max(a => a.Count);

        var result = new PhpArray(max_count);

        // Reuse a buffer for arguments
        var argsBuffer = new PhpValue[count];

        for (int i = 0; i < max_count; i++)
        {
            for (int j = 0; j < count; j++)
            {
                argsBuffer[j] = i < arrays[j].Count ? arrays[j][i] : PhpValue.Null;
            }

            var return_value = map.Invoke(ctx, argsBuffer);

            if (preserve_keys)
            {
                result.Add(i, return_value);
            }
            else
            {
                result.Add(return_value);
            }
        }

        return result;
    }
}