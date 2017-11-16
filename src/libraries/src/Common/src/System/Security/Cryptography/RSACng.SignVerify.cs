// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Internal.Cryptography;

using AsymmetricPaddingMode = Interop.NCrypt.AsymmetricPaddingMode;
using BCRYPT_PKCS1_PADDING_INFO = Interop.BCrypt.BCRYPT_PKCS1_PADDING_INFO;
using BCRYPT_PSS_PADDING_INFO = Interop.BCrypt.BCRYPT_PSS_PADDING_INFO;

namespace System.Security.Cryptography
{
#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
    internal static partial class RSAImplementation
    {
#endif
    public sealed partial class RSACng : RSA
    {
        /// <summary>
        ///     Computes the signature of a hash that was produced by the hash algorithm specified by "hashAlgorithm."
        /// </summary>
        public override byte[] SignHash(byte[] hash, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
        {
            if (hash == null)
            {
                throw new ArgumentNullException(nameof(hash));
            }

            string hashAlgorithmName = hashAlgorithm.Name;
            if (string.IsNullOrEmpty(hashAlgorithmName))
            {
                throw new ArgumentException(SR.Cryptography_HashAlgorithmNameNullOrEmpty, nameof(hashAlgorithm));
            }
            if (padding == null)
            {
                throw new ArgumentNullException(nameof(padding));
            }

            using (SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle())
            {
                IntPtr namePtr = Marshal.StringToHGlobalUni(hashAlgorithmName);
                try
                {
                    unsafe
                    {
                        int estimatedSize = KeySize / 8;
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

        public override unsafe bool TrySignHash(ReadOnlySpan<byte> source, Span<byte> destination, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding, out int bytesWritten)
        {
            string hashAlgorithmName = hashAlgorithm.Name;
            if (string.IsNullOrEmpty(hashAlgorithmName))
            {
                throw new ArgumentException(SR.Cryptography_HashAlgorithmNameNullOrEmpty, nameof(hashAlgorithm));
            }
            if (padding == null)
            {
                throw new ArgumentNullException(nameof(padding));
            }

            using (SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle())
            {
                IntPtr namePtr = Marshal.StringToHGlobalUni(hashAlgorithmName);
                try
                {
                    switch (padding.Mode)
                    {
                        case RSASignaturePaddingMode.Pkcs1:
                            var pkcs1PaddingInfo = new BCRYPT_PKCS1_PADDING_INFO() { pszAlgId = namePtr };
                            return keyHandle.TrySignHash(source, destination, AsymmetricPaddingMode.NCRYPT_PAD_PKCS1_FLAG, &pkcs1PaddingInfo, out bytesWritten);

                        case RSASignaturePaddingMode.Pss:
                            var pssPaddingInfo = new BCRYPT_PSS_PADDING_INFO() { pszAlgId = namePtr, cbSalt = source.Length };
                            return keyHandle.TrySignHash(source, destination, AsymmetricPaddingMode.NCRYPT_PAD_PSS_FLAG, &pssPaddingInfo, out bytesWritten);

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
            if (hash == null)
            {
                throw new ArgumentNullException(nameof(hash));
            }
            if (signature == null)
            {
                throw new ArgumentNullException(nameof(signature));
            }

            return VerifyHash((ReadOnlySpan<byte>)hash, (ReadOnlySpan<byte>)signature, hashAlgorithm, padding);
        }

        public override unsafe bool VerifyHash(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
        {
            string hashAlgorithmName = hashAlgorithm.Name;
            if (string.IsNullOrEmpty(hashAlgorithmName))
            {
                throw new ArgumentException(SR.Cryptography_HashAlgorithmNameNullOrEmpty, nameof(hashAlgorithm));
            }
            if (padding == null)
            {
                throw new ArgumentNullException(nameof(padding));
            }

            using (SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle())
            {
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
#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
    }
#endif
}
