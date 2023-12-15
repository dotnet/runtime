// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable SA1028 // ignore whitespace warnings for generated code
using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography.X509Certificates.Asn1
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct DistributionPointAsn
    {
        internal System.Security.Cryptography.X509Certificates.Asn1.DistributionPointNameAsn? DistributionPoint;
        internal System.Security.Cryptography.X509Certificates.Asn1.ReasonFlagsAsn? Reasons;
        internal System.Security.Cryptography.Asn1.GeneralNameAsn[]? CRLIssuer;

        internal readonly void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }

        internal readonly void Encode(AsnWriter writer, Asn1Tag tag)
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
                writer.WriteNamedBitList(Reasons.Value, new Asn1Tag(TagClass.ContextSpecific, 1));
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
            try
            {
                AsnValueReader reader = new AsnValueReader(encoded.Span, ruleSet);

                DecodeCore(ref reader, expectedTag, encoded, out DistributionPointAsn decoded);
                reader.ThrowIfNotEmpty();
                return decoded;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static void Decode(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out DistributionPointAsn decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
        }

        internal static void Decode(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out DistributionPointAsn decoded)
        {
            try
            {
                DecodeCore(ref reader, expectedTag, rebind, out decoded);
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        private static void DecodeCore(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out DistributionPointAsn decoded)
        {
            decoded = default;
            AsnValueReader sequenceReader = reader.ReadSequence(expectedTag);
            AsnValueReader explicitReader;
            AsnValueReader collectionReader;


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
            {
                explicitReader = sequenceReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
                System.Security.Cryptography.X509Certificates.Asn1.DistributionPointNameAsn tmpDistributionPoint;
                System.Security.Cryptography.X509Certificates.Asn1.DistributionPointNameAsn.Decode(ref explicitReader, rebind, out tmpDistributionPoint);
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
                        System.Security.Cryptography.Asn1.GeneralNameAsn.Decode(ref collectionReader, rebind, out tmpItem);
                        tmpList.Add(tmpItem);
                    }

                    decoded.CRLIssuer = tmpList.ToArray();
                }

            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
