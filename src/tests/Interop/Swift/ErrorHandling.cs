using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

public class ErrorHandlingTests
{
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport("libErrorHandling.dylib", EntryPoint = "$s13ErrorHandling22someFuncThatMightThrow5dummy04willG0SiSV_SbtKF")]
    public static extern nint SomeFuncThatMightThrow ([SwiftErrorReturn]ref IntPtr error, bool willThrow);

    [DllImport("libErrorHandling.dylib", EntryPoint = "$s13ErrorHandling06handleA04fromySPyAA02MyA0OG_tF")]
    public static extern void handleError (IntPtr handle);

    [Fact]
    public static int TestEntryPoint()
    {
        IntPtr errorHandle = IntPtr.Zero; 
        SomeFuncThatMightThrow(ref errorHandle, true);
        if (errorHandle != IntPtr.Zero) {
            Console.WriteLine($"Error instance from R21: 0x{errorHandle:X16}");
            handleError(errorHandle);
            return 100;
        } else {
            return 101;
        }
    }
}
