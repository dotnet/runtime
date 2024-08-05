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
                AsnValueReader reader = new AsnValueReader(encoded.Span, ruleSet);

                DecodeCore(ref reader, expectedTag, encoded, out KeyAgreeRecipientInfoAsn decoded);
                reader.ThrowIfNotEmpty();
                return decoded;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static void Decode(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out KeyAgreeRecipientInfoAsn decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
        }

        internal static void Decode(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out KeyAgreeRecipientInfoAsn decoded)
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

        private static void DecodeCore(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out KeyAgreeRecipientInfoAsn decoded)
        {
            decoded = default;
            AsnValueReader sequenceReader = reader.ReadSequence(expectedTag);
            AsnValueReader explicitReader;
            AsnValueReader collectionReader;
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
}
