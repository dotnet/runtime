// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;

using Xunit;

namespace System.Net.Primitives.Functional.Tests
{
    public static class IPNetworkTest
    {
        [Fact]
        public void Parse_Empty_Throws()
        {
            Assert.Throws<FormatException>(() => IPNetwork.Parse(""));
        }

        [Fact]
        public void ParseSpan_Empty_Throws()
        {
            Assert.Throws<FormatException>(() => IPNetwork.Parse("".AsSpan()));
        }

        [Fact]
        public void Parse_RangeWithoutMask_Throws()
        {
            Assert.Throws<FormatException>(() => IPNetwork.Parse("127.0.0.1"));
        }

        [Fact]
        public void ParseSpan_RangeWithoutMask_Throws()
        {
            Assert.Throws<FormatException>(() => IPNetwork.Parse("127.0.0.1".AsSpan()));
        }

        [Fact]
        public void Parse_PrefixNotValidIP_Throws()
        {
            Assert.Throws<FormatException>(() => IPNetwork.Parse("A.B.C.D/24"));
        }

        [Fact]
        public void ParseSpan_PrefixNotValidIP_Throws()
        {
            Assert.Throws<FormatException>(() => IPNetwork.Parse("A.B.C.D/24".AsSpan()));
        }

        [Fact]
        public void Parse_MaskNotValidInt_Throws()
        {
            Assert.Throws<FormatException>(() => IPNetwork.Parse("127.0.0.1/AB"));
        }

        [Fact]
        public void ParseSpan_MaskNotValidInt_Throws()
        {
            Assert.Throws<FormatException>(() => IPNetwork.Parse("127.0.0.1/AB".AsSpan()));
        }

        [Fact]
        public void Parse_MaskLongerThanPrefix_Throws()
        {
            Assert.Throws<FormatException>(() => IPNetwork.Parse("127.0.0.1/33"));
        }

        [Fact]
        public void ParseSpan_MaskLongerThanPrefix_Throws()
        {
            Assert.Throws<FormatException>(() => IPNetwork.Parse("127.0.0.1/33".AsSpan()));
        }

        [Fact]
        public void Parse_PrefixLongerThanMask_Throws()
        {
            // last octet has 1 at LSB therefore mask /31 is not valid
            Assert.Throws<FormatException>(() => IPNetwork.Parse("127.0.0.1/31"));
        }

        [Fact]
        public void ParseSpan_PrefixLongerThanMask_Throws()
        {
            // last octet has 1 at LSB therefore mask /31 is not valid
            Assert.Throws<FormatException>(() => IPNetwork.Parse("127.0.0.1/31".AsSpan()));
        }

        [Fact]
        public void Parse_ValidIPv4_ReturnsCorrectValue()
        {
            var expectedIP = IPAddress.Parse("127.0.0.254");
            var expectedMask = 31u;

            var range = IPNetwork.Parse($"{expectedIP}/{expectedMask}");

            Assert.Equal(expectedIP, range.BaseAddress);
            Assert.Equal(expectedMask, range.MaskLength);
        }

        [Fact]
        public void ParseSpan_ValidIPv4_ReturnsCorrectValue()
        {
            var expectedIP = IPAddress.Parse("127.0.0.254");
            var expectedMask = 31u;

            var range = IPNetwork.Parse($"{expectedIP}/{expectedMask}".AsSpan());

            Assert.Equal(expectedIP, range.BaseAddress);
            Assert.Equal(expectedMask, range.MaskLength);
        }

        [Fact]
        public void Parse_ValidIPv6_ReturnsCorrectValue()
        {
            var expectedIP = IPAddress.Parse("2002::");
            var expectedMask = 16u;

            var range = IPNetwork.Parse($"{expectedIP}/{expectedMask}");

            Assert.Equal(expectedIP, range.BaseAddress);
            Assert.Equal(expectedMask, range.MaskLength);
        }

        [Fact]
        public void ParseSpan_ValidIPv6_ReturnsCorrectValue()
        {
            var expectedIP = IPAddress.Parse("2002::");
            var expectedMask = 16u;

            var range = IPNetwork.Parse($"{expectedIP}/{expectedMask}".AsSpan());

            Assert.Equal(expectedIP, range.BaseAddress);
            Assert.Equal(expectedMask, range.MaskLength);
        }

        [Fact]
        public void Equals_WhenDifferent_ReturnsFalse()
        {
            var a = IPNetwork.Parse("127.0.0.0/24");

            var rangeWithDifferentPrefix = IPNetwork.Parse("127.0.1.0/24");
            var rangeWithDifferentPrefixLength = IPNetwork.Parse("127.0.0.0/25");

            Assert.False(a.Equals(rangeWithDifferentPrefix));
            Assert.False(a.Equals(rangeWithDifferentPrefixLength));
        }

        [Fact]
        public void EqualsSpan_WhenDifferent_ReturnsFalse()
        {
            var a = IPNetwork.Parse("127.0.0.0/24".AsSpan());

            var rangeWithDifferentPrefix = IPNetwork.Parse("127.0.1.0/24".AsSpan());
            var rangeWithDifferentPrefixLength = IPNetwork.Parse("127.0.0.0/25".AsSpan());

            Assert.False(a.Equals(rangeWithDifferentPrefix));
            Assert.False(a.Equals(rangeWithDifferentPrefixLength));
        }

        [Fact]
        public void Equals_WhenSame_ReturnsFalse()
        {
            var a = IPNetwork.Parse("127.0.0.0/24");
            var b = IPNetwork.Parse("127.0.0.0/24");

            Assert.True(a.Equals(b));
            Assert.True(a == b);
            Assert.False(a != b);
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        [Fact]
        public void EqualsSpan_WhenSame_ReturnsFalse()
        {
            var a = IPNetwork.Parse("127.0.0.0/24".AsSpan());
            var b = IPNetwork.Parse("127.0.0.0/24".AsSpan());

            Assert.True(a.Equals(b));
            Assert.True(a == b);
            Assert.False(a != b);
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        [Fact]
        public void Equals_WhenNull_ReturnsFalse()
        {
            var a = IPNetwork.Parse("127.0.0.0/24".AsSpan());

            Assert.False(a.Equals(null!));
        }
    }
}
