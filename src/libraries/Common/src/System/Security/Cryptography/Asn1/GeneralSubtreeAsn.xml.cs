// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable SA1028 // ignore whitespace warnings for generated code
using System;
using System.Formats.Asn1;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography.X509Certificates.Asn1
{
    file static class SharedGeneralSubtreeAsn
    {
        internal static ReadOnlySpan<byte> DefaultMinimum => [0x02, 0x01, 0x00];

#if DEBUG
        static SharedGeneralSubtreeAsn()
        {
            GeneralSubtreeAsn decoded = default;
            ValueAsnReader reader;

            reader = new ValueAsnReader(SharedGeneralSubtreeAsn.DefaultMinimum, AsnEncodingRules.DER);

            if (!reader.TryReadInt32(out decoded.Minimum))
            {
                reader.ThrowIfNotEmpty();
            }

            reader.ThrowIfNotEmpty();
        }
#endif
    }

    [StructLayout(LayoutKind.Sequential)]
    internal partial struct GeneralSubtreeAsn
    {
        internal System.Security.Cryptography.Asn1.GeneralNameAsn Base;
        internal int Minimum;
        internal int? Maximum;

        internal readonly void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }

        internal readonly void Encode(AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);

            Base.Encode(writer);

            // DEFAULT value handler for Minimum.
            {
                const int AsnManagedIntegerDerMaxEncodeSize = 6;
                AsnWriter tmp = new AsnWriter(AsnEncodingRules.DER, initialCapacity: AsnManagedIntegerDerMaxEncodeSize);
                tmp.WriteInteger(Minimum);

                if (!tmp.EncodedValueEquals(SharedGeneralSubtreeAsn.DefaultMinimum))
                {
                    writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
                    tmp.CopyTo(writer);
                    writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
                }
            }


            if (Maximum.HasValue)
            {
                writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 1));
                writer.WriteInteger(Maximum.Value);
                writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 1));
            }

            writer.PopSequence(tag);
        }

        internal static GeneralSubtreeAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }

        internal static GeneralSubtreeAsn Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            try
            {
                ValueAsnReader reader = new ValueAsnReader(encoded.Span, ruleSet);

                DecodeCore(ref reader, expectedTag, encoded, out GeneralSubtreeAsn decoded);
                reader.ThrowIfNotEmpty();
                return decoded;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static void Decode(ref ValueAsnReader reader, ReadOnlyMemory<byte> rebind, out GeneralSubtreeAsn decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
        }

        internal static void Decode(ref ValueAsnReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out GeneralSubtreeAsn decoded)
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

        private static void DecodeCore(ref ValueAsnReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out GeneralSubtreeAsn decoded)
        {
            decoded = default;
            ValueAsnReader sequenceReader = reader.ReadSequence(expectedTag);
            ValueAsnReader explicitReader;
            ValueAsnReader defaultReader;

            System.Security.Cryptography.Asn1.GeneralNameAsn.Decode(ref sequenceReader, rebind, out decoded.Base);

            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
            {
                explicitReader = sequenceReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0));

                if (!explicitReader.TryReadInt32(out decoded.Minimum))
                {
                    explicitReader.ThrowIfNotEmpty();
                }

                explicitReader.ThrowIfNotEmpty();
            }
            else
            {
                defaultReader = new ValueAsnReader(SharedGeneralSubtreeAsn.DefaultMinimum, AsnEncodingRules.DER);

                if (!defaultReader.TryReadInt32(out decoded.Minimum))
                {
                    defaultReader.ThrowIfNotEmpty();
                }

            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 1)))
            {
                explicitReader = sequenceReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 1));

                if (explicitReader.TryReadInt32(out int tmpMaximum))
                {
                    decoded.Maximum = tmpMaximum;
                }
                else
                {
                    explicitReader.ThrowIfNotEmpty();
                }

                explicitReader.ThrowIfNotEmpty();
            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
