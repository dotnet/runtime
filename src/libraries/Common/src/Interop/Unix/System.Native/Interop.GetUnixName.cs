// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetUnixName")]
        private static extern IntPtr GetUnixNamePrivate();

        internal static string GetUnixName()
        {
            IntPtr ptr = GetUnixNamePrivate();
            return Marshal.PtrToStringAnsi(ptr)!;
        }
    }
}
