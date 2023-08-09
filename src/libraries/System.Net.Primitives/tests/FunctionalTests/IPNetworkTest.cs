// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace System.Net.Primitives.Functional.Tests
{
    public class IPNetworkTest
    {
        public static TheoryData<string> IncorrectFormatData = new TheoryData<string>()
        {
            { "127.0.0.1" },
            { "A.B.C.D/24" },
            { "127.0.0.1/AB" },
            { "127.0.0.1/-1" },
            { "127.0.0.1/+1" },
            { "2a01:110:8012::/f" },
            { "" },
        };

        public static TheoryData<string> InvalidNetworkNotationData = new TheoryData<string>()
        {
            { "127.0.0.1/33" }, // PrefixLength max is 32 for IPv4
            { "ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff/129" }, // PrefixLength max is 128 for IPv6
            { "127.0.0.1/31" }, // Bits exceed the prefix length of 31 (32nd bit is on)
            { "198.51.255.0/23" }, // Bits exceed the prefix length of 23
            { "ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff/127" }, // Bits exceed the prefix length of 31
            { "2a01:110:8012::/45" }, // Bits exceed the prefix length of 45 (47th bit is on)
        };

        public static TheoryData<string> ValidIPNetworkData = new TheoryData<string>()
        {
            { "0.0.0.0/32" }, // the whole IPv4 space
            { "0.0.0.0/0" },
            { "128.0.0.0/1" },
            { "::/128" }, // the whole IPv6 space
            { "255.255.255.255/32" },
            { "198.51.254.0/23" },
            { "42.42.128.0/17" },
            { "ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff/128" },
            { "2a01:110:8012::/47" },
            { "2a01:110:8012::/100" },
        };

        [Theory]
        [MemberData(nameof(ValidIPNetworkData))]
        public void Constructor_Valid_Succeeds(string input)
        {
            string[] splitInput = input.Split('/');
            IPAddress address = IPAddress.Parse(splitInput[0]);
            int prefixLegth = int.Parse(splitInput[1]);

            IPNetwork network = new IPNetwork(address, prefixLegth);

            Assert.Equal(address, network.BaseAddress);
            Assert.Equal(prefixLegth, network.PrefixLength);
        }

        [Fact]
        public void Constructor_NullIPAddress_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new IPNetwork(null, 1));
        }

        [Theory]
        [InlineData("192.168.0.1", -1)]
        [InlineData("192.168.0.1", 33)]
        [InlineData("::", -1)]
        [InlineData("ffff::", 129)]
        public void Constructor_PrefixLenghtOutOfRange_ThrowsArgumentOutOfRangeException(string ipStr, int prefixLength)
        {
            IPAddress address = IPAddress.Parse(ipStr);
            Assert.Throws<ArgumentOutOfRangeException>(() => new IPNetwork(address, prefixLength));
        }

        [Theory]
        [InlineData("192.168.0.1", 31)]
        [InlineData("42.42.192.0", 17)]
        [InlineData("128.0.0.0", 0)]
        [InlineData("2a01:110:8012::", 46)]
        public void Constructor_NonZeroBitsAfterNetworkPrefix_ThrowsArgumentException(string ipStr, int prefixLength)
        {
            IPAddress address = IPAddress.Parse(ipStr);
            Assert.Throws<ArgumentException>(() => new IPNetwork(address, prefixLength));
        }

        [Theory]
        [MemberData(nameof(IncorrectFormatData))]
        public void Parse_IncorrectFormat_ThrowsFormatException(string input)
        {
            Assert.Throws<FormatException>(() => IPNetwork.Parse(input));
        }

        [Theory]
        [MemberData(nameof(IncorrectFormatData))]
        public void TryParse_IncorrectFormat_ReturnsFalse(string input)
        {
            Assert.False(IPNetwork.TryParse(input, out _));
        }

        [Theory]
        [MemberData(nameof(InvalidNetworkNotationData))]
        public void Parse_InvalidNetworkNotation_ThrowsFormatException(string input)
        {
            Assert.Throws<FormatException>(() => IPNetwork.Parse(input));
        }

        [Theory]
        [MemberData(nameof(InvalidNetworkNotationData))]
        public void TryParse_InvalidNetworkNotation_ReturnsFalse(string input)
        {
            Assert.False(IPNetwork.TryParse(input, out _));
        }

        [Theory]
        [MemberData(nameof(ValidIPNetworkData))]
        public void Parse_ValidNetworkNotation_Succeeds(string input)
        {
            var network = IPNetwork.Parse(input);
            Assert.Equal(input, network.ToString());
        }

        [Theory]
        [MemberData(nameof(ValidIPNetworkData))]
        public void TryParse_ValidNetworkNotation_Succeeds(string input)
        {
            Assert.True(IPNetwork.TryParse(input, out IPNetwork network));
            Assert.Equal(input, network.ToString());
        }

        [Fact]
        public void Contains_Null_ThrowsArgumentNullException()
        {
            IPNetwork v4 = IPNetwork.Parse("127.0.0.0/8");
            IPNetwork v6 = IPNetwork.Parse("::1/128");

            Assert.Throws<ArgumentNullException>(() => v4.Contains(null));
            Assert.Throws<ArgumentNullException>(() => v6.Contains(null));
        }

        [Fact]
        public void Contains_DifferentAddressFamily_ReturnsFalse()
        {
            IPNetwork network = IPNetwork.Parse("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff/128");
            Assert.False(network.Contains(IPAddress.Loopback));
        }

        [Theory]
        [InlineData("0.0.0.0/0", "0.0.0.0", "127.127.127.127", "255.255.255.255")] // the whole IPv4 space
        [InlineData("::/0", "::", "ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")] // the whole IPv6 space
        [InlineData("255.255.255.255/32", "255.255.255.255")] // single IPv4 address
        [InlineData("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff/128", "ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")] // single IPv6 address
        [InlineData("255.255.255.0/24", "255.255.255.0", "255.255.255.255")]
        [InlineData("198.51.248.0/22", "198.51.248.0", "198.51.250.42", "198.51.251.255")]
        [InlineData("255.255.255.128/25", "255.255.255.128", "255.255.255.129", "255.255.255.255")]
        [InlineData("2a00::/13", "2a00::", "2a00::1", "2a01::", "2a07::", "2a07:ffff:ffff:ffff:ffff:ffff:ffff:ffff")]
        [InlineData("2a01:110:8012::/47", "2a01:110:8012::", "2a01:110:8012:42::", "2a01:110:8013::", "2a01:110:8013:ffff:ffff:ffff:ffff:ffff")]
        [InlineData("2a01:110:8012:1012:314f:2a00::/87", "2a01:110:8012:1012:314f:2a00::", "2a01:110:8012:1012:314f:2a00::1", "2a01:110:8012:1012:314f:2a00:abcd:4242", "2a01:110:8012:1012:314f:2bff:ffff:ffff")]
        [InlineData("2a01:110:8012:1010:914e:2451:1700:0/105", "2a01:110:8012:1010:914e:2451:1700:0", "2a01:110:8012:1010:914e:2451:1742:4242", "2a01:110:8012:1010:914e:2451:177f:ffff")]
        public void Contains_WhenInNework_ReturnsTrue(string networkString, params string[] addresses)
        {
            var network = IPNetwork.Parse(networkString);

            foreach (string address in addresses)
            {
                Assert.True(network.Contains(IPAddress.Parse(address)));
            }
        }

        [Theory]
        [InlineData("255.255.255.255/32", "255.255.255.254")] // single IPv4 address
        [InlineData("ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff/128", "ffff:ffff:ffff:ffff:ffff:ffff:ffff:fffe")] // single IPv6 address
        [InlineData("255.255.255.0/24", "255.255.254.0")]
        [InlineData("198.51.248.0/22", "198.50.248.1", "198.52.248.1", "198.51.247.1", "198.51.252.1")]
        [InlineData("255.255.255.128/25", "255.255.255.127")]
        [InlineData("2a00::/13", "2900:ffff:ffff:ffff:ffff:ffff:ffff:ffff", "2a08::", "2a10::", "3a00::", "2b00::")]
        [InlineData("2a01:110:8012::/47", "2a01:110:8011:1::", "2a01:110:8014::", "2a00:110:8012::1", "2a01:111:8012::")]
        [InlineData("2a01:110:8012:1012:314f:2a00::/87", "2a01:110:8012:1012:314f:2c00::", "2a01:110:8012:1012:314f:2900::", "2a01:110:8012:1012:324f:2aff:ffff:ffff")]
        [InlineData("2a01:110:8012:1010:914e:2451:1700:0/105", "2a01:110:8012:1010:914e:2451:16ff:ffff", "2a01:110:8012:1010:914e:2451:1780:0", "2a01:110:8013:1010:914e:2451:1700:0")]
        public void Contains_WhenNotInNetwork_ReturnsFalse(string networkString, params string[] addresses)
        {
            var network = IPNetwork.Parse(networkString);

            foreach (string address in addresses)
            {
                Assert.False(network.Contains(IPAddress.Parse(address)));
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Equals_WhenDifferent_ReturnsFalse(bool testOperator)
        {
            var network = IPNetwork.Parse("127.0.0.0/24");

            var rangeWithDifferentPrefix = IPNetwork.Parse("127.0.1.0/24");
            var rangeWithDifferentPrefixLength = IPNetwork.Parse("127.0.0.0/25");

            if (testOperator)
            {
                Assert.False(network == rangeWithDifferentPrefix);
                Assert.False(network == rangeWithDifferentPrefixLength);
                Assert.True(network != rangeWithDifferentPrefix);
                Assert.True(network != rangeWithDifferentPrefixLength);
            }
            else
            {
                Assert.False(network.Equals(rangeWithDifferentPrefix));
                Assert.False(network.Equals(rangeWithDifferentPrefixLength));

                Assert.False(network.Equals((object)rangeWithDifferentPrefix));
                Assert.False(network.Equals((object)rangeWithDifferentPrefixLength));
            }
        }

        [Theory]
        [InlineData("127.0.0.0/24")]
        [InlineData("2a01:110:8012::/47")]
        public void EqualiyMethods_WhenEqual(string input)
        {
            var a = IPNetwork.Parse(input);
            var b = IPNetwork.Parse(input);

            Assert.True(a.Equals(b));
            Assert.True(a.Equals((object)b));
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

        public static IEnumerable<object[]> CidrInputs() =>
            new[]
            {
                "127.0.0.0/24",
                "172.16.0.0/12",
                "10.0.0.0/16",
                "192.168.2.0/24",
            }.Select(s => new object[] { s });

        [Theory]
        [MemberData(nameof(CidrInputs))]
        public void TryFormatSpan_NotEnoughLength_ReturnsFalse(string input)
        {
            IPNetwork network = IPNetwork.Parse(input);

            // UTF16
            {
                Span<char> span = stackalloc char[input.Length - 1];
                Assert.False(network.TryFormat(span, out int charsWritten));
                Assert.Equal(0, charsWritten);
            }

            // UTF8
            {
                Span<byte> span = stackalloc byte[input.Length - 1];
                Assert.False(network.TryFormat(span, out int bytesWritten));
                Assert.Equal(0, bytesWritten);
            }
        }

        [Theory]
        [MemberData(nameof(CidrInputs))]
        public void TryFormatSpan_EnoughLength_Succeeds(string input)
        {
            IPNetwork network = IPNetwork.Parse(input);

            for (int additionalLength = 0; additionalLength < 3; additionalLength++)
            {
                // UTF16
                {
                    Span<char> span = stackalloc char[input.Length + additionalLength];
                    Assert.True(network.TryFormat(span, out int charsWritten));
                    Assert.Equal(input.Length, charsWritten);
                    Assert.Equal(input, span.Slice(0, charsWritten).ToString());
                }

                // UTF8
                {
                    Span<byte> span = stackalloc byte[input.Length + additionalLength];
                    Assert.True(network.TryFormat(span, out int bytesWritten));
                    Assert.Equal(input.Length, bytesWritten);
                    Assert.Equal(input, Encoding.UTF8.GetString(span.Slice(0, bytesWritten)));
                }
            }
        }

        [Fact]
        public void DefaultInstance_IsValid()
        {
            IPNetwork network = default;
            Assert.Equal(IPAddress.Any, network.BaseAddress);
            Assert.Equal(default, network);
            Assert.NotEqual(IPNetwork.Parse("10.20.30.0/24"), network);
            Assert.True(network.Contains(IPAddress.Parse("10.11.12.13")));
        }
    }
}
