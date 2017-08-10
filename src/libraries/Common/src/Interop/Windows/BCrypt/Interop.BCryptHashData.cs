// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

internal partial class Interop
{
    internal partial class BCrypt
    {
        internal static unsafe NTSTATUS BCryptHashData(SafeBCryptHashHandle hHash, ReadOnlySpan<byte> pbInput, int cbInput, int dwFlags)
        {
            fixed (byte* pbInputPtr = &pbInput.DangerousGetPinnableReference())
            {
                return BCryptHashData(hHash, pbInputPtr, cbInput, dwFlags);
            }
        }

        [DllImport(Libraries.BCrypt, CharSet = CharSet.Unicode)]
        private static extern unsafe NTSTATUS BCryptHashData(SafeBCryptHashHandle hHash, byte* pbInput, int cbInput, int dwFlags);
    }
}

