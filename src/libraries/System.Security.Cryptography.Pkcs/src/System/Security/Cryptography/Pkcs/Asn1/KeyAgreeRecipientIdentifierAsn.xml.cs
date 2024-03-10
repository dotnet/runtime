// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable SA1028 // ignore whitespace warnings for generated code
using System;
using System.Formats.Asn1;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Pkcs.Asn1
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct KeyAgreeRecipientIdentifierAsn
    {
        internal System.Security.Cryptography.Pkcs.Asn1.IssuerAndSerialNumberAsn? IssuerAndSerialNumber;
        internal System.Security.Cryptography.Pkcs.Asn1.RecipientKeyIdentifier? RKeyId;

#if DEBUG
        static KeyAgreeRecipientIdentifierAsn()
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
            ensureUniqueTag(new Asn1Tag(TagClass.ContextSpecific, 0), "RKeyId");
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

            if (RKeyId.HasValue)
            {
                if (wroteValue)
                    throw new CryptographicException();

                RKeyId.Value.Encode(writer, new Asn1Tag(TagClass.ContextSpecific, 0));
                wroteValue = true;
            }

            if (!wroteValue)
            {
                throw new CryptographicException();
            }
        }

        internal static KeyAgreeRecipientIdentifierAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            try
            {
                AsnValueReader reader = new AsnValueReader(encoded.Span, ruleSet);

                DecodeCore(ref reader, encoded, out KeyAgreeRecipientIdentifierAsn decoded);
                reader.ThrowIfNotEmpty();
                return decoded;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static void Decode(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out KeyAgreeRecipientIdentifierAsn decoded)
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

        private static void DecodeCore(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out KeyAgreeRecipientIdentifierAsn decoded)
        {
            decoded = default;
            Asn1Tag tag = reader.PeekTag();

            if (tag.HasSameClassAndValue(Asn1Tag.Sequence))
            {
                System.Security.Cryptography.Pkcs.Asn1.IssuerAndSerialNumberAsn tmpIssuerAndSerialNumber;
                System.Security.Cryptography.Pkcs.Asn1.IssuerAndSerialNumberAsn.Decode(ref reader, rebind, out tmpIssuerAndSerialNumber);
                decoded.IssuerAndSerialNumber = tmpIssuerAndSerialNumber;

            }
            else if (tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
            {
                System.Security.Cryptography.Pkcs.Asn1.RecipientKeyIdentifier tmpRKeyId;
                System.Security.Cryptography.Pkcs.Asn1.RecipientKeyIdentifier.Decode(ref reader, new Asn1Tag(TagClass.ContextSpecific, 0), rebind, out tmpRKeyId);
                decoded.RKeyId = tmpRKeyId;

            }
            else
            {
                throw new CryptographicException();
            }
        }
    }
}
