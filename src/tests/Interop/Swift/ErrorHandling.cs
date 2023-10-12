using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

public class GlobalFunctionsTests
{
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport("libErrorHandling.dylib", EntryPoint = "$s13ErrorHandling22someFuncThatMightThrowSiyKF")]
    public static extern nint SomeFuncThatMightThrow ([SwiftErrorReturn]ref IntPtr error);

    [Fact]
    public static int TestEntryPoint()
    {
        IntPtr errorHandle = IntPtr.Zero; 
        nint result = SomeFuncThatMightThrow(ref errorHandle);
        Console.WriteLine(result);
        if (errorHandle != IntPtr.Zero) {
            return 100;
        } else {
            Console.WriteLine(errorHandle);
            return 101;
        }
    }
}
