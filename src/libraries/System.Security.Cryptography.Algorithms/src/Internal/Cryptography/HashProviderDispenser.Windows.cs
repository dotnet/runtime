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
            public static unsafe int MacData(
                string hashAlgorithmId,
                ReadOnlySpan<byte> key,
                ReadOnlySpan<byte> source,
                Span<byte> destination)
            {
                int hashSize; // in bytes

                // Use a pseudo-handle if available.
                if (Interop.BCrypt.PseudoHandlesSupported)
                {
                    HashDataUsingPseudoHandle(hashAlgorithmId, source, key, isHmac: true, destination, out hashSize);
                    return hashSize;
                }
                else
                {
                    // Pseudo-handle not available. Fall back to a shared handle with no using or dispose.
                    SafeBCryptAlgorithmHandle cachedAlgorithmHandle = BCryptAlgorithmCache.GetCachedBCryptAlgorithmHandle(
                        hashAlgorithmId,
                        BCryptOpenAlgorithmProviderFlags.BCRYPT_ALG_HANDLE_HMAC_FLAG,
                        out hashSize);

                    if (destination.Length < hashSize)
                    {
                        Debug.Fail("Caller should have checked length.");
                        throw new CryptographicException();
                    }

                    HashUpdateAndFinish(cachedAlgorithmHandle, hashSize, key, source, destination);

                    return hashSize;
                }
            }

            public static unsafe int HashData(string hashAlgorithmId, ReadOnlySpan<byte> source, Span<byte> destination)
            {
                int hashSize; // in bytes

                // Use a pseudo-handle if available.
                if (Interop.BCrypt.PseudoHandlesSupported)
                {
                    HashDataUsingPseudoHandle(hashAlgorithmId, source, key: default, isHmac : false, destination, out hashSize);
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

                    HashUpdateAndFinish(cachedAlgorithmHandle, hashSize, key: default, source, destination);

                    return hashSize;
                }
            }

            private static unsafe void HashDataUsingPseudoHandle(
                string hashAlgorithmId,
                ReadOnlySpan<byte> source,
                ReadOnlySpan<byte> key,
                bool isHmac,
                Span<byte> destination,
                out int hashSize)
            {
                hashSize = default;

                Debug.Assert(isHmac ? true : key.IsEmpty);

                Interop.BCrypt.BCryptAlgPseudoHandle algHandle;
                int digestSizeInBytes;

                if (hashAlgorithmId == HashAlgorithmNames.MD5)
                {
                    algHandle = isHmac ?
                        Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_HMAC_MD5_ALG_HANDLE :
                        Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_MD5_ALG_HANDLE;
                    digestSizeInBytes = 128 / 8;
                }
                else if (hashAlgorithmId == HashAlgorithmNames.SHA1)
                {
                    algHandle = isHmac ?
                        Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_HMAC_SHA1_ALG_HANDLE :
                        Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_SHA1_ALG_HANDLE;
                    digestSizeInBytes = 160 / 8;
                }
                else if (hashAlgorithmId == HashAlgorithmNames.SHA256)
                {
                    algHandle = isHmac ?
                        Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_HMAC_SHA256_ALG_HANDLE :
                        Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_SHA256_ALG_HANDLE;
                    digestSizeInBytes = 256 / 8;
                }
                else if (hashAlgorithmId == HashAlgorithmNames.SHA384)
                {
                    algHandle = isHmac ?
                        Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_HMAC_SHA384_ALG_HANDLE :
                        Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_SHA384_ALG_HANDLE;
                    digestSizeInBytes = 384 / 8;
                }
                else if (hashAlgorithmId == HashAlgorithmNames.SHA512)
                {
                    algHandle = isHmac ?
                        Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_HMAC_SHA512_ALG_HANDLE :
                        Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_SHA512_ALG_HANDLE;
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

                fixed (byte* pKey = &MemoryMarshal.GetReference(key))
                fixed (byte* pSrc = &MemoryMarshal.GetReference(source))
                fixed (byte* pDest = &MemoryMarshal.GetReference(destination))
                {
                    NTSTATUS ntStatus = Interop.BCrypt.BCryptHash((uint)algHandle, pKey, key.Length, pSrc, source.Length, pDest, digestSizeInBytes);

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
                ReadOnlySpan<byte> key,
                ReadOnlySpan<byte> source,
                Span<byte> destination)
            {
                NTSTATUS ntStatus = Interop.BCrypt.BCryptCreateHash(
                    algHandle,
                    out SafeBCryptHashHandle hHash,
                    IntPtr.Zero,
                    0,
                    key,
                    key.Length,
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
