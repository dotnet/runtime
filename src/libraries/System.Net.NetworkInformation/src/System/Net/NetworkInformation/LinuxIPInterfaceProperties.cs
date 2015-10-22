// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace System.Net.NetworkInformation
{
    internal class LinuxIPInterfaceProperties : UnixIPInterfaceProperties
    {
        private readonly LinuxNetworkInterface _linuxNetworkInterface;
        private readonly GatewayIPAddressInformationCollection _gatewayAddresses;
        private readonly IPAddressCollection _dhcpServerAddresses;
        private readonly IPAddressCollection _winsServerAddresses;
        private readonly LinuxIPv4InterfaceProperties _ipv4Properties;
        private readonly LinuxIPv6InterfaceProperties _ipv6Properties;

        public LinuxIPInterfaceProperties(LinuxNetworkInterface lni)
            : base(lni)
        {
            _linuxNetworkInterface = lni;
            _gatewayAddresses = GetGatewayAddresses();
            _dhcpServerAddresses = GetDhcpServerAddresses();
            _winsServerAddresses = GetWinsServerAddresses();
            _ipv4Properties = new LinuxIPv4InterfaceProperties(lni);
            _ipv6Properties = new LinuxIPv6InterfaceProperties(lni);
        }

        public override bool IsDynamicDnsEnabled
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        public override IPAddressInformationCollection AnycastAddresses
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        public override GatewayIPAddressInformationCollection GatewayAddresses { get { return _gatewayAddresses; } }

        public override IPAddressCollection DhcpServerAddresses
        {
            get
            {
                return _dhcpServerAddresses;
            }
        }

        public override IPAddressCollection WinsServersAddresses
        {
            get
            {
                return _winsServerAddresses;
            }
        }

        public override IPv4InterfaceProperties GetIPv4Properties()
        {
            return _ipv4Properties;
        }

        public override IPv6InterfaceProperties GetIPv6Properties()
        {
            return _ipv6Properties;
        }

        // /proc/net/route contains some information about gateway addresses,
        // and seperates the information about by each interface.
        public GatewayIPAddressInformationCollection GetGatewayAddresses()
        {
            GatewayIPAddressInformationCollection collection = new GatewayIPAddressInformationCollection();
            // Columns are as follows (first-line header):
            // Iface  Destination  Gateway  Flags  RefCnt  Use  Metric  Mask  MTU  Window  IRTT
            string[] fileLines = File.ReadAllLines(NetworkFiles.Ipv4RouteFile);
            foreach (string line in fileLines)
            {
                if (line.StartsWith(_linuxNetworkInterface.Name))
                {
                    StringParser parser = new StringParser(line, '\t', skipEmpty: true);
                    parser.MoveNext();
                    parser.MoveNextOrFail();
                    string gatewayIPHex = parser.MoveAndExtractNext();
                    long addressValue = Convert.ToInt64(gatewayIPHex, 16);
                    IPAddress address = new IPAddress(addressValue);
                    collection.InternalAdd(new SimpleGatewayIPAddressInformation(address));
                }
            }

            return collection;
        }

        private IPAddressCollection GetDhcpServerAddresses()
        {
            // Parse the /var/lib/dhcp/dhclient.leases file, if it exists.
            // If any errors occur, like the file not existing or being
            // improperly formatted, just bail and return an empty collection.
            InternalIPAddressCollection collection = new InternalIPAddressCollection();
            try
            {
                string fileContents = File.ReadAllText(NetworkFiles.DHClientLeasesFile);
                int leaseIndex = -1;
                int secondBrace = -1;
                while ((leaseIndex = fileContents.IndexOf("lease", leaseIndex + 1)) != -1)
                {
                    int firstBrace = fileContents.IndexOf("{", leaseIndex);
                    secondBrace = fileContents.IndexOf("}", leaseIndex);
                    int blockLength = secondBrace - firstBrace;

                    int interfaceIndex = fileContents.IndexOf("interface", firstBrace, blockLength);
                    int afterName = fileContents.IndexOf(';', interfaceIndex);
                    int beforeName = fileContents.LastIndexOf(' ', afterName);
                    string interfaceName = fileContents.Substring(beforeName + 2, afterName - beforeName - 3);
                    if (interfaceName != _linuxNetworkInterface.Name)
                    {
                        continue;
                    }

                    int indexOfDhcp = fileContents.IndexOf("dhcp-server-identifier", firstBrace, blockLength);
                    int afterAddress = fileContents.IndexOf(";", indexOfDhcp);
                    int beforeAddress = fileContents.LastIndexOf(' ', afterAddress);
                    string dhcpAddressString = fileContents.Substring(beforeAddress + 1, afterAddress - beforeAddress - 1);
                    IPAddress dhcpAddress;
                    if (IPAddress.TryParse(dhcpAddressString, out dhcpAddress))
                    {
                        collection.InternalAdd(dhcpAddress);
                    }
                }
            }
            catch
            {
                // If any parsing or file reading exception occurs, just ignore it and return the collection.
            }

            return collection;
        }

        private IPAddressCollection GetWinsServerAddresses()
        {
            InternalIPAddressCollection collection = new InternalIPAddressCollection();
            try
            {
                string fileContents = File.ReadAllText(NetworkFiles.SmbConfFile);
                string label = "wins server = ";
                int labelIndex = fileContents.IndexOf(label);
                int labelLineStart = fileContents.LastIndexOf(Environment.NewLine, labelIndex);
                if (labelLineStart < labelIndex)
                {
                    int commentIndex = fileContents.IndexOf(';', labelLineStart, labelIndex - labelLineStart);
                    if (commentIndex != -1)
                    {
                        return collection;
                    }
                }
                int endOfLine = fileContents.IndexOf(Environment.NewLine, labelIndex);
                string addressString = fileContents.Substring(labelIndex + label.Length, endOfLine - (labelIndex + label.Length));
                IPAddress address = IPAddress.Parse(addressString);
                collection.InternalAdd(address);
            }
            catch
            {
                // If any parsing or file reading exception occurs, just ignore it and return the collection.
            }

            return collection;
        }
    }
}
