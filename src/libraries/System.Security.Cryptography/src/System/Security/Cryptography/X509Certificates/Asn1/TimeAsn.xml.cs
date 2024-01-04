// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable SA1028 // ignore whitespace warnings for generated code
using System;
using System.Formats.Asn1;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography.X509Certificates.Asn1
{
    [StructLayout(LayoutKind.Sequential)]
    internal partial struct TimeAsn
    {
        internal DateTimeOffset? UtcTime;
        internal DateTimeOffset? GeneralTime;

#if DEBUG
        static TimeAsn()
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

            ensureUniqueTag(Asn1Tag.UtcTime, "UtcTime");
            ensureUniqueTag(Asn1Tag.GeneralizedTime, "GeneralTime");
        }
#endif

        internal readonly void Encode(AsnWriter writer)
        {
            bool wroteValue = false;

            if (UtcTime.HasValue)
            {
                if (wroteValue)
                    throw new CryptographicException();

                writer.WriteUtcTime(UtcTime.Value);
                wroteValue = true;
            }

            if (GeneralTime.HasValue)
            {
                if (wroteValue)
                    throw new CryptographicException();

                writer.WriteGeneralizedTime(GeneralTime.Value, omitFractionalSeconds: true);
                wroteValue = true;
            }

            if (!wroteValue)
            {
                throw new CryptographicException();
            }
        }

        internal static TimeAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
        {
            try
            {
                AsnValueReader reader = new AsnValueReader(encoded.Span, ruleSet);

                DecodeCore(ref reader, out TimeAsn decoded);
                reader.ThrowIfNotEmpty();
                return decoded;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        internal static void Decode(ref AsnValueReader reader, out TimeAsn decoded)
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

        private static void DecodeCore(ref AsnValueReader reader, out TimeAsn decoded)
        {
            decoded = default;
            Asn1Tag tag = reader.PeekTag();

            if (tag.HasSameClassAndValue(Asn1Tag.UtcTime))
            {
                decoded.UtcTime = reader.ReadUtcTime();
            }
            else if (tag.HasSameClassAndValue(Asn1Tag.GeneralizedTime))
            {
                decoded.GeneralTime = reader.ReadGeneralizedTime();

                if (decoded.GeneralTime!.Value.Ticks % TimeSpan.TicksPerSecond != 0)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

            }
            else
            {
                throw new CryptographicException();
            }
        }
    }
}
