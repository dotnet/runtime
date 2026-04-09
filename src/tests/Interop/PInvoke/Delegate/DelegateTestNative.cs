// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

static class DelegateTestNative
{
    public delegate int TestDelegate();

    public struct CallbackWithExpectedValue
    {
        public int expectedValue;
        public TestDelegate del;
    }

    public struct DispatchDelegateWithExpectedValue
    {
        public int expectedValue;
        [MarshalAs(UnmanagedType.IDispatch)]

        public TestDelegate del;
    }

    // Delegate as Function Pointer tests.

    [DllImport(nameof(DelegateTestNative))]
    public static extern bool ValidateDelegateReturnsExpected(int i, TestDelegate @delegate);
    [DllImport(nameof(DelegateTestNative))]
    public static extern bool ReplaceDelegate(int expectedValue, ref TestDelegate pDelegate, out int pNewExpectedValue);
    [DllImport(nameof(DelegateTestNative))]
    public static extern void GetNativeTestFunction(out TestDelegate pDelegate, out int pExpectedValue);
    [DllImport(nameof(DelegateTestNative))]
    public static extern TestDelegate GetNativeTestFunctionReturned(out int pExpectedValue);
    [DllImport(nameof(DelegateTestNative))]
    public static extern bool ValidateCallbackWithValue(CallbackWithExpectedValue val);
    [DllImport(nameof(DelegateTestNative))]
    public static extern bool ValidateAndUpdateCallbackWithValue(ref CallbackWithExpectedValue val);
    [DllImport(nameof(DelegateTestNative))]
    public static extern bool GetNativeCallbackAndValue(out CallbackWithExpectedValue val);

    // Delegate as IDispatch tests

    [DllImport(nameof(DelegateTestNative))]
    public static extern bool ValidateDelegateValueMatchesExpected(int i, [MarshalAs(UnmanagedType.IDispatch)] TestDelegate @delegate);
    [DllImport(nameof(DelegateTestNative))]
    public static extern bool ValidateDelegateValueMatchesExpectedAndClear(int i, [MarshalAs(UnmanagedType.IDispatch)] ref TestDelegate @delegate);
    [DllImport(nameof(DelegateTestNative))]
    public static extern bool DuplicateDelegate(int i, [MarshalAs(UnmanagedType.IDispatch)] TestDelegate delegateIn, [MarshalAs(UnmanagedType.IDispatch)] out TestDelegate delegateOut);
    [DllImport(nameof(DelegateTestNative))]
    [return: MarshalAs(UnmanagedType.IDispatch)]
    public static extern TestDelegate DuplicateDelegateReturned([MarshalAs(UnmanagedType.IDispatch)] TestDelegate delegateIn);
    [DllImport(nameof(DelegateTestNative))]
    public static extern bool ValidateStructDelegateValueMatchesExpected(DispatchDelegateWithExpectedValue dispatch);
    [DllImport(nameof(DelegateTestNative))]
    public static extern bool ValidateDelegateValueMatchesExpectedAndClearStruct(ref DispatchDelegateWithExpectedValue dispatch);
    [DllImport(nameof(DelegateTestNative))]
    public static extern bool DuplicateStruct(DispatchDelegateWithExpectedValue dispatchIn, out DispatchDelegateWithExpectedValue dispatchOut);

    [DllImport(nameof(DelegateTestNative), EntryPoint = "Invalid", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr MarshalDelegateAsInterface([MarshalAs(UnmanagedType.Interface)] TestDelegate del);
}
