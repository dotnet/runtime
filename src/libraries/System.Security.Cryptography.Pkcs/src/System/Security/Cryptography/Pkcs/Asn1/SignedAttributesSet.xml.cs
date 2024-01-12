// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable SA1028 // ignore whitespace warnings for generated code
using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Pkcs
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct SignedAttributesSet
    {
        internal System.Security.Cryptography.Asn1.AttributeAsn[]? SignedAttributes;

#if DEBUG
        static SignedAttributesSet()
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

            ensureUniqueTag(new Asn1Tag(TagClass.ContextSpecific, 0), "SignedAttributes");
        }
#endif

        internal readonly void Encode(AsnWriter writer)
        {
            bool wroteValue = false;

            if (SignedAttributes != null)
            {
                if (wroteValue)
                    throw new CryptographicException();


                writer.PushSetOf(new Asn1Tag(TagClass.ContextSpecific, 0));
                for (int i = 0; i < SignedAttributes.Length; i++)
                {
                    SignedAttributes[i].Encode(writer);
                }
                writer.PopSetOf(new Asn1Tag(TagClass.ContextSpecific, 0));

                wroteValue = true;
            }

            if (!wroteValue)
            {
                throw new CryptographicException();
            }
        }

        internal static SignedAttributesSet Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            try
            {
                AsnValueReader reader = new AsnValueReader(encoded.Span, ruleSet);

                DecodeCore(ref reader, encoded, out SignedAttributesSet decoded);
                reader.ThrowIfNotEmpty();
                return decoded;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static void Decode(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out SignedAttributesSet decoded)
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

        private static void DecodeCore(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out SignedAttributesSet decoded)
        {
            decoded = default;
            Asn1Tag tag = reader.PeekTag();
            AsnValueReader collectionReader;

            if (tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
            {

                // Decode SEQUENCE OF for SignedAttributes
                {
                    collectionReader = reader.ReadSetOf(new Asn1Tag(TagClass.ContextSpecific, 0));
                    var tmpList = new List<System.Security.Cryptography.Asn1.AttributeAsn>();
                    System.Security.Cryptography.Asn1.AttributeAsn tmpItem;

                    while (collectionReader.HasData)
                    {
                        System.Security.Cryptography.Asn1.AttributeAsn.Decode(ref collectionReader, rebind, out tmpItem);
                        tmpList.Add(tmpItem);
                    }

                    decoded.SignedAttributes = tmpList.ToArray();
                }

            }
            else
            {
                throw new CryptographicException();
            }
        }
    }
}
