// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

using Internal.Cryptography;
using Internal.Cryptography.Pal;

namespace System.Security.Cryptography.X509Certificates
{
    public sealed class X509SubjectKeyIdentifierExtension : X509Extension
    {
        public X509SubjectKeyIdentifierExtension()
            : base(Oids.SubjectKeyIdentifier)
        {
            _subjectKeyIdentifier = null;
            _decoded = true;
        }

        public X509SubjectKeyIdentifierExtension(AsnEncodedData encodedSubjectKeyIdentifier, bool critical)
            : base(Oids.SubjectKeyIdentifier, encodedSubjectKeyIdentifier.RawData, critical)
        {
        }

        public X509SubjectKeyIdentifierExtension(byte[] subjectKeyIdentifier, bool critical)
            : base(Oids.SubjectKeyIdentifier, EncodeExtension(subjectKeyIdentifier), critical)
        {
        }

        public X509SubjectKeyIdentifierExtension(PublicKey key, bool critical)
            : this(key, X509SubjectKeyIdentifierHashAlgorithm.Sha1, critical)
        {
        }

        public X509SubjectKeyIdentifierExtension(PublicKey key, X509SubjectKeyIdentifierHashAlgorithm algorithm, bool critical)
            : base(Oids.SubjectKeyIdentifier, EncodeExtension(key, algorithm), critical)
        {
        }

        public X509SubjectKeyIdentifierExtension(String subjectKeyIdentifier, bool critical)
            : base(Oids.SubjectKeyIdentifier, EncodeExtension(subjectKeyIdentifier), critical)
        {
        }

        public String SubjectKeyIdentifier
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

        private static byte[] EncodeExtension(byte[] subjectKeyIdentifier)
        {
            if (subjectKeyIdentifier == null)
                throw new ArgumentNullException("subjectKeyIdentifier");
            if (subjectKeyIdentifier.Length == 0)
                throw new ArgumentException("subjectKeyIdentifier");
            return X509Pal.Instance.EncodeX509SubjectKeyIdentifierExtension(subjectKeyIdentifier);
        }

        private static byte[] EncodeExtension(String subjectKeyIdentifier)
        {
            if (subjectKeyIdentifier == null)
                throw new ArgumentNullException("subjectKeyIdentifier");

            byte[] subjectKeyIdentifiedBytes = subjectKeyIdentifier.DecodeHexString();
            return EncodeExtension(subjectKeyIdentifiedBytes);
        }

        private static byte[] EncodeExtension(PublicKey key, X509SubjectKeyIdentifierHashAlgorithm algorithm)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            byte[] subjectKeyIdentifier = GenerateSubjectKeyIdentifierFromPublicKey(key, algorithm);
            return EncodeExtension(subjectKeyIdentifier);
        }

        private static byte[] GenerateSubjectKeyIdentifierFromPublicKey(PublicKey key, X509SubjectKeyIdentifierHashAlgorithm algorithm)
        {
            switch (algorithm)
            {
                case X509SubjectKeyIdentifierHashAlgorithm.Sha1:
                    return ComputeSha1(key.EncodedKeyValue.RawData);

                case X509SubjectKeyIdentifierHashAlgorithm.ShortSha1:
                    {
                        byte[] sha1 = ComputeSha1(key.EncodedKeyValue.RawData);

                        //  ShortSha1: The keyIdentifier is composed of a four bit type field with
                        //  the value 0100 followed by the least significant 60 bits of the
                        //  SHA-1 hash of the value of the BIT STRING subjectPublicKey 
                        // (excluding the tag, length, and number of unused bit string bits)
                        byte[] shortSha1 = new byte[8];
                        Array.Copy(sha1, sha1.Length - 8, shortSha1, 0, shortSha1.Length);
                        shortSha1[0] &= 0x0f;
                        shortSha1[0] |= 0x40;
                        return shortSha1;
                    }

                case X509SubjectKeyIdentifierHashAlgorithm.CapiSha1:
                    return X509Pal.Instance.ComputeCapiSha1OfPublicKey(key);

                default:
                    throw new ArgumentException("algorithm");
            }
        }

        private static byte[] ComputeSha1(byte[] data)
        {
            return SHA1.Create().ComputeHash(data);
        }

        private String _subjectKeyIdentifier;
        private bool _decoded = false;
    }
}

