// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace System.Net.Connections.Tests
{
    public class ConnectionBaseTest
    {
        [Fact]
        public void Dispose_CallsClose_Success()
        {
            ConnectionCloseMethod? method = null;

            var con = new MockConnection();
            con.OnCloseAsyncCore = (m, t) =>
            {
                method = m;
                return default(ValueTask);
            };

            con.Dispose();

            Assert.Equal(ConnectionCloseMethod.GracefulShutdown, method);
        }

        [Fact]
        public async Task DisposeAsync_CallsClose_Success()
        {
            ConnectionCloseMethod? method = null;

            var con = new MockConnection();
            con.OnCloseAsyncCore = (m, t) =>
            {
                method = m;
                return default(ValueTask);
            };

            await con.DisposeAsync();

            Assert.Equal(ConnectionCloseMethod.GracefulShutdown, method);
        }

        [Fact]
        public void Dispose_CalledOnce_Success()
        {
            int callCount = 0;

            var con = new MockConnection();
            con.OnCloseAsyncCore = delegate
            {
                ++callCount;
                return default(ValueTask);
            };

            con.Dispose();
            con.Dispose();

            Assert.Equal(1, callCount);
        }

        [Fact]
        public async Task DisposeAsync_CalledOnce_Success()
        {
            int callCount = 0;

            var con = new MockConnection();
            con.OnCloseAsyncCore = delegate
            {
                ++callCount;
                return default(ValueTask);
            };

            await con.DisposeAsync();
            await con.DisposeAsync();

            Assert.Equal(1, callCount);
        }
    }
}
