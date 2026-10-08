// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable SA1028 // ignore whitespace warnings for generated code
using System;
using System.Formats.Asn1;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Asn1
{
    file static class SharedPssParamsAsn
    {
        internal static ReadOnlySpan<byte> DefaultHashAlgorithm => [0x30, 0x09, 0x06, 0x05, 0x2B, 0x0E, 0x03, 0x02, 0x1A, 0x05, 0x00];

        internal static ReadOnlySpan<byte> DefaultMaskGenAlgorithm => [0x30, 0x16, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x08, 0x30, 0x09, 0x06, 0x05, 0x2B, 0x0E, 0x03, 0x02, 0x1A, 0x05, 0x00];

        internal static ReadOnlySpan<byte> DefaultSaltLength => [0x02, 0x01, 0x14];

        internal static ReadOnlySpan<byte> DefaultTrailerField => [0x02, 0x01, 0x01];

#if DEBUG
        static SharedPssParamsAsn()
        {
            ValuePssParamsAsn decoded = default;
            ValueAsnReader reader;

            reader = new ValueAsnReader(SharedPssParamsAsn.DefaultHashAlgorithm, AsnEncodingRules.DER);
            System.Security.Cryptography.Asn1.ValueAlgorithmIdentifierAsn.Decode(ref reader, out decoded.HashAlgorithm);
            reader.ThrowIfNotEmpty();

            reader = new ValueAsnReader(SharedPssParamsAsn.DefaultMaskGenAlgorithm, AsnEncodingRules.DER);
            System.Security.Cryptography.Asn1.ValueAlgorithmIdentifierAsn.Decode(ref reader, out decoded.MaskGenAlgorithm);
            reader.ThrowIfNotEmpty();

            reader = new ValueAsnReader(SharedPssParamsAsn.DefaultSaltLength, AsnEncodingRules.DER);

            if (!reader.TryReadInt32(out decoded.SaltLength))
            {
                reader.ThrowIfNotEmpty();
            }

            reader.ThrowIfNotEmpty();

            reader = new ValueAsnReader(SharedPssParamsAsn.DefaultTrailerField, AsnEncodingRules.DER);

            if (!reader.TryReadInt32(out decoded.TrailerField))
            {
                reader.ThrowIfNotEmpty();
            }

            reader.ThrowIfNotEmpty();
        }
#endif
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref partial struct ValuePssParamsAsn
    {
        internal System.Security.Cryptography.Asn1.ValueAlgorithmIdentifierAsn HashAlgorithm;
        internal System.Security.Cryptography.Asn1.ValueAlgorithmIdentifierAsn MaskGenAlgorithm;
        internal int SaltLength;
        internal int TrailerField;

        internal readonly void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }

        internal readonly void Encode(AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);


            // DEFAULT value handler for HashAlgorithm.
            {
                AsnWriter tmp = new AsnWriter(AsnEncodingRules.DER);
                HashAlgorithm.Encode(tmp);

                if (!tmp.EncodedValueEquals(SharedPssParamsAsn.DefaultHashAlgorithm))
                {
                    writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
                    tmp.CopyTo(writer);
                    writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
                }
            }


            // DEFAULT value handler for MaskGenAlgorithm.
            {
                AsnWriter tmp = new AsnWriter(AsnEncodingRules.DER);
                MaskGenAlgorithm.Encode(tmp);

                if (!tmp.EncodedValueEquals(SharedPssParamsAsn.DefaultMaskGenAlgorithm))
                {
                    writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 1));
                    tmp.CopyTo(writer);
                    writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 1));
                }
            }


            // DEFAULT value handler for SaltLength.
            {
                const int AsnManagedIntegerDerMaxEncodeSize = 6;
                AsnWriter tmp = new AsnWriter(AsnEncodingRules.DER, initialCapacity: AsnManagedIntegerDerMaxEncodeSize);
                tmp.WriteInteger(SaltLength);

                if (!tmp.EncodedValueEquals(SharedPssParamsAsn.DefaultSaltLength))
                {
                    writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 2));
                    tmp.CopyTo(writer);
                    writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 2));
                }
            }


            // DEFAULT value handler for TrailerField.
            {
                const int AsnManagedIntegerDerMaxEncodeSize = 6;
                AsnWriter tmp = new AsnWriter(AsnEncodingRules.DER, initialCapacity: AsnManagedIntegerDerMaxEncodeSize);
                tmp.WriteInteger(TrailerField);

                if (!tmp.EncodedValueEquals(SharedPssParamsAsn.DefaultTrailerField))
                {
                    writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 3));
                    tmp.CopyTo(writer);
                    writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 3));
                }
            }

            writer.PopSequence(tag);
        }

        internal static void Decode(ReadOnlySpan<byte> encoded, AsnEncodingRules ruleSet, out ValuePssParamsAsn decoded)
        {
            Decode(Asn1Tag.Sequence, encoded, ruleSet, out decoded);
        }

        internal static void Decode(Asn1Tag expectedTag, ReadOnlySpan<byte> encoded, AsnEncodingRules ruleSet, out ValuePssParamsAsn decoded)
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

        internal static void Decode(scoped ref ValueAsnReader reader, out ValuePssParamsAsn decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, out decoded);
        }

        internal static void Decode(scoped ref ValueAsnReader reader, Asn1Tag expectedTag, out ValuePssParamsAsn decoded)
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

        private static void DecodeCore(scoped ref ValueAsnReader reader, Asn1Tag expectedTag, out ValuePssParamsAsn decoded)
        {
            decoded = default;
            ValueAsnReader sequenceReader = reader.ReadSequence(expectedTag);
            ValueAsnReader explicitReader;
            ValueAsnReader defaultReader;


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
            {
                explicitReader = sequenceReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
                System.Security.Cryptography.Asn1.ValueAlgorithmIdentifierAsn.Decode(ref explicitReader, out decoded.HashAlgorithm);
                explicitReader.ThrowIfNotEmpty();
            }
            else
            {
                defaultReader = new ValueAsnReader(SharedPssParamsAsn.DefaultHashAlgorithm, AsnEncodingRules.DER);
                System.Security.Cryptography.Asn1.ValueAlgorithmIdentifierAsn.Decode(ref defaultReader, out decoded.HashAlgorithm);
            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 1)))
            {
                explicitReader = sequenceReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 1));
                System.Security.Cryptography.Asn1.ValueAlgorithmIdentifierAsn.Decode(ref explicitReader, out decoded.MaskGenAlgorithm);
                explicitReader.ThrowIfNotEmpty();
            }
            else
            {
                defaultReader = new ValueAsnReader(SharedPssParamsAsn.DefaultMaskGenAlgorithm, AsnEncodingRules.DER);
                System.Security.Cryptography.Asn1.ValueAlgorithmIdentifierAsn.Decode(ref defaultReader, out decoded.MaskGenAlgorithm);
            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 2)))
            {
                explicitReader = sequenceReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 2));

                if (!explicitReader.TryReadInt32(out decoded.SaltLength))
                {
                    explicitReader.ThrowIfNotEmpty();
                }

                explicitReader.ThrowIfNotEmpty();
            }
            else
            {
                defaultReader = new ValueAsnReader(SharedPssParamsAsn.DefaultSaltLength, AsnEncodingRules.DER);

                if (!defaultReader.TryReadInt32(out decoded.SaltLength))
                {
                    defaultReader.ThrowIfNotEmpty();
                }

            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 3)))
            {
                explicitReader = sequenceReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 3));

                if (!explicitReader.TryReadInt32(out decoded.TrailerField))
                {
                    explicitReader.ThrowIfNotEmpty();
                }

                explicitReader.ThrowIfNotEmpty();
            }
            else
            {
                defaultReader = new ValueAsnReader(SharedPssParamsAsn.DefaultTrailerField, AsnEncodingRules.DER);

                if (!defaultReader.TryReadInt32(out decoded.TrailerField))
                {
                    defaultReader.ThrowIfNotEmpty();
                }

            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
