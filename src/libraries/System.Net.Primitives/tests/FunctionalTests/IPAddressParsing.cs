// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Sockets;
using Xunit;

namespace System.Net.Primitives.Functional.Tests
{
    public class IPAddressParsing
    {
        [Theory]
        // Decimal
        [InlineData("192.168.0.1", "192.168.0.1")]
        [InlineData("0.0.0.0", "0.0.0.0")]
        [InlineData("0", "0.0.0.0")]
        [InlineData("12", "0.0.0.12")]
        [InlineData("12.1.7", "12.1.0.7")]
        [InlineData("12.1.7", "12.1.0.7")]
        [InlineData("255.255.255.255", "255.255.255.255")]
        [InlineData("20.65535", "20.0.255.255")]
        [InlineData("157.3873051", "157.59.25.27")]
        [InlineData("157.6427", "157.0.25.27")]
        [InlineData("65535", "0.0.255.255")]
        [InlineData("65536", "0.1.0.0")]
        [InlineData("1434328179", "85.126.28.115")]
        [InlineData("2637895963", "157.59.25.27")]
        [InlineData("3397943208", "202.136.127.168")]
        [InlineData("4294967294", "255.255.255.254")]
        [InlineData("4294967295", "255.255.255.255")]
        //Hex
        [InlineData("0xFF.0xFF.0xFF.0xFF", "255.255.255.255")]
        [InlineData("0x0", "0.0.0.0")]
        [InlineData("0xFFFFFFFE", "255.255.255.254")]
        [InlineData("0xFFFFFFFF", "255.255.255.255")]
        [InlineData("0x9D3B191B", "157.59.25.27")]
        [InlineData("0X9D.0x3B.0X19.0x1B", "157.59.25.27")]
        [InlineData("0x89.0xab.0xcd.0xef", "137.171.205.239")]
		[InlineData("0xff.0x7f.0x20.0x01", "255.127.32.1")]
        // Octal
        [InlineData("0313.027035210", "203.92.58.136")]
        [InlineData("0313.0134.035210", "203.92.58.136")]
        [InlineData("0377.0377.0377.0377", "255.255.255.255")]
        [InlineData("037777777776", "255.255.255.254")]
        [InlineData("037777777777", "255.255.255.255")]
        [InlineData("023516614433", "157.59.25.27")]
        [InlineData("00000023516614433", "157.59.25.27")]
        [InlineData("000235.000073.0000031.00000033", "157.59.25.27")]
        [InlineData("0235.073.031.033", "157.59.25.27")]
        [InlineData("157.59.25.033", "157.59.25.27")] // Partial octal
        // Mixed base
        [InlineData("157.59.25.0x1B", "157.59.25.27")]
        [InlineData("157.59.0x001B", "157.59.0.27")]
        [InlineData("157.0x00001B", "157.0.0.27")]
        [InlineData("157.59.0x25.033", "157.59.37.27")]
        public void ParseIPv4_ValidAddress_Success(string address, string expected)
        {
            IPAddress ip = IPAddress.Parse(address);

            // Validate the ToString of the parsed address matches the expected value
            Assert.Equal(expected, ip.ToString());
            Assert.Equal(AddressFamily.InterNetwork, ip.AddressFamily);

            // Validate the ToString representation can be parsed as well back into the same IP
            IPAddress ip2 = IPAddress.Parse(ip.ToString());
            Assert.Equal(ip, ip2);
        }

        [Theory]
        [InlineData("")] // empty
        [InlineData(" ")] // whitespace
        [InlineData("  ")] // whitespace
        [InlineData(" 127.0.0.1")] // leading whitespace
        [InlineData("127.0.0.1 ")] // trailing whitespace
        [InlineData(" 127.0.0.1 ")] // leading and trailing whitespace
        [InlineData("192.168.0.0/16")] // with subnet
        [InlineData("157.3B191B")] // Hex without 0x
        [InlineData("1.1.1.0x")] // Empty trailing hex segment
        [InlineData("0000X9D.0x3B.0X19.0x1B")] // Leading zeros on hex
        [InlineData("0x.1.1.1")] // Empty leading hex segment
        [InlineData("0.0.0.089")] // Octal (leading zero) but with 8 or 9
        [InlineData("260.156")] // Left dotted segments can't be more than 255
        [InlineData("255.260.156")] // Left dotted segments can't be more than 255
        [InlineData("255.1.1.256")] // Right dotted segment can't be more than 255
        [InlineData("0xFF.0xFFFFFF.0xFF")] // Middle segment too large
        [InlineData("0xFFFFFF.0xFF.0xFFFFFF")] // Leading segment too large
        [InlineData("4294967296")] // Decimal overflow by 1
        [InlineData("040000000000")] // Octal overflow by 1
        [InlineData("01011101001110110001100100011011")] // Binary? Read as octal, overflows
        [InlineData("10011101001110110001100100011011")] // Binary? Read as decimal, overflows
        [InlineData("0x100000000")] // Hex overflow by 1
        [InlineData("1.1\u67081.1.1")] // Invalid char (unicode)
        [InlineData("...")] // Empty sections
        [InlineData("1.1.1.")] // Empty trailing section
        [InlineData("1..1.1")] // Empty internal section
        [InlineData(".1.1.1")] // Empty leading section
        [InlineData("..11.1")] // Empty sections
        [InlineData(" text")] // alpha text
		[InlineData("1.. .")] // whitespace section
        [InlineData("12.1.8. ")] // trailing whitespace section
        [InlineData("12.+1.1.4")] // plus sign in section
        [InlineData("12.1.-1.5")] // minus sign in section
        [InlineData("12.1.abc.5")] // text in section
        public void ParseIPv4_InvalidAddress_Failure(string address)
        {
            ParseInvalidAddress(address, hasInnerSocketException: !PlatformDetection.IsFullFramework);
        }

        [Theory]
        [InlineData("192.168.0.0:80")] // with port
        [InlineData("192.168.0.1:80")] // with port
        public void ParseIPv4_InvalidAddress_ThrowsFormatExceptionWithInnerException(string address)
        {
            ParseInvalidAddress(address, hasInnerSocketException: true);
        }

        [Theory]
        [InlineData("Fe08::1", "fe08::1")]
        [InlineData("0000:0000:0000:0000:0000:0000:0000:0000", "::")]
        [InlineData("FFFF:FFFF:FFFF:FFFF:FFFF:FFFF:FFFF:FFFF", "ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff")]
        [InlineData("0:0:0:0:0:0:0:0", "::")]
        [InlineData("1:0:0:0:0:0:0:0", "1::")]
        [InlineData("0:1:0:0:0:0:0:0", "0:1::")]
        [InlineData("0:0:1:0:0:0:0:0", "0:0:1::")]
        [InlineData("0:0:0:1:0:0:0:0", "0:0:0:1::")]
        [InlineData("0:0:0:0:1:0:0:0", "::1:0:0:0")]
        [InlineData("0:0:0:0:0:1:0:0", "::1:0:0")]
        [InlineData("0:0:0:0:0:0:1:0", "::0.1.0.0")]
        [InlineData("0:0:0:0:0:0:2:0", "::0.2.0.0")]
        [InlineData("0:0:0:0:0:0:F:0", "::0.15.0.0")]
        [InlineData("0:0:0:0:0:0:10:0", "::0.16.0.0")]
        [InlineData("0:0:0:0:0:0:A0:0", "::0.160.0.0")]
        [InlineData("0:0:0:0:0:0:F0:0", "::0.240.0.0")]
        [InlineData("0:0:0:0:0:0:FF:0", "::0.255.0.0")]
        [InlineData("0:0:0:0:0:0:0:1", "::1")]
        [InlineData("0:0:0:0:0:0:0:2", "::2")]
        [InlineData("0:0:0:0:0:0:0:F", "::F")]
        [InlineData("0:0:0:0:0:0:0:10", "::10")]
        [InlineData("0:0:0:0:0:0:0:1A", "::1A")]
        [InlineData("0:0:0:0:0:0:0:A0", "::A0")]
        [InlineData("0:0:0:0:0:0:0:F0", "::F0")]
        [InlineData("0:0:0:0:0:0:0:FF", "::FF")]
        [InlineData("0:0:0:0:0:0:0:1001", "::1001")]
        [InlineData("0:0:0:0:0:0:0:1002", "::1002")]
        [InlineData("0:0:0:0:0:0:0:100F", "::100F")]
        [InlineData("0:0:0:0:0:0:0:1010", "::1010")]
        [InlineData("0:0:0:0:0:0:0:10A0", "::10A0")]
        [InlineData("0:0:0:0:0:0:0:10F0", "::10F0")]
        [InlineData("0:0:0:0:0:0:0:10FF", "::10FF")]
        [InlineData("0:0:0:0:0:0:1:1", "::0.1.0.1")]
        [InlineData("0:0:0:0:0:0:2:2", "::0.2.0.2")]
        [InlineData("0:0:0:0:0:0:F:F", "::0.15.0.15")]
        [InlineData("0:0:0:0:0:0:10:10", "::0.16.0.16")]
        [InlineData("0:0:0:0:0:0:A0:A0", "::0.160.0.160")]
        [InlineData("0:0:0:0:0:0:F0:F0", "::0.240.0.240")]
        [InlineData("0:0:0:0:0:0:FF:FF", "::0.255.0.255")]
        [InlineData("0:0:0:0:0:FFFF:0:1", "::FFFF:0:1")]
        [InlineData("0:0:0:0:0:FFFF:0:2", "::FFFF:0:2")]
        [InlineData("0:0:0:0:0:FFFF:0:F", "::FFFF:0:F")]
        [InlineData("0:0:0:0:0:FFFF:0:10", "::FFFF:0:10")]
        [InlineData("0:0:0:0:0:FFFF:0:A0", "::FFFF:0:A0")]
        [InlineData("0:0:0:0:0:FFFF:0:F0", "::FFFF:0:F0")]
        [InlineData("0:0:0:0:0:FFFF:0:FF", "::FFFF:0:FF")]
        [InlineData("0:0:0:0:0:FFFF:1:0", "::FFFF:0.1.0.0")]
        [InlineData("0:0:0:0:0:FFFF:2:0", "::FFFF:0.2.0.0")]
        [InlineData("0:0:0:0:0:FFFF:F:0", "::FFFF:0.15.0.0")]
        [InlineData("0:0:0:0:0:FFFF:10:0", "::FFFF:0.16.0.0")]
        [InlineData("0:0:0:0:0:FFFF:A0:0", "::FFFF:0.160.0.0")]
        [InlineData("0:0:0:0:0:FFFF:F0:0", "::FFFF:0.240.0.0")]
        [InlineData("0:0:0:0:0:FFFF:FF:0", "::FFFF:0.255.0.0")]
        [InlineData("0:0:0:0:0:FFFF:0:1001", "::FFFF:0:1001")]
        [InlineData("0:0:0:0:0:FFFF:0:1002", "::FFFF:0:1002")]
        [InlineData("0:0:0:0:0:FFFF:0:100F", "::FFFF:0:100F")]
        [InlineData("0:0:0:0:0:FFFF:0:1010", "::FFFF:0:1010")]
        [InlineData("0:0:0:0:0:FFFF:0:10A0", "::FFFF:0:10A0")]
        [InlineData("0:0:0:0:0:FFFF:0:10F0", "::FFFF:0:10F0")]
        [InlineData("0:0:0:0:0:FFFF:0:10FF", "::FFFF:0:10FF")]
        [InlineData("0:0:0:0:0:FFFF:1:1", "::FFFF:0.1.0.1")]
        [InlineData("0:0:0:0:0:FFFF:2:2", "::FFFF:0.2.0.2")]
        [InlineData("0:0:0:0:0:FFFF:F:F", "::FFFF:0.15.0.15")]
        [InlineData("0:0:0:0:0:FFFF:10:10", "::FFFF:0.16.0.16")]
        [InlineData("0:0:0:0:0:FFFF:A0:A0", "::FFFF:0.160.0.160")]
        [InlineData("0:0:0:0:0:FFFF:F0:F0", "::FFFF:0.240.0.240")]
        [InlineData("0:0:0:0:0:FFFF:FF:FF", "::FFFF:0.255.0.255")]
        [InlineData("0:7:7:7:7:7:7:7", "0:7:7:7:7:7:7:7")]
        [InlineData("1:0:0:0:0:0:0:1", "1::1")]
        [InlineData("1:1:0:0:0:0:0:0", "1:1::")]
        [InlineData("2:2:0:0:0:0:0:0", "2:2::")]
        [InlineData("1:1:0:0:0:0:0:1", "1:1::1")]
        [InlineData("1:0:1:0:0:0:0:1", "1:0:1::1")]
        [InlineData("1:0:0:1:0:0:0:1", "1:0:0:1::1")]
        [InlineData("1:0:0:0:1:0:0:1", "1::1:0:0:1")]
        [InlineData("1:0:0:0:0:1:0:1", "1::1:0:1")]
        [InlineData("1:0:0:0:0:0:1:1", "1::1:1")]
        [InlineData("1:1:0:0:1:0:0:1", "1:1::1:0:0:1")]
        [InlineData("1:0:1:0:0:1:0:1", "1:0:1::1:0:1")]
        [InlineData("1:0:0:1:0:0:1:1", "1::1:0:0:1:1")]
        [InlineData("1:1:0:0:0:1:0:1", "1:1::1:0:1")]
        [InlineData("1:0:0:0:1:0:1:1", "1::1:0:1:1")]
        [InlineData("1:1:1:1:1:1:1:0", "1:1:1:1:1:1:1:0")]
        [InlineData("7:7:7:7:7:7:7:0", "7:7:7:7:7:7:7:0")]
        [InlineData("E:0:0:0:0:0:0:1", "E::1")]
        [InlineData("E:0:0:0:0:0:2:2", "E::2:2")]
        [InlineData("E:0:6:6:6:6:6:6", "E:0:6:6:6:6:6:6")]
        [InlineData("E:E:0:0:0:0:0:1", "E:E::1")]
        [InlineData("E:E:0:0:0:0:2:2", "E:E::2:2")]
        [InlineData("E:E:0:5:5:5:5:5", "E:E:0:5:5:5:5:5")]
        [InlineData("E:E:E:0:0:0:0:1", "E:E:E::1")]
        [InlineData("E:E:E:0:0:0:2:2", "E:E:E::2:2")]
        [InlineData("E:E:E:0:4:4:4:4", "E:E:E:0:4:4:4:4")]
        [InlineData("E:E:E:E:0:0:0:1", "E:E:E:E::1")]
        [InlineData("E:E:E:E:0:0:2:2", "E:E:E:E::2:2")]
        [InlineData("E:E:E:E:0:3:3:3", "E:E:E:E:0:3:3:3")]
        [InlineData("E:E:E:E:E:0:0:1", "E:E:E:E:E::1")]
        [InlineData("E:E:E:E:E:0:2:2", "E:E:E:E:E:0:2:2")]
        [InlineData("E:E:E:E:E:E:0:1", "E:E:E:E:E:E:0:1")]
        [InlineData("::FFFF:192.168.0.1", "::FFFF:192.168.0.1")]
        [InlineData("::FFFF:0.168.0.1", "::FFFF:0.168.0.1")]
        [InlineData("::0.0.255.255", "::FFFF")]
        [InlineData("::EEEE:10.0.0.1", "::EEEE:A00:1")]
        [InlineData("::10.0.0.1", "::10.0.0.1")]
        [InlineData("1234:0:0:0:0:1234:0:0", "1234::1234:0:0")]
        [InlineData("1:0:1:0:1:0:1:0", "1:0:1:0:1:0:1:0")]
        [InlineData("1:1:1:0:0:1:1:0", "1:1:1::1:1:0")]
        [InlineData("0:0:0:0:0:1234:0:0", "::1234:0:0")]
        [InlineData("3ffe:38e1::0100:1:0001", "3ffe:38e1::100:1:1")]
        [InlineData("0:0:1:2:00:00:000:0000", "0:0:1:2::")]
        [InlineData("100:0:1:2:0:0:000:abcd", "100:0:1:2::abcd")]
        [InlineData("ffff:0:0:0:0:0:00:abcd", "ffff::abcd")]
        [InlineData("ffff:0:0:2:0:0:00:abcd", "ffff:0:0:2::abcd")]
        [InlineData("0:0:1:2:0:00:0000:0000", "0:0:1:2::")]
        [InlineData("0000:0000::1:0000:0000", "::1:0:0")]
        [InlineData("0:0:111:234:5:6:789A:0", "::111:234:5:6:789a:0")]
        [InlineData("11:22:33:44:55:66:77:8", "11:22:33:44:55:66:77:8")]
        [InlineData("::7711:ab42:1230:0:0:0", "0:0:7711:ab42:1230::")]
        [InlineData("::", "::")]
        [InlineData("[Fe08::1]", "fe08::1")] // brackets dropped
        [InlineData("[Fe08::1]:0x80", "fe08::1")] // brackets and port dropped
        [InlineData("[Fe08::1]:0xFA", "fe08::1")] // brackets and port dropped
        [InlineData("2001:0db8::0001", "2001:db8::1")] // leading 0s suppressed
        [InlineData("3731:54:65fe:2::a7", "3731:54:65fe:2::a7")] // Unicast
        [InlineData("3731:54:65fe:2::a8", "3731:54:65fe:2::a8")] // Anycast
        // ScopeID
        [InlineData("Fe08::1%13542", "fe08::1%13542")]
        [InlineData("1::%1", "1::%1")]
        [InlineData("::1%12", "::1%12")]
        [InlineData("::%123", "::%123")]
        // v4 as v6
        [InlineData("FE08::192.168.0.1", "fe08::c0a8:1")] // Output is not IPv4 mapped
        [InlineData("::192.168.0.1", "::192.168.0.1")]
        [InlineData("::FFFF:192.168.0.1", "::ffff:192.168.0.1")] // SIIT
        [InlineData("::FFFF:0:192.168.0.1", "::ffff:0:192.168.0.1")] // SIIT
        [InlineData("::5EFE:192.168.0.1", "::5efe:192.168.0.1")] // ISATAP
        [InlineData("1::5EFE:192.168.0.1", "1::5efe:192.168.0.1")] // ISATAP
        [InlineData("::192.168.0.010", "::192.168.0.10")] // Embedded IPv4 octal, read as decimal
        public void ParseIPv6_ValidAddress_RoundtripMatchesExpected(string address, string expected)
        {
            IPAddress ip = IPAddress.Parse(address);

            // Validate the ToString of the parsed address matches the expected value
            Assert.Equal(expected.ToLowerInvariant(), ip.ToString());
            Assert.Equal(AddressFamily.InterNetworkV6, ip.AddressFamily);

            // Validate the ToString representation can be parsed as well back into the same IP
            IPAddress ip2 = IPAddress.Parse(ip.ToString());
            Assert.Equal(ip, ip2);

            // Validate that anything that doesn't already start with brackets
            // can be surrounded with brackets and still parse successfully.
            if (!address.StartsWith("["))
            {
                Assert.Equal(
                    expected.ToLowerInvariant(),
                    IPAddress.Parse("[" + address + "]").ToString());
            }
        }

        [Theory]
		[InlineData(":::4df")]
		[InlineData("4df:::")]
        [InlineData("0:::4df")]
        [InlineData("4df:::0")]
        [InlineData("::4df:::")]
        [InlineData("0::4df:::")]
        [InlineData(" ::1")]
        [InlineData(":: 1")]
        [InlineData(":")]
        [InlineData("0:0:0:0:0:0:0:0:0")]
        [InlineData("0:0:0:0:0:0:0")]
        [InlineData("0FFFF::")]
        [InlineData("FFFF0::")]
        [InlineData("[::1")]
        [InlineData("Fe08::/64")] // with subnet
        [InlineData("[Fe08::1]:80Z")] // brackets and invalid port
        [InlineData("[Fe08::1")] // leading bracket
        [InlineData("[[Fe08::1")] // two leading brackets
        [InlineData("Fe08::1]")] // trailing bracket
        [InlineData("Fe08::1]]")] // two trailing brackets
        [InlineData("[Fe08::1]]")] // one leading and two trailing brackets
        [InlineData(":1")] // leading single colon
        [InlineData("1:")] // trailing single colon
        [InlineData(" ::1")] // leading whitespace
        [InlineData("::1 ")] // trailing whitespace
        [InlineData(" ::1 ")] // leading and trailing whitespace
        [InlineData("1::1::1")] // ambiguous failure
        [InlineData("1234::ABCD:1234::ABCD:1234:ABCD")] // can only use :: once
        [InlineData("1:1\u67081:1:1")] // invalid char
        [InlineData("FE08::260.168.0.1")] // out of range
        [InlineData("::192.168.0.0x0")] // hex failure
        [InlineData("G::")] // invalid hex
        [InlineData("FFFFF::")] // invalid value
        [InlineData(":%12")] // colon scope
        [InlineData("::%1a")] // alphanumeric scope
        [InlineData("[2001:0db8:85a3:08d3:1319:8a2e:0370:7344]:443/")] // errneous ending slash after ignored port
        [InlineData("::1234%0x12")] // invalid scope ID
        public void ParseIPv6_InvalidAddress_ThrowsFormatException(string invalidAddress)
        {
            ParseInvalidAddress(invalidAddress, hasInnerSocketException: true);
        }

        [Theory]
        [InlineData("")] // empty
        [InlineData(" ")] // whitespace
        [InlineData("  ")] // whitespace
        [InlineData("%12")] // just scope
        [InlineData("[192.168.0.1]")] // raw v4
        [InlineData("[1]")] // incomplete
        public void ParseIPv6_InvalidAddress_ThrowsFormatExceptionWithNoInnerExceptionInNetfx(string invalidAddress)
        {
            ParseInvalidAddress(invalidAddress, hasInnerSocketException: !PlatformDetection.IsFullFramework);
        }

        private static void ParseInvalidAddress(string invalidAddress, bool hasInnerSocketException)
        {
            FormatException fe = Assert.Throws<FormatException>(() => IPAddress.Parse(invalidAddress));
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
            Assert.False(IPAddress.TryParse(invalidAddress, out result));
            Assert.Null(result);
        }

        [Fact]
        public void Parse_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => { IPAddress.Parse(null); });

            IPAddress ipAddress;
            Assert.False(IPAddress.TryParse(null, out ipAddress));
            Assert.Null(ipAddress);
        }
    }
}
