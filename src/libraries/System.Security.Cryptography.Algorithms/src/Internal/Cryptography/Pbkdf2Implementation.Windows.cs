// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;
using BCryptAlgPseudoHandle = Interop.BCrypt.BCryptAlgPseudoHandle;
using BCryptOpenAlgorithmProviderFlags = Interop.BCrypt.BCryptOpenAlgorithmProviderFlags;
using NTSTATUS = Interop.BCrypt.NTSTATUS;

namespace Internal.Cryptography
{
    internal partial class Pbkdf2Implementation
    {
        private static volatile bool s_useCompatOneShot;

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
            NTSTATUS status;

            fixed (byte* pPassword = password)
            fixed (byte* pSalt = salt)
            fixed (byte* pDestination = destination)
            {
                if (!s_useCompatOneShot)
                {
                    BCryptAlgPseudoHandle psuedoHandle = PseudoHandleFromIdentifier(hashAlgorithmName.Name);
                    status = Interop.BCrypt.BCryptDeriveKeyPBKDF2(
                        (nuint)psuedoHandle,
                        pPassword,
                        password.Length,
                        pSalt,
                        salt.Length,
                        (ulong)iterations,
                        pDestination,
                        destination.Length,
                        dwFlags: 0);

                    switch (status)
                    {
                        case NTSTATUS.STATUS_SUCCESS:
                            return;
                        case NTSTATUS.STATUS_INVALID_HANDLE:
                            s_useCompatOneShot = true;
                            break;
                        default:
                            throw Interop.BCrypt.CreateCryptographicException(status);
                    }
                }

                Debug.Assert(s_useCompatOneShot);

                // Do not dispose handle since it is shared and cached.
                SafeBCryptAlgorithmHandle handle =
                    Interop.BCrypt.BCryptAlgorithmCache.GetCachedBCryptAlgorithmHandle(hashAlgorithmName.Name, OpenAlgorithmFlags, out _);

                status = Interop.BCrypt.BCryptDeriveKeyPBKDF2(
                    handle,
                    pPassword,
                    password.Length,
                    pSalt,
                    salt.Length,
                    (ulong)iterations,
                    pDestination,
                    destination.Length,
                    dwFlags: 0);

                if (status != NTSTATUS.STATUS_SUCCESS)
                {
                    throw Interop.BCrypt.CreateCryptographicException(status);
                }
            }
        }

        private static BCryptAlgPseudoHandle PseudoHandleFromIdentifier(string hashAlgorithmId)
        {
            return hashAlgorithmId switch {
                HashAlgorithmNames.SHA1 => BCryptAlgPseudoHandle.BCRYPT_HMAC_SHA1_ALG_HANDLE,
                HashAlgorithmNames.SHA256 => BCryptAlgPseudoHandle.BCRYPT_HMAC_SHA256_ALG_HANDLE,
                HashAlgorithmNames.SHA384 => BCryptAlgPseudoHandle.BCRYPT_HMAC_SHA384_ALG_HANDLE,
                HashAlgorithmNames.SHA512 => BCryptAlgPseudoHandle.BCRYPT_HMAC_SHA512_ALG_HANDLE,
                _ => throw new CryptographicException()
            };
        }
    }
}
