// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        internal const int REG_NOTIFY_CHANGE_NAME = 0x1;
        internal const int REG_NOTIFY_CHANGE_ATTRIBUTES = 0x2;
        internal const int REG_NOTIFY_CHANGE_LAST_SET = 0x4;
        internal const int REG_NOTIFY_CHANGE_SECURITY = 0x8;
        internal const int REG_NOTIFY_THREAD_AGNOSTIC = 0x10000000;

        [LibraryImport(Libraries.Advapi32, EntryPoint = "RegNotifyChangeKeyValue", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial int RegNotifyChangeKeyValue(
            SafeHandle hKey,
            [MarshalAs(UnmanagedType.Bool)] bool watchSubtree,
            uint notifyFilter,
            SafeHandle hEvent,
            [MarshalAs(UnmanagedType.Bool)] bool asynchronous);
    }
}
