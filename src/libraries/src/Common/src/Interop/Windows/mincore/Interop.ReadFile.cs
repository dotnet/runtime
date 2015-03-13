// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class mincore
    {
        [DllImport(Libraries.CoreFile, SetLastError = true)]
        unsafe internal static extern int ReadFile(
            IntPtr handle,
            byte* bytes,
            int numBytesToRead,
            out int numBytesRead,
            IntPtr mustBeZero);
    }
}
