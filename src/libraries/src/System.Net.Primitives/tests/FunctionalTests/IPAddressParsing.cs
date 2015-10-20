// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;

using Xunit;

namespace System.Net.Primitives.Functional.Tests
{
    public class IPAddressParsing
    {
        #region IPv4

        [Fact]
        public void ParseIPv4_Basic_Success()
        {
            Assert.Equal("192.168.0.1", IPAddress.Parse("192.168.0.1").ToString());
        }

        [Fact]
        public void ParseIPv4_WithSubnet_Failure()
        {
            Assert.Throws<FormatException>(() => { IPAddress.Parse("192.168.0.0/16"); });
        }

        [Fact]
        public void ParseIPv4_WithPort_Failure()
        {
            Assert.Throws<FormatException>(() => { IPAddress.Parse("192.168.0.1:80"); });
        }

        #endregion

        #region IPv6

        [Fact]
        public void ParseIPv6_NoBrackets_Success()
        {
            Assert.Equal("fe08::1", IPAddress.Parse("Fe08::1").ToString());
        }

        [Fact]
        public void ParseIPv6_Brackets_SuccessBracketsDropped()
        {
            Assert.Equal("fe08::1", IPAddress.Parse("[Fe08::1]").ToString());
        }

        [Fact]
        public void ParseIPv6_LeadingBracket_Failure()
        {
            Assert.Throws<FormatException>(() => { IPAddress.Parse("[Fe08::1"); });
        }

        [Fact]
        public void ParseIPv6_TrailingBracket_Failure()
        {
            Assert.Throws<FormatException>(() => { IPAddress.Parse("Fe08::1]"); });
        }

        [Fact]
        public void ParseIPv6_BracketsAndPort_SuccessBracketsAndPortDropped()
        {
            Assert.Equal("fe08::1", IPAddress.Parse("[Fe08::1]:80").ToString());
        }

        [Fact]
        public void ParseIPv6_BracketsAndInvalidPort_Failure()
        {
            Assert.Throws<FormatException>(() => { IPAddress.Parse("[Fe08::1]:80Z"); });
        }

        [Fact]
        public void ParseIPv6_BracketsAndHexPort_SuccessBracketsAndPortDropped()
        {
            Assert.Equal("fe08::1", IPAddress.Parse("[Fe08::1]:0xFA").ToString());
        }

        [Fact]
        public void ParseIPv6_WithSubnet_Failure()
        {
            Assert.Throws<FormatException>(() => { IPAddress.Parse("Fe08::/64"); });
        }

        [Fact]
        public void ParseIPv6_ScopeId_Success()
        {
            Assert.Equal("fe08::1%13542", IPAddress.Parse("Fe08::1%13542").ToString());
        }

        #endregion

        [Fact]
        public void Parse_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => { IPAddress.Parse(null); });
        }

        [Fact]
        public void TryParse_Null_False()
        {
            IPAddress ipAddress;
            Assert.False(IPAddress.TryParse(null, out ipAddress));
        }

        [Fact]
        public void Parse_Empty_Throws()
        {
            Assert.Throws<FormatException>(() => { IPAddress.Parse(String.Empty); });
        }

        [Fact]
        public void TryParse_Empty_False()
        {
            IPAddress ipAddress;
            Assert.False(IPAddress.TryParse(String.Empty, out ipAddress));
        }
    }
}
