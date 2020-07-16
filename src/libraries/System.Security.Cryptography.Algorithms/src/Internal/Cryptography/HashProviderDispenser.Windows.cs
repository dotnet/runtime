// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;
using NTSTATUS = Interop.BCrypt.NTSTATUS;
using BCryptOpenAlgorithmProviderFlags = Interop.BCrypt.BCryptOpenAlgorithmProviderFlags;
using BCryptCreateHashFlags = Interop.BCrypt.BCryptCreateHashFlags;
using BCryptAlgorithmCache = Interop.BCrypt.BCryptAlgorithmCache;

namespace Internal.Cryptography
{
    //
    // Provides hash services via the native provider (CNG).
    //
    internal static partial class HashProviderDispenser
    {
        public static HashProvider CreateHashProvider(string hashAlgorithmId)
        {
            return new HashProviderCng(hashAlgorithmId, null);
        }

        public static HashProvider CreateMacProvider(string hashAlgorithmId, ReadOnlySpan<byte> key)
        {
            return new HashProviderCng(hashAlgorithmId, key, isHmac: true);
        }

        public static class OneShotHashProvider
        {
            private static volatile bool s_useCompatOneShot;

            public static unsafe int HashData(string hashAlgorithmId, ReadOnlySpan<byte> source, Span<byte> destination)
            {
                // Shared handle, no using or dispose.
                SafeBCryptAlgorithmHandle cachedAlgorithmHandle = BCryptAlgorithmCache.GetCachedBCryptAlgorithmHandle(
                    hashAlgorithmId,
                    BCryptOpenAlgorithmProviderFlags.None);

                int hashSize;

                NTSTATUS ntStatus = Interop.BCrypt.BCryptGetProperty(
                    cachedAlgorithmHandle,
                    Interop.BCrypt.BCryptPropertyStrings.BCRYPT_HASH_LENGTH,
                    &hashSize,
                    sizeof(int),
                    out _,
                    0);

                if (ntStatus != NTSTATUS.STATUS_SUCCESS)
                {
                    throw Interop.BCrypt.CreateCryptographicException(ntStatus);
                }

                if (destination.Length < hashSize)
                {
                    throw new CryptographicException();
                }

                if (!s_useCompatOneShot)
                {
                    try
                    {
                        fixed (byte* pSource = source)
                        fixed (byte* pDestination = destination)
                        {
                            ntStatus = Interop.BCrypt.BCryptHash(cachedAlgorithmHandle, null, 0, pSource, source.Length, pDestination, hashSize);

                            if (ntStatus != NTSTATUS.STATUS_SUCCESS)
                            {
                                throw Interop.BCrypt.CreateCryptographicException(ntStatus);
                            }
                        }

                        return hashSize;
                    }
                    catch (EntryPointNotFoundException)
                    {
                        s_useCompatOneShot = true;
                    }
                }

                Debug.Assert(s_useCompatOneShot);
                HashUpdateAndFinish(cachedAlgorithmHandle, hashSize, source, destination);

                return hashSize;
            }

            private static void HashUpdateAndFinish(
                SafeBCryptAlgorithmHandle algHandle,
                int hashSize,
                ReadOnlySpan<byte> source,
                Span<byte> destination)
            {
                NTSTATUS ntStatus = Interop.BCrypt.BCryptCreateHash(
                    algHandle,
                    out SafeBCryptHashHandle hHash,
                    IntPtr.Zero,
                    0,
                    default,
                    0,
                    BCryptCreateHashFlags.None);

                if (ntStatus != NTSTATUS.STATUS_SUCCESS)
                {
                    throw Interop.BCrypt.CreateCryptographicException(ntStatus);
                }

                using (hHash)
                {
                    ntStatus = Interop.BCrypt.BCryptHashData(hHash, source, source.Length, 0);

                    if (ntStatus != NTSTATUS.STATUS_SUCCESS)
                    {
                        throw Interop.BCrypt.CreateCryptographicException(ntStatus);
                    }

                    Interop.BCrypt.BCryptFinishHash(hHash, destination, hashSize, 0);
                }
            }
        }
    }
}
