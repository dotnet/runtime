// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

using static System.Tests.Utf8TestUtilities;

namespace System.Tests
{
    public unsafe partial class Utf8StringTests
    {
        [Fact]
        public static void AreEquivalent_Utf8StringAndString_NullHandling()
        {
            Assert.True(Utf8String.AreEquivalent((Utf8String)null, (string)null));
            Assert.False(Utf8String.AreEquivalent(Utf8String.Empty, (string)null));
            Assert.False(Utf8String.AreEquivalent((Utf8String)null, string.Empty));
            Assert.True(Utf8String.AreEquivalent(Utf8String.Empty, string.Empty));
        }

        [Theory]
        [InlineData("Hello", "hello", false)]
        [InlineData("hello", "hello", true)]
        [InlineData("hello", "helloo", false)]
        [InlineData("hellooo", "helloo", false)]
        [InlineData("encyclopaedia", "encyclopaedia", true)]
        [InlineData("encyclopaedia", "encyclop\u00e6dia", false)]
        [InlineData("encyclop\u00e6dia", "encyclop\u00e6dia", true)]
        public static void AreEquivalent_Tests(string utf8Input, string utf16Input, bool expected)
        {
            Utf8String asUtf8 = u8(utf8Input);

            // Call all three overloads

            Assert.Equal(expected, Utf8String.AreEquivalent(asUtf8, utf16Input));
            Assert.Equal(expected, Utf8String.AreEquivalent(asUtf8.AsSpan(), utf16Input.AsSpan()));
            Assert.Equal(expected, Utf8String.AreEquivalent(asUtf8.AsBytes(), utf16Input.AsSpan()));
        }

        [Theory]
        [InlineData(new byte[] { 0xED, 0xA0, 0x80 }, new char[] { '\uD800' })] // don't support "wobbly" UTF-8
        [InlineData(new byte[] { 0xED, 0xA0, 0x80, 0xED, 0xBF, 0xBF }, new char[] { '\uD800', '\uDFFF' })] // don't support "wobbly" UTF-8
        [InlineData(new byte[] { 0xED }, new char[] { '\uD800' })] // don't support partials
        public static void AreEquivalent_IllFormedData_AlwaysReturnsFalse(byte[] asUtf8, char[] asUtf16)
        {
            Assert.False(Utf8String.AreEquivalent(asUtf8, asUtf16));
        }
    }
}
