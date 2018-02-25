// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Security.Cryptography.Apple;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    internal sealed class EccSecurityTransforms : IDisposable
    {
        private SecKeyPair _keys;

        public void Dispose()
        {
            _keys?.Dispose();
            _keys = null;
        }

        internal int GenerateKey(ECCurve curve)
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

            GenerateKey(keySize);
            return keySize;
        }

        private SecKeyPair GenerateKey(int keySizeInBits)
        {
            SafeSecKeyRefHandle publicKey;
            SafeSecKeyRefHandle privateKey;

            Interop.AppleCrypto.EccGenerateKey(keySizeInBits, out publicKey, out privateKey);

            SecKeyPair newPair = SecKeyPair.PublicPrivatePair(publicKey, privateKey);
            SetKey(newPair);
            return newPair;
        }

        internal SecKeyPair GetOrGenerateKeys(int keySizeInBits)
        {
            SecKeyPair current = _keys;

            if (current != null)
            {
                return current;
            }

            return GenerateKey(keySizeInBits);
        }

        internal int SetKeyAndGetSize(SecKeyPair keyPair)
        {
            int size = GetKeySize(keyPair);
            SetKey(keyPair);
            return size;
        }

        private void SetKey(SecKeyPair keyPair)
        {
            SecKeyPair current = _keys;
            _keys = keyPair;
            current?.Dispose();
        }

        internal ECParameters ExportParameters(bool includePrivateParameters, int keySizeInBIts)
        {
            SecKeyPair keys = GetOrGenerateKeys(keySizeInBIts);

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

            int size = AsymmetricAlgorithmHelpers.BitsToBytes(keySizeInBIts);

            KeyBlobHelpers.PadOrTrim(ref parameters.Q.X, size);
            KeyBlobHelpers.PadOrTrim(ref parameters.Q.Y, size);

            if (includePrivateParameters)
            {
                KeyBlobHelpers.PadOrTrim(ref parameters.D, size);
            }

            return parameters;
        }

        public int ImportParameters(ECParameters parameters)
        {
            parameters.Validate();

            bool isPrivateKey = parameters.D != null;
            SecKeyPair newKeys;

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

                newKeys = SecKeyPair.PublicPrivatePair(publicKey, privateKey);
            }
            else
            {
                SafeSecKeyRefHandle publicKey = ImportKey(parameters);
                newKeys = SecKeyPair.PublicOnly(publicKey);
            }

            int size = GetKeySize(newKeys);
            SetKey(newKeys);

            return size;
        }

        private static int GetKeySize(SecKeyPair newKeys)
        {
            long size = Interop.AppleCrypto.EccGetKeySizeInBits(newKeys.PublicKey);
            Debug.Assert(size == 256 || size == 384 || size == 521, $"Unknown keysize ({size})");
            return (int)size;
        }

        private static SafeSecKeyRefHandle ImportKey(ECParameters parameters)
        {
            bool isPrivateKey = parameters.D != null;
            byte[] blob = isPrivateKey ? parameters.ToPrivateKeyBlob() : parameters.ToSubjectPublicKeyInfo();

            return Interop.AppleCrypto.ImportEphemeralKey(blob, isPrivateKey);
        }
    }

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

            try
            {
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
            finally
            {
                Array.Clear(privateKeyBlob, 0, privateKeyBlob.Length);
            }
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
