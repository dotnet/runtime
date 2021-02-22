// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
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
            public static unsafe int HashData(string hashAlgorithmId, ReadOnlySpan<byte> source, Span<byte> destination)
            {
                int hashSize; // in bytes

                // Try using a pseudo-handle if available.
                if (Interop.BCrypt.PseudoHandlesSupported)
                {
                    HashDataUsingPseudoHandle(hashAlgorithmId, source, destination, out hashSize);
                    return hashSize;
                }
                else
                {
                    // Pseudo-handle not available. Fall back to a shared handle with no using or dispose.
                    SafeBCryptAlgorithmHandle cachedAlgorithmHandle = BCryptAlgorithmCache.GetCachedBCryptAlgorithmHandle(
                        hashAlgorithmId,
                        BCryptOpenAlgorithmProviderFlags.None,
                        out hashSize);

                    if (destination.Length < hashSize)
                    {
                        Debug.Fail("Caller should have checked length.");
                        throw new CryptographicException();
                    }

                    HashUpdateAndFinish(cachedAlgorithmHandle, hashSize, source, destination);

                    return hashSize;
                }
            }

            private static unsafe void HashDataUsingPseudoHandle(string hashAlgorithmId, ReadOnlySpan<byte> source, Span<byte> destination, out int hashSize)
            {
                hashSize = default;

                Interop.BCrypt.BCryptAlgPseudoHandle algHandle;
                int digestSizeInBytes;

                if (hashAlgorithmId == HashAlgorithmNames.MD5)
                {
                    algHandle = Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_MD5_ALG_HANDLE;
                    digestSizeInBytes = 128 / 8;
                }
                else if (hashAlgorithmId == HashAlgorithmNames.SHA1)
                {
                    algHandle = Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_SHA1_ALG_HANDLE;
                    digestSizeInBytes = 160 / 8;
                }
                else if (hashAlgorithmId == HashAlgorithmNames.SHA256)
                {
                    algHandle = Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_SHA256_ALG_HANDLE;
                    digestSizeInBytes = 256 / 8;
                }
                else if (hashAlgorithmId == HashAlgorithmNames.SHA384)
                {
                    algHandle = Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_SHA384_ALG_HANDLE;
                    digestSizeInBytes = 384 / 8;
                }
                else if (hashAlgorithmId == HashAlgorithmNames.SHA512)
                {
                    algHandle = Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_SHA512_ALG_HANDLE;
                    digestSizeInBytes = 512 / 8;
                }
                else
                {
                    Debug.Fail("Unknown hash algorithm.");
                    throw new CryptographicException();
                }

                if (destination.Length < digestSizeInBytes)
                {
                    Debug.Fail("Caller should have checked length.");
                    throw new CryptographicException();
                }

                fixed (byte* pSrc = &MemoryMarshal.GetReference(source))
                fixed (byte* pDest = &MemoryMarshal.GetReference(destination))
                {
                    NTSTATUS ntStatus = Interop.BCrypt.BCryptHash((uint)algHandle, pbSecret: null, cbSecret: 0, pSrc, source.Length, pDest, digestSizeInBytes);

                    if (ntStatus != NTSTATUS.STATUS_SUCCESS)
                    {
                        throw Interop.BCrypt.CreateCryptographicException(ntStatus);
                    }
                }

                hashSize = digestSizeInBytes;
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
                    hHash.Dispose();
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
