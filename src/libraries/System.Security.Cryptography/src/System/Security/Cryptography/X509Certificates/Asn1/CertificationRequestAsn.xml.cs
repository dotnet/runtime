// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable SA1028 // ignore whitespace warnings for generated code
using System;
using System.Formats.Asn1;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography.X509Certificates.Asn1
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct CertificationRequestAsn
    {
        internal System.Security.Cryptography.X509Certificates.Asn1.CertificationRequestInfoAsn CertificationRequestInfo;
        internal System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn SignatureAlgorithm;
        internal ReadOnlyMemory<byte> SignatureValue;

        internal readonly void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }

        internal readonly void Encode(AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);

            CertificationRequestInfo.Encode(writer);
            SignatureAlgorithm.Encode(writer);
            writer.WriteBitString(SignatureValue.Span, 0);
            writer.PopSequence(tag);
        }

        internal static CertificationRequestAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }

        internal static CertificationRequestAsn Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            try
            {
                ValueAsnReader reader = new ValueAsnReader(encoded.Span, ruleSet);

                DecodeCore(ref reader, expectedTag, encoded, out CertificationRequestAsn decoded);
                reader.ThrowIfNotEmpty();
                return decoded;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static void Decode(ref ValueAsnReader reader, ReadOnlyMemory<byte> rebind, out CertificationRequestAsn decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
        }

        internal static void Decode(ref ValueAsnReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out CertificationRequestAsn decoded)
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

        private static void DecodeCore(ref ValueAsnReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out CertificationRequestAsn decoded)
        {
            decoded = default;
            ValueAsnReader sequenceReader = reader.ReadSequence(expectedTag);
            ReadOnlySpan<byte> rebindSpan = rebind.Span;
            int offset;
            ReadOnlySpan<byte> tmpSpan;

            System.Security.Cryptography.X509Certificates.Asn1.CertificationRequestInfoAsn.Decode(ref sequenceReader, rebind, out decoded.CertificationRequestInfo);
            System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(ref sequenceReader, rebind, out decoded.SignatureAlgorithm);

            if (sequenceReader.TryReadPrimitiveBitString(out _, out tmpSpan))
            {
                decoded.SignatureValue = rebindSpan.Overlaps(tmpSpan, out offset) ? rebind.Slice(offset, tmpSpan.Length) : tmpSpan.ToArray();
            }
            else
            {
                decoded.SignatureValue = sequenceReader.ReadBitString(out _);
            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref partial struct ValueCertificationRequestAsn
    {
        internal ReadOnlySpan<byte> CertificationRequestInfo;
        internal ReadOnlySpan<byte> SignatureAlgorithm;
        internal ReadOnlySpan<byte> SignatureValue;

        internal static void Decode(ReadOnlySpan<byte> encoded, AsnEncodingRules ruleSet, out ValueCertificationRequestAsn decoded)
        {
            Decode(Asn1Tag.Sequence, encoded, ruleSet, out decoded);
        }

        internal static void Decode(Asn1Tag expectedTag, ReadOnlySpan<byte> encoded, AsnEncodingRules ruleSet, out ValueCertificationRequestAsn decoded)
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

        internal static void Decode(scoped ref ValueAsnReader reader, out ValueCertificationRequestAsn decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, out decoded);
        }

        internal static void Decode(scoped ref ValueAsnReader reader, Asn1Tag expectedTag, out ValueCertificationRequestAsn decoded)
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

        private static void DecodeCore(scoped ref ValueAsnReader reader, Asn1Tag expectedTag, out ValueCertificationRequestAsn decoded)
        {
            decoded = default;
            ValueAsnReader sequenceReader = reader.ReadSequence(expectedTag);
            ReadOnlySpan<byte> tmpSpan;

            decoded.CertificationRequestInfo = sequenceReader.ReadEncodedValue();
            decoded.SignatureAlgorithm = sequenceReader.ReadEncodedValue();

            if (sequenceReader.TryReadPrimitiveBitString(out _, out tmpSpan))
            {
                decoded.SignatureValue = tmpSpan;
            }
            else
            {
                decoded.SignatureValue = sequenceReader.ReadBitString(out _);
            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
