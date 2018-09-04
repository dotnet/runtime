// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Asn1
{
    // https://tools.ietf.org/html/rfc8017#appendix-A.2.3
    //
    // RSASSA-PSS-params ::= SEQUENCE {
    //   hashAlgorithm[0] HashAlgorithm      DEFAULT sha1,
    //   maskGenAlgorithm[1] MaskGenAlgorithm DEFAULT mgf1SHA1,
    //   saltLength[2] INTEGER            DEFAULT 20,
    //   trailerField[3] TrailerField       DEFAULT trailerFieldBC }
    //
    [StructLayout(LayoutKind.Sequential)]
    internal struct PssParamsAsn
    {
        // SEQUENCE(id-sha1, NULL)
        [DefaultValue(0x30, 0x09, 0x06, 0x05, 0x2B, 0x0E, 0x03, 0x02, 0x1A, 0x05, 0x00)]
        [ExpectedTag(0, ExplicitTag = true)]
        public AlgorithmIdentifierAsn HashAlgorithm;

        // SEQUENCE(id-mgf1, SEQUENCE(id-sha1, NULL))
        [DefaultValue(
            0x30, 0x16, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x08, 0x30, 0x09, 0x06,
            0x05, 0x2B, 0x0E, 0x03, 0x02, 0x1A, 0x05, 0x00)]
        [ExpectedTag(1, ExplicitTag = true)]
        public AlgorithmIdentifierAsn MaskGenAlgorithm;

        [DefaultValue(0x02, 0x01, 0x14)]
        [ExpectedTag(2, ExplicitTag = true)]
        public uint SaltLength;

        [DefaultValue(0x02, 0x01, 0x01)]
        [ExpectedTag(3, ExplicitTag = true)]
        public uint TrailerField;

        internal void Encode(AsnWriter writer)
        {
            AsnSerializer.Serialize(this, writer);
        }

        internal static void Decode(AsnReader reader, out PssParamsAsn decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            ReadOnlyMemory<byte> value = reader.GetEncodedValue();
            decoded = AsnSerializer.Deserialize<PssParamsAsn>(value, reader.RuleSet);
        }

        internal static PssParamsAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            AsnReader reader = new AsnReader(encoded, ruleSet);

            Decode(reader, out PssParamsAsn decoded);
            reader.ThrowIfNotEmpty();
            return decoded;
        }
    }
}
