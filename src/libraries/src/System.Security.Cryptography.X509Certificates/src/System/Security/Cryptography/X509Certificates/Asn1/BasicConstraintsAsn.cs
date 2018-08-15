// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.X509Certificates.Asn1
{
    // https://tools.ietf.org/html/rfc5280#section-4.2.1.9
    //
    // BasicConstraints ::= SEQUENCE {
    //     cA                      BOOLEAN DEFAULT FALSE,
    //     pathLenConstraint       INTEGER (0..MAX) OPTIONAL
    // }
    [StructLayout(LayoutKind.Sequential)]
    internal struct BasicConstraintsAsn
    {
        [DefaultValue(0x01, 0x01, 0x00)]
        internal bool CA;

        [OptionalValue]
        internal int? PathLengthConstraint;
    }
}
