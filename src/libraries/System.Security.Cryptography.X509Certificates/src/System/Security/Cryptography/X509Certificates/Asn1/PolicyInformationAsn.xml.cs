using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.X509Certificates.Asn1
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct PolicyInformationAsn
    {
        internal string PolicyIdentifier;
        internal ReadOnlyMemory<byte>? PolicyQualifiers;
      
        internal void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }
    
        internal void Encode(AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);
            
            writer.WriteObjectIdentifier(PolicyIdentifier);

            if (PolicyQualifiers.HasValue)
            {
                writer.WriteEncodedValue(PolicyQualifiers.Value);
            }

            writer.PopSequence(tag);
        }

        internal static PolicyInformationAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }
        
        internal static PolicyInformationAsn Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            AsnReader reader = new AsnReader(encoded, ruleSet);
            
            Decode(reader, expectedTag, out PolicyInformationAsn decoded);
            reader.ThrowIfNotEmpty();
            return decoded;
        }

        internal static void Decode(AsnReader reader, out PolicyInformationAsn decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            Decode(reader, Asn1Tag.Sequence, out decoded);
        }

        internal static void Decode(AsnReader reader, Asn1Tag expectedTag, out PolicyInformationAsn decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            decoded = default;
            AsnReader sequenceReader = reader.ReadSequence(expectedTag);
            
            decoded.PolicyIdentifier = sequenceReader.ReadObjectIdentifierAsString();

            if (sequenceReader.HasData)
            {
                decoded.PolicyQualifiers = sequenceReader.GetEncodedValue();
            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
