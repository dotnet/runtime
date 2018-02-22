// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.Pkcs.Asn1
{
    // https://tools.ietf.org/html/rfc5652#section-6.2.2
    //
    // OriginatorPublicKey ::= SEQUENCE {
    //   algorithm AlgorithmIdentifier,
    //   publicKey BIT STRING }
    [StructLayout(LayoutKind.Sequential)]
    internal sealed class OriginatorPublicKeyAsn
    {
        internal AlgorithmIdentifierAsn Algorithm;

        [BitString]
        internal ReadOnlyMemory<byte> PublicKey;
    }
}
