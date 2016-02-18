// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

using Microsoft.Win32.SafeHandles;

using Internal.Cryptography;

using ErrorCode = Interop.NCrypt.ErrorCode;
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
                throw new ArgumentNullException(nameof(hash));

            unsafe
            {
                byte[] signature = null;
                SignOrVerify(padding, hashAlgorithm, hash,
                    delegate (AsymmetricPaddingMode paddingMode, void* pPaddingInfo)
                    {
                        int estimatedSize = KeySize / 8;
                        signature = GetKeyHandle().SignHash(hash, paddingMode, pPaddingInfo, estimatedSize);
                    }
                );
                return signature;
            }
        }

        /// <summary>
        ///     Verifies that alleged signature of a hash is, in fact, a valid signature of that hash.
        /// </summary>
        public override bool VerifyHash(byte[] hash, byte[] signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
        {
            if (hash == null)
                throw new ArgumentNullException(nameof(hash));
            if (signature == null)
                throw new ArgumentNullException(nameof(signature));

            unsafe
            {
                bool verified = false;
                SignOrVerify(padding, hashAlgorithm, hash,
                    delegate (AsymmetricPaddingMode paddingMode, void* pPaddingInfo)
                    {
                        verified = GetKeyHandle().VerifyHash(hash, signature, paddingMode, pPaddingInfo);
                    }
                );
                return verified;
            }
        }

        //
        // Common helper for SignHash() and VerifyHash(). Creates the necessary PADDING_INFO structure based on the chosen padding mode and then passes it
        // to "signOrVerify" which performs the actual signing or verification.
        //
        private static unsafe void SignOrVerify(RSASignaturePadding padding, HashAlgorithmName hashAlgorithm, byte[] hash, SignOrVerifyAction signOrVerify)
        {
            string hashAlgorithmName = hashAlgorithm.Name;
            if (string.IsNullOrEmpty(hashAlgorithmName))
                throw new ArgumentException(SR.Cryptography_HashAlgorithmNameNullOrEmpty, nameof(hashAlgorithm));

            if (padding == null)
                throw new ArgumentNullException(nameof(padding));

            switch (padding.Mode)
            {
                case RSASignaturePaddingMode.Pkcs1:
                    {
                        using (SafeUnicodeStringHandle safeHashAlgorithmName = new SafeUnicodeStringHandle(hashAlgorithmName))
                        {
                            BCRYPT_PKCS1_PADDING_INFO paddingInfo = new BCRYPT_PKCS1_PADDING_INFO()
                            {
                                pszAlgId = safeHashAlgorithmName.DangerousGetHandle(),
                            };
                            signOrVerify(AsymmetricPaddingMode.NCRYPT_PAD_PKCS1_FLAG, &paddingInfo);
                        }
                        break;
                    }

                case RSASignaturePaddingMode.Pss:
                    {
                        using (SafeUnicodeStringHandle safeHashAlgorithmName = new SafeUnicodeStringHandle(hashAlgorithmName))
                        {
                            BCRYPT_PSS_PADDING_INFO paddingInfo = new BCRYPT_PSS_PADDING_INFO()
                            {
                                pszAlgId = safeHashAlgorithmName.DangerousGetHandle(),
                                cbSalt = hash.Length,
                            };
                            signOrVerify(AsymmetricPaddingMode.NCRYPT_PAD_PSS_FLAG, &paddingInfo);
                        }
                        break;
                    }

                default:
                    throw new CryptographicException(SR.Cryptography_UnsupportedPaddingMode);
            }
        }

        private unsafe delegate void SignOrVerifyAction(AsymmetricPaddingMode paddingMode, void* pPaddingInfo);
    }
#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
    }
#endif
}
