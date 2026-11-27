// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable SA1028 // ignore whitespace warnings for generated code
using System;
using System.Formats.Asn1;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Asn1
{
#if DEBUG
    file static class ValidatePbkdf2SaltChoice
    {
        static ValidatePbkdf2SaltChoice()
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

            ensureUniqueTag(Asn1Tag.PrimitiveOctetString, "Specified");
            ensureUniqueTag(Asn1Tag.Sequence, "OtherSource");
        }

        [System.Runtime.CompilerServices.MethodImpl(
            System.Runtime.CompilerServices.MethodImplOptions.NoInlining |
            System.Runtime.CompilerServices.MethodImplOptions.NoOptimization)]
        internal static void Validate() { }
    }
#endif

    [StructLayout(LayoutKind.Sequential)]
    internal ref partial struct ValuePbkdf2SaltChoice
    {

        internal ReadOnlySpan<byte> Specified
        {
            get;
            set
            {
                HasSpecified = true;
                field = value;
            }
        }

        internal bool HasSpecified { get; private set; }

        internal System.Security.Cryptography.Asn1.ValueAlgorithmIdentifierAsn OtherSource
        {
            get;
            set
            {
                HasOtherSource = true;
                field = value;
            }
        }

        internal bool HasOtherSource { get; private set; }

#if DEBUG
        static ValuePbkdf2SaltChoice()
        {
            ValidatePbkdf2SaltChoice.Validate();
        }
#endif

        internal readonly void Encode(AsnWriter writer)
        {
            bool wroteValue = false;

            if (HasSpecified)
            {
                if (wroteValue)
                    throw new CryptographicException();

                writer.WriteOctetString(Specified);
                wroteValue = true;
            }

            if (HasOtherSource)
            {
                if (wroteValue)
                    throw new CryptographicException();

                OtherSource.Encode(writer);
                wroteValue = true;
            }

            if (!wroteValue)
            {
                throw new CryptographicException();
            }
        }

        internal static void Decode(ReadOnlySpan<byte> encoded, AsnEncodingRules ruleSet, out ValuePbkdf2SaltChoice decoded)
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

        internal static void Decode(scoped ref ValueAsnReader reader, out ValuePbkdf2SaltChoice decoded)
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

        private static void DecodeCore(scoped ref ValueAsnReader reader, out ValuePbkdf2SaltChoice decoded)
        {
            decoded = default;
            Asn1Tag tag = reader.PeekTag();
            ReadOnlySpan<byte> tmpSpan;

            if (tag.HasSameClassAndValue(Asn1Tag.PrimitiveOctetString))
            {

                if (reader.TryReadPrimitiveOctetString(out tmpSpan))
                {
                    decoded.Specified = tmpSpan;
                }
                else
                {
                    decoded.Specified = reader.ReadOctetString();
                }

                decoded.HasSpecified = true;
            }
            else if (tag.HasSameClassAndValue(Asn1Tag.Sequence))
            {
                System.Security.Cryptography.Asn1.ValueAlgorithmIdentifierAsn tmpOtherSource;
                System.Security.Cryptography.Asn1.ValueAlgorithmIdentifierAsn.Decode(ref reader, out tmpOtherSource);
                decoded.OtherSource = tmpOtherSource;

                decoded.HasOtherSource = true;
            }
            else
            {
                throw new CryptographicException();
            }
        }
    }
}
