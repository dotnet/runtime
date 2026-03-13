// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable SA1028 // ignore whitespace warnings for generated code
using System;
using System.Formats.Asn1;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Asn1
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct RSAPrivateKeyAsn
    {
        internal int Version;
        internal ReadOnlyMemory<byte> Modulus;
        internal ReadOnlyMemory<byte> PublicExponent;
        internal ReadOnlyMemory<byte> PrivateExponent;
        internal ReadOnlyMemory<byte> Prime1;
        internal ReadOnlyMemory<byte> Prime2;
        internal ReadOnlyMemory<byte> Exponent1;
        internal ReadOnlyMemory<byte> Exponent2;
        internal ReadOnlyMemory<byte> Coefficient;

        internal readonly void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }

        internal readonly void Encode(AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);

            writer.WriteInteger(Version);
            writer.WriteInteger(Modulus.Span);
            writer.WriteInteger(PublicExponent.Span);
            writer.WriteInteger(PrivateExponent.Span);
            writer.WriteInteger(Prime1.Span);
            writer.WriteInteger(Prime2.Span);
            writer.WriteInteger(Exponent1.Span);
            writer.WriteInteger(Exponent2.Span);
            writer.WriteInteger(Coefficient.Span);
            writer.PopSequence(tag);
        }

        internal static RSAPrivateKeyAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }

        internal static RSAPrivateKeyAsn Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            try
            {
                ValueAsnReader reader = new ValueAsnReader(encoded.Span, ruleSet);

                DecodeCore(ref reader, expectedTag, encoded, out RSAPrivateKeyAsn decoded);
                reader.ThrowIfNotEmpty();
                return decoded;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static void Decode(ref ValueAsnReader reader, ReadOnlyMemory<byte> rebind, out RSAPrivateKeyAsn decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
        }

        internal static void Decode(ref ValueAsnReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out RSAPrivateKeyAsn decoded)
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

        private static void DecodeCore(ref ValueAsnReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out RSAPrivateKeyAsn decoded)
        {
            decoded = default;
            ValueAsnReader sequenceReader = reader.ReadSequence(expectedTag);
            ReadOnlySpan<byte> rebindSpan = rebind.Span;
            int offset;
            ReadOnlySpan<byte> tmpSpan;


            if (!sequenceReader.TryReadInt32(out decoded.Version))
            {
                sequenceReader.ThrowIfNotEmpty();
            }

            tmpSpan = sequenceReader.ReadIntegerBytes();
            decoded.Modulus = rebindSpan.Overlaps(tmpSpan, out offset) ? rebind.Slice(offset, tmpSpan.Length) : tmpSpan.ToArray();
            tmpSpan = sequenceReader.ReadIntegerBytes();
            decoded.PublicExponent = rebindSpan.Overlaps(tmpSpan, out offset) ? rebind.Slice(offset, tmpSpan.Length) : tmpSpan.ToArray();
            tmpSpan = sequenceReader.ReadIntegerBytes();
            decoded.PrivateExponent = rebindSpan.Overlaps(tmpSpan, out offset) ? rebind.Slice(offset, tmpSpan.Length) : tmpSpan.ToArray();
            tmpSpan = sequenceReader.ReadIntegerBytes();
            decoded.Prime1 = rebindSpan.Overlaps(tmpSpan, out offset) ? rebind.Slice(offset, tmpSpan.Length) : tmpSpan.ToArray();
            tmpSpan = sequenceReader.ReadIntegerBytes();
            decoded.Prime2 = rebindSpan.Overlaps(tmpSpan, out offset) ? rebind.Slice(offset, tmpSpan.Length) : tmpSpan.ToArray();
            tmpSpan = sequenceReader.ReadIntegerBytes();
            decoded.Exponent1 = rebindSpan.Overlaps(tmpSpan, out offset) ? rebind.Slice(offset, tmpSpan.Length) : tmpSpan.ToArray();
            tmpSpan = sequenceReader.ReadIntegerBytes();
            decoded.Exponent2 = rebindSpan.Overlaps(tmpSpan, out offset) ? rebind.Slice(offset, tmpSpan.Length) : tmpSpan.ToArray();
            tmpSpan = sequenceReader.ReadIntegerBytes();
            decoded.Coefficient = rebindSpan.Overlaps(tmpSpan, out offset) ? rebind.Slice(offset, tmpSpan.Length) : tmpSpan.ToArray();

            sequenceReader.ThrowIfNotEmpty();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref partial struct ValueRSAPrivateKeyAsn
    {
        internal int Version;
        internal ReadOnlySpan<byte> Modulus;
        internal ReadOnlySpan<byte> PublicExponent;
        internal ReadOnlySpan<byte> PrivateExponent;
        internal ReadOnlySpan<byte> Prime1;
        internal ReadOnlySpan<byte> Prime2;
        internal ReadOnlySpan<byte> Exponent1;
        internal ReadOnlySpan<byte> Exponent2;
        internal ReadOnlySpan<byte> Coefficient;

        internal readonly void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }

        internal readonly void Encode(AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);

            writer.WriteInteger(Version);
            writer.WriteInteger(Modulus);
            writer.WriteInteger(PublicExponent);
            writer.WriteInteger(PrivateExponent);
            writer.WriteInteger(Prime1);
            writer.WriteInteger(Prime2);
            writer.WriteInteger(Exponent1);
            writer.WriteInteger(Exponent2);
            writer.WriteInteger(Coefficient);
            writer.PopSequence(tag);
        }

        internal static void Decode(ReadOnlySpan<byte> encoded, AsnEncodingRules ruleSet, out ValueRSAPrivateKeyAsn decoded)
        {
            Decode(Asn1Tag.Sequence, encoded, ruleSet, out decoded);
        }

        internal static void Decode(Asn1Tag expectedTag, ReadOnlySpan<byte> encoded, AsnEncodingRules ruleSet, out ValueRSAPrivateKeyAsn decoded)
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

        internal static void Decode(scoped ref ValueAsnReader reader, out ValueRSAPrivateKeyAsn decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, out decoded);
        }

        internal static void Decode(scoped ref ValueAsnReader reader, Asn1Tag expectedTag, out ValueRSAPrivateKeyAsn decoded)
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

        private static void DecodeCore(scoped ref ValueAsnReader reader, Asn1Tag expectedTag, out ValueRSAPrivateKeyAsn decoded)
        {
            decoded = default;
            ValueAsnReader sequenceReader = reader.ReadSequence(expectedTag);


            if (!sequenceReader.TryReadInt32(out decoded.Version))
            {
                sequenceReader.ThrowIfNotEmpty();
            }

            decoded.Modulus = sequenceReader.ReadIntegerBytes();
            decoded.PublicExponent = sequenceReader.ReadIntegerBytes();
            decoded.PrivateExponent = sequenceReader.ReadIntegerBytes();
            decoded.Prime1 = sequenceReader.ReadIntegerBytes();
            decoded.Prime2 = sequenceReader.ReadIntegerBytes();
            decoded.Exponent1 = sequenceReader.ReadIntegerBytes();
            decoded.Exponent2 = sequenceReader.ReadIntegerBytes();
            decoded.Coefficient = sequenceReader.ReadIntegerBytes();

            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
