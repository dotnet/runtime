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

        internal static void Decode(ReadOnlySpan<byte> encoded, AsnEncodingRules ruleSet, out ValueECPrivateKey decoded)
        {
            Decode(Asn1Tag.Sequence, encoded, ruleSet, out decoded);
        }

        internal static void Decode(Asn1Tag expectedTag, ReadOnlySpan<byte> encoded, AsnEncodingRules ruleSet, out ValueECPrivateKey decoded)
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

        internal static void Decode(scoped ref ValueAsnReader reader, out ValueECPrivateKey decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, out decoded);
        }

        internal static void Decode(scoped ref ValueAsnReader reader, Asn1Tag expectedTag, out ValueECPrivateKey decoded)
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

        private static void DecodeCore(scoped ref ValueAsnReader reader, Asn1Tag expectedTag, out ValueECPrivateKey decoded)
        {
            decoded = default;
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
                System.Security.Cryptography.Asn1.ValueECDomainParameters tmpParameters;
                System.Security.Cryptography.Asn1.ValueECDomainParameters.Decode(ref explicitReader, out tmpParameters);
                decoded.Parameters = tmpParameters;

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
        }
    }
}
