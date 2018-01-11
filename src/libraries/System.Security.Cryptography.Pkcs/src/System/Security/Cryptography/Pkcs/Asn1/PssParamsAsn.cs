// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.Pkcs.Asn1
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
#pragma warning disable CS3016
        // SEQUENCE(id-sha1, NULL)
        [DefaultValue(0xA0, 0x09, 0x30, 0x07, 0x06, 0x05, 0x2B, 0x0E, 0x03, 0x02, 0x1A)]
        [ExpectedTag(0, ExplicitTag = true)]
        public AlgorithmIdentifierAsn HashAlgorithm;

        // SEQUENCE(id-mgf1, SEQUENCE(id-sha1, NULL))
        [DefaultValue(
            0xA1, 0x16,
            0x30, 0x14, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x08, 0x30, 0x09, 0x06,
            0x05, 0x2B, 0x0E, 0x03, 0x02, 0x1A)]
        [ExpectedTag(1, ExplicitTag = true)]
        public AlgorithmIdentifierAsn MaskGenAlgorithm;

        [DefaultValue(0xA2, 0x03, 0x02, 0x01, 0x14)]
        [ExpectedTag(2, ExplicitTag = true)]
        public int SaltLength;

        [DefaultValue(0xA3, 0x03, 0x02, 0x01, 0x01)]
        [ExpectedTag(3, ExplicitTag = true)]
        public int TrailerField;
#pragma warning restore CS3016
    }
}
