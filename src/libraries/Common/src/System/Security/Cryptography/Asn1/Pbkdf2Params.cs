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
    //   prf AlgorithmIdentifier {{PBKDF2-PRFs}} DEFAULT algid-hmacWithSHA1
    // }
    [StructLayout(LayoutKind.Sequential)]
    internal struct Pbkdf2Params
    {
        public Pbkdf2SaltChoice Salt;

        // The spec calls out that while there's technically no limit to IterationCount,
        // that specific platforms may have their own limits.
        //
        // This defines ours to uint.MaxValue (and, conveniently, not a negative number)
        public uint IterationCount;

        // The biggest value that makes sense currently is 256/8 => 32.
        [OptionalValue]
        public byte? KeyLength;

        [DefaultValue(
            0x30, 0x0C,
            0x06, 0x08, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x02, 0x07,
            0x05, 0x00)]
        public AlgorithmIdentifierAsn Prf;
    }
}
