// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.Pkcs.Asn1
{
    // https://tools.ietf.org/html/rfc5652#section-10.2.7
    //
    // OtherKeyAttribute ::= SEQUENCE {
    //   keyAttrId OBJECT IDENTIFIER,
    //   keyAttr ANY DEFINED BY keyAttrId OPTIONAL }
    [StructLayout(LayoutKind.Sequential)]
    internal struct OtherKeyAttributeAsn
    {
        [ObjectIdentifier]
        internal string KeyAttrId;

        [OptionalValue]
        [AnyValue]
        internal ReadOnlyMemory<byte>? KeyAttr;
    }
}
