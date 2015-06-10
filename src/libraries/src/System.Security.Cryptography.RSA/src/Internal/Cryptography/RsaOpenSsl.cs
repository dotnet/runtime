// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography;
using System.Threading;

using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography
{
    internal sealed class RSAOpenSsl : RSA
    {
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

        public override byte[] DecryptValue(byte[] data)
        {
            throw new NotSupportedException("NotSupported_Method");
        }

        internal byte[] Decrypt(byte[] data, Interop.libcrypto.OpenSslRsaPadding padding)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

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
      
        public override byte[] EncryptValue(byte[] data)
        {
            throw new NotSupportedException("NotSupported_Method");
        }

        internal byte[] Encrypt(byte[] data, Interop.libcrypto.OpenSslRsaPadding padding)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

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

            return Interop.libcrypto.ExportRsaParameters(key, includePrivateParameters);
        }
        
        public override unsafe void ImportParameters(RSAParameters parameters)
        {
            SafeRsaHandle key = Interop.libcrypto.RSA_new();
            bool imported = false;

            CheckInvalidNewKey(key);

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
            base.KeySize = 8 * Interop.libcrypto.RSA_size(key);
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

        private static void CheckInvalidKey(SafeRsaHandle key)
        {
            if (key == null || key.IsInvalid)
            {
                throw new CryptographicException(SR.Cryptography_OpenInvalidHandle);
            }
        }

        private static void CheckInvalidNewKey(SafeRsaHandle key)
        {
            if (key == null || key.IsInvalid)
            {
                throw CreateOpenSslException();
            }
        }

        private static void CheckReturn(int returnValue)
        {
            if (returnValue == -1)
            {
                throw CreateOpenSslException();
            }
        }

        private static void CheckBoolReturn(int returnValue)
        {
            if (returnValue == 1)
            {
                return;
            }

            throw CreateOpenSslException();
        }

        private SafeRsaHandle GenerateKey()
        {
            SafeRsaHandle key = Interop.libcrypto.RSA_new();
            bool generated = false;

            CheckInvalidNewKey(key);

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

        private static Exception CreateOpenSslException()
        {
            return new CryptographicException(Interop.libcrypto.GetOpenSslErrorString());
        }
    }
}
