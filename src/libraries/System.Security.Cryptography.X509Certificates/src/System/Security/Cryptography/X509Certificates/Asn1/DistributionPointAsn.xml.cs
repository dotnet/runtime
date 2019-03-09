// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.X509Certificates.Asn1
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct DistributionPointAsn
    {
        internal System.Security.Cryptography.X509Certificates.Asn1.DistributionPointNameAsn? DistributionPoint;
        internal System.Security.Cryptography.X509Certificates.Asn1.ReasonFlagsAsn? Reasons;
        internal System.Security.Cryptography.Asn1.GeneralNameAsn[] CRLIssuer;
      
        internal void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }
    
        internal void Encode(AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);
            

            if (DistributionPoint.HasValue)
            {
                writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
                DistributionPoint.Value.Encode(writer);
                writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
            }


            if (Reasons.HasValue)
            {
                writer.WriteNamedBitList(new Asn1Tag(TagClass.ContextSpecific, 1), Reasons.Value);
            }


            if (CRLIssuer != null)
            {

                writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 2));
                for (int i = 0; i < CRLIssuer.Length; i++)
                {
                    CRLIssuer[i].Encode(writer); 
                }
                writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 2));

            }

            writer.PopSequence(tag);
        }

        internal static DistributionPointAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }
        
        internal static DistributionPointAsn Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            AsnReader reader = new AsnReader(encoded, ruleSet);
            
            Decode(reader, expectedTag, out DistributionPointAsn decoded);
            reader.ThrowIfNotEmpty();
            return decoded;
        }

        internal static void Decode(AsnReader reader, out DistributionPointAsn decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            Decode(reader, Asn1Tag.Sequence, out decoded);
        }

        internal static void Decode(AsnReader reader, Asn1Tag expectedTag, out DistributionPointAsn decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            decoded = default;
            AsnReader sequenceReader = reader.ReadSequence(expectedTag);
            AsnReader explicitReader;
            AsnReader collectionReader;
            

            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
            {
                explicitReader = sequenceReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
                System.Security.Cryptography.X509Certificates.Asn1.DistributionPointNameAsn tmpDistributionPoint;
                System.Security.Cryptography.X509Certificates.Asn1.DistributionPointNameAsn.Decode(explicitReader, out tmpDistributionPoint);
                decoded.DistributionPoint = tmpDistributionPoint;

                explicitReader.ThrowIfNotEmpty();
            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 1)))
            {
                decoded.Reasons = sequenceReader.ReadNamedBitListValue<System.Security.Cryptography.X509Certificates.Asn1.ReasonFlagsAsn>(new Asn1Tag(TagClass.ContextSpecific, 1));
            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 2)))
            {

                // Decode SEQUENCE OF for CRLIssuer
                {
                    collectionReader = sequenceReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 2));
                    var tmpList = new List<System.Security.Cryptography.Asn1.GeneralNameAsn>();
                    System.Security.Cryptography.Asn1.GeneralNameAsn tmpItem;

                    while (collectionReader.HasData)
                    {
                        System.Security.Cryptography.Asn1.GeneralNameAsn.Decode(collectionReader, out tmpItem); 
                        tmpList.Add(tmpItem);
                    }

                    decoded.CRLIssuer = tmpList.ToArray();
                }

            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
