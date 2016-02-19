// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

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
                throw new ArgumentException(SR.Cryptography_OpenInvalidHandle, nameof(handle));

            SafeEcKeyHandle ecKeyHandle = SafeEcKeyHandle.DuplicateHandle(handle);

            // Set base.KeySize rather than this.KeySize to avoid an unnecessary Lazy<> allocation.
            base.KeySize = GetKeySize(ecKeyHandle);
            _key = new Lazy<SafeEcKeyHandle>(() => ecKeyHandle);
        }

        /// <summary>
        /// Create an ECDsaOpenSsl from an <see cref="SafeEvpPKeyHandle"/> whose value is an existing
        /// OpenSSL <c>EVP_PKEY*</c> wrapping an <c>EC_KEY*</c>
        /// </summary>
        /// <param name="pkeyHandle">A SafeHandle for an OpenSSL <c>EVP_PKEY*</c></param>
        /// <exception cref="ArgumentNullException"><paramref name="pkeyHandle"/> is <c>null</c></exception>
        /// <exception cref="ArgumentException"><paramref name="pkeyHandle"/> <see cref="SafeHandle.IsInvalid" /></exception>
        /// <exception cref="CryptographicException"><paramref name="pkeyHandle"/> is not a valid enveloped <c>EC_KEY*</c></exception>
        public ECDsaOpenSsl(SafeEvpPKeyHandle pkeyHandle)
        {
            if (pkeyHandle == null)
                throw new ArgumentNullException(nameof(pkeyHandle));
            if (pkeyHandle.IsInvalid)
                throw new ArgumentException(SR.Cryptography_OpenInvalidHandle, nameof(pkeyHandle));

            // If ecKey is valid it has already been up-ref'd, so we can just use this handle as-is.
            SafeEcKeyHandle ecKey = Interop.Crypto.EvpPkeyGetEcKey(pkeyHandle);

            if (ecKey.IsInvalid)
            {
                throw Interop.Crypto.CreateOpenSslCryptographicException();
            }

            // Set base.KeySize rather than this.KeySize to avoid an unnecessary Lazy<> allocation.
            base.KeySize = GetKeySize(ecKey);
            _key = new Lazy<SafeEcKeyHandle>(() => ecKey);
        }

        /// <summary>
        /// Obtain a SafeHandle version of an EVP_PKEY* which wraps an EC_KEY* equivalent
        /// to the current key for this instance.
        /// </summary>
        /// <returns>A SafeHandle for the EC_KEY key in OpenSSL</returns>
        public SafeEvpPKeyHandle DuplicateKeyHandle()
        {
            SafeEcKeyHandle currentKey = _key.Value;
            SafeEvpPKeyHandle pkeyHandle = Interop.Crypto.EvpPkeyCreate();

            try
            {
                // Wrapping our key in an EVP_PKEY will up_ref our key.
                // When the EVP_PKEY is Disposed it will down_ref the key.
                // So everything should be copacetic.
                if (!Interop.Crypto.EvpPkeySetEcKey(pkeyHandle, currentKey))
                {
                    throw Interop.Crypto.CreateOpenSslCryptographicException();
                }

                return pkeyHandle;
            }
            catch
            {
                pkeyHandle.Dispose();
                throw;
            }
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
                throw new ArgumentNullException(nameof(hash));

            SafeEcKeyHandle key = _key.Value;
            int signatureLength = Interop.Crypto.EcDsaSize(key);
            byte[] signature = new byte[signatureLength];
            if (!Interop.Crypto.EcDsaSign(hash, hash.Length, signature, ref signatureLength, key))
                throw Interop.Crypto.CreateOpenSslCryptographicException();

            byte[] converted = ConvertToApiFormat(signature, 0, signatureLength);

            return converted;
        }

        public override bool VerifyHash(byte[] hash, byte[] signature)
        {
            if (hash == null)
                throw new ArgumentNullException(nameof(hash));
            if (signature == null)
                throw new ArgumentNullException(nameof(signature));

            // The signature format for .NET is r.Concat(s). Each of r and s are of length BitsToBytes(KeySize), even
            // when they would have leading zeroes.  If it's the correct size, then we need to encode it from
            // r.Concat(s) to SEQUENCE(INTEGER(r), INTEGER(s)), because that's the format that OpenSSL expects.

            int expectedBytes = 2 * GetSignatureFieldSize();

            if (signature.Length != expectedBytes)
            {
                // The input isn't of the right length, so we can't sensibly re-encode it.
                return false;
            }

            byte[] openSslFormat = ConvertToOpenSslFormat(signature);

            SafeEcKeyHandle key = _key.Value;
            int verifyResult = Interop.Crypto.EcDsaVerify(hash, hash.Length, openSslFormat, openSslFormat.Length, key);
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

        private int GetSignatureFieldSize()
        {
            int keySizeBits = KeySize;
            int keySizeBytes = (keySizeBits + 7) / 8;
            return keySizeBytes;
        }

        private static byte[] ConvertToOpenSslFormat(byte[] input)
        {
            Debug.Assert(input != null);
            Debug.Assert(input.Length % 2 == 0);
            Debug.Assert(input.Length > 1);

            // Input is (r, s), each of them exactly half of the array.
            // Output is the DER encoded value of CONSTRUCTEDSEQUENCE(INTEGER(r), INTEGER(s)).
            int halfLength = input.Length / 2;

            byte[][] rEncoded = DerEncoder.SegmentedEncodeUnsignedInteger(input, 0, halfLength);
            byte[][] sEncoded = DerEncoder.SegmentedEncodeUnsignedInteger(input, halfLength, halfLength);

            return DerEncoder.ConstructSequence(rEncoded, sEncoded);
        }

        private byte[] ConvertToApiFormat(byte[] input, int inputOffset, int inputCount)
        {
            int size = GetSignatureFieldSize();

            try
            {
                DerSequenceReader reader = new DerSequenceReader(input, inputOffset, inputCount);
                byte[] rDer = reader.ReadIntegerBytes();
                byte[] sDer = reader.ReadIntegerBytes();
                byte[] response = new byte[2 * size];

                CopySignatureField(rDer, response, 0, size);
                CopySignatureField(sDer, response, size, size);
                
                return response;
            }
            catch (InvalidOperationException e)
            {
                throw new CryptographicException(SR.Arg_CryptographyException, e);
            }
        }

        private static void CopySignatureField(byte[] signatureField, byte[] response, int offset, int fieldLength)
        {
            if (signatureField.Length > fieldLength)
            {
                // The only way this should be true is if the value required a zero-byte-pad.
                Debug.Assert(signatureField.Length == fieldLength + 1, "signatureField.Length == fieldLength + 1");
                Debug.Assert(signatureField[0] == 0, "signatureField[0] == 0");
                Debug.Assert(signatureField[1] > 0x7F, "signatureField[1] > 0x7F");

                Buffer.BlockCopy(signatureField, 1, response, offset, fieldLength);
            }
            else if (signatureField.Length == fieldLength)
            {
                Buffer.BlockCopy(signatureField, 0, response, offset, fieldLength);
            }
            else
            {
                // If the field is too short then it needs to be prepended
                // with zeroes in the response.  Since the array was already
                // zeroed out, just figure out where we need to start copying.
                int writeOffset = fieldLength - signatureField.Length;

                Buffer.BlockCopy(signatureField, 0, response, offset + writeOffset, signatureField.Length);
            }
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

        private static int GetKeySize(SafeEcKeyHandle ecKeyHandle)
        {
            int nid = Interop.Crypto.EcKeyGetCurveName(ecKeyHandle);
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
                string curveNameOid = Interop.Crypto.GetOidValue(Interop.Crypto.ObjNid2Obj(nid));
                throw new NotSupportedException(SR.Format(SR.Cryptography_UnsupportedEcKeyAlgorithm, curveNameOid));
            }

            return keySize;
        }

        private SafeEcKeyHandle GenerateKey()
        {
            int keySize = KeySize;
            for (int i = 0; i < s_supportedAlgorithms.Length; i++)
            {
                if (keySize == s_supportedAlgorithms[i].KeySize)
                {
                    int nid = s_supportedAlgorithms[i].Nid;
                    SafeEcKeyHandle key = Interop.Crypto.EcKeyCreateByCurveName(nid);
                    if (key == null || key.IsInvalid)
                        throw Interop.Crypto.CreateOpenSslCryptographicException();

                    if (!Interop.Crypto.EcKeyGenerateKey(key))
                        throw Interop.Crypto.CreateOpenSslCryptographicException();

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
            RemoveAlgorithmsUnsupportedByOs(
                new SupportedAlgorithm[]
                {
                    new SupportedAlgorithm(keySize: 224, nid: Interop.Crypto.NID_secp224r1),
                    new SupportedAlgorithm(keySize: 256, nid: Interop.Crypto.NID_X9_62_prime256v1),
                    new SupportedAlgorithm(keySize: 384, nid: Interop.Crypto.NID_secp384r1),
                    new SupportedAlgorithm(keySize: 521, nid: Interop.Crypto.NID_secp521r1),
                }
            );

        private static SupportedAlgorithm[] RemoveAlgorithmsUnsupportedByOs(SupportedAlgorithm[] supportedAlgorithms)
        {
            List<SupportedAlgorithm> filteredSupportedAlgorithms = new List<SupportedAlgorithm>(supportedAlgorithms.Length);
            foreach (SupportedAlgorithm supportedAlgorithm in supportedAlgorithms)
            {
                int nid = supportedAlgorithm.Nid;
                using (SafeEcKeyHandle key = Interop.Crypto.EcKeyCreateByCurveName(nid))
                {
                    if (key != null && !key.IsInvalid)
                    {
                        filteredSupportedAlgorithms.Add(supportedAlgorithm);
                    }
                }
            }
            return filteredSupportedAlgorithms.ToArray();
        }
    }
}
