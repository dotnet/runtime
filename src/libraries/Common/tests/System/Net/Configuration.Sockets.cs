// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

namespace System.Net.Test.Common
{
    public static partial class Configuration
    {
        public static partial class Sockets
        {
            public static Uri SocketServer => GetUriValue("DOTNET_TEST_NET_SOCKETS_SERVERURI", new Uri("http://" + DefaultAzureServer));

            public static string InvalidHost => GetValue("DOTNET_TEST_NET_SOCKETS_INVALIDSERVER", "notahostname.invalid.corp.microsoft.com");

            public static IPAddress? LinkLocalAddress => GetIPv6LinkLocalAddress();

            public static IEnumerable<object[]> LocalAddresses()
            {
                if (LinkLocalAddress != null)
                {
                    yield return new[] { LinkLocalAddress };
                }
                if (Socket.OSSupportsIPv4)
                {
                    yield return new[] { IPAddress.Loopback };
                }
                if (Socket.OSSupportsIPv6 && IsIPv6LoopbackAvailable)
                {
                    yield return new[] { IPAddress.IPv6Loopback };
                }
            }

            private static IPAddress GetIPv6LinkLocalAddress() =>
                NetworkInterface
                    .GetAllNetworkInterfaces()
                    .Where(i => !i.Description.StartsWith("PANGP Virtual Ethernet"))    // This is a VPN adapter, but is reported as a regular Ethernet interface with
                                                                                        // a valid link-local address, but the link-local address doesn't actually work.
                                                                                        // So just manually filter it out.
                    .Where(i => !i.Name.Contains("Tailscale"))                          // Same as PANGP above.
                    .SelectMany(i => i.GetIPProperties().UnicastAddresses)
                    .Select(a => a.Address)
                    .Where(a => a.IsIPv6LinkLocal)
                    .FirstOrDefault();

            private static readonly Lazy<bool> _isIPv6LoopbackAvailable = new Lazy<bool>(GetIsIPv6LoopbackAvailable);
            public static bool IsIPv6LoopbackAvailable => _isIPv6LoopbackAvailable.Value;

            private static bool GetIsIPv6LoopbackAvailable()
            {
                try
                {
                    using Socket s = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
                    s.Bind(new IPEndPoint(IPAddress.IPv6Loopback, 0));
                    return true;
                }
                catch (SocketException)
                {
                    return false;
                }
            }
        }
    }
}
