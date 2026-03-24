// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable SA1028 // ignore whitespace warnings for generated code
using System;
using System.Formats.Asn1;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Asn1
{
    [StructLayout(LayoutKind.Sequential)]
    internal ref partial struct ValueCurveAsn
    {
        internal ReadOnlySpan<byte> A;
        internal ReadOnlySpan<byte> B;

        internal ReadOnlySpan<byte> Seed
        {
            get;
            set
            {
                HasSeed = true;
                field = value;
            }
        }

        internal bool HasSeed { get; private set; }

        internal readonly void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }

        internal readonly void Encode(AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);

            writer.WriteOctetString(A);
            writer.WriteOctetString(B);

            if (HasSeed)
            {
                writer.WriteBitString(Seed, 0);
            }

            writer.PopSequence(tag);
        }

        internal static ValueCurveAsn Decode(ReadOnlySpan<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }

        internal static ValueCurveAsn Decode(Asn1Tag expectedTag, ReadOnlySpan<byte> encoded, AsnEncodingRules ruleSet)
        {
            try
            {
                ValueAsnReader reader = new ValueAsnReader(encoded, ruleSet);

                ValueCurveAsn decoded = DecodeCore(ref reader, expectedTag);
                reader.ThrowIfNotEmpty();
                return decoded;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static ValueCurveAsn Decode(scoped ref ValueAsnReader reader)
        {
            return Decode(ref reader, Asn1Tag.Sequence);
        }

        internal static ValueCurveAsn Decode(scoped ref ValueAsnReader reader, Asn1Tag expectedTag)
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

        private static ValueCurveAsn DecodeCore(scoped ref ValueAsnReader reader, Asn1Tag expectedTag)
        {
            ValueCurveAsn decoded = default;
            ValueAsnReader sequenceReader = reader.ReadSequence(expectedTag);
            ReadOnlySpan<byte> tmpSpan;


            if (sequenceReader.TryReadPrimitiveOctetString(out tmpSpan))
            {
                decoded.A = tmpSpan;
            }
            else
            {
                decoded.A = sequenceReader.ReadOctetString();
            }


            if (sequenceReader.TryReadPrimitiveOctetString(out tmpSpan))
            {
                decoded.B = tmpSpan;
            }
            else
            {
                decoded.B = sequenceReader.ReadOctetString();
            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(Asn1Tag.PrimitiveBitString))
            {

                if (sequenceReader.TryReadPrimitiveBitString(out _, out tmpSpan))
                {
                    decoded.Seed = tmpSpan;
                }
                else
                {
                    decoded.Seed = sequenceReader.ReadBitString(out _);
                }

                decoded.HasSeed = true;
            }


            sequenceReader.ThrowIfNotEmpty();
            return decoded;
        }
    }
}
