// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.Pkcs.Asn1
{
    // https://tools.ietf.org/html/rfc3161#section-2.4.1
    //
    // MessageImprint::= SEQUENCE  {
    //      hashAlgorithm AlgorithmIdentifier,
    //      hashedMessage                OCTET STRING  }
    [StructLayout(LayoutKind.Sequential)]
    internal struct MessageImprint
    {
        internal AlgorithmIdentifierAsn HashAlgorithm;

        [OctetString]
        internal ReadOnlyMemory<byte> HashedMessage;
    }
}
