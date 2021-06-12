// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using TestLibrary;

class SetLastErrorTest
{
    private static class SetLastErrorNative
    {
        private const string SetError = nameof(SetError);

        [DllImport(nameof(SetLastErrorNative), EntryPoint = SetError)]
        public static extern void SetError_Default(int error, [MarshalAs(UnmanagedType.U1)] bool shouldSetError);

        [DllImport(nameof(SetLastErrorNative), EntryPoint = SetError, SetLastError = false)]
        public static extern void SetError_False(int error, [MarshalAs(UnmanagedType.U1)] bool shouldSetError);

        [DllImport(nameof(SetLastErrorNative), EntryPoint = SetError, SetLastError = true)]
        public static extern void SetError_True(int error, [MarshalAs(UnmanagedType.U1)] bool shouldSetError);
    }

    public static void LastErrorHasExpectedValue()
    {
        // Default (same behaviour as SetLastError=false)
        {
            int expected = Marshal.GetLastPInvokeError();
            SetLastErrorNative.SetError_Default(expected + 1, shouldSetError: true);
            int actual = Marshal.GetLastPInvokeError();
            Assert.AreEqual(expected, actual);
        }

        // SetLastError=false
        {
            int expected = Marshal.GetLastPInvokeError();
            SetLastErrorNative.SetError_False(expected + 1, shouldSetError: true);
            int actual = Marshal.GetLastPInvokeError();
            Assert.AreEqual(expected, actual);
        }

        // SetLastError=true
        {
            int expected = Marshal.GetLastPInvokeError();
            expected++;
            SetLastErrorNative.SetError_True(expected, shouldSetError: true);
            int actual = Marshal.GetLastPInvokeError();
            Assert.AreEqual(expected, actual);
        }
    }

    public static void ClearPreviousError()
    {
        // Set the last P/Invoke error to non-zero
        int error = 100;
        Marshal.SetLastPInvokeError(error);

        // Don't actually set the error in the native call.
        // Calling a P/Invoke with SetLastError=true should clear any existing error.
        SetLastErrorNative.SetError_True(error, shouldSetError: false);
        int actual = Marshal.GetLastPInvokeError();
        Assert.AreEqual(0, actual);
    }

    static int Main(string[] args)
    {
        try
        {
            LastErrorHasExpectedValue();
            ClearPreviousError();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Test Failure: {e}");
            return 101;
        }

        return 100;
    }
}
