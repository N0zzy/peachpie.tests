using Pchp.Core;
using peachpie.items.Arrays;
using Xunit.Abstractions;

namespace peachpie.xunit
{
    public class ArrayMapTests(ITestOutputHelper testOutputHelper)
    {
        private readonly Context _context = Context.CreateEmpty();

        /// <summary>
        /// Проверяет базовый случай: применение функции к одному массиву.
        /// </summary>
        [Fact]
        public void TestSingleArray()
        {
            // Arrange
            var array = new PhpArray { 1, 2, 3, 4 };
            IPhpCallable callback = PhpCallback.Create((ctx, args) => PhpValue.Create(args[0].ToLong() * 2));

            // Act
            var result = ArrayMapExample.array_map_Optimized(_context, callback, array);

            // Assert
            Assert.Equal(4, result.Count);
            Assert.Equal(2, result[0].ToLong());
            Assert.Equal(4, result[1].ToLong());
            Assert.Equal(6, result[2].ToLong());
            Assert.Equal(8, result[3].ToLong());
        }

        /// <summary>
        /// Проверяет случай с несколькими массивами одинаковой длины.
        /// </summary>
        [Fact]
        public void TestMultipleArraysSameLength()
        {
            // Arrange
            var array1 = new PhpArray { 1, 2, 3 };
            var array2 = new PhpArray { 4, 5, 6 };
            IPhpCallable callback = PhpCallback.Create((ctx, args) => PhpValue.Create(args[0].ToLong() + args[1].ToLong()));

            // Act
            var result = ArrayMapExample.array_map_Optimized(_context, callback, array1, array2);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Equal(5, result[0].ToLong());
            Assert.Equal(7, result[1].ToLong());
            Assert.Equal(9, result[2].ToLong());
        }

        /// <summary>
        /// Проверяет случай с массивами разной длины (дополнение null).
        /// </summary>
        [Fact]
        public void TestMultipleArraysWithDifferentLengths()
        {
            // Arrange
            var array1 = new PhpArray { 1, 2 };
            var array2 = new PhpArray { 3, 4, 5 };
            IPhpCallable callback = PhpCallback.Create((ctx, args) =>
            {
                long sum = 0;
                foreach (var arg in args)
                {
                    sum += arg.IsNull ? 0 : arg.ToLong();
                }
                return PhpValue.Create(sum);
            });

            // Act
            var result = ArrayMapExample.array_map_Optimized(_context, callback, array1, array2);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Equal(4, result[0].ToLong()); // 1 + 3
            Assert.Equal(6, result[1].ToLong()); // 2 + 4
            Assert.Equal(5, result[2].ToLong()); // 0 + 5
        }

        /// <summary>
        /// Проверяет случай с пустыми массивами.
        /// </summary>
        [Fact]
        public void TestEmptyArrays()
        {
            // Arrange
            var array1 = new PhpArray();
            var array2 = new PhpArray();
            IPhpCallable callback = PhpCallback.Create((ctx, args) => PhpValue.Create(args[0].ToLong() + args[1].ToLong()));

            // Act
            var result = ArrayMapExample.array_map_Optimized(_context, callback, array1, array2);
            // Assert
            Assert.Empty(result);
        }

        /// <summary>
        /// Проверяет случай с null вместо массива.
        /// </summary>
        [Fact]
        public void TestNullArray()
        {
            // Arrange
            PhpArray array = null;
            IPhpCallable callback = PhpCallback.Create((ctx, args) => PhpValue.Create(args[0].ToLong() * 2));

            // Act
            var result = ArrayMapExample.array_map_Optimized(_context, callback, array);

            // Assert
            Assert.Null(result);
        }

        /// <summary>
        /// Проверяет случай с отсутствующим callback (используется идентичность).
        /// </summary>
        [Fact]
        public void TestIdentityCallback()
        {
            // Arrange
            var array = new PhpArray { 1, 2, 3 };

            // Act
            var result = ArrayMapExample.array_map_Optimized(_context, null, array);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Equal(1, result[0].ToLong());
            Assert.Equal(2, result[1].ToLong());
            Assert.Equal(3, result[2].ToLong());
        }

        /// <summary>
        /// Проверяет случай с сохранением ключей при одном массиве.
        /// </summary>
        [Fact]
        public void TestPreserveKeysWithSingleArray()
        {
            // Arrange
            var array = new PhpArray { ["a"] = 1, ["b"] = 2, ["c"] = 3 };
            IPhpCallable callback = PhpCallback.Create((ctx, args) => PhpValue.Create(args[0].ToLong() * 2));

            // Act
            var result = ArrayMapExample.array_map_Optimized(_context, callback, array);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Equal(2, result["a"].ToLong());
            Assert.Equal(4, result["b"].ToLong());
            Assert.Equal(6, result["c"].ToLong());
        }

        /// <summary>
        /// Проверяет случай с переиндексацией ключей при нескольких массивах.
        /// </summary>
        [Fact]
        public void TestReindexKeysWithMultipleArrays()
        {
            // Arrange
            var array1 = new PhpArray { ["a"] = 1, ["b"] = 2 };
            var array2 = new PhpArray { ["c"] = 3, ["d"] = 4 };
            IPhpCallable callback = PhpCallback.Create((ctx, args) => PhpValue.Create(args[0].ToLong() + args[1].ToLong()));

            // Act
            var result = ArrayMapExample.array_map_Optimized(_context, callback, array1, array2);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal(4, result[0].ToLong()); // 1 + 3
            Assert.Equal(6, result[1].ToLong()); // 2 + 4
        }

        /// <summary>
        /// Проверяет случай с большим количеством массивов.
        /// </summary>
        [Fact]
        public void TestWithManyArrays()
        {
            // Arrange
            var arrays = new PhpArray[]
            {
                new PhpArray { 1, 2, 3 },
                new PhpArray { 4, 5, 6 },
                new PhpArray { 7, 8, 9 }
            };
            IPhpCallable callback = PhpCallback.Create((ctx, args) =>
            {
                long sum = 0;
                foreach (var arg in args)
                {
                    sum += arg.ToLong();
                }
                return PhpValue.Create(sum);
            });

            // Act
            var result = ArrayMapExample.array_map_Optimized(_context, callback, arrays);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Equal(12, result[0].ToLong()); // 1 + 4 + 7
            Assert.Equal(15, result[1].ToLong()); // 2 + 5 + 8
            Assert.Equal(18, result[2].ToLong()); // 3 + 6 + 9
        }
    }
}