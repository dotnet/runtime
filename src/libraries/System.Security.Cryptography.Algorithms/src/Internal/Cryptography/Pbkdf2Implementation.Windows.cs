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
        // BCryptDeriveKeyPBKDF2 on Windows 7 will crash the process with an access violation
        // when given a pseudo handle, so we can't detect based on the NTSTATUS of the native call.
        private static readonly bool s_usePseudoHandles = OperatingSystem.IsWindowsVersionAtLeast(10, 0, 0);

        public static unsafe void Fill(
            ReadOnlySpan<byte> password,
            ReadOnlySpan<byte> salt,
            int iterations,
            HashAlgorithmName hashAlgorithmName,
            Span<byte> destination)
        {
            Debug.Assert(iterations >= 0);
            Debug.Assert(hashAlgorithmName.Name is not null);

            NTSTATUS status;

            fixed (byte* pPassword = password)
            fixed (byte* pSalt = salt)
            fixed (byte* pDestination = destination)
            {
                if (s_usePseudoHandles)
                {
                    BCryptAlgPseudoHandle pseudoHandle = PseudoHandleFromIdentifier(hashAlgorithmName.Name);
                    status = Interop.BCrypt.BCryptDeriveKeyPBKDF2(
                        (nuint)pseudoHandle,
                        pPassword,
                        password.Length,
                        pSalt,
                        salt.Length,
                        (ulong)iterations,
                        pDestination,
                        destination.Length,
                        dwFlags: 0);
                }
                else
                {
                    const BCryptOpenAlgorithmProviderFlags OpenAlgorithmFlags = BCryptOpenAlgorithmProviderFlags.BCRYPT_ALG_HANDLE_HMAC_FLAG;
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
                }

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
