// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.Net.NetworkInformation.Tests
{
    public class PingOptionsTest
    {
        [Fact]
        public void DefaultProperties()
        {
            Assert.Equal(128, new PingOptions().Ttl);
            Assert.False(new PingOptions().DontFragment);
        }

        [Fact]
        public void CtorValuesPassedToProperties()
        {
            const int Ttl = 42;
            Assert.Equal(Ttl, new PingOptions(Ttl, false).Ttl);
            Assert.True(new PingOptions(1, true).DontFragment);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Ttl_InvalidValues_ThrowsException(int ttl)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PingOptions(ttl, false));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PingOptions().Ttl = ttl);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(int.MaxValue)]
        public void Ttl_SetGetValidValues_Success(int ttl)
        {
            Assert.Equal(ttl, new PingOptions() { Ttl = ttl }.Ttl);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void DontFragment_SetGetValidValues_Success(bool dontFragment)
        {
            Assert.Equal(dontFragment, new PingOptions(1, dontFragment).DontFragment);
            Assert.Equal(dontFragment, new PingOptions { DontFragment = dontFragment }.DontFragment);
        }
    }
}
