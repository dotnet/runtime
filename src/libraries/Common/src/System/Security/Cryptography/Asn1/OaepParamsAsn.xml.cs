﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable SA1028 // ignore whitespace warnings for generated code
using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.Asn1
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct OaepParamsAsn
    {
        private static ReadOnlySpan<byte> DefaultHashFunc => new byte[] { 0x30, 0x09, 0x06, 0x05, 0x2B, 0x0E, 0x03, 0x02, 0x1A, 0x05, 0x00 };

        private static ReadOnlySpan<byte> DefaultMaskGenFunc => new byte[] { 0x30, 0x16, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x08, 0x30, 0x09, 0x06, 0x05, 0x2B, 0x0E, 0x03, 0x02, 0x1A, 0x05, 0x00 };

        private static ReadOnlySpan<byte> DefaultPSourceFunc => new byte[] { 0x30, 0x0D, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x09, 0x04, 0x00 };

        internal System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn HashFunc;
        internal System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn MaskGenFunc;
        internal System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn PSourceFunc;

#if DEBUG
        static OaepParamsAsn()
        {
            OaepParamsAsn decoded = default;
            ReadOnlyMemory<byte> rebind = default;
            AsnValueReader reader;

            reader = new AsnValueReader(DefaultHashFunc, AsnEncodingRules.DER);
            System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(ref reader, rebind, out decoded.HashFunc);
            reader.ThrowIfNotEmpty();

            reader = new AsnValueReader(DefaultMaskGenFunc, AsnEncodingRules.DER);
            System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(ref reader, rebind, out decoded.MaskGenFunc);
            reader.ThrowIfNotEmpty();

            reader = new AsnValueReader(DefaultPSourceFunc, AsnEncodingRules.DER);
            System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(ref reader, rebind, out decoded.PSourceFunc);
            reader.ThrowIfNotEmpty();
        }
#endif

        internal void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }

        internal void Encode(AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);


            // DEFAULT value handler for HashFunc.
            {
                using (AsnWriter tmp = new AsnWriter(AsnEncodingRules.DER))
                {
                    HashFunc.Encode(tmp);
                    ReadOnlySpan<byte> encoded = tmp.EncodeAsSpan();

                    if (!encoded.SequenceEqual(DefaultHashFunc))
                    {
                        writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
                        writer.WriteEncodedValue(encoded);
                        writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
                    }
                }
            }


            // DEFAULT value handler for MaskGenFunc.
            {
                using (AsnWriter tmp = new AsnWriter(AsnEncodingRules.DER))
                {
                    MaskGenFunc.Encode(tmp);
                    ReadOnlySpan<byte> encoded = tmp.EncodeAsSpan();

                    if (!encoded.SequenceEqual(DefaultMaskGenFunc))
                    {
                        writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 1));
                        writer.WriteEncodedValue(encoded);
                        writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 1));
                    }
                }
            }


            // DEFAULT value handler for PSourceFunc.
            {
                using (AsnWriter tmp = new AsnWriter(AsnEncodingRules.DER))
                {
                    PSourceFunc.Encode(tmp);
                    ReadOnlySpan<byte> encoded = tmp.EncodeAsSpan();

                    if (!encoded.SequenceEqual(DefaultPSourceFunc))
                    {
                        writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 2));
                        writer.WriteEncodedValue(encoded);
                        writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 2));
                    }
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
            AsnValueReader reader = new AsnValueReader(encoded.Span, ruleSet);

            Decode(ref reader, expectedTag, encoded, out OaepParamsAsn decoded);
            reader.ThrowIfNotEmpty();
            return decoded;
        }

        internal static void Decode(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out OaepParamsAsn decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
        }

        internal static void Decode(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out OaepParamsAsn decoded)
        {
            decoded = default;
            AsnValueReader sequenceReader = reader.ReadSequence(expectedTag);
            AsnValueReader explicitReader;
            AsnValueReader defaultReader;


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
            {
                explicitReader = sequenceReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
                System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(ref explicitReader, rebind, out decoded.HashFunc);
                explicitReader.ThrowIfNotEmpty();
            }
            else
            {
                defaultReader = new AsnValueReader(DefaultHashFunc, AsnEncodingRules.DER);
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
                defaultReader = new AsnValueReader(DefaultMaskGenFunc, AsnEncodingRules.DER);
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
                defaultReader = new AsnValueReader(DefaultPSourceFunc, AsnEncodingRules.DER);
                System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(ref defaultReader, rebind, out decoded.PSourceFunc);
            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
