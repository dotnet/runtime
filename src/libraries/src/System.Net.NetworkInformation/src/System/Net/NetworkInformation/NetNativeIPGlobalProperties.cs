// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

namespace System.Net.NetworkInformation
{
    internal class NetNativeIPGlobalProperties : IPGlobalProperties
    {
        public override string DhcpScopeName 
        { 
            get
            {
                throw new PlatformNotSupportedException();
            } 
        }

        public override string DomainName
        { 
            get
            {
                throw new PlatformNotSupportedException();
            } 
        }
        
        public override string HostName
        { 
            get
            {
                throw new PlatformNotSupportedException();
            } 
        }
        
        public override bool IsWinsProxy
        { 
            get
            {
                throw new PlatformNotSupportedException();
            } 
        }
        
        public override NetBiosNodeType NodeType
        {
            get
            {
                throw new PlatformNotSupportedException();
            } 
        }

        public override TcpConnectionInformation[] GetActiveTcpConnections()
        {
            throw new PlatformNotSupportedException();
        } 

        public override IPEndPoint[] GetActiveTcpListeners()
        {
            throw new PlatformNotSupportedException();
        } 
        
        public override IPEndPoint[] GetActiveUdpListeners()
        {
            throw new PlatformNotSupportedException();
        } 

        public override IcmpV4Statistics GetIcmpV4Statistics()
        {
            throw new PlatformNotSupportedException();
        } 

        public override IcmpV6Statistics GetIcmpV6Statistics()
        {
            throw new PlatformNotSupportedException();
        } 

        public static new IPGlobalProperties GetIPGlobalProperties()
        {
            throw new PlatformNotSupportedException();
        }

        public override IPGlobalStatistics GetIPv4GlobalStatistics()
        {
            throw new PlatformNotSupportedException();
        }

        public override IPGlobalStatistics GetIPv6GlobalStatistics()
        {
            throw new PlatformNotSupportedException();
        }        

        public override TcpStatistics GetTcpIPv4Statistics()
        {
            throw new PlatformNotSupportedException();
        }        

        public override TcpStatistics GetTcpIPv6Statistics()
        {
            throw new PlatformNotSupportedException();
        }        

        public override UdpStatistics GetUdpIPv4Statistics()
        {
            throw new PlatformNotSupportedException();
        }        

        public override UdpStatistics GetUdpIPv6Statistics()
        {
            throw new PlatformNotSupportedException();
        }

        public override IAsyncResult BeginGetUnicastAddresses(AsyncCallback callback, object state)
        {
            throw new PlatformNotSupportedException();
        }

        public override UnicastIPAddressInformationCollection EndGetUnicastAddresses(IAsyncResult asyncResult)
        {
            throw new PlatformNotSupportedException();
        }

        public override UnicastIPAddressInformationCollection GetUnicastAddresses()
        {
            throw new PlatformNotSupportedException();
        }

        public override Task<UnicastIPAddressInformationCollection> GetUnicastAddressesAsync()
        {
            throw new PlatformNotSupportedException();
        }
    }
}
