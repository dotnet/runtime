// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.Net.NetworkInformation.Tests
{
    public class MiscParsingTests
    {
        [Fact]
        public static void NumRoutesParsing()
        {
            FileUtil.NormalizeLineEndings("route", "route_normalized0");
            int numRoutes = StringParsingHelpers.ParseNumRoutesFromRouteFile("route_normalized0");
            Assert.Equal(4, numRoutes);
        }

        [Fact]
        public static void DefaultTtlParsing()
        {
            FileUtil.NormalizeLineEndings("snmp", "snmp_normalized0");
            int ttl = StringParsingHelpers.ParseDefaultTtlFromFile("snmp_normalized0");
            Assert.Equal(64, ttl);
        }

        [Fact]
        public static void RawIntFileParsing()
        {
            int val = StringParsingHelpers.ParseRawIntFile("rawint");
            Assert.Equal(12, val);

            int max = StringParsingHelpers.ParseRawIntFile("rawint_maxvalue");
            Assert.Equal(int.MaxValue, max);
        }

        [Fact]
        public static void RawLongFileParsing()
        {
            long val = StringParsingHelpers.ParseRawLongFile("rawlong");
            Assert.Equal(3147483647L, val);

            long max = StringParsingHelpers.ParseRawLongFile("rawlong_maxvalue");
            Assert.Equal(long.MaxValue, max);
        }

        [Fact]
        public static void RawHexIntParsing()
        {
            int val = StringParsingHelpers.ParseRawHexFileAsInt("rawhexint");
            Assert.Equal(10, val);
        }
    }
}
