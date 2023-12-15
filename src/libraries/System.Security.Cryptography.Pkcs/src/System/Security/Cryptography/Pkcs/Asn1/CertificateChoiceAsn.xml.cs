// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable SA1028 // ignore whitespace warnings for generated code
using System;
using System.Formats.Asn1;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Pkcs.Asn1
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct CertificateChoiceAsn
    {
        internal ReadOnlyMemory<byte>? Certificate;
        internal ReadOnlyMemory<byte>? ExtendedCertificate;
        internal ReadOnlyMemory<byte>? AttributeCertificateV1;
        internal ReadOnlyMemory<byte>? AttributeCertificateV2;
        internal System.Security.Cryptography.Pkcs.Asn1.OtherCertificateFormat? OtherCertificateFormat;

#if DEBUG
        static CertificateChoiceAsn()
        {
            var usedTags = new System.Collections.Generic.Dictionary<Asn1Tag, string>();
            Action<Asn1Tag, string> ensureUniqueTag = (tag, fieldName) =>
            {
                if (usedTags.TryGetValue(tag, out string? existing))
                {
                    throw new InvalidOperationException($"Tag '{tag}' is in use by both '{existing}' and '{fieldName}'");
                }

                usedTags.Add(tag, fieldName);
            };

            ensureUniqueTag(new Asn1Tag((UniversalTagNumber)16), "Certificate");
            ensureUniqueTag(new Asn1Tag(TagClass.ContextSpecific, 0), "ExtendedCertificate");
            ensureUniqueTag(new Asn1Tag(TagClass.ContextSpecific, 1), "AttributeCertificateV1");
            ensureUniqueTag(new Asn1Tag(TagClass.ContextSpecific, 2), "AttributeCertificateV2");
            ensureUniqueTag(new Asn1Tag(TagClass.ContextSpecific, 3), "OtherCertificateFormat");
        }
#endif

        internal readonly void Encode(AsnWriter writer)
        {
            bool wroteValue = false;

            if (Certificate.HasValue)
            {
                if (wroteValue)
                    throw new CryptographicException();

                // Validator for tag constraint for Certificate
                {
                    if (!Asn1Tag.TryDecode(Certificate.Value.Span, out Asn1Tag validateTag, out _) ||
                        !validateTag.HasSameClassAndValue(new Asn1Tag((UniversalTagNumber)16)))
                    {
                        throw new CryptographicException();
                    }
                }

                try
                {
                    writer.WriteEncodedValue(Certificate.Value.Span);
                }
                catch (ArgumentException e)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
                }
                wroteValue = true;
            }

            if (ExtendedCertificate.HasValue)
            {
                if (wroteValue)
                    throw new CryptographicException();

                // Validator for tag constraint for ExtendedCertificate
                {
                    if (!Asn1Tag.TryDecode(ExtendedCertificate.Value.Span, out Asn1Tag validateTag, out _) ||
                        !validateTag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
                    {
                        throw new CryptographicException();
                    }
                }

                try
                {
                    writer.WriteEncodedValue(ExtendedCertificate.Value.Span);
                }
                catch (ArgumentException e)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
                }
                wroteValue = true;
            }

            if (AttributeCertificateV1.HasValue)
            {
                if (wroteValue)
                    throw new CryptographicException();

                // Validator for tag constraint for AttributeCertificateV1
                {
                    if (!Asn1Tag.TryDecode(AttributeCertificateV1.Value.Span, out Asn1Tag validateTag, out _) ||
                        !validateTag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 1)))
                    {
                        throw new CryptographicException();
                    }
                }

                try
                {
                    writer.WriteEncodedValue(AttributeCertificateV1.Value.Span);
                }
                catch (ArgumentException e)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
                }
                wroteValue = true;
            }

            if (AttributeCertificateV2.HasValue)
            {
                if (wroteValue)
                    throw new CryptographicException();

                // Validator for tag constraint for AttributeCertificateV2
                {
                    if (!Asn1Tag.TryDecode(AttributeCertificateV2.Value.Span, out Asn1Tag validateTag, out _) ||
                        !validateTag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 2)))
                    {
                        throw new CryptographicException();
                    }
                }

                try
                {
                    writer.WriteEncodedValue(AttributeCertificateV2.Value.Span);
                }
                catch (ArgumentException e)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
                }
                wroteValue = true;
            }

            if (OtherCertificateFormat.HasValue)
            {
                if (wroteValue)
                    throw new CryptographicException();

                OtherCertificateFormat.Value.Encode(writer, new Asn1Tag(TagClass.ContextSpecific, 3));
                wroteValue = true;
            }

            if (!wroteValue)
            {
                throw new CryptographicException();
            }
        }

        internal static CertificateChoiceAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            try
            {
                AsnValueReader reader = new AsnValueReader(encoded.Span, ruleSet);

                DecodeCore(ref reader, encoded, out CertificateChoiceAsn decoded);
                reader.ThrowIfNotEmpty();
                return decoded;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static void Decode(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out CertificateChoiceAsn decoded)
        {
            try
            {
                DecodeCore(ref reader, rebind, out decoded);
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        private static void DecodeCore(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out CertificateChoiceAsn decoded)
        {
            decoded = default;
            Asn1Tag tag = reader.PeekTag();
            ReadOnlySpan<byte> rebindSpan = rebind.Span;
            int offset;
            ReadOnlySpan<byte> tmpSpan;

            if (tag.HasSameClassAndValue(new Asn1Tag((UniversalTagNumber)16)))
            {
                tmpSpan = reader.ReadEncodedValue();
                decoded.Certificate = rebindSpan.Overlaps(tmpSpan, out offset) ? rebind.Slice(offset, tmpSpan.Length) : tmpSpan.ToArray();
            }
            else if (tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
            {
                tmpSpan = reader.ReadEncodedValue();
                decoded.ExtendedCertificate = rebindSpan.Overlaps(tmpSpan, out offset) ? rebind.Slice(offset, tmpSpan.Length) : tmpSpan.ToArray();
            }
            else if (tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 1)))
            {
                tmpSpan = reader.ReadEncodedValue();
                decoded.AttributeCertificateV1 = rebindSpan.Overlaps(tmpSpan, out offset) ? rebind.Slice(offset, tmpSpan.Length) : tmpSpan.ToArray();
            }
            else if (tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 2)))
            {
                tmpSpan = reader.ReadEncodedValue();
                decoded.AttributeCertificateV2 = rebindSpan.Overlaps(tmpSpan, out offset) ? rebind.Slice(offset, tmpSpan.Length) : tmpSpan.ToArray();
            }
            else if (tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 3)))
            {
                System.Security.Cryptography.Pkcs.Asn1.OtherCertificateFormat tmpOtherCertificateFormat;
                System.Security.Cryptography.Pkcs.Asn1.OtherCertificateFormat.Decode(ref reader, new Asn1Tag(TagClass.ContextSpecific, 3), rebind, out tmpOtherCertificateFormat);
                decoded.OtherCertificateFormat = tmpOtherCertificateFormat;

            }
            else
            {
                throw new CryptographicException();
            }
        }
    }
}
