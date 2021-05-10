// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Formats.Cbor
{
    /// <summary>Defines supported conformance modes for encoding and decoding CBOR data.</summary>
    public enum CborConformanceMode
    {
        /// <summary>Ensures that the CBOR data is well-formed, as specified in RFC7049.</summary>
        Lax,

        /// <summary>
        /// <para>Ensures that the CBOR data adheres to strict mode, as specified in RFC7049 section 3.10.</para>
        /// <para>Extends lax conformance with the following requirements:</para>
        /// <list type="bullet">
        /// <item>Maps (major type 5) must not contain duplicate keys.</item>
        /// <item>Simple values (major type 7) must be encoded as small a possible and exclude the reserved values 24-31.</item>
        /// <item>UTF-8 string encodings must be valid.</item>
        /// </list>
        /// </summary>
        Strict,

        /// <summary>
        /// <para>Ensures that the CBOR data is canonical, as specified in RFC7049 section 3.9.</para>
        /// <para>Extends strict conformance with the following requirements:</para>
        /// <list type="bullet">
        /// <item>Integers must be encoded as small as possible.</item>
        /// <item>Maps (major type 5) must contain keys sorted by encoding.</item>
        /// <item>Indefinite-length items must be made into definite-length items.</item>
        /// </list>
        /// </summary>
        Canonical,

        /// <summary>
        /// <para>Ensures that the CBOR data is canonical, as specified by the CTAP v2.0 standard, section 6.</para>
        /// <para>Extends strict conformance with the following requirements:</para>
        /// <list type="bullet">
        /// <item>Maps (major type 5) must contain keys sorted by encoding.</item>
        /// <item>Indefinite-length items must be made into definite-length items.</item>
        /// <item>Integers must be encoded as small as possible.</item>
        /// <item>The representations of any floating-point values are not changed.</item>
        /// <item>CBOR tags (major type 6) are not permitted.</item>
        /// </list>
        /// </summary>
        Ctap2Canonical,
    }

    internal static class CborConformanceModeHelpers
    {
        private static readonly UTF8Encoding s_utf8EncodingLax    = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);
        private static readonly UTF8Encoding s_utf8EncodingStrict = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        public static void Validate(CborConformanceMode conformanceMode)
        {
            if (conformanceMode < CborConformanceMode.Lax ||
                conformanceMode > CborConformanceMode.Ctap2Canonical)
            {
                throw new ArgumentOutOfRangeException(nameof(conformanceMode));
            }
        }

        public static bool RequiresCanonicalIntegerRepresentation(CborConformanceMode conformanceMode)
        {
            switch (conformanceMode)
            {
                case CborConformanceMode.Lax:
                case CborConformanceMode.Strict:
                    return false;

                case CborConformanceMode.Canonical:
                case CborConformanceMode.Ctap2Canonical:
                    return true;

                default:
                    throw new ArgumentOutOfRangeException(nameof(conformanceMode));
            };
        }

        public static bool RequiresPreservingFloatPrecision(CborConformanceMode conformanceMode)
        {
            switch (conformanceMode)
            {
                case CborConformanceMode.Lax:
                case CborConformanceMode.Strict:
                case CborConformanceMode.Canonical:
                    return false;

                case CborConformanceMode.Ctap2Canonical:
                    return true;

                default:
                    throw new ArgumentOutOfRangeException(nameof(conformanceMode));
            };
        }

        public static bool RequiresUtf8Validation(CborConformanceMode conformanceMode)
        {
            switch (conformanceMode)
            {
                case CborConformanceMode.Lax:
                    return false;

                case CborConformanceMode.Strict:
                case CborConformanceMode.Canonical:
                case CborConformanceMode.Ctap2Canonical:
                    return true;

                default:
                    throw new ArgumentOutOfRangeException(nameof(conformanceMode));
            };
        }

        public static Encoding GetUtf8Encoding(CborConformanceMode conformanceMode)
        {
            return conformanceMode == CborConformanceMode.Lax ? s_utf8EncodingLax : s_utf8EncodingStrict;
        }

        public static bool RequiresDefiniteLengthItems(CborConformanceMode conformanceMode)
        {
            switch (conformanceMode)
            {
                case CborConformanceMode.Lax:
                case CborConformanceMode.Strict:
                    return false;

                case CborConformanceMode.Canonical:
                case CborConformanceMode.Ctap2Canonical:
                    return true;

                default:
                    throw new ArgumentOutOfRangeException(nameof(conformanceMode));
            };
        }

        public static bool AllowsTags(CborConformanceMode conformanceMode)
        {
            switch (conformanceMode)
            {
                case CborConformanceMode.Lax:
                case CborConformanceMode.Strict:
                case CborConformanceMode.Canonical:
                    return true;

                case CborConformanceMode.Ctap2Canonical:
                    return false;

                default:
                    throw new ArgumentOutOfRangeException(nameof(conformanceMode));
            };
        }

        public static bool RequiresUniqueKeys(CborConformanceMode conformanceMode)
        {
            switch (conformanceMode)
            {
                case CborConformanceMode.Lax:
                    return false;

                case CborConformanceMode.Strict:
                case CborConformanceMode.Canonical:
                case CborConformanceMode.Ctap2Canonical:
                    return true;

                default:
                    throw new ArgumentOutOfRangeException(nameof(conformanceMode));
            };
        }

        public static bool RequiresSortedKeys(CborConformanceMode conformanceMode)
        {
            switch (conformanceMode)
            {
                case CborConformanceMode.Strict:
                case CborConformanceMode.Lax:
                    return false;

                case CborConformanceMode.Canonical:
                case CborConformanceMode.Ctap2Canonical:
                    return true;

                default:
                    throw new ArgumentOutOfRangeException(nameof(conformanceMode));
            };
        }

        public static bool RequireCanonicalSimpleValueEncodings(CborConformanceMode conformanceMode)
        {
            switch (conformanceMode)
            {
                case CborConformanceMode.Lax:
                    return false;

                case CborConformanceMode.Strict:
                case CborConformanceMode.Canonical:
                case CborConformanceMode.Ctap2Canonical:
                    return true;

                default:
                    throw new ArgumentOutOfRangeException(nameof(conformanceMode));
            }
        }

        public static int GetKeyEncodingHashCode(ReadOnlySpan<byte> encoding)
        {
            HashCode hash = default;

            // TODO: Use https://github.com/dotnet/runtime/issues/48702 if/when it's available
            while (encoding.Length >= sizeof(int))
            {
                hash.Add(MemoryMarshal.Read<int>(encoding));
                encoding = encoding.Slice(sizeof(int));
            }

            foreach (byte b in encoding)
            {
                hash.Add(b);
            }

            return hash.ToHashCode();
        }

        public static bool AreEqualKeyEncodings(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
        {
            return left.SequenceEqual(right);
        }

        public static int CompareKeyEncodings(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right, CborConformanceMode mode)
        {
            Debug.Assert(!left.IsEmpty && !right.IsEmpty);

            switch (mode)
            {
                case CborConformanceMode.Canonical:
                    // Implements key sorting according to
                    // https://tools.ietf.org/html/rfc7049#section-3.9

                    if (left.Length != right.Length)
                    {
                        return left.Length - right.Length;
                    }

                    return left.SequenceCompareTo(right);

                case CborConformanceMode.Ctap2Canonical:
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
                    Debug.Fail("Invalid conformance mode used in encoding sort.");
                    throw new Exception();
            }
        }
    }
}
