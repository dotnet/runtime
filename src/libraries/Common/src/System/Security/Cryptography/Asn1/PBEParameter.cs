// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Asn1
{
    // https://tools.ietf.org/html/rfc2898#appendix-A.3
    //
    // PBEParameter ::= SEQUENCE {
    //   salt OCTET STRING (SIZE(8)),
    //   iterationCount INTEGER }
    //
    // The version from PKCS#12 (pkcs-12PbeParams, https://tools.ietf.org/html/rfc7292#appendix-C)
    // is the same, without limiting the size of the salt value.
    [StructLayout(LayoutKind.Sequential)]
    internal struct PBEParameter
    {
        [OctetString]
        public ReadOnlyMemory<byte> Salt;

        // The spec calls out that while there's technically no limit to IterationCount,
        // that specific platforms may have their own limits.
        //
        // This defines ours to uint.MaxValue (and, conveniently, not a negative number)
        public uint IterationCount;
    }
}
