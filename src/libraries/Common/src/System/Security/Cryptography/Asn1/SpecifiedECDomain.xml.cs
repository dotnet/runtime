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
    internal partial struct SpecifiedECDomain
    {
        internal byte Version;
        internal System.Security.Cryptography.Asn1.FieldID FieldID;
        internal System.Security.Cryptography.Asn1.CurveAsn Curve;
        internal ReadOnlyMemory<byte> Base;
        internal ReadOnlyMemory<byte> Order;
        internal ReadOnlyMemory<byte>? Cofactor;
        internal Oid Hash;
      
        internal void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }
    
        internal void Encode(AsnWriter writer, Asn1Tag tag)
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

            writer.WriteObjectIdentifier(Hash);
            writer.PopSequence(tag);
        }

        internal static SpecifiedECDomain Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }
        
        internal static SpecifiedECDomain Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            AsnReader reader = new AsnReader(encoded, ruleSet);
            
            Decode(reader, expectedTag, out SpecifiedECDomain decoded);
            reader.ThrowIfNotEmpty();
            return decoded;
        }

        internal static void Decode(AsnReader reader, out SpecifiedECDomain decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            Decode(reader, Asn1Tag.Sequence, out decoded);
        }

        internal static void Decode(AsnReader reader, Asn1Tag expectedTag, out SpecifiedECDomain decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            decoded = default;
            AsnReader sequenceReader = reader.ReadSequence(expectedTag);
            

            if (!sequenceReader.TryReadUInt8(out decoded.Version))
            {
                sequenceReader.ThrowIfNotEmpty();
            }

            System.Security.Cryptography.Asn1.FieldID.Decode(sequenceReader, out decoded.FieldID);
            System.Security.Cryptography.Asn1.CurveAsn.Decode(sequenceReader, out decoded.Curve);

            if (sequenceReader.TryReadPrimitiveOctetStringBytes(out ReadOnlyMemory<byte> tmpBase))
            {
                decoded.Base = tmpBase;
            }
            else
            {
                decoded.Base = sequenceReader.ReadOctetString();
            }

            decoded.Order = sequenceReader.ReadIntegerBytes();

            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(Asn1Tag.Integer))
            {
                decoded.Cofactor = sequenceReader.ReadIntegerBytes();
            }

            decoded.Hash = sequenceReader.ReadObjectIdentifier();

            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
