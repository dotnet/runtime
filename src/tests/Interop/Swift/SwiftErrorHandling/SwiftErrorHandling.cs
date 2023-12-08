// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Swift;
using System.Text;
using Xunit;

public class ErrorHandlingTests
{
    private const string SwiftLib = "libSwiftErrorHandling.dylib";

    [DllImport(SwiftLib, EntryPoint = "$s18SwiftErrorHandling05setMyB7Message5bytesySPys4Int8VG_tF")]
    public static extern void SetErrorMessage(byte[] strBytes);

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s18SwiftErrorHandling018conditionallyThrowB004willE0SiSb_tKF")]
    public unsafe static extern nint conditionallyThrowError(bool willThrow, SwiftError* error);

    [DllImport(SwiftLib, EntryPoint = "$s18SwiftErrorHandling05getMyB7Message4fromSPys4Int8VGSgs0B0_p_tF")]
    public static extern IntPtr GetErrorMessage(IntPtr handle);

    [Fact]
    public unsafe static void TestSwiftErrorThrown()
    {
        const string expectedErrorMessage = "Catch me if you can!";
        SetErrorMessageForSwift(expectedErrorMessage);

        SwiftError error;

        // This will throw an error
        conditionallyThrowError(true, &error);
        Assert.True(error.Value != IntPtr.Zero, "A Swift error was expected to be thrown.");

        string errorMessage = GetErrorMessageFromSwift(error);
        Assert.True(errorMessage == expectedErrorMessage, string.Format("The error message retrieved from Swift does not match the expected message. Expected: {0}, Actual: {1}", expectedErrorMessage, errorMessage));
    }

    [Fact]
    public unsafe static void TestSwiftErrorNotThrown()
    {
        const string expectedErrorMessage = "Catch me if you can!";
        SetErrorMessageForSwift(expectedErrorMessage);

        SwiftError error;

        // This will not throw an error
        int result = (int)conditionallyThrowError(false, &error);

        Assert.True(error.Value == IntPtr.Zero, "No Swift error was expected to be thrown.");
        Assert.True(result == 42, "The result from Swift does not match the expected value.");
    }
    
    private static void SetErrorMessageForSwift(string message)
    {
        byte[] messageBytes = Encoding.UTF8.GetBytes(message);
        SetErrorMessage(messageBytes);
    }

    private unsafe static string GetErrorMessageFromSwift(SwiftError error)
    {
        IntPtr pointer = GetErrorMessage(error.Value);
        string errorMessage = Marshal.PtrToStringUTF8(pointer);
        NativeMemory.Free((void*)pointer);
        return errorMessage;
    }
}
