// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable SA1028 // ignore whitespace warnings for generated code
using System;
using System.Formats.Asn1;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Pkcs.Asn1
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct EssCertId
    {
        internal ReadOnlyMemory<byte> Hash;
        internal System.Security.Cryptography.Pkcs.Asn1.CadesIssuerSerial? IssuerSerial;

        internal readonly void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }

        internal readonly void Encode(AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);

            writer.WriteOctetString(Hash.Span);

            if (IssuerSerial.HasValue)
            {
                IssuerSerial.Value.Encode(writer);
            }

            writer.PopSequence(tag);
        }

        internal static EssCertId Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }

        internal static EssCertId Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            try
            {
                ValueAsnReader reader = new ValueAsnReader(encoded.Span, ruleSet);

                DecodeCore(ref reader, expectedTag, encoded, out EssCertId decoded);
                reader.ThrowIfNotEmpty();
                return decoded;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static void Decode(ref ValueAsnReader reader, ReadOnlyMemory<byte> rebind, out EssCertId decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
        }

        internal static void Decode(ref ValueAsnReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out EssCertId decoded)
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

        private static void DecodeCore(ref ValueAsnReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out EssCertId decoded)
        {
            decoded = default;
            ValueAsnReader sequenceReader = reader.ReadSequence(expectedTag);
            ReadOnlySpan<byte> rebindSpan = rebind.Span;
            int offset;
            ReadOnlySpan<byte> tmpSpan;


            if (sequenceReader.TryReadPrimitiveOctetString(out tmpSpan))
            {
                decoded.Hash = rebindSpan.Overlaps(tmpSpan, out offset) ? rebind.Slice(offset, tmpSpan.Length) : tmpSpan.ToArray();
            }
            else
            {
                decoded.Hash = sequenceReader.ReadOctetString();
            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(Asn1Tag.Sequence))
            {
                System.Security.Cryptography.Pkcs.Asn1.CadesIssuerSerial tmpIssuerSerial;
                System.Security.Cryptography.Pkcs.Asn1.CadesIssuerSerial.Decode(ref sequenceReader, rebind, out tmpIssuerSerial);
                decoded.IssuerSerial = tmpIssuerSerial;

            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref partial struct ValueEssCertId
    {
        internal ReadOnlySpan<byte> Hash;
        internal ReadOnlySpan<byte> IssuerSerial;
        internal bool HasIssuerSerial;

        internal static void Decode(ReadOnlySpan<byte> encoded, AsnEncodingRules ruleSet, out ValueEssCertId decoded)
        {
            Decode(Asn1Tag.Sequence, encoded, ruleSet, out decoded);
        }

        internal static void Decode(Asn1Tag expectedTag, ReadOnlySpan<byte> encoded, AsnEncodingRules ruleSet, out ValueEssCertId decoded)
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

        internal static void Decode(scoped ref ValueAsnReader reader, out ValueEssCertId decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, out decoded);
        }

        internal static void Decode(scoped ref ValueAsnReader reader, Asn1Tag expectedTag, out ValueEssCertId decoded)
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

        private static void DecodeCore(scoped ref ValueAsnReader reader, Asn1Tag expectedTag, out ValueEssCertId decoded)
        {
            decoded = default;
            ValueAsnReader sequenceReader = reader.ReadSequence(expectedTag);
            ReadOnlySpan<byte> tmpSpan;


            if (sequenceReader.TryReadPrimitiveOctetString(out tmpSpan))
            {
                decoded.Hash = tmpSpan;
            }
            else
            {
                decoded.Hash = sequenceReader.ReadOctetString();
            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(Asn1Tag.Sequence))
            {
                decoded.IssuerSerial = sequenceReader.ReadEncodedValue();
                decoded.HasIssuerSerial = true;
            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
