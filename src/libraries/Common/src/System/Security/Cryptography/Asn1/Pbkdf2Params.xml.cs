// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable SA1028 // ignore whitespace warnings for generated code
using System;
using System.Formats.Asn1;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Asn1
{
    file static class SharedPbkdf2Params
    {
        internal static ReadOnlySpan<byte> DefaultPrf => [0x30, 0x0C, 0x06, 0x08, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x02, 0x07, 0x05, 0x00];

#if DEBUG
        static SharedPbkdf2Params()
        {
            Pbkdf2Params decoded = default;
            ReadOnlyMemory<byte> rebind = default;
            ValueAsnReader reader;

            reader = new ValueAsnReader(SharedPbkdf2Params.DefaultPrf, AsnEncodingRules.DER);
            System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(ref reader, rebind, out decoded.Prf);
            reader.ThrowIfNotEmpty();
        }
#endif
    }

    [StructLayout(LayoutKind.Sequential)]
    internal partial struct Pbkdf2Params
    {
        internal System.Security.Cryptography.Asn1.Pbkdf2SaltChoice Salt;
        internal int IterationCount;
        internal int? KeyLength;
        internal System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn Prf;

        internal readonly void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }

        internal readonly void Encode(AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);

            Salt.Encode(writer);
            writer.WriteInteger(IterationCount);

            if (KeyLength.HasValue)
            {
                writer.WriteInteger(KeyLength.Value);
            }


            // DEFAULT value handler for Prf.
            {
                AsnWriter tmp = new AsnWriter(AsnEncodingRules.DER);
                Prf.Encode(tmp);

                if (!tmp.EncodedValueEquals(SharedPbkdf2Params.DefaultPrf))
                {
                    tmp.CopyTo(writer);
                }
            }

            writer.PopSequence(tag);
        }

        internal static Pbkdf2Params Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }

        internal static Pbkdf2Params Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            try
            {
                ValueAsnReader reader = new ValueAsnReader(encoded.Span, ruleSet);

                DecodeCore(ref reader, expectedTag, encoded, out Pbkdf2Params decoded);
                reader.ThrowIfNotEmpty();
                return decoded;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static void Decode(ref ValueAsnReader reader, ReadOnlyMemory<byte> rebind, out Pbkdf2Params decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
        }

        internal static void Decode(ref ValueAsnReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out Pbkdf2Params decoded)
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

        private static void DecodeCore(ref ValueAsnReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out Pbkdf2Params decoded)
        {
            decoded = default;
            ValueAsnReader sequenceReader = reader.ReadSequence(expectedTag);
            ValueAsnReader defaultReader;

            System.Security.Cryptography.Asn1.Pbkdf2SaltChoice.Decode(ref sequenceReader, rebind, out decoded.Salt);

            if (!sequenceReader.TryReadInt32(out decoded.IterationCount))
            {
                sequenceReader.ThrowIfNotEmpty();
            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(Asn1Tag.Integer))
            {

                if (sequenceReader.TryReadInt32(out int tmpKeyLength))
                {
                    decoded.KeyLength = tmpKeyLength;
                }
                else
                {
                    sequenceReader.ThrowIfNotEmpty();
                }

            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(Asn1Tag.Sequence))
            {
                System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(ref sequenceReader, rebind, out decoded.Prf);
            }
            else
            {
                defaultReader = new ValueAsnReader(SharedPbkdf2Params.DefaultPrf, AsnEncodingRules.DER);
                System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(ref defaultReader, rebind, out decoded.Prf);
            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
