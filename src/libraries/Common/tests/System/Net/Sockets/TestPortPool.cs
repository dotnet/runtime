// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
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
    /// Distributes unique ports from the range defined by <see cref="Configuration.Sockets.TestPoolPortRange"/>
    /// Useful in socket testing scenarios, where port collisions across protocols are not acceptable.
    /// This kind of uniqueness is not guaranteed when binding to OS ephemeral ports, and might lead to issues on Unix.
    /// For more information see:
    /// https://github.com/dotnet/runtime/issues/19162#issuecomment-523195762
    /// </summary>
    internal static class TestPortPool
    {
        internal const int ThrowExhaustedAfter = 10;

        public static PortRange ConfiguredPortRange = new PortRange(
            System.Net.Test.Common.Configuration.Sockets.TestPoolPortRange.Min,
            System.Net.Test.Common.Configuration.Sockets.TestPoolPortRange.Max);


        private static readonly ConcurrentDictionary<int, int> s_usedPorts = GetAllPortsUsedBySystem();
        private static int s_counter = int.MinValue;

        public static PortLease RentPort()
        {
            for (int i = 0; i < ThrowExhaustedAfter; i++)
            {
                // Although race may occur theoretically because the following code block is not atomic,
                // it requires the s_counter to move at least PortRangeLength steps between Increment and TryAdd,
                // which is very unlikely considering the actual port range and the low number of tests utilizing TestPortPool
                long portLong = (long)Interlocked.Increment(ref s_counter) - int.MinValue;
                portLong = (portLong % ConfiguredPortRange.Length) + ConfiguredPortRange.Min;
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

        // Exclude ports which are unavailable at initialization time
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

            for (int port = ConfiguredPortRange.Min; port < ConfiguredPortRange.Max; port++)
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

    internal readonly struct PortRange
    {
        public int Min { get; }
        public int Max { get; }

        public int Length => Max - Min;

        public PortRange(int min, int max)
        {
            Min = min;
            Max = max;
        }

        public override string ToString() => $"({Min} .. {Max})";

        public static bool AreOverlappingRanges(in PortRange a, in PortRange b)
        {
            return a.Min < b.Min ? a.Max > b.Min : b.Max > a.Min;
        }

        public static PortRange GetDefaultOsDynamicPortRange()
        {
            if (PlatformDetection.IsWindows)
            {
                // For TestPortPool functionality, we need to take the intersection of 4 intervals:
                PortRange ipv4Tcp = ParseCmdletOutputWindows(RunCmldet("netsh", "int ipv4 show dynamicport tcp"));
                PortRange ipv4Udp = ParseCmdletOutputWindows(RunCmldet("netsh", "int ipv4 show dynamicport udp"));
                PortRange ipv6Tcp = ParseCmdletOutputWindows(RunCmldet("netsh", "int ipv6 show dynamicport tcp"));
                PortRange ipv6Udp = ParseCmdletOutputWindows(RunCmldet("netsh", "int ipv6 show dynamicport udp"));

                int min = Math.Max(ipv4Tcp.Min, Math.Max(ipv4Udp.Min, Math.Max(ipv6Tcp.Min, ipv6Udp.Min)));
                int max = Math.Min(ipv4Tcp.Max, Math.Min(ipv4Udp.Max, Math.Min(ipv6Tcp.Max, ipv6Udp.Max)));
                return new PortRange(min, max);
            }

            if (PlatformDetection.IsOSX)
            {
                return ParseCmdletOutputMac(RunCmldet("sysctl", "net.inet.ip.portrange.first net.inet.ip.portrange.last"));
            }

            return ParseCmdletOutputLinux(RunCmldet("cat", "/proc/sys/net/ipv4/ip_local_port_range"));
        }

        internal static PortRange ParseCmdletOutputWindows(string cmdOutput)
        {
            PortRange temp = ParseCmdletOutputMac(cmdOutput);

            // On Windows, second number is 'Number of Ports'
            return new PortRange(temp.Min, temp.Min + temp.Max);
        }

        internal static PortRange ParseCmdletOutputLinux(string cmdOutput)
        {
            ReadOnlySpan<char> span = cmdOutput.AsSpan();
            int firstSpace = span.IndexOf(' ');
            ReadOnlySpan<char> left = span.Slice(0, firstSpace).Trim();
            ReadOnlySpan<char> right = span.Slice(firstSpace).Trim();
            return new PortRange(int.Parse(left), int.Parse(right));
        }

        internal static PortRange ParseCmdletOutputMac(string cmdOutput)
        {
            int semicolon1 = cmdOutput.IndexOf(':', 0);
            int eol1 = cmdOutput.IndexOf('\n', semicolon1);
            int semicolon2 = cmdOutput.IndexOf(':', eol1);
            int eol2 = cmdOutput.IndexOf('\n', semicolon2);
            if (eol2 < 0) eol2 = cmdOutput.Length;

            int start = ParseImpl(cmdOutput, semicolon1 + 1, eol1);
            int end = ParseImpl(cmdOutput, semicolon2 + 1, eol2);
            return new PortRange(start, end);
        }

        private static string RunCmldet(string cmdlet, string args)
        {
            ProcessStartInfo psi = new ProcessStartInfo()
            {
                FileName = cmdlet,
                Arguments = args,
                RedirectStandardOutput = true
            };

            using Process process = Process.Start(psi);
            process.WaitForExit(10000);
            return process.StandardOutput.ReadToEnd();
        }

        private static int ParseImpl(string cmdOutput, int start, int end)
        {
            ReadOnlySpan<char> span = cmdOutput.AsSpan(start, end - start).Trim();
            return int.Parse(span);
        }
    }
}
