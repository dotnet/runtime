// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable SA1028 // ignore whitespace warnings for generated code
using System;
using System.Formats.Asn1;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography.X509Certificates.Asn1
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct CertificateTemplateAsn
    {
        internal string TemplateID;
        internal int TemplateMajorVersion;
        internal int? TemplateMinorVersion;

        internal readonly void Encode(AsnWriter writer)
        {
            Encode(writer, Asn1Tag.Sequence);
        }

        internal readonly void Encode(AsnWriter writer, Asn1Tag tag)
        {
            writer.PushSequence(tag);

            try
            {
                writer.WriteObjectIdentifier(TemplateID);
            }
            catch (ArgumentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
            writer.WriteInteger(TemplateMajorVersion);

            if (TemplateMinorVersion.HasValue)
            {
                writer.WriteInteger(TemplateMinorVersion.Value);
            }

            writer.PopSequence(tag);
        }

        internal static CertificateTemplateAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            return Decode(Asn1Tag.Sequence, encoded, ruleSet);
        }

        internal static CertificateTemplateAsn Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            try
            {
                ValueAsnReader reader = new ValueAsnReader(encoded.Span, ruleSet);

                DecodeCore(ref reader, expectedTag, out CertificateTemplateAsn decoded);
                reader.ThrowIfNotEmpty();
                return decoded;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static void Decode(ref ValueAsnReader reader, out CertificateTemplateAsn decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, out decoded);
        }

        internal static void Decode(ref ValueAsnReader reader, Asn1Tag expectedTag, out CertificateTemplateAsn decoded)
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

        private static void DecodeCore(ref ValueAsnReader reader, Asn1Tag expectedTag, out CertificateTemplateAsn decoded)
        {
            decoded = default;
            ValueAsnReader sequenceReader = reader.ReadSequence(expectedTag);

            decoded.TemplateID = sequenceReader.ReadObjectIdentifier();

            if (!sequenceReader.TryReadInt32(out decoded.TemplateMajorVersion))
            {
                sequenceReader.ThrowIfNotEmpty();
            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(Asn1Tag.Integer))
            {

                if (sequenceReader.TryReadInt32(out int tmpTemplateMinorVersion))
                {
                    decoded.TemplateMinorVersion = tmpTemplateMinorVersion;
                }
                else
                {
                    sequenceReader.ThrowIfNotEmpty();
                }

            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref partial struct ValueCertificateTemplateAsn
    {
        internal string TemplateID;
        internal int TemplateMajorVersion;
        internal int? TemplateMinorVersion;

        internal static void Decode(ReadOnlySpan<byte> encoded, AsnEncodingRules ruleSet, out ValueCertificateTemplateAsn decoded)
        {
            Decode(Asn1Tag.Sequence, encoded, ruleSet, out decoded);
        }

        internal static void Decode(Asn1Tag expectedTag, ReadOnlySpan<byte> encoded, AsnEncodingRules ruleSet, out ValueCertificateTemplateAsn decoded)
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

        internal static void Decode(scoped ref ValueAsnReader reader, out ValueCertificateTemplateAsn decoded)
        {
            Decode(ref reader, Asn1Tag.Sequence, out decoded);
        }

        internal static void Decode(scoped ref ValueAsnReader reader, Asn1Tag expectedTag, out ValueCertificateTemplateAsn decoded)
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

        private static void DecodeCore(scoped ref ValueAsnReader reader, Asn1Tag expectedTag, out ValueCertificateTemplateAsn decoded)
        {
            decoded = default;
            ValueAsnReader sequenceReader = reader.ReadSequence(expectedTag);

            decoded.TemplateID = sequenceReader.ReadObjectIdentifier();

            if (!sequenceReader.TryReadInt32(out decoded.TemplateMajorVersion))
            {
                sequenceReader.ThrowIfNotEmpty();
            }


            if (sequenceReader.HasData && sequenceReader.PeekTag().HasSameClassAndValue(Asn1Tag.Integer))
            {

                if (sequenceReader.TryReadInt32(out int tmpTemplateMinorVersion))
                {
                    decoded.TemplateMinorVersion = tmpTemplateMinorVersion;
                }
                else
                {
                    sequenceReader.ThrowIfNotEmpty();
                }

            }


            sequenceReader.ThrowIfNotEmpty();
        }
    }
}
