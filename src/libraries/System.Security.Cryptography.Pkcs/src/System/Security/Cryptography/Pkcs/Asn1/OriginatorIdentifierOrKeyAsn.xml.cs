// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable SA1028 // ignore whitespace warnings for generated code
using System;
using System.Formats.Asn1;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Pkcs.Asn1
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct OriginatorIdentifierOrKeyAsn
    {
        internal System.Security.Cryptography.Pkcs.Asn1.IssuerAndSerialNumberAsn? IssuerAndSerialNumber;
        internal ReadOnlyMemory<byte>? SubjectKeyIdentifier;
        internal System.Security.Cryptography.Pkcs.Asn1.OriginatorPublicKeyAsn? OriginatorKey;

#if DEBUG
        static OriginatorIdentifierOrKeyAsn()
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

            ensureUniqueTag(Asn1Tag.Sequence, "IssuerAndSerialNumber");
            ensureUniqueTag(new Asn1Tag(TagClass.ContextSpecific, 0), "SubjectKeyIdentifier");
            ensureUniqueTag(new Asn1Tag(TagClass.ContextSpecific, 1), "OriginatorKey");
        }
#endif

        internal readonly void Encode(AsnWriter writer)
        {
            bool wroteValue = false;

            if (IssuerAndSerialNumber.HasValue)
            {
                if (wroteValue)
                    throw new CryptographicException();

                IssuerAndSerialNumber.Value.Encode(writer);
                wroteValue = true;
            }

            if (SubjectKeyIdentifier.HasValue)
            {
                if (wroteValue)
                    throw new CryptographicException();

                writer.WriteOctetString(SubjectKeyIdentifier.Value.Span, new Asn1Tag(TagClass.ContextSpecific, 0));
                wroteValue = true;
            }

            if (OriginatorKey.HasValue)
            {
                if (wroteValue)
                    throw new CryptographicException();

                OriginatorKey.Value.Encode(writer, new Asn1Tag(TagClass.ContextSpecific, 1));
                wroteValue = true;
            }

            if (!wroteValue)
            {
                throw new CryptographicException();
            }
        }

        internal static OriginatorIdentifierOrKeyAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            try
            {
                AsnValueReader reader = new AsnValueReader(encoded.Span, ruleSet);

                DecodeCore(ref reader, encoded, out OriginatorIdentifierOrKeyAsn decoded);
                reader.ThrowIfNotEmpty();
                return decoded;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static void Decode(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out OriginatorIdentifierOrKeyAsn decoded)
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

        private static void DecodeCore(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out OriginatorIdentifierOrKeyAsn decoded)
        {
            decoded = default;
            Asn1Tag tag = reader.PeekTag();
            ReadOnlySpan<byte> rebindSpan = rebind.Span;
            int offset;
            ReadOnlySpan<byte> tmpSpan;

            if (tag.HasSameClassAndValue(Asn1Tag.Sequence))
            {
                System.Security.Cryptography.Pkcs.Asn1.IssuerAndSerialNumberAsn tmpIssuerAndSerialNumber;
                System.Security.Cryptography.Pkcs.Asn1.IssuerAndSerialNumberAsn.Decode(ref reader, rebind, out tmpIssuerAndSerialNumber);
                decoded.IssuerAndSerialNumber = tmpIssuerAndSerialNumber;

            }
            else if (tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
            {

                if (reader.TryReadPrimitiveOctetString(out tmpSpan, new Asn1Tag(TagClass.ContextSpecific, 0)))
                {
                    decoded.SubjectKeyIdentifier = rebindSpan.Overlaps(tmpSpan, out offset) ? rebind.Slice(offset, tmpSpan.Length) : tmpSpan.ToArray();
                }
                else
                {
                    decoded.SubjectKeyIdentifier = reader.ReadOctetString(new Asn1Tag(TagClass.ContextSpecific, 0));
                }

            }
            else if (tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 1)))
            {
                System.Security.Cryptography.Pkcs.Asn1.OriginatorPublicKeyAsn tmpOriginatorKey;
                System.Security.Cryptography.Pkcs.Asn1.OriginatorPublicKeyAsn.Decode(ref reader, new Asn1Tag(TagClass.ContextSpecific, 1), rebind, out tmpOriginatorKey);
                decoded.OriginatorKey = tmpOriginatorKey;

            }
            else
            {
                throw new CryptographicException();
            }
        }
    }
}
