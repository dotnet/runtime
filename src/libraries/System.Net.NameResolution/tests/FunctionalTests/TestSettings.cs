// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace System.Net.NameResolution.Tests
{
    internal static class TestSettings
    {
        public const string LocalHost = "localhost";

        public const string LocalIPString = "127.0.0.1";

        // Timeout values in milliseconds.
        public const int PassingTestTimeout = 30_000;

        public static Task<IPAddress> GetLocalIPAddress()
        {
            return ResolveHost(TestSettings.LocalHost, TestSettings.AddressFamily);
        }

        public static AddressFamily AddressFamily
        {
            get
            {
                // *nix machines are not always configured to resolve localhost to an IPv6 address.
                return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                    AddressFamily.InterNetworkV6 :
                    AddressFamily.InterNetwork;
            }
        }

        public static Task WhenAllOrAnyFailedWithTimeout(params Task[] tasks) => tasks.WhenAllOrAnyFailed(PassingTestTimeout);

        private static async Task<IPAddress> ResolveHost(string host, AddressFamily family)
        {
            var hostEntry = await Dns.GetHostEntryAsync(host);

            foreach (var address in hostEntry.AddressList)
            {
                if (address.AddressFamily == family)
                {
                    return address;
                }
            }
            return null;
        }
    }
}

