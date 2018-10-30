// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Net.Sockets;
using Xunit;

namespace System.Net.Primitives.Functional.Tests
{
    public class IPEndPointParsing
    {
        [Theory]
        [MemberData(nameof(IPAddressParsing.ValidIpv4Addresses), MemberType = typeof(IPAddressParsing))]    // Just borrow the list from IPAddressParsing
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
                var addressAndPort = isIPv4 ? $"{address}:{i}" : $"[{address}]:{i}";

                // TryParse should return true
                Assert.True(IPEndPoint.TryParse(addressAndPort, out result));
                Assert.Equal(expectedAddress, result.Address.ToString());
                Assert.Equal(i, result.Port);

                // Parse should give us the same result
                result = IPEndPoint.Parse(addressAndPort);
                Assert.Equal(expectedAddress, result.Address.ToString());
                Assert.Equal(i, result.Port);

                // i.e.: 1; 12; 123; 1234; 12345
                portNumber *= 10;
                portNumber += i + 2;
            }
        }

        [Theory]
        [MemberData(nameof(IPAddressParsing.InvalidIpv4Addresses), MemberType = typeof(IPAddressParsing))]
        [MemberData(nameof(IPAddressParsing.InvalidIpv4AddressesStandalone), MemberType = typeof(IPAddressParsing))]
        public void Parse_InvalidAddress_IPv4_Throws(string address)
        {
            Parse_InvalidAddress_Throws(address, true);            
        }

        [Theory]
        [MemberData(nameof(IPAddressParsing.InvalidIpv6Addresses), MemberType = typeof(IPAddressParsing))]
        [MemberData(nameof(IPAddressParsing.InvalidIpv6AddressesNoInner), MemberType = typeof(IPAddressParsing))]
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
                string addressAndPort = isIPv4 ? $"{address}:{i}" : $"[{address}]:{i}";

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
        [MemberData(nameof(IPAddressParsing.ValidIpv4Addresses), MemberType = typeof(IPAddressParsing))]
        public void Parse_InvalidPort_IPv4_Throws(string address, string expectedAddress)
        {
            Parse_InvalidPort_Throws(address, true);
        }

        [Theory]
        [MemberData(nameof(IPAddressParsing.ValidIpv6Addresses), MemberType = typeof(IPAddressParsing))]
        public void Parse_InvalidPort_IPv6_Throws(string address, string expectedAddress)
        {
            Parse_InvalidPort_Throws(address, false);
        }

        private void Parse_InvalidPort_Throws(string address, bool isIPv4)
        {
            InvalidPortHelper(isIPv4 ? $"{address}:65536" : $"[{address}]:65536");  // port exceeds max
            InvalidPortHelper(isIPv4 ? $"{address}:-300" : $"[{address}]:-300");    // port is negative
            InvalidPortHelper(isIPv4 ? $"{address}:+300" : $"[{address}]:+300");    // plug sign

            int portNumber = 1;
            for (int i = 0; i < 5; i++)
            {
                InvalidPortHelper(isIPv4 ? $"{address}:a{i}" : $"[{address}]:a{i}");        // character at start of port
                InvalidPortHelper(isIPv4 ? $"{address}:{i}a" : $"[{address}]:{i}a");        // character at end of port
                InvalidPortHelper(isIPv4 ? $"{address}]:{i}" : $"[{address}]]:{i}");        // bracket where it should not be
                InvalidPortHelper(isIPv4 ? $"{address}:]{i}" : $"[{address}]:]{i}");        // bracket after colon
                InvalidPortHelper(isIPv4 ? $"{address}:{i}]" : $"[{address}]:{i}]");        // trailing bracket
                InvalidPortHelper(isIPv4 ? $"{address}:{i}:" : $"[{address}]:{i}:");        // trailing colon
                InvalidPortHelper(isIPv4 ? $"{address}:{i}:{i}" : $"[{address}]:{i}]:{i}"); // double port
                InvalidPortHelper(isIPv4 ? $"{address}:{i}a{i}" : $"[{address}]:{i}a{i}");  // character in the middle of numbers

                string addressAndPort = isIPv4 ? $"{address}::{i}" : $"[{address}]::{i}";   // double delimiter
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
    }
}
