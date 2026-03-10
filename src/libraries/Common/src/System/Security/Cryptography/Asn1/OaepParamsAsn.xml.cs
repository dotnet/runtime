// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable SA1028 // ignore whitespace warnings for generated code
using System;
using System.Formats.Asn1;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Asn1
{
    file static class SharedOaepParamsAsn
    {
        internal static ReadOnlySpan<byte> DefaultHashFunc => [0x30, 0x09, 0x06, 0x05, 0x2B, 0x0E, 0x03, 0x02, 0x1A, 0x05, 0x00];

        internal static ReadOnlySpan<byte> DefaultMaskGenFunc => [0x30, 0x16, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x08, 0x30, 0x09, 0x06, 0x05, 0x2B, 0x0E, 0x03, 0x02, 0x1A, 0x05, 0x00];

        internal static ReadOnlySpan<byte> DefaultPSourceFunc => [0x30, 0x0D, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x09, 0x04, 0x00];

#if DEBUG
        static SharedOaepParamsAsn()
        {
            OaepParamsAsn decoded = default;
            ReadOnlyMemory<byte> rebind = default;
            ValueAsnReader reader;

            reader = new ValueAsnReader(SharedOaepParamsAsn.DefaultHashFunc, AsnEncodingRules.DER);
            System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(ref reader, rebind, out decoded.HashFunc);
            reader.ThrowIfNotEmpty();

            reader = new ValueAsnReader(SharedOaepParamsAsn.DefaultMaskGenFunc, AsnEncodingRules.DER);
            System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(ref reader, rebind, out decoded.MaskGenFunc);
            reader.ThrowIfNotEmpty();

            reader = new ValueAsnReader(SharedOaepParamsAsn.DefaultPSourceFunc, AsnEncodingRules.DER);
            System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(ref reader, rebind, out decoded.PSourceFunc);
            reader.ThrowIfNotEmpty();
        }
#endif
    }

    [StructLayout(LayoutKind.Sequential)]
    internal partial struct OaepParamsAsn
    {
        internal System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn HashFunc;
        internal System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn MaskGenFunc;
        internal System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn PSourceFunc;

        internal readonly void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }

        internal readonly void Encode(AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);


            // DEFAULT value handler for HashFunc.
            {
                AsnWriter tmp = new AsnWriter(AsnEncodingRules.DER);
                HashFunc.Encode(tmp);

                if (!tmp.EncodedValueEquals(SharedOaepParamsAsn.DefaultHashFunc))
                {
                    writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
                    tmp.CopyTo(writer);
                    writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
                }
            }


            // DEFAULT value handler for MaskGenFunc.
            {
                AsnWriter tmp = new AsnWriter(AsnEncodingRules.DER);
                MaskGenFunc.Encode(tmp);

                if (!tmp.EncodedValueEquals(SharedOaepParamsAsn.DefaultMaskGenFunc))
                {
                    writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 1));
                    tmp.CopyTo(writer);
                    writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 1));
                }
            }


            // DEFAULT value handler for PSourceFunc.
            {
                AsnWriter tmp = new AsnWriter(AsnEncodingRules.DER);
                PSourceFunc.Encode(tmp);

                if (!tmp.EncodedValueEquals(SharedOaepParamsAsn.DefaultPSourceFunc))
                {
                    writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 2));
                    tmp.CopyTo(writer);
                    writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 2));
                }
            }

            writer.PopSequence(tag);
        }

        internal static OaepParamsAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }

        internal static OaepParamsAsn Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            try
            {
                ValueAsnReader reader = new ValueAsnReader(encoded.Span, ruleSet);

                DecodeCore(ref reader, expectedTag, encoded, out OaepParamsAsn decoded);
                reader.ThrowIfNotEmpty();
                return decoded;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static void Decode(ref ValueAsnReader reader, ReadOnlyMemory<byte> rebind, out OaepParamsAsn decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
        }

        internal static void Decode(ref ValueAsnReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out OaepParamsAsn decoded)
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

        private static void DecodeCore(ref ValueAsnReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out OaepParamsAsn decoded)
        {
            decoded = default;
            ValueAsnReader sequenceReader = reader.ReadSequence(expectedTag);
            ValueAsnReader explicitReader;
            ValueAsnReader defaultReader;


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
            {
                explicitReader = sequenceReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
                System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(ref explicitReader, rebind, out decoded.HashFunc);
                explicitReader.ThrowIfNotEmpty();
            }
            else
            {
                defaultReader = new ValueAsnReader(SharedOaepParamsAsn.DefaultHashFunc, AsnEncodingRules.DER);
                System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(ref defaultReader, rebind, out decoded.HashFunc);
            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 1)))
            {
                explicitReader = sequenceReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 1));
                System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(ref explicitReader, rebind, out decoded.MaskGenFunc);
                explicitReader.ThrowIfNotEmpty();
            }
            else
            {
                defaultReader = new ValueAsnReader(SharedOaepParamsAsn.DefaultMaskGenFunc, AsnEncodingRules.DER);
                System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(ref defaultReader, rebind, out decoded.MaskGenFunc);
            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 2)))
            {
                explicitReader = sequenceReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 2));
                System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(ref explicitReader, rebind, out decoded.PSourceFunc);
                explicitReader.ThrowIfNotEmpty();
            }
            else
            {
                defaultReader = new ValueAsnReader(SharedOaepParamsAsn.DefaultPSourceFunc, AsnEncodingRules.DER);
                System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(ref defaultReader, rebind, out decoded.PSourceFunc);
            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
