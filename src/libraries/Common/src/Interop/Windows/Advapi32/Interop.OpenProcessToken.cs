// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Advapi32
    {
        [DllImport(Libraries.Advapi32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool OpenProcessToken(SafeProcessHandle ProcessHandle, int DesiredAccess, out SafeTokenHandle TokenHandle);
    }
}
