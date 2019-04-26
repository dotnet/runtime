// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        private readonly bool? _supportsMulticast;
        private readonly long? _speed;
        private readonly LinuxIPInterfaceProperties _ipProperties;

        internal LinuxNetworkInterface(string name, int index) : base(name)
        {
            _index = index;
            _operationalStatus = GetOperationalStatus(name);
            _supportsMulticast = GetSupportsMulticast(name);
            _speed = GetSpeed(name);
            _ipProperties = new LinuxIPInterfaceProperties(this);
        }

        public static unsafe NetworkInterface[] GetLinuxNetworkInterfaces()
        {
            Dictionary<string, LinuxNetworkInterface> interfacesByName = new Dictionary<string, LinuxNetworkInterface>();
            List<Exception> exceptions = null;
            const int MaxTries = 3;

            for (int attempt = 0; attempt < MaxTries; attempt++)
            {
                // Because these callbacks are executed in a reverse-PInvoke, we do not want any exceptions
                // to propagate out, because they will not be catchable. Instead, we track all the exceptions
                // that are thrown in these callbacks, and aggregate them at the end.
                int result = Interop.Sys.EnumerateInterfaceAddresses(
                    (name, ipAddr, maskAddr) =>
                    {
                        try
                        {
                            LinuxNetworkInterface lni = GetOrCreate(interfacesByName, name, ipAddr->InterfaceIndex);
                            lni.ProcessIpv4Address(ipAddr, maskAddr);
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
                            LinuxNetworkInterface lni = GetOrCreate(interfacesByName, name, ipAddr->InterfaceIndex);
                            lni.ProcessIpv6Address(ipAddr, *scopeId);
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
                            LinuxNetworkInterface lni = GetOrCreate(interfacesByName, name, llAddr->InterfaceIndex);
                            lni.ProcessLinkLayerAddress(llAddr);
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
                    return interfacesByName.Values.ToArray();
                }
                else
                {
                    interfacesByName.Clear();
                }
            }

            throw new NetworkInformationException(SR.net_PInvokeError);
        }

        /// <summary>
        /// Gets or creates a LinuxNetworkInterface, based on whether it already exists in the given Dictionary.
        /// If created, it is added to the Dictionary.
        /// </summary>
        /// <param name="interfaces">The Dictionary of existing interfaces.</param>
        /// <param name="name">The name of the interface.</param>
        /// <param name="index">Interafce index of the interface.</param>
        /// <returns>The cached or new LinuxNetworkInterface with the given name.</returns>
        private static LinuxNetworkInterface GetOrCreate(Dictionary<string, LinuxNetworkInterface> interfaces, string name, int index)
        {
            LinuxNetworkInterface lni;
            if (!interfaces.TryGetValue(name, out lni))
            {
                lni = new LinuxNetworkInterface(name, index);
                interfaces.Add(name, lni);
            }

            return lni;
        }

        public override bool SupportsMulticast
        {
            get
            {
                if (_supportsMulticast.HasValue)
                {
                    return _supportsMulticast.Value;
                }
                else
                {
                    throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);
                }
            }
        }

        private static bool? GetSupportsMulticast(string name)
        {
            // /sys/class/net/<interface_name>/flags
            string path = Path.Combine(NetworkFiles.SysClassNetFolder, name, NetworkFiles.FlagsFileName);

            if (File.Exists(path))
            {
                try
                {
                    Interop.LinuxNetDeviceFlags flags = (Interop.LinuxNetDeviceFlags)StringParsingHelpers.ParseRawHexFileAsInt(path);
                    return (flags & Interop.LinuxNetDeviceFlags.IFF_MULTICAST) == Interop.LinuxNetDeviceFlags.IFF_MULTICAST;
                }
                catch (Exception) // Ignore any problems accessing or parsing the file.
                {
                }
            }

            return null;
        }

        public override IPInterfaceProperties GetIPProperties()
        {
            return _ipProperties;
        }

        public override IPInterfaceStatistics GetIPStatistics()
        {
            return new LinuxIPInterfaceStatistics(_name);
        }

        public override IPv4InterfaceStatistics GetIPv4Statistics()
        {
            return new LinuxIPv4InterfaceStatistics(_name);
        }

        public override OperationalStatus OperationalStatus { get { return _operationalStatus; } }

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
                long megabitsPerSecond = StringParsingHelpers.ParseRawLongFile(path);
                return megabitsPerSecond == -1
                    ? megabitsPerSecond
                    : megabitsPerSecond * 1_000_000; // Value must be returned in bits per second, not megabits.
            }
            catch (Exception) // Ignore any problems accessing or parsing the file.
            {
                return null;
            }
        }

        private static OperationalStatus GetOperationalStatus(string name)
        {
            // /sys/class/net/<name>/operstate
            string path = Path.Combine(NetworkFiles.SysClassNetFolder, name, NetworkFiles.OperstateFileName);
            if (File.Exists(path))
            {
                try
                {
                    string state = File.ReadAllText(path).Trim();
                    return MapState(state);
                }
                catch (Exception) // Ignore any problems accessing or parsing the file.
                {
                }
            }

            return OperationalStatus.Unknown;
        }

        // Maps values from /sys/class/net/<interface>/operstate to OperationalStatus values.
        private static OperationalStatus MapState(string state)
        {
            //
            // http://users.sosdg.org/~qiyong/lxr/source/Documentation/networking/operstates.txt?a=um#L41
            //
            switch (state)
            {
                case "unknown":
                    return OperationalStatus.Unknown;
                case "notpresent":
                    return OperationalStatus.NotPresent;
                case "down":
                    return OperationalStatus.Down;
                case "lowerlayerdown":
                    return OperationalStatus.LowerLayerDown;
                case "testing":
                    return OperationalStatus.Testing;
                case "dormant":
                    return OperationalStatus.Dormant;
                case "up":
                    return OperationalStatus.Up;
                default:
                    return OperationalStatus.Unknown;
            }
        }
    }
}
