// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System;
using System.Buffers;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [GeneratedDllImport(Libraries.Advapi32, EntryPoint = "GetServiceKeyNameW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        internal static unsafe partial bool GetServiceKeyName(SafeServiceHandle? SCMHandle, string displayName, char* KeyName, ref int KeyNameLength);
    }
}
