// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace System.Net.NetworkInformation
{
    internal abstract class UnixIPGlobalProperties : IPGlobalProperties
    {
        public override string DhcpScopeName { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        public override string DomainName { get { return HostInformation.DomainName; } }

        public override string HostName { get { return HostInformation.HostName; } }

        public override bool IsWinsProxy { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        public override NetBiosNodeType NodeType { get { return NetBiosNodeType.Unknown; } }

        public override IAsyncResult BeginGetUnicastAddresses(AsyncCallback callback, object state)
        {
            Task<UnicastIPAddressInformationCollection> t = GetUnicastAddressesAsync();
            return TaskToApm.Begin(t, callback, state);
        }

        public override UnicastIPAddressInformationCollection EndGetUnicastAddresses(IAsyncResult asyncResult)
        {
            return TaskToApm.End<UnicastIPAddressInformationCollection>(asyncResult);
        }

        public sealed override Task<UnicastIPAddressInformationCollection> GetUnicastAddressesAsync()
        {
            return Task.Factory.StartNew(s => ((UnixIPGlobalProperties)s).GetUnicastAddresses(), this,
                CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        public unsafe override UnicastIPAddressInformationCollection GetUnicastAddresses()
        {
            UnicastIPAddressInformationCollection collection = new UnicastIPAddressInformationCollection();

            Interop.Sys.EnumerateInterfaceAddresses(
                (name, ipAddressInfo, netmaskInfo) =>
                {
                    IPAddress ipAddress = IPAddressUtil.GetIPAddressFromNativeInfo(ipAddressInfo);
                    if (!IPAddressUtil.IsMulticast(ipAddress))
                    {
                        IPAddress netMaskAddress = IPAddressUtil.GetIPAddressFromNativeInfo(netmaskInfo);
                        collection.InternalAdd(new UnixUnicastIPAddressInformation(ipAddress, netMaskAddress));
                    }
                },
                (name, ipAddressInfo, scopeId) =>
                {
                    IPAddress ipAddress = IPAddressUtil.GetIPAddressFromNativeInfo(ipAddressInfo);
                    if (!IPAddressUtil.IsMulticast(ipAddress))
                    {
                        collection.InternalAdd(new UnixUnicastIPAddressInformation(ipAddress, IPAddress.Any));
                    }
                },
                // Ignore link-layer addresses that are discovered; don't create a callback.
                null);

            return collection;
        }
    }
}
