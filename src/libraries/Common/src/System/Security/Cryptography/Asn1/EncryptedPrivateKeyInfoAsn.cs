// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Asn1
{
    // https://tools.ietf.org/html/rfc5208#section-6
    //
    // EncryptedPrivateKeyInfo ::= SEQUENCE {
    //  encryptionAlgorithm  EncryptionAlgorithmIdentifier,
    //  encryptedData        EncryptedData }
    //
    // EncryptionAlgorithmIdentifier ::= AlgorithmIdentifier
    // EncryptedData ::= OCTET STRING
    [StructLayout(LayoutKind.Sequential)]
    internal struct EncryptedPrivateKeyInfoAsn
    {
        public AlgorithmIdentifierAsn EncryptionAlgorithm;

        [OctetString]
        public ReadOnlyMemory<byte> EncryptedData;
    }
}
