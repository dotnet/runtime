// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Net.Sockets;
using System.Text;
using Xunit;

namespace System.Net.Primitives.Functional.Tests
{
    public class IPEndPointParsing
    {
        [Theory]
        [MemberData(nameof(IPAddressParsingFormatting.ValidIpv4Addresses), MemberType = typeof(IPAddressParsingFormatting))]    // Just borrow the list from IPAddressParsing
        public void Parse_ValidEndPoint_IPv4_Success(string address, string expectedAddress)
        {
            Parse_ValidEndPoint_Success(address, expectedAddress, true);
        }

        [Theory]
        [MemberData(nameof(ValidIpv6AddressesNoPort))]  // We need our own list here to explicitly exclude port numbers and brackets without making the test overly complicated (and less valid)
        public void Parse_ValidEndPoint_IPv6_Success(string address, string expectedAddress)
        {
            Parse_ValidEndPoint_Success(address, expectedAddress, false);
        }

        private void Parse_ValidEndPoint_Success(string address, string expectedAddress, bool isIPv4)
        {
            // We'll parse just the address alone followed by the address with various port numbers

            expectedAddress = expectedAddress.ToLowerInvariant();   // This is done in the IP parse routines

            // TryParse should return true
            Assert.True(IPEndPoint.TryParse(address, out IPEndPoint result));
            Assert.Equal(expectedAddress, result.Address.ToString());
            Assert.Equal(0, result.Port);

            // Parse should give us the same result
            result = IPEndPoint.Parse(address);
            Assert.Equal(expectedAddress, result.Address.ToString());
            Assert.Equal(0, result.Port);

            // Cover varying lengths of port number
            int portNumber = 1;
            for (int i = 0; i < 5; i++)
            {
                var addressAndPort = isIPv4 ? $"{address}:{portNumber}" : $"[{address}]:{portNumber}";

                // TryParse should return true
                Assert.True(IPEndPoint.TryParse(addressAndPort, out result));
                Assert.Equal(expectedAddress, result.Address.ToString());
                Assert.Equal(portNumber, result.Port);

                // Parse should give us the same result
                result = IPEndPoint.Parse(addressAndPort);
                Assert.Equal(expectedAddress, result.Address.ToString());
                Assert.Equal(portNumber, result.Port);

                // i.e.: 1; 12; 123; 1234; 12345
                portNumber *= 10;
                portNumber += i + 2;
            }
        }

        [Theory]
        [MemberData(nameof(IPAddressParsingFormatting.InvalidIpv4Addresses), MemberType = typeof(IPAddressParsingFormatting))]
        [MemberData(nameof(IPAddressParsingFormatting.InvalidIpv4AddressesStandalone), MemberType = typeof(IPAddressParsingFormatting))]
        public void Parse_InvalidAddress_IPv4_Throws(string address)
        {
            Parse_InvalidAddress_Throws(address, true);
        }

        [Theory]
        [MemberData(nameof(IPAddressParsingFormatting.InvalidIpv6Addresses), MemberType = typeof(IPAddressParsingFormatting))]
        [MemberData(nameof(IPAddressParsingFormatting.InvalidIpv6AddressesNoInner), MemberType = typeof(IPAddressParsingFormatting))]
        public void Parse_InvalidAddress_IPv6_Throws(string address)
        {
            Parse_InvalidAddress_Throws(address, false);
        }

        private void Parse_InvalidAddress_Throws(string address, bool isIPv4)
        {
            // TryParse should return false and set result to null
            Assert.False(IPEndPoint.TryParse(address, out IPEndPoint result));
            Assert.Null(result);

            // Parse should throw
            Assert.Throws<FormatException>(() => IPEndPoint.Parse(address));

            int portNumber = 1;
            for (int i = 0; i < 5; i++)
            {
                string addressAndPort = isIPv4 ? $"{address}:{portNumber}" : $"[{address}]:{portNumber}";

                // TryParse should return false and set result to null
                result = new IPEndPoint(IPAddress.Parse("0"), 25);
                Assert.False(IPEndPoint.TryParse(addressAndPort, out result));
                Assert.Null(result);

                // Parse should throw
                Assert.Throws<FormatException>(() => IPEndPoint.Parse(addressAndPort));

                // i.e.: 1; 12; 123; 1234; 12345
                portNumber *= 10;
                portNumber += i + 2;
            }
        }

        [Theory]
        [MemberData(nameof(IPAddressParsingFormatting.ValidIpv4Addresses), MemberType = typeof(IPAddressParsingFormatting))]
        public void Parse_InvalidPort_IPv4_Throws(string address, string expectedAddress)
        {
            _ = expectedAddress;
            Parse_InvalidPort_Throws(address, isIPv4: true);
        }

        [Theory]
        [MemberData(nameof(IPAddressParsingFormatting.ValidIpv6Addresses), MemberType = typeof(IPAddressParsingFormatting))]
        public void Parse_InvalidPort_IPv6_Throws(string address, string expectedAddress)
        {
            _ = expectedAddress;
            Parse_InvalidPort_Throws(address, isIPv4: false);
        }

        private void Parse_InvalidPort_Throws(string address, bool isIPv4)
        {
            InvalidPortHelper(isIPv4 ? $"{address}:65536" : $"[{address}]:65536");  // port exceeds max
            InvalidPortHelper(isIPv4 ? $"{address}:-300" : $"[{address}]:-300");    // port is negative
            InvalidPortHelper(isIPv4 ? $"{address}:+300" : $"[{address}]:+300");    // plug sign

            int portNumber = 1;
            for (int i = 0; i < 5; i++)
            {
                InvalidPortHelper(isIPv4 ? $"{address}:a{portNumber}" : $"[{address}]:a{portNumber}");        // character at start of port
                InvalidPortHelper(isIPv4 ? $"{address}:{portNumber}a" : $"[{address}]:{portNumber}a");        // character at end of port
                InvalidPortHelper(isIPv4 ? $"{address}]:{portNumber}" : $"[{address}]]:{portNumber}");        // bracket where it should not be
                InvalidPortHelper(isIPv4 ? $"{address}:]{portNumber}" : $"[{address}]:]{portNumber}");        // bracket after colon
                InvalidPortHelper(isIPv4 ? $"{address}:{portNumber}]" : $"[{address}]:{portNumber}]");        // trailing bracket
                InvalidPortHelper(isIPv4 ? $"{address}:{portNumber}:" : $"[{address}]:{portNumber}:");        // trailing colon
                InvalidPortHelper(isIPv4 ? $"{address}:{portNumber}:{portNumber}" : $"[{address}]:{portNumber}]:{portNumber}"); // double port
                InvalidPortHelper(isIPv4 ? $"{address}:{portNumber}a{portNumber}" : $"[{address}]:{portNumber}a{portNumber}");  // character in the middle of numbers

                string addressAndPort = isIPv4 ? $"{address}::{portNumber}" : $"[{address}]::{portNumber}";   // double delimiter
                // Appending two colons to an address may create a valid one (e.g. "0" becomes "0::x").
                // If and only if the address parsers says it's not valid then we should as well
                if (!IPAddress.TryParse(addressAndPort, out IPAddress ipAddress))
                {
                    InvalidPortHelper(addressAndPort);
                }

                // i.e.: 1; 12; 123; 1234; 12345
                portNumber *= 10;
                portNumber += i + 2;
            }
        }

        private void InvalidPortHelper(string addressAndPort)
        {
            // TryParse should return false and set result to null
            Assert.False(IPEndPoint.TryParse(addressAndPort, out IPEndPoint result));
            Assert.Null(result);

            // Parse should throw
            Assert.Throws<FormatException>(() => IPEndPoint.Parse(addressAndPort));
        }

        public static readonly object[][] ValidIpv6AddressesNoPort =
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
            new object[] { "2001:0db8::0001", "2001:db8::1" }, // leading 0s suppressed
            new object[] { "3731:54:65fe:2::a7", "3731:54:65fe:2::a7" }, // Unicast
            new object[] { "3731:54:65fe:2::a8", "3731:54:65fe:2::a8" }, // Anycast
            // ScopeID
            new object[] { "Fe08::1%13542", "fe08::1%13542" },
            new object[] { "1::%1", "1::%1" },
            new object[] { "::1%12", "::1%12" },
            new object[] { "::%123", "::%123" },
            // v4 as v6
            new object[] { "FE08::192.168.0.1", "fe08::c0a8:1" }, // Output is not IPv4 mapped
            new object[] { "::192.168.0.1", "::192.168.0.1" },
            new object[] { "::FFFF:192.168.0.1", "::ffff:192.168.0.1" }, // SIIT
            new object[] { "::FFFF:0:192.168.0.1", "::ffff:0:192.168.0.1" }, // SIIT
            new object[] { "::5EFE:192.168.0.1", "::5efe:192.168.0.1" }, // ISATAP
            new object[] { "1::5EFE:192.168.0.1", "1::5efe:192.168.0.1" }, // ISATAP
            new object[] { "::192.168.0.010", "::192.168.0.10" }, // Embedded IPv4 octal, read as decimal
        };

        public static readonly object[][] ValidIpv4Andv6AddressesWithAndWithoutPort =
        {
            ["[::1]", "::1", 0],
            ["[::1]:5040", "::1", 5040],
            ["10.12.13.14", "10.12.13.14", 0],
            ["10.12.13.14:5040", "10.12.13.14", 5040],
            ["0:0:111:234:5:6:789A:0", "::111:234:5:6:789a:0", 0],
            ["[0:0:111:234:5:6:789A:0]:443", "::111:234:5:6:789a:0", 443],
            ["E:E:E:E:E:E:0:1", "e:e:e:e:e:e:0:1", 0],
            ["[E:E:E:E:E:E:0:1]:443", "e:e:e:e:e:e:0:1", 443]
        };

        public static readonly object[][] InvalidIpv4Andv6AddressesWithAndWithoutPort =
        {
            ["10.12.13.-14"],
            ["10.a12.13.14"],
            ["10.12.13.14:-135"],
            ["10.12.13.14:1a35"],
            ["10.12.13.14:"],
            ["10.12.13.14:135:135"],
            ["[E:E:E:E:E:E:0:1]:]443"],
            ["[[E:E:E:E:E:E:0:1]:443"],
            ["[E:E:E:E:E:E:0:1]:443:443"]
        };

        public static readonly object[][] ValidAndInvalidIpv4Andv6AddressesWithAndWithoutPort =
        {
            ["[::1]", true, "::1", 0],
            ["[::1]:5040", true, "::1", 5040],
            ["10.12.13.14", true, "10.12.13.14", 0],
            ["10.12.13.14:5040", true, "10.12.13.14", 5040],
            ["10.12.13.14:65548", false, "", 0],
            ["10.12.13.14:-135", false, "", 0],
            ["10.12.13.14:1a35", false, "", 0],
            ["0:0:111:234:5:6:789A:0", true, "::111:234:5:6:789a:0", 0],
            ["[0:0:111:234:5:6:789A:0]:443", true, "::111:234:5:6:789a:0", 443],
            ["[0:0:111:234:5:6:789A:0]:65548", false, "", 0],
            ["[0:0:111:234:5:6:789A:0]:-135", false, "", 0],
            ["[0:0:111:234:5:6:789A:0]:1a35", false, "", 0],
            ["E:E:E:E:E:E:0:1", true, "e:e:e:e:e:e:0:1", 0],
            ["[E:E:E:E:E:E:0:1]:443", true, "e:e:e:e:e:e:0:1", 443],
            ["[E:E:E:E:E:E:0:1]:1a35", false, "", 0]
        };

        public static readonly object[][] FormattedIpv4Andv6AddressesWithAndWithoutPort =
        {
            ["[::1]", "[::1]:0"],
            ["[::1]:5040", "[::1]:5040"],
            ["10.12.13.14", "10.12.13.14:0"],
            ["10.12.13.14:5040", "10.12.13.14:5040"],
            ["0:0:111:234:5:6:789A:0", "[::111:234:5:6:789a:0]:0"],
            ["[0:0:111:234:5:6:789A:0]:443", "[::111:234:5:6:789a:0]:443"],
            ["E:E:E:E:E:E:0:1", "[e:e:e:e:e:e:0:1]:0"],
            ["[E:E:E:E:E:E:0:1]:443", "[e:e:e:e:e:e:0:1]:443"],
        };

        [Theory]
        [MemberData(nameof(ValidIpv4Andv6AddressesWithAndWithoutPort))]
        public static void ParseByteSpan(string input, string address, int port)
        {
            ReadOnlySpan<byte> bytes = Encoding.UTF8.GetBytes(input);

            IPEndPoint result = IPEndPoint.Parse(bytes);

            Assert.Equal(port, result.Port);
            Assert.Equal(address, result.Address.ToString());
        }

        [Theory]
        [MemberData(nameof(InvalidIpv4Andv6AddressesWithAndWithoutPort))]
        public static void ParseInvalidByteSpan_ThrowsFormatException(string input)
        {
            Assert.Throws<FormatException>(() =>
            {
                ReadOnlySpan<byte> bytes = Encoding.UTF8.GetBytes(input);
                IPEndPoint.Parse(bytes);
            });
        }

        [Theory]
        [MemberData(nameof(ValidAndInvalidIpv4Andv6AddressesWithAndWithoutPort))]
        public static void TryParseByteSpan(string input, bool parsed, string address, int port)
        {
            ReadOnlySpan<byte> bytes = Encoding.UTF8.GetBytes(input);

            Assert.Equal(parsed, IPEndPoint.TryParse(bytes, out IPEndPoint result));

            if (parsed)
            {
                Assert.Equal(port, result.Port);
                Assert.Equal(address, result.Address.ToString());
            }
        }

        [Theory]
        [MemberData(nameof(FormattedIpv4Andv6AddressesWithAndWithoutPort))]
        public static void TryFormatCharSpan(string input, string expectedFormattedString)
        {
            Span<char> destination = stackalloc char[expectedFormattedString.Length];

            Assert.True(IPEndPoint.Parse(input).TryFormat(destination, out int charsWritten));
            Assert.Equal(expectedFormattedString.Length, charsWritten);
            Assert.True(MemoryExtensions.Equals(destination, expectedFormattedString, StringComparison.Ordinal));
        }

        [Theory]
        [MemberData(nameof(FormattedIpv4Andv6AddressesWithAndWithoutPort))]
        public static void TryFormatByteSpan(string input, string expected)
        {
            Span<byte> destination = stackalloc byte[expected.Length];

            Assert.True(IPEndPoint.Parse(input).TryFormat(destination, out int charsWritten));
            Assert.Equal(expected.Length, charsWritten);
            Assert.True(MemoryExtensions.SequenceEqual(destination, Encoding.UTF8.GetBytes(expected)));
        }

        [Theory]
        [MemberData(nameof(FormattedIpv4Andv6AddressesWithAndWithoutPort))]
        public static void ToFormatIFormattable(string input, string expectedFormat)
        {
            IPEndPoint ep = IPEndPoint.Parse(input);
            string result = string.Format("display {0:G}", ep);

            Assert.Equal($"display {expectedFormat}", result);
        }

        private static class ParsableHelper<TSelf> where TSelf : IParsable<TSelf>
        {
            public static TSelf Parse(string s, IFormatProvider provider) => TSelf.Parse(s, provider);

            public static bool TryParse(string? s, IFormatProvider provider, out TSelf result) => TSelf.TryParse(s, provider, out result);
        }

        [Theory]
        [MemberData(nameof(ValidIpv4Andv6AddressesWithAndWithoutPort))]
        public static void ParseIParsable(string input, string address, int port)
        {
            IPEndPoint result = ParsableHelper<IPEndPoint>.Parse(input, CultureInfo.InvariantCulture);
            Assert.Equal(port, result.Port);
            Assert.Equal(address, result.Address.ToString());
        }

        [Fact]
        public static void ParseNullIParsable_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ParsableHelper<IPEndPoint>.Parse(null, CultureInfo.InvariantCulture));
        }

        [Theory]
        [MemberData(nameof(InvalidIpv4Andv6AddressesWithAndWithoutPort))]
        public static void ParseInvalidIParsable_ThrowsFormatException(string input)
        {
            Assert.Throws<FormatException>(() => ParsableHelper<IPEndPoint>.Parse(input, CultureInfo.InvariantCulture));
        }

        [Theory]
        [MemberData(nameof(ValidAndInvalidIpv4Andv6AddressesWithAndWithoutPort))]
        public static void TryParseIParsable(string? input, bool parsed, string address, int port)
        {
            Assert.Equal(parsed, ParsableHelper<IPEndPoint>.TryParse(input, CultureInfo.InvariantCulture, out IPEndPoint result));

            if (parsed)
            {
                Assert.Equal(port, result.Port);
                Assert.Equal(address, result.Address.ToString());
            }
        }

        private static class SpanParsableHelper<TSelf> where TSelf : ISpanParsable<TSelf>
        {
            public static TSelf Parse(ReadOnlySpan<char> s, IFormatProvider provider) => TSelf.Parse(s, provider);

            public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider provider, out TSelf result) => TSelf.TryParse(s, provider, out result);
        }

        [Theory]
        [MemberData(nameof(ValidIpv4Andv6AddressesWithAndWithoutPort))]
        public static void ParseISpanParsable(string input, string address, int port)
        {
            IPEndPoint result = SpanParsableHelper<IPEndPoint>.Parse(input, CultureInfo.InvariantCulture);

            Assert.Equal(port, result.Port);
            Assert.Equal(address, result.Address.ToString());
        }

        [Theory]
        [MemberData(nameof(InvalidIpv4Andv6AddressesWithAndWithoutPort))]
        public static void ParseInvalidISpanParsable_ThrowsFormatException(string input)
        {
            Assert.Throws<FormatException>(() => SpanParsableHelper<IPEndPoint>.Parse(input, CultureInfo.InvariantCulture));
        }

        [Theory]
        [MemberData(nameof(ValidAndInvalidIpv4Andv6AddressesWithAndWithoutPort))]
        public static void TryParseISpanParsable(string input, bool parsed, string address, int port)
        {
            Assert.Equal(parsed, SpanParsableHelper<IPEndPoint>.TryParse(input, CultureInfo.InvariantCulture, out IPEndPoint result));

            if (parsed )
            {
                Assert.Equal(port, result.Port);
                Assert.Equal(address, result.Address.ToString());
            }
        }

        private static class Utf8SpanParsableHelper<TSelf> where TSelf : IUtf8SpanParsable<TSelf>
        {
            public static TSelf Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider provider) => TSelf.Parse(utf8Text, provider);

            public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider provider, out TSelf result) => TSelf.TryParse(utf8Text, provider, out result);
        }

        [Theory]
        [MemberData(nameof(ValidIpv4Andv6AddressesWithAndWithoutPort))]
        public static void ParseIUtf8SpanParsable(string input, string address, int port)
        {
            ReadOnlySpan<byte> bytes = Encoding.UTF8.GetBytes(input);

            IPEndPoint result = Utf8SpanParsableHelper<IPEndPoint>.Parse(bytes, CultureInfo.InvariantCulture);

            Assert.Equal(port, result.Port);
            Assert.Equal(address, result.Address.ToString());
        }

        [Theory]
        [MemberData(nameof(InvalidIpv4Andv6AddressesWithAndWithoutPort))]
        public static void ParseInvalidIUtf8SpanParsable_ThrowsFormatException(string input)
        {
            Assert.Throws<FormatException>(() =>
            {
                ReadOnlySpan<byte> bytes = Encoding.UTF8.GetBytes(input);
                Utf8SpanParsableHelper<IPEndPoint>.Parse(bytes, CultureInfo.InvariantCulture);
            });
        }

        [Theory]
        [MemberData(nameof(ValidAndInvalidIpv4Andv6AddressesWithAndWithoutPort))]
        public static void TryParseIUtf8SpanParsable(string input, bool parsed, string address, int port)
        {
            ReadOnlySpan<byte> bytes = Encoding.UTF8.GetBytes(input);

            Assert.Equal(parsed, Utf8SpanParsableHelper<IPEndPoint>.TryParse(bytes, CultureInfo.InvariantCulture, out IPEndPoint result));

            if (parsed)
            {
                Assert.Equal(port, result.Port);
                Assert.Equal(address, result.Address.ToString());
            }
        }
    }
}
