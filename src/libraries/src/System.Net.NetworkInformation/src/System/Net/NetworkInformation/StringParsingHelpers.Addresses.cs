// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.IO;

namespace System.Net.NetworkInformation
{
    internal static partial class StringParsingHelpers
    {
        // /proc/net/route contains some information about gateway addresses,
        // and seperates the information about by each interface.
        internal static Collection<GatewayIPAddressInformation> ParseGatewayAddressesFromRouteFile(string filePath, string interfaceName)
        {
            Collection<GatewayIPAddressInformation> collection = new Collection<GatewayIPAddressInformation>();
            // Columns are as follows (first-line header):
            // Iface  Destination  Gateway  Flags  RefCnt  Use  Metric  Mask  MTU  Window  IRTT
            string[] fileLines = File.ReadAllLines(filePath);
            foreach (string line in fileLines)
            {
                if (line.StartsWith(interfaceName))
                {
                    StringParser parser = new StringParser(line, '\t', skipEmpty: true);
                    parser.MoveNext();
                    parser.MoveNextOrFail();
                    string gatewayIPHex = parser.MoveAndExtractNext();
                    long addressValue = Convert.ToInt64(gatewayIPHex, 16);
                    IPAddress address = new IPAddress(addressValue);
                    collection.Add(new SimpleGatewayIPAddressInformation(address));
                }
            }

            return collection;
        }

        internal static Collection<IPAddress> ParseDhcpServerAddressesFromLeasesFile(string filePath, string name)
        {
            // Parse the /var/lib/dhcp/dhclient.leases file, if it exists.
            // If any errors occur, like the file not existing or being
            // improperly formatted, just bail and return an empty collection.
            Collection<IPAddress> collection = new Collection<IPAddress>();
            try
            {
                string fileContents = File.ReadAllText(filePath);
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
                    if (interfaceName != name)
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
                        collection.Add(dhcpAddress);
                    }
                }
            }
            catch
            {
                // If any parsing or file reading exception occurs, just ignore it and return the collection.
            }

            return collection;
        }

        internal static Collection<IPAddress> ParseWinsServerAddressesFromSmbConfFile(string smbConfFilePath)
        {
            Collection<IPAddress> collection = new Collection<IPAddress>();
            try
            {
                string fileContents = File.ReadAllText(smbConfFilePath);
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
                collection.Add(address);
            }
            catch
            {
                // If any parsing or file reading exception occurs, just ignore it and return the collection.
            }

            return collection;
        }

    }
}
