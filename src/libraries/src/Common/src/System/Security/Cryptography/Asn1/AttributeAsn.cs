// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Asn1
{
    // https://tools.ietf.org/html/rfc5652#section-5.3
    //
    // Attribute ::= SEQUENCE {
    //   attrType OBJECT IDENTIFIER,
    //   attrValues SET OF AttributeValue }
    //
    // AttributeValue ::= ANY
    [StructLayout(LayoutKind.Sequential)]
    internal struct AttributeAsn
    {
        public Oid AttrType;

        [AnyValue]
        public ReadOnlyMemory<byte> AttrValues;

        internal void Encode(AsnWriter writer)
        {
            AsnSerializer.Serialize(this, writer);
        }

        internal static void Decode(AsnReader reader, out AttributeAsn decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            ReadOnlyMemory<byte> value = reader.GetEncodedValue();
            decoded = AsnSerializer.Deserialize<AttributeAsn>(value, reader.RuleSet);
        }
    }
}
