// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            int val = StringParsingHelpers.ParseRawIntFile(FileUtil.TestFile("rawint"));
            Assert.Equal(12, val);

            int max = StringParsingHelpers.ParseRawIntFile(FileUtil.TestFile("rawint_maxvalue"));
            Assert.Equal(int.MaxValue, max);
        }

        [Fact]
        public static void RawLongFileParsing()
        {
            long val = StringParsingHelpers.ParseRawLongFile(FileUtil.TestFile("rawlong"));
            Assert.Equal(3147483647L, val);

            long max = StringParsingHelpers.ParseRawLongFile(FileUtil.TestFile("rawlong_maxvalue"));
            Assert.Equal(long.MaxValue, max);
        }

        [Fact]
        public static void RawHexIntParsing()
        {
            int val = StringParsingHelpers.ParseRawHexFileAsInt(FileUtil.TestFile("rawhexint"));
            Assert.Equal(10, val);
        }
    }
}
