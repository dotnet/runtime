// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [DllImport(Libraries.SystemNative, SetLastError = true)]
        internal static extern int MAdvise(IntPtr addr, ulong length, MemoryAdvice advice);

        internal enum MemoryAdvice
        {
            MADV_DONTFORK = 1,
        }
    }
}
