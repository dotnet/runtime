// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Internal.Cryptography;

using AsymmetricPaddingMode = Interop.NCrypt.AsymmetricPaddingMode;
using BCRYPT_PKCS1_PADDING_INFO = Interop.BCrypt.BCRYPT_PKCS1_PADDING_INFO;
using BCRYPT_PSS_PADDING_INFO = Interop.BCrypt.BCRYPT_PSS_PADDING_INFO;

namespace System.Security.Cryptography
{
    public sealed partial class RSACng : RSA
    {
        private static readonly ConcurrentDictionary<HashAlgorithmName, int> s_hashSizes =
            new ConcurrentDictionary<HashAlgorithmName, int>(
                new[]
                {
                    KeyValuePair.Create(HashAlgorithmName.SHA256, 256 / 8),
                    KeyValuePair.Create(HashAlgorithmName.SHA384, 384 / 8),
                    KeyValuePair.Create(HashAlgorithmName.SHA512, 512 / 8),
                });

        internal static int GetHashSizeInBytes(HashAlgorithmName hashAlgorithm)
        {
            return s_hashSizes.GetOrAdd(
                hashAlgorithm,
                static hashAlgorithm =>
                {
                    using (HashProviderCng hashProvider = new HashProviderCng(hashAlgorithm.Name!, null))
                    {
                        return hashProvider.HashSizeInBytes;
                    }
                });
        }

        /// <summary>
        ///     Computes the signature of a hash that was produced by the hash algorithm specified by "hashAlgorithm."
        /// </summary>
        public override byte[] SignHash(byte[] hash, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
        {
            ArgumentNullException.ThrowIfNull(hash);
            string? hashAlgorithmName = hashAlgorithm.Name;
            ArgumentException.ThrowIfNullOrEmpty(hashAlgorithmName, nameof(hashAlgorithm));
            ArgumentNullException.ThrowIfNull(padding);

            if (hash.Length != GetHashSizeInBytes(hashAlgorithm))
            {
                throw new CryptographicException(SR.Cryptography_SignHash_WrongSize);
            }

            using (SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle())
            {
                IntPtr namePtr = Marshal.StringToHGlobalUni(hashAlgorithmName);
                try
                {
                    unsafe
                    {
                        int estimatedSize = GetMaxOutputSize();
                        switch (padding.Mode)
                        {
                            case RSASignaturePaddingMode.Pkcs1:
                                var pkcsPaddingInfo = new BCRYPT_PKCS1_PADDING_INFO() { pszAlgId = namePtr };
                                return keyHandle.SignHash(hash, AsymmetricPaddingMode.NCRYPT_PAD_PKCS1_FLAG, &pkcsPaddingInfo, estimatedSize);

                            case RSASignaturePaddingMode.Pss:
                                var pssPaddingInfo = new BCRYPT_PSS_PADDING_INFO() { pszAlgId = namePtr, cbSalt = hash.Length };
                                return keyHandle.SignHash(hash, AsymmetricPaddingMode.NCRYPT_PAD_PSS_FLAG, &pssPaddingInfo, estimatedSize);

                            default:
                                throw new CryptographicException(SR.Cryptography_UnsupportedPaddingMode);
                        }
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(namePtr);
                }
            }
        }

        public override unsafe bool TrySignHash(ReadOnlySpan<byte> hash, Span<byte> destination, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding, out int bytesWritten)
        {
            string? hashAlgorithmName = hashAlgorithm.Name;
            ArgumentException.ThrowIfNullOrEmpty(hashAlgorithmName, nameof(hashAlgorithm));
            ArgumentNullException.ThrowIfNull(padding);

            using (SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle())
            {
                if (hash.Length != GetHashSizeInBytes(hashAlgorithm))
                {
                    throw new CryptographicException(SR.Cryptography_SignHash_WrongSize);
                }

                IntPtr namePtr = Marshal.StringToHGlobalUni(hashAlgorithmName);
                try
                {
                    switch (padding.Mode)
                    {
                        case RSASignaturePaddingMode.Pkcs1:
                            var pkcs1PaddingInfo = new BCRYPT_PKCS1_PADDING_INFO() { pszAlgId = namePtr };
                            return keyHandle.TrySignHash(hash, destination, AsymmetricPaddingMode.NCRYPT_PAD_PKCS1_FLAG, &pkcs1PaddingInfo, out bytesWritten);

                        case RSASignaturePaddingMode.Pss:
                            var pssPaddingInfo = new BCRYPT_PSS_PADDING_INFO() { pszAlgId = namePtr, cbSalt = hash.Length };
                            return keyHandle.TrySignHash(hash, destination, AsymmetricPaddingMode.NCRYPT_PAD_PSS_FLAG, &pssPaddingInfo, out bytesWritten);

                        default:
                            throw new CryptographicException(SR.Cryptography_UnsupportedPaddingMode);
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(namePtr);
                }
            }
        }

        /// <summary>
        ///     Verifies that alleged signature of a hash is, in fact, a valid signature of that hash.
        /// </summary>
        public override bool VerifyHash(byte[] hash, byte[] signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
        {
            ArgumentNullException.ThrowIfNull(hash);
            ArgumentNullException.ThrowIfNull(signature);

            return VerifyHash((ReadOnlySpan<byte>)hash, (ReadOnlySpan<byte>)signature, hashAlgorithm, padding);
        }

        public override unsafe bool VerifyHash(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
        {
            string? hashAlgorithmName = hashAlgorithm.Name;
            ArgumentException.ThrowIfNullOrEmpty(hashAlgorithmName, nameof(hashAlgorithm));
            ArgumentNullException.ThrowIfNull(padding);

            using (SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle())
            {
                if (hash.Length != GetHashSizeInBytes(hashAlgorithm))
                {
                    return false;
                }

                IntPtr namePtr = Marshal.StringToHGlobalUni(hashAlgorithmName);
                try
                {
                    switch (padding.Mode)
                    {
                        case RSASignaturePaddingMode.Pkcs1:
                            var pkcs1PaddingInfo = new BCRYPT_PKCS1_PADDING_INFO() { pszAlgId = namePtr };
                            return keyHandle.VerifyHash(hash, signature, AsymmetricPaddingMode.NCRYPT_PAD_PKCS1_FLAG, &pkcs1PaddingInfo);

                        case RSASignaturePaddingMode.Pss:
                            var pssPaddingInfo = new BCRYPT_PSS_PADDING_INFO() { pszAlgId = namePtr, cbSalt = hash.Length };
                            return keyHandle.VerifyHash(hash, signature, AsymmetricPaddingMode.NCRYPT_PAD_PSS_FLAG, &pssPaddingInfo);

                        default:
                            throw new CryptographicException(SR.Cryptography_UnsupportedPaddingMode);
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(namePtr);
                }
            }
        }
    }
}
