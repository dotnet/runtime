// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Test.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Stress.Data.SqlClient
{
    internal class MultiSubnetFailoverSetup
    {
        private HostsFileManager _hostsFile;

        internal MultiSubnetFailoverSetup(SqlServerDataSource source)
        {
            this.Source = source;
        }

        internal string MultiSubnetFailoverHostNameForIntegratedSecurity { get; private set; }

        private List<string> _multiSubnetFailoverHostNames;

        internal string GetMultiSubnetFailoverHostName(Random rnd)
        {
            return _multiSubnetFailoverHostNames[rnd.Next(_multiSubnetFailoverHostNames.Count)];
        }

        public SqlServerDataSource Source { get; private set; }

        internal void InitializeFakeHostsForMultiSubnetFailover()
        {
            // initialize fake hosts for MultiSubnetFailover
            string originalHost, protocol, instance;
            int? port;
            NetUtils.ParseDataSource(this.Source.DataSource, out protocol, out originalHost, out instance, out port);

            // get the IPv4 addresses
            IPAddress[] ipV4 = NetUtils.EnumerateIPv4Addresses(originalHost).ToArray();
            if (ipV4 == null || ipV4.Length == 0)
            {
                // consider supporting IPv6 when it becomes relevant (not a goal right now)
                throw new ArgumentException("The target server " + originalHost + " has no IPv4 addresses associated with it in DNS");
            }

            // construct different host names for MSF with valid server IP located in a different place each time
            List<HostsFileManager.HostEntry> allEntries = new List<HostsFileManager.HostEntry>();

            int nextValidIp = 0;
            int nextInvalidIp = 0;
            _multiSubnetFailoverHostNames = new List<string>();

            // construct some interesting cases for MultiSubnetFailover stress

            // for integrated security to work properly, the server name in connection string must match the target server host name.
            // thus, create one entry in the hosts with the true server name: either FQDN or the short name
            Task<IPHostEntry> task = Dns.GetHostEntryAsync(ipV4[0]);
            string nameToUse = task.Result.HostName;
            if (originalHost.Contains('.'))
            {
                // if the original hosts is FQDN, put short name in the hosts instead
                // otherwise, put FQDN in hosts
                int shortNameEnd = nameToUse.IndexOf('.');
                if (shortNameEnd > 0)
                    nameToUse = nameToUse.Substring(0, shortNameEnd);
            }
            // since true server name is being re-mapped, keep the valid IP first in the list
            AddEntryHelper(allEntries, _multiSubnetFailoverHostNames, nameToUse,
                ipV4[(nextValidIp++) % ipV4.Length],
                NetUtils.GetNonExistingIPv4((nextInvalidIp++) % NetUtils.NonExistingIPv4AddressCount));
            this.MultiSubnetFailoverHostNameForIntegratedSecurity = nameToUse;

            // single valid IP
            AddEntryHelper(allEntries, _multiSubnetFailoverHostNames, "MSF_MP_Stress_V",
                ipV4[(nextValidIp++) % ipV4.Length]);

            // valid + invalid
            AddEntryHelper(
                allEntries, _multiSubnetFailoverHostNames, "MSF_MP_Stress_VI",
                ipV4[(nextValidIp++) % ipV4.Length],
                NetUtils.GetNonExistingIPv4((nextInvalidIp++) % NetUtils.NonExistingIPv4AddressCount));

            // invalid + valid + invalid
            AddEntryHelper(
                allEntries, _multiSubnetFailoverHostNames, "MSF_MP_Stress_IVI",
                NetUtils.GetNonExistingIPv4((nextInvalidIp++) % NetUtils.NonExistingIPv4AddressCount),
                ipV4[(nextValidIp++) % ipV4.Length],
                NetUtils.GetNonExistingIPv4((nextInvalidIp++) % NetUtils.NonExistingIPv4AddressCount));

            // Using more than one active IP associated with the virtual name (VNN) is not a supported scenario with MultiSubnetFailover.
            // But, this can definitly happen in reality - add special cases here to cover two valid IPs.
            AddEntryHelper(
                allEntries, _multiSubnetFailoverHostNames, "MSF_MP_Stress_IVI",
                ipV4[(nextValidIp++) % ipV4.Length],
                NetUtils.GetNonExistingIPv4((nextInvalidIp++) % NetUtils.NonExistingIPv4AddressCount),
                ipV4[(nextValidIp++) % ipV4.Length]);

            // big boom with 7 IPs - for stress purposes only
            AddEntryHelper(
                allEntries, _multiSubnetFailoverHostNames, "MSF_MP_Stress_BIGBOOM",
                NetUtils.GetNonExistingIPv4((nextInvalidIp++) % NetUtils.NonExistingIPv4AddressCount),
                NetUtils.GetNonExistingIPv4((nextInvalidIp++) % NetUtils.NonExistingIPv4AddressCount),
                NetUtils.GetNonExistingIPv4((nextInvalidIp++) % NetUtils.NonExistingIPv4AddressCount),
                ipV4[(nextValidIp++) % ipV4.Length],
                NetUtils.GetNonExistingIPv4((nextInvalidIp++) % NetUtils.NonExistingIPv4AddressCount),
                ipV4[(nextValidIp++) % ipV4.Length],
                NetUtils.GetNonExistingIPv4((nextInvalidIp++) % NetUtils.NonExistingIPv4AddressCount)
                );

            // list of fake hosts is ready, initialize hosts file manager and update the file
            _hostsFile = new HostsFileManager();
            _hostsFile.AddRange(allEntries);
        }


        private static void AddEntryHelper(List<HostsFileManager.HostEntry> entries, List<string> names, string msfHostName, params IPAddress[] addresses)
        {
            for (int i = 0; i < addresses.Length; i++)
                entries.Add(new HostsFileManager.HostEntry(msfHostName, addresses[i]));
            names.Add(msfHostName);
        }

        internal void Terminate()
        {
            // revert hosts file
            if (_hostsFile != null)
            {
                _hostsFile.Dispose();
            }
        }
    }
}
