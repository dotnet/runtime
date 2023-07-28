// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using Xunit;

namespace System.Net.Primitives.Functional.Tests
{
    public class IPAddressParsingFormatting_String : IPAddressParsingFormatting
    {
        public override IPAddress Parse(string ipString) => IPAddress.Parse(ipString);
        public override bool TryParse(string ipString, out IPAddress address) => IPAddress.TryParse(ipString, out address);
        public virtual string ToString(IPAddress address) => address.ToString();

        [Fact]
        public void Parse_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => Parse((string)null));

            Assert.False(TryParse((string)null, out IPAddress ipAddress));
            Assert.Null(ipAddress);
        }

        [Theory]
        [MemberData(nameof(ValidIpv4Addresses))]
        [MemberData(nameof(ValidIpv6Addresses))]
        public void ToString_MatchesExpected(string addressString, string expected)
        {
            IPAddress address = Parse(addressString);
            Assert.Equal(expected.ToLowerInvariant(), ToString(address));
        }
    }

    public class IPAddressParsingFormatting_Span : IPAddressParsingFormatting
    {
        public override IPAddress Parse(string ipString) => IPAddress.Parse(ipString.AsSpan());
        public override bool TryParse(string ipString, out IPAddress address) => IPAddress.TryParse(ipString.AsSpan(), out address);
        public virtual bool TryFormat(IPAddress address, Span<char> destination, out int charsWritten) => address.TryFormat(destination, out charsWritten);
        public virtual bool TryFormat(IPAddress address, Span<byte> utf8Destination, out int bytesWritten) => address.TryFormat(utf8Destination, out bytesWritten);

        [Theory]
        [MemberData(nameof(ValidIpv4Addresses))]
        [MemberData(nameof(ValidIpv6Addresses))]
        public void TryFormat_ProvidedBufferTooSmall_Failure(string addressString, string expected)
        {
            _ = expected;
            IPAddress address = Parse(addressString);

            // UTF16
            {
                var result = new char[address.ToString().Length - 1];
                Assert.False(TryFormat(address, new Span<char>(result), out int charsWritten));
                Assert.Equal(0, charsWritten);
            }

            // UTF8
            {
                var result = new byte[address.ToString().Length - 1];
                Assert.False(TryFormat(address, new Span<byte>(result), out int bytesWritten));
                Assert.Equal(0, bytesWritten);
            }
        }

        [Theory]
        [MemberData(nameof(ValidIpv4Addresses))]
        [MemberData(nameof(ValidIpv6Addresses))]
        public void TryFormat_ProvidedBufferExactRightSize_Success(string addressString, string expected)
        {
            IPAddress address = Parse(addressString);
            int requiredLength = address.ToString().Length;

            // UTF16
            {
                var exactRequired = new char[requiredLength];
                Assert.True(TryFormat(address, new Span<char>(exactRequired), out int charsWritten));
                Assert.Equal(expected.Length, charsWritten);
                Assert.Equal(
                    address.AddressFamily == AddressFamily.InterNetworkV6 ? expected.ToLowerInvariant() : expected,
                    new string(exactRequired));
            }

            // UTF8
            {
                var exactRequired = new byte[requiredLength];
                Assert.True(TryFormat(address, new Span<byte>(exactRequired), out int bytesWritten));
                Assert.Equal(expected.Length, bytesWritten);
                Assert.Equal(
                    address.AddressFamily == AddressFamily.InterNetworkV6 ? expected.ToLowerInvariant() : expected,
                    Encoding.UTF8.GetString(exactRequired));
            }
        }

        [Theory]
        [MemberData(nameof(ValidIpv4Addresses))]
        [MemberData(nameof(ValidIpv6Addresses))]
        public void TryFormat_ProvidedBufferLargerThanNeeded_Success(string addressString, string expected)
        {
            IPAddress address = Parse(addressString);
            int requiredLength = address.ToString().Length;

            // UTF16
            {
                var largerThanRequired = new char[requiredLength + 1];
                Assert.True(TryFormat(address, new Span<char>(largerThanRequired), out int charsWritten));
                Assert.Equal(expected.Length, charsWritten);
                Assert.Equal(
                    address.AddressFamily == AddressFamily.InterNetworkV6 ? expected.ToLowerInvariant() : expected,
                    new string(largerThanRequired, 0, charsWritten));
            }

            // UTF8
            {
                var largerThanRequired = new byte[requiredLength + 1];
                Assert.True(TryFormat(address, new Span<byte>(largerThanRequired), out int charsWritten));
                Assert.Equal(expected.Length, charsWritten);
                Assert.Equal(
                    address.AddressFamily == AddressFamily.InterNetworkV6 ? expected.ToLowerInvariant() : expected,
                    Encoding.UTF8.GetString(largerThanRequired.AsSpan(0, charsWritten)));
            }
        }
    }

    public sealed class IPAddressParsingFormatting_IParsable_IFormattable : IPAddressParsingFormatting_String
    {
        public override IPAddress Parse(string ipString) => Parse<IPAddress>(ipString);
        public override bool TryParse(string ipString, out IPAddress address) => TryParse<IPAddress>(ipString, out address);
        public override string ToString(IPAddress address) => ((IFormattable)address).ToString(null, null);

        private static T Parse<T>(string s) where T : IParsable<T> => T.Parse(s, null);
        private static bool TryParse<T>(string s, out T result) where T : IParsable<T> => T.TryParse(s, null, out result);
    }

    public sealed class IPAddressParsingFormatting_ISpanParsable_ISpanFormattable : IPAddressParsingFormatting_Span
    {
        public override IPAddress Parse(string ipString) => Parse<IPAddress>(ipString);
        public override bool TryParse(string ipString, out IPAddress address) => TryParse<IPAddress>(ipString, out address);
        public override bool TryFormat(IPAddress address, Span<char> destination, out int charsWritten) => ((ISpanFormattable)address).TryFormat(destination, out charsWritten, default, null);

        private static T Parse<T>(string s) where T : ISpanParsable<T> => T.Parse(s.AsSpan(), null);
        private static bool TryParse<T>(string s, out T result) where T : ISpanParsable<T> => T.TryParse(s.AsSpan(), null, out result);
    }

    public abstract class IPAddressParsingFormatting
    {
        public abstract IPAddress Parse(string ipString);
        public abstract bool TryParse(string ipString, out IPAddress address);

        public static readonly object[][] ValidIpv4Addresses =
        {
            // Decimal
            new object[] { "192.168.0.1", "192.168.0.1" },
            new object[] { "0.0.0.0", "0.0.0.0" },
            new object[] { "0", "0.0.0.0" },
            new object[] { "12", "0.0.0.12" },
            new object[] { "12.1.7", "12.1.0.7" },
            new object[] { "12.1.7", "12.1.0.7" },
            new object[] { "255.255.255.255", "255.255.255.255" },
            new object[] { "20.65535", "20.0.255.255" },
            new object[] { "157.3873051", "157.59.25.27" },
            new object[] { "157.6427", "157.0.25.27" },
            new object[] { "65535", "0.0.255.255" },
            new object[] { "65536", "0.1.0.0" },
            new object[] { "1434328179", "85.126.28.115" },
            new object[] { "2637895963", "157.59.25.27" },
            new object[] { "3397943208", "202.136.127.168" },
            new object[] { "4294967294", "255.255.255.254" },
            new object[] { "4294967295", "255.255.255.255" },
            //Hex
            new object[] { "0xFF.0xFF.0xFF.0xFF", "255.255.255.255" },
            new object[] { "0x0", "0.0.0.0" },
            new object[] { "0xFFFFFFFE", "255.255.255.254" },
            new object[] { "0xFFFFFFFF", "255.255.255.255" },
            new object[] { "0x9D3B191B", "157.59.25.27" },
            new object[] { "0X9D.0x3B.0X19.0x1B", "157.59.25.27" },
            new object[] { "0x89.0xab.0xcd.0xef", "137.171.205.239" },
            new object[] { "0xff.0x7f.0x20.0x01", "255.127.32.1" },
            // Octal
            new object[] { "0313.027035210", "203.92.58.136" },
            new object[] { "0313.0134.035210", "203.92.58.136" },
            new object[] { "0377.0377.0377.0377", "255.255.255.255" },
            new object[] { "037777777776", "255.255.255.254" },
            new object[] { "037777777777", "255.255.255.255" },
            new object[] { "023516614433", "157.59.25.27" },
            new object[] { "00000023516614433", "157.59.25.27" },
            new object[] { "000235.000073.0000031.00000033", "157.59.25.27" },
            new object[] { "0235.073.031.033", "157.59.25.27" },
            new object[] { "157.59.25.033", "157.59.25.27" }, // Partial octal
            // Mixed base
            new object[] { "157.59.25.0x1B", "157.59.25.27" },
            new object[] { "157.59.0x001B", "157.59.0.27" },
            new object[] { "157.0x00001B", "157.0.0.27" },
            new object[] { "157.59.0x25.033", "157.59.37.27" },
        };

        [Theory]
        [MemberData(nameof(ValidIpv4Addresses))]
        public void ParseIPv4_ValidAddress_Success(string address, string expected)
        {
            IPAddress ip = Parse(address);

            // Validate the ToString of the parsed address matches the expected value
            Assert.Equal(expected, ip.ToString());
            Assert.Equal(AddressFamily.InterNetwork, ip.AddressFamily);

            // Validate the ToString representation can be parsed as well back into the same IP
            IPAddress ip2 = Parse(ip.ToString());
            Assert.Equal(ip, ip2);
        }

        public static readonly object[][] InvalidIpv4Addresses =
        {
            new object[] { " 127.0.0.1" }, // leading whitespace
            new object[] { "127.0.0.1 " }, // trailing whitespace
            new object[] { " 127.0.0.1 " }, // leading and trailing whitespace
            new object[] { "192.168.0.0/16" }, // with subnet
            new object[] { "157.3B191B" }, // Hex without 0x
            new object[] { "1.1.1.0x" }, // Empty trailing hex segment
            new object[] { "0000X9D.0x3B.0X19.0x1B" }, // Leading zeros on hex
            new object[] { "0x.1.1.1" }, // Empty leading hex segment
            new object[] { "260.156" }, // Left dotted segments can't be more than 255
            new object[] { "255.260.156" }, // Left dotted segments can't be more than 255
            new object[] { "255.1.1.256" }, // Right dotted segment can't be more than 255
            new object[] { "0xFF.0xFFFFFF.0xFF" }, // Middle segment too large
            new object[] { "0xFFFFFF.0xFF.0xFFFFFF" }, // Leading segment too large
            new object[] { "4294967296" }, // Decimal overflow by 1
            new object[] { "040000000000" }, // Octal overflow by 1
            new object[] { "01011101001110110001100100011011" }, // Binary? Read as octal, overflows
            new object[] { "10011101001110110001100100011011" }, // Binary? Read as decimal, overflows
            new object[] { "0x100000000" }, // Hex overflow by 1
            new object[] { "1.1\u67081.1.1" }, // Invalid char (unicode)
            new object[] { "..." }, // Empty sections
            new object[] { "1.1.1." }, // Empty trailing section
            new object[] { "1..1.1" }, // Empty internal section
            new object[] { ".1.1.1" }, // Empty leading section
            new object[] { "..11.1" }, // Empty sections
            new object[] { " text" }, // alpha text
            new object[] { "1.. ." }, // whitespace section
            new object[] { "12.1.8. " }, // trailing whitespace section
            new object[] { "12.+1.1.4" }, // plus sign in section
            new object[] { "12.1.-1.5" }, // minus sign in section
            new object[] { "12.1.abc.5" }, // text in section
        };

        public static readonly object[][] InvalidIpv4AddressesStandalone = // but valid as part of IPv6 addresses
        {
            new object[] { "" }, // empty
            new object[] { " " }, // whitespace
            new object[] { "  " }, // whitespace
            new object[] { "0.0.0.089" }, // Octal (leading zero) but with 8 or 9
        };

        [Theory]
        [MemberData(nameof(InvalidIpv4Addresses))]
        [MemberData(nameof(InvalidIpv4AddressesStandalone))]
        public void ParseIPv4_InvalidAddress_Failure(string address)
        {
            ParseInvalidAddress(address, hasInnerSocketException: !PlatformDetection.IsNetFramework);
        }


        public static readonly object[][] Ipv4AddressesWithPort =
        {
            new object[] { "192.168.0.0:80" }, // with port
            new object[] { "192.168.0.1:80" }, // with port
        };

        [Theory]
        [MemberData(nameof(Ipv4AddressesWithPort))]
        public void ParseIPv4_InvalidAddress_ThrowsFormatExceptionWithInnerException(string address)
        {
            ParseInvalidAddress(address, hasInnerSocketException: true);
        }

        public static readonly object[][] ValidIpv6Addresses =
        {
            new object[] { "Fe08::1", "fe08::1" },
            new object[] { "0000:0000:0000:0000:0000:0000:0000:0000", "::" },
            new object[] { "FFFF:FFFF:FFFF:FFFF:FFFF:FFFF:FFFF:FFFF", "ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff" },
            new object[] { "0:0:0:0:0:0:0:0", "::" },
            new object[] { "1:0:0:0:0:0:0:0", "1::" },
            new object[] { "0:1:0:0:0:0:0:0", "0:1::" },
            new object[] { "0:0:1:0:0:0:0:0", "0:0:1::" },
            new object[] { "0:0:0:1:0:0:0:0", "0:0:0:1::" },
            new object[] { "0:0:0:0:1:0:0:0", "::1:0:0:0" },
            new object[] { "0:0:0:0:0:1:0:0", "::1:0:0" },
            new object[] { "0:0:0:0:0:0:1:0", "::0.1.0.0" },
            new object[] { "0:0:0:0:0:0:2:0", "::0.2.0.0" },
            new object[] { "0:0:0:0:0:0:F:0", "::0.15.0.0" },
            new object[] { "0:0:0:0:0:0:10:0", "::0.16.0.0" },
            new object[] { "0:0:0:0:0:0:A0:0", "::0.160.0.0" },
            new object[] { "0:0:0:0:0:0:F0:0", "::0.240.0.0" },
            new object[] { "0:0:0:0:0:0:FF:0", "::0.255.0.0" },
            new object[] { "0:0:0:0:0:0:0:1", "::1" },
            new object[] { "0:0:0:0:0:0:0:2", "::2" },
            new object[] { "0:0:0:0:0:0:0:F", "::F" },
            new object[] { "0:0:0:0:0:0:0:10", "::10" },
            new object[] { "0:0:0:0:0:0:0:1A", "::1A" },
            new object[] { "0:0:0:0:0:0:0:A0", "::A0" },
            new object[] { "0:0:0:0:0:0:0:F0", "::F0" },
            new object[] { "0:0:0:0:0:0:0:FF", "::FF" },
            new object[] { "0:0:0:0:0:0:0:1001", "::1001" },
            new object[] { "0:0:0:0:0:0:0:1002", "::1002" },
            new object[] { "0:0:0:0:0:0:0:100F", "::100F" },
            new object[] { "0:0:0:0:0:0:0:1010", "::1010" },
            new object[] { "0:0:0:0:0:0:0:10A0", "::10A0" },
            new object[] { "0:0:0:0:0:0:0:10F0", "::10F0" },
            new object[] { "0:0:0:0:0:0:0:10FF", "::10FF" },
            new object[] { "0:0:0:0:0:0:1:1", "::0.1.0.1" },
            new object[] { "0:0:0:0:0:0:2:2", "::0.2.0.2" },
            new object[] { "0:0:0:0:0:0:F:F", "::0.15.0.15" },
            new object[] { "0:0:0:0:0:0:10:10", "::0.16.0.16" },
            new object[] { "0:0:0:0:0:0:A0:A0", "::0.160.0.160" },
            new object[] { "0:0:0:0:0:0:F0:F0", "::0.240.0.240" },
            new object[] { "0:0:0:0:0:0:FF:FF", "::0.255.0.255" },
            new object[] { "0:0:0:0:0:FFFF:0:1", "::FFFF:0:1" },
            new object[] { "0:0:0:0:0:FFFF:0:2", "::FFFF:0:2" },
            new object[] { "0:0:0:0:0:FFFF:0:F", "::FFFF:0:F" },
            new object[] { "0:0:0:0:0:FFFF:0:10", "::FFFF:0:10" },
            new object[] { "0:0:0:0:0:FFFF:0:A0", "::FFFF:0:A0" },
            new object[] { "0:0:0:0:0:FFFF:0:F0", "::FFFF:0:F0" },
            new object[] { "0:0:0:0:0:FFFF:0:FF", "::FFFF:0:FF" },
            new object[] { "0:0:0:0:0:FFFF:1:0", "::FFFF:0.1.0.0" },
            new object[] { "0:0:0:0:0:FFFF:2:0", "::FFFF:0.2.0.0" },
            new object[] { "0:0:0:0:0:FFFF:F:0", "::FFFF:0.15.0.0" },
            new object[] { "0:0:0:0:0:FFFF:10:0", "::FFFF:0.16.0.0" },
            new object[] { "0:0:0:0:0:FFFF:A0:0", "::FFFF:0.160.0.0" },
            new object[] { "0:0:0:0:0:FFFF:F0:0", "::FFFF:0.240.0.0" },
            new object[] { "0:0:0:0:0:FFFF:FF:0", "::FFFF:0.255.0.0" },
            new object[] { "0:0:0:0:0:FFFF:0:1001", "::FFFF:0:1001" },
            new object[] { "0:0:0:0:0:FFFF:0:1002", "::FFFF:0:1002" },
            new object[] { "0:0:0:0:0:FFFF:0:100F", "::FFFF:0:100F" },
            new object[] { "0:0:0:0:0:FFFF:0:1010", "::FFFF:0:1010" },
            new object[] { "0:0:0:0:0:FFFF:0:10A0", "::FFFF:0:10A0" },
            new object[] { "0:0:0:0:0:FFFF:0:10F0", "::FFFF:0:10F0" },
            new object[] { "0:0:0:0:0:FFFF:0:10FF", "::FFFF:0:10FF" },
            new object[] { "0:0:0:0:0:FFFF:1:1", "::FFFF:0.1.0.1" },
            new object[] { "0:0:0:0:0:FFFF:2:2", "::FFFF:0.2.0.2" },
            new object[] { "0:0:0:0:0:FFFF:F:F", "::FFFF:0.15.0.15" },
            new object[] { "0:0:0:0:0:FFFF:10:10", "::FFFF:0.16.0.16" },
            new object[] { "0:0:0:0:0:FFFF:A0:A0", "::FFFF:0.160.0.160" },
            new object[] { "0:0:0:0:0:FFFF:F0:F0", "::FFFF:0.240.0.240" },
            new object[] { "0:0:0:0:0:FFFF:FF:FF", "::FFFF:0.255.0.255" },
            new object[] { "0:7:7:7:7:7:7:7", "0:7:7:7:7:7:7:7" },
            new object[] { "1:0:0:0:0:0:0:1", "1::1" },
            new object[] { "1:1:0:0:0:0:0:0", "1:1::" },
            new object[] { "2:2:0:0:0:0:0:0", "2:2::" },
            new object[] { "1:1:0:0:0:0:0:1", "1:1::1" },
            new object[] { "1:0:1:0:0:0:0:1", "1:0:1::1" },
            new object[] { "1:0:0:1:0:0:0:1", "1:0:0:1::1" },
            new object[] { "1:0:0:0:1:0:0:1", "1::1:0:0:1" },
            new object[] { "1:0:0:0:0:1:0:1", "1::1:0:1" },
            new object[] { "1:0:0:0:0:0:1:1", "1::1:1" },
            new object[] { "1:1:0:0:1:0:0:1", "1:1::1:0:0:1" },
            new object[] { "1:0:1:0:0:1:0:1", "1:0:1::1:0:1" },
            new object[] { "1:0:0:1:0:0:1:1", "1::1:0:0:1:1" },
            new object[] { "1:1:0:0:0:1:0:1", "1:1::1:0:1" },
            new object[] { "1:0:0:0:1:0:1:1", "1::1:0:1:1" },
            new object[] { "1:1:1:1:1:1:1:0", "1:1:1:1:1:1:1:0" },
            new object[] { "7:7:7:7:7:7:7:0", "7:7:7:7:7:7:7:0" },
            new object[] { "E:0:0:0:0:0:0:1", "E::1" },
            new object[] { "E:0:0:0:0:0:2:2", "E::2:2" },
            new object[] { "E:0:6:6:6:6:6:6", "E:0:6:6:6:6:6:6" },
            new object[] { "E:E:0:0:0:0:0:1", "E:E::1" },
            new object[] { "E:E:0:0:0:0:2:2", "E:E::2:2" },
            new object[] { "E:E:0:5:5:5:5:5", "E:E:0:5:5:5:5:5" },
            new object[] { "E:E:E:0:0:0:0:1", "E:E:E::1" },
            new object[] { "E:E:E:0:0:0:2:2", "E:E:E::2:2" },
            new object[] { "E:E:E:0:4:4:4:4", "E:E:E:0:4:4:4:4" },
            new object[] { "E:E:E:E:0:0:0:1", "E:E:E:E::1" },
            new object[] { "E:E:E:E:0:0:2:2", "E:E:E:E::2:2" },
            new object[] { "E:E:E:E:0:3:3:3", "E:E:E:E:0:3:3:3" },
            new object[] { "E:E:E:E:E:0:0:1", "E:E:E:E:E::1" },
            new object[] { "E:E:E:E:E:0:2:2", "E:E:E:E:E:0:2:2" },
            new object[] { "E:E:E:E:E:E:0:1", "E:E:E:E:E:E:0:1" },
            new object[] { "::2:3:4:5:6:7:8", "0:2:3:4:5:6:7:8" },
            new object[] { "1:2:3:4:5:6:7::", "1:2:3:4:5:6:7:0" },
            new object[] { "::FFFF:192.168.0.1", "::FFFF:192.168.0.1" },
            new object[] { "::FFFF:0.168.0.1", "::FFFF:0.168.0.1" },
            new object[] { "::0.0.255.255", "::FFFF" },
            new object[] { "::EEEE:10.0.0.1", "::EEEE:A00:1" },
            new object[] { "::10.0.0.1", "::10.0.0.1" },
            new object[] { "1234:0:0:0:0:1234:0:0", "1234::1234:0:0" },
            new object[] { "1:0:1:0:1:0:1:0", "1:0:1:0:1:0:1:0" },
            new object[] { "1:1:1:0:0:1:1:0", "1:1:1::1:1:0" },
            new object[] { "0:0:0:0:0:1234:0:0", "::1234:0:0" },
            new object[] { "3ffe:38e1::0100:1:0001", "3ffe:38e1::100:1:1" },
            new object[] { "0:0:1:2:00:00:000:0000", "0:0:1:2::" },
            new object[] { "100:0:1:2:0:0:000:abcd", "100:0:1:2::abcd" },
            new object[] { "ffff:0:0:0:0:0:00:abcd", "ffff::abcd" },
            new object[] { "ffff:0:0:2:0:0:00:abcd", "ffff:0:0:2::abcd" },
            new object[] { "0:0:1:2:0:00:0000:0000", "0:0:1:2::" },
            new object[] { "0000:0000::1:0000:0000", "::1:0:0" },
            new object[] { "0:0:111:234:5:6:789A:0", "::111:234:5:6:789a:0" },
            new object[] { "11:22:33:44:55:66:77:8", "11:22:33:44:55:66:77:8" },
            new object[] { "::7711:ab42:1230:0:0:0", "0:0:7711:ab42:1230::" },
            new object[] { "::", "::" },
            new object[] { "[Fe08::1]", "fe08::1" }, // brackets dropped
            new object[] { "[Fe08::1]:0x80", "fe08::1" }, // brackets and port dropped
            new object[] { "[Fe08::1]:0xFA", "fe08::1" }, // brackets and port dropped
            new object[] { "2001:0db8::0001", "2001:db8::1" }, // leading 0s suppressed
            new object[] { "3731:54:65fe:2::a7", "3731:54:65fe:2::a7" }, // Unicast
            new object[] { "3731:54:65fe:2::a8", "3731:54:65fe:2::a8" }, // Anycast
            // ScopeID
            new object[] { "Fe08::1%13542", "fe08::1%13542" },
            new object[] { "1::%1", "1::%1" },
            new object[] { "::1%12", "::1%12" },
            new object[] { "::%123", "::%123" },
            new object[] { "Fe08::1%unknowninterface", "fe08::1" },
            // v4 as v6
            new object[] { "FE08::192.168.0.1", "fe08::c0a8:1" }, // Output is not IPv4 mapped
            new object[] { "::192.168.0.1", "::192.168.0.1" },
            new object[] { "::FFFF:192.168.0.1", "::ffff:192.168.0.1" }, // SIIT
            new object[] { "::FFFF:0:192.168.0.1", "::ffff:0:192.168.0.1" }, // SIIT
            new object[] { "::5EFE:192.168.0.1", "::5efe:192.168.0.1" }, // ISATAP
            new object[] { "1::5EFE:192.168.0.1", "1::5efe:192.168.0.1" }, // ISATAP
            new object[] { "::192.168.0.010", "::192.168.0.10" }, // Embedded IPv4 octal, read as decimal
        };

        [Theory]
        [MemberData(nameof(ValidIpv6Addresses))]
        public void ParseIPv6_ValidAddress_RoundtripMatchesExpected(string address, string expected)
        {
            IPAddress ip = Parse(address);

            // Validate the ToString of the parsed address matches the expected value
            Assert.Equal(expected.ToLowerInvariant(), ip.ToString());
            Assert.Equal(AddressFamily.InterNetworkV6, ip.AddressFamily);

            // Validate the ToString representation can be parsed as well back into the same IP
            IPAddress ip2 = Parse(ip.ToString());
            Assert.Equal(ip, ip2);

            // Validate that anything that doesn't already start with brackets
            // can be surrounded with brackets and still parse successfully.
            if (!address.StartsWith("["))
            {
                Assert.Equal(
                    expected.ToLowerInvariant(),
                    Parse("[" + address + "]").ToString());
            }
        }

        [Theory]
        [MemberData(nameof(ValidIpv6Addresses))]
        public void TryParseIPv6_ValidAddress_RoundtripMatchesExpected(string address, string expected)
        {
            Assert.True(TryParse(address, out IPAddress ip));

            // Validate the ToString of the parsed address matches the expected value
            Assert.Equal(expected.ToLowerInvariant(), ip.ToString());
            Assert.Equal(AddressFamily.InterNetworkV6, ip.AddressFamily);

            // Validate the ToString representation can be parsed as well back into the same IP
            Assert.True(TryParse(ip.ToString(), out IPAddress ip2));
            Assert.Equal(ip, ip2);

            // Validate that anything that doesn't already start with brackets
            // can be surrounded with brackets and still parse successfully.
            if (!address.StartsWith("["))
            {
                Assert.Equal(
                    expected.ToLowerInvariant(),
                    Parse("[" + address + "]").ToString());
            }
        }

        public static readonly object[][] ScopeIds =
        {
            new object[] { "Fe08::1%123", 123},
            new object[] { "Fe08::1%12345678", 12345678},
            new object[] { "fe80::e8b0:63ff:fee8:6b3b%9", 9},
            new object[] { "fe80::e8b0:63ff:fee8:6b3b", 0},
            new object[] { "fe80::e8b0:63ff:fee8:6b3b%abcd0", 0},
            new object[] { "::%unknownInterface", 0},
            new object[] { "::%0", 0},
        };

        [Theory]
        [MemberData(nameof(ScopeIds))]
        public void ParseIPv6_ExtractsScopeId(string address, int expectedScopeId)
        {
            IPAddress ip = Parse(address);
            Assert.Equal(expectedScopeId, ip.ScopeId);
        }

        public static IEnumerable<object[]> InvalidIpv6Addresses()
        {
            yield return new object[] { "[:]" }; // malformed
            yield return new object[] { ":::4df" };
            yield return new object[] { "4df:::" };
            yield return new object[] { "0:::4df" };
            yield return new object[] { "4df:::0" };
            yield return new object[] { "::4df:::" };
            yield return new object[] { "0::4df:::" };
            yield return new object[] { " ::1" };
            yield return new object[] { ":: 1" };
            yield return new object[] { ":" };
            yield return new object[] { "0:0:0:0:0:0:0:0:0" };
            yield return new object[] { "0:0:0:0:0:0:0" };
            yield return new object[] { "0FFFF::" };
            yield return new object[] { "FFFF0::" };
            yield return new object[] { "[::1" }; // missing closing bracket
            yield return new object[] { "Fe08::/64" }; // with subnet
            yield return new object[] { "[Fe08::1]:80Z" }; // brackets and invalid port
            yield return new object[] { "[Fe08::1" }; // leading bracket
            yield return new object[] { "[[Fe08::1" }; // two leading brackets
            yield return new object[] { "Fe08::1]" }; // trailing bracket
            yield return new object[] { "Fe08::1]]" }; // two trailing brackets
            yield return new object[] { "[Fe08::1]]" }; // one leading and two trailing brackets
            yield return new object[] { ":1" }; // leading single colon
            yield return new object[] { ":1:2" }; // leading single colon
            yield return new object[] { ":1:2:3" }; // leading single colon
            yield return new object[] { ":1:2:3:4" }; // leading single colon
            yield return new object[] { ":1:2:3:4:5" }; // leading single colon
            yield return new object[] { ":1:2:3:4:5:6" }; // leading single colon
            yield return new object[] { ":1:2:3:4:5:6:7" }; // leading single colon
            yield return new object[] { ":1:2:3:4:5:6:7:8" }; // leading single colon
            yield return new object[] { ":1:2:3:4:5:6:7:8:9" }; // leading single colon
            yield return new object[] { "::1:2:3:4:5:6:7:8" }; // compressor with too many number groups
            yield return new object[] { "1::2:3:4:5:6:7:8" }; // compressor with too many number groups
            yield return new object[] { "1:2::3:4:5:6:7:8" }; // compressor with too many number groups
            yield return new object[] { "1:2:3::4:5:6:7:8" }; // compressor with too many number groups
            yield return new object[] { "1:2:3:4::5:6:7:8" }; // compressor with too many number groups
            yield return new object[] { "1:2:3:4:5::6:7:8" }; // compressor with too many number groups
            yield return new object[] { "1:2:3:4:5:6::7:8" }; // compressor with too many number groups
            yield return new object[] { "1:2:3:4:5:6:7::8" }; // compressor with too many number groups
            yield return new object[] { "1:2:3:4:5:6:7:8::" }; // compressor with too many number groups
            yield return new object[] { "::1:2:3:4:5:6:7:8:9" }; // compressor with too many number groups
            yield return new object[] { "1:" }; // trailing single colon
            yield return new object[] { " ::1" }; // leading whitespace
            yield return new object[] { "::1 " }; // trailing whitespace
            yield return new object[] { " ::1 " }; // leading and trailing whitespace
            yield return new object[] { "1::1::1" }; // ambiguous failure
            yield return new object[] { "1234::ABCD:1234::ABCD:1234:ABCD" }; // can only use :: once
            yield return new object[] { "1:1\u67081:1:1" }; // invalid char
            yield return new object[] { "FE08::260.168.0.1" }; // out of range
            yield return new object[] { "::192.168.0.0x0" }; // hex failure
            yield return new object[] { "G::" }; // invalid hex
            yield return new object[] { "FFFFF::" }; // invalid value
            yield return new object[] { ":%12" }; // colon scope
            yield return new object[] { "[2001:0db8:85a3:08d3:1319:8a2e:0370:7344]:443/" }; // errneous ending slash after ignored port

            yield return new object[] { "e3fff:ffff:ffff:ffff:ffff:ffff:ffff:abcd" }; // 1st number too long
            yield return new object[] { "3fff:effff:ffff:ffff:ffff:ffff:ffff:abcd" }; // 2nd number too long
            yield return new object[] { "3fff:ffff:effff:ffff:ffff:ffff:ffff:abcd" }; // 3rd number too long
            yield return new object[] { "3fff:ffff:ffff:effff:ffff:ffff:ffff:abcd" }; // 4th number too long
            yield return new object[] { "3fff:ffff:ffff:ffff:effff:ffff:ffff:abcd" }; // 5th number too long
            yield return new object[] { "3fff:ffff:ffff:ffff:ffff:effff:ffff:abcd" }; // 6th number too long
            yield return new object[] { "3fff:ffff:ffff:ffff:ffff:ffff:effff:abcd" }; // 7th number too long
            yield return new object[] { "3fff:ffff:ffff:ffff:ffff:ffff:ffff:eabcd" }; // 8th number too long

            // Various IPv6 addresses including invalid IPv4 addresses
            foreach (object[] invalidIPv4AddressArray in InvalidIpv4Addresses)
            {
                string invalidIPv4Address = (string)invalidIPv4AddressArray[0];
                yield return new object[] { "3fff:ffff:ffff:ffff:ffff:ffff:ffff:" + invalidIPv4Address };
                yield return new object[] { "::" + invalidIPv4Address }; // SIIT
                yield return new object[] { "::FF:" + invalidIPv4Address }; // SIIT
                yield return new object[] { "::5EFE:" + invalidIPv4Address }; // ISATAP
                yield return new object[] { "1::5EFE:" + invalidIPv4Address }; // ISATAP
            }
        }

        [Theory]
        [MemberData(nameof(InvalidIpv6Addresses))]
        public void ParseIPv6_InvalidAddress_ThrowsFormatException(string invalidAddress)
        {
            ParseInvalidAddress(invalidAddress, hasInnerSocketException: true);
        }

        public static readonly object[][] InvalidIpv6AddressesNoInner =
        {
            new object[] { "" }, // empty
            new object[] { " " }, // whitespace
            new object[] { "  " }, // whitespace
            new object[] { "%12" }, // just scope
            new object[] { "[192.168.0.1]" }, // raw v4
            new object[] { "[1]" }, // incomplete
            new object[] { "" }, // malformed
            new object[] { "[" }, // malformed
            new object[] { "[]" }, // malformed
        };

        [Theory]
        [MemberData(nameof(InvalidIpv6AddressesNoInner))]
        public void ParseIPv6_InvalidAddress_ThrowsFormatExceptionWithNoInnerExceptionInNetfx(string invalidAddress)
        {
            ParseInvalidAddress(invalidAddress, hasInnerSocketException: !PlatformDetection.IsNetFramework);
        }

        private void ParseInvalidAddress(string invalidAddress, bool hasInnerSocketException)
        {
            FormatException fe = Assert.Throws<FormatException>(() => Parse(invalidAddress));
            if (hasInnerSocketException)
            {
                SocketException se = Assert.IsType<SocketException>(fe.InnerException);
                Assert.NotEmpty(se.Message);
            }
            else
            {
                Assert.Null(fe.InnerException);
            }

            IPAddress result = IPAddress.Loopback;
            Assert.False(TryParse(invalidAddress, out result));
            Assert.Null(result);
        }
    }
}
