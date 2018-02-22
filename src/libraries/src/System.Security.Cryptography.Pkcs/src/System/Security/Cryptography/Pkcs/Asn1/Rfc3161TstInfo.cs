// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.Pkcs.Asn1
{
    // https://tools.ietf.org/html/rfc3161#section-2.4.2
    // 
    // TSTInfo ::= SEQUENCE  {
    //    version INTEGER { v1(1) },
    //    policy TSAPolicyId,
    //    messageImprint MessageImprint,
    //      -- MUST have the same value as the similar field in
    //      -- TimeStampReq
    //    serialNumber INTEGER,
    //      -- Time-Stamping users MUST be ready to accommodate integers
    //      -- up to 160 bits.
    //    genTime GeneralizedTime,
    //    accuracy Accuracy OPTIONAL,
    //    ordering BOOLEAN DEFAULT FALSE,
    //    nonce INTEGER                  OPTIONAL,
    //      -- MUST be present if the similar field was present
    //      -- in TimeStampReq.In that case it MUST have the same value.
    //    tsa[0] GeneralName OPTIONAL,
    //    extensions[1] IMPLICIT Extensions OPTIONAL  }
    //
    [StructLayout(LayoutKind.Sequential)]
    internal sealed class Rfc3161TstInfo
    {
        internal int Version;

        [ObjectIdentifier(PopulateFriendlyName = true)]
        internal Oid Policy;

        internal MessageImprint MessageImprint;

        [Integer]
        internal ReadOnlyMemory<byte> SerialNumber;

        // Timestamps SHOULD omit fractions "when there is no need".
        // That means that we need to still support reading and writing them.
        [GeneralizedTime(DisallowFractions = false)]
        internal DateTimeOffset GenTime;

        [OptionalValue]
        internal Rfc3161Accuracy? Accuracy;

#pragma warning disable CS3016 // Arrays as attribute arguments is not CLS-compliant
        [DefaultValue(0x01, 0x01, 0x00)]
#pragma warning restore CS3016 // Arrays as attribute arguments is not CLS-compliant
        internal bool Ordering;

        [Integer]
        [OptionalValue]
        internal ReadOnlyMemory<byte>? Nonce;

        [ExpectedTag(0, ExplicitTag = true)]
        [OptionalValue]
        internal GeneralName? Tsa;

        [ExpectedTag(1)]
        [OptionalValue]
        internal X509ExtensionAsn[] Extensions;
    }
}
