// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable SA1028 // ignore whitespace warnings for generated code
using System;
using System.Formats.Asn1;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Asn1
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct SpecifiedECDomain
    {
        internal int Version;
        internal System.Security.Cryptography.Asn1.FieldID FieldID;
        internal System.Security.Cryptography.Asn1.CurveAsn Curve;
        internal ReadOnlyMemory<byte> Base;
        internal ReadOnlyMemory<byte> Order;
        internal ReadOnlyMemory<byte>? Cofactor;
        internal string? Hash;

        internal readonly void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }

        internal readonly void Encode(AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);

            writer.WriteInteger(Version);
            FieldID.Encode(writer);
            Curve.Encode(writer);
            writer.WriteOctetString(Base.Span);
            writer.WriteInteger(Order.Span);

            if (Cofactor.HasValue)
            {
                writer.WriteInteger(Cofactor.Value.Span);
            }


            if (Hash != null)
            {
                try
                {
                    writer.WriteObjectIdentifier(Hash);
                }
                catch (ArgumentException e)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
                }
            }

            writer.PopSequence(tag);
        }

        internal static SpecifiedECDomain Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }

        internal static SpecifiedECDomain Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            try
            {
                ValueAsnReader reader = new ValueAsnReader(encoded.Span, ruleSet);

                DecodeCore(ref reader, expectedTag, encoded, out SpecifiedECDomain decoded);
                reader.ThrowIfNotEmpty();
                return decoded;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static void Decode(ref ValueAsnReader reader, ReadOnlyMemory<byte> rebind, out SpecifiedECDomain decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
        }

        internal static void Decode(ref ValueAsnReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out SpecifiedECDomain decoded)
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

        private static void DecodeCore(ref ValueAsnReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out SpecifiedECDomain decoded)
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

            System.Security.Cryptography.Asn1.FieldID.Decode(ref sequenceReader, rebind, out decoded.FieldID);
            System.Security.Cryptography.Asn1.CurveAsn.Decode(ref sequenceReader, rebind, out decoded.Curve);

            if (sequenceReader.TryReadPrimitiveOctetString(out tmpSpan))
            {
                decoded.Base = rebindSpan.Overlaps(tmpSpan, out offset) ? rebind.Slice(offset, tmpSpan.Length) : tmpSpan.ToArray();
            }
            else
            {
                decoded.Base = sequenceReader.ReadOctetString();
            }

            tmpSpan = sequenceReader.ReadIntegerBytes();
            decoded.Order = rebindSpan.Overlaps(tmpSpan, out offset) ? rebind.Slice(offset, tmpSpan.Length) : tmpSpan.ToArray();

            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(Asn1Tag.Integer))
            {
                tmpSpan = sequenceReader.ReadIntegerBytes();
                decoded.Cofactor = rebindSpan.Overlaps(tmpSpan, out offset) ? rebind.Slice(offset, tmpSpan.Length) : tmpSpan.ToArray();
            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(Asn1Tag.ObjectIdentifier))
            {
                decoded.Hash = sequenceReader.ReadObjectIdentifier();
            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref partial struct ValueSpecifiedECDomain
    {
        internal int Version;
        internal ReadOnlySpan<byte> FieldID;
        internal ReadOnlySpan<byte> Curve;
        internal ReadOnlySpan<byte> Base;
        internal ReadOnlySpan<byte> Order;
        internal ReadOnlySpan<byte> Cofactor;
        internal bool HasCofactor;
        internal string? Hash;

        internal static void Decode(ReadOnlySpan<byte> encoded, AsnEncodingRules ruleSet, out ValueSpecifiedECDomain decoded)
        {
            Decode(Asn1Tag.Sequence, encoded, ruleSet, out decoded);
        }

        internal static void Decode(Asn1Tag expectedTag, ReadOnlySpan<byte> encoded, AsnEncodingRules ruleSet, out ValueSpecifiedECDomain decoded)
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

        internal static void Decode(scoped ref ValueAsnReader reader, out ValueSpecifiedECDomain decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, out decoded);
        }

        internal static void Decode(scoped ref ValueAsnReader reader, Asn1Tag expectedTag, out ValueSpecifiedECDomain decoded)
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

        private static void DecodeCore(scoped ref ValueAsnReader reader, Asn1Tag expectedTag, out ValueSpecifiedECDomain decoded)
        {
            decoded = default;
            ValueAsnReader sequenceReader = reader.ReadSequence(expectedTag);
            ReadOnlySpan<byte> tmpSpan;


            if (!sequenceReader.TryReadInt32(out decoded.Version))
            {
                sequenceReader.ThrowIfNotEmpty();
            }

            decoded.FieldID = sequenceReader.ReadEncodedValue();
            decoded.Curve = sequenceReader.ReadEncodedValue();

            if (sequenceReader.TryReadPrimitiveOctetString(out tmpSpan))
            {
                decoded.Base = tmpSpan;
            }
            else
            {
                decoded.Base = sequenceReader.ReadOctetString();
            }

            decoded.Order = sequenceReader.ReadIntegerBytes();

            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(Asn1Tag.Integer))
            {
                decoded.Cofactor = sequenceReader.ReadIntegerBytes();
                decoded.HasCofactor = true;
            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(Asn1Tag.ObjectIdentifier))
            {
                decoded.Hash = sequenceReader.ReadObjectIdentifier();
            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
