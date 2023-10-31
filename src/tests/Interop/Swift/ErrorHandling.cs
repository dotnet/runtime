using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Swift;
using Xunit;

public class ErrorHandlingTests
{
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport("libErrorHandling.dylib", EntryPoint = "$s13ErrorHandling22someFuncThatMightThrow04willG05dummySiSb_SVtKF")]
    public unsafe static extern nint SomeFuncThatMightThrow (bool willThrow, SwiftError* error);

    [DllImport("libErrorHandling.dylib", EntryPoint = "$s13ErrorHandling06handleA04fromySPyAA02MyA0OG_tF")]
    public static extern void handleError (IntPtr handle);

    [Fact]
    public unsafe static int TestEntryPoint()
    {
        SwiftError errorHandle = new SwiftError();
        SomeFuncThatMightThrow(true, &errorHandle);
        if (errorHandle.Value != IntPtr.Zero) {
            Console.WriteLine($"Error instance from R21: 0x{errorHandle.Value:X16}");
            handleError(errorHandle.Value);
            return 100;
        } else {
            return 101;
        }
    }
}
