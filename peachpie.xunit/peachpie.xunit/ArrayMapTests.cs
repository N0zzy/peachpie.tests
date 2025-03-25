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
            var arrays = new PhpArray[]
            {
                new PhpArray(new PhpValue[] { PhpValue.Create(1), PhpValue.Create(2) }),
                new PhpArray(new PhpValue[] { PhpValue.Create(3) })
            };
            var expectedResult = new PhpArray(new PhpValue[] { PhpValue.Create(4), PhpValue.Null });

            // Act
            var originalResult = ArrayMapExample.array_map_Original(ctx, Callback, arrays);
            var optimizedResult = ArrayMapExample.array_map_Optimized(ctx, Callback, arrays);
            var optimizedStruct = ArrayMapExample.array_map_Optimized_With_Struct(ctx, Callback, arrays);
            var optimizedFinal = ArrayMapExample.array_map_Optimized_Final(ctx, Callback, arrays);
            var optimizedOther = ArrayMapExample.array_map_Optimized_Other(ctx, Callback, arrays);

            // Assert
            testOutputHelper.WriteLine($"Expected: {PrintPhpArray( expectedResult )}");
            testOutputHelper.WriteLine($"Original: {PrintPhpArray( originalResult )}");
            testOutputHelper.WriteLine($"Optimized: {PrintPhpArray( optimizedResult )}");
            testOutputHelper.WriteLine($"Optimized Struct: {PrintPhpArray( optimizedStruct )}");
            testOutputHelper.WriteLine($"Optimized Final: {PrintPhpArray( optimizedFinal )}");
            testOutputHelper.WriteLine($"Optimized Other: {PrintPhpArray( optimizedOther )}");

            // Assert
            Assert.True(ArePhpArraysEqual(originalResult, optimizedResult));
            Assert.True(ArePhpArraysEqual(originalResult, optimizedStruct));
            Assert.True(ArePhpArraysEqual(originalResult, optimizedFinal));
            Assert.True(ArePhpArraysEqual(originalResult, optimizedOther));
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