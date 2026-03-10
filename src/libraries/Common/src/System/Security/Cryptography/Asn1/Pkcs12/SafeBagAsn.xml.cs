// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable SA1028 // ignore whitespace warnings for generated code
using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Asn1.Pkcs12
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct SafeBagAsn
    {
        internal string BagId;
        internal ReadOnlyMemory<byte> BagValue;
        internal System.Security.Cryptography.Asn1.AttributeAsn[]? BagAttributes;

        internal readonly void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }

        internal readonly void Encode(AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);

            try
            {
                writer.WriteObjectIdentifier(BagId);
            }
            catch (ArgumentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
            writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
            try
            {
                writer.WriteEncodedValue(BagValue.Span);
            }
            catch (ArgumentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
            writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 0));

            if (BagAttributes != null)
            {

                writer.PushSetOf();
                for (int i = 0; i < BagAttributes.Length; i++)
                {
                    BagAttributes[i].Encode(writer);
                }
                writer.PopSetOf();

            }

            writer.PopSequence(tag);
        }

        internal static SafeBagAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }

        internal static SafeBagAsn Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            try
            {
                ValueAsnReader reader = new ValueAsnReader(encoded.Span, ruleSet);

                DecodeCore(ref reader, expectedTag, encoded, out SafeBagAsn decoded);
                reader.ThrowIfNotEmpty();
                return decoded;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static void Decode(ref ValueAsnReader reader, ReadOnlyMemory<byte> rebind, out SafeBagAsn decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
        }

        internal static void Decode(ref ValueAsnReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out SafeBagAsn decoded)
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

        private static void DecodeCore(ref ValueAsnReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out SafeBagAsn decoded)
        {
            decoded = default;
            ValueAsnReader sequenceReader = reader.ReadSequence(expectedTag);
            ValueAsnReader explicitReader;
            ValueAsnReader collectionReader;
            ReadOnlySpan<byte> rebindSpan = rebind.Span;
            int offset;
            ReadOnlySpan<byte> tmpSpan;

            decoded.BagId = sequenceReader.ReadObjectIdentifier();

            explicitReader = sequenceReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
            tmpSpan = explicitReader.ReadEncodedValue();
            decoded.BagValue = rebindSpan.Overlaps(tmpSpan, out offset) ? rebind.Slice(offset, tmpSpan.Length) : tmpSpan.ToArray();
            explicitReader.ThrowIfNotEmpty();


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(Asn1Tag.SetOf))
            {

                // Decode SEQUENCE OF for BagAttributes
                {
                    collectionReader = sequenceReader.ReadSetOf();
                    var tmpList = new List<System.Security.Cryptography.Asn1.AttributeAsn>();
                    System.Security.Cryptography.Asn1.AttributeAsn tmpItem;

                    while (collectionReader.HasData)
                    {
                        System.Security.Cryptography.Asn1.AttributeAsn.Decode(ref collectionReader, rebind, out tmpItem);
                        tmpList.Add(tmpItem);
                    }

                    decoded.BagAttributes = tmpList.ToArray();
                }

            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref partial struct ValueSafeBagAsn
    {
        internal string BagId;
        internal ReadOnlySpan<byte> BagValue;
        internal ReadOnlySpan<byte> BagAttributes;
        internal bool HasBagAttributes;

        internal static void Decode(ReadOnlySpan<byte> encoded, AsnEncodingRules ruleSet, out ValueSafeBagAsn decoded)
        {
            Decode(Asn1Tag.Sequence, encoded, ruleSet, out decoded);
        }

        internal static void Decode(Asn1Tag expectedTag, ReadOnlySpan<byte> encoded, AsnEncodingRules ruleSet, out ValueSafeBagAsn decoded)
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

        internal static void Decode(scoped ref ValueAsnReader reader, out ValueSafeBagAsn decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, out decoded);
        }

        internal static void Decode(scoped ref ValueAsnReader reader, Asn1Tag expectedTag, out ValueSafeBagAsn decoded)
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

        private static void DecodeCore(scoped ref ValueAsnReader reader, Asn1Tag expectedTag, out ValueSafeBagAsn decoded)
        {
            decoded = default;
            ValueAsnReader sequenceReader = reader.ReadSequence(expectedTag);
            ValueAsnReader explicitReader;

            decoded.BagId = sequenceReader.ReadObjectIdentifier();

            explicitReader = sequenceReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
            decoded.BagValue = explicitReader.ReadEncodedValue();
            explicitReader.ThrowIfNotEmpty();


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(Asn1Tag.SetOf))
            {
                decoded.BagAttributes = sequenceReader.ReadEncodedValue();
                decoded.HasBagAttributes = true;
            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
