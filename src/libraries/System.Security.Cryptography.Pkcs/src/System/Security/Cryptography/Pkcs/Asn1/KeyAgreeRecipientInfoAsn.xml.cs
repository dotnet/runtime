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
    internal partial struct KeyAgreeRecipientInfoAsn
    {
        internal int Version;
        internal System.Security.Cryptography.Pkcs.Asn1.OriginatorIdentifierOrKeyAsn Originator;
        internal ReadOnlyMemory<byte>? Ukm;
        internal System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn KeyEncryptionAlgorithm;
        internal System.Security.Cryptography.Pkcs.Asn1.RecipientEncryptedKeyAsn[] RecipientEncryptedKeys;

        internal readonly void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }

        internal readonly void Encode(AsnWriter writer, Asn1Tag tag)
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
            try
            {
                ValueAsnReader reader = new ValueAsnReader(encoded.Span, ruleSet);

                DecodeCore(ref reader, expectedTag, encoded, out KeyAgreeRecipientInfoAsn decoded);
                reader.ThrowIfNotEmpty();
                return decoded;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static void Decode(ref ValueAsnReader reader, ReadOnlyMemory<byte> rebind, out KeyAgreeRecipientInfoAsn decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
        }

        internal static void Decode(ref ValueAsnReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out KeyAgreeRecipientInfoAsn decoded)
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

        private static void DecodeCore(ref ValueAsnReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out KeyAgreeRecipientInfoAsn decoded)
        {
            decoded = default;
            ValueAsnReader sequenceReader = reader.ReadSequence(expectedTag);
            ValueAsnReader explicitReader;
            ValueAsnReader collectionReader;
            ReadOnlySpan<byte> rebindSpan = rebind.Span;
            int offset;
            ReadOnlySpan<byte> tmpSpan;


            if (!sequenceReader.TryReadInt32(out decoded.Version))
            {
                sequenceReader.ThrowIfNotEmpty();
            }


            explicitReader = sequenceReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
            System.Security.Cryptography.Pkcs.Asn1.OriginatorIdentifierOrKeyAsn.Decode(ref explicitReader, rebind, out decoded.Originator);
            explicitReader.ThrowIfNotEmpty();


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 1)))
            {
                explicitReader = sequenceReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 1));

                if (explicitReader.TryReadPrimitiveOctetString(out tmpSpan))
                {
                    decoded.Ukm = rebindSpan.Overlaps(tmpSpan, out offset) ? rebind.Slice(offset, tmpSpan.Length) : tmpSpan.ToArray();
                }
                else
                {
                    decoded.Ukm = explicitReader.ReadOctetString();
                }

                explicitReader.ThrowIfNotEmpty();
            }

            System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(ref sequenceReader, rebind, out decoded.KeyEncryptionAlgorithm);

            // Decode SEQUENCE OF for RecipientEncryptedKeys
            {
                collectionReader = sequenceReader.ReadSequence();
                var tmpList = new List<System.Security.Cryptography.Pkcs.Asn1.RecipientEncryptedKeyAsn>();
                System.Security.Cryptography.Pkcs.Asn1.RecipientEncryptedKeyAsn tmpItem;

                while (collectionReader.HasData)
                {
                    System.Security.Cryptography.Pkcs.Asn1.RecipientEncryptedKeyAsn.Decode(ref collectionReader, rebind, out tmpItem);
                    tmpList.Add(tmpItem);
                }

                decoded.RecipientEncryptedKeys = tmpList.ToArray();
            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref partial struct ValueKeyAgreeRecipientInfoAsn
    {
        internal int Version;
        internal ReadOnlySpan<byte> Originator;
        internal ReadOnlySpan<byte> Ukm;
        internal bool HasUkm;
        internal ReadOnlySpan<byte> KeyEncryptionAlgorithm;
        internal ReadOnlySpan<byte> RecipientEncryptedKeys;

        internal static void Decode(ReadOnlySpan<byte> encoded, AsnEncodingRules ruleSet, out ValueKeyAgreeRecipientInfoAsn decoded)
        {
            Decode(Asn1Tag.Sequence, encoded, ruleSet, out decoded);
        }

        internal static void Decode(Asn1Tag expectedTag, ReadOnlySpan<byte> encoded, AsnEncodingRules ruleSet, out ValueKeyAgreeRecipientInfoAsn decoded)
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

        internal static void Decode(scoped ref ValueAsnReader reader, out ValueKeyAgreeRecipientInfoAsn decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, out decoded);
        }

        internal static void Decode(scoped ref ValueAsnReader reader, Asn1Tag expectedTag, out ValueKeyAgreeRecipientInfoAsn decoded)
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

        private static void DecodeCore(scoped ref ValueAsnReader reader, Asn1Tag expectedTag, out ValueKeyAgreeRecipientInfoAsn decoded)
        {
            decoded = default;
            ValueAsnReader sequenceReader = reader.ReadSequence(expectedTag);
            ValueAsnReader explicitReader;
            ReadOnlySpan<byte> tmpSpan;


            if (!sequenceReader.TryReadInt32(out decoded.Version))
            {
                sequenceReader.ThrowIfNotEmpty();
            }


            explicitReader = sequenceReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
            decoded.Originator = explicitReader.ReadEncodedValue();
            explicitReader.ThrowIfNotEmpty();


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 1)))
            {
                explicitReader = sequenceReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 1));

                if (explicitReader.TryReadPrimitiveOctetString(out tmpSpan))
                {
                    decoded.Ukm = tmpSpan;
                }
                else
                {
                    decoded.Ukm = explicitReader.ReadOctetString();
                }

                decoded.HasUkm = true;
                explicitReader.ThrowIfNotEmpty();
            }

            decoded.KeyEncryptionAlgorithm = sequenceReader.ReadEncodedValue();
            decoded.RecipientEncryptedKeys = sequenceReader.ReadEncodedValue();

            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
