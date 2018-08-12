// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.Pkcs.Asn1
{
    // https://tools.ietf.org/html/rfc7292#section-4
    //
    // PFX ::= SEQUENCE {
    //   version    INTEGER {v3(3)}(v3,...),
    //   authSafe   ContentInfo,
    //   macData    MacData OPTIONAL
    // }
    [StructLayout(LayoutKind.Sequential)]
    internal struct PfxAsn
    {
        public byte Version;

        public ContentInfoAsn AuthSafe;

        [OptionalValue]
        public MacData? MacData;
    }
}
