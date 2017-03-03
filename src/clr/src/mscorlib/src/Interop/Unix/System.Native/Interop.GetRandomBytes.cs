// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal static unsafe void GetRandomBytes(byte* bytes, int byteCount)
    {
        // We want to avoid dependencies on the Crypto library when compiling in CoreCLR. This
        // will use the existing PAL implementation.
        byte[] buffer = new byte[byteCount];
        Microsoft.Win32.Win32Native.Random(bStrong: true, buffer: buffer, length: byteCount);
        System.Runtime.InteropServices.Marshal.Copy(buffer, 0, (IntPtr)bytes, byteCount);
    }
}
