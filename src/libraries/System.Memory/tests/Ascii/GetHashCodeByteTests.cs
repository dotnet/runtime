// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace System.Buffers.Text.Tests
{
    public class GetHashCodeByteTests
    {
        [Theory]
        [InlineData(new byte[] { 128 })]
        [InlineData(new byte[] { 91, 91, 128, 91 })] // >= 4 chars can execute a different code path
        public void InvalidCharactersInValueThrowsOrReturnsFalse(byte[] value)
        {
            Assert.Throws<ArgumentException>(() => Ascii.GetHashCode(value));
            Assert.Throws<ArgumentException>(() => Ascii.GetHashCodeIgnoreCase(value));

            Assert.False(Ascii.TryGetHashCode(value, out int hashCode));
            Assert.Equal(default(int), hashCode);
            Assert.False(Ascii.TryGetHashCodeIgnoreCase(value, out hashCode));
            Assert.Equal(default(int), hashCode);
        }

        public IEnumerable<object[]> ValidInputValidOutput_TestData
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
        public void ValidInputValidOutput(string input)
        {
            // The contract makes it clear that hash code is randomized and is not guaranteed to match string.GetHashCode.
            // But.. re-using same types used internally by string.GetHashCode was the simplest way to get good hashing implementaiton.
            // So this test verifies implementation detail.

            // string.GetHashcode treats string as buffer of bytes
            // this is why this test casts ROS<char> to ROS<byte>, rather than doing actual encoding conversion (this would narrow the bytes)
            ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(input.AsSpan());

            int expectedHashCode = input.GetHashCode();
            Assert.Equal(expectedHashCode, Ascii.GetHashCode(bytes));
            Assert.True(Ascii.TryGetHashCode(input, out int actualHashCode));
            Assert.Equal(expectedHashCode, actualHashCode);

            // Ascii.*GetHashCodeIgnoreCase(bytes) processes four ASCII bytes at a time
            // rather than two ascii chars as string.GetHashCode(StringComparison.OrdinalIgnoreCase) does.
            // This is why they might produce different outputs and their results are not checked for equality.

            bytes = Encoding.ASCII.GetBytes(input);
            expectedHashCode = Ascii.GetHashCodeIgnoreCase(bytes);

            // just verify that the output is the same for multiple invocations
            for (int i = 0; i < 10; i++)
            {
                Assert.Equal(expectedHashCode, Ascii.GetHashCodeIgnoreCase(bytes));
                Assert.True(Ascii.TryGetHashCodeIgnoreCase(bytes, out actualHashCode));
                Assert.Equal(expectedHashCode, actualHashCode);
            }
        }
    }
}
