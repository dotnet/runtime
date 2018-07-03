// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Asn1
{
    // https://tools.ietf.org/html/rfc2898#appendix-A.2
    //
    // PBKDF2-params ::= SEQUENCE {
    //   salt CHOICE {
    //     specified OCTET STRING,
    //     otherSource AlgorithmIdentifier {{PBKDF2-SaltSources}}
    //   },
    //   iterationCount INTEGER (1..MAX),
    //   keyLength INTEGER (1..MAX) OPTIONAL,
    //   prf AlgorithmIdentifier {{PBKDF2-PRFs}} DEFAULT
    //   algid-hmacWithSHA1
    // }
    [Choice]
    [StructLayout(LayoutKind.Sequential)]
    internal struct Pbkdf2SaltChoice
    {
        [OctetString]
        public ReadOnlyMemory<byte>? Specified;

        public AlgorithmIdentifierAsn? OtherSource;
    }
}
