// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Sockets;

using Xunit;

namespace System.PrivateUri.Tests
{
    public class UriIpHostTest
    {
        #region IPv4

        [Fact]
        public void UriIPv4Host_CanonicalDotedDecimal_Success()
        {
            ParseIPv4Address(IPAddress.Loopback.ToString());
        }

        [Theory]
        [InlineData("0.0.0.0", "0.0.0.0")]
        [InlineData("0", "0.0.0.0")]
        [InlineData("0x0", "0.0.0.0")]
        public void UriIPv4Host_Any_Success(string address, string expected)
        {
            ParseIPv4Address(address, expected);
        }

        [Theory]
        // 255.255.255.255
        [InlineData("255.255.255.255", "255.255.255.255")]
        [InlineData("0xFF.0xFF.0xFF.0xFF", "255.255.255.255")]
        [InlineData("0377.0377.0377.0377", "255.255.255.255")]
        // Biggest value 4.0 supported in this format
        [InlineData("4294967294", "255.255.255.254")]
        [InlineData("0xFFFFFFFE", "255.255.255.254")]
        [InlineData("037777777776", "255.255.255.254")] // Octal
        // IPAddress doesn't support these formats, except on XP / 2003?
        [InlineData("4294967295", "255.255.255.255")]
        [InlineData("0xFFFFFFFF", "255.255.255.255")]
        [InlineData("037777777777", "255.255.255.255")] // Octal
        public void UriIPv4Host_None_Success(string address, string expected)
        {
            ParseIPv4Address(address, expected);
        }

        [Fact]
        public void UriIPv4Host_FullDec_Success()
        {
            ParseIPv4Address("2637895963", expected: "157.59.25.27");
        }

        [Theory]
        [InlineData("157.3873051", "157.59.25.27")]
        [InlineData("157.6427", "157.0.25.27")]
        public void UriIPv4Host_PartialDec_Success(string address, string expected)
        {
            ParseIPv4Address(address, expected);
        }

        [Fact]
        public void UriIPv4Host_FullHex_Success()
        {
            ParseIPv4Address("0x9D3B191B", expected: "157.59.25.27");
        }

        [Fact]
        public void UriIPv4Host_DottedHex_Success()
        {
            ParseIPv4Address("0X9D.0x3B.0X19.0x1B", expected: "157.59.25.27");
        }

        [Fact]
        public void UriIPv4Host_DottedHexLowerCase_Success()
        {
            ParseIPv4Address("0x89.0xab.0xcd.0xef", expected: "137.171.205.239");
        }

        [Theory]
        [InlineData("157.59.25.0x1B", "157.59.25.27")]
        [InlineData("157.59.0x001B", "157.59.0.27")]
        [InlineData("157.0x00001B", "157.0.0.27")]
        public void UriIPv4Host_PartialHex_Success(string address, string expected)
        {
            ParseIPv4Address(address, expected);
        }

        [Fact]
        public void UriIPv4Host_FullOctal_Success()
        {
            // 4.0 Uri truncates the leading zeros and reads these as decimal
            ParseIPv4Address("023516614433", expected: "157.59.25.27");
        }

        [Fact]
        public void UriIPv4Host_FullOctalExtraLeadingZeros_Success()
        {
            // 4.0 Uri truncates the leading zeros and reads these as decimal
            ParseIPv4Address("00000023516614433", expected: "157.59.25.27");
        }

        [Fact]
        public void UriIPv4Host_DottedOctal_Success()
        {
            // 4.0 Uri truncates the leading zeros and reads these as decimal
            ParseIPv4Address("0235.073.031.033", expected: "157.59.25.27");
        }

        [Fact]
        public void UriIPv4Host_DottedOctalExtraLeadingZeros_Success()
        {
            // 4.0 Uri truncates the leading zeros and reads these as decimal
            ParseIPv4Address("000235.000073.0000031.00000033", expected: "157.59.25.27");
        }

        [Fact]
        public void UriIPv4Host_PartialOctal_Success()
        {
            // 4.0 Uri truncates the leading zeros and reads these as decimal
            ParseIPv4Address("157.59.25.033", expected: "157.59.25.27");
        }

        [Fact]
        public void UriIPv4Host_PartDecHexOct_Success()
        {
            // 4.0 Uri truncates the leading zeros and reads these as decimal
            ParseIPv4Address("157.59.0x25.033", expected: "157.59.37.27");
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("127.0.0.1 ")]
        [InlineData(" 127.0.0.1")]
        [InlineData("157.3B191B")] // Hex without 0x
        [InlineData("260.156")] // Left dotted segments can't be more than 255
        [InlineData("255.260.156")] // Left dotted segments can't be more than 255
        [InlineData("1.1.1.0x")] // Empty trailing hex segment
        [InlineData("0x.1.1.1")] // Empty leading hex segment
        [InlineData("...")] // Empty sections
        [InlineData("1.1.1.")] // Empty trailing section
        [InlineData("1..1.1")] // Empty internal section
        [InlineData(".1.1.1")] // Empty leading section
        [InlineData("..11.1")] // Empty sections
        [InlineData("0xFF.0xFFFFFF.0xFF")] // Middle segment too large
        [InlineData("0xFFFFFF.0xFF.0xFFFFFF")] // Leading segment too large
        [InlineData("1.1\u67081.1.1")] // Unicode, Crashes .NET 4.0 IPAddress.TryParse
        [InlineData("0000X9D.0x3B.0X19.0x1B")] // Leading zeros on hex
        [InlineData("01011101001110110001100100011011")] // Binary? Read as octal, overflows
        [InlineData("10011101001110110001100100011011")] // Binary? Read as decimal, overflows
        [InlineData("040000000000")] // Octal overflow by 1
        [InlineData("4294967296")] // Decimal overflow by 1
        [InlineData("0x100000000")] // Hex overflow by 1
        [InlineData("0.0.0.089")] // Octal (leading zero) but with 8 or 9
        public void UriIPv4Host_BadAddresses_AllFail(string address)
        {
            ParseBadIPv4Address(address);
        }

        [Fact]
        public void UriIPv4Host_UriWithPort_Success()
        {
            Uri testUri;
            Assert.True(Uri.TryCreate("http://" + IPAddress.Loopback.ToString() + ":9090", UriKind.Absolute, out testUri));
            Assert.Equal(UriHostNameType.IPv4, testUri.HostNameType);
            Assert.Equal(IPAddress.Loopback.ToString(), testUri.Host);
            Assert.Equal(IPAddress.Loopback.ToString(), testUri.DnsSafeHost);
        }

        [Fact]
        public void UriIPv4Host_UriWithQuery_Success()
        {
            Uri testUri;
            Assert.True(Uri.TryCreate("http://" + IPAddress.Loopback.ToString() + "?Query", UriKind.Absolute, out testUri));
            Assert.Equal(UriHostNameType.IPv4, testUri.HostNameType);
            Assert.Equal(IPAddress.Loopback.ToString(), testUri.Host);
            Assert.Equal(IPAddress.Loopback.ToString(), testUri.DnsSafeHost);
        }

        [Fact]
        public void UriIPv4Host_UriWithFragment_Success()
        {
            Uri testUri;
            Assert.True(Uri.TryCreate("http://" + IPAddress.Loopback.ToString() + "#fragment", UriKind.Absolute, out testUri));
            Assert.Equal(UriHostNameType.IPv4, testUri.HostNameType);
            Assert.Equal(IPAddress.Loopback.ToString(), testUri.Host);
            Assert.Equal(IPAddress.Loopback.ToString(), testUri.DnsSafeHost);
        }

        #region Helpers

        private void ParseIPv4Address(string ipv4String)
        {
            ParseIPv4Address(ipv4String, expected: ipv4String);
        }

        private void ParseIPv4Address(string ipv4String, string expected)
        {
            // TryCreate
            Uri testUri;
            Assert.True(Uri.TryCreate("http://" + ipv4String, UriKind.Absolute, out testUri), ipv4String);
            Assert.Equal(UriHostNameType.IPv4, testUri.HostNameType);
            Assert.Equal(expected, testUri.Host);
            Assert.Equal(expected, testUri.DnsSafeHost);

            // Constructor
            testUri = new Uri("http://" + ipv4String);
            Assert.Equal(UriHostNameType.IPv4, testUri.HostNameType);
            Assert.Equal(expected, testUri.Host);
            Assert.Equal(expected, testUri.DnsSafeHost);

            // CheckHostName
            Assert.Equal(UriHostNameType.IPv4, Uri.CheckHostName(ipv4String));
        }

        private void ParseBadIPv4Address(string badIpv4String)
        {
            // CheckHostName
            Assert.NotEqual(UriHostNameType.IPv4, Uri.CheckHostName(badIpv4String));

            // TryCreate
            Uri testUri;
            if (Uri.TryCreate("http://" + badIpv4String + "/", UriKind.Absolute, out testUri))
            {
                Assert.NotEqual(UriHostNameType.IPv4, testUri.HostNameType);
            }
        }

        #endregion Helpers

        #endregion IPv4

        #region IPv6

        [Fact]
        public void UriIPv6Host_CanonicalCollonHex_Success()
        {
            ParseIPv6Address(IPAddress.IPv6Loopback.ToString());
        }

        [Theory]
        [InlineData("::")]
        [InlineData("0000:0000:0000:0000:0000:0000:0000:0000")]
        public void UriIPv6Host_Any_Success(string address)
        {
            ParseIPv6Address(address, expected: "::");
        }

        [Fact]
        public void UriIPv6Host_MaxValue_Success()
        {
            ParseIPv6Address("FFFF:FFFF:FFFF:FFFF:FFFF:FFFF:FFFF:FFFF", expected: "ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff");
        }

        [Fact]
        public void UriIPv6Host_LeadingValue_Success()
        {
            ParseIPv6Address("1::");
        }

        [Theory]
        [InlineData("0:0:0:0:0:0:0:0", "::")]
        [InlineData("1:0:0:0:0:0:0:0", "1::")]
        [InlineData("0:1:0:0:0:0:0:0", "0:1::")]
        [InlineData("0:0:1:0:0:0:0:0", "0:0:1::")]
        [InlineData("0:0:0:1:0:0:0:0", "0:0:0:1::")]
        [InlineData("0:0:0:0:1:0:0:0", "::1:0:0:0")]
        [InlineData("0:0:0:0:0:1:0:0", "::1:0:0")]
        [InlineData("0:0:0:0:0:0:1:0", "::0.1.0.0")]
        [InlineData("0:0:0:0:0:0:0:1", "::1")]
        [InlineData("1:0:0:0:0:0:0:1", "1::1")]
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
        public void UriIPv6Host_CompressionRangeSelection_Success(string address, string expected)
        {
            ParseIPv6Address(address, expected);
        }

        [Theory]
        [InlineData("1::%1")]
        [InlineData("::1%12")]
        [InlineData("::%123")]
        public void UriIPv6Host_ScopeId_Success(string address)
        {
            ParseIPv6Address(address);
        }

        [Theory]
        [InlineData("FE08::192.168.0.1", "fe08::c0a8:1")] // Output is not IPv4 mapped
        [InlineData("::192.168.0.1", "::192.168.0.1")]
        [InlineData("::FFFF:192.168.0.1", "::ffff:192.168.0.1")] // SIIT
        [InlineData("::FFFF:0:192.168.0.1", "::ffff:0:192.168.0.1")] // SIIT
        [InlineData("::5EFE:192.168.0.1", "::5efe:192.168.0.1")] // ISATAP
        [InlineData("1::5EFE:192.168.0.1", "1::5efe:192.168.0.1")] // ISATAP
        [InlineData("::192.168.0.010", "::192.168.0.10")] // Embedded IPv4 octal, read as decimal
        public void UriIPv6Host_EmbeddedIPv4_Success(string address, string expected)
        {
            ParseIPv6Address(address, expected);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("1")]
        [InlineData(":1")]
        [InlineData("1:")]
        [InlineData("::1 ")]
        [InlineData(" ::1")]
        [InlineData("1::1::1")] // Ambigious
        [InlineData("1:1\u67081:1:1")] // Unicoded. Crashes .NET 4.0 IPAddress.TryParse
        [InlineData("FE08::260.168.0.1")] // Embedded IPv4 out of range
        [InlineData("::192.168.0.0x0")] // Embedded IPv4 hex
        [InlineData("192.168.0.1")] // Raw IPv4
        [InlineData("G::")] // Hex out of range
        [InlineData("FFFFF::")] // Hex out of range
        [InlineData(":%12")] // Colon Scope
        [InlineData("%12")] // Just Scope
        // TODO # 8330 Discrepency: IPAddress doesn't accept bad scopes, Uri does.
        //[InlineData("::%1a")] // Alpha numeric Scope
        public void UriIPv6Host_BadAddress(string address)
        {
            ParseBadIPv6Address(address);
        }

        #region Helpers

        private void ParseIPv6Address(string ipv6String)
        {
            ParseIPv6Address(ipv6String, expected: ipv6String);
        }

        private void ParseIPv6Address(string ipv6String, string expected)
        {
            // Host property returns bracketed address without the scope ID
            int scopeIndex = expected.IndexOf('%');
            string expectedResultWithBrackets = $"[{((scopeIndex == -1) ? expected : expected.Substring(0, scopeIndex))}]";

            // TryCreate
            Uri testUri;
            Assert.True(Uri.TryCreate("http://[" + ipv6String + "]", UriKind.Absolute, out testUri), ipv6String);
            Assert.Equal(UriHostNameType.IPv6, testUri.HostNameType);
            Assert.Equal(expectedResultWithBrackets, testUri.Host);
            Assert.Equal(expected, testUri.DnsSafeHost);

            // Constructor
            testUri = new Uri("http://[" + ipv6String + "]");
            Assert.Equal(UriHostNameType.IPv6, testUri.HostNameType);
            Assert.Equal(expectedResultWithBrackets, testUri.Host);
            Assert.Equal(expected, testUri.DnsSafeHost);

            // CheckHostName
            Assert.Equal(UriHostNameType.IPv6, Uri.CheckHostName(ipv6String));
        }

        private void ParseBadIPv6Address(string badIpv6String)
        {
            // CheckHostName
            Assert.NotEqual(UriHostNameType.IPv6, Uri.CheckHostName(badIpv6String));

            // TryCreate
            Uri testUri;
            Assert.False(Uri.TryCreate("http://[" + badIpv6String + "]/", UriKind.Absolute, out testUri),
                badIpv6String);
        }

        #endregion Helpers

        #endregion IPv6
    }
}
