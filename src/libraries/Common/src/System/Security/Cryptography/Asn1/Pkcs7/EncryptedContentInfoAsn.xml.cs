// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable SA1028 // ignore whitespace warnings for generated code
using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.Asn1.Pkcs7
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct EncryptedContentInfoAsn
    {
        internal string ContentType;
        internal System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn ContentEncryptionAlgorithm;
        internal ReadOnlyMemory<byte>? EncryptedContent;

        internal void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }

        internal void Encode(AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);

            writer.WriteObjectIdentifier(ContentType);
            ContentEncryptionAlgorithm.Encode(writer);

            if (EncryptedContent.HasValue)
            {
                writer.WriteOctetString(new Asn1Tag(TagClass.ContextSpecific, 0), EncryptedContent.Value.Span);
            }

            writer.PopSequence(tag);
        }

        internal static EncryptedContentInfoAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }

        internal static EncryptedContentInfoAsn Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            AsnValueReader reader = new AsnValueReader(encoded.Span, ruleSet);

            Decode(ref reader, expectedTag, encoded, out EncryptedContentInfoAsn decoded);
            reader.ThrowIfNotEmpty();
            return decoded;
        }

        internal static void Decode(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out EncryptedContentInfoAsn decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
        }

        internal static void Decode(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out EncryptedContentInfoAsn decoded)
        {
            decoded = default;
            AsnValueReader sequenceReader = reader.ReadSequence(expectedTag);
            ReadOnlySpan<byte> rebindSpan = rebind.Span;
            int offset;
            ReadOnlySpan<byte> tmpSpan;

            decoded.ContentType = sequenceReader.ReadObjectIdentifierAsString();
            System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(ref sequenceReader, rebind, out decoded.ContentEncryptionAlgorithm);

            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
            {

                if (sequenceReader.TryReadPrimitiveOctetStringBytes(new Asn1Tag(TagClass.ContextSpecific, 0), out tmpSpan))
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
}
