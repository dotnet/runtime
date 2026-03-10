// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable SA1028 // ignore whitespace warnings for generated code
using System;
using System.Formats.Asn1;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Asn1.Pkcs7
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct EncryptedContentInfoAsn
    {
        internal string ContentType;
        internal System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn ContentEncryptionAlgorithm;
        internal ReadOnlyMemory<byte>? EncryptedContent;

        internal readonly void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }

        internal readonly void Encode(AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);

            try
            {
                writer.WriteObjectIdentifier(ContentType);
            }
            catch (ArgumentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
            ContentEncryptionAlgorithm.Encode(writer);

            if (EncryptedContent.HasValue)
            {
                writer.WriteOctetString(EncryptedContent.Value.Span, new Asn1Tag(TagClass.ContextSpecific, 0));
            }

            writer.PopSequence(tag);
        }

        internal static EncryptedContentInfoAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }

        internal static EncryptedContentInfoAsn Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            try
            {
                ValueAsnReader reader = new ValueAsnReader(encoded.Span, ruleSet);

                DecodeCore(ref reader, expectedTag, encoded, out EncryptedContentInfoAsn decoded);
                reader.ThrowIfNotEmpty();
                return decoded;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static void Decode(ref ValueAsnReader reader, ReadOnlyMemory<byte> rebind, out EncryptedContentInfoAsn decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
        }

        internal static void Decode(ref ValueAsnReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out EncryptedContentInfoAsn decoded)
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

        private static void DecodeCore(ref ValueAsnReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out EncryptedContentInfoAsn decoded)
        {
            decoded = default;
            ValueAsnReader sequenceReader = reader.ReadSequence(expectedTag);
            ReadOnlySpan<byte> rebindSpan = rebind.Span;
            int offset;
            ReadOnlySpan<byte> tmpSpan;

            decoded.ContentType = sequenceReader.ReadObjectIdentifier();
            System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(ref sequenceReader, rebind, out decoded.ContentEncryptionAlgorithm);

            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
            {

                if (sequenceReader.TryReadPrimitiveOctetString(out tmpSpan, new Asn1Tag(TagClass.ContextSpecific, 0)))
                {
                    decoded.EncryptedContent = rebindSpan.Overlaps(tmpSpan, out offset) ? rebind.Slice(offset, tmpSpan.Length) : tmpSpan.ToArray();
                }
                else
                {
                    decoded.EncryptedContent = sequenceReader.ReadOctetString(new Asn1Tag(TagClass.ContextSpecific, 0));
                }

            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref partial struct ValueEncryptedContentInfoAsn
    {
        internal string ContentType;
        internal ReadOnlySpan<byte> ContentEncryptionAlgorithm;
        internal ReadOnlySpan<byte> EncryptedContent;
        internal bool HasEncryptedContent;

        internal static void Decode(ReadOnlySpan<byte> encoded, AsnEncodingRules ruleSet, out ValueEncryptedContentInfoAsn decoded)
        {
            Decode(Asn1Tag.Sequence, encoded, ruleSet, out decoded);
        }

        internal static void Decode(Asn1Tag expectedTag, ReadOnlySpan<byte> encoded, AsnEncodingRules ruleSet, out ValueEncryptedContentInfoAsn decoded)
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

        internal static void Decode(scoped ref ValueAsnReader reader, out ValueEncryptedContentInfoAsn decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, out decoded);
        }

        internal static void Decode(scoped ref ValueAsnReader reader, Asn1Tag expectedTag, out ValueEncryptedContentInfoAsn decoded)
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

        private static void DecodeCore(scoped ref ValueAsnReader reader, Asn1Tag expectedTag, out ValueEncryptedContentInfoAsn decoded)
        {
            decoded = default;
            ValueAsnReader sequenceReader = reader.ReadSequence(expectedTag);
            ReadOnlySpan<byte> tmpSpan;

            decoded.ContentType = sequenceReader.ReadObjectIdentifier();
            decoded.ContentEncryptionAlgorithm = sequenceReader.ReadEncodedValue();

            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
            {

                if (sequenceReader.TryReadPrimitiveOctetString(out tmpSpan, new Asn1Tag(TagClass.ContextSpecific, 0)))
                {
                    decoded.EncryptedContent = tmpSpan;
                }
                else
                {
                    decoded.EncryptedContent = sequenceReader.ReadOctetString(new Asn1Tag(TagClass.ContextSpecific, 0));
                }

                decoded.HasEncryptedContent = true;
            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
