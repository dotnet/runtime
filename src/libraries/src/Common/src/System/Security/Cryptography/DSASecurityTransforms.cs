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

    public partial class DSA : AsymmetricAlgorithm
    {
        public static new DSA Create()
        {
            return new DSAImplementation.DSASecurityTransforms();
        }
#endif

        internal static partial class DSAImplementation
        {
            public sealed partial class DSASecurityTransforms : DSA
            {
                private SecKeyPair _keys;

                public DSASecurityTransforms()
                    : this(1024)
                {
                }

                public DSASecurityTransforms(int keySize)
                {
                    KeySize = keySize;
                }

                internal DSASecurityTransforms(SafeSecKeyRefHandle publicKey)
                {
                    SetKey(SecKeyPair.PublicOnly(publicKey));
                }

                internal DSASecurityTransforms(SafeSecKeyRefHandle publicKey, SafeSecKeyRefHandle privateKey)
                {
                    SetKey(SecKeyPair.PublicPrivatePair(publicKey, privateKey));
                }

                public override KeySizes[] LegalKeySizes
                {
                    get
                    {
                        return new[] { new KeySizes(minSize: 512, maxSize: 1024, skipSize: 64) };
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

                public override DSAParameters ExportParameters(bool includePrivateParameters)
                {
                    SecKeyPair keys = GetKeys();

                    if (keys.PublicKey == null ||
                        (includePrivateParameters && keys.PrivateKey == null))
                    { 
                        throw new CryptographicException(SR.Cryptography_OpenInvalidHandle);
                    }

                    DSAParameters parameters = new DSAParameters();

                    DerSequenceReader publicKeyReader =
                        Interop.AppleCrypto.SecKeyExport(keys.PublicKey, exportPrivate: false);

                    publicKeyReader.ReadSubjectPublicKeyInfo(ref parameters);

                    if (includePrivateParameters)
                    {
                        DerSequenceReader privateKeyReader =
                            Interop.AppleCrypto.SecKeyExport(keys.PrivateKey, exportPrivate: true);

                        privateKeyReader.ReadPkcs8Blob(ref parameters);
                    }

                    KeyBlobHelpers.TrimPaddingByte(ref parameters.P);
                    KeyBlobHelpers.TrimPaddingByte(ref parameters.Q);

                    KeyBlobHelpers.PadOrTrim(ref parameters.G, parameters.P.Length);
                    KeyBlobHelpers.PadOrTrim(ref parameters.Y, parameters.P.Length);

                    if (includePrivateParameters)
                    {
                        KeyBlobHelpers.PadOrTrim(ref parameters.X, parameters.Q.Length);
                    }

                    return parameters;
                }

                public override void ImportParameters(DSAParameters parameters)
                {
                    if (parameters.P == null || parameters.Q == null || parameters.G == null || parameters.Y == null)
                        throw new ArgumentException(SR.Cryptography_InvalidDsaParameters_MissingFields);

                    // J is not required and is not even used on CNG blobs.
                    // It should, however, be less than P (J == (P-1) / Q).
                    // This validation check is just to maintain parity with DSACng and DSACryptoServiceProvider,
                    // which also perform this check.
                    if (parameters.J != null && parameters.J.Length >= parameters.P.Length)
                        throw new ArgumentException(SR.Cryptography_InvalidDsaParameters_MismatchedPJ);

                    int keySize = parameters.P.Length;
                    bool hasPrivateKey = parameters.X != null;

                    if (parameters.G.Length != keySize || parameters.Y.Length != keySize)
                        throw new ArgumentException(SR.Cryptography_InvalidDsaParameters_MismatchedPGY);

                    if (hasPrivateKey && parameters.X.Length != parameters.Q.Length)
                        throw new ArgumentException(SR.Cryptography_InvalidDsaParameters_MismatchedQX);

                    if (!(8 * parameters.P.Length).IsLegalSize(LegalKeySizes))
                        throw new CryptographicException(SR.Cryptography_InvalidKeySize);

                    if (parameters.Q.Length != 20)
                        throw new CryptographicException(SR.Cryptography_InvalidDsaParameters_QRestriction_ShortKey);

                    if (hasPrivateKey)
                    {
                        SafeSecKeyRefHandle privateKey = ImportKey(parameters);

                        DSAParameters publicOnly = parameters;
                        publicOnly.X = null;

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

                private static SafeSecKeyRefHandle ImportKey(DSAParameters parameters)
                {
                    bool hasPrivateKey = parameters.X != null;
                    byte[] blob = hasPrivateKey ? parameters.ToPrivateKeyBlob() : parameters.ToSubjectPublicKeyInfo();

                    return Interop.AppleCrypto.ImportEphemeralKey(blob, hasPrivateKey);
                }

                public override byte[] CreateSignature(byte[] rgbHash)
                {
                    if (rgbHash == null)
                        throw new ArgumentNullException(nameof(rgbHash));

                    SecKeyPair keys = GetKeys();

                    if (keys.PrivateKey == null)
                    {
                        throw new CryptographicException(SR.Cryptography_CSP_NoPrivateKey);
                    }

                    byte[] derFormatSignature = Interop.AppleCrypto.GenerateSignature(keys.PrivateKey, rgbHash);

                    // Since the AppleCrypto implementation is limited to FIPS 186-2, signature field sizes
                    // are always 160 bits / 20 bytes (the size of SHA-1, and the only legal length for Q).
                    byte[] ieeeFormatSignature = AsymmetricAlgorithmHelpers.ConvertDerToIeee1363(
                        derFormatSignature,
                        0,
                        derFormatSignature.Length,
                        fieldSizeBits: 160);

                    return ieeeFormatSignature;
                }

                public override bool VerifySignature(byte[] hash, byte[] signature)
                {
                    if (hash == null)
                        throw new ArgumentNullException(nameof(hash));
                    if (signature == null)
                        throw new ArgumentNullException(nameof(signature));

                    return VerifySignature((ReadOnlySpan<byte>)hash, (ReadOnlySpan<byte>)signature);
                }

                public override bool VerifySignature(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature)
                {
                    byte[] derFormatSignature = AsymmetricAlgorithmHelpers.ConvertIeee1363ToDer(signature);

                    return Interop.AppleCrypto.VerifySignature(
                        GetKeys().PublicKey,
                        hash,
                        derFormatSignature);
                }

                protected override byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm)
                {
                    if (hashAlgorithm != HashAlgorithmName.SHA1)
                    {
                        // Matching DSACryptoServiceProvider's "I only understand SHA-1/FIPS 186-2" exception
                        throw new CryptographicException(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithm.Name);
                    }

                    return AsymmetricAlgorithmHelpers.HashData(data, offset, count, hashAlgorithm);
                }

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

                internal SecKeyPair GetKeys()
                {
                    SecKeyPair current = _keys;

                    if (current != null)
                    {
                        return current;
                    }

                    // macOS 10.11 and macOS 10.12 declare DSA invalid for key generation.
                    // Rather than write code which might or might not work, returning
                    // (OSStatus)-4 (errSecUnimplemented), just make the exception occur here.
                    //
                    // When the native code can be verified, then it can be added.
                    throw new PlatformNotSupportedException(SR.Cryptography_DSA_KeyGenNotSupported);
                }

                private void SetKey(SecKeyPair newKeyPair)
                {
                    SecKeyPair current = _keys;
                    _keys = newKeyPair;
                    current?.Dispose();

                    if (newKeyPair != null)
                    {
                        int size = Interop.AppleCrypto.GetSimpleKeySizeInBits(newKeyPair.PublicKey);
                        KeySizeValue = size;
                    }
                }
            }
        }
#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
    }
#else
    internal static class KeySizeHelpers
    {

        public static bool IsLegalSize(this int size, KeySizes[] legalSizes)
        {
            for (int i = 0; i < legalSizes.Length; i++)
            {
                KeySizes currentSizes = legalSizes[i];

                // If a cipher has only one valid key size, MinSize == MaxSize and SkipSize will be 0
                if (currentSizes.SkipSize == 0)
                {
                    if (currentSizes.MinSize == size)
                        return true;
                }
                else if (size >= currentSizes.MinSize && size <= currentSizes.MaxSize)
                {
                    // If the number is in range, check to see if it's a legal increment above MinSize
                    int delta = size - currentSizes.MinSize;

                    // While it would be unusual to see KeySizes { 10, 20, 5 } and { 11, 14, 1 }, it could happen.
                    // So don't return false just because this one doesn't match.
                    if (delta % currentSizes.SkipSize == 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
#endif

    internal static class DsaKeyBlobHelpers
    {
        private static readonly Oid s_idDsa = new Oid("1.2.840.10040.4.1");

        internal static void ReadSubjectPublicKeyInfo(this DerSequenceReader keyInfo, ref DSAParameters parameters)
        {
            // SubjectPublicKeyInfo::= SEQUENCE  {
            //    algorithm AlgorithmIdentifier,
            //    subjectPublicKey     BIT STRING  }
            DerSequenceReader algorithm = keyInfo.ReadSequence();
            string algorithmOid = algorithm.ReadOidAsString();

            // EC Public Key
            if (algorithmOid != s_idDsa.Value)
            {
                throw new CryptographicException();
            }

            // Dss-Parms ::= SEQUENCE {
            //   p INTEGER,
            //   q INTEGER,
            //   g INTEGER
            // }

            DerSequenceReader algParameters = algorithm.ReadSequence();
            byte[] publicKeyBlob = keyInfo.ReadBitString();
            // We don't care about the rest of the blob here, but it's expected to not exist.

            ReadSubjectPublicKeyInfo(algParameters, publicKeyBlob, ref parameters);
        }

        internal static void ReadSubjectPublicKeyInfo(
            this DerSequenceReader algParameters,
            byte[] publicKeyBlob,
            ref DSAParameters parameters)
        {
            parameters.P = algParameters.ReadIntegerBytes();
            parameters.Q = algParameters.ReadIntegerBytes();
            parameters.G = algParameters.ReadIntegerBytes();

            DerSequenceReader privateKeyReader = DerSequenceReader.CreateForPayload(publicKeyBlob);
            parameters.Y = privateKeyReader.ReadIntegerBytes();

            KeyBlobHelpers.TrimPaddingByte(ref parameters.P);
            KeyBlobHelpers.TrimPaddingByte(ref parameters.Q);

            KeyBlobHelpers.PadOrTrim(ref parameters.G, parameters.P.Length);
            KeyBlobHelpers.PadOrTrim(ref parameters.Y, parameters.P.Length);
        }

        internal static byte[] ToSubjectPublicKeyInfo(this DSAParameters parameters)
        {
            // SubjectPublicKeyInfo::= SEQUENCE  {
            //    algorithm AlgorithmIdentifier,
            //    subjectPublicKey     BIT STRING  }

            // Dss-Parms ::= SEQUENCE {
            //   p INTEGER,
            //   q INTEGER,
            //   g INTEGER
            // }

            return DerEncoder.ConstructSequence(
                DerEncoder.ConstructSegmentedSequence(
                    DerEncoder.SegmentedEncodeOid(s_idDsa),
                    DerEncoder.ConstructSegmentedSequence(
                        DerEncoder.SegmentedEncodeUnsignedInteger(parameters.P),
                        DerEncoder.SegmentedEncodeUnsignedInteger(parameters.Q),
                        DerEncoder.SegmentedEncodeUnsignedInteger(parameters.G)
                    )
                ),
                DerEncoder.SegmentedEncodeBitString(
                    DerEncoder.SegmentedEncodeUnsignedInteger(parameters.Y))
            );
        }

        internal static void ReadPkcs8Blob(this DerSequenceReader reader, ref DSAParameters parameters)
        {
            // Since the PKCS#8 blob for DSS/DSA does not include the public key (Y) this
            // structure is only read after filling the public half.
            Debug.Assert(parameters.P != null);
            Debug.Assert(parameters.Q != null);
            Debug.Assert(parameters.G != null);
            Debug.Assert(parameters.Y != null);

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
                // Ensure we're reading DSA, extract the parameters
                DerSequenceReader algorithm = reader.ReadSequence();

                string algorithmOid = algorithm.ReadOidAsString();

                if (algorithmOid != s_idDsa.Value)
                {
                    throw new CryptographicException();
                }

                // The Dss-Params SEQUENCE is present here, but not needed since
                // we got it from the public key already.
            }

            byte[] privateKeyBlob = reader.ReadOctetString();
            DerSequenceReader privateKeyReader = DerSequenceReader.CreateForPayload(privateKeyBlob);
            parameters.X = privateKeyReader.ReadIntegerBytes();
        }

        internal static byte[] ToPrivateKeyBlob(this DSAParameters parameters)
        {
            Debug.Assert(parameters.X != null);

            // DSAPrivateKey ::= SEQUENCE(
            //   version INTEGER,
            //   p INTEGER,
            //   q INTEGER,
            //   g INTEGER,
            //   y INTEGER,
            //   x INTEGER,
            // )

            return DerEncoder.ConstructSequence(
                DerEncoder.SegmentedEncodeUnsignedInteger(new byte[] { 0 }),
                DerEncoder.SegmentedEncodeUnsignedInteger(parameters.P),
                DerEncoder.SegmentedEncodeUnsignedInteger(parameters.Q),
                DerEncoder.SegmentedEncodeUnsignedInteger(parameters.G),
                DerEncoder.SegmentedEncodeUnsignedInteger(parameters.Y),
                DerEncoder.SegmentedEncodeUnsignedInteger(parameters.X));
        }
    }
}
