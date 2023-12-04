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

    [DllImport(SwiftLib, EntryPoint = "$s18SwiftErrorHandling05setMyB7Message5bytes6lengthySPys5UInt8VG_SitF")]
    public static extern void SetErrorMessage(byte[] strBytes, int length);

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s18SwiftErrorHandling018conditionallyThrowB004willE0SiSb_tKF")]
    public unsafe static extern nint conditionallyThrowError(bool willThrow, SwiftError* error);

    [DllImport(SwiftLib, EntryPoint = "$s18SwiftErrorHandling05getMyB7Message4fromSPys5UInt8VGSgs0B0_p_tF")]
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

        string errorMessage = GetErrorMessageFromSwift(error, expectedErrorMessage.Length);
        Assert.True(errorMessage == expectedErrorMessage, "The error message retrieved from Swift does not match the expected message.");
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
        var messageBytes = Encoding.UTF8.GetBytes(message);
        SetErrorMessage(messageBytes, messageBytes.Length);
    }

    private static string GetErrorMessageFromSwift(SwiftError error, int length)
    {
        IntPtr pointer = GetErrorMessage(error.Value);
        byte[] byteArray = new byte[length];
        Marshal.Copy(pointer, byteArray, 0, length);
        Marshal.FreeCoTaskMem(pointer);
        return Encoding.UTF8.GetString(byteArray);
    }
}
