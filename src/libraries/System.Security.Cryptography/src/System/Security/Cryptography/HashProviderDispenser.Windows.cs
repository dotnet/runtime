// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;
using BCryptAlgorithmCache = Interop.BCrypt.BCryptAlgorithmCache;
using BCryptCreateHashFlags = Interop.BCrypt.BCryptCreateHashFlags;
using BCryptOpenAlgorithmProviderFlags = Interop.BCrypt.BCryptOpenAlgorithmProviderFlags;
using NTSTATUS = Interop.BCrypt.NTSTATUS;

namespace System.Security.Cryptography
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

        internal static bool HashSupported(string hashAlgorithmId)
        {
            switch (hashAlgorithmId)
            {
                // We know that MD5, SHA1, and SHA2 are supported on all platforms. Don't bother asking.
                case HashAlgorithmNames.MD5:
                case HashAlgorithmNames.SHA1:
                case HashAlgorithmNames.SHA256:
                case HashAlgorithmNames.SHA384:
                case HashAlgorithmNames.SHA512:
                    return true;
                case HashAlgorithmNames.SHA3_256:
                case HashAlgorithmNames.SHA3_384:
                case HashAlgorithmNames.SHA3_512:
                case HashAlgorithmNames.CSHAKE128:
                case HashAlgorithmNames.CSHAKE256:
                    return BCryptAlgorithmCache.IsBCryptAlgorithmSupported(
                        hashAlgorithmId,
                        BCryptOpenAlgorithmProviderFlags.None);
                default:
                    return false;
            }
        }

        internal static bool MacSupported(string hashAlgorithmId)
        {
            switch (hashAlgorithmId)
            {
                // We know that MD5, SHA1, and SHA2 are supported on all platforms. Don't bother asking.
                case HashAlgorithmNames.MD5:
                case HashAlgorithmNames.SHA1:
                case HashAlgorithmNames.SHA256:
                case HashAlgorithmNames.SHA384:
                case HashAlgorithmNames.SHA512:
                    return true;
                case HashAlgorithmNames.SHA3_256:
                case HashAlgorithmNames.SHA3_384:
                case HashAlgorithmNames.SHA3_512:
                    return BCryptAlgorithmCache.IsBCryptAlgorithmSupported(
                        hashAlgorithmId,
                        BCryptOpenAlgorithmProviderFlags.BCRYPT_ALG_HANDLE_HMAC_FLAG);
                default:
                    return false;
            }
        }

        internal static bool KmacSupported(string algorithmId)
        {
            switch (algorithmId)
            {
                case HashAlgorithmNames.KMAC128:
                case HashAlgorithmNames.KMAC256:
                    break;
                default:
                    return false;
            }

            // KMAC was originally introduced in Windows build 25324. However, it contains a bug that results in incorrect
            // behavior when the handle is duplicated. Therefore, we require Windows build 26016 or later for KMAC
            // so that a broken KMAC is not used. This Windows build is known to have the fix for KMAC.
            // As an additional sanity check we also ensure the algorithm is available by asking CNG.
            return OperatingSystem.IsWindowsVersionAtLeast(10, 0, 26016) &&
                BCryptAlgorithmCache.IsBCryptAlgorithmSupported(algorithmId, BCryptOpenAlgorithmProviderFlags.None);
        }

        public static class OneShotHashProvider
        {
            public static void KmacData(
                string algorithmId,
                ReadOnlySpan<byte> key,
                ReadOnlySpan<byte> source,
                Span<byte> destination,
                ReadOnlySpan<byte> customizationString,
                bool xof)
            {
                using (LiteKmac kmac = LiteHashProvider.CreateKmac(algorithmId, key, customizationString, xof))
                {
                    kmac.Append(source);
                    kmac.Finalize(destination);
                }
            }

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

            public static void HashDataXof(string hashAlgorithmId, ReadOnlySpan<byte> source, Span<byte> destination)
            {
                Debug.Assert(Interop.BCrypt.PseudoHandlesSupported);
                HashDataUsingPseudoHandle(hashAlgorithmId, source, key: default, isHmac: false, destination, out _);
            }

            public static unsafe int HashData(string hashAlgorithmId, ReadOnlySpan<byte> source, Span<byte> destination)
            {
                int hashSize; // in bytes

                // Use a pseudo-handle if available.
                if (Interop.BCrypt.PseudoHandlesSupported)
                {
                    HashDataUsingPseudoHandle(hashAlgorithmId, source, key: default, isHmac: false, destination, out hashSize);
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
                    digestSizeInBytes = MD5.HashSizeInBytes;
                }
                else if (hashAlgorithmId == HashAlgorithmNames.SHA1)
                {
                    algHandle = isHmac ?
                        Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_HMAC_SHA1_ALG_HANDLE :
                        Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_SHA1_ALG_HANDLE;
                    digestSizeInBytes = SHA1.HashSizeInBytes;
                }
                else if (hashAlgorithmId == HashAlgorithmNames.SHA256)
                {
                    algHandle = isHmac ?
                        Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_HMAC_SHA256_ALG_HANDLE :
                        Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_SHA256_ALG_HANDLE;
                    digestSizeInBytes = SHA256.HashSizeInBytes;
                }
                else if (hashAlgorithmId == HashAlgorithmNames.SHA384)
                {
                    algHandle = isHmac ?
                        Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_HMAC_SHA384_ALG_HANDLE :
                        Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_SHA384_ALG_HANDLE;
                    digestSizeInBytes = SHA384.HashSizeInBytes;
                }
                else if (hashAlgorithmId == HashAlgorithmNames.SHA512)
                {
                    algHandle = isHmac ?
                        Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_HMAC_SHA512_ALG_HANDLE :
                        Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_SHA512_ALG_HANDLE;
                    digestSizeInBytes = SHA512.HashSizeInBytes;
                }
                else if (hashAlgorithmId == HashAlgorithmNames.SHA3_256)
                {
                    algHandle = isHmac ?
                        Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_HMAC_SHA3_256_ALG_HANDLE :
                        Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_SHA3_256_ALG_HANDLE;
                    digestSizeInBytes = SHA3_256.HashSizeInBytes;
                }
                else if (hashAlgorithmId == HashAlgorithmNames.SHA3_384)
                {
                    algHandle = isHmac ?
                        Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_HMAC_SHA3_384_ALG_HANDLE :
                        Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_SHA3_384_ALG_HANDLE;
                    digestSizeInBytes = SHA3_384.HashSizeInBytes;
                }
                else if (hashAlgorithmId == HashAlgorithmNames.SHA3_512)
                {
                    algHandle = isHmac ?
                        Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_HMAC_SHA3_512_ALG_HANDLE :
                        Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_SHA3_512_ALG_HANDLE;
                    digestSizeInBytes = SHA3_512.HashSizeInBytes;
                }
                else if (hashAlgorithmId == HashAlgorithmNames.CSHAKE128)
                {
                    algHandle = Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_CSHAKE128_ALG_HANDLE;
                    digestSizeInBytes = destination.Length;
                }
                else if (hashAlgorithmId == HashAlgorithmNames.CSHAKE256)
                {
                    algHandle = Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_CSHAKE256_ALG_HANDLE;
                    digestSizeInBytes = destination.Length;
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
                fixed (byte* pDest = &Helpers.GetNonNullPinnableReference(destination))
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
