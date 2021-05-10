// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [DllImport(Libraries.Advapi32, EntryPoint = "StartServiceW", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool StartService(SafeServiceHandle serviceHandle, int argNum, IntPtr argPtrs);
    }
}
