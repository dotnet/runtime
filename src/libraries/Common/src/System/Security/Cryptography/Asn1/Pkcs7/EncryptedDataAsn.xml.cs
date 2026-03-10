// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable SA1028 // ignore whitespace warnings for generated code
using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Asn1.Pkcs7
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct EncryptedDataAsn
    {
        internal int Version;
        internal System.Security.Cryptography.Asn1.Pkcs7.EncryptedContentInfoAsn EncryptedContentInfo;
        internal System.Security.Cryptography.Asn1.AttributeAsn[]? UnprotectedAttributes;

        internal readonly void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }

        internal readonly void Encode(AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);

            writer.WriteInteger(Version);
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

        internal static EncryptedDataAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }

        internal static EncryptedDataAsn Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            try
            {
                ValueAsnReader reader = new ValueAsnReader(encoded.Span, ruleSet);

                DecodeCore(ref reader, expectedTag, encoded, out EncryptedDataAsn decoded);
                reader.ThrowIfNotEmpty();
                return decoded;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static void Decode(ref ValueAsnReader reader, ReadOnlyMemory<byte> rebind, out EncryptedDataAsn decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
        }

        internal static void Decode(ref ValueAsnReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out EncryptedDataAsn decoded)
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

        private static void DecodeCore(ref ValueAsnReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out EncryptedDataAsn decoded)
        {
            decoded = default;
            ValueAsnReader sequenceReader = reader.ReadSequence(expectedTag);
            ValueAsnReader collectionReader;


            if (!sequenceReader.TryReadInt32(out decoded.Version))
            {
                sequenceReader.ThrowIfNotEmpty();
            }

            System.Security.Cryptography.Asn1.Pkcs7.EncryptedContentInfoAsn.Decode(ref sequenceReader, rebind, out decoded.EncryptedContentInfo);

            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 1)))
            {

                // Decode SEQUENCE OF for UnprotectedAttributes
                {
                    collectionReader = sequenceReader.ReadSetOf(new Asn1Tag(TagClass.ContextSpecific, 1));
                    var tmpList = new List<System.Security.Cryptography.Asn1.AttributeAsn>();
                    System.Security.Cryptography.Asn1.AttributeAsn tmpItem;

                    while (collectionReader.HasData)
                    {
                        System.Security.Cryptography.Asn1.AttributeAsn.Decode(ref collectionReader, rebind, out tmpItem);
                        tmpList.Add(tmpItem);
                    }

                    decoded.UnprotectedAttributes = tmpList.ToArray();
                }

            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref partial struct ValueEncryptedDataAsn
    {
        internal int Version;
        internal ReadOnlySpan<byte> EncryptedContentInfo;
        internal ReadOnlySpan<byte> UnprotectedAttributes;
        internal bool HasUnprotectedAttributes;

        internal static void Decode(ReadOnlySpan<byte> encoded, AsnEncodingRules ruleSet, out ValueEncryptedDataAsn decoded)
        {
            Decode(Asn1Tag.Sequence, encoded, ruleSet, out decoded);
        }

        internal static void Decode(Asn1Tag expectedTag, ReadOnlySpan<byte> encoded, AsnEncodingRules ruleSet, out ValueEncryptedDataAsn decoded)
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

        internal static void Decode(scoped ref ValueAsnReader reader, out ValueEncryptedDataAsn decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, out decoded);
        }

        internal static void Decode(scoped ref ValueAsnReader reader, Asn1Tag expectedTag, out ValueEncryptedDataAsn decoded)
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

        private static void DecodeCore(scoped ref ValueAsnReader reader, Asn1Tag expectedTag, out ValueEncryptedDataAsn decoded)
        {
            decoded = default;
            ValueAsnReader sequenceReader = reader.ReadSequence(expectedTag);


            if (!sequenceReader.TryReadInt32(out decoded.Version))
            {
                sequenceReader.ThrowIfNotEmpty();
            }

            decoded.EncryptedContentInfo = sequenceReader.ReadEncodedValue();

            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 1)))
            {
                decoded.UnprotectedAttributes = sequenceReader.ReadEncodedValue();
                decoded.HasUnprotectedAttributes = true;
            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
