using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

public class ClosureFunctionsTests
{
    public delegate double OperationDelegate(double a, double b);

    [DllImport("libMathLibrary.dylib", EntryPoint = "add")]
    public static extern double add(double a, double b);

    [DllImport("libMathLibrary.dylib", EntryPoint = "applyOperation")]
    public static extern double applyOperation(double a, double b, OperationDelegate operation);

    [Fact]
    public static int TestEntryPoint()
    {
        OperationDelegate addDelegate = add;

        double result = 0.0;
        result = applyOperation(5.0, 3.0, addDelegate);
        Assert.Equal(8.0, result);

        return 100;
    }
}
