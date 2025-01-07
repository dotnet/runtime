// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable SA1028 // ignore whitespace warnings for generated code
using System;
using System.Formats.Asn1;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography.X509Certificates.Asn1
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct BasicConstraintsAsn
    {
        private static ReadOnlySpan<byte> DefaultCA => [0x01, 0x01, 0x00];

        internal bool CA;
        internal int? PathLengthConstraint;

#if DEBUG
        static BasicConstraintsAsn()
        {
            BasicConstraintsAsn decoded = default;
            AsnValueReader reader;

            reader = new AsnValueReader(DefaultCA, AsnEncodingRules.DER);
            decoded.CA = reader.ReadBoolean();
            reader.ThrowIfNotEmpty();
        }
#endif

        internal readonly void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }

        internal readonly void Encode(AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);


            // DEFAULT value handler for CA.
            {
                const int AsnBoolDerEncodeSize = 3;
                AsnWriter tmp = new AsnWriter(AsnEncodingRules.DER, initialCapacity: AsnBoolDerEncodeSize);
                tmp.WriteBoolean(CA);

                if (!tmp.EncodedValueEquals(DefaultCA))
                {
                    tmp.CopyTo(writer);
                }
            }


            if (PathLengthConstraint.HasValue)
            {
                writer.WriteInteger(PathLengthConstraint.Value);
            }

            writer.PopSequence(tag);
        }

        internal static BasicConstraintsAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }

        internal static BasicConstraintsAsn Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            try
            {
                AsnValueReader reader = new AsnValueReader(encoded.Span, ruleSet);

                DecodeCore(ref reader, expectedTag, out BasicConstraintsAsn decoded);
                reader.ThrowIfNotEmpty();
                return decoded;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static void Decode(ref AsnValueReader reader, out BasicConstraintsAsn decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, out decoded);
        }

        internal static void Decode(ref AsnValueReader reader, Asn1Tag expectedTag, out BasicConstraintsAsn decoded)
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

        private static void DecodeCore(ref AsnValueReader reader, Asn1Tag expectedTag, out BasicConstraintsAsn decoded)
        {
            decoded = default;
            AsnValueReader sequenceReader = reader.ReadSequence(expectedTag);
            AsnValueReader defaultReader;


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(Asn1Tag.Boolean))
            {
                decoded.CA = sequenceReader.ReadBoolean();
            }
            else
            {
                defaultReader = new AsnValueReader(DefaultCA, AsnEncodingRules.DER);
                decoded.CA = defaultReader.ReadBoolean();
            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(Asn1Tag.Integer))
            {

                if (sequenceReader.TryReadInt32(out int tmpPathLengthConstraint))
                {
                    decoded.PathLengthConstraint = tmpPathLengthConstraint;
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
