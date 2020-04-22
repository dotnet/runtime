using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Security.Cryptography.Encoding.Tests.Cbor
{
    internal enum CborConformanceLevel
    {
        NonStrict = 0,
        Strict = 1,
        Rfc7049Canonical = 2,
        Ctap2Canonical = 3,
    }

    internal static class CborConformanceLevelHelpers
    {
        public static bool RequiresUniqueKeys(CborConformanceLevel level)
        {
            return level switch
            {
                CborConformanceLevel.Rfc7049Canonical => true,
                CborConformanceLevel.Ctap2Canonical => true,
                CborConformanceLevel.Strict => true,
                CborConformanceLevel.NonStrict => false,
                _ => false,
            };
        }

        public static bool RequiresSortedKeys(CborConformanceLevel level)
        {
            return level switch
            {
                CborConformanceLevel.Rfc7049Canonical => true,
                CborConformanceLevel.Ctap2Canonical => true,
                CborConformanceLevel.Strict => false,
                CborConformanceLevel.NonStrict => false,
                _ => false,
            };
        }

        public static int CompareEncodings(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right, CborConformanceLevel level)
        {
            Debug.Assert(!left.IsEmpty && !right.IsEmpty);

            switch (level)
            {
                // Strict mode only concerns itself with uniqueness, not sorting
                // so any total order for buffers should do.
                case CborConformanceLevel.Strict:
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
