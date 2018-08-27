using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.X509Certificates.Asn1
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct X501AttributeAsn
    {
        internal Oid AttrId;
        internal ReadOnlyMemory<byte>[] AttrValues;
      
        internal void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }
    
        internal void Encode(AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);
            
            writer.WriteObjectIdentifier(AttrId);

            writer.PushSetOf();
            for (int i = 0; i < AttrValues.Length; i++)
            {
                writer.WriteEncodedValue(AttrValues[i]); 
            }
            writer.PopSetOf();

            writer.PopSequence(tag);
        }

        internal static X501AttributeAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }
        
        internal static X501AttributeAsn Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            AsnReader reader = new AsnReader(encoded, ruleSet);
            
            Decode(reader, expectedTag, out X501AttributeAsn decoded);
            reader.ThrowIfNotEmpty();
            return decoded;
        }

        internal static void Decode(AsnReader reader, out X501AttributeAsn decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            Decode(reader, Asn1Tag.Sequence, out decoded);
        }

        internal static void Decode(AsnReader reader, Asn1Tag expectedTag, out X501AttributeAsn decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            decoded = default;
            AsnReader sequenceReader = reader.ReadSequence(expectedTag);
            AsnReader collectionReader;
            
            decoded.AttrId = sequenceReader.ReadObjectIdentifier();

            // Decode SEQUENCE OF for AttrValues
            {
                collectionReader = sequenceReader.ReadSetOf();
                var tmpList = new List<ReadOnlyMemory<byte>>();
                ReadOnlyMemory<byte> tmpItem;

                while (collectionReader.HasData)
                {
                    tmpItem = collectionReader.GetEncodedValue(); 
                    tmpList.Add(tmpItem);
                }

                decoded.AttrValues = tmpList.ToArray();
            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
