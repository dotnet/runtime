// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Formats.Asn1;
using Internal.Cryptography;

namespace System.Security.Cryptography.X509Certificates
{
    public sealed class X509SubjectKeyIdentifierExtension : X509Extension
    {
        private byte[]? _subjectKeyIdentifierBytes;
        private string? _subjectKeyIdentifierString;
        private bool _decoded;

        public X509SubjectKeyIdentifierExtension()
            : base(Oids.SubjectKeyIdentifierOid)
        {
            _decoded = true;
        }

        public X509SubjectKeyIdentifierExtension(AsnEncodedData encodedSubjectKeyIdentifier, bool critical)
            : base(Oids.SubjectKeyIdentifierOid, encodedSubjectKeyIdentifier.RawData, critical)
        {
        }

        public X509SubjectKeyIdentifierExtension(byte[] subjectKeyIdentifier, bool critical)
            : this((ReadOnlySpan<byte>)(subjectKeyIdentifier ?? throw new ArgumentNullException(nameof(subjectKeyIdentifier))), critical)
        {
        }

        public X509SubjectKeyIdentifierExtension(ReadOnlySpan<byte> subjectKeyIdentifier, bool critical)
            : base(Oids.SubjectKeyIdentifierOid, EncodeExtension(subjectKeyIdentifier), critical, skipCopy: true)
        {
        }

        public X509SubjectKeyIdentifierExtension(PublicKey key, bool critical)
            : this(key, X509SubjectKeyIdentifierHashAlgorithm.Sha1, critical)
        {
        }

        public X509SubjectKeyIdentifierExtension(PublicKey key, X509SubjectKeyIdentifierHashAlgorithm algorithm, bool critical)
            : base(Oids.SubjectKeyIdentifierOid, EncodeExtension(key, algorithm), critical, skipCopy: true)
        {
        }

        public X509SubjectKeyIdentifierExtension(string subjectKeyIdentifier, bool critical)
            : base(Oids.SubjectKeyIdentifierOid, EncodeExtension(subjectKeyIdentifier), critical, skipCopy: true)
        {
        }

        public string? SubjectKeyIdentifier
        {
            get
            {
                if (!_decoded)
                {
                    Decode(RawData);
                }

                return _subjectKeyIdentifierString;
            }
        }

        /// <summary>
        ///   Gets a value whose contents represent the subject key identifier (SKI) for a certificate.
        /// </summary>
        /// <value>
        ///   The subject key identifier (SKI) for a certificate.
        /// </value>
        public ReadOnlyMemory<byte> SubjectKeyIdentifierBytes
        {
            get
            {
                // Rather than check _decoded, this property checks for a null _subjectKeyIdentifierBytes so that
                // using the default constructor, not calling CopyFrom, and then calling this property will throw
                // instead of using Nullable to talk about that degenerate state.
                if (_subjectKeyIdentifierBytes is null)
                {
                    Decode(RawData);
                }

                return _subjectKeyIdentifierBytes;
            }
        }

        public override void CopyFrom(AsnEncodedData asnEncodedData)
        {
            base.CopyFrom(asnEncodedData);
            _decoded = false;
        }

        private void Decode(byte[] rawData)
        {
            _subjectKeyIdentifierBytes = DecodeX509SubjectKeyIdentifierExtension(rawData);
            _subjectKeyIdentifierString = _subjectKeyIdentifierBytes.ToHexStringUpper();
            _decoded = true;
        }

        internal static byte[] DecodeX509SubjectKeyIdentifierExtension(byte[] encoded)
        {
            ReadOnlySpan<byte> contents;

            try
            {
                bool gotContents = AsnDecoder.TryReadPrimitiveOctetString(
                    encoded,
                    AsnEncodingRules.BER,
                    out contents,
                    out int consumed);

                if (!gotContents || consumed != encoded.Length)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }

            return contents.ToArray();
        }

        private static byte[] EncodeExtension(ReadOnlySpan<byte> subjectKeyIdentifier)
        {
            if (subjectKeyIdentifier.Length == 0)
                throw new ArgumentException(SR.Arg_EmptyOrNullArray, nameof(subjectKeyIdentifier));

            // https://tools.ietf.org/html/rfc5280#section-4.2.1.2
            //
            // subjectKeyIdentifier EXTENSION ::= {
            //     SYNTAX SubjectKeyIdentifier
            //     IDENTIFIED BY id - ce - subjectKeyIdentifier
            // }
            //
            // SubjectKeyIdentifier::= KeyIdentifier
            //
            // KeyIdentifier ::= OCTET STRING

            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
            writer.WriteOctetString(subjectKeyIdentifier);
            return writer.Encode();
        }

        private static byte[] EncodeExtension(string subjectKeyIdentifier)
        {
            ArgumentNullException.ThrowIfNull(subjectKeyIdentifier);

            byte[] subjectKeyIdentifiedBytes = subjectKeyIdentifier.LaxDecodeHexString();
            return EncodeExtension(subjectKeyIdentifiedBytes);
        }

        private static byte[] EncodeExtension(PublicKey key, X509SubjectKeyIdentifierHashAlgorithm algorithm)
        {
            ArgumentNullException.ThrowIfNull(key);

            byte[] subjectKeyIdentifier = GenerateSubjectKeyIdentifierFromPublicKey(key, algorithm);
            return EncodeExtension(subjectKeyIdentifier);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5350", Justification = "SHA1 is required by RFC3280")]
        private static byte[] GenerateSubjectKeyIdentifierFromPublicKey(PublicKey key, X509SubjectKeyIdentifierHashAlgorithm algorithm)
        {
            switch (algorithm)
            {
                case X509SubjectKeyIdentifierHashAlgorithm.Sha1:
                    return SHA1.HashData(key.EncodedKeyValue.RawData);

                case X509SubjectKeyIdentifierHashAlgorithm.ShortSha1:
                    {
                        Span<byte> sha1 = stackalloc byte[SHA1.HashSizeInBytes];
                        int written = SHA1.HashData(key.EncodedKeyValue.RawData, sha1);
                        Debug.Assert(written == SHA1.HashSizeInBytes);

                        //  ShortSha1: The keyIdentifier is composed of a four bit type field with
                        //  the value 0100 followed by the least significant 60 bits of the
                        //  SHA-1 hash of the value of the BIT STRING subjectPublicKey
                        // (excluding the tag, length, and number of unused bit string bits)
                        byte[] shortSha1 = sha1.Slice(SHA1.HashSizeInBytes - 8).ToArray();
                        shortSha1[0] &= 0x0f;
                        shortSha1[0] |= 0x40;
                        return shortSha1;
                    }

                case X509SubjectKeyIdentifierHashAlgorithm.CapiSha1:
                    // CAPI SHA1 is the SHA-1 hash over the whole SubjectPublicKeyInfo
                    return HashSubjectPublicKeyInfo(key, HashAlgorithmName.SHA1);
                default:
                    throw new ArgumentException(SR.Format(SR.Arg_EnumIllegalVal, algorithm), nameof(algorithm));
            }
        }

        private static byte[] HashSubjectPublicKeyInfo(PublicKey key, HashAlgorithmName hashAlgorithmName)
        {
            Span<byte> hash = stackalloc byte[512 / 8]; // Largest known hash is 512-bits.
            AsnWriter writer = key.EncodeSubjectPublicKeyInfo();

             // An RSA 4096 SPKI is going to be about 550 bytes. 640 for a little extra space. Anything bigger will rent.
            const int MaxSpkiStackSize = 640;
            byte[]? rented = null;
            int encodedLength = writer.GetEncodedLength();
            Span<byte> spkiBuffer = encodedLength <= MaxSpkiStackSize ?
                stackalloc byte[MaxSpkiStackSize] :
                (rented = CryptoPool.Rent(encodedLength));

            int spkiWritten = writer.Encode(spkiBuffer);
            int hashWritten = CryptographicOperations.HashData(hashAlgorithmName, spkiBuffer.Slice(0, spkiWritten), hash);

            if (rented is not null)
            {
                CryptoPool.Return(rented, clearSize: 0); // SPKI is public so no need to zero it.
            }

            return hash.Slice(0, hashWritten).ToArray();
        }
    }
}
