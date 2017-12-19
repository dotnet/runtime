// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.Pkcs.Asn1
{
    // https://tools.ietf.org/html/rfc5652#section-3
    //
    // ContentInfo ::= SEQUENCE {
    //   contentType ContentType,
    //   content[0] EXPLICIT ANY DEFINED BY contentType }
    //
    // ContentType::= OBJECT IDENTIFIER
    [StructLayout(LayoutKind.Sequential)]
    internal struct ContentInfoAsn
    {
        [ObjectIdentifier]
        public string ContentType;

        [ExpectedTag(0, ExplicitTag = true)]
        [AnyValue]
        public ReadOnlyMemory<byte> Content;
    }
}
