// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable SA1028 // ignore whitespace warnings for generated code
using System;
using System.Formats.Asn1;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Asn1
{
    [StructLayout(LayoutKind.Sequential)]
    internal ref partial struct ValueSpecifiedECDomain
    {
        internal int Version;
        internal System.Security.Cryptography.Asn1.ValueFieldID FieldID;
        internal System.Security.Cryptography.Asn1.ValueCurveAsn Curve;
        internal ReadOnlySpan<byte> Base;
        internal ReadOnlySpan<byte> Order;

        internal ReadOnlySpan<byte> Cofactor
        {
            get;
            set
            {
                HasCofactor = true;
                field = value;
            }
        }

        internal bool HasCofactor { get; private set; }
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
            writer.WriteOctetString(Base);
            writer.WriteInteger(Order);

            if (HasCofactor)
            {
                writer.WriteInteger(Cofactor);
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

        internal static ValueSpecifiedECDomain Decode(ReadOnlySpan<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }

        internal static ValueSpecifiedECDomain Decode(Asn1Tag expectedTag, ReadOnlySpan<byte> encoded, AsnEncodingRules ruleSet)
        {
            try
            {
                ValueAsnReader reader = new ValueAsnReader(encoded, ruleSet);

                ValueSpecifiedECDomain decoded = DecodeCore(ref reader, expectedTag);
                reader.ThrowIfNotEmpty();
                return decoded;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static ValueSpecifiedECDomain Decode(scoped ref ValueAsnReader reader)
        {
            return Decode(ref reader, Asn1Tag.Sequence);
        }

        internal static ValueSpecifiedECDomain Decode(scoped ref ValueAsnReader reader, Asn1Tag expectedTag)
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

        private static ValueSpecifiedECDomain DecodeCore(scoped ref ValueAsnReader reader, Asn1Tag expectedTag)
        {
            ValueSpecifiedECDomain decoded = default;
            ValueAsnReader sequenceReader = reader.ReadSequence(expectedTag);
            ReadOnlySpan<byte> tmpSpan;


            if (!sequenceReader.TryReadInt32(out decoded.Version))
            {
                sequenceReader.ThrowIfNotEmpty();
            }

            decoded.FieldID = System.Security.Cryptography.Asn1.ValueFieldID.Decode(ref sequenceReader);
            decoded.Curve = System.Security.Cryptography.Asn1.ValueCurveAsn.Decode(ref sequenceReader);

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
            return decoded;
        }
    }
}
