// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [DllImport(Libraries.Kernel32, SetLastError = true)]
        internal static extern unsafe int WriteFile(
            IntPtr handle,
            byte* bytes,
            int numBytesToWrite,
            out int numBytesWritten,
            IntPtr mustBeZero);
    }
}
