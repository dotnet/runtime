// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.Asn1
{
    internal partial struct AttributeAsn
    {
        public AttributeAsn(AsnEncodedData attribute)
        {
#if NET
            ArgumentNullException.ThrowIfNull(attribute);
#else
            if (attribute is null)
            {
                throw new ArgumentNullException(nameof(attribute));
            }
#endif

            AttrType = attribute.Oid!.Value!;
            AttrValues = new[] { new ReadOnlyMemory<byte>(attribute.RawData) };
        }
    }
}
