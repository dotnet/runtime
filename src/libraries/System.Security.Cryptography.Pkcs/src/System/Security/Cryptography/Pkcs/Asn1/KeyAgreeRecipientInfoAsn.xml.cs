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
    internal partial struct KeyAgreeRecipientInfoAsn
    {
        internal int Version;
        internal System.Security.Cryptography.Pkcs.Asn1.OriginatorIdentifierOrKeyAsn Originator;
        internal ReadOnlyMemory<byte>? Ukm;
        internal System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn KeyEncryptionAlgorithm;
        internal System.Security.Cryptography.Pkcs.Asn1.RecipientEncryptedKeyAsn[] RecipientEncryptedKeys;
      
        internal void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }
    
        internal void Encode(AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);
            
            writer.WriteInteger(Version);
            writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
            Originator.Encode(writer);
            writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 0));

            if (Ukm.HasValue)
            {
                writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 1));
                writer.WriteOctetString(Ukm.Value.Span);
                writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 1));
            }

            KeyEncryptionAlgorithm.Encode(writer);

            writer.PushSequence();
            for (int i = 0; i < RecipientEncryptedKeys.Length; i++)
            {
                RecipientEncryptedKeys[i].Encode(writer); 
            }
            writer.PopSequence();

            writer.PopSequence(tag);
        }

        internal static KeyAgreeRecipientInfoAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }
        
        internal static KeyAgreeRecipientInfoAsn Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            AsnReader reader = new AsnReader(encoded, ruleSet);
            
            Decode(reader, expectedTag, out KeyAgreeRecipientInfoAsn decoded);
            reader.ThrowIfNotEmpty();
            return decoded;
        }

        internal static void Decode(AsnReader reader, out KeyAgreeRecipientInfoAsn decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            Decode(reader, Asn1Tag.Sequence, out decoded);
        }

        internal static void Decode(AsnReader reader, Asn1Tag expectedTag, out KeyAgreeRecipientInfoAsn decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            decoded = default;
            AsnReader sequenceReader = reader.ReadSequence(expectedTag);
            AsnReader explicitReader;
            AsnReader collectionReader;
            

            if (!sequenceReader.TryReadInt32(out decoded.Version))
            {
                sequenceReader.ThrowIfNotEmpty();
            }


            explicitReader = sequenceReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
            System.Security.Cryptography.Pkcs.Asn1.OriginatorIdentifierOrKeyAsn.Decode(explicitReader, out decoded.Originator);
            explicitReader.ThrowIfNotEmpty();


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 1)))
            {
                explicitReader = sequenceReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 1));

                if (explicitReader.TryReadPrimitiveOctetStringBytes(out ReadOnlyMemory<byte> tmpUkm))
                {
                    decoded.Ukm = tmpUkm;
                }
                else
                {
                    decoded.Ukm = explicitReader.ReadOctetString();
                }

                explicitReader.ThrowIfNotEmpty();
            }

            System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(sequenceReader, out decoded.KeyEncryptionAlgorithm);

            // Decode SEQUENCE OF for RecipientEncryptedKeys
            {
                collectionReader = sequenceReader.ReadSequence();
                var tmpList = new List<System.Security.Cryptography.Pkcs.Asn1.RecipientEncryptedKeyAsn>();
                System.Security.Cryptography.Pkcs.Asn1.RecipientEncryptedKeyAsn tmpItem;

                while (collectionReader.HasData)
                {
                    System.Security.Cryptography.Pkcs.Asn1.RecipientEncryptedKeyAsn.Decode(collectionReader, out tmpItem); 
                    tmpList.Add(tmpItem);
                }

                decoded.RecipientEncryptedKeys = tmpList.ToArray();
            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
