// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Buffers.Text.Tests
{
    public static class GetHashCodeCharTests
    {
        [Theory]
        [InlineData("\u00C0")]
        [InlineData("aaa\u00C0bbb")]
        public static void InvalidCharactersInValueThrowsOrReturnsFalse(string value)
        {
            Assert.Throws<ArgumentException>(() => Ascii.GetHashCode(value));
            Assert.Throws<ArgumentException>(() => Ascii.GetHashCodeIgnoreCase(value));

            Assert.False(Ascii.TryGetHashCode(value, out int hashCode));
            Assert.Equal(default(int), hashCode);
            Assert.False(Ascii.TryGetHashCodeIgnoreCase(value, out hashCode));
            Assert.Equal(default(int), hashCode);
        }

        public static IEnumerable<object[]> ValidInputValidOutput_TestData
        {
            get
            {
                yield return new object[] { "test" };
                yield return new object[] { "tESt" };
                yield return new object[] { "!@#$%^&*()" };
                yield return new object[] { "0123456789" };
                yield return new object[] { " \t\r\n" };
                yield return new object[] { new string(Enumerable.Range(0, 127).Select(i => (char)i).ToArray()) };
            }
        }

        [Theory]
        [InlineData(nameof(ValidInputValidOutput_TestData))]
        public static void ValidInputValidOutput(string input)
        {
            // The contract makes it clear that hash code is randomized and is not guaranteed to match string.GetHashCode.
            // But.. re-using same types used internally by string.GetHashCode was the simplest way to get good hashing implementaiton.
            // So this test verifies implementation detail.

            int expectedHashCode = input.GetHashCode();
            Assert.Equal(expectedHashCode, Ascii.GetHashCode(input));
            Assert.True(Ascii.TryGetHashCode(input, out int actualHashCode));
            Assert.Equal(expectedHashCode, actualHashCode);

            expectedHashCode = input.GetHashCode(StringComparison.OrdinalIgnoreCase);
            Assert.Equal(expectedHashCode, Ascii.GetHashCodeIgnoreCase(input));
            Assert.True(Ascii.TryGetHashCodeIgnoreCase(input, out actualHashCode));
            Assert.Equal(expectedHashCode, actualHashCode);
        }
    }
}
