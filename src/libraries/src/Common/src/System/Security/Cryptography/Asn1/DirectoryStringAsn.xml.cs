using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.Asn1
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct DirectoryStringAsn
    {
        internal string TeletexString;
        internal string PrintableString;
        internal ReadOnlyMemory<byte>? UniversalString;
        internal string Utf8String;
        internal string BmpString;

#if DEBUG
        static DirectoryStringAsn()
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
            
            ensureUniqueTag(new Asn1Tag(UniversalTagNumber.T61String), "TeletexString");
            ensureUniqueTag(new Asn1Tag(UniversalTagNumber.PrintableString), "PrintableString");
            ensureUniqueTag(new Asn1Tag((UniversalTagNumber)28), "UniversalString");
            ensureUniqueTag(new Asn1Tag(UniversalTagNumber.UTF8String), "Utf8String");
            ensureUniqueTag(new Asn1Tag(UniversalTagNumber.BMPString), "BmpString");
        }
#endif

        internal void Encode(AsnWriter writer)
        {
            bool wroteValue = false; 
            
            if (TeletexString != null)
            {
                if (wroteValue)
                    throw new CryptographicException();
                
                writer.WriteCharacterString(UniversalTagNumber.T61String, TeletexString);
                wroteValue = true;
            }

            if (PrintableString != null)
            {
                if (wroteValue)
                    throw new CryptographicException();
                
                writer.WriteCharacterString(UniversalTagNumber.PrintableString, PrintableString);
                wroteValue = true;
            }

            if (UniversalString.HasValue)
            {
                if (wroteValue)
                    throw new CryptographicException();
                
                // Validator for tag constraint for UniversalString
                {
                    if (!Asn1Tag.TryParse(UniversalString.Value.Span, out Asn1Tag validateTag, out _) ||
                        !validateTag.HasSameClassAndValue(new Asn1Tag((UniversalTagNumber)28)))
                    {
                        throw new CryptographicException();
                    }
                }

                writer.WriteEncodedValue(UniversalString.Value);
                wroteValue = true;
            }

            if (Utf8String != null)
            {
                if (wroteValue)
                    throw new CryptographicException();
                
                writer.WriteCharacterString(UniversalTagNumber.UTF8String, Utf8String);
                wroteValue = true;
            }

            if (BmpString != null)
            {
                if (wroteValue)
                    throw new CryptographicException();
                
                writer.WriteCharacterString(UniversalTagNumber.BMPString, BmpString);
                wroteValue = true;
            }

            if (!wroteValue)
            {
                throw new CryptographicException();
            }
        }

        internal static DirectoryStringAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            AsnReader reader = new AsnReader(encoded, ruleSet);
            
            Decode(reader, out DirectoryStringAsn decoded);
            reader.ThrowIfNotEmpty();
            return decoded;
        }

        internal static void Decode(AsnReader reader, out DirectoryStringAsn decoded)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            decoded = default;
            Asn1Tag tag = reader.PeekTag();
            
            if (tag.HasSameClassAndValue(new Asn1Tag(UniversalTagNumber.T61String)))
            {
                decoded.TeletexString = reader.GetCharacterString(UniversalTagNumber.T61String);
            }
            else if (tag.HasSameClassAndValue(new Asn1Tag(UniversalTagNumber.PrintableString)))
            {
                decoded.PrintableString = reader.GetCharacterString(UniversalTagNumber.PrintableString);
            }
            else if (tag.HasSameClassAndValue(new Asn1Tag((UniversalTagNumber)28)))
            {
                decoded.UniversalString = reader.GetEncodedValue();
            }
            else if (tag.HasSameClassAndValue(new Asn1Tag(UniversalTagNumber.UTF8String)))
            {
                decoded.Utf8String = reader.GetCharacterString(UniversalTagNumber.UTF8String);
            }
            else if (tag.HasSameClassAndValue(new Asn1Tag(UniversalTagNumber.BMPString)))
            {
                decoded.BmpString = reader.GetCharacterString(UniversalTagNumber.BMPString);
            }
            else
            {
                throw new CryptographicException();
            }
        }
    }
}
