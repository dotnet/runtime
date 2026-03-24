// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable SA1028 // ignore whitespace warnings for generated code
using System;
using System.Formats.Asn1;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Asn1
{
    [StructLayout(LayoutKind.Sequential)]
    internal ref partial struct ValueECPrivateKey
    {
        internal int Version;
        internal ReadOnlySpan<byte> PrivateKey;

        internal System.Security.Cryptography.Asn1.ValueECDomainParameters Parameters
        {
            get;
            set
            {
                HasParameters = true;
                field = value;
            }
        }

        internal bool HasParameters { get; private set; }

        internal ReadOnlySpan<byte> PublicKey
        {
            get;
            set
            {
                HasPublicKey = true;
                field = value;
            }
        }

        internal bool HasPublicKey { get; private set; }

        internal readonly void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }

        internal readonly void Encode(AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);

            writer.WriteInteger(Version);
            writer.WriteOctetString(PrivateKey);

            if (HasParameters)
            {
                writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
                Parameters.Encode(writer);
                writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
            }


            if (HasPublicKey)
            {
                writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 1));
                writer.WriteBitString(PublicKey, 0);
                writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 1));
            }

            writer.PopSequence(tag);
        }

        internal static ValueECPrivateKey Decode(ReadOnlySpan<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }

        internal static ValueECPrivateKey Decode(Asn1Tag expectedTag, ReadOnlySpan<byte> encoded, AsnEncodingRules ruleSet)
        {
            try
            {
                ValueAsnReader reader = new ValueAsnReader(encoded, ruleSet);

                ValueECPrivateKey decoded = DecodeCore(ref reader, expectedTag);
                reader.ThrowIfNotEmpty();
                return decoded;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static ValueECPrivateKey Decode(scoped ref ValueAsnReader reader)
        {
            return Decode(ref reader, Asn1Tag.Sequence);
        }

        internal static ValueECPrivateKey Decode(scoped ref ValueAsnReader reader, Asn1Tag expectedTag)
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

        private static ValueECPrivateKey DecodeCore(scoped ref ValueAsnReader reader, Asn1Tag expectedTag)
        {
            ValueECPrivateKey decoded = default;
            ValueAsnReader sequenceReader = reader.ReadSequence(expectedTag);
            ValueAsnReader explicitReader;
            ReadOnlySpan<byte> tmpSpan;


            if (!sequenceReader.TryReadInt32(out decoded.Version))
            {
                sequenceReader.ThrowIfNotEmpty();
            }


            if (sequenceReader.TryReadPrimitiveOctetString(out tmpSpan))
            {
                decoded.PrivateKey = tmpSpan;
            }
            else
            {
                decoded.PrivateKey = sequenceReader.ReadOctetString();
            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
            {
                explicitReader = sequenceReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
                decoded.Parameters = System.Security.Cryptography.Asn1.ValueECDomainParameters.Decode(ref explicitReader);
                decoded.HasParameters = true;
                explicitReader.ThrowIfNotEmpty();
            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 1)))
            {
                explicitReader = sequenceReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 1));

                if (explicitReader.TryReadPrimitiveBitString(out _, out tmpSpan))
                {
                    decoded.PublicKey = tmpSpan;
                }
                else
                {
                    decoded.PublicKey = explicitReader.ReadBitString(out _);
                }

                decoded.HasPublicKey = true;
                explicitReader.ThrowIfNotEmpty();
            }


            sequenceReader.ThrowIfNotEmpty();
            return decoded;
        }
    }
}
