// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable SA1028 // ignore whitespace warnings for generated code
using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Pkcs.Asn1
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct OriginatorInfoAsn
    {
        internal System.Security.Cryptography.Asn1.Pkcs7.CertificateChoiceAsn[]? CertificateSet;
        internal ReadOnlyMemory<byte>[]? RevocationInfoChoices;

        internal readonly void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }

        internal readonly void Encode(AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);


            if (CertificateSet != null)
            {

                writer.PushSetOf(new Asn1Tag(TagClass.ContextSpecific, 0));
                for (int i = 0; i < CertificateSet.Length; i++)
                {
                    CertificateSet[i].Encode(writer);
                }
                writer.PopSetOf(new Asn1Tag(TagClass.ContextSpecific, 0));

            }


            if (RevocationInfoChoices != null)
            {

                writer.PushSetOf(new Asn1Tag(TagClass.ContextSpecific, 1));
                for (int i = 0; i < RevocationInfoChoices.Length; i++)
                {
                    try
                    {
                        writer.WriteEncodedValue(RevocationInfoChoices[i].Span);
                    }
                    catch (ArgumentException e)
                    {
                        throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
                    }
                }
                writer.PopSetOf(new Asn1Tag(TagClass.ContextSpecific, 1));

            }

            writer.PopSequence(tag);
        }

        internal static OriginatorInfoAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }

        internal static OriginatorInfoAsn Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            try
            {
                ValueAsnReader reader = new ValueAsnReader(encoded.Span, ruleSet);

                DecodeCore(ref reader, expectedTag, encoded, out OriginatorInfoAsn decoded);
                reader.ThrowIfNotEmpty();
                return decoded;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static void Decode(ref ValueAsnReader reader, ReadOnlyMemory<byte> rebind, out OriginatorInfoAsn decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
        }

        internal static void Decode(ref ValueAsnReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out OriginatorInfoAsn decoded)
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

        private static void DecodeCore(ref ValueAsnReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out OriginatorInfoAsn decoded)
        {
            decoded = default;
            ValueAsnReader sequenceReader = reader.ReadSequence(expectedTag);
            ValueAsnReader collectionReader;
            ReadOnlySpan<byte> rebindSpan = rebind.Span;
            int offset;
            ReadOnlySpan<byte> tmpSpan;


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
            {

                // Decode SEQUENCE OF for CertificateSet
                {
                    collectionReader = sequenceReader.ReadSetOf(new Asn1Tag(TagClass.ContextSpecific, 0));
                    var tmpList = new List<System.Security.Cryptography.Asn1.Pkcs7.CertificateChoiceAsn>();
                    System.Security.Cryptography.Asn1.Pkcs7.CertificateChoiceAsn tmpItem;

                    while (collectionReader.HasData)
                    {
                        System.Security.Cryptography.Asn1.Pkcs7.CertificateChoiceAsn.Decode(ref collectionReader, rebind, out tmpItem);
                        tmpList.Add(tmpItem);
                    }

                    decoded.CertificateSet = tmpList.ToArray();
                }

            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 1)))
            {

                // Decode SEQUENCE OF for RevocationInfoChoices
                {
                    collectionReader = sequenceReader.ReadSetOf(new Asn1Tag(TagClass.ContextSpecific, 1));
                    var tmpList = new List<ReadOnlyMemory<byte>>();
                    ReadOnlyMemory<byte> tmpItem;

                    while (collectionReader.HasData)
                    {
                        tmpSpan = collectionReader.ReadEncodedValue();
                        tmpItem = rebindSpan.Overlaps(tmpSpan, out offset) ? rebind.Slice(offset, tmpSpan.Length) : tmpSpan.ToArray();
                        tmpList.Add(tmpItem);
                    }

                    decoded.RevocationInfoChoices = tmpList.ToArray();
                }

            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref partial struct ValueOriginatorInfoAsn
    {
        internal ReadOnlySpan<byte> CertificateSet;
        internal bool HasCertificateSet;
        internal ReadOnlySpan<byte> RevocationInfoChoices;
        internal bool HasRevocationInfoChoices;

        internal static void Decode(ReadOnlySpan<byte> encoded, AsnEncodingRules ruleSet, out ValueOriginatorInfoAsn decoded)
        {
            Decode(Asn1Tag.Sequence, encoded, ruleSet, out decoded);
        }

        internal static void Decode(Asn1Tag expectedTag, ReadOnlySpan<byte> encoded, AsnEncodingRules ruleSet, out ValueOriginatorInfoAsn decoded)
        {
            try
            {
                ValueAsnReader reader = new ValueAsnReader(encoded, ruleSet);

                DecodeCore(ref reader, expectedTag, out decoded);
                reader.ThrowIfNotEmpty();
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static void Decode(scoped ref ValueAsnReader reader, out ValueOriginatorInfoAsn decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, out decoded);
        }

        internal static void Decode(scoped ref ValueAsnReader reader, Asn1Tag expectedTag, out ValueOriginatorInfoAsn decoded)
        {
            try
            {
                DecodeCore(ref reader, expectedTag, out decoded);
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        private static void DecodeCore(scoped ref ValueAsnReader reader, Asn1Tag expectedTag, out ValueOriginatorInfoAsn decoded)
        {
            decoded = default;
            ValueAsnReader sequenceReader = reader.ReadSequence(expectedTag);


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
            {
                decoded.CertificateSet = sequenceReader.ReadEncodedValue();
                decoded.HasCertificateSet = true;
            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 1)))
            {
                decoded.RevocationInfoChoices = sequenceReader.ReadEncodedValue();
                decoded.HasRevocationInfoChoices = true;
            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
