// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable SA1028 // ignore whitespace warnings for generated code
using System;
using System.Formats.Asn1;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Pkcs.Asn1
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct RecipientInfoAsn
    {
        internal System.Security.Cryptography.Pkcs.Asn1.KeyTransRecipientInfoAsn? Ktri;
        internal System.Security.Cryptography.Pkcs.Asn1.KeyAgreeRecipientInfoAsn? Kari;

#if DEBUG
        static RecipientInfoAsn()
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

            ensureUniqueTag(Asn1Tag.Sequence, "Ktri");
            ensureUniqueTag(new Asn1Tag(TagClass.ContextSpecific, 1), "Kari");
        }
#endif

        internal readonly void Encode(AsnWriter writer)
        {
            bool wroteValue = false;

            if (Ktri.HasValue)
            {
                if (wroteValue)
                    throw new CryptographicException();

                Ktri.Value.Encode(writer);
                wroteValue = true;
            }

            if (Kari.HasValue)
            {
                if (wroteValue)
                    throw new CryptographicException();

                Kari.Value.Encode(writer, new Asn1Tag(TagClass.ContextSpecific, 1));
                wroteValue = true;
            }

            if (!wroteValue)
            {
                throw new CryptographicException();
            }
        }

        internal static RecipientInfoAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            try
            {
                AsnValueReader reader = new AsnValueReader(encoded.Span, ruleSet);

                DecodeCore(ref reader, encoded, out RecipientInfoAsn decoded);
                reader.ThrowIfNotEmpty();
                return decoded;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static void Decode(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out RecipientInfoAsn decoded)
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

        private static void DecodeCore(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out RecipientInfoAsn decoded)
        {
            decoded = default;
            Asn1Tag tag = reader.PeekTag();

            if (tag.HasSameClassAndValue(Asn1Tag.Sequence))
            {
                System.Security.Cryptography.Pkcs.Asn1.KeyTransRecipientInfoAsn tmpKtri;
                System.Security.Cryptography.Pkcs.Asn1.KeyTransRecipientInfoAsn.Decode(ref reader, rebind, out tmpKtri);
                decoded.Ktri = tmpKtri;

            }
            else if (tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 1)))
            {
                System.Security.Cryptography.Pkcs.Asn1.KeyAgreeRecipientInfoAsn tmpKari;
                System.Security.Cryptography.Pkcs.Asn1.KeyAgreeRecipientInfoAsn.Decode(ref reader, new Asn1Tag(TagClass.ContextSpecific, 1), rebind, out tmpKari);
                decoded.Kari = tmpKari;

            }
            else
            {
                throw new CryptographicException();
            }
        }
    }
}
