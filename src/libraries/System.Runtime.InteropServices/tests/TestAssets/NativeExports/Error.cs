// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace NativeExports
{
    public static unsafe class Error
    {
        private class Kernel32
        {
            [DllImport(nameof(Kernel32))]
            public static extern void SetLastError(int error);

            [DllImport(nameof(Kernel32))]
            public static extern int GetLastError();
        }

        private class @libc
        {
            [DllImport("libc")]
            internal static unsafe extern int* __errno_location();

            [DllImport("libc")]
            internal static unsafe extern int* __error();
        }

        [UnmanagedCallersOnly(EntryPoint = "set_error")]
        public static int SetError(int error, byte shouldSetError)
        {
            if (shouldSetError != 0)
                SetLastError(error);

            return error;
        }

        [UnmanagedCallersOnly(EntryPoint = "return_error")]
        public static int ReturnError(int error) => error;

        [UnmanagedCallersOnly(EntryPoint = "set_error_out")]
        public static void SetErrorOut(int error, int* errorValue)
        {
            *errorValue = error;
        }

        [UnmanagedCallersOnly(EntryPoint = "set_error_ref")]
        public static void SetErrorRef(int error, int* errorValue)
        {
            *errorValue += error;
        }

        [UnmanagedCallersOnly(EntryPoint = "set_constant_error_out")]
        public static void SetConstantErrorOut(int* errorValue)
        {
            *errorValue = 0;
        }

        [UnmanagedCallersOnly(EntryPoint = "return_error_with_output")]
        public static int ReturnErrorWithOutput(int error, int* output)
        {
            *output = 42;
            return error;
        }

        [UnmanagedCallersOnly(EntryPoint = "return_error_with_input")]
        public static int ReturnErrorWithInput(int error, int* input)
        {
            return *input == 42 ? error : -1;
        }

        [UnmanagedCallersOnly(EntryPoint = "return_error_with_hidden_return")]
        public static int ReturnErrorWithHiddenReturn(int error, int* value)
        {
            *value = 42;
            return error;
        }

        [UnmanagedCallersOnly(EntryPoint = "set_output_and_error_out")]
        public static void SetOutputAndErrorOut(int error, int* output, int* errorValue)
        {
            *output = 42;
            *errorValue = error;
        }

        [UnmanagedCallersOnly(EntryPoint = "set_error_return_string")]
        public static ushort* SetErrorReturnString(int error, byte shouldSetError, ushort* errorString)
        {
            ushort* ret = (ushort*)Marshal.StringToCoTaskMemUni(new string((char*)errorString));

            if (shouldSetError != 0)
                SetLastError(error);

            return ret;
        }

        private static void SetLastError(int error)
        {
            if (OperatingSystem.IsWindows())
            {
                Kernel32.SetLastError(error);
            }
            else if (OperatingSystem.IsMacOS())
            {
                *libc.__error() = error;
            }
            else if (OperatingSystem.IsLinux())
            {
                *libc.__errno_location() = error;
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
