// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.X509Certificates;

namespace System.Security.Cryptography.X509Certificates.Asn1
{
    internal partial struct X501AttributeAsn
    {
        public X501AttributeAsn(X501Attribute attribute)
        {
            if (attribute == null)
            {
                throw new ArgumentNullException(nameof(attribute));
            }

            AttrId = attribute.Oid;
            AttrValues = new[] { new ReadOnlyMemory<byte>(attribute.RawData) };
        }
    }
}
