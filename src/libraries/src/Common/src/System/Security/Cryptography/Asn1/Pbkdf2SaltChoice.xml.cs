// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.Asn1
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct Pbkdf2SaltChoice
    {
        internal ReadOnlyMemory<byte>? Specified;
        internal System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn? OtherSource;

#if DEBUG
        static Pbkdf2SaltChoice()
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
            
            ensureUniqueTag(Asn1Tag.PrimitiveOctetString, "Specified");
            ensureUniqueTag(Asn1Tag.Sequence, "OtherSource");
        }
#endif

        internal void Encode(AsnWriter writer)
        {
            bool wroteValue = false; 
            
            if (Specified.HasValue)
            {
                if (wroteValue)
                    throw new CryptographicException();
                
                writer.WriteOctetString(Specified.Value.Span);
                wroteValue = true;
            }

            if (OtherSource.HasValue)
            {
                if (wroteValue)
                    throw new CryptographicException();
                
                OtherSource.Value.Encode(writer);
                wroteValue = true;
            }

            if (!wroteValue)
            {
                throw new CryptographicException();
            }
        }

        internal static Pbkdf2SaltChoice Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            AsnReader reader = new AsnReader(encoded, ruleSet);
            
            Decode(reader, out Pbkdf2SaltChoice decoded);
            reader.ThrowIfNotEmpty();
            return decoded;
        }

        internal static void Decode(AsnReader reader, out Pbkdf2SaltChoice decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            decoded = default;
            Asn1Tag tag = reader.PeekTag();
            
            if (tag.HasSameClassAndValue(Asn1Tag.PrimitiveOctetString))
            {

                if (reader.TryReadPrimitiveOctetStringBytes(out ReadOnlyMemory<byte> tmpSpecified))
                {
                    decoded.Specified = tmpSpecified;
                }
                else
                {
                    decoded.Specified = reader.ReadOctetString();
                }

            }
            else if (tag.HasSameClassAndValue(Asn1Tag.Sequence))
            {
                System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn tmpOtherSource;
                System.Security.Cryptography.Asn1.AlgorithmIdentifierAsn.Decode(reader, out tmpOtherSource);
                decoded.OtherSource = tmpOtherSource;

            }
            else
            {
                throw new CryptographicException();
            }
        }
    }
}
