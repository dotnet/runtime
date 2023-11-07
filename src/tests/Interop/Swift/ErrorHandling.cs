using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Swift;
using System.Text;
using Xunit;

public class ErrorHandlingTests
{
    private const string SwiftLib = "libErrorHandling.dylib";

    [DllImport(SwiftLib, EntryPoint = "$s13ErrorHandling05setMyA7Message5bytes6lengthySPys5UInt8VG_SitF")]
    public static extern void SetErrorMessage(byte[] strBytes, int length);

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s13ErrorHandling018conditionallyThrowA004willD0SiSb_tKF")]
    public unsafe static extern nint conditionallyThrowError(bool willThrow, SwiftError* error);

    [DllImport(SwiftLib, EntryPoint = "$s13ErrorHandling05getMyA7Message4fromSPys5UInt8VGSgSPyAA0dA0OG_tF")]
    public static extern IntPtr GetErrorMessage(IntPtr handle);

    [Fact]
    public unsafe static int TestEntryPoint()
    {
        const string expectedErrorMessage = "Catch me if you can!";
        SetErrorMessageForSwift(expectedErrorMessage);

        SwiftError error;

        // This will throw an error
        conditionallyThrowError(true, &error);
        if (error.Value == IntPtr.Zero) { 
            return 101;
        }
        string errorMessage = GetErrorMessageFromSwift(error, expectedErrorMessage.Length);
        Assert.Equal(expectedErrorMessage, errorMessage);

        // This will not throw an error
        int result = (int) conditionallyThrowError(false, &error);
        if (error.Value != IntPtr.Zero) { 
            return 102;
        }
        Assert.Equal(42, result);
        return 100;
    }
    
    private static void SetErrorMessageForSwift(string message)
    {
        var messageBytes = Encoding.UTF8.GetBytes(message);
        SetErrorMessage(messageBytes, messageBytes.Length);
    }

    private static string GetErrorMessageFromSwift(SwiftError error, int length)
    {
        IntPtr pointer = GetErrorMessage(error.Value);
        byte[] byteArray = new byte[length];
        Marshal.Copy(pointer, byteArray, 0, length);
        return Encoding.UTF8.GetString(byteArray);
    }
}
