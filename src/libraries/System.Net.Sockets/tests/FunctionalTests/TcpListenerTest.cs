// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Sockets.Tests
{
    public class TcpListenerTest
    {
        [Fact]
        public void Ctor_InvalidArguments_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("localEP", () => new TcpListener(null));
            AssertExtensions.Throws<ArgumentNullException>("localaddr", () => new TcpListener(null, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("port", () => new TcpListener(IPAddress.Loopback, -1));
#pragma warning disable 0618 // ctor is obsolete
            AssertExtensions.Throws<ArgumentOutOfRangeException>("port", () => new TcpListener(66000));
#pragma warning restore 0618
            AssertExtensions.Throws<ArgumentOutOfRangeException>("port", () => TcpListener.Create(66000));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsSubsystemForLinux))] // [ActiveIssue(11057)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void Active_TrueWhileRunning(int ctor)
        {
            var listener =
                ctor == 0 ? new DerivedTcpListener(new IPEndPoint(IPAddress.Loopback, 0)) :
                ctor == 1 ? new DerivedTcpListener(IPAddress.Loopback, 0) :
                new DerivedTcpListener(0);
            Assert.False(listener.Active);
            listener.Start();
            Assert.True(listener.Active);
            Assert.Throws<InvalidOperationException>(() => listener.AllowNatTraversal(false));
            Assert.Throws<InvalidOperationException>(() => listener.ExclusiveAddressUse = true);
            Assert.Throws<InvalidOperationException>(() => listener.ExclusiveAddressUse = false);
            bool ignored = listener.ExclusiveAddressUse; // we can get it while active, just not set it
            listener.Stop();
            Assert.False(listener.Active);
        }

        [Fact]
        public void Start_InvalidArgs_Throws()
        {
            var listener = new DerivedTcpListener(IPAddress.Loopback, 0);

            Assert.Throws<ArgumentOutOfRangeException>(() => listener.Start(-1));
            Assert.False(listener.Active);

            listener.Start(1);
            listener.Start(1); // ok to call twice
            listener.Stop();
        }

        [Fact]
        public async Task Pending_TrueWhenWaitingRequest()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);

            Assert.Throws<InvalidOperationException>(() => listener.Pending());
            listener.Start();
            Assert.False(listener.Pending());
            using (TcpClient client = new TcpClient(new IPEndPoint(IPAddress.Loopback, 0)))
            {
                Task connectTask = client.ConnectAsync(IPAddress.Loopback, ((IPEndPoint)listener.LocalEndpoint).Port);
                Assert.True(SpinWait.SpinUntil(() => listener.Pending(), 30000), "Expected Pending to be true within timeout");
                listener.AcceptSocket().Dispose();
                await connectTask;
            }
            listener.Stop();
            Assert.Throws<InvalidOperationException>(() => listener.Pending());
        }

        [Fact]
        public void Accept_Invalid_Throws()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);

            Assert.Throws<InvalidOperationException>(() => listener.AcceptSocket());
            Assert.Throws<InvalidOperationException>(() => listener.AcceptTcpClient());
            Assert.Throws<InvalidOperationException>(() => listener.BeginAcceptSocket(null, null));
            Assert.Throws<InvalidOperationException>(() => listener.BeginAcceptTcpClient(null, null));
            Assert.Throws<InvalidOperationException>(() => { listener.AcceptSocketAsync(); });
            Assert.Throws<InvalidOperationException>(() => { listener.AcceptTcpClientAsync(); });

            Assert.Throws<ArgumentNullException>(() => listener.EndAcceptSocket(null));
            Assert.Throws<ArgumentNullException>(() => listener.EndAcceptTcpClient(null));

            AssertExtensions.Throws<ArgumentException>("asyncResult", () => listener.EndAcceptSocket(Task.CompletedTask));
            AssertExtensions.Throws<ArgumentException>("asyncResult", () => listener.EndAcceptTcpClient(Task.CompletedTask));
        }

        [Fact]
        public async Task Accept_AcceptsPendingSocketOrClient()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();

            using (var client = new TcpClient())
            {
                Task connectTask = client.ConnectAsync(IPAddress.Loopback, ((IPEndPoint)listener.LocalEndpoint).Port);
                using (Socket s = listener.AcceptSocket())
                {
                    Assert.False(listener.Pending());
                }
                await connectTask;
            }

            using (var client = new TcpClient())
            {
                Task connectTask = client.ConnectAsync(IPAddress.Loopback, ((IPEndPoint)listener.LocalEndpoint).Port);
                using (TcpClient c = listener.AcceptTcpClient())
                {
                    Assert.False(listener.Pending());
                }
                await connectTask;
            }

            listener.Stop();
        }

        [Fact]
        // This verify that basic constructs do work when IPv6 is NOT available.
        public void IPv6_Only_Works()
        {
            if (Socket.OSSupportsIPv6 || !Socket.OSSupportsIPv4)
            {
                // TBD we should figure out better way how to execute this in IPv4 only environment.
                return; 
            }

            // This should not throw e.g. default to IPv6.
            TcpListener  l = TcpListener.Create(0);
            l.Stop();

            Socket s = new Socket(SocketType.Stream, ProtocolType.Tcp);
            s.Close();
        }

        private sealed class DerivedTcpListener : TcpListener
        {
#pragma warning disable 0618
            public DerivedTcpListener(int port) : base(port) { }
#pragma warning restore 0618
            public DerivedTcpListener(IPEndPoint endpoint) : base(endpoint) { }
            public DerivedTcpListener(IPAddress address, int port) : base(address, port) { }
            public new bool Active => base.Active;
        }
    }
}
