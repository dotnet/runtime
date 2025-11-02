// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Formats.Asn1;

namespace System.Security.Cryptography.X509Certificates
{
    public sealed class X509EnhancedKeyUsageExtension : X509Extension
    {
        public X509EnhancedKeyUsageExtension()
            : base(Oids.EnhancedKeyUsageOid)
        {
            _enhancedKeyUsages = new OidCollection();
            _decoded = true;
        }

        public X509EnhancedKeyUsageExtension(AsnEncodedData encodedEnhancedKeyUsages, bool critical)
            : base(Oids.EnhancedKeyUsageOid, encodedEnhancedKeyUsages.RawData, critical)
        {
        }

        public X509EnhancedKeyUsageExtension(OidCollection enhancedKeyUsages, bool critical)
            : base(Oids.EnhancedKeyUsageOid, EncodeExtension(enhancedKeyUsages), critical, skipCopy: true)
        {
        }

        public OidCollection EnhancedKeyUsages
        {
            get
            {
                if (!_decoded)
                {
                    DecodeX509EnhancedKeyUsageExtension(RawData, out _enhancedKeyUsages);
                    _decoded = true;
                }

                OidCollection oids = new OidCollection(_enhancedKeyUsages!.Count);

                foreach (Oid oid in _enhancedKeyUsages)
                {
                    oids.Add(oid);
                }

                return oids;
            }
        }

        public override void CopyFrom(AsnEncodedData asnEncodedData)
        {
            base.CopyFrom(asnEncodedData);
            _decoded = false;
        }

        private static byte[] EncodeExtension(OidCollection enhancedKeyUsages)
        {
            ArgumentNullException.ThrowIfNull(enhancedKeyUsages);

            // https://tools.ietf.org/html/rfc5280#section-4.2.1.12
            //
            // extKeyUsage EXTENSION ::= {
            //     SYNTAX SEQUENCE SIZE(1..MAX) OF KeyPurposeId
            //     IDENTIFIED BY id-ce-extKeyUsage
            // }
            //
            // KeyPurposeId ::= OBJECT IDENTIFIER

            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);

            using (writer.PushSequence())
            {
                foreach (Oid usage in enhancedKeyUsages)
                {
                    writer.WriteObjectIdentifierForCrypto(usage.Value!);
                }
            }

            return writer.Encode();
        }

        private static void DecodeX509EnhancedKeyUsageExtension(byte[] encoded, out OidCollection usages)
        {
            // https://tools.ietf.org/html/rfc5924#section-4.1
            //
            // ExtKeyUsageSyntax ::= SEQUENCE SIZE (1..MAX) OF KeyPurposeId
            //
            // KeyPurposeId ::= OBJECT IDENTIFIER

            try
            {
                AsnReader reader = new AsnReader(encoded, AsnEncodingRules.BER);
                AsnReader sequenceReader = reader.ReadSequence();
                reader.ThrowIfNotEmpty();
                usages = new OidCollection();

                while (sequenceReader.HasData)
                {
                    usages.Add(new Oid(sequenceReader.ReadObjectIdentifier(), null));
                }
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        private OidCollection? _enhancedKeyUsages;
        private bool _decoded;
    }
}
