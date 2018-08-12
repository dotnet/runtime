// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.Pkcs.Asn1
{
    // https://tools.ietf.org/html/rfc7292#section-4.2
    //
    // SafeBag ::= SEQUENCE {
    //   bagId          BAG-TYPE.&id ({PKCS12BagSet})
    //   bagValue       [0] EXPLICIT BAG-TYPE.&Type({PKCS12BagSet}{@bagId}),
    //   bagAttributes  SET OF PKCS12Attribute OPTIONAL
    // }
    [StructLayout(LayoutKind.Sequential)]
    internal struct SafeBagAsn
    {
        [ObjectIdentifier]
        public string BagId;

        [AnyValue]
        [ExpectedTag(0, ExplicitTag = true)]
        public ReadOnlyMemory<byte> BagValue;

        [OptionalValue]
        [SetOf]
        public AttributeAsn[] BagAttributes;
    }
}
