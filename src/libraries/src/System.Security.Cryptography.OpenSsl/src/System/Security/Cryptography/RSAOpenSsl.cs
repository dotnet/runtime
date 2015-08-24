// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Threading;

using Microsoft.Win32.SafeHandles;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    public sealed class RSAOpenSsl : RSA
    {
        private const int BitsPerByte = 8;

        // 65537 (0x10001) in big-endian form
        private static readonly byte[] s_defaultExponent = { 0x01, 0x00, 0x01 };

        // OpenSSL seems to accept answers of all sizes.
        // Choosing a non-multiple of 8 would make some calculations misalign
        // (like assertions of (output.Length * 8) == KeySize).
        // Choosing a number too small is insecure.
        // Choosing a number too large will cause GenerateKey to take much
        // longer than anyone would be willing to wait.
        //
        // So, copying the values from RSACryptoServiceProvider
        private static readonly KeySizes s_legalKeySizes = new KeySizes(384, 16384, 8);

        private Lazy<SafeRsaHandle> _key;

        public RSAOpenSsl()
            : this(1024)
        {
        }

        public RSAOpenSsl(int keySize)
        {
            _legalKeySizesValue = new[] { s_legalKeySizes };
            KeySize = keySize;
            _key = new Lazy<SafeRsaHandle>(GenerateKey);
        }

        public RSAOpenSsl(RSAParameters parameters)
        {
            _legalKeySizesValue = new[] { s_legalKeySizes };
            ImportParameters(parameters);
        }

        /// <summary>
        /// Create an RSAOpenSsl from an existing <see cref="IntPtr"/> whose value is an
        /// existing OpenSSL <c>RSA*</c>.
        /// </summary>
        /// <remarks>
        /// This method will increase the reference count of the <c>RSA*</c>, the caller should
        /// continue to manage the lifetime of their reference.
        /// </remarks>
        /// <param name="handle">A pointer to an OpenSSL <c>RSA*</c></param>
        /// <exception cref="ArgumentException"><paramref name="handle" /> is invalid</exception>
        public RSAOpenSsl(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
                throw new ArgumentException(SR.Cryptography_OpenInvalidHandle, "handle");

            _legalKeySizesValue = new[] { s_legalKeySizes };

            SafeRsaHandle rsaHandle = SafeRsaHandle.DuplicateHandle(handle);

            // Set base.KeySize to avoid throwing an extra Lazy at the GC when
            // using something other than the default keysize.
            base.KeySize = BitsPerByte * Interop.libcrypto.RSA_size(rsaHandle);
            _key = new Lazy<SafeRsaHandle>(() => rsaHandle);
        }

        public override int KeySize
        {
            set
            {
                if (KeySize == value)
                {
                    return;
                }

                FreeKey();
                base.KeySize = value;
                _key = new Lazy<SafeRsaHandle>(GenerateKey);
            }
        }

        public override byte[] Decrypt(byte[] data, RSAEncryptionPadding padding)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            if (padding == null)
                throw new ArgumentNullException("padding");

            Interop.libcrypto.OpenSslRsaPadding openSslPadding;

            if (padding == RSAEncryptionPadding.Pkcs1)
            {
                openSslPadding = Interop.libcrypto.OpenSslRsaPadding.RSA_PKCS1_PADDING;
            }
            else if (padding == RSAEncryptionPadding.OaepSHA1)
            {
                openSslPadding = Interop.libcrypto.OpenSslRsaPadding.RSA_PKCS1_OAEP_PADDING;
            }
            else
            {
                throw PaddingModeNotSupported();
            }

            return Decrypt(data, openSslPadding);
        }

        private byte[] Decrypt(byte[] data, Interop.libcrypto.OpenSslRsaPadding padding)
        {
            SafeRsaHandle key = _key.Value;

            CheckInvalidKey(key);

            byte[] buf = new byte[Interop.libcrypto.RSA_size(key)];

            int returnValue = Interop.libcrypto.RSA_private_decrypt(
                data.Length,
                data,
                buf,
                key,
                padding);

            CheckReturn(returnValue);

            // If the padding mode is RSA_NO_PADDING then the size of the decrypted block
            // will be RSA_size, so let's just return buf.
            //
            // If any padding was used, then some amount (determined by the padding algorithm)
            // will have been reduced, and only returnValue bytes were part of the decrypted
            // body, so copy the decrypted bytes to an appropriately sized array before
            // returning it.
            if (returnValue == buf.Length)
            {
                return buf;
            }

            byte[] plainBytes = new byte[returnValue];
            Array.Copy(buf, plainBytes, returnValue);
            return plainBytes;
        }

        public override byte[] Encrypt(byte[] data, RSAEncryptionPadding padding)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            if (padding == null)
                throw new ArgumentNullException("padding");

            Interop.libcrypto.OpenSslRsaPadding openSslPadding;

            if (padding == RSAEncryptionPadding.Pkcs1)
            {
                openSslPadding = Interop.libcrypto.OpenSslRsaPadding.RSA_PKCS1_PADDING;
            }
            else if (padding == RSAEncryptionPadding.OaepSHA1)
            {
                openSslPadding = Interop.libcrypto.OpenSslRsaPadding.RSA_PKCS1_OAEP_PADDING;
            }
            else
            {
                throw PaddingModeNotSupported();
            }

            return Encrypt(data, openSslPadding);
        }

        private byte[] Encrypt(byte[] data, Interop.libcrypto.OpenSslRsaPadding padding)
        {
            SafeRsaHandle key = _key.Value;

            CheckInvalidKey(key);

            byte[] buf = new byte[Interop.libcrypto.RSA_size(key)];

            int returnValue = Interop.libcrypto.RSA_public_encrypt(
                data.Length,
                data,
                buf,
                key,
                padding);

            CheckReturn(returnValue);

            return buf;
        }

        public override RSAParameters ExportParameters(bool includePrivateParameters)
        {
            // It's entirely possible that this line will cause the key to be generated in the first place.
            SafeRsaHandle key = _key.Value;

            CheckInvalidKey(key);

            RSAParameters rsaParameters = Interop.libcrypto.ExportRsaParameters(key, includePrivateParameters);
            bool hasPrivateKey = rsaParameters.D != null;

            if (hasPrivateKey != includePrivateParameters || !HasConsistentPrivateKey(ref rsaParameters))
            {
                throw new CryptographicException(SR.Cryptography_CSP_NoPrivateKey);
            }

            return rsaParameters;
        }
        
        public override unsafe void ImportParameters(RSAParameters parameters)
        {
            ValidateParameters(ref parameters);

            SafeRsaHandle key = Interop.libcrypto.RSA_new();
            bool imported = false;

            Interop.libcrypto.CheckValidOpenSslHandle(key);

            try
            {
                Interop.libcrypto.RSA_ST* rsaStructure = (Interop.libcrypto.RSA_ST*)key.DangerousGetHandle();

                // RSA_free is going to take care of freeing any of these as long as they successfully
                // get assigned.

                // CreateBignumPtr returns IntPtr.Zero for null input, so this just does the right thing
                // on a public-key-only set of RSAParameters.
                rsaStructure->n = Interop.libcrypto.CreateBignumPtr(parameters.Modulus);
                rsaStructure->e = Interop.libcrypto.CreateBignumPtr(parameters.Exponent);
                rsaStructure->d = Interop.libcrypto.CreateBignumPtr(parameters.D);
                rsaStructure->p = Interop.libcrypto.CreateBignumPtr(parameters.P);
                rsaStructure->dmp1 = Interop.libcrypto.CreateBignumPtr(parameters.DP);
                rsaStructure->q = Interop.libcrypto.CreateBignumPtr(parameters.Q);
                rsaStructure->dmq1 = Interop.libcrypto.CreateBignumPtr(parameters.DQ);
                rsaStructure->iqmp = Interop.libcrypto.CreateBignumPtr(parameters.InverseQ);

                imported = true;
            }
            finally
            {
                if (!imported)
                {
                    key.Dispose();
                }
            }

            FreeKey();
            _key = new Lazy<SafeRsaHandle>(() => key, LazyThreadSafetyMode.None);

            // Set base.KeySize directly, since we don't want to free the key
            // (which we would do if the keysize changed on import)
            base.KeySize = BitsPerByte * Interop.libcrypto.RSA_size(key);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                FreeKey();
            }

            base.Dispose(disposing);
        }

        private void FreeKey()
        {
            if (_key != null && _key.IsValueCreated)
            {
                SafeRsaHandle handle = _key.Value;

                if (handle != null)
                {
                    handle.Dispose();
                }
            }
        }

        private static void ValidateParameters(ref RSAParameters parameters)
        {
            if (parameters.Modulus == null || parameters.Exponent == null)
                throw new CryptographicException(SR.Argument_InvalidValue);

            if (!HasConsistentPrivateKey(ref parameters))
                throw new CryptographicException(SR.Argument_InvalidValue);
        }

        private static bool HasConsistentPrivateKey(ref RSAParameters parameters)
        {
            if (parameters.D == null)
            {
                if (parameters.P != null ||
                    parameters.DP != null ||
                    parameters.Q != null ||
                    parameters.DQ != null ||
                    parameters.InverseQ != null)
                {
                    return false;
                }
            }
            else
            {
                if (parameters.P == null ||
                    parameters.DP == null ||
                    parameters.Q == null ||
                    parameters.DQ == null ||
                    parameters.InverseQ == null)
                {
                    return false;
                }
            }

            return true;
        }

        private static void CheckInvalidKey(SafeRsaHandle key)
        {
            if (key == null || key.IsInvalid)
            {
                throw new CryptographicException(SR.Cryptography_OpenInvalidHandle);
            }
        }

        private static void CheckReturn(int returnValue)
        {
            if (returnValue == -1)
            {
                throw Interop.libcrypto.CreateOpenSslCryptographicException();
            }
        }

        private static void CheckBoolReturn(int returnValue)
        {
            if (returnValue == 1)
            {
                return;
            }

            throw Interop.libcrypto.CreateOpenSslCryptographicException();
        }

        private SafeRsaHandle GenerateKey()
        {
            SafeRsaHandle key = Interop.libcrypto.RSA_new();
            bool generated = false;

            Interop.libcrypto.CheckValidOpenSslHandle(key);

            try
            {
                using (SafeBignumHandle exponent = Interop.libcrypto.CreateBignum(s_defaultExponent))
                {
                    // The documentation for RSA_generate_key_ex does not say that it returns only
                    // 0 or 1, so the call marshalls it back as a full Int32 and checks for a value
                    // of 1 explicitly.
                    int response = Interop.libcrypto.RSA_generate_key_ex(
                        key,
                        KeySize,
                        exponent,
                        IntPtr.Zero);

                    CheckBoolReturn(response);
                    generated = true;
                }
            }
            finally
            {
                if (!generated)
                {
                    key.Dispose();
                }
            }

            return key;
        }

        protected override byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm)
        {
            return OpenSslAsymmetricAlgorithmCore.HashData(data, offset, count, hashAlgorithm);
        }

        protected override byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm)
        {
            return OpenSslAsymmetricAlgorithmCore.HashData(data, hashAlgorithm);
        }

        public override byte[] SignHash(byte[] hash, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
        {
            if (hash == null)
                throw new ArgumentNullException("hash");
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
                throw HashAlgorithmNameNullOrEmpty();
            if (padding == null)
                throw new ArgumentNullException("padding");
            if (padding != RSASignaturePadding.Pkcs1)
                throw PaddingModeNotSupported();

            return SignHash(hash, hashAlgorithm);
        }

        private byte[] SignHash(byte[] hash, HashAlgorithmName hashAlgorithmName)
        {
            int algorithmNid = GetAlgorithmNid(hashAlgorithmName);
            SafeRsaHandle rsa = _key.Value;
            byte[] signature = new byte[Interop.libcrypto.RSA_size(rsa)];
            int signatureSize;

            bool success = Interop.libcrypto.RSA_sign(
                algorithmNid,
                hash,
                hash.Length,
                signature,
                out signatureSize,
                rsa);

            if (!success)
            {
                throw Interop.libcrypto.CreateOpenSslCryptographicException();
            }

            Debug.Assert(
                signatureSize == signature.Length,
                "RSA_sign reported an unexpected signature size",
                "RSA_sign reported signatureSize was {0}, when {1} was expected",
                signatureSize,
                signature.Length);

            return signature;
        }

        public override bool VerifyHash(
            byte[] hash,
            byte[] signature,
            HashAlgorithmName hashAlgorithm,
            RSASignaturePadding padding)
        {
            if (hash == null)
                throw new ArgumentNullException("hash");
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
                throw HashAlgorithmNameNullOrEmpty();
            if (padding == null)
                throw new ArgumentNullException("padding");
            if (padding != RSASignaturePadding.Pkcs1)
                throw PaddingModeNotSupported();

            return VerifyHash(hash, signature, hashAlgorithm);
        }

        private bool VerifyHash(byte[] hash, byte[] signature, HashAlgorithmName hashAlgorithmName)
        {
            int algorithmNid = GetAlgorithmNid(hashAlgorithmName);
            SafeRsaHandle rsa = _key.Value;

            return Interop.libcrypto.RSA_verify(
                algorithmNid,
                hash,
                hash.Length,
                signature,
                signature.Length,
                rsa);
        }

        private static int GetAlgorithmNid(HashAlgorithmName hashAlgorithmName)
        {
            // All of the current HashAlgorithmName values correspond to the SN values in OpenSSL 0.9.8.
            // If there's ever a new one that doesn't, translate it here.
            string sn = hashAlgorithmName.Name;

            int nid = Interop.libcrypto.OBJ_sn2nid(sn);

            if (nid == Interop.libcrypto.NID_undef)
            {
                throw new CryptographicException(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithmName.Name);
            }

            return nid;
        }

        private static Exception PaddingModeNotSupported()
        {
            return new CryptographicException(SR.Cryptography_InvalidPaddingMode);
        }

        private static Exception HashAlgorithmNameNullOrEmpty()
        {
            return new ArgumentException(SR.Cryptography_HashAlgorithmNameNullOrEmpty, "hashAlgorithm");
        }

    }
}
