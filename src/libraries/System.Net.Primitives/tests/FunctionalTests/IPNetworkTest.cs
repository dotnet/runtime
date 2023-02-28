// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Net.Primitives.Functional.Tests
{
    public class IPNetworkTest
    {
        [Theory]
        [InlineData("127.0.0.1")]
        [InlineData("A.B.C.D/24")]
        [InlineData("127.0.0.1/AB")]
        [InlineData("")]
        public void Parse_IncorrectFormat_Throws(string input)
        {
            Assert.Throws<FormatException>(() => IPNetwork.Parse(input));
        }

        [Theory]
        [InlineData("127.0.0.1/33")] // PrefixLength max is 32
        [InlineData("127.0.0.1/31")] // LastSetBit in the host mask (32nd bit is on)
        [InlineData("")]
        public void Parse_InvalidNetworkNotation_Throws(string input)
        {
            Assert.Throws<FormatException>(() => IPNetwork.Parse(input));
        }

        [Theory]
        [InlineData("0.0.0.0/32")] // the whole IPv4 space
        [InlineData("::/128")] // the whole IPv6 space
        [InlineData("255.255.255.255/32")] // single IPv4 address
        [InlineData("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff/128")] // single IPv6 address
        public void Parse_ValidNetworkNotation_Succeeds(string input)
        {
            var network = IPNetwork.Parse(input);
            Assert.Equal(input, network.ToString());
        }

        [Theory]
        [InlineData("0.0.0.0/0", "0.0.0.0", "127.127.127.127", "255.255.255.255")] // the whole IPv4 space
        [InlineData("::/0", "::", "ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")] // the whole IPv6 space
        [InlineData("255.255.255.255/32", "255.255.255.255")] // single IPv4 address
        [InlineData("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff/128", "ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")] // single IPv6 address
        [InlineData("255.255.255.0/24", "255.255.255.0", "255.255.255.255")]
        [InlineData("255.255.255.128/25", "255.255.255.129", "255.255.255.255")]
        public void Contains_ValidAddresses_ReturnsTrue(string networkString, params string[] validAddresses)
        {
            var network = IPNetwork.Parse(networkString);

            foreach (var address in validAddresses)
            {
                Assert.True(network.Contains(IPAddress.Parse(address)));
            }
        }

        [Theory]
        [InlineData("255.255.255.255/32", "255.255.255.254")] // single IPv4 address
        [InlineData("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff/128", "ffff:ffff:ffff:ffff:ffff:ffff:ffff:fffe")] // single IPv6 address
        [InlineData("255.255.255.0/24", "255.255.254.0")]
        [InlineData("255.255.255.128/25", "255.255.255.127")]
        public void Contains_InvalidAddresses_ReturnsFalse(string networkString, params string[] invalidAddresses)
        {
            var network = IPNetwork.Parse(networkString);

            foreach (var address in invalidAddresses)
            {
                Assert.False(network.Contains(IPAddress.Parse(address)));
            }
        }

        [Fact]
        public void Equals_WhenDifferent_ReturnsFalse()
        {
            var network = IPNetwork.Parse("127.0.0.0/24");

            var rangeWithDifferentPrefix = IPNetwork.Parse("127.0.1.0/24");
            var rangeWithDifferentPrefixLength = IPNetwork.Parse("127.0.0.0/25");

            Assert.False(network.Equals(rangeWithDifferentPrefix));
            Assert.False(network.Equals(rangeWithDifferentPrefixLength));
        }

        [Fact]
        public void Equals_WhenSame_ReturnsTrue()
        {
            var a = IPNetwork.Parse("127.0.0.0/24");
            var b = IPNetwork.Parse("127.0.0.0/24");

            Assert.True(a.Equals(b));
            Assert.True(a == b);
            Assert.False(a != b);
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        [Fact]
        public void Equals_WhenNull_ReturnsFalse()
        {
            var network = IPNetwork.Parse("127.0.0.0/24");

            Assert.False(network.Equals(null));
        }

        [Fact]
        public void TryFormatSpan_EnoughLength_Succeeds()
        {
            var input = "127.0.0.0/24";
            var network = IPNetwork.Parse(input);

            Span<char> span = stackalloc char[15]; // IPAddress.TryFormat requires a size of 15

            Assert.True(network.TryFormat(span, out int charsWritten));
            Assert.Equal(input.Length, charsWritten);
            Assert.Equal(input, span.Slice(0, charsWritten).ToString());
        }

        [Theory]
        [InlineData("127.127.127.127/32", 15)]
        [InlineData("127.127.127.127/32", 0)]
        [InlineData("127.127.127.127/32", 1)]
        public void TryFormatSpan_NotEnoughLength_FailsWithoutException(string input, int spanLengthToTest)
        {
            var network = IPNetwork.Parse(input);

            Span<char> span = stackalloc char[spanLengthToTest];

            Assert.False(network.TryFormat(span, out int charsWritten));
        }
    }
}
