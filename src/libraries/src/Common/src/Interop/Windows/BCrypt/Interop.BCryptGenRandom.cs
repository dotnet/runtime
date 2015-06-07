// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class BCrypt
    {
        internal static void BCryptGenRandom(byte[] buffer)
        {
            NTSTATUS ntStatus = BCryptGenRandom(IntPtr.Zero, buffer, buffer.Length, BCRYPT_USE_SYSTEM_PREFERRED_RNG);
            if (ntStatus != NTSTATUS.STATUS_SUCCESS)
                throw CreateCryptographicException(ntStatus);
        }

        private const int BCRYPT_USE_SYSTEM_PREFERRED_RNG = 0x00000002;

        [DllImport(Libraries.BCrypt, CharSet = CharSet.Unicode)]
        private static extern NTSTATUS BCryptGenRandom(IntPtr hAlgorithm, [In, Out] byte[] pbBuffer, int cbBuffer, int dwFlags);
    }
}
