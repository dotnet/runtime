// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_MLock", SetLastError = true)]
        internal static extern int MLock(IntPtr addr, ulong len);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_MUnlock", SetLastError = true)]
        internal static extern int MUnlock(IntPtr addr, ulong len);
    }
}
