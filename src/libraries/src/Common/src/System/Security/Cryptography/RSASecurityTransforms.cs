// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Security.Cryptography.Apple;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    public partial class RSA : AsymmetricAlgorithm
    {
        public static new RSA Create()
        {
            return new RSAImplementation.RSASecurityTransforms();
        }
    }

    internal static partial class RSAImplementation
    {
        public sealed partial class RSASecurityTransforms : RSA
        {
            private SecKeyPair _keys;

            public RSASecurityTransforms()
                : this(2048)
            {
            }

            public RSASecurityTransforms(int keySize)
            {
                KeySize = keySize;
            }

            public override KeySizes[] LegalKeySizes
            {
                get
                {
                    return new KeySizes[]
                    {
                        // All values are in bits.
                        // 1024 was achieved via experimentation.
                        // 1024 and 1024+64 both generated successfully, 1024-64 produced errSecParam.
                        new KeySizes(minSize: 1024, maxSize: 16384, skipSize: 64),
                    };
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

                    // Set the KeySize before freeing the key so that an invalid value doesn't throw away the key
                    base.KeySize = value;

                    _keys?.Dispose();
                    _keys = null;
                }
            }

            public override RSAParameters ExportParameters(bool includePrivateParameters)
            {
                SecKeyPair keys = GetKeys();

                SafeSecKeyRefHandle keyHandle = includePrivateParameters ? keys.PrivateKey : keys.PublicKey;

                if (keyHandle == null)
                {
                    throw new CryptographicException(SR.Cryptography_OpenInvalidHandle);
                }

                DerSequenceReader keyReader = Interop.AppleCrypto.SecKeyExport(keyHandle, includePrivateParameters);
                RSAParameters parameters = new RSAParameters();

                if (includePrivateParameters)
                {
                    keyReader.ReadPkcs8Blob(ref parameters);
                }
                else
                {
                    keyReader.ReadSubjectPublicKeyInfo(ref parameters);
                }

                return parameters;
            }

            public override void ImportParameters(RSAParameters parameters)
            {
                bool isPrivateKey = parameters.D != null;

                if (isPrivateKey)
                {
                    // Start with the private key, in case some of the private key fields
                    // don't match the public key fields.
                    //
                    // Public import should go off without a hitch.
                    SafeSecKeyRefHandle privateKey = ImportKey(parameters);

                    RSAParameters publicOnly = new RSAParameters
                    {
                        Modulus = parameters.Modulus,
                        Exponent = parameters.Exponent,
                    };

                    SafeSecKeyRefHandle publicKey;
                    try
                    {
                        publicKey = ImportKey(publicOnly);
                    }
                    catch
                    {
                        privateKey.Dispose();
                        throw;
                    }

                    SetKey(SecKeyPair.PublicPrivatePair(publicKey, privateKey));
                }
                else
                {
                    SafeSecKeyRefHandle publicKey = ImportKey(parameters);
                    SetKey(SecKeyPair.PublicOnly(publicKey));
                }
            }

            public override byte[] Encrypt(byte[] data, RSAEncryptionPadding padding)
            {
                return Interop.AppleCrypto.RsaEncrypt(GetKeys().PublicKey, data, padding);
            }

            public override byte[] Decrypt(byte[] data, RSAEncryptionPadding padding)
            {
                SecKeyPair keys = GetKeys();

                if (keys.PrivateKey == null)
                {
                    throw new CryptographicException(SR.Cryptography_CSP_NoPrivateKey);
                }

                return Interop.AppleCrypto.RsaDecrypt(keys.PrivateKey, data, padding);
            }

            public override byte[] SignHash(byte[] hash, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
            {
                if (padding != RSASignaturePadding.Pkcs1)
                    throw new CryptographicException(SR.Cryptography_InvalidPaddingMode);

                SecKeyPair keys = GetKeys();

                if (keys.PrivateKey == null)
                {
                    throw new CryptographicException(SR.Cryptography_CSP_NoPrivateKey);
                }

                return Interop.AppleCrypto.GenerateSignature(
                    keys.PrivateKey,
                    hash,
                    PalAlgorithmFromAlgorithmName(hashAlgorithm));
            }

            public override bool VerifyHash(
                byte[] hash,
                byte[] signature,
                HashAlgorithmName hashAlgorithm,
                RSASignaturePadding padding)
            {
                if (padding != RSASignaturePadding.Pkcs1)
                    throw new CryptographicException(SR.Cryptography_InvalidPaddingMode);

                return Interop.AppleCrypto.VerifySignature(
                    GetKeys().PublicKey,
                    hash,
                    signature,
                    PalAlgorithmFromAlgorithmName(hashAlgorithm));
            }

            protected override byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm)
            {
                return AsymmetricAlgorithmHelpers.HashData(data, offset, count, hashAlgorithm);
            }

            protected override byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm)
            {
                return AsymmetricAlgorithmHelpers.HashData(data, hashAlgorithm);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _keys?.Dispose();
                    _keys = null;
                }

                base.Dispose(disposing);
            }

            private static Interop.AppleCrypto.PAL_HashAlgorithm PalAlgorithmFromAlgorithmName(
                HashAlgorithmName hashAlgorithmName)
            {
                if (hashAlgorithmName == HashAlgorithmName.MD5)
                {
                    return Interop.AppleCrypto.PAL_HashAlgorithm.Md5;
                }
                else if (hashAlgorithmName == HashAlgorithmName.SHA1)
                {
                    return Interop.AppleCrypto.PAL_HashAlgorithm.Sha1;
                }
                else if (hashAlgorithmName == HashAlgorithmName.SHA256)
                {
                    return Interop.AppleCrypto.PAL_HashAlgorithm.Sha256;
                }
                else if (hashAlgorithmName == HashAlgorithmName.SHA384)
                {
                    return Interop.AppleCrypto.PAL_HashAlgorithm.Sha384;
                }
                else if (hashAlgorithmName == HashAlgorithmName.SHA512)
                {
                    return Interop.AppleCrypto.PAL_HashAlgorithm.Sha512;
                }

                throw new CryptographicException(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithmName.Name);
            }

            private SecKeyPair GetKeys()
            {
                SecKeyPair current = _keys;

                if (current != null)
                {
                    return current;
                }

#if APPLE_ASYMMETRIC_GENERATION
                SafeSecKeyRefHandle publicKey;
                SafeSecKeyRefHandle privateKey;
                int osStatus;

                int gen = Interop.AppleCrypto.RsaGenerateKey(KeySizeValue, out publicKey, out privateKey, out osStatus);

                if (gen != 1)
                {
                    Debug.Assert(gen == 0, $"Expected gen=0, got gen={gen}");
                    throw Interop.AppleCrypto.CreateExceptionForCCError(osStatus, Interop.AppleCrypto.OSStatus);
                }

                current = SecKeyPair.PublicPrivatePair(publicKey, privateKey);
                _keys = current;
                return current;
#else
                throw new CryptographicException("RSA Key Generation is temporarily disabled");
#endif
            }

            private void SetKey(SecKeyPair newKeyPair)
            {
                SecKeyPair current = _keys;
                _keys = newKeyPair;
                current?.Dispose();

                if (newKeyPair != null)
                {
                    KeySizeValue = Interop.AppleCrypto.GetSimpleKeySizeInBits(newKeyPair.PublicKey);
                }
            }

            private static SafeSecKeyRefHandle ImportKey(RSAParameters parameters)
            {
                bool isPrivateKey = parameters.D != null;
                byte[] pkcs1Blob = isPrivateKey ? parameters.ToPkcs1Blob() : parameters.ToSubjectPublicKeyInfo();

                return Interop.AppleCrypto.ImportEphemeralKey(pkcs1Blob, isPrivateKey);
            }
        }
    }

    internal static class RsaKeyBlobHelpers
    {
        private const string RsaOid = "1.2.840.113549.1.1.1";

        // The PKCS#1 version blob for an RSA key based on 2 primes.
        private static readonly byte[] s_versionNumberBytes = { 0 };

        // The AlgorithmIdentifier structure for RSA contains an explicit NULL, for legacy/compat reasons.
        private static readonly byte[][] s_encodedRsaAlgorithmIdentifier =
            DerEncoder.ConstructSegmentedSequence(
                DerEncoder.SegmentedEncodeOid(new Oid(RsaOid)),
                // DER:NULL (0x05 0x00)
                new byte[][]
                {
                    new byte[] { (byte)DerSequenceReader.DerTag.Null },
                    new byte[] { 0 }, 
                    Array.Empty<byte>(),
                });

        internal static byte[] ToPkcs1Blob(this RSAParameters parameters)
        {
            if (parameters.Exponent == null || parameters.Modulus == null)
                throw new CryptographicException(SR.Cryptography_InvalidRsaParameters);

            if (parameters.D == null)
            {
                if (parameters.P != null ||
                    parameters.DP != null ||
                    parameters.Q != null ||
                    parameters.DQ != null ||
                    parameters.InverseQ != null)
                {
                    throw new CryptographicException(SR.Cryptography_InvalidRsaParameters);
                }

                return DerEncoder.ConstructSequence(
                    DerEncoder.SegmentedEncodeUnsignedInteger(parameters.Modulus),
                    DerEncoder.SegmentedEncodeUnsignedInteger(parameters.Exponent));
            }

            if (parameters.P == null ||
                parameters.DP == null ||
                parameters.Q == null ||
                parameters.DQ == null ||
                parameters.InverseQ == null)
            {
                throw new CryptographicException(SR.Cryptography_InvalidRsaParameters);
            }

            return DerEncoder.ConstructSequence(
                DerEncoder.SegmentedEncodeUnsignedInteger(s_versionNumberBytes),
                DerEncoder.SegmentedEncodeUnsignedInteger(parameters.Modulus),
                DerEncoder.SegmentedEncodeUnsignedInteger(parameters.Exponent),
                DerEncoder.SegmentedEncodeUnsignedInteger(parameters.D),
                DerEncoder.SegmentedEncodeUnsignedInteger(parameters.P),
                DerEncoder.SegmentedEncodeUnsignedInteger(parameters.Q),
                DerEncoder.SegmentedEncodeUnsignedInteger(parameters.DP),
                DerEncoder.SegmentedEncodeUnsignedInteger(parameters.DQ),
                DerEncoder.SegmentedEncodeUnsignedInteger(parameters.InverseQ));
        }

        internal static void ReadPkcs8Blob(this DerSequenceReader reader, ref RSAParameters parameters)
        {
            // OneAsymmetricKey ::= SEQUENCE {
            //   version                   Version,
            //   privateKeyAlgorithm       PrivateKeyAlgorithmIdentifier,
            //   privateKey                PrivateKey,
            //   attributes            [0] Attributes OPTIONAL,
            //   ...,
            //   [[2: publicKey        [1] PublicKey OPTIONAL ]],
            //   ...
            // }
            //
            // PrivateKeyInfo ::= OneAsymmetricKey
            //
            // PrivateKey ::= OCTET STRING

            int version = reader.ReadInteger();

            // We understand both version 0 and 1 formats,
            // which are now known as v1 and v2, respectively.
            if (version > 1)
            {
                throw new CryptographicException();
            }

            {
                // Ensure we're reading RSA
                DerSequenceReader algorithm = reader.ReadSequence();

                string algorithmOid = algorithm.ReadOidAsString();

                if (algorithmOid != RsaOid)
                {
                    throw new CryptographicException();
                }
            }

            byte[] privateKeyBytes = reader.ReadOctetString();
            // Because this was an RSA private key, the key format is PKCS#1.
            ReadPkcs1PrivateBlob(privateKeyBytes, ref parameters);

            // We don't care about the rest of the blob here, but it's expected to not exist.
        }

        internal static byte[] ToSubjectPublicKeyInfo(this RSAParameters parameters)
        {
            Debug.Assert(parameters.D == null);

            // SubjectPublicKeyInfo::= SEQUENCE  {
            //    algorithm AlgorithmIdentifier,
            //    subjectPublicKey     BIT STRING  }
            return DerEncoder.ConstructSequence(
                s_encodedRsaAlgorithmIdentifier,
                DerEncoder.SegmentedEncodeBitString(
                    parameters.ToPkcs1Blob()));
        }

        internal static void ReadSubjectPublicKeyInfo(this DerSequenceReader keyInfo, ref RSAParameters parameters)
        {
            // SubjectPublicKeyInfo::= SEQUENCE  {
            //    algorithm AlgorithmIdentifier,
            //    subjectPublicKey     BIT STRING  }
            DerSequenceReader algorithm = keyInfo.ReadSequence();
            string algorithmOid = algorithm.ReadOidAsString();

            if (algorithmOid != RsaOid)
            {
                throw new CryptographicException();
            }

            byte[] subjectPublicKeyBytes = keyInfo.ReadBitString();

            DerSequenceReader subjectPublicKey = new DerSequenceReader(subjectPublicKeyBytes);

            parameters.Modulus = KeyBlobHelpers.TrimPaddingByte(subjectPublicKey.ReadIntegerBytes());
            parameters.Exponent = KeyBlobHelpers.TrimPaddingByte(subjectPublicKey.ReadIntegerBytes());

            if (subjectPublicKey.HasData)
                throw new CryptographicException();
        }

        private static void ReadPkcs1PrivateBlob(byte[] privateKeyBytes, ref RSAParameters parameters)
        {
            // RSAPrivateKey::= SEQUENCE {
            //    version Version,
            //    modulus           INTEGER,  --n
            //    publicExponent INTEGER,  --e
            //    privateExponent INTEGER,  --d
            //    prime1 INTEGER,  --p
            //    prime2 INTEGER,  --q
            //    exponent1 INTEGER,  --d mod(p - 1)
            //    exponent2 INTEGER,  --d mod(q - 1)
            //    coefficient INTEGER,  --(inverse of q) mod p
            //    otherPrimeInfos OtherPrimeInfos OPTIONAL
            // }
            DerSequenceReader privateKey = new DerSequenceReader(privateKeyBytes);
            int version = privateKey.ReadInteger();

            if (version != 0)
            {
                throw new CryptographicException();
            }

            parameters.Modulus = KeyBlobHelpers.TrimPaddingByte(privateKey.ReadIntegerBytes());
            parameters.Exponent = KeyBlobHelpers.TrimPaddingByte(privateKey.ReadIntegerBytes());

            int modulusLen = parameters.Modulus.Length;
            int halfModulus = modulusLen / 2;

            parameters.D = KeyBlobHelpers.PadOrTrim(privateKey.ReadIntegerBytes(), modulusLen);
            parameters.P = KeyBlobHelpers.PadOrTrim(privateKey.ReadIntegerBytes(), halfModulus);
            parameters.Q = KeyBlobHelpers.PadOrTrim(privateKey.ReadIntegerBytes(), halfModulus);
            parameters.DP = KeyBlobHelpers.PadOrTrim(privateKey.ReadIntegerBytes(), halfModulus);
            parameters.DQ = KeyBlobHelpers.PadOrTrim(privateKey.ReadIntegerBytes(), halfModulus);
            parameters.InverseQ = KeyBlobHelpers.PadOrTrim(privateKey.ReadIntegerBytes(), halfModulus);

            if (privateKey.HasData)
            {
                throw new CryptographicException();
            }
        }
    }
}
