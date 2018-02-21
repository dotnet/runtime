// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.Pkcs.Asn1
{
    // https://tools.ietf.org/html/rfc5652#section-6.1
    //
    // EnvelopedData ::= SEQUENCE {
    //   version CMSVersion,
    //   originatorInfo [0] IMPLICIT OriginatorInfo OPTIONAL,
    //   recipientInfos RecipientInfos,
    //   encryptedContentInfo EncryptedContentInfo,
    //   unprotectedAttrs [1] IMPLICIT UnprotectedAttributes OPTIONAL }
    //
    // RecipientInfos ::= SET SIZE (1..MAX) OF RecipientInfo
    [StructLayout(LayoutKind.Sequential)]
    internal struct EnvelopedDataAsn
    {
        public int Version;

        [OptionalValue]
        [ExpectedTag(0)]
        public OriginatorInfoAsn OriginatorInfo;

        [SetOf]
        public RecipientInfoAsn[] RecipientInfos;

        public EncryptedContentInfoAsn EncryptedContentInfo;

        [ExpectedTag(1)]
        [SetOf]
        [OptionalValue]
        public AttributeAsn[] UnprotectedAttributes;
    }
}
