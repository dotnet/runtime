// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            AsnReader reader = new AsnReader(encoded, ruleSet);
            
            Decode(reader, expectedTag, out CurveAsn decoded);
            reader.ThrowIfNotEmpty();
            return decoded;
        }

        internal static void Decode(AsnReader reader, out CurveAsn decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            Decode(reader, Asn1Tag.Sequence, out decoded);
        }

        internal static void Decode(AsnReader reader, Asn1Tag expectedTag, out CurveAsn decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            decoded = default;
            AsnReader sequenceReader = reader.ReadSequence(expectedTag);
            

            if (sequenceReader.TryReadPrimitiveOctetStringBytes(out ReadOnlyMemory<byte> tmpA))
            {
                decoded.A = tmpA;
            }
            else
            {
                decoded.A = sequenceReader.ReadOctetString();
            }


            if (sequenceReader.TryReadPrimitiveOctetStringBytes(out ReadOnlyMemory<byte> tmpB))
            {
                decoded.B = tmpB;
            }
            else
            {
                decoded.B = sequenceReader.ReadOctetString();
            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(Asn1Tag.PrimitiveBitString))
            {

                if (sequenceReader.TryReadPrimitiveBitStringValue(out _, out ReadOnlyMemory<byte> tmpSeed))
                {
                    decoded.Seed = tmpSeed;
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
