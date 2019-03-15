// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.X509Certificates.Asn1
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct DistributionPointNameAsn
    {
        internal System.Security.Cryptography.Asn1.GeneralNameAsn[] FullName;
        internal ReadOnlyMemory<byte>? NameRelativeToCRLIssuer;

#if DEBUG
        static DistributionPointNameAsn()
        {
            var usedTags = new System.Collections.Generic.Dictionary<Asn1Tag, string>();
            Action<Asn1Tag, string> ensureUniqueTag = (tag, fieldName) =>
            {
                if (usedTags.TryGetValue(tag, out string existing))
                {
                    throw new InvalidOperationException($"Tag '{tag}' is in use by both '{existing}' and '{fieldName}'");
                }

                usedTags.Add(tag, fieldName);
            };
            
            ensureUniqueTag(new Asn1Tag(TagClass.ContextSpecific, 0), "FullName");
            ensureUniqueTag(new Asn1Tag(TagClass.ContextSpecific, 1), "NameRelativeToCRLIssuer");
        }
#endif

        internal void Encode(AsnWriter writer)
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

                writer.WriteEncodedValue(NameRelativeToCRLIssuer.Value.Span);
                wroteValue = true;
            }

            if (!wroteValue)
            {
                throw new CryptographicException();
            }
        }

        internal static DistributionPointNameAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            AsnReader reader = new AsnReader(encoded, ruleSet);
            
            Decode(reader, out DistributionPointNameAsn decoded);
            reader.ThrowIfNotEmpty();
            return decoded;
        }

        internal static void Decode(AsnReader reader, out DistributionPointNameAsn decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            decoded = default;
            Asn1Tag tag = reader.PeekTag();
            AsnReader collectionReader;
            
            if (tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
            {

                // Decode SEQUENCE OF for FullName
                {
                    collectionReader = reader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
                    var tmpList = new List<System.Security.Cryptography.Asn1.GeneralNameAsn>();
                    System.Security.Cryptography.Asn1.GeneralNameAsn tmpItem;

                    while (collectionReader.HasData)
                    {
                        System.Security.Cryptography.Asn1.GeneralNameAsn.Decode(collectionReader, out tmpItem); 
                        tmpList.Add(tmpItem);
                    }

                    decoded.FullName = tmpList.ToArray();
                }

            }
            else if (tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 1)))
            {
                decoded.NameRelativeToCRLIssuer = reader.ReadEncodedValue();
            }
            else
            {
                throw new CryptographicException();
            }
        }
    }
}
