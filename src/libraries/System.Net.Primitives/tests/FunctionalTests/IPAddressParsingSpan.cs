// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Sockets;
using Xunit;

namespace System.Net.Primitives.Functional.Tests
{
    public sealed class IPAddressParsingSpan : IPAddressParsing
    {
        const int IPv4MaxLength = 15;

        [Theory]
        [MemberData(nameof(ValidIpv4Addresses))]
        public void ParseIPv4Span_ValidAddress_Success(string address, string expected)
        {
            IPAddress ip = IPAddress.Parse(address.AsReadOnlySpan());

            // Validate the ToString of the parsed address matches the expected value
            Assert.Equal(expected, ip.ToString());
            Assert.Equal(AddressFamily.InterNetwork, ip.AddressFamily);

            // Validate the ToString representation can be parsed as well back into the same IP
            IPAddress ip2 = IPAddress.Parse(address.AsReadOnlySpan());
            Assert.Equal(ip, ip2);
        }

        [Theory]
        [MemberData(nameof(ValidIpv4Addresses))]
        public void TryParseIPv4Span_ValidAddress_Success(string address, string expected)
        {
            Assert.True(IPAddress.TryParse(address.AsReadOnlySpan(), out IPAddress ip));

            // Validate the ToString of the parsed address matches the expected value
            Assert.Equal(expected, ip.ToString());
            Assert.Equal(AddressFamily.InterNetwork, ip.AddressFamily);

            // Validate the ToString representation can be parsed as well back into the same IP
            Assert.True(IPAddress.TryParse(ip.ToString().AsReadOnlySpan(), out IPAddress ip2));
            Assert.Equal(ip, ip2);
        }

        [Theory]
        [MemberData(nameof(ValidIpv4Addresses))]
        public void TryFormatIPv4_ValidAddress_Success(string address, string expected)
        {
            const int IPv4MaxPlusOneLength = IPv4MaxLength + 1;

            var ip = IPAddress.Parse(address);

            var exactSize = new char[IPv4MaxLength];
            Assert.True(ip.TryFormat(new Span<char>(exactSize), out int charsWritten));
            Assert.Equal(expected.Length, charsWritten);
            Assert.Equal(expected, new string(exactSize, 0, charsWritten));

            var largerThanRequired = new char[IPv4MaxPlusOneLength];
            Assert.True(ip.TryFormat(new Span<char>(largerThanRequired), out charsWritten));
            Assert.Equal(expected.Length, charsWritten);
            Assert.Equal(expected, new string(largerThanRequired, 0, charsWritten));
        }

        [Theory]
        [MemberData(nameof(ValidIpv4Addresses))]
        public void TryFormatIPv4_ProvidedBufferTooSmall_Failure(string address, string expected)
        {
            const int bufferSize = IPv4MaxLength - 1;
            var ip = IPAddress.Parse(address);

            var result = new char[bufferSize];
            Assert.False(ip.TryFormat(new Span<char>(result), out int charsWritten));
            Assert.Equal(0, charsWritten);
            Assert.Equal<char>(new char[bufferSize], result);
        }

        [Theory]
        [MemberData(nameof(InvalidIpv4Addresses))]
        public void TryParseIPv4_InvalidAddress_Failure(string address)
        {
            Assert.False(IPAddress.TryParse(address.AsReadOnlySpan(), out IPAddress ip));
            Assert.Equal(null, ip);
        }

        [Theory]
        [MemberData(nameof(ValidIpv6Addresses))]
        public void ParseIPv6Span_ValidAddress_RoundtripMatchesExpected(string address, string expected)
        {
            IPAddress ip = IPAddress.Parse(address.AsReadOnlySpan());

            // Validate the ToString of the parsed address matches the expected value
            Assert.Equal(expected.ToLowerInvariant(), ip.ToString());
            Assert.Equal(AddressFamily.InterNetworkV6, ip.AddressFamily);

            // Validate the ToString representation can be parsed as well back into the same IP
            IPAddress ip2 = IPAddress.Parse(address.AsReadOnlySpan());
            Assert.Equal(ip, ip2);

            // Validate that anything that doesn't already start with brackets
            // can be surrounded with brackets and still parse successfully.
            if (!address.StartsWith("["))
            {
                Assert.Equal(
                    expected.ToLowerInvariant(),
                    IPAddress.Parse(("[" + address + "]").AsReadOnlySpan()).ToString());
            }
        }

        [Theory]
        [MemberData(nameof(ValidIpv6Addresses))]
        public void TryParseIPv6Span_ValidAddress_RoundtripMatchesExpected(string address, string expected)
        {
            Assert.True(IPAddress.TryParse(address.AsReadOnlySpan(), out IPAddress ip));

            // Validate the ToString of the parsed address matches the expected value
            Assert.Equal(expected.ToLowerInvariant(), ip.ToString());
            Assert.Equal(AddressFamily.InterNetworkV6, ip.AddressFamily);

            // Validate the ToString representation can be parsed as well back into the same IP
            Assert.True(IPAddress.TryParse(ip.ToString().AsReadOnlySpan(), out IPAddress ip2));
            Assert.Equal(ip, ip2);

            // Validate that anything that doesn't already start with brackets
            // can be surrounded with brackets and still parse successfully.
            if (!address.StartsWith("["))
            {
                Assert.True(IPAddress.TryParse(("[" + address + "]").AsReadOnlySpan(), out IPAddress ip3));
                Assert.Equal(expected.ToLowerInvariant(), ip3.ToString());
            }
        }

        [Theory]
        [MemberData(nameof(ValidIpv6Addresses))]
        public void TryFormatIPv6_ValidAddress_Success(string address, string expected)
        {
            var ip = IPAddress.Parse(address);

            var exactSize = new char[expected.Length];
            Assert.True(ip.TryFormat(new Span<char>(exactSize), out int charsWritten));
            Assert.Equal(expected.Length, charsWritten);
            Assert.Equal(expected.ToLowerInvariant(), new string(exactSize, 0, charsWritten));

            var largerThanRequired = new char[expected.Length + 1];
            Assert.True(ip.TryFormat(new Span<char>(largerThanRequired), out charsWritten));
            Assert.Equal(expected.Length, charsWritten);
            Assert.Equal(expected.ToLowerInvariant(), new string(largerThanRequired, 0, charsWritten));
        }

        [Theory]
        [MemberData(nameof(ValidIpv6Addresses))]
        public void TryFormatIPv6_ProvidedBufferTooSmall_Failure(string address, string expected)
        {
            int bufferSize = expected.Length - 1;
            var result = new char[bufferSize];
            var ip = IPAddress.Parse(address);

            Assert.False(ip.TryFormat(new Span<char>(result), out int charsWritten));
            Assert.Equal(0, charsWritten);
            Assert.Equal<char>(new char[bufferSize], result);
        }

        [Theory]
        [MemberData(nameof(InvalidIpv6Addresses))]
        public void TryParseIPv6_InvalidAddress_ReturnsFalse(string invalidAddress)
        {
            Assert.False(IPAddress.TryParse(invalidAddress.AsReadOnlySpan(), out IPAddress ip));
            Assert.Equal(null, ip);
        }

    }
}
