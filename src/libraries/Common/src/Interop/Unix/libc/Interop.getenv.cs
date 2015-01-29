// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class libc
    {
        internal static string getenv(string name)
        {
            return Marshal.PtrToStringAnsi(getenv_core(name)); // TODO: Use correct encoding
        }

        [DllImport(Libraries.Libc, EntryPoint = "getenv")]
        private static extern IntPtr getenv_core(string name);
    }
}
