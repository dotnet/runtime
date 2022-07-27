// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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

        private struct Context
        {
            internal Dictionary<string, BsdNetworkInterface> _interfaces;
            internal List<Exception>? _exceptions;

            /// <summary>
            /// Gets or creates an BsdNetworkInterface, based on whether it already exists in the given Dictionary.
            /// If created, it is added to the Dictionary.
            /// </summary>
            /// <param name="pName">The name of the interface.</param>
            /// <param name="index">Interface index of the interface.</param>
            /// <returns>The cached or new BsdNetworkInterface with the given name.</returns>
            internal unsafe BsdNetworkInterface GetOrCreate(byte* pName, int index)
            {
                string name = new string((sbyte*)pName);

                BsdNetworkInterface? oni;
                if (!_interfaces.TryGetValue(name, out oni))
                {
                    oni = new BsdNetworkInterface(name, index);
                    _interfaces.Add(name, oni);
                }
                return oni;
            }

            internal void AddException(Exception e)
            {
                _exceptions ??= new List<Exception>();
                _exceptions.Add(e);
            }
        }

        [UnmanagedCallersOnly]
        private static unsafe void ProcessIpv4Address(void* pContext, byte* ifaceName, Interop.Sys.IpAddressInfo* ipAddr)
        {
            ref Context context = ref Unsafe.As<byte, Context>(ref *(byte*)pContext);
            try
            {
                context.GetOrCreate(ifaceName, ipAddr->InterfaceIndex).ProcessIpv4Address(ipAddr);
            }
            catch (Exception e)
            {
                context.AddException(e);
            }
        }

        [UnmanagedCallersOnly]
        private static unsafe void ProcessIpv6Address(void* pContext, byte* ifaceName, Interop.Sys.IpAddressInfo* ipAddr, uint* scopeId)
        {
            ref Context context = ref Unsafe.As<byte, Context>(ref *(byte*)pContext);
            try
            {
                context.GetOrCreate(ifaceName, ipAddr->InterfaceIndex).ProcessIpv6Address(ipAddr, *scopeId);
            }
            catch (Exception e)
            {
                context.AddException(e);
            }
        }

        [UnmanagedCallersOnly]
        private static unsafe void ProcessLinkLayerAddress(void* pContext, byte* ifaceName, Interop.Sys.LinkLayerAddressInfo* llAddr)
        {
            ref Context context = ref Unsafe.As<byte, Context>(ref *(byte*)pContext);
            try
            {
                context.GetOrCreate(ifaceName, llAddr->InterfaceIndex).ProcessLinkLayerAddress(llAddr);
            }
            catch (Exception e)
            {
                context.AddException(e);
            }
        }

        public static unsafe NetworkInterface[] GetBsdNetworkInterfaces()
        {
            const int MaxTries = 3;
            for (int attempt = 0; attempt < MaxTries; attempt++)
            {
                Context context;
                context._interfaces = new Dictionary<string, BsdNetworkInterface>();
                context._exceptions = null;

                // Because these callbacks are executed in a reverse-PInvoke, we do not want any exceptions
                // to propagate out, because they will not be catchable. Instead, we track all the exceptions
                // that are thrown in these callbacks, and aggregate them at the end.
                int result = Interop.Sys.EnumerateInterfaceAddresses(Unsafe.AsPointer(ref context), &ProcessIpv4Address, &ProcessIpv6Address, &ProcessLinkLayerAddress);
                if (context._exceptions != null)
                {
                    throw new NetworkInformationException(SR.net_PInvokeError, new AggregateException(context._exceptions));
                }
                if (result == 0)
                {
                    var results = new BsdNetworkInterface[context._interfaces.Count];
                    int i = 0;
                    foreach (KeyValuePair<string, BsdNetworkInterface> item in context._interfaces)
                    {
                        results[i++] = item.Value;
                    }
                    return results;
                }
            }

            throw new NetworkInformationException(SR.net_PInvokeError);
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
