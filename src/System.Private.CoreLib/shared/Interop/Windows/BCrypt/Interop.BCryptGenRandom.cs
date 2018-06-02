// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class BCrypt
    {
        internal const int BCRYPT_USE_SYSTEM_PREFERRED_RNG = 0x00000002;

        [DllImport(Libraries.BCrypt, CharSet = CharSet.Unicode)]
        internal static extern unsafe NTSTATUS BCryptGenRandom(IntPtr hAlgorithm, byte* pbBuffer, int cbBuffer, int dwFlags);        
    }
}
