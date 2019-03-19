// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.Pkcs.Asn1
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct EnvelopedDataAsn
    {
        internal int Version;
        internal System.Security.Cryptography.Pkcs.Asn1.OriginatorInfoAsn? OriginatorInfo;
        internal System.Security.Cryptography.Pkcs.Asn1.RecipientInfoAsn[] RecipientInfos;
        internal System.Security.Cryptography.Pkcs.Asn1.EncryptedContentInfoAsn EncryptedContentInfo;
        internal System.Security.Cryptography.Asn1.AttributeAsn[] UnprotectedAttributes;
      
        internal void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }
    
        internal void Encode(AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);
            
            writer.WriteInteger(Version);

            if (OriginatorInfo.HasValue)
            {
                OriginatorInfo.Value.Encode(writer, new Asn1Tag(TagClass.ContextSpecific, 0));
            }


            writer.PushSetOf();
            for (int i = 0; i < RecipientInfos.Length; i++)
            {
                RecipientInfos[i].Encode(writer); 
            }
            writer.PopSetOf();

            EncryptedContentInfo.Encode(writer);

            if (UnprotectedAttributes != null)
            {

                writer.PushSetOf(new Asn1Tag(TagClass.ContextSpecific, 1));
                for (int i = 0; i < UnprotectedAttributes.Length; i++)
                {
                    UnprotectedAttributes[i].Encode(writer); 
                }
                writer.PopSetOf(new Asn1Tag(TagClass.ContextSpecific, 1));

            }

            writer.PopSequence(tag);
        }

        internal static EnvelopedDataAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }
        
        internal static EnvelopedDataAsn Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            AsnReader reader = new AsnReader(encoded, ruleSet);
            
            Decode(reader, expectedTag, out EnvelopedDataAsn decoded);
            reader.ThrowIfNotEmpty();
            return decoded;
        }

        internal static void Decode(AsnReader reader, out EnvelopedDataAsn decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            Decode(reader, Asn1Tag.Sequence, out decoded);
        }

        internal static void Decode(AsnReader reader, Asn1Tag expectedTag, out EnvelopedDataAsn decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            decoded = default;
            AsnReader sequenceReader = reader.ReadSequence(expectedTag);
            AsnReader collectionReader;
            

            if (!sequenceReader.TryReadInt32(out decoded.Version))
            {
                sequenceReader.ThrowIfNotEmpty();
            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
            {
                System.Security.Cryptography.Pkcs.Asn1.OriginatorInfoAsn tmpOriginatorInfo;
                System.Security.Cryptography.Pkcs.Asn1.OriginatorInfoAsn.Decode(sequenceReader, new Asn1Tag(TagClass.ContextSpecific, 0), out tmpOriginatorInfo);
                decoded.OriginatorInfo = tmpOriginatorInfo;

            }


            // Decode SEQUENCE OF for RecipientInfos
            {
                collectionReader = sequenceReader.ReadSetOf();
                var tmpList = new List<System.Security.Cryptography.Pkcs.Asn1.RecipientInfoAsn>();
                System.Security.Cryptography.Pkcs.Asn1.RecipientInfoAsn tmpItem;

                while (collectionReader.HasData)
                {
                    System.Security.Cryptography.Pkcs.Asn1.RecipientInfoAsn.Decode(collectionReader, out tmpItem); 
                    tmpList.Add(tmpItem);
                }

                decoded.RecipientInfos = tmpList.ToArray();
            }

            System.Security.Cryptography.Pkcs.Asn1.EncryptedContentInfoAsn.Decode(sequenceReader, out decoded.EncryptedContentInfo);

            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 1)))
            {

                // Decode SEQUENCE OF for UnprotectedAttributes
                {
                    collectionReader = sequenceReader.ReadSetOf(new Asn1Tag(TagClass.ContextSpecific, 1));
                    var tmpList = new List<System.Security.Cryptography.Asn1.AttributeAsn>();
                    System.Security.Cryptography.Asn1.AttributeAsn tmpItem;

                    while (collectionReader.HasData)
                    {
                        System.Security.Cryptography.Asn1.AttributeAsn.Decode(collectionReader, out tmpItem); 
                        tmpList.Add(tmpItem);
                    }

                    decoded.UnprotectedAttributes = tmpList.ToArray();
                }

            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
