// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.Pkcs.Asn1
{
    // https://tools.ietf.org/html/rfc2313#section-10.1.2
    //
    // DigestInfo ::= SEQUENCE {
    //   digestAlgorithm DigestAlgorithmIdentifier,
    //   digest Digest }
    // 
    // DigestAlgorithmIdentifier ::= AlgorithmIdentifier
    // Digest ::= OCTET STRING
    [StructLayout(LayoutKind.Sequential)]
    internal struct DigestInfoAsn
    {
        public AlgorithmIdentifierAsn DigestAlgorithm;

        [OctetString]
        public ReadOnlyMemory<byte> Digest;
    }
}
