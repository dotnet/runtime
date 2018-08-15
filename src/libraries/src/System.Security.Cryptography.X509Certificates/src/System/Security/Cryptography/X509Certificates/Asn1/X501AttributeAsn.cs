// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.X509Certificates;
using Internal.Cryptography;

namespace System.Security.Cryptography.X509Certificates.Asn1
{
    // https://tools.ietf.org/html/rfc2986#section-4.1
    //
    // Attribute { ATTRIBUTE:IOSet } ::= SEQUENCE {
    //     type   ATTRIBUTE.&id({IOSet}),
    //     values SET SIZE(1..MAX) OF ATTRIBUTE.&Type({IOSet}{@type})
    // }
    [StructLayout(LayoutKind.Sequential)]
    internal struct X501AttributeAsn
    {
        [ObjectIdentifier]
        internal string AttrId;

        [SetOf]
        internal AttributeValueAsn[] AttrValues;

        public X501AttributeAsn(X501Attribute attribute, bool copyValue=true)
        {
            if (attribute == null)
            {
                throw new ArgumentNullException(nameof(attribute));
            }

            AttrId = attribute.Oid.Value;
            AttrValues = new[] { new AttributeValueAsn { AttrValue = copyValue ? attribute.RawData.CloneByteArray() : attribute.RawData } };
        }

        [Choice]
        internal struct AttributeValueAsn
        {
            [AnyValue]
            internal ReadOnlyMemory<byte>? AttrValue;
        }
    }
}
