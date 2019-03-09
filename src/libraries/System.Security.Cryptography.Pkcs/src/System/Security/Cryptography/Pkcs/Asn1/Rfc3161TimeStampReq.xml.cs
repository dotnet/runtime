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
    internal partial struct Rfc3161TimeStampReq
    {
        private static byte[] s_defaultCertReq = { 0x01, 0x01, 0x00 };
  
        internal int Version;
        internal System.Security.Cryptography.Pkcs.Asn1.MessageImprint MessageImprint;
        internal Oid ReqPolicy;
        internal ReadOnlyMemory<byte>? Nonce;
        internal bool CertReq;
        internal System.Security.Cryptography.Asn1.X509ExtensionAsn[] Extensions;
      
#if DEBUG  
        static Rfc3161TimeStampReq()
        {
            Rfc3161TimeStampReq decoded = default;
            AsnReader reader;

            reader = new AsnReader(s_defaultCertReq, AsnEncodingRules.DER);
            decoded.CertReq = reader.ReadBoolean();
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
            MessageImprint.Encode(writer);

            if (ReqPolicy != null)
            {
                writer.WriteObjectIdentifier(ReqPolicy);
            }


            if (Nonce.HasValue)
            {
                writer.WriteInteger(Nonce.Value.Span);
            }

        
            // DEFAULT value handler for CertReq.
            {
                using (AsnWriter tmp = new AsnWriter(AsnEncodingRules.DER))
                {
                    tmp.WriteBoolean(CertReq);
                    ReadOnlySpan<byte> encoded = tmp.EncodeAsSpan();

                    if (!encoded.SequenceEqual(s_defaultCertReq))
                    {
                        writer.WriteEncodedValue(encoded.ToArray());
                    }
                }
            }


            if (Extensions != null)
            {

                writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
                for (int i = 0; i < Extensions.Length; i++)
                {
                    Extensions[i].Encode(writer); 
                }
                writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 0));

            }

            writer.PopSequence(tag);
        }

        internal static Rfc3161TimeStampReq Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }
        
        internal static Rfc3161TimeStampReq Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            AsnReader reader = new AsnReader(encoded, ruleSet);
            
            Decode(reader, expectedTag, out Rfc3161TimeStampReq decoded);
            reader.ThrowIfNotEmpty();
            return decoded;
        }

        internal static void Decode(AsnReader reader, out Rfc3161TimeStampReq decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            Decode(reader, Asn1Tag.Sequence, out decoded);
        }

        internal static void Decode(AsnReader reader, Asn1Tag expectedTag, out Rfc3161TimeStampReq decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            decoded = default;
            AsnReader sequenceReader = reader.ReadSequence(expectedTag);
            AsnReader defaultReader;
            AsnReader collectionReader;
            

            if (!sequenceReader.TryReadInt32(out decoded.Version))
            {
                sequenceReader.ThrowIfNotEmpty();
            }

            System.Security.Cryptography.Pkcs.Asn1.MessageImprint.Decode(sequenceReader, out decoded.MessageImprint);

            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(Asn1Tag.ObjectIdentifier))
            {
                decoded.ReqPolicy = sequenceReader.ReadObjectIdentifier();
            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(Asn1Tag.Integer))
            {
                decoded.Nonce = sequenceReader.ReadIntegerBytes();
            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(Asn1Tag.Boolean))
            {
                decoded.CertReq = sequenceReader.ReadBoolean();
            }
            else
            {
                defaultReader = new AsnReader(s_defaultCertReq, AsnEncodingRules.DER);
                decoded.CertReq = defaultReader.ReadBoolean();
            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
            {

                // Decode SEQUENCE OF for Extensions
                {
                    collectionReader = sequenceReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
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
