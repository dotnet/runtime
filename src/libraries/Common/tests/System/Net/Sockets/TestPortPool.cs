// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Security;
using System.Net.Test.Common;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.DotNet.RemoteExecutor;

namespace System.Net.Sockets.Tests
{
    internal readonly struct PortLease : IDisposable
    {
        public int Port { get; }

        internal PortLease(int port) => Port = port;

        public void Dispose() => TestPortPool.Return(this);
    }

    internal class TestPortPoolExhaustedException : Exception
    {
        public TestPortPoolExhaustedException()
            : base($"TestPortPool failed to find an available port after {TestPortPool.ThrowExhaustedAfter} attempts")
        {
        }
    }

    /// <summary>
    /// Distributes ports from the range defined by <see cref="Configuration.Sockets.TestPoolPortRange"/>
    /// in a synchronized way. Synchronization does not work across multiple processes.
    /// </summary>
    internal static class TestPortPool
    {
        internal const int ThrowExhaustedAfter = 10;

        private static readonly int MinPort =
            System.Net.Test.Common.Configuration.Sockets.TestPoolPortRange.Min;
        private static readonly int MaxPort =
            System.Net.Test.Common.Configuration.Sockets.TestPoolPortRange.Max;
        private static readonly int PortRangeLength = MaxPort - MinPort;

        private static readonly ConcurrentDictionary<int, int> s_usedPorts = GetAllPortsUsedBySystem();
        private static int s_counter = int.MinValue;

        public static PortLease RentPort()
        {
            for (int i = 0; i < ThrowExhaustedAfter; i++)
            {
                // Although race conditions may happen theoretically because the following code block is not atomic,
                // it requires the s_counter to move at least PortRangeLength steps between Increment and TryAdd,
                // which is very unlikely considering the actual port range.

                long portLong = (long)Interlocked.Increment(ref s_counter) - int.MinValue;
                portLong = (portLong % PortRangeLength) + MinPort;
                int port = (int)portLong;

                if (s_usedPorts.TryAdd(port, 0))
                {
                    return new PortLease(port);
                }
            }

            throw new TestPortPoolExhaustedException();
        }

        public static void Return(PortLease portLease)
        {
            s_usedPorts.TryRemove(portLease.Port, out _);
        }

        public static PortLease RentPortAndBindSocket(Socket socket, IPAddress address)
        {
            PortLease lease = RentPort();
            try
            {
                socket.Bind(new IPEndPoint(address, lease.Port));
                return lease;
            }
            catch (SocketException)
            {
                lease.Dispose();
                throw;
            }
        }

        // Exclude ports which are reserved at initialization time
        private static ConcurrentDictionary<int, int> GetAllPortsUsedBySystem()
        {
            IPEndPoint ep4 = new IPEndPoint(IPAddress.Loopback, 0);
            IPEndPoint ep6 = new IPEndPoint(IPAddress.IPv6Loopback, 0);

            bool IsPortUsed(int port,
                AddressFamily addressFamily,
                SocketType socketType,
                ProtocolType protocolType)
            {
                try
                {
                    IPEndPoint ep = addressFamily == AddressFamily.InterNetwork ? ep4 : ep6;
                    ep.Port = port;
                    using Socket socket = new Socket(addressFamily, socketType, protocolType);
                    socket.Bind(ep);
                    return false;
                }
                catch (SocketException)
                {
                    return true;
                }
            }

            ConcurrentDictionary<int, int> result = new ConcurrentDictionary<int, int>();

            for (int port = MinPort; port < MaxPort; port++)
            {
                if (IsPortUsed(port, AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) ||
                    IsPortUsed(port, AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp) ||
                    IsPortUsed(port, AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp) ||
                    IsPortUsed(port, AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp))
                {
                    result.TryAdd(port, 0);
                }
            }

            return result;
        }
    }
}
