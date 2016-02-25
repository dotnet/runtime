// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using Windows.Networking;

namespace System.Net.NetworkInformation
{
    internal static class HostInformationPal
    {
        // Changing this information requires a reboot, so it's safe to cache.
        private static Interop.IpHlpApi.FIXED_INFO s_fixedInfo;
        private static bool s_fixedInfoInitialized;
        private static object s_syncObject = new object();

        public static string GetHostName()
        {
            EnsureFixedInfo();
            return s_fixedInfo.hostName;
        }

        public static string GetDomainName()
        {
            EnsureFixedInfo();
            return s_fixedInfo.domainName;
        }

        // TODO: #2485: Temporarily made GetFixedInfo() public to make things build.
        // This function needs to be switched back to private since it has no correspondent in the Unix world.
        public static Interop.IpHlpApi.FIXED_INFO GetFixedInfo()
        {
            Interop.IpHlpApi.FIXED_INFO fixedInfo = new Interop.IpHlpApi.FIXED_INFO();

            IReadOnlyList<HostName> hostNamesList = global::Windows.Networking.Connectivity.NetworkInformation.GetHostNames();

            foreach (HostName entry in hostNamesList)
            {
                // The first DomainName entry in the GetHostNames() list
                // is the fdqn of the machine itself.
                if (entry.Type == HostNameType.DomainName)
                {
                    string host = entry.ToString();
                    int dot = host.IndexOf('.');
                    if (dot != -1)
                    {
                        // The machine is domain joined.
                        fixedInfo.hostName = host.Substring(0, dot);
                        fixedInfo.domainName = host.Substring(dot+1);
                    }
                    else
                    {
                        // The machine is not domain joined.
                        fixedInfo.hostName = host;
                        fixedInfo.domainName = string.Empty;
                    }
                    
                    break;
                }
            }

            return fixedInfo;
        }

        private static void EnsureFixedInfo()
        {
            if (!Volatile.Read(ref s_fixedInfoInitialized))
            {
                lock (s_syncObject)
                {
                    if (!s_fixedInfoInitialized)
                    {
                        s_fixedInfo = GetFixedInfo();
                        Volatile.Write(ref s_fixedInfoInitialized, true);
                    }
                }
            }
        }
    }
}
