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
        internal System.Numerics.BigInteger Modulus;
        internal System.Numerics.BigInteger PublicExponent;
        internal System.Numerics.BigInteger PrivateExponent;
        internal System.Numerics.BigInteger Prime1;
        internal System.Numerics.BigInteger Prime2;
        internal System.Numerics.BigInteger Exponent1;
        internal System.Numerics.BigInteger Exponent2;
        internal System.Numerics.BigInteger Coefficient;

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

        internal static RSAPrivateKeyAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }

        internal static RSAPrivateKeyAsn Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            try
            {
                AsnValueReader reader = new AsnValueReader(encoded.Span, ruleSet);

                DecodeCore(ref reader, expectedTag, out RSAPrivateKeyAsn decoded);
                reader.ThrowIfNotEmpty();
                return decoded;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static void Decode(ref AsnValueReader reader, out RSAPrivateKeyAsn decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, out decoded);
        }

        internal static void Decode(ref AsnValueReader reader, Asn1Tag expectedTag, out RSAPrivateKeyAsn decoded)
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

        private static void DecodeCore(ref AsnValueReader reader, Asn1Tag expectedTag, out RSAPrivateKeyAsn decoded)
        {
            decoded = default;
            AsnValueReader sequenceReader = reader.ReadSequence(expectedTag);


            if (!sequenceReader.TryReadInt32(out decoded.Version))
            {
                sequenceReader.ThrowIfNotEmpty();
            }

            decoded.Modulus = sequenceReader.ReadInteger();
            decoded.PublicExponent = sequenceReader.ReadInteger();
            decoded.PrivateExponent = sequenceReader.ReadInteger();
            decoded.Prime1 = sequenceReader.ReadInteger();
            decoded.Prime2 = sequenceReader.ReadInteger();
            decoded.Exponent1 = sequenceReader.ReadInteger();
            decoded.Exponent2 = sequenceReader.ReadInteger();
            decoded.Coefficient = sequenceReader.ReadInteger();

            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
