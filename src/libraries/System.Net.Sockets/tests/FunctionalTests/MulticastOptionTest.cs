// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace System.Net.Sockets.Tests
{
    public class MulticastOptionTest
    {
        [Fact]
        public void MulticastOption_Ctor_InvalidArguments_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("group", () => new MulticastOption(null));
            AssertExtensions.Throws<ArgumentNullException>("group", () => new MulticastOption(null, 0));
            AssertExtensions.Throws<ArgumentNullException>("group", () => new MulticastOption(null, null));
            AssertExtensions.Throws<ArgumentNullException>("mcint", () => new MulticastOption(IPAddress.Loopback, null));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("interfaceIndex", () => new MulticastOption(IPAddress.Loopback, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("interfaceIndex", () => new MulticastOption(IPAddress.Loopback, int.MaxValue));
        }

        [Fact]
        public void MulticastOption_Group_Roundtrips()
        {
            var option = new MulticastOption(IPAddress.Any);
            Assert.Same(IPAddress.Any, option.Group);

            option.Group = null;
            Assert.Null(option.Group);

            option.Group = IPAddress.Broadcast;
            Assert.Same(IPAddress.Broadcast, option.Group);
        }

        [Fact]
        public void MulticastOption_InterfaceIndex_Roundtrips()
        {
            var option = new MulticastOption(IPAddress.Any);
            Assert.Equal(0, option.InterfaceIndex);

            option = new MulticastOption(IPAddress.Any, 42);
            Assert.Equal(42, option.InterfaceIndex);

            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => option.InterfaceIndex = -1);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => option.InterfaceIndex = int.MaxValue);

            option.InterfaceIndex = 1;
            Assert.Equal(1, option.InterfaceIndex);
        }

        [Fact]
        public void MulticastOption_LocalAddress_Roundtrips()
        {
            var options = new MulticastOption(IPAddress.Any);
            Assert.Same(IPAddress.Any, options.LocalAddress);

            options = new MulticastOption(IPAddress.Loopback, 42);
            Assert.Equal(42, options.InterfaceIndex);
            Assert.Null(options.LocalAddress);

            options.LocalAddress = IPAddress.Broadcast;
            Assert.Equal(0, options.InterfaceIndex);
            Assert.Same(IPAddress.Broadcast, options.LocalAddress);

            options = new MulticastOption(IPAddress.Loopback, IPAddress.Any);
            Assert.Same(IPAddress.Any, options.LocalAddress);
        }

        [Fact]
        public void IPv6MulticastOption_Ctor_InvalidArguments_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("group", () => new IPv6MulticastOption(null));
            AssertExtensions.Throws<ArgumentNullException>("group", () => new IPv6MulticastOption(null, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("ifindex", () => new IPv6MulticastOption(IPAddress.Loopback, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("ifindex", () => new IPv6MulticastOption(IPAddress.Loopback, long.MaxValue));
        }

        [Fact]
        public void IPv6MulticastOption_Group_Roundtrips()
        {
            var option = new IPv6MulticastOption(IPAddress.Any);
            Assert.Same(IPAddress.Any, option.Group);

            AssertExtensions.Throws<ArgumentNullException>("value", () => option.Group = null);

            option.Group = IPAddress.Broadcast;
            Assert.Same(IPAddress.Broadcast, option.Group);
        }

        [Fact]
        public void IPv6MulticastOption_InterfaceIndex_Roundtrips()
        {
            var option = new IPv6MulticastOption(IPAddress.Any);
            Assert.Equal(0, option.InterfaceIndex);

            option = new IPv6MulticastOption(IPAddress.Any, 42);
            Assert.Equal(42, option.InterfaceIndex);

            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => option.InterfaceIndex = -1);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => option.InterfaceIndex = long.MaxValue);

            option.InterfaceIndex = 1;
            Assert.Equal(1, option.InterfaceIndex);
        }
    }
}
