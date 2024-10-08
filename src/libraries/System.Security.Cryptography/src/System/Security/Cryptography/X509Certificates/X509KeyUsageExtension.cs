// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Formats.Asn1;
using System.Security.Cryptography.X509Certificates.Asn1;

namespace System.Security.Cryptography.X509Certificates
{
    public sealed class X509KeyUsageExtension : X509Extension
    {
        public X509KeyUsageExtension()
            : base(Oids.KeyUsageOid)
        {
            _decoded = true;
        }

        public X509KeyUsageExtension(AsnEncodedData encodedKeyUsage, bool critical)
            : base(Oids.KeyUsageOid, encodedKeyUsage.RawData, critical)
        {
        }

        public X509KeyUsageExtension(X509KeyUsageFlags keyUsages, bool critical)
            : base(Oids.KeyUsageOid, EncodeX509KeyUsageExtension(keyUsages), critical, skipCopy: true)
        {
        }

        public X509KeyUsageFlags KeyUsages
        {
            get
            {
                if (!_decoded)
                {
                    DecodeX509KeyUsageExtension(RawData, out _keyUsages);
                    _decoded = true;
                }
                return _keyUsages;
            }
        }

        public override void CopyFrom(AsnEncodedData asnEncodedData)
        {
            base.CopyFrom(asnEncodedData);
            _decoded = false;
        }

        private static byte[] EncodeX509KeyUsageExtension(X509KeyUsageFlags keyUsages)
        {
            // The numeric values of X509KeyUsageFlags mean that if we interpret it as a little-endian
            // ushort it will line up with the flags in the spec. We flip bit order of each byte to get
            // the KeyUsageFlagsAsn order expected by AsnWriter.
            KeyUsageFlagsAsn keyUsagesAsn =
                (KeyUsageFlagsAsn)ReverseBitOrder((byte)keyUsages) |
                (KeyUsageFlagsAsn)(ReverseBitOrder((byte)(((ushort)keyUsages >> 8))) << 8);

            // The expected output of this method isn't the SEQUENCE value, but just the payload bytes.
            // We expect to encode at most 9 bits in the bit list, which can encode to at most 5 octets: two
            // for the tag and definite length, two for the bit string, and one for the unused bit count.
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER, initialCapacity: 5);
            writer.WriteNamedBitList(keyUsagesAsn);
            return writer.Encode();
        }


        private static void DecodeX509KeyUsageExtension(byte[] encoded, out X509KeyUsageFlags keyUsages)
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

        private static byte ReverseBitOrder(byte b) => (byte)(unchecked(b * 0x0202020202ul & 0x010884422010ul) % 1023);

        private bool _decoded;
        private X509KeyUsageFlags _keyUsages;
    }
}
