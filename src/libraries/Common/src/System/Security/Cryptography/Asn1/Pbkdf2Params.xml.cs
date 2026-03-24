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
            ValuePbkdf2Params decoded = default;
            ValueAsnReader reader;

            reader = new ValueAsnReader(SharedPbkdf2Params.DefaultPrf, AsnEncodingRules.DER);
            decoded.Prf = System.Security.Cryptography.Asn1.ValueAlgorithmIdentifierAsn.Decode(ref reader);
            reader.ThrowIfNotEmpty();
        }
#endif
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref partial struct ValuePbkdf2Params
    {
        internal System.Security.Cryptography.Asn1.ValuePbkdf2SaltChoice Salt;
        internal int IterationCount;
        internal int? KeyLength;
        internal System.Security.Cryptography.Asn1.ValueAlgorithmIdentifierAsn Prf;

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

        internal static ValuePbkdf2Params Decode(ReadOnlySpan<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }

        internal static ValuePbkdf2Params Decode(Asn1Tag expectedTag, ReadOnlySpan<byte> encoded, AsnEncodingRules ruleSet)
        {
            try
            {
                ValueAsnReader reader = new ValueAsnReader(encoded, ruleSet);

                ValuePbkdf2Params decoded = DecodeCore(ref reader, expectedTag);
                reader.ThrowIfNotEmpty();
                return decoded;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static ValuePbkdf2Params Decode(scoped ref ValueAsnReader reader)
        {
            return Decode(ref reader, Asn1Tag.Sequence);
        }

        internal static ValuePbkdf2Params Decode(scoped ref ValueAsnReader reader, Asn1Tag expectedTag)
        {
            try
            {
                return DecodeCore(ref reader, expectedTag);
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        private static ValuePbkdf2Params DecodeCore(scoped ref ValueAsnReader reader, Asn1Tag expectedTag)
        {
            ValuePbkdf2Params decoded = default;
            ValueAsnReader sequenceReader = reader.ReadSequence(expectedTag);
            ValueAsnReader defaultReader;

            decoded.Salt = System.Security.Cryptography.Asn1.ValuePbkdf2SaltChoice.Decode(ref sequenceReader);

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
                decoded.Prf = System.Security.Cryptography.Asn1.ValueAlgorithmIdentifierAsn.Decode(ref sequenceReader);
            }
            else
            {
                defaultReader = new ValueAsnReader(SharedPbkdf2Params.DefaultPrf, AsnEncodingRules.DER);
                decoded.Prf = System.Security.Cryptography.Asn1.ValueAlgorithmIdentifierAsn.Decode(ref defaultReader);
            }


            sequenceReader.ThrowIfNotEmpty();
            return decoded;
        }
    }
}
