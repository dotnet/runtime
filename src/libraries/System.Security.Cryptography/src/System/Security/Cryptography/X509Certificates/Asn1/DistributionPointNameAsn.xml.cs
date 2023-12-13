// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable SA1028 // ignore whitespace warnings for generated code
using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography.X509Certificates.Asn1
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct DistributionPointNameAsn
    {
        internal System.Security.Cryptography.Asn1.GeneralNameAsn[]? FullName;
        internal ReadOnlyMemory<byte>? NameRelativeToCRLIssuer;

#if DEBUG
        static DistributionPointNameAsn()
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

            ensureUniqueTag(new Asn1Tag(TagClass.ContextSpecific, 0), "FullName");
            ensureUniqueTag(new Asn1Tag(TagClass.ContextSpecific, 1), "NameRelativeToCRLIssuer");
        }
#endif

        internal readonly void Encode(AsnWriter writer)
        {
            bool wroteValue = false;

            if (FullName != null)
            {
                if (wroteValue)
                    throw new CryptographicException();


                writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
                for (int i = 0; i < FullName.Length; i++)
                {
                    FullName[i].Encode(writer);
                }
                writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 0));

                wroteValue = true;
            }

            if (NameRelativeToCRLIssuer.HasValue)
            {
                if (wroteValue)
                    throw new CryptographicException();

                // Validator for tag constraint for NameRelativeToCRLIssuer
                {
                    if (!Asn1Tag.TryDecode(NameRelativeToCRLIssuer.Value.Span, out Asn1Tag validateTag, out _) ||
                        !validateTag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 1)))
                    {
                        throw new CryptographicException();
                    }
                }

                try
                {
                    writer.WriteEncodedValue(NameRelativeToCRLIssuer.Value.Span);
                }
                catch (ArgumentException e)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
                }
                wroteValue = true;
            }

            if (!wroteValue)
            {
                throw new CryptographicException();
            }
        }

        internal static DistributionPointNameAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            try
            {
                AsnValueReader reader = new AsnValueReader(encoded.Span, ruleSet);

                DecodeCore(ref reader, encoded, out DistributionPointNameAsn decoded);
                reader.ThrowIfNotEmpty();
                return decoded;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static void Decode(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out DistributionPointNameAsn decoded)
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

        private static void DecodeCore(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out DistributionPointNameAsn decoded)
        {
            decoded = default;
            Asn1Tag tag = reader.PeekTag();
            AsnValueReader collectionReader;
            ReadOnlySpan<byte> rebindSpan = rebind.Span;
            int offset;
            ReadOnlySpan<byte> tmpSpan;

            if (tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
            {

                // Decode SEQUENCE OF for FullName
                {
                    collectionReader = reader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
                    var tmpList = new List<System.Security.Cryptography.Asn1.GeneralNameAsn>();
                    System.Security.Cryptography.Asn1.GeneralNameAsn tmpItem;

                    while (collectionReader.HasData)
                    {
                        System.Security.Cryptography.Asn1.GeneralNameAsn.Decode(ref collectionReader, rebind, out tmpItem);
                        tmpList.Add(tmpItem);
                    }

                    decoded.FullName = tmpList.ToArray();
                }

            }
            else if (tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 1)))
            {
                tmpSpan = reader.ReadEncodedValue();
                decoded.NameRelativeToCRLIssuer = rebindSpan.Overlaps(tmpSpan, out offset) ? rebind.Slice(offset, tmpSpan.Length) : tmpSpan.ToArray();
            }
            else
            {
                throw new CryptographicException();
            }
        }
    }
}
