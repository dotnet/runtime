// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;
using BCryptOpenAlgorithmProviderFlags = Interop.BCrypt.BCryptOpenAlgorithmProviderFlags;
using NTSTATUS = Interop.BCrypt.NTSTATUS;

namespace Internal.Cryptography
{
    internal partial class Pbkdf2Implementation
    {
        public static unsafe void Fill(
            ReadOnlySpan<byte> password,
            ReadOnlySpan<byte> salt,
            int iterations,
            HashAlgorithmName hashAlgorithmName,
            Span<byte> destination)
        {
            Debug.Assert(iterations >= 0);
            Debug.Assert(hashAlgorithmName.Name is not null);
            const BCryptOpenAlgorithmProviderFlags OpenAlgorithmFlags = BCryptOpenAlgorithmProviderFlags.BCRYPT_ALG_HANDLE_HMAC_FLAG;

            // Do not dispose handle since it is shared and cached.
            SafeBCryptAlgorithmHandle handle =
                Interop.BCrypt.BCryptAlgorithmCache.GetCachedBCryptAlgorithmHandle(hashAlgorithmName.Name, OpenAlgorithmFlags, out _);

            fixed (byte* pPassword = password)
            fixed (byte* pSalt = salt)
            fixed (byte* pDestination = destination)
            {
                NTSTATUS ntStatus = Interop.BCrypt.BCryptDeriveKeyPBKDF2(
                    handle,
                    pPassword,
                    password.Length,
                    pSalt,
                    salt.Length,
                    (ulong)iterations,
                    pDestination,
                    destination.Length,
                    dwFlags: 0);

                if (ntStatus != NTSTATUS.STATUS_SUCCESS)
                {
                    throw Interop.BCrypt.CreateCryptographicException(ntStatus);
                }
            }
        }
    }
}
