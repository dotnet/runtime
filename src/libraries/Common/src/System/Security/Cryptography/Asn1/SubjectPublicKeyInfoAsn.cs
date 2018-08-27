// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Asn1
{
    // https://tools.ietf.org/html/rfc3280#section-4.1
    //
    // SubjectPublicKeyInfo  ::=  SEQUENCE  {
    //   algorithm            AlgorithmIdentifier,
    //   subjectPublicKey     BIT STRING  }
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct SubjectPublicKeyInfoAsn
    {
        internal AlgorithmIdentifierAsn Algorithm;

        [BitString]
        internal ReadOnlyMemory<byte> SubjectPublicKey;

        internal void Encode(AsnWriter writer)
        {
            AsnSerializer.Serialize(this, writer);
        }

        internal static void Decode(AsnReader reader, out SubjectPublicKeyInfoAsn decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            ReadOnlyMemory<byte> value = reader.GetEncodedValue();
            decoded = AsnSerializer.Deserialize<SubjectPublicKeyInfoAsn>(value, reader.RuleSet);
        }
    }
}
