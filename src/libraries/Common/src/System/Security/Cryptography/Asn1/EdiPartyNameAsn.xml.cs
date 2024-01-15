// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable SA1028 // ignore whitespace warnings for generated code
using System;
using System.Formats.Asn1;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Asn1
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct EdiPartyNameAsn
    {
        internal System.Security.Cryptography.Asn1.DirectoryStringAsn? NameAssigner;
        internal System.Security.Cryptography.Asn1.DirectoryStringAsn PartyName;

        internal readonly void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }

        internal readonly void Encode(AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);


            if (NameAssigner.HasValue)
            {
                writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
                NameAssigner.Value.Encode(writer);
                writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
            }

            writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 1));
            PartyName.Encode(writer);
            writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 1));
            writer.PopSequence(tag);
        }

        internal static EdiPartyNameAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }

        internal static EdiPartyNameAsn Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            try
            {
                AsnValueReader reader = new AsnValueReader(encoded.Span, ruleSet);

                DecodeCore(ref reader, expectedTag, encoded, out EdiPartyNameAsn decoded);
                reader.ThrowIfNotEmpty();
                return decoded;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static void Decode(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out EdiPartyNameAsn decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
        }

        internal static void Decode(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out EdiPartyNameAsn decoded)
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

        private static void DecodeCore(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out EdiPartyNameAsn decoded)
        {
            decoded = default;
            AsnValueReader sequenceReader = reader.ReadSequence(expectedTag);
            AsnValueReader explicitReader;


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
            {
                explicitReader = sequenceReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
                System.Security.Cryptography.Asn1.DirectoryStringAsn tmpNameAssigner;
                System.Security.Cryptography.Asn1.DirectoryStringAsn.Decode(ref explicitReader, rebind, out tmpNameAssigner);
                decoded.NameAssigner = tmpNameAssigner;

                explicitReader.ThrowIfNotEmpty();
            }


            explicitReader = sequenceReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 1));
            System.Security.Cryptography.Asn1.DirectoryStringAsn.Decode(ref explicitReader, rebind, out decoded.PartyName);
            explicitReader.ThrowIfNotEmpty();


            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
