// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Net.NetworkInformation
{
    internal sealed class BsdNetworkInterface : UnixNetworkInterface
    {
        private readonly BsdIpInterfaceProperties _ipProperties;
        private readonly OperationalStatus _operationalStatus;
        private readonly bool _supportsMulticast;
        private readonly long _speed;

        private unsafe BsdNetworkInterface(string name, int index) : base(name)
        {
            _index = index;
            Interop.Sys.NativeIPInterfaceStatistics nativeStats;
            if (Interop.Sys.GetNativeIPInterfaceStatistics(name, out nativeStats) == -1)
            {
                throw new NetworkInformationException(SR.net_PInvokeError);
            }

            if ((nativeStats.Flags & (ulong)Interop.Sys.InterfaceFlags.InterfaceError) != 0)
            {
                _operationalStatus = OperationalStatus.Unknown;
            }
            else
            {
                _operationalStatus = (nativeStats.Flags & (ulong)Interop.Sys.InterfaceFlags.InterfaceHasLink) != 0 ?  OperationalStatus.Up : OperationalStatus.Down;
            }

            _supportsMulticast = (nativeStats.Flags & (ulong)Interop.Sys.InterfaceFlags.InterfaceSupportsMulticast) != 0;
            _speed = (long)nativeStats.Speed;
            _ipProperties = new BsdIpInterfaceProperties(this, (int)nativeStats.Mtu);
        }

        public static unsafe NetworkInterface[] GetBsdNetworkInterfaces()
        {
            Dictionary<string, BsdNetworkInterface> interfacesByName = new Dictionary<string, BsdNetworkInterface>();
            List<Exception>? exceptions = null;
            const int MaxTries = 3;
            for (int attempt = 0; attempt < MaxTries; attempt++)
            {
                // Because these callbacks are executed in a reverse-PInvoke, we do not want any exceptions
                // to propagate out, because they will not be catchable. Instead, we track all the exceptions
                // that are thrown in these callbacks, and aggregate them at the end.
                int result = Interop.Sys.EnumerateInterfaceAddresses(
                    (name, ipAddr) =>
                    {
                        try
                        {
                            BsdNetworkInterface oni = GetOrCreate(interfacesByName, name, ipAddr->InterfaceIndex);
                            oni.ProcessIpv4Address(ipAddr);
                        }
                        catch (Exception e)
                        {
                            if (exceptions == null)
                            {
                                exceptions = new List<Exception>();
                            }
                            exceptions.Add(e);
                        }
                    },
                    (name, ipAddr, scopeId) =>
                    {
                        try
                        {
                            BsdNetworkInterface oni = GetOrCreate(interfacesByName, name, ipAddr->InterfaceIndex);
                            oni.ProcessIpv6Address(ipAddr, *scopeId);
                        }
                        catch (Exception e)
                        {
                            if (exceptions == null)
                            {
                                exceptions = new List<Exception>();
                            }
                            exceptions.Add(e);
                        }
                    },
                    (name, llAddr) =>
                    {
                        try
                        {
                            BsdNetworkInterface oni = GetOrCreate(interfacesByName, name, llAddr->InterfaceIndex);
                            oni.ProcessLinkLayerAddress(llAddr);
                        }
                        catch (Exception e)
                        {
                            if (exceptions == null)
                            {
                                exceptions = new List<Exception>();
                            }
                            exceptions.Add(e);
                        }
                    });
                if (exceptions != null)
                {
                    throw new NetworkInformationException(SR.net_PInvokeError, new AggregateException(exceptions));
                }
                else if (result == 0)
                {
                    var results = new BsdNetworkInterface[interfacesByName.Count];
                    int i = 0;
                    foreach (KeyValuePair<string, BsdNetworkInterface> item in interfacesByName)
                    {
                        results[i++] = item.Value;
                    }
                    return results;
                }
                else
                {
                    interfacesByName.Clear();
                }
            }

            throw new NetworkInformationException(SR.net_PInvokeError);
        }

        /// <summary>
        /// Gets or creates an BsdNetworkInterface, based on whether it already exists in the given Dictionary.
        /// If created, it is added to the Dictionary.
        /// </summary>
        /// <param name="interfaces">The Dictionary of existing interfaces.</param>
        /// <param name="name">The name of the interface.</param>
        /// <param name="index">Interface index of the interface.</param>
        /// <returns>The cached or new BsdNetworkInterface with the given name.</returns>
        private static BsdNetworkInterface GetOrCreate(Dictionary<string, BsdNetworkInterface> interfaces, string name, int index)
        {
            BsdNetworkInterface? oni;
            if (!interfaces.TryGetValue(name, out oni))
            {
                oni = new BsdNetworkInterface(name, index);
                interfaces.Add(name, oni);
            }

            return oni;
        }

        public override IPInterfaceProperties GetIPProperties()
        {
            return _ipProperties;
        }

        public override IPInterfaceStatistics GetIPStatistics()
        {
            return new BsdIpInterfaceStatistics(Name);
        }

        public override IPv4InterfaceStatistics GetIPv4Statistics()
        {
            return new BsdIPv4InterfaceStatistics(Name);
        }

        public override OperationalStatus OperationalStatus { get { return _operationalStatus; } }

        public override long Speed { get { return _speed; } }

        public override bool SupportsMulticast { get { return _supportsMulticast; } }

        public override bool IsReceiveOnly { get { return false; } }
    }
}
