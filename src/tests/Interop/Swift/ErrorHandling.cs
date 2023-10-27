using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

public class ErrorHandlingTests
{
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport("libErrorHandling.dylib", EntryPoint = "$s13ErrorHandling22someFuncThatMightThrow5dummy04willG0SiSV_SbtKF")]
    public static extern nint SomeFuncThatMightThrow (ref SwiftError error, bool willThrow);

    [DllImport("libErrorHandling.dylib", EntryPoint = "$s13ErrorHandling06handleA04fromySPyAA02MyA0OG_tF")]
    public static extern void handleError (IntPtr handle);

    [Fact]
    public static int TestEntryPoint()
    {
        SwiftError errorHandle = new SwiftError();
        SomeFuncThatMightThrow(ref errorHandle, true);
        if (errorHandle.Value != IntPtr.Zero) {
            Console.WriteLine($"Error instance from R21: 0x{errorHandle.Value:X16}");
            handleError(errorHandle.Value);
            return 100;
        } else {
            return 101;
        }
    }
}
