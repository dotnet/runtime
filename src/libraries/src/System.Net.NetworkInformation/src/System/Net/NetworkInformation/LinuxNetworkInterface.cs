// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace System.Net.NetworkInformation
{
    /// <summary>
    /// Implements a NetworkInterface on Linux.
    /// </summary>
    internal class LinuxNetworkInterface : UnixNetworkInterface
    {
        private readonly OperationalStatus _operationalStatus;
        private readonly bool _supportsMulticast;
        private readonly long? _speed;
        private readonly LinuxIPInterfaceProperties _ipProperties;

        internal LinuxNetworkInterface(string name) : base(name)
        {
            _operationalStatus = GetOperationalStatus(name);
            _supportsMulticast = GetSupportsMulticast(name);
            _speed = GetSpeed(name);
            _ipProperties = new LinuxIPInterfaceProperties(this);
        }

        public unsafe static NetworkInterface[] GetLinuxNetworkInterfaces()
        {
            Dictionary<string, LinuxNetworkInterface> interfacesByName = new Dictionary<string, LinuxNetworkInterface>();
            Interop.Sys.EnumerateInterfaceAddresses(
                (name, ipAddr, maskAddr) =>
                {
                    LinuxNetworkInterface lni = GetOrCreate(interfacesByName, name);
                    lni.ProcessIpv4Address(ipAddr, maskAddr);
                },
                (name, ipAddr, scopeId) =>
                {
                    LinuxNetworkInterface lni = GetOrCreate(interfacesByName, name);
                    lni.ProcessIpv6Address( ipAddr, *scopeId);
                },
                (name, llAddr) =>
                {
                    LinuxNetworkInterface lni = GetOrCreate(interfacesByName, name);
                    lni.ProcessLinkLayerAddress(llAddr);
                });

            return interfacesByName.Values.ToArray();
        }

        /// <summary>
        /// Gets or creates a LinuxNetworkInterface, based on whether it already exists in the given Dictionary.
        /// If created, it is added to the Dictionary.
        /// </summary>
        /// <param name="interfaces">The Dictionary of existing interfaces.</param>
        /// <param name="name">The name of the interface.</param>
        /// <returns>The cached or new LinuxNetworkInterface with the given name.</returns>
        private static LinuxNetworkInterface GetOrCreate(Dictionary<string, LinuxNetworkInterface> interfaces, string name)
        {
            LinuxNetworkInterface lni;
            if (!interfaces.TryGetValue(name, out lni))
            {
                lni = new LinuxNetworkInterface(name);
                interfaces.Add(name, lni);
            }

            return lni;
        }

        public override bool SupportsMulticast { get { return _supportsMulticast; } }

        private static bool GetSupportsMulticast(string name)
        {
            // /sys/class/net/<interface_name>/flags
            string path = Path.Combine(NetworkFiles.SysClassNetFolder, name, NetworkFiles.FlagsFileName);
            Interop.LinuxNetDeviceFlags flags = (Interop.LinuxNetDeviceFlags)StringParsingHelpers.ParseRawHexFileAsInt(path);

            return (flags & Interop.LinuxNetDeviceFlags.IFF_MULTICAST) == Interop.LinuxNetDeviceFlags.IFF_MULTICAST;
        }

        public override IPInterfaceProperties GetIPProperties()
        {
            return _ipProperties;
        }

        public override IPInterfaceStatistics GetIPStatistics()
        {
            return new LinuxIPInterfaceStatistics(_name);
        }

        public override OperationalStatus OperationalStatus { get { return _operationalStatus; } }

        public override string Id { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        public override string Description { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        public override long Speed
        {
            get
            {
                if (_speed.HasValue)
                {
                    return _speed.Value;
                }
                else
                {
                    throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);
                }
            }
        }

        public override bool IsReceiveOnly { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        private static long? GetSpeed(string name)
        {
            try
            {
                string path = Path.Combine(NetworkFiles.SysClassNetFolder, name, NetworkFiles.SpeedFileName);
                return StringParsingHelpers.ParseRawLongFile(path);
            }
            catch (IOException) // Some interfaces may give an "Invalid argument" error when opening this file.
            {
                return null;
            }
        }

        private static OperationalStatus GetOperationalStatus(string name)
        {
            // /sys/class/net/<name>/operstate
            string path = Path.Combine(NetworkFiles.SysClassNetFolder, name, NetworkFiles.OperstateFileName);
            string state = File.ReadAllText(path).Trim();
            return MapState(state);
        }

        // Maps values from /sys/class/net/<interface>/operstate to OperationStatus values.
        private static OperationalStatus MapState(string state)
        {
            // TODO: Figure out the possible values that Linux might return.
            switch (state)
            {
                case "up":
                    return OperationalStatus.Up;
                case "down":
                    return OperationalStatus.Down;
                default:
                    return OperationalStatus.Unknown;
            }
        }
    }
}
