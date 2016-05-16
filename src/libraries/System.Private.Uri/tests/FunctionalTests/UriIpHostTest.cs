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

        [Fact]
        public void UriIPv4Host_Any_Success()
        {
            // IPAddress.Any
            ParseIPv4Address("0.0.0.0");
            ParseIPv4Address("0");
            ParseIPv4Address("0x0");
        }

        [Fact]
        public void UriIPv4Host_None_Success()
        {
            // 255.255.255.255
            ParseIPv4Address("255.255.255.255");
            ParseIPv4Address("0xFF.0xFF.0xFF.0xFF");
            ParseIPv4Address("0377.0377.0377.0377");

            // Biggest value 4.0 supported in this format
            ParseIPv4Address((UInt32.MaxValue - 1).ToString());
            ParseIPv4Address("0x" + (UInt32.MaxValue - 1).ToString("X"));
            ParseIPv4Address("037777777776"); // Octal

            // IPAddress doesn't support these formats, except on XP / 2003?
            ParseIPv4Address(UInt32.MaxValue.ToString());
            ParseIPv4Address("0x" + UInt32.MaxValue.ToString("X"));
            ParseIPv4Address("037777777777"); // Octal
        }

        [Fact]
        public void UriIPv4Host_FullDec_Success()
        {
            ParseIPv4Address("2637895963");
        }

        [Fact]
        public void UriIPv4Host_PartialDec_Success()
        {
            ParseIPv4Address("157.3873051");
            ParseIPv4Address("157.6427");
        }

        [Fact]
        public void UriIPv4Host_FullHex_Success()
        {
            ParseIPv4Address("0x9D3B191B");
        }

        [Fact]
        public void UriIPv4Host_DottedHex_Success()
        {
            ParseIPv4Address("0X9D.0x3B.0X19.0x1B");
        }

        [Fact]
        public void UriIPv4Host_DottedHexLowerCase_Success()
        {
            ParseIPv4Address("0x89.0xab.0xcd.0xef");
        }

        [Fact]
        public void UriIPv4Host_PartialHex_Success()
        {
            ParseIPv4Address("157.59.25.0x1B");
            ParseIPv4Address("157.59.0x001B");
            ParseIPv4Address("157.0x00001B");
        }

        [Fact]
        public void UriIPv4Host_FullOctal_Success()
        {
            // 4.0 Uri truncates the leading zeros and reads these as decimal
            ParseIPv4Address("023516614433");
        }

        [Fact]
        public void UriIPv4Host_FullOctalExtraLeadingZeros_Success()
        {
            // 4.0 Uri truncates the leading zeros and reads these as decimal
            ParseIPv4Address("00000023516614433");
        }

        [Fact]
        [ActiveIssue(8362, PlatformID.OSX)]
        public void UriIPv4Host_DottedOctal_Success()
        {
            // 4.0 Uri truncates the leading zeros and reads these as decimal
            ParseIPv4Address("0235.073.031.033");
        }

        [Fact]
        [ActiveIssue(8362, PlatformID.OSX)]
        public void UriIPv4Host_DottedOctalExtraLeadingZeros_Success()
        {
            // 4.0 Uri truncates the leading zeros and reads these as decimal
            ParseIPv4Address("000235.000073.0000031.00000033");
        }

        [Fact]
        [ActiveIssue(8362, PlatformID.OSX)]
        public void UriIPv4Host_PartialOctal_Success()
        {
            // 4.0 Uri truncates the leading zeros and reads these as decimal
            ParseIPv4Address("157.59.25.033");
        }

        [Fact]
        public void UriIPv4Host_PartDecHexOct_Success()
        {
            // 4.0 Uri truncates the leading zeros and reads these as decimal
            ParseIPv4Address("157.59.0x25.033");
        }

        [Fact]
        [ActiveIssue(8361, PlatformID.AnyUnix)]
        public void UriIPv4Host_BadAddresses_AllFail()
        {
            ParseBadIPv4Address("");
            ParseBadIPv4Address(" ");
            ParseBadIPv4Address(IPAddress.Loopback.ToString() + " ");
            ParseBadIPv4Address(" " + IPAddress.Loopback.ToString());
            ParseBadIPv4Address("157.3B191B"); // Hex without 0x
            ParseBadIPv4Address("260.156"); // Left dotted segments can't be more than 255
            ParseBadIPv4Address("255.260.156"); // Left dotted segments can't be more than 255
            ParseBadIPv4Address("1.1.1.0x"); // Empty trailing hex segment
            ParseBadIPv4Address("0x.1.1.1"); // Empty leading hex segment
            ParseBadIPv4Address("..."); // Empty sections
            ParseBadIPv4Address("1.1.1."); // Empty trailing section
            ParseBadIPv4Address("1..1.1"); // Empty internal section
            ParseBadIPv4Address(".1.1.1"); // Empty leading section
            ParseBadIPv4Address("..11.1"); // Empty sections
            ParseBadIPv4Address("0xFF.0xFFFFFF.0xFF"); // Middle segment too large
            ParseBadIPv4Address("0xFFFFFF.0xFF.0xFFFFFF"); // Leading segment too large
            ParseBadIPv4Address("1.1\u67081.1.1"); // Unicode, Crashes .NET 4.0 IPAddress.TryParse
            ParseBadIPv4Address("0000X9D.0x3B.0X19.0x1B"); // Leading zeros on hex
            ParseBadIPv4Address("01011101001110110001100100011011"); // Binary? Read as octal, overflows
            ParseBadIPv4Address("10011101001110110001100100011011"); // Binary? Read as decimal, overflows
            ParseBadIPv4Address("040000000000"); // Octal overflow by 1
            ParseBadIPv4Address("4294967296"); // Decimal overflow by 1
            ParseBadIPv4Address("0x100000000"); // Hex overflow by 1
            ParseBadIPv4Address("0.0.0.089"); // Octal (leading zero) but with 8 or 9
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
            IPAddress address; // Sanity test
            Assert.True(IPAddress.TryParse(ipv4String, out address), ipv4String);
            Assert.Equal(AddressFamily.InterNetwork, address.AddressFamily);

            // TryCreate
            Uri testUri;
            Assert.True(Uri.TryCreate("http://" + ipv4String, UriKind.Absolute, out testUri), ipv4String);
            Assert.Equal(UriHostNameType.IPv4, testUri.HostNameType);
            Assert.Equal(address.ToString(), testUri.Host);
            Assert.Equal(address.ToString(), testUri.DnsSafeHost);

            // Constructor
            testUri = new Uri("http://" + ipv4String);
            Assert.Equal(UriHostNameType.IPv4, testUri.HostNameType);
            Assert.Equal(address.ToString(), testUri.Host);
            Assert.Equal(address.ToString(), testUri.DnsSafeHost);

            // CheckHostName
            Assert.Equal(UriHostNameType.IPv4, Uri.CheckHostName(ipv4String));
        }

        private void ParseBadIPv4Address(string badIpv4String)
        {
            IPAddress address; // Sanity test
            Assert.False(IPAddress.TryParse(badIpv4String, out address), badIpv4String);

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

        [Fact]
        public void UriIPv6Host_Any_Success()
        {
            // IPv6Any/IPv6None
            ParseIPv6Address("::");
            ParseIPv6Address("0000:0000:0000:0000:0000:0000:0000:0000");
        }

        [Fact]
        public void UriIPv6Host_MaxValue_Success()
        {
            ParseIPv6Address("FFFF:FFFF:FFFF:FFFF:FFFF:FFFF:FFFF:FFFF");
        }

        [Fact]
        public void UriIPv6Host_LeadingValue_Success()
        {
            ParseIPv6Address("1::");
        }

        [Fact]
        public void UriIPv6Host_CompressionRangeSelection_Success()
        {
            ParseIPv6Address("0:0:0:0:0:0:0:0");
            ParseIPv6Address("1:0:0:0:0:0:0:0");
            ParseIPv6Address("0:1:0:0:0:0:0:0");
            ParseIPv6Address("0:0:1:0:0:0:0:0");
            ParseIPv6Address("0:0:0:1:0:0:0:0");
            ParseIPv6Address("0:0:0:0:1:0:0:0");
            ParseIPv6Address("0:0:0:0:0:1:0:0");
            ParseIPv6Address("0:0:0:0:0:0:1:0");
            ParseIPv6Address("0:0:0:0:0:0:0:1");
            ParseIPv6Address("1:0:0:0:0:0:0:1");
            ParseIPv6Address("1:0:0:0:0:0:0:1");
            ParseIPv6Address("1:1:0:0:0:0:0:1");
            ParseIPv6Address("1:0:1:0:0:0:0:1");
            ParseIPv6Address("1:0:0:1:0:0:0:1");
            ParseIPv6Address("1:0:0:0:1:0:0:1");
            ParseIPv6Address("1:0:0:0:0:1:0:1");
            ParseIPv6Address("1:0:0:0:0:0:1:1");
            ParseIPv6Address("1:1:0:0:1:0:0:1");
            ParseIPv6Address("1:0:1:0:0:1:0:1");
            ParseIPv6Address("1:0:0:1:0:0:1:1");
            ParseIPv6Address("1:1:0:0:0:1:0:1");
            ParseIPv6Address("1:0:0:0:1:0:1:1");
        }

        [Fact]
        public void UriIPv6Host_ScopeId_Success()
        {
            ParseIPv6Address("1::%1");
            ParseIPv6Address("::1%12");
            ParseIPv6Address("::%123");

            // Discrepency: IPAddrees doesn't accept bad scopes, Uri does
            // ParseIPv6Address("::%1a"); // Alpha numeric Scope
        }

        [Fact]
        [ActiveIssue(8360, PlatformID.AnyUnix)]
        public void UriIPv6Host_EmbeddedIPv4_Success()
        {
            ParseIPv6Address("FE08::192.168.0.1"); // Output is not IPv4 mapped
            ParseIPv6Address("::192.168.0.1");
            ParseIPv6Address("::FFFF:192.168.0.1"); // SIIT
            ParseIPv6Address("::FFFF:0:192.168.0.1"); // SIIT

            ParseIPv6Address("::5EFE:192.168.0.1"); // ISATAP
            ParseIPv6Address("1::5EFE:192.168.0.1"); // ISATAP

            ParseIPv6Address("::192.168.0.010"); // Embedded IPv4 octal, read as decimal
        }

        [Fact]
        [ActiveIssue(8356, PlatformID.AnyUnix)]
        public void UriIPv6Host_BadAddresses_AllFail()
        {
            ParseBadIPv6Address("");
            ParseBadIPv6Address(" ");
            ParseBadIPv6Address("1");
            ParseBadIPv6Address(":1");
            ParseBadIPv6Address("1:");
            ParseBadIPv6Address(IPAddress.IPv6Loopback.ToString() + " ");
            ParseBadIPv6Address(" " + IPAddress.IPv6Loopback.ToString());
            ParseBadIPv6Address("1::1::1"); // Ambigious
            ParseBadIPv6Address("1:1\u67081:1:1"); // Unicoded. Crashes .NET 4.0 IPAddress.TryParse
            ParseBadIPv6Address("FE08::260.168.0.1"); // Embedded IPv4 out of range
            ParseBadIPv6Address("::192.168.0.0x0"); // Embedded IPv4 hex
            ParseBadIPv6Address("192.168.0.1"); // Raw IPv4
            ParseBadIPv6Address("G::"); // Hex out of range
            ParseBadIPv6Address("FFFFF::"); // Hex out of range
            ParseBadIPv6Address(":%12"); // Colon Scope
            ParseBadIPv6Address("%12"); // Just Scope

            // TODO # 8330 Discrepency: IPAddress doesn't accept bad scopes, Uri does.
            // ParseBadIPv6Address("::%1a"); // Alpha numeric Scope
        }

        #region Helpers

        // IPAddress parsing succeeds, Uri parsing succeeds, and the canonicalized results match.
        private void ParseIPv6Address(string ipv6String)
        {
            IPAddress address; // Sanity test
            Assert.True(IPAddress.TryParse(ipv6String, out address), ipv6String);
            Assert.Equal(AddressFamily.InterNetworkV6, address.AddressFamily);

            string expectedResult = address.ToString();
            string expectedResultWithBrackets = "[" +
                ((address.ScopeId == 0) ? expectedResult
                    : expectedResult.Substring(0, expectedResult.IndexOf('%'))) // Drop scope id
                + "]";

            // TryCreate
            Uri testUri;
            Assert.True(Uri.TryCreate("http://[" + ipv6String + "]", UriKind.Absolute, out testUri), ipv6String);
            Assert.Equal(UriHostNameType.IPv6, testUri.HostNameType);
            Assert.Equal(expectedResultWithBrackets, testUri.Host);
            Assert.Equal(expectedResult, testUri.DnsSafeHost);

            // Constructor
            testUri = new Uri("http://[" + ipv6String + "]");
            Assert.Equal(UriHostNameType.IPv6, testUri.HostNameType);
            Assert.Equal(expectedResultWithBrackets, testUri.Host);
            Assert.Equal(expectedResult, testUri.DnsSafeHost);

            // CheckHostName
            Assert.Equal(UriHostNameType.IPv6, Uri.CheckHostName(ipv6String));
        }

        private void ParseBadIPv6Address(string badIpv6String)
        {
            IPAddress address; // Sanity test
            Assert.True(!IPAddress.TryParse(badIpv6String, out address)
                || address.AddressFamily != AddressFamily.InterNetworkV6, badIpv6String);

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
