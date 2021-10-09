// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.NetworkInformation;
using System.Runtime.Serialization;
using System.Threading;

namespace System.Net
{
    public partial class WebProxy : IWebProxy, ISerializable
    {
        private static volatile string? s_domainName;
        private static volatile IPAddress[]? s_localAddresses;
        private static int s_networkChangeRegistered;

        private bool IsLocal(Uri host)
        {
            if (host.IsLoopback)
            {
                return true;
            }

            string hostString = host.Host;

            if (IPAddress.TryParse(hostString, out IPAddress? hostAddress))
            {
                EnsureNetworkChangeRegistration();
                IPAddress[] localAddresses = s_localAddresses ??= Dns.GetHostEntry(Dns.GetHostName()).AddressList;
                return Array.IndexOf(localAddresses, hostAddress) != -1;
            }

            // No dot?  Local.
            int dot = hostString.IndexOf('.');
            if (dot == -1)
            {
                return true;
            }

            // If it matches the primary domain, it's local (whether or not the hostname matches).
            EnsureNetworkChangeRegistration();
            string local = s_domainName ??= "." + IPGlobalProperties.GetIPGlobalProperties().DomainName;
            return
                local.Length == (hostString.Length - dot) &&
                string.Compare(local, 0, hostString, dot, local.Length, StringComparison.OrdinalIgnoreCase) == 0;
        }

        /// <summary>Ensures we've registered with NetworkChange to clear out statically-cached state upon a network change notification.</summary>
        private static void EnsureNetworkChangeRegistration()
        {
            if (s_networkChangeRegistered == 0)
            {
                Register();

                static void Register()
                {
                    if (Interlocked.Exchange(ref s_networkChangeRegistered, 1) != 0)
                    {
                        return;
                    }

                    // Clear out cached state when we get notification of a network-related change.
                    NetworkChange.NetworkAddressChanged += (s, e) =>
                    {
                        s_domainName = null;
                        s_localAddresses = null;
                    };
                    NetworkChange.NetworkAvailabilityChanged += (s, e) =>
                    {
                        s_domainName = null;
                        s_localAddresses = null;
                    };
                }
            }
        }
    }
}
