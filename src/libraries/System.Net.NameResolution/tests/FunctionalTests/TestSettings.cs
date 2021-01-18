// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.NameResolution.Tests
{
    [CollectionDefinition("NoParallelTests", DisableParallelization = true)]
    public partial class NoParallelTests { }

    internal static class TestSettings
    {
        // A hostname that will not exist in any DNS caches, forcing some I/O to lookup.
        public static string UncachedHost => $"nonexistent-{Guid.NewGuid():N}.contoso.com";

        public const string IPv4Host = LocalHost;

        // Linux CI servers not configured with IPv6 localhost.
        // Windows CI servers not configured with IPv6 name resolution.
        public static readonly string IPv6Host = PlatformDetection.IsWindows ? LocalHost : "ipv6.google.com";

        public const string LocalHost = "localhost";

        public const string LocalIPString = "127.0.0.1";

        // Timeout values in milliseconds.
        public const int PassingTestTimeout = 30_000;

        public static Task<IPAddress> GetLocalIPAddress() =>
            ResolveHost(TestSettings.LocalHost, TestSettings.AddressFamily);

        public static AddressFamily AddressFamily =>
            AddressFamily.InterNetwork;

        public static Task WhenAllOrAnyFailedWithTimeout(params Task[] tasks) => tasks.WhenAllOrAnyFailed(PassingTestTimeout);

        private static async Task<IPAddress> ResolveHost(string host, AddressFamily family)
        {
            IPHostEntry hostEntry = await Dns.GetHostEntryAsync(host);

            foreach (IPAddress address in hostEntry.AddressList)
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
