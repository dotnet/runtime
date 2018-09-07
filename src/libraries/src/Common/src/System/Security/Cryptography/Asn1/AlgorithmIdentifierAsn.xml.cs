using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.Asn1
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct AlgorithmIdentifierAsn
    {
        internal Oid Algorithm;
        internal ReadOnlyMemory<byte>? Parameters;
      
        internal void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }
    
        internal void Encode(AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);
            
            writer.WriteObjectIdentifier(Algorithm);

            if (Parameters.HasValue)
            {
                writer.WriteEncodedValue(Parameters.Value);
            }

            writer.PopSequence(tag);
        }

        internal static AlgorithmIdentifierAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }
        
        internal static AlgorithmIdentifierAsn Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            AsnReader reader = new AsnReader(encoded, ruleSet);
            
            Decode(reader, expectedTag, out AlgorithmIdentifierAsn decoded);
            reader.ThrowIfNotEmpty();
            return decoded;
        }

        internal static void Decode(AsnReader reader, out AlgorithmIdentifierAsn decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            Decode(reader, Asn1Tag.Sequence, out decoded);
        }

        internal static void Decode(AsnReader reader, Asn1Tag expectedTag, out AlgorithmIdentifierAsn decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            decoded = default;
            AsnReader sequenceReader = reader.ReadSequence(expectedTag);
            
            decoded.Algorithm = sequenceReader.ReadObjectIdentifier();

            if (sequenceReader.HasData)
            {
                decoded.Parameters = sequenceReader.GetEncodedValue();
            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
