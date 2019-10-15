// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_MAdvise", SetLastError = true)]
        internal static extern int MAdvise(IntPtr addr, ulong length, MemoryAdvice advice);

        internal enum MemoryAdvice
        {
            MADV_DONTFORK = 1,
        }
    }
}
