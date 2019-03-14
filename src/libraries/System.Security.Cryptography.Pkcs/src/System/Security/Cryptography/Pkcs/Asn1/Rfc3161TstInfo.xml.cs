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
    internal partial struct Rfc3161TstInfo
    {
        private static byte[] s_defaultOrdering = { 0x01, 0x01, 0x00 };
  
        internal int Version;
        internal Oid Policy;
        internal System.Security.Cryptography.Pkcs.Asn1.MessageImprint MessageImprint;
        internal ReadOnlyMemory<byte> SerialNumber;
        internal DateTimeOffset GenTime;
        internal System.Security.Cryptography.Pkcs.Asn1.Rfc3161Accuracy? Accuracy;
        internal bool Ordering;
        internal ReadOnlyMemory<byte>? Nonce;
        internal System.Security.Cryptography.Asn1.GeneralNameAsn? Tsa;
        internal System.Security.Cryptography.Asn1.X509ExtensionAsn[] Extensions;
      
#if DEBUG  
        static Rfc3161TstInfo()
        {
            Rfc3161TstInfo decoded = default;
            AsnReader reader;

            reader = new AsnReader(s_defaultOrdering, AsnEncodingRules.DER);
            decoded.Ordering = reader.ReadBoolean();
            reader.ThrowIfNotEmpty();
        }
#endif
 
        internal void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }
    
        internal void Encode(AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);
            
            writer.WriteInteger(Version);
            writer.WriteObjectIdentifier(Policy);
            MessageImprint.Encode(writer);
            writer.WriteInteger(SerialNumber.Span);
            writer.WriteGeneralizedTime(GenTime);

            if (Accuracy.HasValue)
            {
                Accuracy.Value.Encode(writer);
            }

        
            // DEFAULT value handler for Ordering.
            {
                using (AsnWriter tmp = new AsnWriter(AsnEncodingRules.DER))
                {
                    tmp.WriteBoolean(Ordering);
                    ReadOnlySpan<byte> encoded = tmp.EncodeAsSpan();

                    if (!encoded.SequenceEqual(s_defaultOrdering))
                    {
                        writer.WriteEncodedValue(encoded.ToArray());
                    }
                }
            }


            if (Nonce.HasValue)
            {
                writer.WriteInteger(Nonce.Value.Span);
            }


            if (Tsa.HasValue)
            {
                writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
                Tsa.Value.Encode(writer);
                writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
            }


            if (Extensions != null)
            {

                writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 1));
                for (int i = 0; i < Extensions.Length; i++)
                {
                    Extensions[i].Encode(writer); 
                }
                writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 1));

            }

            writer.PopSequence(tag);
        }

        internal static Rfc3161TstInfo Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }
        
        internal static Rfc3161TstInfo Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            AsnReader reader = new AsnReader(encoded, ruleSet);
            
            Decode(reader, expectedTag, out Rfc3161TstInfo decoded);
            reader.ThrowIfNotEmpty();
            return decoded;
        }

        internal static void Decode(AsnReader reader, out Rfc3161TstInfo decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            Decode(reader, Asn1Tag.Sequence, out decoded);
        }

        internal static void Decode(AsnReader reader, Asn1Tag expectedTag, out Rfc3161TstInfo decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            decoded = default;
            AsnReader sequenceReader = reader.ReadSequence(expectedTag);
            AsnReader explicitReader;
            AsnReader defaultReader;
            AsnReader collectionReader;
            

            if (!sequenceReader.TryReadInt32(out decoded.Version))
            {
                sequenceReader.ThrowIfNotEmpty();
            }

            decoded.Policy = sequenceReader.ReadObjectIdentifier();
            System.Security.Cryptography.Pkcs.Asn1.MessageImprint.Decode(sequenceReader, out decoded.MessageImprint);
            decoded.SerialNumber = sequenceReader.ReadIntegerBytes();
            decoded.GenTime = sequenceReader.ReadGeneralizedTime();

            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(Asn1Tag.Sequence))
            {
                System.Security.Cryptography.Pkcs.Asn1.Rfc3161Accuracy tmpAccuracy;
                System.Security.Cryptography.Pkcs.Asn1.Rfc3161Accuracy.Decode(sequenceReader, out tmpAccuracy);
                decoded.Accuracy = tmpAccuracy;

            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(Asn1Tag.Boolean))
            {
                decoded.Ordering = sequenceReader.ReadBoolean();
            }
            else
            {
                defaultReader = new AsnReader(s_defaultOrdering, AsnEncodingRules.DER);
                decoded.Ordering = defaultReader.ReadBoolean();
            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(Asn1Tag.Integer))
            {
                decoded.Nonce = sequenceReader.ReadIntegerBytes();
            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
            {
                explicitReader = sequenceReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
                System.Security.Cryptography.Asn1.GeneralNameAsn tmpTsa;
                System.Security.Cryptography.Asn1.GeneralNameAsn.Decode(explicitReader, out tmpTsa);
                decoded.Tsa = tmpTsa;

                explicitReader.ThrowIfNotEmpty();
            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 1)))
            {

                // Decode SEQUENCE OF for Extensions
                {
                    collectionReader = sequenceReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 1));
                    var tmpList = new List<System.Security.Cryptography.Asn1.X509ExtensionAsn>();
                    System.Security.Cryptography.Asn1.X509ExtensionAsn tmpItem;

                    while (collectionReader.HasData)
                    {
                        System.Security.Cryptography.Asn1.X509ExtensionAsn.Decode(collectionReader, out tmpItem); 
                        tmpList.Add(tmpItem);
                    }

                    decoded.Extensions = tmpList.ToArray();
                }

            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
