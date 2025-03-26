using System.Text;
using Pchp.Core;
using peachpie.items.Arrays;
using Xunit.Abstractions;

namespace peachpie.xunit
{
    public class ArrayMapTests(ITestOutputHelper testOutputHelper)
    {
        /// <summary>
        /// Коллбэк для тестирования.
        /// </summary>
        private static readonly IPhpCallable Callback = PhpCallback.Create((ctx, args) =>
        {
            long sum = 0;
            string concat = "";
            foreach (var arg in args)
            {
                if (arg.IsNull)
                {
                    continue;
                }
                else if (arg.IsInteger())
                {
                    sum += arg.ToLong();
                }
                else if (arg.IsString())
                {
                    concat += arg.ToString(ctx);
                }
            }
            return PhpValue.Create(concat != "" ? concat : sum);
        });

        /// <summary>
        /// Тест для массивов разной длины.
        /// </summary>
        [Fact]
        public void TestDifferentLengths()
        {
            // Arrange
            var ctx = Context.CreateEmpty();
            
            // Test Values
            var arrays = new PhpArray 
            { 
                { "a", 10 }, 
                { "b", 20 }, 
                { 0, "value1" }, 
                { 1, "value2" } 
            };

            // Act
            var originalResult = ArrayMapExample.array_map_Original(ctx, Callback, arrays);
            var optimizedResult = ArrayMapExample.array_map_Optimized(ctx, Callback, arrays);

            // Assert
            testOutputHelper.WriteLine($"Expected: {PrintPhpArray( arrays )}");
            testOutputHelper.WriteLine($"Original: {PrintPhpArray( originalResult )}");
            testOutputHelper.WriteLine($"Optimized: {PrintPhpArray( optimizedResult )}");

            // Assert
            Assert.True(ArePhpArraysEqual(originalResult, optimizedResult));
        }

        /// <summary>
        /// Сравнивает два PhpArray на равенство.
        /// </summary>
        private bool ArePhpArraysEqual(PhpArray? array1, PhpArray? array2)
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
        /// Выводит содержимое PhpArray в виде строки.
        /// </summary>
        private string PrintPhpArray(PhpArray array)
        {
            var result = new StringBuilder("[");
            bool first = true;

            foreach (var key in array.Keys)
            {
                if (!first)
                {
                    result.Append(", ");
                }

                result.Append($"{key}: {array[key]}");
                first = false;
            }

            result.Append("]");
            return result.ToString();
        }
    }
}