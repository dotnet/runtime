// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;

namespace System.Net.NetworkInformation
{
    internal static partial class StringParsingHelpers
    {
        // /proc/net/route contains some information about gateway addresses,
        // and separates the information about by each interface.
        internal static List<GatewayIPAddressInformation> ParseGatewayAddressesFromRouteFile(string filePath, string interfaceName)
        {
            if (!File.Exists(filePath))
            {
                throw ExceptionHelper.CreateForInformationUnavailable();
            }

            List<GatewayIPAddressInformation> collection = new List<GatewayIPAddressInformation>();
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

        internal static List<IPAddress> ParseDhcpServerAddressesFromLeasesFile(string filePath, string name)
        {
            // Parse the /var/lib/dhcp/dhclient.leases file, if it exists.
            // If any errors occur, like the file not existing or being
            // improperly formatted, just bail and return an empty collection.
            List<IPAddress> collection = new List<IPAddress>();
            try
            {
                string fileContents = File.ReadAllText(filePath);
                int leaseIndex = -1;
                int secondBrace = -1;
                while ((leaseIndex = fileContents.IndexOf("lease", leaseIndex + 1, StringComparison.Ordinal)) != -1)
                {
                    int firstBrace = fileContents.IndexOf('{', leaseIndex);
                    secondBrace = fileContents.IndexOf('}', leaseIndex);
                    int blockLength = secondBrace - firstBrace;

                    int interfaceIndex = fileContents.IndexOf("interface", firstBrace, blockLength, StringComparison.Ordinal);
                    int afterName = fileContents.IndexOf(';', interfaceIndex);
                    int beforeName = fileContents.LastIndexOf(' ', afterName);
                    string interfaceName = fileContents.Substring(beforeName + 2, afterName - beforeName - 3);
                    if (interfaceName != name)
                    {
                        continue;
                    }

                    int indexOfDhcp = fileContents.IndexOf("dhcp-server-identifier", firstBrace, blockLength, StringComparison.Ordinal);
                    int afterAddress = fileContents.IndexOf(';', indexOfDhcp);
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

        internal static List<IPAddress> ParseWinsServerAddressesFromSmbConfFile(string smbConfFilePath)
        {
            List<IPAddress> collection = new List<IPAddress>();
            try
            {
                string fileContents = File.ReadAllText(smbConfFilePath);
                string label = "wins server = ";
                int labelIndex = fileContents.IndexOf(label);
                int labelLineStart = fileContents.LastIndexOf(Environment.NewLine, labelIndex, StringComparison.Ordinal);
                if (labelLineStart < labelIndex)
                {
                    int commentIndex = fileContents.IndexOf(';', labelLineStart, labelIndex - labelLineStart);
                    if (commentIndex != -1)
                    {
                        return collection;
                    }
                }
                int endOfLine = fileContents.IndexOf(Environment.NewLine, labelIndex, StringComparison.Ordinal);
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
