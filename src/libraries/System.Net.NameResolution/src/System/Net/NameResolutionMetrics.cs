// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net.Sockets;

namespace System.Net
{
    internal static class NameResolutionMetrics
    {
        private static readonly Meter s_meter = new("System.Net.NameResolution");

        private static readonly Counter<long> s_lookupsRequestedCounter = s_meter.CreateCounter<long>(
            name: "dns-lookups-requested",
            description: "Number of DNS lookups requested.");

        public static bool IsEnabled() => s_lookupsRequestedCounter.Enabled;

        public static void BeforeResolution(object hostNameOrAddress, out string? host)
        {
            if (s_lookupsRequestedCounter.Enabled)
            {
                host = GetHostnameFromStateObject(hostNameOrAddress);

                s_lookupsRequestedCounter.Add(1, KeyValuePair.Create("hostname", (object?)host));
            }
            else
            {
                host = null;
            }
        }

        public static string GetHostnameFromStateObject(object hostNameOrAddress)
        {
            Debug.Assert(hostNameOrAddress is not null);

            string host = hostNameOrAddress switch
            {
                string h => h,
                KeyValuePair<string, AddressFamily> t => t.Key,
                IPAddress a => a.ToString(),
                KeyValuePair<IPAddress, AddressFamily> t => t.Key.ToString(),
                _ => null!
            };

            Debug.Assert(host is not null, $"Unknown hostNameOrAddress type: {hostNameOrAddress.GetType().Name}");

            return host;
        }
    }
}
