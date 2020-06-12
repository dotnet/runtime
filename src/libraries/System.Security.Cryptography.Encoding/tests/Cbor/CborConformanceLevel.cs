// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Text;

namespace System.Formats.Cbor
{
    /// <summary>
    ///   Defines supported conformance levels for encoding and decoding CBOR data.
    /// </summary>
    public enum CborConformanceLevel
    {
        /// <summary>
        ///   Ensures that the CBOR data is well-formed, as specified in RFC7049.
        /// </summary>
        Lax,

        /// <summary>
        ///   Ensures that the CBOR data adheres to strict mode, as specified in RFC7049 section 3.10.
        ///   Extends lax conformance with the following requirements:
        ///   <list type="bullet">
        ///   <item>Maps (major type 5) must not contain duplicate keys.</item>
        ///   <item>Simple values (major type 7) must be encoded as small a possible and exclude the reserved values 24-31.</item>
        ///   <item>UTF-8 string encodings must be valid.</item>
        ///   </list>
        /// </summary>
        Strict,

        /// <summary>
        ///   Ensures that the CBOR data is canonical, as specified in RFC7049 section 3.9.
        ///   Extends strict conformance with the following requirements:
        ///   <list type="bullet">
        ///   <item>Integers must be encoded as small as possible.</item>
        ///   <item>Maps (major type 5) must contain keys sorted by encoding.</item>
        ///   <item>Indefinite-length items must be made into definite-length items.</item>
        ///   </list>
        /// </summary>
        Canonical,

        /// <summary>
        ///   Ensures that the CBOR data is canonical, as specified by the CTAP v2.0 standard, section 6.
        ///   Extends strict conformance with the following requirements:
        ///   <list type="bullet">
        ///   <item>Maps (major type 5) must contain keys sorted by encoding.</item>
        ///   <item>Indefinite-length items must be made into definite-length items.</item>
        ///   <item>Integers must be encoded as small as possible.</item>
        ///   <item>The representations of any floating-point values are not changed.</item>
        ///   <item>CBOR tags (major type 6) are not permitted.</item>
        ///   </list>
        /// </summary>
        Ctap2Canonical,
    }

    internal static class CborConformanceLevelHelpers
    {
        private static readonly UTF8Encoding s_utf8EncodingLax    = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);
        private static readonly UTF8Encoding s_utf8EncodingStrict = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        public static void Validate(CborConformanceLevel conformanceLevel)
        {
            if (conformanceLevel < CborConformanceLevel.Lax ||
                conformanceLevel > CborConformanceLevel.Ctap2Canonical)
            {
                throw new ArgumentOutOfRangeException(nameof(conformanceLevel));
            }
        }

        public static bool RequiresCanonicalIntegerRepresentation(CborConformanceLevel conformanceLevel)
        {
            switch (conformanceLevel)
            {
                case CborConformanceLevel.Lax:
                case CborConformanceLevel.Strict:
                    return false;

                case CborConformanceLevel.Canonical:
                case CborConformanceLevel.Ctap2Canonical:
                    return true;

                default:
                    throw new ArgumentOutOfRangeException(nameof(conformanceLevel));
            };
        }

        public static bool RequiresUtf8Validation(CborConformanceLevel conformanceLevel)
        {
            switch (conformanceLevel)
            {
                case CborConformanceLevel.Lax:
                    return false;

                case CborConformanceLevel.Strict:
                case CborConformanceLevel.Canonical:
                case CborConformanceLevel.Ctap2Canonical:
                    return true;

                default:
                    throw new ArgumentOutOfRangeException(nameof(conformanceLevel));
            };
        }

        public static Encoding GetUtf8Encoding(CborConformanceLevel conformanceLevel)
        {
            return conformanceLevel == CborConformanceLevel.Lax ? s_utf8EncodingLax : s_utf8EncodingStrict;
        }

        public static bool RequiresDefiniteLengthItems(CborConformanceLevel conformanceLevel)
        {
            switch (conformanceLevel)
            {
                case CborConformanceLevel.Lax:
                case CborConformanceLevel.Strict:
                    return false;

                case CborConformanceLevel.Canonical:
                case CborConformanceLevel.Ctap2Canonical:
                    return true;

                default:
                    throw new ArgumentOutOfRangeException(nameof(conformanceLevel));
            };
        }

        public static bool AllowsTags(CborConformanceLevel conformanceLevel)
        {
            switch (conformanceLevel)
            {
                case CborConformanceLevel.Lax:
                case CborConformanceLevel.Strict:
                case CborConformanceLevel.Canonical:
                    return true;

                case CborConformanceLevel.Ctap2Canonical:
                    return false;

                default:
                    throw new ArgumentOutOfRangeException(nameof(conformanceLevel));
            };
        }

        public static bool RequiresUniqueKeys(CborConformanceLevel conformanceLevel)
        {
            switch (conformanceLevel)
            {
                case CborConformanceLevel.Lax:
                    return false;

                case CborConformanceLevel.Strict:
                case CborConformanceLevel.Canonical:
                case CborConformanceLevel.Ctap2Canonical:
                    return true;

                default:
                    throw new ArgumentOutOfRangeException(nameof(conformanceLevel));
            };
        }

        public static bool RequiresSortedKeys(CborConformanceLevel conformanceLevel)
        {
            switch (conformanceLevel)
            {
                case CborConformanceLevel.Strict:
                case CborConformanceLevel.Lax:
                    return false;

                case CborConformanceLevel.Canonical:
                case CborConformanceLevel.Ctap2Canonical:
                    return true;

                default:
                    throw new ArgumentOutOfRangeException(nameof(conformanceLevel));
            };
        }

        public static bool RequireCanonicalSimpleValueEncodings(CborConformanceLevel conformanceLevel)
        {
            switch (conformanceLevel)
            {
                case CborConformanceLevel.Lax:
                    return false;

                case CborConformanceLevel.Strict:
                case CborConformanceLevel.Canonical:
                case CborConformanceLevel.Ctap2Canonical:
                    return true;

                default:
                    throw new ArgumentOutOfRangeException(nameof(conformanceLevel));
            }
        }

        public static int GetKeyEncodingHashCode(ReadOnlySpan<byte> encoding)
        {
            return System.Marvin.ComputeHash32(encoding, System.Marvin.DefaultSeed);
        }

        public static bool AreEqualKeyEncodings(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
        {
            return left.SequenceEqual(right);
        }

        public static int CompareKeyEncodings(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right, CborConformanceLevel level)
        {
            Debug.Assert(!left.IsEmpty && !right.IsEmpty);

            switch (level)
            {
                case CborConformanceLevel.Canonical:
                    // Implements key sorting according to
                    // https://tools.ietf.org/html/rfc7049#section-3.9

                    if (left.Length != right.Length)
                    {
                        return left.Length - right.Length;
                    }

                    return left.SequenceCompareTo(right);

                case CborConformanceLevel.Ctap2Canonical:
                    // Implements key sorting according to
                    // https://fidoalliance.org/specs/fido-v2.0-ps-20190130/fido-client-to-authenticator-protocol-v2.0-ps-20190130.html#message-encoding

                    int leftMt = (int)new CborInitialByte(left[0]).MajorType;
                    int rightMt = (int)new CborInitialByte(right[0]).MajorType;

                    if (leftMt != rightMt)
                    {
                        return leftMt - rightMt;
                    }

                    if (left.Length != right.Length)
                    {
                        return left.Length - right.Length;
                    }

                    return left.SequenceCompareTo(right);

                default:
                    Debug.Fail("Invalid conformance level used in encoding sort.");
                    throw new Exception();
            }
        }
    }
}
