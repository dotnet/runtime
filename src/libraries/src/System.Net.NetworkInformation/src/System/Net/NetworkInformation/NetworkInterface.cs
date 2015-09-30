// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net.NetworkInformation
{
    public abstract class NetworkInterface
    {
        /// Returns objects that describe the network interfaces on the local computer.
        public static NetworkInterface[] GetAllNetworkInterfaces()
        {
            return SystemNetworkInterface.GetNetworkInterfaces();
        }

        public static bool GetIsNetworkAvailable()
        {
            return SystemNetworkInterface.InternalGetIsNetworkAvailable();
        }

        public static int LoopbackInterfaceIndex
        {
            get
            {
                return SystemNetworkInterface.InternalLoopbackInterfaceIndex;
            }
        }

        public static int IPv6LoopbackInterfaceIndex
        {
            get
            {
                return SystemNetworkInterface.InternalIPv6LoopbackInterfaceIndex;
            }
        }

        public virtual string Id { get { throw NotImplemented.ByDesignWithMessage(SR.net_PropertyNotImplementedException); } }

        /// Gets the name of the network interface.
        public virtual string Name { get { throw NotImplemented.ByDesignWithMessage(SR.net_PropertyNotImplementedException); } }

        /// Gets the description of the network interface
        public virtual string Description { get { throw NotImplemented.ByDesignWithMessage(SR.net_PropertyNotImplementedException); } }

        /// Gets the IP properties for this network interface.
        public virtual IPInterfaceProperties GetIPProperties()
        {
            throw NotImplemented.ByDesignWithMessage(SR.net_MethodNotImplementedException);
        }

        /// Provides Internet Protocol (IP) statistical data for this network interface.
        public virtual IPInterfaceStatistics GetIPStatistics()
        {
            throw NotImplemented.ByDesignWithMessage(SR.net_MethodNotImplementedException);
        }

        /// Gets the current operational state of the network connection.
        public virtual OperationalStatus OperationalStatus { get { throw NotImplemented.ByDesignWithMessage(SR.net_PropertyNotImplementedException); } }

        /// Gets the speed of the interface in bits per second as reported by the interface.
        public virtual long Speed { get { throw NotImplemented.ByDesignWithMessage(SR.net_PropertyNotImplementedException); } }

        /// Gets a bool value that indicates whether the network interface is set to only receive data packets.
        public virtual bool IsReceiveOnly { get { throw NotImplemented.ByDesignWithMessage(SR.net_PropertyNotImplementedException); } }

        /// Gets a bool value that indicates whether this network interface is enabled to receive multicast packets.
        public virtual bool SupportsMulticast { get { throw NotImplemented.ByDesignWithMessage(SR.net_PropertyNotImplementedException); } }

        /// Gets the physical address of this network interface
        public virtual PhysicalAddress GetPhysicalAddress()
        {
            throw NotImplemented.ByDesignWithMessage(SR.net_MethodNotImplementedException);
        }

        /// Gets the interface type.
        public virtual NetworkInterfaceType NetworkInterfaceType { get { throw NotImplemented.ByDesignWithMessage(SR.net_PropertyNotImplementedException); } }

        public virtual bool Supports(NetworkInterfaceComponent networkInterfaceComponent)
        {
            throw NotImplemented.ByDesignWithMessage(SR.net_MethodNotImplementedException);
        }
    }
}
