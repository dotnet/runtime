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
    internal partial struct SignedDataAsn
    {
        internal int Version;
        internal System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn[] DigestAlgorithms;
        internal System.Security.Cryptography.Pkcs.Asn1.EncapsulatedContentInfoAsn EncapContentInfo;
        internal System.Security.Cryptography.Pkcs.Asn1.CertificateChoiceAsn[] CertificateSet;
        internal ReadOnlyMemory<byte>[] Crls;
        internal System.Security.Cryptography.Pkcs.Asn1.SignerInfoAsn[] SignerInfos;
      
        internal void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }
    
        internal void Encode(AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);
            
            writer.WriteInteger(Version);

            writer.PushSetOf();
            for (int i = 0; i < DigestAlgorithms.Length; i++)
            {
                DigestAlgorithms[i].Encode(writer); 
            }
            writer.PopSetOf();

            EncapContentInfo.Encode(writer);

            if (CertificateSet != null)
            {

                writer.PushSetOf(new Asn1Tag(TagClass.ContextSpecific, 0));
                for (int i = 0; i < CertificateSet.Length; i++)
                {
                    CertificateSet[i].Encode(writer); 
                }
                writer.PopSetOf(new Asn1Tag(TagClass.ContextSpecific, 0));

            }


            if (Crls != null)
            {

                writer.PushSetOf(new Asn1Tag(TagClass.ContextSpecific, 1));
                for (int i = 0; i < Crls.Length; i++)
                {
                    writer.WriteEncodedValue(Crls[i].Span); 
                }
                writer.PopSetOf(new Asn1Tag(TagClass.ContextSpecific, 1));

            }


            writer.PushSetOf();
            for (int i = 0; i < SignerInfos.Length; i++)
            {
                SignerInfos[i].Encode(writer); 
            }
            writer.PopSetOf();

            writer.PopSequence(tag);
        }

        internal static SignedDataAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }
        
        internal static SignedDataAsn Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            AsnReader reader = new AsnReader(encoded, ruleSet);
            
            Decode(reader, expectedTag, out SignedDataAsn decoded);
            reader.ThrowIfNotEmpty();
            return decoded;
        }

        internal static void Decode(AsnReader reader, out SignedDataAsn decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            Decode(reader, Asn1Tag.Sequence, out decoded);
        }

        internal static void Decode(AsnReader reader, Asn1Tag expectedTag, out SignedDataAsn decoded)
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


            // Decode SEQUENCE OF for DigestAlgorithms
            {
                collectionReader = sequenceReader.ReadSetOf();
                var tmpList = new List<System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn>();
                System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn tmpItem;

                while (collectionReader.HasData)
                {
                    System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(collectionReader, out tmpItem); 
                    tmpList.Add(tmpItem);
                }

                decoded.DigestAlgorithms = tmpList.ToArray();
            }

            System.Security.Cryptography.Pkcs.Asn1.EncapsulatedContentInfoAsn.Decode(sequenceReader, out decoded.EncapContentInfo);

            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
            {

                // Decode SEQUENCE OF for CertificateSet
                {
                    collectionReader = sequenceReader.ReadSetOf(new Asn1Tag(TagClass.ContextSpecific, 0));
                    var tmpList = new List<System.Security.Cryptography.Pkcs.Asn1.CertificateChoiceAsn>();
                    System.Security.Cryptography.Pkcs.Asn1.CertificateChoiceAsn tmpItem;

                    while (collectionReader.HasData)
                    {
                        System.Security.Cryptography.Pkcs.Asn1.CertificateChoiceAsn.Decode(collectionReader, out tmpItem); 
                        tmpList.Add(tmpItem);
                    }

                    decoded.CertificateSet = tmpList.ToArray();
                }

            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 1)))
            {

                // Decode SEQUENCE OF for Crls
                {
                    collectionReader = sequenceReader.ReadSetOf(new Asn1Tag(TagClass.ContextSpecific, 1));
                    var tmpList = new List<ReadOnlyMemory<byte>>();
                    ReadOnlyMemory<byte> tmpItem;

                    while (collectionReader.HasData)
                    {
                        tmpItem = collectionReader.ReadEncodedValue(); 
                        tmpList.Add(tmpItem);
                    }

                    decoded.Crls = tmpList.ToArray();
                }

            }


            // Decode SEQUENCE OF for SignerInfos
            {
                collectionReader = sequenceReader.ReadSetOf();
                var tmpList = new List<System.Security.Cryptography.Pkcs.Asn1.SignerInfoAsn>();
                System.Security.Cryptography.Pkcs.Asn1.SignerInfoAsn tmpItem;

                while (collectionReader.HasData)
                {
                    System.Security.Cryptography.Pkcs.Asn1.SignerInfoAsn.Decode(collectionReader, out tmpItem); 
                    tmpList.Add(tmpItem);
                }

                decoded.SignerInfos = tmpList.ToArray();
            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
