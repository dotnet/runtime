// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Asn1
{
    [StructLayout(LayoutKind.Sequential)]
    // https://tools.ietf.org/html/rfc3280#section-4.1.1.2
    //
    // AlgorithmIdentifier  ::=  SEQUENCE  {
    //   algorithm OBJECT IDENTIFIER,
    //   parameters ANY DEFINED BY algorithm OPTIONAL  }
    internal partial struct AlgorithmIdentifierAsn
    {
        internal static readonly ReadOnlyMemory<byte> ExplicitDerNull = new byte[] { 0x05, 0x00 };

        [ObjectIdentifier(PopulateFriendlyName = true)]
        public Oid Algorithm;

        [AnyValue, OptionalValue]
        public ReadOnlyMemory<byte>? Parameters;

        internal bool Equals(ref AlgorithmIdentifierAsn other)
        {
            if (Algorithm.Value != other.Algorithm.Value)
            {
                return false;
            }

            bool isNull = RepresentsNull(Parameters);
            bool isOtherNull = RepresentsNull(other.Parameters);

            if (isNull != isOtherNull)
            {
                return false;
            }

            if (isNull)
            {
                return true;
            }

            return Parameters.Value.Span.SequenceEqual(other.Parameters.Value.Span);
        }

        internal bool HasNullEquivalentParameters()
        {
            return RepresentsNull(Parameters);
        }

        private static bool RepresentsNull(ReadOnlyMemory<byte>? parameters)
        {
            if (parameters == null)
            {
                return true;
            }

            ReadOnlySpan<byte> span = parameters.Value.Span;

            if (span.Length != 2)
            {
                return false;
            }

            if (span[0] != 0x05)
            {
                return false;
            }

            return span[1] == 0;
        }

        internal void Encode(AsnWriter writer)
        {
            AsnSerializer.Serialize(this, writer);
        }

        internal static void Decode(AsnReader reader, out AlgorithmIdentifierAsn decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            ReadOnlyMemory<byte> value = reader.GetEncodedValue();
            decoded = AsnSerializer.Deserialize<AlgorithmIdentifierAsn>(value, reader.RuleSet);
        }

        internal static AlgorithmIdentifierAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            AsnReader reader = new AsnReader(encoded, ruleSet);

            Decode(reader, out AlgorithmIdentifierAsn decoded);
            reader.ThrowIfNotEmpty();
            return decoded;
        }
    }
}
