// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
