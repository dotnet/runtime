// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Security.Cryptography.Apple;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
    public partial class ECDsa : AsymmetricAlgorithm
    {
        /// <summary>
        /// Creates an instance of the platform specific implementation of the cref="ECDsa" algorithm.
        /// </summary>
        public static new ECDsa Create()
        {
            return new ECDsaImplementation.ECDsaSecurityTransforms();
        }

        /// <summary>
        /// Creates an instance of the platform specific implementation of the cref="ECDsa" algorithm.
        /// </summary>
        /// <param name="curve">
        /// The <see cref="ECCurve"/> representing the elliptic curve.
        /// </param>
        public static ECDsa Create(ECCurve curve)
        {
            ECDsa ecdsa = Create();
            ecdsa.GenerateKey(curve);
            return ecdsa;
        }

        /// <summary>
        /// Creates an instance of the platform specific implementation of the cref="ECDsa" algorithm.
        /// </summary>
        /// <param name="parameters">
        /// The <see cref="ECParameters"/> representing the elliptic curve parameters.
        /// </param>
        public static ECDsa Create(ECParameters parameters)
        {
            ECDsa ecdsa = Create();
            ecdsa.ImportParameters(parameters);
            return ecdsa;
        }
#endif
        internal static partial class ECDsaImplementation
        {
            public sealed partial class ECDsaSecurityTransforms : ECDsa
            {
                private SecKeyPair _keys;

                public ECDsaSecurityTransforms()
                {
                    KeySize = 521;
                }

                internal ECDsaSecurityTransforms(SafeSecKeyRefHandle publicKey)
                {
                    SetKey(SecKeyPair.PublicOnly(publicKey));
                }

                internal ECDsaSecurityTransforms(SafeSecKeyRefHandle publicKey, SafeSecKeyRefHandle privateKey)
                {
                    SetKey(SecKeyPair.PublicPrivatePair(publicKey, privateKey));
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

                        if (_keys != null)
                        {
                            _keys.Dispose();
                            _keys = null;
                        }
                    }
                }

                public override byte[] SignHash(byte[] hash)
                {
                    if (hash == null)
                        throw new ArgumentNullException(nameof(hash));

                    SecKeyPair keys = GetKeys();

                    if (keys.PrivateKey == null)
                    {
                        throw new CryptographicException(SR.Cryptography_CSP_NoPrivateKey);
                    }

                    byte[] derFormatSignature = Interop.AppleCrypto.GenerateSignature(keys.PrivateKey, hash);
                    byte[] ieeeFormatSignature = AsymmetricAlgorithmHelpers.ConvertDerToIeee1363(
                        derFormatSignature,
                        0,
                        derFormatSignature.Length,
                        KeySize);

                    return ieeeFormatSignature;
                }

                public override bool TrySignHash(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
                {
                    SecKeyPair keys = GetKeys();
                    if (keys.PrivateKey == null)
                    {
                        throw new CryptographicException(SR.Cryptography_CSP_NoPrivateKey);
                    }

                    byte[] derFormatSignature = Interop.AppleCrypto.GenerateSignature(keys.PrivateKey, source);
                    byte[] ieeeFormatSignature = AsymmetricAlgorithmHelpers.ConvertDerToIeee1363(
                        derFormatSignature,
                        0,
                        derFormatSignature.Length,
                        KeySize);

                    if (ieeeFormatSignature.Length <= destination.Length)
                    {
                        new ReadOnlySpan<byte>(ieeeFormatSignature).CopyTo(destination);
                        bytesWritten = ieeeFormatSignature.Length;
                        return true;
                    }
                    else
                    {
                        bytesWritten = 0;
                        return false;
                    }
                }

                public override bool VerifyHash(byte[] hash, byte[] signature)
                {
                    if (hash == null)
                        throw new ArgumentNullException(nameof(hash));
                    if (signature == null)
                        throw new ArgumentNullException(nameof(signature));

                    return VerifyHash((ReadOnlySpan<byte>)hash, (ReadOnlySpan<byte>)signature);
                }

                public override bool VerifyHash(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature) =>
                    Interop.AppleCrypto.VerifySignature(
                        GetKeys().PublicKey,
                        hash,
                        AsymmetricAlgorithmHelpers.ConvertIeee1363ToDer(signature));

                protected override byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm) =>
                    AsymmetricAlgorithmHelpers.HashData(data, offset, count, hashAlgorithm);

                protected override byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm) =>
                    AsymmetricAlgorithmHelpers.HashData(data, hashAlgorithm);

                protected override bool TryHashData(ReadOnlySpan<byte> source, Span<byte> destination, HashAlgorithmName hashAlgorithm, out int bytesWritten) =>
                    AsymmetricAlgorithmHelpers.TryHashData(source, destination, hashAlgorithm, out bytesWritten);

                protected override void Dispose(bool disposing)
                {
                    if (disposing)
                    {
                        if (_keys != null)
                        {
                            _keys.Dispose();
                            _keys = null;
                        }
                    }

                    base.Dispose(disposing);
                }

                public override ECParameters ExportExplicitParameters(bool includePrivateParameters)
                {
                    throw new PlatformNotSupportedException(SR.Cryptography_ECC_NamedCurvesOnly);
                }

                public override ECParameters ExportParameters(bool includePrivateParameters)
                {
                    SecKeyPair keys = GetKeys();

                    SafeSecKeyRefHandle keyHandle = includePrivateParameters ? keys.PrivateKey : keys.PublicKey;

                    if (keyHandle == null)
                    {
                        throw new CryptographicException(SR.Cryptography_OpenInvalidHandle);
                    }

                    DerSequenceReader keyReader = Interop.AppleCrypto.SecKeyExport(keyHandle, includePrivateParameters);
                    ECParameters parameters = new ECParameters();

                    if (includePrivateParameters)
                    {
                        keyReader.ReadPkcs8Blob(ref parameters);
                    }
                    else
                    {
                        keyReader.ReadSubjectPublicKeyInfo(ref parameters);
                    }

                    int size = AsymmetricAlgorithmHelpers.BitsToBytes(KeySize);

                    KeyBlobHelpers.PadOrTrim(ref parameters.Q.X, size);
                    KeyBlobHelpers.PadOrTrim(ref parameters.Q.Y, size);

                    if (includePrivateParameters)
                    {
                        KeyBlobHelpers.PadOrTrim(ref parameters.D, size);
                    }

                    return parameters;
                }

                public override void ImportParameters(ECParameters parameters)
                {
                    parameters.Validate();

                    bool isPrivateKey = parameters.D != null;

                    if (isPrivateKey)
                    {
                        // Start with the private key, in case some of the private key fields don't
                        // match the public key fields and the system determines an integrity failure.
                        //
                        // Public import should go off without a hitch.
                        SafeSecKeyRefHandle privateKey = ImportKey(parameters);

                        ECParameters publicOnly = parameters;
                        publicOnly.D = null;

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

                public override void GenerateKey(ECCurve curve)
                {
                    curve.Validate();

                    if (!curve.IsNamed)
                    {
                        throw new PlatformNotSupportedException(SR.Cryptography_ECC_NamedCurvesOnly);
                    }

                    int keySize;

                    switch (curve.Oid.Value)
                    {
                        // secp256r1 / nistp256
                        case "1.2.840.10045.3.1.7":
                            keySize = 256;
                            break;
                        // secp384r1 / nistp384
                        case "1.3.132.0.34":
                            keySize = 384;
                            break;
                        // secp521r1 / nistp521
                        case "1.3.132.0.35":
                            keySize = 521;
                            break;
                        default:
                            throw new PlatformNotSupportedException(
                                SR.Format(SR.Cryptography_CurveNotSupported, curve.Oid.Value));
                    }

                    // Clear the current key, because GenerateKey on the same curve makes a new key,
                    // unlike setting the KeySize property to the current value.
                    SetKey(null);
                    KeySizeValue = keySize;

                    // Generate the keys immediately, because that's what the verb of this method is.
                    GetKeys();
                }

                private static SafeSecKeyRefHandle ImportKey(ECParameters parameters)
                {
                    bool isPrivateKey = parameters.D != null;
                    byte[] blob = isPrivateKey ? parameters.ToPrivateKeyBlob() : parameters.ToSubjectPublicKeyInfo();

                    return Interop.AppleCrypto.ImportEphemeralKey(blob, isPrivateKey);
                }

                private void SetKey(SecKeyPair newKeyPair)
                {
                    SecKeyPair current = _keys;
                    _keys = newKeyPair;
                    current?.Dispose();

                    if (newKeyPair != null)
                    {
                        long size = Interop.AppleCrypto.EccGetKeySizeInBits(newKeyPair.PublicKey);

                        Debug.Assert(size == 256 || size == 384 || size == 521, $"Unknown keysize ({size})");
                        KeySizeValue = (int)size;
                    }
                }

                internal SecKeyPair GetKeys()
                {
                    SecKeyPair current = _keys;

                    if (current != null)
                    {
                        return current;
                    }

                    SafeSecKeyRefHandle publicKey;
                    SafeSecKeyRefHandle privateKey;

                    Interop.AppleCrypto.EccGenerateKey(KeySizeValue, out publicKey, out privateKey);

                    current = SecKeyPair.PublicPrivatePair(publicKey, privateKey);
                    _keys = current;
                    return current;
                }
            }
        }
#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
    }
#endif

    internal static class EcKeyBlobHelpers
    {
        private static readonly byte[] s_version1 = { 1 };
        private static readonly byte[][] s_encodedVersion1 = DerEncoder.SegmentedEncodeUnsignedInteger(s_version1);

        private static readonly Oid s_idEcPublicKey = new Oid("1.2.840.10045.2.1", null);
        private static readonly byte[][] s_encodedIdEcPublicKey = DerEncoder.SegmentedEncodeOid(s_idEcPublicKey);

        internal static void ReadPkcs8Blob(this DerSequenceReader reader, ref ECParameters parameters)
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
                // Ensure we're reading EC Public Key (well, Private, but the OID says Public)
                DerSequenceReader algorithm = reader.ReadSequence();

                string algorithmOid = algorithm.ReadOidAsString();

                if (algorithmOid != s_idEcPublicKey.Value)
                {
                    throw new CryptographicException();
                }
            }

            byte[] privateKeyBlob = reader.ReadOctetString();

            // ECPrivateKey{CURVES:IOSet} ::= SEQUENCE {
            //   version INTEGER { ecPrivkeyVer1(1) } (ecPrivkeyVer1),
            //   privateKey OCTET STRING,
            //   parameters [0] Parameters{{IOSet}} OPTIONAL,
            //   publicKey  [1] BIT STRING OPTIONAL
            // }
            DerSequenceReader keyReader = new DerSequenceReader(privateKeyBlob);
            version = keyReader.ReadInteger();

            // We understand the version 1 format
            if (version > 1)
            {
                throw new CryptographicException();
            }

            parameters.D = keyReader.ReadOctetString();

            // Check for context specific 0
            const byte ConstructedContextSpecific =
                DerSequenceReader.ContextSpecificTagFlag | DerSequenceReader.ConstructedFlag;

            const byte ConstructedContextSpecific0 = (ConstructedContextSpecific | 0);
            const byte ConstructedContextSpecific1 = (ConstructedContextSpecific | 1);

            if (keyReader.PeekTag() != ConstructedContextSpecific0)
            {
                throw new CryptographicException();
            }

            // Parameters ::= CHOICE {
            //   ecParameters ECParameters,
            //   namedCurve CURVES.&id({ CurveNames}),
            //   implicitlyCA  NULL
            // }
            DerSequenceReader parametersReader = keyReader.ReadSequence();

            if (parametersReader.PeekTag() != (int)DerSequenceReader.DerTag.ObjectIdentifier)
            {
                throw new PlatformNotSupportedException(SR.Cryptography_ECC_NamedCurvesOnly);
            }

            parameters.Curve = ECCurve.CreateFromValue(parametersReader.ReadOidAsString());

            // Check for context specific 1
            if (keyReader.PeekTag() != ConstructedContextSpecific1)
            {
                throw new CryptographicException();
            }

            keyReader = keyReader.ReadSequence();
            byte[] encodedPoint = keyReader.ReadBitString();
            ReadEncodedPoint(encodedPoint, ref parameters);

            // We don't care about the rest of the blob here, but it's expected to not exist.
        }

        internal static void ReadSubjectPublicKeyInfo(this DerSequenceReader keyInfo, ref ECParameters parameters)
        {
            // SubjectPublicKeyInfo::= SEQUENCE  {
            //    algorithm AlgorithmIdentifier,
            //    subjectPublicKey     BIT STRING  }
            DerSequenceReader algorithm = keyInfo.ReadSequence();
            string algorithmOid = algorithm.ReadOidAsString();

            // EC Public Key
            if (algorithmOid != s_idEcPublicKey.Value)
            {
                throw new CryptographicException();
            }

            if (algorithm.PeekTag() != (int)DerSequenceReader.DerTag.ObjectIdentifier)
            {
                throw new PlatformNotSupportedException(SR.Cryptography_ECC_NamedCurvesOnly);
            }

            parameters.Curve = ECCurve.CreateFromValue(algorithm.ReadOidAsString());

            byte[] encodedPoint = keyInfo.ReadBitString();
            ReadEncodedPoint(encodedPoint, ref parameters);

            // We don't care about the rest of the blob here, but it's expected to not exist.
        }

        internal static byte[] ToSubjectPublicKeyInfo(this ECParameters parameters)
        {
            parameters.Validate();

            if (!parameters.Curve.IsNamed)
            {
                throw new PlatformNotSupportedException(SR.Cryptography_ECC_NamedCurvesOnly);
            }

            byte[] pointBlob = GetPointBlob(ref parameters);

            return DerEncoder.ConstructSequence(
                DerEncoder.ConstructSegmentedSequence(
                    s_encodedIdEcPublicKey,
                    DerEncoder.SegmentedEncodeOid(parameters.Curve.Oid)),
                DerEncoder.SegmentedEncodeBitString(pointBlob));
        }

        private static byte[] GetPointBlob(ref ECParameters parameters)
        {
            byte[] pointBlob = new byte[parameters.Q.X.Length + parameters.Q.Y.Length + 1];

            // Uncompressed point
            pointBlob[0] = 0x04;
            Buffer.BlockCopy(parameters.Q.X, 0, pointBlob, 1, parameters.Q.X.Length);
            Buffer.BlockCopy(parameters.Q.Y, 0, pointBlob, 1 + parameters.Q.X.Length, parameters.Q.Y.Length);
            return pointBlob;
        }

        internal static byte[] ToPrivateKeyBlob(this ECParameters parameters)
        {
            parameters.Validate();

            if (!parameters.Curve.IsNamed)
            {
                throw new PlatformNotSupportedException(SR.Cryptography_ECC_NamedCurvesOnly);
            }

            byte[] pointBlob = GetPointBlob(ref parameters);

            // ECPrivateKey{CURVES:IOSet} ::= SEQUENCE {
            //   version INTEGER { ecPrivkeyVer1(1) } (ecPrivkeyVer1),
            //   privateKey OCTET STRING,
            //   parameters [0] Parameters{{IOSet}} OPTIONAL,
            //   publicKey  [1] BIT STRING OPTIONAL
            // }
            return DerEncoder.ConstructSequence(
                s_encodedVersion1,
                DerEncoder.SegmentedEncodeOctetString(parameters.D),
                DerEncoder.ConstructSegmentedContextSpecificValue(
                    0,
                    DerEncoder.SegmentedEncodeOid(parameters.Curve.Oid)),
                DerEncoder.ConstructSegmentedContextSpecificValue(
                    1,
                    DerEncoder.SegmentedEncodeBitString(pointBlob)));
        }

        private static void ReadEncodedPoint(byte[] encodedPoint, ref ECParameters parameters)
        {
            if (encodedPoint == null || encodedPoint.Length < 1)
            {
                throw new CryptographicException();
            }

            byte encoding = encodedPoint[0];

            switch (encoding)
            {
                // Uncompressed encoding (04 xbytes ybytes)
                case 0x04:
                // Hybrid encoding, ~yp == 0 (06 xbytes ybytes)
                case 0x06:
                // Hybrid encoding, ~yp == 1 (07 xbytes ybytes)
                case 0x07:
                    break;
                default:
                    Debug.Fail($"Don't know how to read point encoding {encoding}");
                    throw new CryptographicException();
            }

            // For formats 04, 06, and 07 the X and Y points are equal length, and they should
            // already be left-padded with zeros in cases where they're short.
            int pointEncodingSize = (encodedPoint.Length - 1) / 2;
            byte[] encodedX = new byte[pointEncodingSize];
            byte[] encodedY = new byte[pointEncodingSize];
            Buffer.BlockCopy(encodedPoint, 1, encodedX, 0, pointEncodingSize);
            Buffer.BlockCopy(encodedPoint, 1 + pointEncodingSize, encodedY, 0, pointEncodingSize);
            parameters.Q.X = encodedX;
            parameters.Q.Y = encodedY;
        }
    }
}
