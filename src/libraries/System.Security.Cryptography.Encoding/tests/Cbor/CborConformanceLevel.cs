// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Formats.Cbor
{
    public enum CborConformanceLevel
    {
        Lax = 0,
        Strict = 1,
        Rfc7049Canonical = 2,
        Ctap2Canonical = 3,
    }

    internal static class CborConformanceLevelHelpers
    {
        public static void Validate(CborConformanceLevel conformanceLevel)
        {
            if (conformanceLevel < CborConformanceLevel.Lax ||
                conformanceLevel > CborConformanceLevel.Ctap2Canonical)
            {
                throw new ArgumentOutOfRangeException(nameof(conformanceLevel));
            }
        }

        public static bool RequiresMinimalIntegerRepresentation(CborConformanceLevel conformanceLevel)
        {
            switch (conformanceLevel)
            {
                case CborConformanceLevel.Lax:
                case CborConformanceLevel.Strict:
                    return false;

                case CborConformanceLevel.Rfc7049Canonical:
                case CborConformanceLevel.Ctap2Canonical:
                    return true;

                default:
                    throw new ArgumentOutOfRangeException(nameof(conformanceLevel));
            };
        }

        public static bool RequiresDefiniteLengthItems(CborConformanceLevel conformanceLevel)
        {
            switch (conformanceLevel)
            {
                case CborConformanceLevel.Lax:
                case CborConformanceLevel.Strict:
                    return false;

                case CborConformanceLevel.Rfc7049Canonical:
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
                case CborConformanceLevel.Rfc7049Canonical:
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
                case CborConformanceLevel.Rfc7049Canonical:
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

                case CborConformanceLevel.Rfc7049Canonical:
                case CborConformanceLevel.Ctap2Canonical:
                    return true;

                default:
                    throw new ArgumentOutOfRangeException(nameof(conformanceLevel));
            };
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
                case CborConformanceLevel.Rfc7049Canonical:
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
                    throw new Exception("Invalid conformance level used in encoding sort.");
            }
        }
    }
}
