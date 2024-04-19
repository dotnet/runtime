// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;
using BCryptCreateHashFlags = Interop.BCrypt.BCryptCreateHashFlags;
using BCryptOpenAlgorithmProviderFlags = Interop.BCrypt.BCryptOpenAlgorithmProviderFlags;
using NTSTATUS = Interop.BCrypt.NTSTATUS;

namespace System.Security.Cryptography
{
    internal static partial class LiteHashProvider
    {
        private static LiteHash CreateHash(string hashAlgorithmId)
        {
            return new LiteHash(hashAlgorithmId);
        }

        private static LiteHmac CreateHmac(string hashAlgorithmId, ReadOnlySpan<byte> key)
        {
            return new LiteHmac(hashAlgorithmId, key);
        }

        internal static LiteXof CreateXof(string hashAlgorithmId)
        {
            return new LiteXof(hashAlgorithmId);
        }

        internal static LiteKmac CreateKmac(string algorithmId, ReadOnlySpan<byte> key, ReadOnlySpan<byte> customizationString, bool xof)
        {
            return new LiteKmac(algorithmId, key, customizationString, xof);
        }
    }

    internal readonly struct LiteKmac : ILiteHash
    {
        private readonly SafeBCryptHashHandle _hashHandle;
        private readonly int _finishFlags;

        private const string BCRYPT_CUSTOMIZATION_STRING = "CustomizationString";
        private const int BCRYPT_HASH_DONT_RESET_FLAG = 0x00000001;

        internal LiteKmac(string algorithm, ReadOnlySpan<byte> key, ReadOnlySpan<byte> customizationString, bool xof)
        {
            _finishFlags = xof ? BCRYPT_HASH_DONT_RESET_FLAG : 0;
            nuint algorithmHandle = algorithm switch
            {
                HashAlgorithmNames.KMAC128 => (nuint)Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_KMAC128_ALG_HANDLE,
                HashAlgorithmNames.KMAC256 => (nuint)Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_KMAC256_ALG_HANDLE,
                _ => throw FailThrow(algorithm),
            };

            NTSTATUS ntStatus = Interop.BCrypt.BCryptCreateHash(
                algorithmHandle,
                out SafeBCryptHashHandle hashHandle,
                pbHashObject: IntPtr.Zero,
                cbHashObject: 0,
                key,
                key.Length,
                BCryptCreateHashFlags.None);

            if (ntStatus != NTSTATUS.STATUS_SUCCESS)
            {
                hashHandle.Dispose();
                throw Interop.BCrypt.CreateCryptographicException(ntStatus);
            }

            if (!customizationString.IsEmpty)
            {
                ntStatus = Internal.NativeCrypto.Cng.Interop.BCryptSetProperty(
                    hashHandle,
                    BCRYPT_CUSTOMIZATION_STRING,
                    customizationString,
                    customizationString.Length,
                    0);

                if (ntStatus != NTSTATUS.STATUS_SUCCESS)
                {
                    hashHandle.Dispose();
                    throw Interop.BCrypt.CreateCryptographicException(ntStatus);
                }
            }

            _hashHandle = hashHandle;

            static Exception FailThrow(string algorithm)
            {
                Debug.Fail($"Unexpected hash algorithm name '{algorithm}'.");
                return new CryptographicException();
            }
        }

        public int HashSizeInBytes
        {
            get
            {
                Debug.Fail("Unexpectedly asked for the hash size of KMAC.");
                throw new CryptographicException();
            }
        }

        public void Reset()
        {
            // Reset only does something meaningful in XOF mode. In non-XOF mode, Finalize always
            // does a reset.
            if ((_finishFlags & BCRYPT_HASH_DONT_RESET_FLAG) == BCRYPT_HASH_DONT_RESET_FLAG)
            {
                Span<byte> buffer = stackalloc byte[1];
                CheckStatus(Interop.BCrypt.BCryptFinishHash(_hashHandle, buffer, 0, dwFlags: 0));
            }
        }

        public void Append(ReadOnlySpan<byte> data)
        {
            if (data.IsEmpty)
            {
                return;
            }

            CheckStatus(Interop.BCrypt.BCryptHashData(_hashHandle, data, data.Length, dwFlags: 0));
        }

        public unsafe int Current(Span<byte> destination)
        {
            fixed (byte* pDestination = &Helpers.GetNonNullPinnableReference(destination))
            {
                using (SafeBCryptHashHandle dup = Interop.BCrypt.BCryptDuplicateHash(_hashHandle))
                {
                    CheckStatus(Interop.BCrypt.BCryptFinishHash(dup, pDestination, destination.Length, _finishFlags));
                }
            }

            return destination.Length;
        }

        public unsafe int Finalize(Span<byte> destination)
        {
            fixed (byte* pDestination = &Helpers.GetNonNullPinnableReference(destination))
            {
                CheckStatus(Interop.BCrypt.BCryptFinishHash(_hashHandle, pDestination, destination.Length, _finishFlags));
            }

            return destination.Length;
        }

        public void Dispose()
        {
            _hashHandle.Dispose();
        }

        private static void CheckStatus(NTSTATUS status)
        {
            if (status != NTSTATUS.STATUS_SUCCESS)
            {
                throw Interop.BCrypt.CreateCryptographicException(status);
            }
        }
    }

    internal struct LiteXof : ILiteHash
    {
        private readonly nuint _algorithm;
        private SafeBCryptHashHandle _hashHandle;

        internal LiteXof(string algorithm)
        {
            _algorithm = algorithm switch
            {
                HashAlgorithmNames.CSHAKE128 => (nuint)Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_CSHAKE128_ALG_HANDLE,
                HashAlgorithmNames.CSHAKE256 => (nuint)Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_CSHAKE256_ALG_HANDLE,
                _ => throw FailThrow(algorithm),
            };

            Reset();

            static Exception FailThrow(string algorithm)
            {
                Debug.Fail($"Unexpected hash algorithm name '{algorithm}'.");
                return new CryptographicException();
            }
        }

        public readonly int HashSizeInBytes
        {
            get
            {
                Debug.Fail("Unexpectedly asked for the hash size of a XOF.");
                throw new CryptographicException();
            }
        }

        public readonly void Append(ReadOnlySpan<byte> data)
        {
            if (data.IsEmpty)
            {
                return;
            }

            NTSTATUS ntStatus = Interop.BCrypt.BCryptHashData(_hashHandle, data, data.Length, dwFlags: 0);

            if (ntStatus != NTSTATUS.STATUS_SUCCESS)
            {
                throw Interop.BCrypt.CreateCryptographicException(ntStatus);
            }
        }

        public readonly unsafe int Finalize(Span<byte> destination)
        {
            fixed (byte* pDestination = &Helpers.GetNonNullPinnableReference(destination))
            {
                NTSTATUS ntStatus = Interop.BCrypt.BCryptFinishHash(_hashHandle, pDestination, destination.Length, dwFlags: 0);

                if (ntStatus != NTSTATUS.STATUS_SUCCESS)
                {
                    throw Interop.BCrypt.CreateCryptographicException(ntStatus);
                }

                return destination.Length;
            }
        }

        [MemberNotNull(nameof(_hashHandle))]
        public void Reset()
        {
            _hashHandle?.Dispose();
            SafeBCryptHashHandle hashHandle;

            NTSTATUS ntStatus = Interop.BCrypt.BCryptCreateHash(
                _algorithm,
                out hashHandle,
                pbHashObject: IntPtr.Zero,
                cbHashObject: 0,
                secret: ReadOnlySpan<byte>.Empty,
                cbSecret: 0,
                BCryptCreateHashFlags.None);

            if (ntStatus != NTSTATUS.STATUS_SUCCESS)
            {
                hashHandle.Dispose();
                throw Interop.BCrypt.CreateCryptographicException(ntStatus);
            }

            _hashHandle = hashHandle;
        }

        public readonly unsafe void Current(Span<byte> destination)
        {
            using (SafeBCryptHashHandle tmpHash = Interop.BCrypt.BCryptDuplicateHash(_hashHandle))
            fixed (byte* pDestination = &Helpers.GetNonNullPinnableReference(destination))
            {
                NTSTATUS ntStatus = Interop.BCrypt.BCryptFinishHash(tmpHash, pDestination, destination.Length, dwFlags: 0);

                if (ntStatus != NTSTATUS.STATUS_SUCCESS)
                {
                    throw Interop.BCrypt.CreateCryptographicException(ntStatus);
                }
            }
        }

        public readonly void Dispose()
        {
            _hashHandle.Dispose();
        }
    }

    internal readonly struct LiteHash : ILiteHash
    {
        private readonly SafeBCryptHashHandle _hashHandle;
        private readonly int _hashSizeInBytes;

        public int HashSizeInBytes => _hashSizeInBytes;

        internal LiteHash(string algorithm)
        {

            BCryptOpenAlgorithmProviderFlags algorithmFlags =
                BCryptOpenAlgorithmProviderFlags.None;

            // This is a shared handle, do not put this in a using.
            SafeBCryptAlgorithmHandle algorithmHandle = Interop.BCrypt.BCryptAlgorithmCache.GetCachedBCryptAlgorithmHandle(
                algorithm,
                algorithmFlags,
                out _hashSizeInBytes);

            SafeBCryptHashHandle hashHandle;

            NTSTATUS ntStatus = Interop.BCrypt.BCryptCreateHash(
                algorithmHandle,
                out hashHandle,
                pbHashObject: IntPtr.Zero,
                cbHashObject: 0,
                secret: ReadOnlySpan<byte>.Empty,
                cbSecret: 0,
                BCryptCreateHashFlags.None);

            if (ntStatus != NTSTATUS.STATUS_SUCCESS)
            {
                hashHandle.Dispose();
                throw Interop.BCrypt.CreateCryptographicException(ntStatus);
            }

            _hashHandle = hashHandle;
        }

        public void Append(ReadOnlySpan<byte> data)
        {
            if (data.IsEmpty)
            {
                return;
            }

            NTSTATUS ntStatus = Interop.BCrypt.BCryptHashData(_hashHandle, data, data.Length, dwFlags: 0);

            if (ntStatus != NTSTATUS.STATUS_SUCCESS)
            {
                throw Interop.BCrypt.CreateCryptographicException(ntStatus);
            }
        }

        public int Finalize(Span<byte> destination)
        {
            Debug.Assert(destination.Length >= _hashSizeInBytes, $"{destination.Length} >= {_hashSizeInBytes}");

            NTSTATUS ntStatus = Interop.BCrypt.BCryptFinishHash(_hashHandle, destination, _hashSizeInBytes, dwFlags: 0);

            if (ntStatus != NTSTATUS.STATUS_SUCCESS)
            {
                throw Interop.BCrypt.CreateCryptographicException(ntStatus);
            }

            return _hashSizeInBytes;
        }

        public void Dispose()
        {
            _hashHandle.Dispose();
        }
    }

    internal readonly struct LiteHmac : ILiteHash
    {
        private readonly SafeBCryptHashHandle _hashHandle;
        private readonly int _hashSizeInBytes;

        public int HashSizeInBytes => _hashSizeInBytes;

        internal LiteHmac(string algorithm, ReadOnlySpan<byte> key)
        {
            BCryptOpenAlgorithmProviderFlags algorithmFlags =
                BCryptOpenAlgorithmProviderFlags.BCRYPT_ALG_HANDLE_HMAC_FLAG;

            // This is a shared handle, do not put this in a using.
            SafeBCryptAlgorithmHandle algorithmHandle = Interop.BCrypt.BCryptAlgorithmCache.GetCachedBCryptAlgorithmHandle(
                algorithm,
                algorithmFlags,
                out _hashSizeInBytes);

            SafeBCryptHashHandle hashHandle;

            NTSTATUS ntStatus = Interop.BCrypt.BCryptCreateHash(
                algorithmHandle,
                out hashHandle,
                pbHashObject: IntPtr.Zero,
                cbHashObject: 0,
                key,
                key.Length,
                BCryptCreateHashFlags.None);

            if (ntStatus != NTSTATUS.STATUS_SUCCESS)
            {
                hashHandle.Dispose();
                throw Interop.BCrypt.CreateCryptographicException(ntStatus);
            }

            _hashHandle = hashHandle;
        }

        public void Append(ReadOnlySpan<byte> data)
        {
            if (data.IsEmpty)
            {
                return;
            }

            NTSTATUS ntStatus = Interop.BCrypt.BCryptHashData(_hashHandle, data, data.Length, dwFlags: 0);

            if (ntStatus != NTSTATUS.STATUS_SUCCESS)
            {
                throw Interop.BCrypt.CreateCryptographicException(ntStatus);
            }
        }

        public int Finalize(Span<byte> destination)
        {
            Debug.Assert(destination.Length >= _hashSizeInBytes);

            NTSTATUS ntStatus = Interop.BCrypt.BCryptFinishHash(_hashHandle, destination, _hashSizeInBytes, dwFlags: 0);

            if (ntStatus != NTSTATUS.STATUS_SUCCESS)
            {
                throw Interop.BCrypt.CreateCryptographicException(ntStatus);
            }

            return _hashSizeInBytes;
        }

        public void Dispose()
        {
            _hashHandle.Dispose();
        }
    }
}
