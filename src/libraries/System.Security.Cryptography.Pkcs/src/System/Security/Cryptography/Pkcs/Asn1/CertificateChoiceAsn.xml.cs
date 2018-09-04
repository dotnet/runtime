using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.Pkcs.Asn1
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct CertificateChoiceAsn
    {
        internal ReadOnlyMemory<byte>? Certificate;

#if DEBUG
        static CertificateChoiceAsn()
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
            
            ensureUniqueTag(new Asn1Tag((UniversalTagNumber)16), "Certificate");
        }
#endif

        internal void Encode(AsnWriter writer)
        {
            bool wroteValue = false; 
            
            if (Certificate.HasValue)
            {
                if (wroteValue)
                    throw new CryptographicException();
                
                // Validator for tag constraint for Certificate
                {
                    if (!Asn1Tag.TryParse(Certificate.Value.Span, out Asn1Tag validateTag, out _) ||
                        !validateTag.HasSameClassAndValue(new Asn1Tag((UniversalTagNumber)16)))
                    {
                        throw new CryptographicException();
                    }
                }

                writer.WriteEncodedValue(Certificate.Value);
                wroteValue = true;
            }

            if (!wroteValue)
            {
                throw new CryptographicException();
            }
        }

        internal static CertificateChoiceAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            AsnReader reader = new AsnReader(encoded, ruleSet);
            
            Decode(reader, out CertificateChoiceAsn decoded);
            reader.ThrowIfNotEmpty();
            return decoded;
        }

        internal static void Decode(AsnReader reader, out CertificateChoiceAsn decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            decoded = default;
            Asn1Tag tag = reader.PeekTag();
            
            if (tag.HasSameClassAndValue(new Asn1Tag((UniversalTagNumber)16)))
            {
                decoded.Certificate = reader.GetEncodedValue();
            }
            else
            {
                throw new CryptographicException();
            }
        }
    }
}
