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

            System.Security.Cryptography.Asn1.ValueFieldID.Decode(ref sequenceReader, out decoded.FieldID);
            System.Security.Cryptography.Asn1.ValueCurveAsn.Decode(ref sequenceReader, out decoded.Curve);

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
