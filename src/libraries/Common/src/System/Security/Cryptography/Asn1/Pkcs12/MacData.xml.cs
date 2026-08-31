// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable SA1028 // ignore whitespace warnings for generated code
using System;
using System.Formats.Asn1;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Asn1.Pkcs12
{
    file static class SharedMacData
    {
        internal static ReadOnlySpan<byte> DefaultIterationCount => [0x02, 0x01, 0x01];

#if DEBUG
        static SharedMacData()
        {
            MacData decoded = default;
            ValueAsnReader reader;

            reader = new ValueAsnReader(SharedMacData.DefaultIterationCount, AsnEncodingRules.DER);

            if (!reader.TryReadInt32(out decoded.IterationCount))
            {
                reader.ThrowIfNotEmpty();
            }

            reader.ThrowIfNotEmpty();
        }
#endif
    }

    [StructLayout(LayoutKind.Sequential)]
    internal partial struct MacData
    {
        internal System.Security.Cryptography.Asn1.DigestInfoAsn Mac;
        internal ReadOnlyMemory<byte> MacSalt;
        internal int IterationCount;

        internal readonly void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }

        internal readonly void Encode(AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);

            Mac.Encode(writer);
            writer.WriteOctetString(MacSalt.Span);

            // DEFAULT value handler for IterationCount.
            {
                const int AsnManagedIntegerDerMaxEncodeSize = 6;
                AsnWriter tmp = new AsnWriter(AsnEncodingRules.DER, initialCapacity: AsnManagedIntegerDerMaxEncodeSize);
                tmp.WriteInteger(IterationCount);

                if (!tmp.EncodedValueEquals(SharedMacData.DefaultIterationCount))
                {
                    tmp.CopyTo(writer);
                }
            }

            writer.PopSequence(tag);
        }

        internal static MacData Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }

        internal static MacData Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            try
            {
                ValueAsnReader reader = new ValueAsnReader(encoded.Span, ruleSet);

                DecodeCore(ref reader, expectedTag, encoded, out MacData decoded);
                reader.ThrowIfNotEmpty();
                return decoded;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static void Decode(ref ValueAsnReader reader, ReadOnlyMemory<byte> rebind, out MacData decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
        }

        internal static void Decode(ref ValueAsnReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out MacData decoded)
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

        private static void DecodeCore(ref ValueAsnReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out MacData decoded)
        {
            decoded = default;
            ValueAsnReader sequenceReader = reader.ReadSequence(expectedTag);
            ValueAsnReader defaultReader;
            ReadOnlySpan<byte> rebindSpan = rebind.Span;
            int offset;
            ReadOnlySpan<byte> tmpSpan;

            System.Security.Cryptography.Asn1.DigestInfoAsn.Decode(ref sequenceReader, rebind, out decoded.Mac);

            if (sequenceReader.TryReadPrimitiveOctetString(out tmpSpan))
            {
                decoded.MacSalt = rebindSpan.Overlaps(tmpSpan, out offset) ? rebind.Slice(offset, tmpSpan.Length) : tmpSpan.ToArray();
            }
            else
            {
                decoded.MacSalt = sequenceReader.ReadOctetString();
            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(Asn1Tag.Integer))
            {

                if (!sequenceReader.TryReadInt32(out decoded.IterationCount))
                {
                    sequenceReader.ThrowIfNotEmpty();
                }

            }
            else
            {
                defaultReader = new ValueAsnReader(SharedMacData.DefaultIterationCount, AsnEncodingRules.DER);

                if (!defaultReader.TryReadInt32(out decoded.IterationCount))
                {
                    defaultReader.ThrowIfNotEmpty();
                }

            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref partial struct ValueMacData
    {
        internal System.Security.Cryptography.Asn1.ValueDigestInfoAsn Mac;
        internal ReadOnlySpan<byte> MacSalt;
        internal int IterationCount;

        internal readonly void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }

        internal readonly void Encode(AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);

            Mac.Encode(writer);
            writer.WriteOctetString(MacSalt);

            // DEFAULT value handler for IterationCount.
            {
                const int AsnManagedIntegerDerMaxEncodeSize = 6;
                AsnWriter tmp = new AsnWriter(AsnEncodingRules.DER, initialCapacity: AsnManagedIntegerDerMaxEncodeSize);
                tmp.WriteInteger(IterationCount);

                if (!tmp.EncodedValueEquals(SharedMacData.DefaultIterationCount))
                {
                    tmp.CopyTo(writer);
                }
            }

            writer.PopSequence(tag);
        }

        internal static void Decode(ReadOnlySpan<byte> encoded, AsnEncodingRules ruleSet, out ValueMacData decoded)
        {
            Decode(Asn1Tag.Sequence, encoded, ruleSet, out decoded);
        }

        internal static void Decode(Asn1Tag expectedTag, ReadOnlySpan<byte> encoded, AsnEncodingRules ruleSet, out ValueMacData decoded)
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

        internal static void Decode(scoped ref ValueAsnReader reader, out ValueMacData decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, out decoded);
        }

        internal static void Decode(scoped ref ValueAsnReader reader, Asn1Tag expectedTag, out ValueMacData decoded)
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

        private static void DecodeCore(scoped ref ValueAsnReader reader, Asn1Tag expectedTag, out ValueMacData decoded)
        {
            decoded = default;
            ValueAsnReader sequenceReader = reader.ReadSequence(expectedTag);
            ValueAsnReader defaultReader;
            ReadOnlySpan<byte> tmpSpan;

            System.Security.Cryptography.Asn1.ValueDigestInfoAsn.Decode(ref sequenceReader, out decoded.Mac);

            if (sequenceReader.TryReadPrimitiveOctetString(out tmpSpan))
            {
                decoded.MacSalt = tmpSpan;
            }
            else
            {
                decoded.MacSalt = sequenceReader.ReadOctetString();
            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(Asn1Tag.Integer))
            {

                if (!sequenceReader.TryReadInt32(out decoded.IterationCount))
                {
                    sequenceReader.ThrowIfNotEmpty();
                }

            }
            else
            {
                defaultReader = new ValueAsnReader(SharedMacData.DefaultIterationCount, AsnEncodingRules.DER);

                if (!defaultReader.TryReadInt32(out decoded.IterationCount))
                {
                    defaultReader.ThrowIfNotEmpty();
                }

            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
