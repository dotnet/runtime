// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable SA1028 // ignore whitespace warnings for generated code
using System;
using System.Formats.Asn1;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Asn1
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct MLKemPrivateKeyAsn
    {
        internal ReadOnlyMemory<byte>? Seed;
        internal ReadOnlyMemory<byte>? ExpandedKey;
        internal System.Security.Cryptography.Asn1.MLKemPrivateKeyBothAsn? Both;

#if DEBUG
        static MLKemPrivateKeyAsn()
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

            ensureUniqueTag(new Asn1Tag(TagClass.ContextSpecific, 0), "Seed");
            ensureUniqueTag(Asn1Tag.PrimitiveOctetString, "ExpandedKey");
            ensureUniqueTag(Asn1Tag.Sequence, "Both");
        }
#endif

        internal readonly void Encode(AsnWriter writer)
        {
            bool wroteValue = false;

            if (Seed.HasValue)
            {
                if (wroteValue)
                    throw new CryptographicException();

                writer.WriteOctetString(Seed.Value.Span, new Asn1Tag(TagClass.ContextSpecific, 0));
                wroteValue = true;
            }

            if (ExpandedKey.HasValue)
            {
                if (wroteValue)
                    throw new CryptographicException();

                writer.WriteOctetString(ExpandedKey.Value.Span);
                wroteValue = true;
            }

            if (Both.HasValue)
            {
                if (wroteValue)
                    throw new CryptographicException();

                Both.Value.Encode(writer);
                wroteValue = true;
            }

            if (!wroteValue)
            {
                throw new CryptographicException();
            }
        }

        internal static MLKemPrivateKeyAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            try
            {
                AsnValueReader reader = new AsnValueReader(encoded.Span, ruleSet);

                DecodeCore(ref reader, encoded, out MLKemPrivateKeyAsn decoded);
                reader.ThrowIfNotEmpty();
                return decoded;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static void Decode(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out MLKemPrivateKeyAsn decoded)
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

        private static void DecodeCore(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out MLKemPrivateKeyAsn decoded)
        {
            decoded = default;
            Asn1Tag tag = reader.PeekTag();
            ReadOnlySpan<byte> rebindSpan = rebind.Span;
            int offset;
            ReadOnlySpan<byte> tmpSpan;

            if (tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
            {

                if (reader.TryReadPrimitiveOctetString(out tmpSpan, new Asn1Tag(TagClass.ContextSpecific, 0)))
                {
                    decoded.Seed = rebindSpan.Overlaps(tmpSpan, out offset) ? rebind.Slice(offset, tmpSpan.Length) : tmpSpan.ToArray();
                }
                else
                {
                    decoded.Seed = reader.ReadOctetString(new Asn1Tag(TagClass.ContextSpecific, 0));
                }

            }
            else if (tag.HasSameClassAndValue(Asn1Tag.PrimitiveOctetString))
            {

                if (reader.TryReadPrimitiveOctetString(out tmpSpan))
                {
                    decoded.ExpandedKey = rebindSpan.Overlaps(tmpSpan, out offset) ? rebind.Slice(offset, tmpSpan.Length) : tmpSpan.ToArray();
                }
                else
                {
                    decoded.ExpandedKey = reader.ReadOctetString();
                }

            }
            else if (tag.HasSameClassAndValue(Asn1Tag.Sequence))
            {
                System.Security.Cryptography.Asn1.MLKemPrivateKeyBothAsn tmpBoth;
                System.Security.Cryptography.Asn1.MLKemPrivateKeyBothAsn.Decode(ref reader, rebind, out tmpBoth);
                decoded.Both = tmpBoth;

            }
            else
            {
                throw new CryptographicException();
            }
        }
    }
}
