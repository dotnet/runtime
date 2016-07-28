// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.IO;

namespace System.Security.Cryptography
{
#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
    internal static partial class ECDsaImplementation
    {
#endif
        public sealed partial class ECDsaOpenSsl : ECDsa
        {
            internal const string ECDSA_P256_OID_VALUE = "1.2.840.10045.3.1.7"; // Also called nistP256 or secP256r1
            internal const string ECDSA_P384_OID_VALUE = "1.3.132.0.34"; // Also called nistP384 or secP384r1
            internal const string ECDSA_P521_OID_VALUE = "1.3.132.0.35"; // Also called nistP521or secP521r1

            private Lazy<SafeEcKeyHandle> _key;

            /// <summary>
            /// Create an ECDsaOpenSsl algorithm with a named curve.
            /// </summary>
            /// <param name="curve">The <see cref="ECCurve"/> representing the curve.</param>
            /// <exception cref="ArgumentNullException">if <paramref name="curve" /> is null.</exception>
            public ECDsaOpenSsl(ECCurve curve)
            {
                GenerateKey(curve);
            }

            /// <summary>
            ///     Create an ECDsaOpenSsl algorithm with a random 521 bit key pair.
            /// </summary>
            public ECDsaOpenSsl()
                : this(521)
            {
            }

            /// <summary>
            ///     Creates a new ECDsaOpenSsl object that will use a randomly generated key of the specified size.
            /// </summary>
            /// <param name="keySize">Size of the key to generate, in bits.</param>
            public ECDsaOpenSsl(int keySize)
            {
                KeySize = keySize;
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
                SetKey(ecKeyHandle);
            }

            /// <summary>
            /// Set the KeySize without validating against LegalKeySizes.
            /// </summary>
            /// <param name="newKeySize">The value to set the KeySize to.</param>
            private void ForceSetKeySize(int newKeySize)
            {
                // In the event that a key was loaded via ImportParameters, curve name, or an IntPtr/SafeHandle
                // it could be outside of the bounds that we currently represent as "legal key sizes".
                // Since that is our view into the underlying component it can be detached from the
                // component's understanding.  If it said it has opened a key, and this is the size, trust it.
                KeySizeValue = newKeySize;
            }

            public override KeySizes[] LegalKeySizes
            {
                get
                {
                    // Return the three sizes that can be explicitly set (for backwards compatibility)
                    return new[] {
                        new KeySizes(minSize: 256, maxSize: 384, skipSize: 128),
                        new KeySizes(minSize: 521, maxSize: 521, skipSize: 0),
                    };
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

            public override int KeySize
            {
                get
                {
                    return base.KeySize;
                }
                set
                {
                    if (KeySize == value)
                        return;

                    // Set the KeySize before FreeKey so that an invalid value doesn't throw away the key
                    base.KeySize = value;

                    FreeKey();
                    _key = new Lazy<SafeEcKeyHandle>(GenerateKeyLazy);
                }
            }

            public override void GenerateKey(ECCurve curve)
            {
                curve.Validate();
                FreeKey();

                if (curve.IsNamed)
                {
                    string oid = null;
                    // Use oid Value first if present, otherwise FriendlyName because Oid maintains a hard-coded
                    // cache that may have different casing for FriendlyNames than OpenSsl
                    oid = !string.IsNullOrEmpty(curve.Oid.Value) ? curve.Oid.Value : curve.Oid.FriendlyName;

                    SafeEcKeyHandle key = Interop.Crypto.EcKeyCreateByOid(oid);

                    if (key == null || key.IsInvalid)
                        throw new PlatformNotSupportedException(string.Format(SR.Cryptography_CurveNotSupported, oid));

                    if (!Interop.Crypto.EcKeyGenerateKey(key))
                        throw Interop.Crypto.CreateOpenSslCryptographicException();

                    SetKey(key);
                }
                else if (curve.IsExplicit)
                {
                    SafeEcKeyHandle key = Interop.Crypto.EcKeyCreateByExplicitCurve(curve);

                    if (!Interop.Crypto.EcKeyGenerateKey(key))
                        throw Interop.Crypto.CreateOpenSslCryptographicException();

                    SetKey(key);
                }
                else
                {
                    throw new PlatformNotSupportedException(string.Format(SR.Cryptography_CurveNotSupported, curve.CurveType.ToString()));
                }
            }

            private SafeEcKeyHandle GenerateKeyLazy()
            {
                string oid = null;
                switch (KeySize)
                {
                    case 256: oid = ECDSA_P256_OID_VALUE; break;
                    case 384: oid = ECDSA_P384_OID_VALUE; break;
                    case 521: oid = ECDSA_P521_OID_VALUE; break;
                    default:
                        // Only above three sizes supported for backwards compatibility; named curves should be used instead
                        throw new InvalidOperationException(SR.Cryptography_InvalidKeySize);
                }

                SafeEcKeyHandle key = Interop.Crypto.EcKeyCreateByOid(oid);

                if (key == null || key.IsInvalid)
                    throw new PlatformNotSupportedException(string.Format(SR.Cryptography_CurveNotSupported, oid));

                if (!Interop.Crypto.EcKeyGenerateKey(key))
                    throw Interop.Crypto.CreateOpenSslCryptographicException();

                return key;
            }

            private void FreeKey()
            {
                if (_key != null)
                {
                    if (_key.IsValueCreated)
                    {
                        SafeEcKeyHandle handle = _key.Value;
                        if (handle != null)
                            handle.Dispose();
                    }
                    _key = null;
                }
            }

            private void SetKey(SafeEcKeyHandle newKey)
            {
                // Use ForceSet instead of the property setter to ensure that LegalKeySizes doesn't interfere
                // with the already loaded key.
                ForceSetKeySize(Interop.Crypto.EcKeyGetSize(newKey));

                _key = new Lazy<SafeEcKeyHandle>(() => newKey);

                // Have Lazy<T> consider the key to be loaded
                var dummy = _key.Value;
            }
        }
#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
    }
#endif
}
