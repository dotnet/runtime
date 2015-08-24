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
    public sealed class ECDsaOpenSsl : ECDsa
    {
        public ECDsaOpenSsl()
            : this(521)
        {
        }

        public ECDsaOpenSsl(int keySize)
        {
            KeySize = keySize;
            _key = new Lazy<SafeEcKeyHandle>(GenerateKey);
        }

        /// <summary>
        /// Create an ECDsaOpenSsl from an existing <see cref="IntPtr"/> whose value is an
        /// existing OpenSSL <c>EC_KEY*</c>.
        /// </summary>
        /// <remarks>
        /// This method will increase the reference count of the <c>EC_KEY*</c>, the caller should
        /// continue to manage the lifetime of their reference.
        /// </remarks>
        /// <param name="handle">A pointer to an OpenSSL <c>EC_KEY*</c></param>
        /// <exception cref="ArgumentException"><paramref name="handle" /> is invalid</exception>
        public ECDsaOpenSsl(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
                throw new ArgumentException(SR.Cryptography_OpenInvalidHandle, "handle");

            SafeEcKeyHandle ecKeyHandle = SafeEcKeyHandle.DuplicateHandle(handle);

            int nid = Interop.libcrypto.EcKeyGetCurveName(ecKeyHandle);
            int keySize = 0;
            for (int i = 0; i < s_supportedAlgorithms.Length; i++)
            {
                if (s_supportedAlgorithms[i].Nid == nid)
                {
                    keySize = s_supportedAlgorithms[i].KeySize;
                    break;
                }
            }
            if (keySize == 0)
            {
                string curveNameOid = Interop.libcrypto.OBJ_obj2txt_helper(Interop.libcrypto.OBJ_nid2obj(nid));
                throw new NotSupportedException(SR.Format(SR.Cryptography_UnsupportedEcKeyAlgorithm, curveNameOid));
            }

            // Set base.KeySize rather than this.KeySize to avoid an unnecessary Lazy<> allocation.
            base.KeySize = keySize;
            _key = new Lazy<SafeEcKeyHandle>(() => ecKeyHandle);
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
                _key = new Lazy<SafeEcKeyHandle>(GenerateKey);
            }
        }

        public override KeySizes[] LegalKeySizes
        {
            get
            {
                KeySizes[] legalKeySizes = new KeySizes[s_supportedAlgorithms.Length];
                for (int i = 0; i < s_supportedAlgorithms.Length; i++)
                {
                    int keySize = s_supportedAlgorithms[i].KeySize;
                    legalKeySizes[i] = new KeySizes(minSize: keySize, maxSize: keySize, skipSize: 0);
                }
                return legalKeySizes;
            }
        }

        public override byte[] SignHash(byte[] hash)
        {
            if (hash == null)
                throw new ArgumentNullException("hash");

            SafeEcKeyHandle key = _key.Value;
            int signatureLength = Interop.libcrypto.ECDSA_size(key);
            byte[] signature = new byte[signatureLength];
            if (!Interop.libcrypto.ECDSA_sign(0, hash, hash.Length, signature, ref signatureLength, key))
                throw Interop.libcrypto.CreateOpenSslCryptographicException();
            Array.Resize(ref signature, signatureLength);
            return signature;
        }

        public override bool VerifyHash(byte[] hash, byte[] signature)
        {
            if (hash == null)
                throw new ArgumentNullException("hash");
            if (signature == null)
                throw new ArgumentNullException("signature");

            SafeEcKeyHandle key = _key.Value;
            int verifyResult = Interop.libcrypto.ECDSA_verify(0, hash, hash.Length, signature, signature.Length, key);
            return verifyResult == 1;
        }

        protected override byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm)
        {
            return OpenSslAsymmetricAlgorithmCore.HashData(data, offset, count, hashAlgorithm);
        }

        protected override byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm)
        {
            return OpenSslAsymmetricAlgorithmCore.HashData(data, hashAlgorithm);
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
                SafeEcKeyHandle handle = _key.Value;

                if (handle != null)
                {
                    handle.Dispose();
                }
            }
        }

        private SafeEcKeyHandle GenerateKey()
        {
            int keySize = KeySize;
            for (int i = 0; i < s_supportedAlgorithms.Length; i++)
            {
                if (keySize == s_supportedAlgorithms[i].KeySize)
                {
                    int nid = s_supportedAlgorithms[i].Nid;
                    SafeEcKeyHandle key = Interop.libcrypto.EC_KEY_new_by_curve_name(nid);
                    if (key == null)
                        throw Interop.libcrypto.CreateOpenSslCryptographicException();

                    if (!Interop.libcrypto.EC_KEY_generate_key(key))
                        throw Interop.libcrypto.CreateOpenSslCryptographicException();

                    return key;
                }
            }

            // The KeySize property should have prevented a bad KeySize from being set.
            Debug.Fail("GenerateKey: Unexpected KeySize: " + keySize);
            throw new InvalidOperationException();  // This is to keep the compiler happy - we don't expect to hit this.
        }

        private Lazy<SafeEcKeyHandle> _key;

        private struct SupportedAlgorithm
        {
            public SupportedAlgorithm(int keySize, int nid)
                : this()
            {
                KeySize = keySize;
                Nid = nid;
            }

            public int KeySize { get; private set; }
            public int Nid { get; private set; }
        }

        private static readonly SupportedAlgorithm[] s_supportedAlgorithms =
            new SupportedAlgorithm[]
            {
                new SupportedAlgorithm(keySize: 224, nid: Interop.libcrypto.NID_secp224r1),
                new SupportedAlgorithm(keySize: 384, nid: Interop.libcrypto.NID_secp384r1),
                new SupportedAlgorithm(keySize: 521, nid: Interop.libcrypto.NID_secp521r1),
            };
    }
}
