// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Cryptography;
using Internal.Cryptography.Pal;

namespace System.Security.Cryptography.X509Certificates
{
    public sealed class X509SubjectKeyIdentifierExtension : X509Extension
    {
        public X509SubjectKeyIdentifierExtension()
            : base(Oids.SubjectKeyIdentifierOid)
        {
            _subjectKeyIdentifier = null;
            _decoded = true;
        }

        public X509SubjectKeyIdentifierExtension(AsnEncodedData encodedSubjectKeyIdentifier, bool critical)
            : base(Oids.SubjectKeyIdentifierOid, encodedSubjectKeyIdentifier.RawData, critical)
        {
        }

        public X509SubjectKeyIdentifierExtension(byte[] subjectKeyIdentifier, bool critical)
            : this(subjectKeyIdentifier.AsSpanParameter(nameof(subjectKeyIdentifier)), critical)
        {
        }

        public X509SubjectKeyIdentifierExtension(ReadOnlySpan<byte> subjectKeyIdentifier, bool critical)
            : base(Oids.SubjectKeyIdentifierOid, EncodeExtension(subjectKeyIdentifier), critical)
        {
        }

        public X509SubjectKeyIdentifierExtension(PublicKey key, bool critical)
            : this(key, X509SubjectKeyIdentifierHashAlgorithm.Sha1, critical)
        {
        }

        public X509SubjectKeyIdentifierExtension(PublicKey key, X509SubjectKeyIdentifierHashAlgorithm algorithm, bool critical)
            : base(Oids.SubjectKeyIdentifierOid, EncodeExtension(key, algorithm), critical)
        {
        }

        public X509SubjectKeyIdentifierExtension(string subjectKeyIdentifier, bool critical)
            : base(Oids.SubjectKeyIdentifierOid, EncodeExtension(subjectKeyIdentifier), critical)
        {
        }

        public string? SubjectKeyIdentifier
        {
            get
            {
                if (!_decoded)
                {
                    byte[] subjectKeyIdentifierValue;
                    X509Pal.Instance.DecodeX509SubjectKeyIdentifierExtension(RawData, out subjectKeyIdentifierValue);
                    _subjectKeyIdentifier = subjectKeyIdentifierValue.ToHexStringUpper();
                    _decoded = true;
                }
                return _subjectKeyIdentifier;
            }
        }

        public override void CopyFrom(AsnEncodedData asnEncodedData)
        {
            base.CopyFrom(asnEncodedData);
            _decoded = false;
        }

        private static byte[] EncodeExtension(ReadOnlySpan<byte> subjectKeyIdentifier)
        {
            if (subjectKeyIdentifier.Length == 0)
                throw new ArgumentException(SR.Arg_EmptyOrNullArray, nameof(subjectKeyIdentifier));

            return X509Pal.Instance.EncodeX509SubjectKeyIdentifierExtension(subjectKeyIdentifier);
        }

        private static byte[] EncodeExtension(string subjectKeyIdentifier)
        {
            if (subjectKeyIdentifier == null)
                throw new ArgumentNullException(nameof(subjectKeyIdentifier));

            byte[] subjectKeyIdentifiedBytes = subjectKeyIdentifier.DecodeHexString();
            return EncodeExtension(subjectKeyIdentifiedBytes);
        }

        private static byte[] EncodeExtension(PublicKey key, X509SubjectKeyIdentifierHashAlgorithm algorithm)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

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
                        byte[] sha1 = SHA1.HashData(key.EncodedKeyValue.RawData);

                        //  ShortSha1: The keyIdentifier is composed of a four bit type field with
                        //  the value 0100 followed by the least significant 60 bits of the
                        //  SHA-1 hash of the value of the BIT STRING subjectPublicKey
                        // (excluding the tag, length, and number of unused bit string bits)
                        byte[] shortSha1 = new byte[8];
                        Buffer.BlockCopy(sha1, sha1.Length - 8, shortSha1, 0, shortSha1.Length);
                        shortSha1[0] &= 0x0f;
                        shortSha1[0] |= 0x40;
                        return shortSha1;
                    }

                case X509SubjectKeyIdentifierHashAlgorithm.CapiSha1:
                    return X509Pal.Instance.ComputeCapiSha1OfPublicKey(key);

                default:
                    throw new ArgumentException(SR.Format(SR.Arg_EnumIllegalVal, algorithm), nameof(algorithm));
            }
        }

        private string? _subjectKeyIdentifier;
        private bool _decoded;
    }
}
