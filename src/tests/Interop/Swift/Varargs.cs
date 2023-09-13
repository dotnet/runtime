using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

public class VarargsTests
{
    [DllImport("libMathLibrary.dylib", EntryPoint = "average")]
    public static extern double average([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] double[] numbers, int count);

    [Fact]
    public static int TestEntryPoint()
    {
        double result = 0.0;
        double[] numbers = { 1.0, 2.0, 3.0 };
        result = average(numbers, numbers.Length);
        Assert.Equal(6.0, result);

        return 100;
    }
}
