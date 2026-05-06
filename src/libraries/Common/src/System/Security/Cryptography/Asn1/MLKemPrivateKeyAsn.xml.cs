// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable SA1028 // ignore whitespace warnings for generated code
using System;
using System.Formats.Asn1;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Asn1
{
#if DEBUG
    file static class ValidateMLKemPrivateKeyAsn
    {
        static ValidateMLKemPrivateKeyAsn()
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

        [System.Runtime.CompilerServices.MethodImpl(
            System.Runtime.CompilerServices.MethodImplOptions.NoInlining |
            System.Runtime.CompilerServices.MethodImplOptions.NoOptimization)]
        internal static void Validate() { }
    }
#endif

    [StructLayout(LayoutKind.Sequential)]
    internal ref partial struct ValueMLKemPrivateKeyAsn
    {

        internal ReadOnlySpan<byte> Seed
        {
            get;
            set
            {
                HasSeed = true;
                field = value;
            }
        }

        internal bool HasSeed { get; private set; }

        internal ReadOnlySpan<byte> ExpandedKey
        {
            get;
            set
            {
                HasExpandedKey = true;
                field = value;
            }
        }

        internal bool HasExpandedKey { get; private set; }

        internal System.Security.Cryptography.Asn1.ValueMLKemPrivateKeyBothAsn Both
        {
            get;
            set
            {
                HasBoth = true;
                field = value;
            }
        }

        internal bool HasBoth { get; private set; }

#if DEBUG
        static ValueMLKemPrivateKeyAsn()
        {
            ValidateMLKemPrivateKeyAsn.Validate();
        }
#endif

        internal readonly void Encode(AsnWriter writer)
        {
            bool wroteValue = false;

            if (HasSeed)
            {
                if (wroteValue)
                    throw new CryptographicException();

                writer.WriteOctetString(Seed, new Asn1Tag(TagClass.ContextSpecific, 0));
                wroteValue = true;
            }

            if (HasExpandedKey)
            {
                if (wroteValue)
                    throw new CryptographicException();

                writer.WriteOctetString(ExpandedKey);
                wroteValue = true;
            }

            if (HasBoth)
            {
                if (wroteValue)
                    throw new CryptographicException();

                Both.Encode(writer);
                wroteValue = true;
            }

            if (!wroteValue)
            {
                throw new CryptographicException();
            }
        }

        internal static void Decode(ReadOnlySpan<byte> encoded, AsnEncodingRules ruleSet, out ValueMLKemPrivateKeyAsn decoded)
        {
            try
            {
                ValueAsnReader reader = new ValueAsnReader(encoded, ruleSet);

                DecodeCore(ref reader, out decoded);
                reader.ThrowIfNotEmpty();
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static void Decode(scoped ref ValueAsnReader reader, out ValueMLKemPrivateKeyAsn decoded)
        {
            try
            {
                DecodeCore(ref reader, out decoded);
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        private static void DecodeCore(scoped ref ValueAsnReader reader, out ValueMLKemPrivateKeyAsn decoded)
        {
            decoded = default;
            Asn1Tag tag = reader.PeekTag();
            ReadOnlySpan<byte> tmpSpan;

            if (tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
            {

                if (reader.TryReadPrimitiveOctetString(out tmpSpan, new Asn1Tag(TagClass.ContextSpecific, 0)))
                {
                    decoded.Seed = tmpSpan;
                }
                else
                {
                    decoded.Seed = reader.ReadOctetString(new Asn1Tag(TagClass.ContextSpecific, 0));
                }

                decoded.HasSeed = true;
            }
            else if (tag.HasSameClassAndValue(Asn1Tag.PrimitiveOctetString))
            {

                if (reader.TryReadPrimitiveOctetString(out tmpSpan))
                {
                    decoded.ExpandedKey = tmpSpan;
                }
                else
                {
                    decoded.ExpandedKey = reader.ReadOctetString();
                }

                decoded.HasExpandedKey = true;
            }
            else if (tag.HasSameClassAndValue(Asn1Tag.Sequence))
            {
                System.Security.Cryptography.Asn1.ValueMLKemPrivateKeyBothAsn tmpBoth;
                System.Security.Cryptography.Asn1.ValueMLKemPrivateKeyBothAsn.Decode(ref reader, out tmpBoth);
                decoded.Both = tmpBoth;

                decoded.HasBoth = true;
            }
            else
            {
                throw new CryptographicException();
            }
        }
    }
}
