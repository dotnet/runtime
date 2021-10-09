// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.Net.NetworkInformation
{
    public abstract class NetworkInterface
    {
        /// <summary>
        /// Returns objects that describe the network interfaces on the local computer.
        /// </summary>
        /// <returns>An array of all network interfaces on the local computer.</returns>
        [UnsupportedOSPlatform("illumos")]
        [UnsupportedOSPlatform("solaris")]
        public static NetworkInterface[] GetAllNetworkInterfaces()
        {
            return NetworkInterfacePal.GetAllNetworkInterfaces();
        }

        [UnsupportedOSPlatform("illumos")]
        [UnsupportedOSPlatform("solaris")]
        public static bool GetIsNetworkAvailable()
        {
            return NetworkInterfacePal.GetIsNetworkAvailable();
        }

        [UnsupportedOSPlatform("illumos")]
        [UnsupportedOSPlatform("solaris")]
        public static int IPv6LoopbackInterfaceIndex
        {
            get
            {
                return NetworkInterfacePal.IPv6LoopbackInterfaceIndex;
            }
        }

        [UnsupportedOSPlatform("illumos")]
        [UnsupportedOSPlatform("solaris")]
        public static int LoopbackInterfaceIndex
        {
            get
            {
                return NetworkInterfacePal.LoopbackInterfaceIndex;
            }
        }

        public virtual string Id { get { throw NotImplemented.ByDesignWithMessage(SR.net_PropertyNotImplementedException); } }

        /// <summary>
        /// Gets the name of the network interface.
        /// </summary>
        public virtual string Name { get { throw NotImplemented.ByDesignWithMessage(SR.net_PropertyNotImplementedException); } }

        /// <summary>
        /// Gets the description of the network interface
        /// </summary>
        public virtual string Description { get { throw NotImplemented.ByDesignWithMessage(SR.net_PropertyNotImplementedException); } }

        /// <summary>
        /// Gets the IP properties for this network interface.
        /// </summary>
        /// <returns>The interface's IP properties.</returns>
        public virtual IPInterfaceProperties GetIPProperties()
        {
            throw NotImplemented.ByDesignWithMessage(SR.net_MethodNotImplementedException);
        }

        /// <summary>
        /// Provides Internet Protocol (IP) statistical data for this network interface.
        /// </summary>
        /// <returns>The interface's IP statistics.</returns>
        public virtual IPInterfaceStatistics GetIPStatistics()
        {
            throw NotImplemented.ByDesignWithMessage(SR.net_MethodNotImplementedException);
        }

        /// <summary>
        /// Provides Internet Protocol (IP) statistical data for this network interface.
        /// Despite the naming, the results are not IPv4 specific.
        /// Do not use this method, use GetIPStatistics instead.
        /// </summary>
        /// <returns>The interface's IP statistics.</returns>
        public virtual IPv4InterfaceStatistics GetIPv4Statistics()
        {
            throw NotImplemented.ByDesignWithMessage(SR.net_MethodNotImplementedException);
        }

        /// <summary>
        /// Gets the current operational state of the network connection.
        /// </summary>
        public virtual OperationalStatus OperationalStatus { get { throw NotImplemented.ByDesignWithMessage(SR.net_PropertyNotImplementedException); } }

        /// <summary>
        /// Gets the speed of the interface in bits per second as reported by the interface.
        /// </summary>
        public virtual long Speed { get { throw NotImplemented.ByDesignWithMessage(SR.net_PropertyNotImplementedException); } }

        /// <summary>
        /// Gets a bool value that indicates whether the network interface is set to only receive data packets.
        /// </summary>
        public virtual bool IsReceiveOnly { get { throw NotImplemented.ByDesignWithMessage(SR.net_PropertyNotImplementedException); } }

        /// <summary>
        /// Gets a bool value that indicates whether this network interface is enabled to receive multicast packets.
        /// </summary>
        public virtual bool SupportsMulticast { get { throw NotImplemented.ByDesignWithMessage(SR.net_PropertyNotImplementedException); } }

        /// <summary>
        /// Gets the physical address of this network interface
        /// </summary>
        /// <returns>The interface's physical address.</returns>
        public virtual PhysicalAddress GetPhysicalAddress()
        {
            throw NotImplemented.ByDesignWithMessage(SR.net_MethodNotImplementedException);
        }

        /// <summary>
        /// Gets the interface type.
        /// </summary>
        public virtual NetworkInterfaceType NetworkInterfaceType { get { throw NotImplemented.ByDesignWithMessage(SR.net_PropertyNotImplementedException); } }

        public virtual bool Supports(NetworkInterfaceComponent networkInterfaceComponent)
        {
            throw NotImplemented.ByDesignWithMessage(SR.net_MethodNotImplementedException);
        }
    }
}
