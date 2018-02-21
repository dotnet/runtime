// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.Pkcs.Asn1
{
    // https://tools.ietf.org/html/rfc5652#section-6.1
    //
    // EncryptedContentInfo ::= SEQUENCE {
    //   contentType ContentType,
    //   contentEncryptionAlgorithm ContentEncryptionAlgorithmIdentifier,
    //   encryptedContent[0] IMPLICIT EncryptedContent OPTIONAL }
    //
    // https://tools.ietf.org/html/rfc5652#section-11.1
    //
    // ContentType ::= OBJECT IDENTIFIER
    [StructLayout(LayoutKind.Sequential)]
    internal struct EncryptedContentInfoAsn
    {
        [ObjectIdentifier]
        internal string ContentType;

        internal AlgorithmIdentifierAsn ContentEncryptionAlgorithm;

        [OptionalValue]
        [OctetString]
        [ExpectedTag(0)]
        internal ReadOnlyMemory<byte>? EncryptedContent;
    }
}
