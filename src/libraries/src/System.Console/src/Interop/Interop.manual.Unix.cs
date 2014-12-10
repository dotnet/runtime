// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

partial class Interop
{
    const string LIBC = "libc";

    [DllImport(LIBC, EntryPoint = "getenv")]
    private static extern IntPtr getenv_core(string name);

    internal static string getenv(string name)
    {
        return Marshal.PtrToStringAnsi(getenv_core(name));
    }

    [DllImport(LIBC)]
    internal static extern unsafe int snprintf(byte* str, IntPtr size, string format, string arg1);

    [DllImport(LIBC)]
    internal static extern unsafe int snprintf(byte* str, IntPtr size, string format, int arg1);
}
