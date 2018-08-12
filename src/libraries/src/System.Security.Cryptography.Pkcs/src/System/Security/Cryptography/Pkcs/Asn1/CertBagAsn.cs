// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.Pkcs.Asn1
{
    // https://tools.ietf.org/html/rfc7292#section-4.2.3
    //
    // CertBag ::= SEQUENCE {
    //   certId      BAG-TYPE.&id   ({CertTypes}),
    //   certValue   [0] EXPLICIT BAG-TYPE.&Type ({CertTypes}{@certId})
    // }
    [StructLayout(LayoutKind.Sequential)]
    internal struct CertBagAsn
    {
        [ObjectIdentifier]
        public string CertId;

        [AnyValue]
        [ExpectedTag(0, ExplicitTag = true)]
        public ReadOnlyMemory<byte> CertValue;
    }
}
