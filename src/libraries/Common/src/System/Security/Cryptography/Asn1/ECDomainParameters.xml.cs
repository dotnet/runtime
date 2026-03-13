// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable SA1028 // ignore whitespace warnings for generated code
using System;
using System.Formats.Asn1;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Asn1
{
#if DEBUG
    file static class ValidateECDomainParameters
    {
        static ValidateECDomainParameters()
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

            ensureUniqueTag(Asn1Tag.Sequence, "Specified");
            ensureUniqueTag(Asn1Tag.ObjectIdentifier, "Named");
        }
    }
#endif

    [StructLayout(LayoutKind.Sequential)]
    internal partial struct ECDomainParameters
    {
        internal System.Security.Cryptography.Asn1.SpecifiedECDomain? Specified;
        internal string? Named;

        internal readonly void Encode(AsnWriter writer)
        {
            bool wroteValue = false;

            if (Specified.HasValue)
            {
                if (wroteValue)
                    throw new CryptographicException();

                Specified.Value.Encode(writer);
                wroteValue = true;
            }

            if (Named != null)
            {
                if (wroteValue)
                    throw new CryptographicException();

                try
                {
                    writer.WriteObjectIdentifier(Named);
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

        internal static ECDomainParameters Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            try
            {
                ValueAsnReader reader = new ValueAsnReader(encoded.Span, ruleSet);

                DecodeCore(ref reader, encoded, out ECDomainParameters decoded);
                reader.ThrowIfNotEmpty();
                return decoded;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static void Decode(ref ValueAsnReader reader, ReadOnlyMemory<byte> rebind, out ECDomainParameters decoded)
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

        private static void DecodeCore(ref ValueAsnReader reader, ReadOnlyMemory<byte> rebind, out ECDomainParameters decoded)
        {
            decoded = default;
            Asn1Tag tag = reader.PeekTag();

            if (tag.HasSameClassAndValue(Asn1Tag.Sequence))
            {
                System.Security.Cryptography.Asn1.SpecifiedECDomain tmpSpecified;
                System.Security.Cryptography.Asn1.SpecifiedECDomain.Decode(ref reader, rebind, out tmpSpecified);
                decoded.Specified = tmpSpecified;

            }
            else if (tag.HasSameClassAndValue(Asn1Tag.ObjectIdentifier))
            {
                decoded.Named = reader.ReadObjectIdentifier();
            }
            else
            {
                throw new CryptographicException();
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref partial struct ValueECDomainParameters
    {
        internal System.Security.Cryptography.Asn1.ValueSpecifiedECDomain Specified;
        internal bool HasSpecified;
        internal string? Named;

        internal readonly void Encode(AsnWriter writer)
        {
            bool wroteValue = false;

            if (HasSpecified)
            {
                if (wroteValue)
                    throw new CryptographicException();

                Specified.Encode(writer);
                wroteValue = true;
            }

            if (Named != null)
            {
                if (wroteValue)
                    throw new CryptographicException();

                try
                {
                    writer.WriteObjectIdentifier(Named);
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

        internal static void Decode(ReadOnlySpan<byte> encoded, AsnEncodingRules ruleSet, out ValueECDomainParameters decoded)
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

        internal static void Decode(scoped ref ValueAsnReader reader, out ValueECDomainParameters decoded)
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

        private static void DecodeCore(scoped ref ValueAsnReader reader, out ValueECDomainParameters decoded)
        {
            decoded = default;
            Asn1Tag tag = reader.PeekTag();

            if (tag.HasSameClassAndValue(Asn1Tag.Sequence))
            {
                System.Security.Cryptography.Asn1.ValueSpecifiedECDomain tmpSpecified;
                System.Security.Cryptography.Asn1.ValueSpecifiedECDomain.Decode(ref reader, out tmpSpecified);
                decoded.Specified = tmpSpecified;

                decoded.HasSpecified = true;
            }
            else if (tag.HasSameClassAndValue(Asn1Tag.ObjectIdentifier))
            {
                decoded.Named = reader.ReadObjectIdentifier();
            }
            else
            {
                throw new CryptographicException();
            }
        }
    }
}
