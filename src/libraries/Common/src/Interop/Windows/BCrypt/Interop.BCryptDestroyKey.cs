// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class BCrypt
    {
        [GeneratedDllImport(Libraries.BCrypt, CharSet = CharSet.Unicode)]
        internal static partial NTSTATUS BCryptDestroyKey(IntPtr hKey);
    }
}
