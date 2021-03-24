// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [DllImport(Libraries.Advapi32, SetLastError = true)]
        internal static extern bool SetThreadToken(IntPtr ThreadHandle, SafeTokenHandle? hToken);
    }
}
