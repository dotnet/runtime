using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

public class DefaultParamsTests
{
    [DllImport("libMathLibrary.dylib", EntryPoint = "multiply")]
    public static extern double multiply(double a, double b = 1.0);

    [Fact]
    public static int TestEntryPoint()
    {
        double result = 0.0;
        result = multiply(5.0);
        Assert.Equal(5.0, result);

        return 100;
    }
}
