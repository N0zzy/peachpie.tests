using Pchp.Core;
using peachpie.items.Arrays;
using Xunit.Abstractions;

/// <summary>
/// https://www.php.net/manual/en/function.array-map.php
/// </summary>
public class ArrayMapTests
{
    private readonly Context _context = Context.CreateEmpty();
    private readonly ITestOutputHelper _testOutputHelper;

    public ArrayMapTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    /// <summary>
    /// example 1
    /// </summary>
    [Fact]
    public void Test_ArrayMap_Cube()
    {
        // Arrange
        var input = new PhpArray { 1, 2, 3, 4, 5 };
        var callback = PhpCallback.Create((ctx, args) => PhpValue.Create(args[0].ToLong() * args[0].ToLong() * args[0].ToLong()));

        // Act
        var result = ArrayMapExample.array_map_Optimized(_context, callback, input);

        // Assert
        Assert.Equal(1, result[0].ToLong());
        Assert.Equal(8, result[1].ToLong());
        Assert.Equal(27, result[2].ToLong());
        Assert.Equal(64, result[3].ToLong());
        Assert.Equal(125, result[4].ToLong());
    }
    /// <summary>
    /// example 2
    /// </summary>
    [Fact]
    public void Test_ArrayMap_LambdaFunction()
    {
        // Arrange
        var input = new PhpArray { 1, 2, 3, 4, 5 };
        var callback = PhpCallback.Create((ctx, args) => PhpValue.Create(args[0].ToLong() * 2));

        // Act
        var result = ArrayMapExample.array_map_Optimized(_context, callback, input);

        // Assert
        Assert.Equal(2, result[0].ToLong());
        Assert.Equal(4, result[1].ToLong());
        Assert.Equal(6, result[2].ToLong());
        Assert.Equal(8, result[3].ToLong());
        Assert.Equal(10, result[4].ToLong());
    }
    
    /// <summary>
    /// example 3
    /// </summary>
    [Fact]
    public void Test_ArrayMap_MultipleArrays()
    {
       
        // Arrange
        var array1 = new PhpArray { 1, 2, 3, 4, 5 };
        var array2 = new PhpArray { "uno", "dos", "tres", "cuatro", "cinco" };
        
        // Callback
        var showSpanishCallback = PhpCallback.Create((ctx, args) 
            => PhpValue.Create($"The number {args[0]} is called {args[1]} in Spanish"));

        // Act
        var result = ArrayMapExample.array_map_Optimized(_context, showSpanishCallback, array1, array2);
        
        for (int i = 0; i < result.Count; i++)
        {
            _testOutputHelper.WriteLine(result[i].ToString());
        }
        
        // Assert
        // Assert.Equal("The number 1 is called uno in Spanish", result[0].ToString());
        // Assert.Equal("The number 2 is called dos in Spanish", result[1].ToString());
        // Assert.Equal("The number 3 is called tres in Spanish", result[2].ToString());
        // Assert.Equal("The number 4 is called cuatro in Spanish", result[3].ToString());
        // Assert.Equal("The number 5 is called cinco in Spanish", result[4].ToString());
    }
    
    /// <summary>
    /// example 4
    /// </summary>
    [Fact]
    public void Test_ArrayMap_ZipOperation()
    {
        // Arrange
        var array1 = new PhpArray(new[] { 1, 2, 3, 4, 5 });
        var array2 = new PhpArray(new[] { "one", "two", "three", "four", "five" });
        var array3 = new PhpArray(new[] { "uno", "dos", "tres", "cuatro", "cinco" });

        // Act
        var result = ArrayMapExample.array_map_Optimized(null, null, array1, array2, array3);

        // Assert
        var expected = new[]
        {
            new PhpArray { { 0, 1 }, { 1, "one" }, { 2, "uno" } },
            new PhpArray { { 0, 2 }, { 1, "two" }, { 2, "dos" } },
            new PhpArray { { 0, 3 }, { 1, "three" }, { 2, "tres" } },
            new PhpArray { { 0, 4 }, { 1, "four" }, { 2, "cuatro" } },
            new PhpArray { { 0, 5 }, { 1, "five" }, { 2, "cinco" } }
        };

        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i][0], result[i].AsArray()[0]);
            Assert.Equal(expected[i][1], result[i].AsArray()[1]);
            Assert.Equal(expected[i][2], result[i].AsArray()[2]);
        }
    }
    
    /// <summary>
    /// example 5
    /// </summary>
    [Fact]
    public void Test_ArrayMap_NullCallback()
    {
        // Arrange
        var input = new PhpArray(new[] { 1, 2, 3 });

        // Act
        var result = ArrayMapExample.array_map_Optimized(null, null, input);

        // Assert
        Assert.Equal(1, result[0].ToLong());
        Assert.Equal(2, result[1].ToLong());
        Assert.Equal(3, result[2].ToLong());
    }
    
    /// <summary>
    /// example 6
    /// </summary>
    [Fact]
    public void Test_ArrayMap_WithStringKeys()
    {
        // Arrange
        var ctx = Context.CreateEmpty();

        // Input array with string key
        var arr = new PhpArray
        {
            { "stringkey", "value" }
        };

        // Callback functions
        IPhpCallable cb1 = PhpCallback.Create((ctx, args) => new PhpArray { args[0] });

        IPhpCallable cb2 = PhpCallback.Create((ctx, args) => new PhpArray { args[0], args[1] });

        // Act
        var result1 = ArrayMapExample.array_map_Optimized(ctx, cb1, arr);
        var result2 = ArrayMapExample.array_map_Optimized(ctx, cb2, arr, arr);
        var result3 = ArrayMapExample.array_map_Optimized(ctx, null, arr);
        var result4 = ArrayMapExample.array_map_Optimized(ctx, null, arr, arr);

        // Assert
        // Check result1: array_map('cb1', $arr)
        Assert.Single(result1);
        Assert.True(result1.ContainsKey("stringkey"));
        var value1 = result1["stringkey"].AsArray();
        Assert.Single(value1);
        Assert.Equal("value", value1[0].ToString());

        // Check result2: array_map('cb2', $arr, $arr)
        Assert.Single(result2);
        Assert.True(result2.ContainsKey("stringkey"));
        var value2 = result2["stringkey"].AsArray();
        Assert.Equal(2, value2.Count);
        Assert.Equal("value", value2[0].ToString());
        Assert.Equal("value", value2[1].ToString());

        // Check result3: array_map(null, $arr)
        Assert.Single(result3);
        Assert.True(result3.ContainsKey("stringkey"));
        Assert.Equal("value", result3["stringkey"].ToString());

        // Check result4: array_map(null, $arr, $arr)
        Assert.Single(result4);
        Assert.True(result4.ContainsKey("stringkey"));
        var value4 = result4["stringkey"].AsArray();
        Assert.Equal(2, value4.Count);
        Assert.Equal("value", value4[0].ToString());
        Assert.Equal("value", value4[1].ToString());
    }
    
    /// <summary>
    /// example 7
    /// </summary>
    [Fact]
    public void Test_ArrayMap_AssociativeArrays()
    {
        // Arrange
        var arr = new PhpArray
        {
            ["v1"] = PhpValue.Create("First release"),
            ["v2"] = PhpValue.Create("Second release"),
            ["v3"] = PhpValue.Create("Third release")
        };

        var keys = new PhpArray(arr.Keys.Select(k => PhpValue.Create(k.ToString())));
        var values = new PhpArray(arr.Values);

        var callback = PhpCallback.Create((ctx, args) => PhpValue.Create(
            $"{args[0].ToString()} was the {args[1].ToString()}"
        ));

        // Act
        var result = ArrayMapExample.array_map_Optimized(null, callback, keys, values);

        // Assert
        Assert.Equal("v1 was the First release", result[0].ToString());
        Assert.Equal("v2 was the Second release", result[1].ToString());
        Assert.Equal("v3 was the Third release", result[2].ToString());
    }
}