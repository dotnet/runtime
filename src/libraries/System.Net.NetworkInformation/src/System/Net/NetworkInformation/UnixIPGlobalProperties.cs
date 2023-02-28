// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable 8500 // taking address of managed types

namespace System.Net.NetworkInformation
{
    internal abstract class UnixIPGlobalProperties : IPGlobalProperties
    {
        [UnsupportedOSPlatform("linux")]
        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("osx")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("freebsd")]
        [UnsupportedOSPlatform("illumos")]
        [UnsupportedOSPlatform("solaris")]
        public override string DhcpScopeName { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        public override string DomainName { get { return HostInformation.DomainName; } }

        public override string HostName { get { return HostInformation.HostName; } }

        [UnsupportedOSPlatform("linux")]
        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("osx")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("freebsd")]
        [UnsupportedOSPlatform("illumos")]
        [UnsupportedOSPlatform("solaris")]
        public override bool IsWinsProxy { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        public override NetBiosNodeType NodeType { get { return NetBiosNodeType.Unknown; } }

        public override IAsyncResult BeginGetUnicastAddresses(AsyncCallback? callback, object? state)
        {
            Task<UnicastIPAddressInformationCollection> t = GetUnicastAddressesAsync();
            return TaskToAsyncResult.Begin(t, callback, state);
        }

        public override UnicastIPAddressInformationCollection EndGetUnicastAddresses(IAsyncResult asyncResult)
        {
            return TaskToAsyncResult.End<UnicastIPAddressInformationCollection>(asyncResult);
        }

        public sealed override Task<UnicastIPAddressInformationCollection> GetUnicastAddressesAsync()
        {
            return Task.Factory.StartNew(s => ((UnixIPGlobalProperties)s!).GetUnicastAddresses(), this,
                CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        private struct Context
        {
            internal UnicastIPAddressInformationCollection _collection;
            internal List<Exception>? _exceptions;

            internal void AddException(Exception e)
            {
                _exceptions ??= new List<Exception>();
                _exceptions.Add(e);
            }
        }

        [UnmanagedCallersOnly]
        private static unsafe void ProcessIpv4Address(void* pContext, byte* ifaceName, Interop.Sys.IpAddressInfo* ipAddr)
        {
            Context* context = (Context*)pContext;
            try
            {
                IPAddress ipAddress = IPAddressUtil.GetIPAddressFromNativeInfo(ipAddr);
                if (!IPAddressUtil.IsMulticast(ipAddress))
                {
                    context->_collection.InternalAdd(new UnixUnicastIPAddressInformation(ipAddress, ipAddr->PrefixLength));
                }
            }
            catch (Exception e)
            {
                context->AddException(e);
            }
        }

        [UnmanagedCallersOnly]
        private static unsafe void ProcessIpv6Address(void* pContext, byte* ifaceName, Interop.Sys.IpAddressInfo* ipAddr, uint* scopeId)
        {
            Context* context = (Context*)pContext;
            try
            {
                IPAddress ipAddress = IPAddressUtil.GetIPAddressFromNativeInfo(ipAddr);
                if (!IPAddressUtil.IsMulticast(ipAddress))
                {
                    context->_collection.InternalAdd(new UnixUnicastIPAddressInformation(ipAddress, ipAddr->PrefixLength));
                }
            }
            catch (Exception e)
            {
                context->AddException(e);
            }
        }

        public override unsafe UnicastIPAddressInformationCollection GetUnicastAddresses()
        {
            Context context;
            context._collection = new UnicastIPAddressInformationCollection();
            context._exceptions = null;

            // Ignore link-layer addresses that are discovered; don't create a callback.
            Interop.Sys.EnumerateInterfaceAddresses(&context, &ProcessIpv4Address, &ProcessIpv6Address, null);

            if (context._exceptions != null)
                throw new NetworkInformationException(SR.net_PInvokeError, new AggregateException(context._exceptions));

            return context._collection;
        }
    }
}
