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
}

