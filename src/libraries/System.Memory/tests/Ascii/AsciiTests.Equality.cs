// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;
using Xunit.Sdk;

namespace System.Buffers.Text.Tests
{
    public partial class AsciiUnitTests
    {
        private delegate int ComputeHashCodeFunc<T>(ReadOnlySpan<T> span);

        public static IEnumerable<(string left, string right, bool areEqualOrdinal, bool areEqualIgnoreCase)> EqualityAsciiCommonTestData()
        {
            yield return ("", "", true, true);
            yield return ("\0", "\0", true, true);
            yield return (" Hello ", " Hello ", true, true);
            yield return (" Hello ", " hello ", false, true);
            yield return (" xYz ", " XyZ ", false, true);
            yield return ("\rhello\n", "\rHELLO\n", false, true);
            yield return ("\nhello\r", "\rHELLO\n", false, false); // swapped control chars
        }

        public static IEnumerable<(string left, string right, bool areEqualOrdinal, bool areEqualIgnoreCase)> EqualityBytesBytesTestData()
        {
            // Exact byte comparisons for non-ASCII bytes
            yield return ("xx\u00C0yy", "xx\u00C0yy", true, true);
            yield return ("xX\u00C0yY", "Xx\u00C0Yy", false, true);
            yield return ("xx\u00C0yy", "xx\u00E0yy", false, false); // U+00C0 and U+00E0 are non-ASCII so shouldn't case-convert
        }

        public static IEnumerable<(string left, string right, bool areEqualOrdinal, bool areEqualIgnoreCase)> EqualityBytesCharsTestData()
        {
            // Non-ASCII bytes should never compare as equal to non-ASCII chars
            yield return ("xx\u00C0yy", "xx\u00C0yy", false, false);
            yield return ("xX\u00C0yY", "Xx\u00C0Yy", false, false);
            yield return ("xx\u00C0yy", "xx\u00E0yy", false, false);

            // Don't normalize non-ASCII before comparisons
            yield return ("\u00C0", "A\u0300", false, false); // U+00C0 => U+0041 + U+0300 (decomposed)
        }

        public static IEnumerable<(string left, string right, bool areEqualOrdinal, bool areEqualIgnoreCase)> EqualityCharsCharsTestData()
        {
            // Exact char comparisons for non-ASCII chars
            yield return ("xx\u20A4yy", "xx\u20A4yy", true, true);

            // Don't normalize non-ASII before comparisons
            yield return ("\u00C0", "A\u0300", false, false); // U+00C0 => U+0041 + U+0300 (decomposed)
            yield return ("\u00C0", "a\u0300", false, false);
            yield return ("A\u0300", "a\u0300", false, true);
        }

        [Fact]
        public void Equals_NullStringParamChecks()
        {
            Assert.Throws<ArgumentNullException>("right", () => Ascii.Equals(ReadOnlySpan<byte>.Empty, (string)null));
            Assert.Throws<ArgumentNullException>("right", () => Ascii.EqualsIgnoreCase(ReadOnlySpan<byte>.Empty, (string)null));
        }

        [Theory]
        [TupleMemberData(nameof(EqualityAsciiCommonTestData))]
        [TupleMemberData(nameof(EqualityBytesBytesTestData))]
        public void Equals_ByteByte(string left, string right, [Alias("areEqualOrdinal")] bool expectedAreEqual)
        {
            byte[] leftBytes = CharsToAsciiBytesChecked(left);
            byte[] rightBytes = CharsToAsciiBytesChecked(right);

            Assert.Equal(expectedAreEqual, Ascii.Equals(leftBytes, rightBytes));
            Assert.Equal(expectedAreEqual, Ascii.Equals(rightBytes, leftBytes));
            ValidateProbabilisticHashCode<byte>(leftBytes, rightBytes, Ascii.GetHashCode, expectedAreEqual);
        }

        [Theory]
        [TupleMemberData(nameof(EqualityAsciiCommonTestData))]
        [TupleMemberData(nameof(EqualityBytesBytesTestData))]
        public void Equals_ByteByte_IgnoreCase(string left, string right, [Alias("areEqualIgnoreCase")] bool expectedAreEqual)
        {
            byte[] leftBytes = CharsToAsciiBytesChecked(left);
            byte[] rightBytes = CharsToAsciiBytesChecked(right);

            Assert.Equal(expectedAreEqual, Ascii.EqualsIgnoreCase(leftBytes, rightBytes));
            Assert.Equal(expectedAreEqual, Ascii.EqualsIgnoreCase(rightBytes, leftBytes));
            ValidateProbabilisticHashCode<byte>(leftBytes, rightBytes, Ascii.GetHashCodeIgnoreCase, expectedAreEqual);
        }

        [Theory]
        [TupleMemberData(nameof(EqualityAsciiCommonTestData))]
        [TupleMemberData(nameof(EqualityBytesCharsTestData))]
        public void Equals_ByteChar(string left, string right, [Alias("areEqualOrdinal")] bool expectedAreEqual)
        {
            byte[] leftBytes = CharsToAsciiBytesChecked(left);

            Assert.Equal(expectedAreEqual, Ascii.Equals(leftBytes, right.AsSpan()));
            Assert.Equal(expectedAreEqual, Ascii.Equals(leftBytes, right /* as string */));
        }

        [Theory]
        [TupleMemberData(nameof(EqualityAsciiCommonTestData))]
        [TupleMemberData(nameof(EqualityBytesCharsTestData))]
        public void Equals_ByteChar_IgnoreCase(string left, string right, [Alias("areEqualIgnoreCase")] bool expectedAreEqual)
        {
            byte[] leftBytes = CharsToAsciiBytesChecked(left);

            Assert.Equal(expectedAreEqual, Ascii.EqualsIgnoreCase(leftBytes, right.AsSpan()));
            Assert.Equal(expectedAreEqual, Ascii.EqualsIgnoreCase(leftBytes, right /* as string */));
        }

        [Theory]
        [TupleMemberData(nameof(EqualityAsciiCommonTestData))]
        [TupleMemberData(nameof(EqualityBytesBytesTestData))]
        [TupleMemberData(nameof(EqualityCharsCharsTestData))]
        public void Equals_CharChar(string left, string right, [Alias("areEqualOrdinal")] bool expectedAreEqual)
        {
            Assert.Equal(expectedAreEqual, Ascii.Equals(left.AsSpan(), right.AsSpan()));
            Assert.Equal(expectedAreEqual, Ascii.Equals(right.AsSpan(), left.AsSpan()));
            ValidateProbabilisticHashCode<char>(left, right, Ascii.GetHashCode, expectedAreEqual);
        }

        [Theory]
        [TupleMemberData(nameof(EqualityAsciiCommonTestData))]
        [TupleMemberData(nameof(EqualityBytesBytesTestData))]
        [TupleMemberData(nameof(EqualityCharsCharsTestData))]
        public void Equals_CharChar_IgnoreCase(string left, string right, [Alias("areEqualIgnoreCase")] bool expectedAreEqual)
        {
            Assert.Equal(expectedAreEqual, Ascii.EqualsIgnoreCase(left.AsSpan(), right.AsSpan()));
            Assert.Equal(expectedAreEqual, Ascii.EqualsIgnoreCase(right.AsSpan(), left.AsSpan()));
            ValidateProbabilisticHashCode<char>(left, right, Ascii.GetHashCodeIgnoreCase, expectedAreEqual);
        }

        private static void ValidateProbabilisticHashCode<T>(ReadOnlySpan<T> left, ReadOnlySpan<T> right, ComputeHashCodeFunc<T> hashCodeFunc, bool expectedAreEqual)
        {
            int hashCodeLeft = hashCodeFunc(left);
            int hashCodeRight = hashCodeFunc(right);

            if (expectedAreEqual == (hashCodeLeft == hashCodeRight))
            {
                return; // "are hash codes equal?" matches "are buffers equal?" - all is well
            }

            if (expectedAreEqual)
            {
                throw new XunitException("Buffers are expected to compare as equal but produced different hash codes.");
            }

            // If we got to this point, we expected the buffers to compare as unequal, but they
            // had the same hash code. We can't eagerly fail here because our hash code calculation
            // routines are randomized, and we expect two unequal buffers to produce the same
            // hash code every so often. To account for this, we'll run the test a few more
            // times with buffers of slightly different length. If they all produce the same
            // result, then there's probably a problem with the hash code computation.

            for (int i = 1; i <= 8; i++)
            {
                T[] leftArray = new T[left.Length + i]; // pad with zeroes at the end
                left.CopyTo(leftArray);

                T[] rightArray = new T[right.Length + i]; // pad with zeroes at the end
                right.CopyTo(rightArray);

                if (hashCodeFunc(leftArray) != hashCodeFunc(rightArray))
                {
                    return; // buffers produced different hash codes - this is fine
                }
            }

            throw new XunitException("Buffers are expected to compare as not equal but produced the same hash code after multiple rounds.");
        }
    }
}
