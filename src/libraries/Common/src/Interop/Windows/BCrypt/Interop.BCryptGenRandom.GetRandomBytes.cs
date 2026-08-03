// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Security.Cryptography;

internal static partial class Interop
{
    internal static unsafe void GetRandomBytes(byte* buffer, int length)
    {
        Debug.Assert(buffer != null);
        Debug.Assert(length >= 0);

        BCrypt.NTSTATUS status = BCrypt.BCryptGenRandom(IntPtr.Zero, buffer, length, BCrypt.BCRYPT_USE_SYSTEM_PREFERRED_RNG);
        if (status != BCrypt.NTSTATUS.STATUS_SUCCESS)
        {
            if (status == BCrypt.NTSTATUS.STATUS_NO_MEMORY)
            {
                throw new OutOfMemoryException();
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }

    // BCryptGenRandom with BCRYPT_USE_SYSTEM_PREFERRED_RNG is always cryptographically secure.
    internal static unsafe void GetCryptographicallySecureRandomBytes(byte* buffer, int length)
    {
        Debug.Assert(buffer != null);
        Debug.Assert(length >= 0);

        BCrypt.NTSTATUS status = BCrypt.BCryptGenRandom(IntPtr.Zero, buffer, length, BCrypt.BCRYPT_USE_SYSTEM_PREFERRED_RNG);
        if (status != BCrypt.NTSTATUS.STATUS_SUCCESS)
        {
            if (status == BCrypt.NTSTATUS.STATUS_NO_MEMORY)
            {
                throw new OutOfMemoryException();
            }
            else
            {
                throw new CryptographicException();
            }
        }
    }
}
