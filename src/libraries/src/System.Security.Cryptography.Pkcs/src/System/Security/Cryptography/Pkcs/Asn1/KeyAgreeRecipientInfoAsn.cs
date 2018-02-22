// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.Pkcs.Asn1
{
    // https://tools.ietf.org/html/rfc5652#section-6.2.2
    //
    // KeyAgreeRecipientInfo ::= SEQUENCE {
    //   version CMSVersion,  -- always set to 3
    //   originator[0] EXPLICIT OriginatorIdentifierOrKey,
    //   ukm[1] EXPLICIT UserKeyingMaterial OPTIONAL,
    //   keyEncryptionAlgorithm KeyEncryptionAlgorithmIdentifier,
    //   recipientEncryptedKeys RecipientEncryptedKeys }
    //
    // https://tools.ietf.org/html/rfc5652#section-10.2.6
    //
    // UserKeyingMaterial ::= OCTET STRING
    [StructLayout(LayoutKind.Sequential)]
    internal sealed class KeyAgreeRecipientInfoAsn
    {
        internal int Version;

        [ExpectedTag(0, ExplicitTag = true)]
        internal OriginatorIdentifierOrKeyAsn Originator;

        [OptionalValue]
        [ExpectedTag(1, ExplicitTag = true)]
        [OctetString]
        internal ReadOnlyMemory<byte>? Ukm;

        internal AlgorithmIdentifierAsn KeyEncryptionAlgorithm;

        internal RecipientEncryptedKeyAsn[] RecipientEncryptedKeys;
    }
}
