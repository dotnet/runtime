using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

public class GlobalFunctionsTests
{
    [DllImport("libMathLibrary.dylib", EntryPoint = "add")]
    public static extern double add(double a, double b);

    [DllImport("libMathLibrary.dylib", EntryPoint = "subtract")]
    public static extern double subtract(double a, double b);

    [Fact]
    public static int TestEntryPoint()
    {
        double result = 0.0;
        result = add(5.0, 3.0);
        Assert.Equal(8.0, result);

        result = subtract(5.0, 3.0);
        Assert.Equal(2.0, result);

        return 100;
    }
}
