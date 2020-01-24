// Licensed to the .NET Foundation under one or more agreements.
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
    internal partial struct CurveAsn
    {
        internal ReadOnlyMemory<byte> A;
        internal ReadOnlyMemory<byte> B;
        internal ReadOnlyMemory<byte>? Seed;

        internal void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }

        internal void Encode(AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);

            writer.WriteOctetString(A.Span);
            writer.WriteOctetString(B.Span);

            if (Seed.HasValue)
            {
                writer.WriteBitString(Seed.Value.Span);
            }

            writer.PopSequence(tag);
        }

        internal static CurveAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }

        internal static CurveAsn Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            AsnValueReader reader = new AsnValueReader(encoded.Span, ruleSet);

            Decode(ref reader, expectedTag, encoded, out CurveAsn decoded);
            reader.ThrowIfNotEmpty();
            return decoded;
        }

        internal static void Decode(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out CurveAsn decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
        }

        internal static void Decode(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out CurveAsn decoded)
        {
            decoded = default;
            AsnValueReader sequenceReader = reader.ReadSequence(expectedTag);
            ReadOnlySpan<byte> rebindSpan = rebind.Span;
            int offset;
            ReadOnlySpan<byte> tmpSpan;


            if (sequenceReader.TryReadPrimitiveOctetStringBytes(out tmpSpan))
            {
                decoded.A = rebindSpan.Overlaps(tmpSpan, out offset) ? rebind.Slice(offset, tmpSpan.Length) : tmpSpan.ToArray();
            }
            else
            {
                decoded.A = sequenceReader.ReadOctetString();
            }


            if (sequenceReader.TryReadPrimitiveOctetStringBytes(out tmpSpan))
            {
                decoded.B = rebindSpan.Overlaps(tmpSpan, out offset) ? rebind.Slice(offset, tmpSpan.Length) : tmpSpan.ToArray();
            }
            else
            {
                decoded.B = sequenceReader.ReadOctetString();
            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(Asn1Tag.PrimitiveBitString))
            {

                if (sequenceReader.TryReadPrimitiveBitStringValue(out _, out tmpSpan))
                {
                    decoded.Seed = rebindSpan.Overlaps(tmpSpan, out offset) ? rebind.Slice(offset, tmpSpan.Length) : tmpSpan.ToArray();
                }
                else
                {
                    decoded.Seed = sequenceReader.ReadBitString(out _);
                }

            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
