// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Formats.Asn1;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.X509Certificates.Asn1;

namespace System.Security.Cryptography.X509Certificates
{
    internal class ManagedX509ExtensionProcessor
    {
        public virtual byte[] EncodeX509KeyUsageExtension(X509KeyUsageFlags keyUsages)
        {
            // The numeric values of X509KeyUsageFlags mean that if we interpret it as a little-endian
            // ushort it will line up with the flags in the spec. We flip bit order of each byte to get
            // the KeyUsageFlagsAsn order expected by AsnWriter.
            KeyUsageFlagsAsn keyUsagesAsn =
                (KeyUsageFlagsAsn)ReverseBitOrder((byte)keyUsages) |
                (KeyUsageFlagsAsn)(ReverseBitOrder((byte)(((ushort)keyUsages >> 8))) << 8);

            // The expected output of this method isn't the SEQUENCE value, but just the payload bytes.
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
            writer.WriteNamedBitList(keyUsagesAsn);
            return writer.Encode();
        }

        public virtual void DecodeX509KeyUsageExtension(byte[] encoded, out X509KeyUsageFlags keyUsages)
        {
            KeyUsageFlagsAsn keyUsagesAsn;

            try
            {
                AsnValueReader reader = new AsnValueReader(encoded, AsnEncodingRules.BER);
                keyUsagesAsn = reader.ReadNamedBitListValue<KeyUsageFlagsAsn>();
                reader.ThrowIfNotEmpty();
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }

            // DER encodings of BIT_STRING values number the bits as
            // 01234567 89 (big endian), plus a number saying how many bits of the last byte were padding.
            //
            // So digitalSignature (0) doesn't mean 2^0 (0x01), it means the most significant bit
            // is set in this byte stream.
            //
            // BIT_STRING values are compact.  So a value of cRLSign (6) | keyEncipherment (2), which
            // is 0b0010001 => 0b0010 0010 (1 bit padding) => 0x22 encoded is therefore
            // 0x02 (length remaining) 0x01 (1 bit padding) 0x22.
            //
            // We will read that, and return 0x22.  0x22 lines up
            // exactly with X509KeyUsageFlags.CrlSign (0x20) | X509KeyUsageFlags.KeyEncipherment (0x02)
            //
            // Once the decipherOnly (8) bit is added to the mix, the values become:
            // 0b001000101 => 0b0010 0010 1000 0000 (7 bits padding)
            // { 0x03 0x07 0x22 0x80 }
            // And we read new byte[] { 0x22 0x80 }
            //
            // The value of X509KeyUsageFlags.DecipherOnly is 0x8000.  0x8000 in a little endian
            // representation is { 0x00 0x80 }.  This means that the DER storage mechanism has effectively
            // ended up being little-endian for BIT_STRING values.  Untwist the bytes, and now the bits all
            // line up with the existing X509KeyUsageFlags.

            keyUsages =
                (X509KeyUsageFlags)ReverseBitOrder((byte)keyUsagesAsn) |
                (X509KeyUsageFlags)(ReverseBitOrder((byte)(((ushort)keyUsagesAsn >> 8))) << 8);
        }

        public virtual byte[] EncodeX509BasicConstraints2Extension(
            bool certificateAuthority,
            bool hasPathLengthConstraint,
            int pathLengthConstraint)
        {
            BasicConstraintsAsn constraints = default;

            constraints.CA = certificateAuthority;
            if (hasPathLengthConstraint)
                constraints.PathLengthConstraint = pathLengthConstraint;

            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
            constraints.Encode(writer);
            return writer.Encode();
        }

        public virtual bool SupportsLegacyBasicConstraintsExtension => false;

        public virtual void DecodeX509BasicConstraintsExtension(
            byte[] encoded,
            out bool certificateAuthority,
            out bool hasPathLengthConstraint,
            out int pathLengthConstraint)
        {
            // No RFC nor ITU document describes the layout of the 2.5.29.10 structure,
            // and OpenSSL doesn't have a decoder for it, either.
            //
            // Since it was never published as a standard (2.5.29.19 replaced it before publication)
            // there shouldn't be too many people upset that we can't decode it for them on Unix.
            throw new PlatformNotSupportedException(SR.NotSupported_LegacyBasicConstraints);
        }

        public virtual void DecodeX509BasicConstraints2Extension(
                byte[] encoded,
                out bool certificateAuthority,
                out bool hasPathLengthConstraint,
                out int pathLengthConstraint)
        {
            BasicConstraintsAsn constraints = BasicConstraintsAsn.Decode(encoded, AsnEncodingRules.BER);
            certificateAuthority = constraints.CA;
            hasPathLengthConstraint = constraints.PathLengthConstraint.HasValue;
            pathLengthConstraint = constraints.PathLengthConstraint.GetValueOrDefault();
        }

        public virtual byte[] EncodeX509EnhancedKeyUsageExtension(OidCollection usages)
        {
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
                foreach (Oid usage in usages)
                {
                    writer.WriteObjectIdentifierForCrypto(usage.Value!);
                }
            }

            return writer.Encode();
        }

        public virtual void DecodeX509EnhancedKeyUsageExtension(byte[] encoded, out OidCollection usages)
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

        private static byte ReverseBitOrder(byte b)
        {
            return (byte)(unchecked(b * 0x0202020202ul & 0x010884422010ul) % 1023);
        }
    }
}
