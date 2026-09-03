// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Swift;
using System.Text;
using Xunit;
using TestLibrary;

[PlatformSpecific(TestPlatforms.AnyApple)]
public class ErrorHandlingTests
{
    private const string SwiftLib = "libSwiftErrorHandling.dylib";

    [StructLayout(LayoutKind.Sequential)]
    private readonly unsafe struct BufferPointer
    {
        private readonly byte* _baseAddress;
        private readonly nint _count;

        internal BufferPointer(byte* baseAddress, nint count)
        {
            _baseAddress = baseAddress;
            _count = count;
        }

        internal byte* BaseAddress => _baseAddress;
        internal nint Count => _count;
    }

    [DllImport(SwiftLib, EntryPoint = "$s18SwiftErrorHandling05setMyB7Message7message6lengthySPys6UInt16VG_s5Int32VtF", CharSet = CharSet.Unicode)]
    public static extern void SetErrorMessage(string message, int length);

    [UnmanagedCallConv(CallConvs = [typeof(CallConvSwift)])]
    [DllImport(SwiftLib, EntryPoint = "$s18SwiftErrorHandling018conditionallyThrowB004willE0s5Int32VAE_tKF")]
    public static extern nint conditionallyThrowError(int willThrow, ref SwiftError error);

    [UnmanagedCallConv(CallConvs = [typeof(CallConvSwift)])]
    [DllImport(SwiftLib, EntryPoint = "$s18SwiftErrorHandling018conditionallyThrowB004willE0s5Int32VAE_tKF")]
    public static extern nint conditionallyThrowErrorOnStack(int willThrow, int dummy1, int dummy2, int dummy3, int dummy4, int dummy5, int dummy6, int dummy7, int dummy8, int dummy9, ref SwiftError error);

    [UnmanagedCallConv(CallConvs = [typeof(CallConvSwift)])]
    [DllImport(SwiftLib, EntryPoint = "conditionallyThrowErrorInFirstStackSlot")]
    public static extern nint conditionallyThrowErrorInFirstStackSlot(int willThrow, int dummy1, int dummy2, int dummy3, int dummy4, int dummy5, int dummy6, int dummy7, ref SwiftError error);

    [UnmanagedCallConv(CallConvs = [typeof(CallConvSwift)])]
    [DllImport(SwiftLib, EntryPoint = "conditionallyThrowErrorAfterSixArguments")]
    public static extern nint conditionallyThrowErrorAfterSixArguments(int willThrow, int dummy1, int dummy2, int dummy3, int dummy4, int dummy5, ref SwiftError error);

    [UnmanagedCallConv(CallConvs = [typeof(CallConvSwift)])]
    [DllImport(SwiftLib, EntryPoint = "validateSwiftAbiOneWord")]
    private static extern nuint ValidateSwiftAbiOneWord(
        nint key,
        BufferPointer buffer1,
        BufferPointer buffer2,
        BufferPointer buffer3,
        BufferPointer buffer4,
        BufferPointer buffer5,
        out SwiftError error);

    [UnmanagedCallConv(CallConvs = [typeof(CallConvSwift)])]
    [DllImport(SwiftLib, EntryPoint = "$s18SwiftErrorHandling26nativeFunctionWithCallback03setB0_ys5Int32V_yAEXEtF")]
    public static extern unsafe void NativeFunctionWithCallback(int setError, delegate* unmanaged[Swift]<SwiftError*, int, void> callback, SwiftError* error);

    [UnmanagedCallConv(CallConvs = [typeof(CallConvSwift)])]
    [DllImport(SwiftLib, EntryPoint = "$s18SwiftErrorHandling26nativeFunctionWithCallback5value03setB0_s5Int32VAF_A3F_AFtXEtF")]
    public static extern unsafe int NativeFunctionWithCallback(int value, int setError, delegate* unmanaged[Swift]<SwiftError*, int, int, int> callback, SwiftError* error);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvSwift)])]
    private static unsafe void ConditionallySetErrorTo21(SwiftError* error, int setError) {
        if (setError != 0)
        {
            *error = new SwiftError((void*)21);
        }
        else
        {
            *error = new SwiftError(null);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvSwift)])]
    private static unsafe int ConditionallySetErrorAndReturn(SwiftError* error, int value, int setError) {
        if (setError != 0)
        {
            *error = new SwiftError((void*)value);
        }
        else
        {
            *error = new SwiftError(null);
        }

        return (value * 2);
    }

    [DllImport(SwiftLib, EntryPoint = "$s18SwiftErrorHandling05getMyB7Message4from13messageLengthSPys6UInt16VGSgs0B0_p_s5Int32VztF")]
    public unsafe static extern void* GetErrorMessage(void* handle, out int length);

    [DllImport(SwiftLib, EntryPoint = "$s18SwiftErrorHandling16freeStringBuffer6bufferySpys6UInt16VG_tF")]
    public unsafe static extern void FreeErrorMessageBuffer(void* stringPtr);

    [Fact]
    public unsafe static void TestSwiftErrorThrown()
    {
        const string expectedErrorMessage = "Catch me if you can!";
        SetErrorMessageForSwift(expectedErrorMessage);

        SwiftError error = new SwiftError();

        // This will throw an error
        conditionallyThrowError(1, ref error);
        Assert.True(error.Value != null, "A Swift error was expected to be thrown.");

        string errorMessage = GetErrorMessageFromSwift(error);
        Assert.True(errorMessage == expectedErrorMessage, string.Format("The error message retrieved from Swift does not match the expected message. Expected: {0}, Actual: {1}", expectedErrorMessage, errorMessage));
    }

    [Fact]
    public unsafe static void TestSwiftErrorNotThrown()
    {
        const string expectedErrorMessage = "Catch me if you can!";
        SetErrorMessageForSwift(expectedErrorMessage);

        SwiftError error = new SwiftError();

        // This will not throw an error
        int result = (int)conditionallyThrowError(0, ref error);

        Assert.True(error.Value == null, "No Swift error was expected to be thrown.");
        Assert.True(result == 42, "The result from Swift does not match the expected value.");
    }

    [Fact]
    public unsafe static void TestSwiftErrorOnStackThrown()
    {
        const string expectedErrorMessage = "Catch me if you can!";
        SetErrorMessageForSwift(expectedErrorMessage);

        SwiftError error = new SwiftError();

        int i = 0;
        // This will throw an error
        conditionallyThrowErrorOnStack(1, i + 1, i + 2, i + 3, i + 4, i + 5, i + 6, i + 7, i + 8, i + 9, ref error);
        Assert.True(error.Value != null, "A Swift error was expected to be thrown.");

        string errorMessage = GetErrorMessageFromSwift(error);
        Assert.True(errorMessage == expectedErrorMessage, string.Format("The error message retrieved from Swift does not match the expected message. Expected: {0}, Actual: {1}", expectedErrorMessage, errorMessage));
    }

    [Fact]
    public unsafe static void TestSwiftErrorOnStackNotThrown()
    {
        const string expectedErrorMessage = "Catch me if you can!";
        SetErrorMessageForSwift(expectedErrorMessage);

        SwiftError error = new SwiftError();

        int i = 0;
        // This will not throw an error
        int result = (int)conditionallyThrowErrorOnStack(0, i + 1, i + 2, i + 3, i + 4, i + 5, i + 6, i + 7, i + 8, i + 9, ref error);

        Assert.True(error.Value == null, "No Swift error was expected to be thrown.");
        Assert.True(result == 42, "The result from Swift does not match the expected value.");
    }

    [Fact]
    public static unsafe void TestSwiftErrorInFirstStackSlotNotThrown()
    {
        SwiftError error = new SwiftError();

        int i = 0;
        int result = (int)conditionallyThrowErrorInFirstStackSlot(0, i + 1, i + 2, i + 3, i + 4, i + 5, i + 6, i + 7, ref error);

        Assert.True(error.Value == null, "No Swift error was expected to be thrown.");
        Assert.True(result == 42, "The result from Swift does not match the expected value.");
    }

    [Fact]
    public static unsafe void TestSwiftErrorAfterSixArgumentsThrown()
    {
        const string expectedErrorMessage = "Catch me if you can!";
        SetErrorMessageForSwift(expectedErrorMessage);

        SwiftError error = new SwiftError();

        int i = 0;
        conditionallyThrowErrorAfterSixArguments(1, i + 1, i + 2, i + 3, i + 4, i + 5, ref error);
        Assert.True(error.Value != null, "A Swift error was expected to be thrown.");

        string errorMessage = GetErrorMessageFromSwift(error);
        Assert.True(errorMessage == expectedErrorMessage, string.Format("The error message retrieved from Swift does not match the expected message. Expected: {0}, Actual: {1}", expectedErrorMessage, errorMessage));
    }

    [Fact]
    public static unsafe void TestSwiftErrorAfterSixArgumentsNotThrown()
    {
        SwiftError error = new SwiftError();

        int i = 0;
        int result = (int)conditionallyThrowErrorAfterSixArguments(0, i + 1, i + 2, i + 3, i + 4, i + 5, ref error);

        Assert.True(error.Value == null, "No Swift error was expected to be thrown.");
        Assert.True(result == 42, "The result from Swift does not match the expected value.");
    }

    [Fact]
    public static unsafe void TestSwiftAbiOneWordArgument()
    {
        byte* values = stackalloc byte[6];
        BufferPointer buffer1 = new BufferPointer(values + 1, 22);
        BufferPointer buffer2 = new BufferPointer(values + 2, 33);
        BufferPointer buffer3 = new BufferPointer(values + 3, 44);
        BufferPointer buffer4 = new BufferPointer(values + 4, 55);
        BufferPointer buffer5 = new BufferPointer(values + 5, 66);

        nuint actual = ValidateSwiftAbiOneWord(
            (nint)values,
            buffer1,
            buffer2,
            buffer3,
            buffer4,
            buffer5,
            out SwiftError error);

        Assert.Equal(Hash((nuint)values, buffer1, buffer2, buffer3, buffer4, buffer5), actual);
        Assert.True(error.Value == null, "No Swift error was expected to be thrown.");
    }

    [Fact]
    public static unsafe void TestUnmanagedCallersOnly()
    {
        SwiftError error;
        int expectedValue = 21;
        NativeFunctionWithCallback(1, &ConditionallySetErrorTo21, &error);

        int value = (int)error.Value;
        Assert.True(value == expectedValue, string.Format("The value retrieved does not match the expected value. Expected: {0}, Actual: {1}", expectedValue, value));

        NativeFunctionWithCallback(0, &ConditionallySetErrorTo21, &error);

        Assert.True(error.Value == null, "Expected SwiftError value to be null.");
    }

    [Fact]
    public static unsafe void TestUnmanagedCallersOnlyWithReturn()
    {
        SwiftError error;
        int expectedValue = 42;
        int retValue = NativeFunctionWithCallback(expectedValue, 1, &ConditionallySetErrorAndReturn, &error);

        int value = (int)error.Value;
        Assert.True(value == expectedValue, string.Format("The value retrieved does not match the expected value. Expected: {0}, Actual: {1}", expectedValue, value));
        Assert.True(retValue == (expectedValue * 2), string.Format("Return value does not match expected value. Expected: {0}, Actual: {1}", (expectedValue * 2), retValue));

        retValue = NativeFunctionWithCallback(expectedValue, 0, &ConditionallySetErrorAndReturn, &error);

        Assert.True(error.Value == null, "Expected SwiftError value to be null.");
        Assert.True(retValue == (expectedValue * 2), string.Format("Return value does not match expected value. Expected: {0}, Actual: {1}", (expectedValue * 2), retValue));
    }

    private static void SetErrorMessageForSwift(string message)
    {
        SetErrorMessage(message, message.Length);
    }

    private unsafe static string GetErrorMessageFromSwift(SwiftError error)
    {
        void* pointer = GetErrorMessage(error.Value, out int messageLength);
        string errorMessage = Marshal.PtrToStringUni((IntPtr)pointer, messageLength);
        FreeErrorMessageBuffer(pointer);
        return errorMessage;
    }

    private static unsafe nuint Hash(
        nuint key,
        BufferPointer buffer1,
        BufferPointer buffer2,
        BufferPointer buffer3,
        BufferPointer buffer4,
        BufferPointer buffer5)
    {
        nuint hash = Mix(FnvOffsetBasis, key);
        hash = HashBuffer(hash, buffer1);
        hash = HashBuffer(hash, buffer2);
        hash = HashBuffer(hash, buffer3);
        hash = HashBuffer(hash, buffer4);
        return HashBuffer(hash, buffer5);
    }

    private static unsafe nuint HashBuffer(nuint hash, BufferPointer buffer)
    {
        hash = Mix(hash, (nuint)buffer.BaseAddress);
        return Mix(hash, (nuint)buffer.Count);
    }

    private static nuint Mix(nuint hash, nuint value) =>
        unchecked((hash ^ value) * (nuint)1099511628211);

    private static nuint FnvOffsetBasis =>
        unchecked((nuint)14695981039346656037UL);
}
