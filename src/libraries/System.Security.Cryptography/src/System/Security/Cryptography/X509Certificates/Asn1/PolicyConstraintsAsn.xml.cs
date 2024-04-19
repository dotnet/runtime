// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable SA1028 // ignore whitespace warnings for generated code
using System;
using System.Formats.Asn1;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography.X509Certificates.Asn1
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct PolicyConstraintsAsn
    {
        internal int? RequireExplicitPolicyDepth;
        internal int? InhibitMappingDepth;

        internal readonly void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }

        internal readonly void Encode(AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);


            if (RequireExplicitPolicyDepth.HasValue)
            {
                writer.WriteInteger(RequireExplicitPolicyDepth.Value, new Asn1Tag(TagClass.ContextSpecific, 0));
            }


            if (InhibitMappingDepth.HasValue)
            {
                writer.WriteInteger(InhibitMappingDepth.Value, new Asn1Tag(TagClass.ContextSpecific, 1));
            }

            writer.PopSequence(tag);
        }

        internal static PolicyConstraintsAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }

        internal static PolicyConstraintsAsn Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            try
            {
                AsnValueReader reader = new AsnValueReader(encoded.Span, ruleSet);

                DecodeCore(ref reader, expectedTag, out PolicyConstraintsAsn decoded);
                reader.ThrowIfNotEmpty();
                return decoded;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static void Decode(ref AsnValueReader reader, out PolicyConstraintsAsn decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, out decoded);
        }

        internal static void Decode(ref AsnValueReader reader, Asn1Tag expectedTag, out PolicyConstraintsAsn decoded)
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

        private static void DecodeCore(ref AsnValueReader reader, Asn1Tag expectedTag, out PolicyConstraintsAsn decoded)
        {
            decoded = default;
            AsnValueReader sequenceReader = reader.ReadSequence(expectedTag);


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
            {

                if (sequenceReader.TryReadInt32(out int tmpRequireExplicitPolicyDepth, new Asn1Tag(TagClass.ContextSpecific, 0)))
                {
                    decoded.RequireExplicitPolicyDepth = tmpRequireExplicitPolicyDepth;
                }
                else
                {
                    sequenceReader.ThrowIfNotEmpty();
                }

            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 1)))
            {

                if (sequenceReader.TryReadInt32(out int tmpInhibitMappingDepth, new Asn1Tag(TagClass.ContextSpecific, 1)))
                {
                    decoded.InhibitMappingDepth = tmpInhibitMappingDepth;
                }
                else
                {
                    sequenceReader.ThrowIfNotEmpty();
                }

            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
