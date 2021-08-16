// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;

namespace System.Net.NetworkInformation
{
    internal static partial class StringParsingHelpers
    {
        private static char[] s_delimiter = new char[1] { ' ' };
        // /proc/net/route contains some information about gateway addresses,
        // and separates the information about by each interface.
        internal static List<GatewayIPAddressInformation> ParseIPv4GatewayAddressesFromRouteFile(List<GatewayIPAddressInformation> collection, string[] fileLines, string interfaceName)
        {
            // Columns are as follows (first-line header):
            // Iface  Destination  Gateway  Flags  RefCnt  Use  Metric  Mask  MTU  Window  IRTT
            foreach (string line in fileLines)
            {
                if (line.StartsWith(interfaceName, StringComparison.Ordinal))
                {
                    StringParser parser = new StringParser(line, '\t', skipEmpty: true);
                    parser.MoveNext();
                    parser.MoveNextOrFail();
                    string gatewayIPHex = parser.MoveAndExtractNext();
                    long addressValue = Convert.ToInt64(gatewayIPHex, 16);
                    if (addressValue != 0)
                    {
                        // Skip device routes without valid NextHop IP address.
                        IPAddress address = new IPAddress(addressValue);
                        collection.Add(new SimpleGatewayIPAddressInformation(address));
                    }
                }
            }

            return collection;
        }

        internal static void ParseIPv6GatewayAddressesFromRouteFile(List<GatewayIPAddressInformation> collection, string[] fileLines, string interfaceName, long scopeId)
        {
            // Columns are as follows (first-line header):
            // 00000000000000000000000000000000 00 00000000000000000000000000000000 00 00000000000000000000000000000000 ffffffff 00000001 00000001 00200200 lo
            // +------------------------------+ ++ +------------------------------+ ++ +------------------------------+ +------+ +------+ +------+ +------+ ++
            // |                                |  |                                |  |                                |        |        |        |        |
            // 0                                1  2                                3  4                                5        6        7        8        9
            //
            // 0. IPv6 destination network displayed in 32 hexadecimal chars without colons as separator
            // 1. IPv6 destination prefix length in hexadecimal
            // 2. IPv6 source network displayed in 32 hexadecimal chars without colons as separator
            // 3. IPv6 source prefix length in hexadecimal
            // 4. IPv6 next hop displayed in 32 hexadecimal chars without colons as separator
            // 5. Metric in hexadecimal
            // 6. Reference counter
            // 7. Use counter
            // 8. Flags
            // 9. Interface name
            foreach (string line in fileLines)
            {
                if (line.StartsWith("00000000000000000000000000000000", StringComparison.Ordinal))
                {
                   string[] token = line.Split(s_delimiter, StringSplitOptions.RemoveEmptyEntries);
                   if (token.Length > 9 && token[4] != "00000000000000000000000000000000")
                   {
                        if (!string.IsNullOrEmpty(interfaceName) && interfaceName != token[9])
                        {
                            continue;
                        }

                        IPAddress address = ParseIPv6HexString(token[4], isNetworkOrder: true);
                        if (address.IsIPv6LinkLocal)
                        {
                            // For Link-Local addresses add ScopeId as that is not part of the route entry.
                            address.ScopeId = scopeId;
                        }
                        collection.Add(new SimpleGatewayIPAddressInformation(address));
                    }
                }
            }
        }

        internal static void ParseDhcpServerAddressesFromLeasesFile(List<IPAddress> collection, string filePath, string name)
        {
            // Parse the /var/lib/dhcp/dhclient.leases file, if it exists.
            // If any errors occur, like the file not existing or being
            // improperly formatted, just bail and return an empty collection.
            try
            {
                if (File.Exists(filePath)) // avoid an exception in most cases if path doesn't already exist
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
                        ReadOnlySpan<char> interfaceName = fileContents.AsSpan(beforeName + 2, afterName - beforeName - 3);
                        if (!interfaceName.SequenceEqual(name))
                        {
                            continue;
                        }

                        int indexOfDhcp = fileContents.IndexOf("dhcp-server-identifier", firstBrace, blockLength, StringComparison.Ordinal);
                        int afterAddress = fileContents.IndexOf(';', indexOfDhcp);
                        int beforeAddress = fileContents.LastIndexOf(' ', afterAddress);
                        ReadOnlySpan<char> dhcpAddressSpan = fileContents.AsSpan(beforeAddress + 1, afterAddress - beforeAddress - 1);
                        if (IPAddress.TryParse(dhcpAddressSpan, out IPAddress? dhcpAddress))
                        {
                            collection.Add(dhcpAddress);
                        }
                    }
                }
            }
            catch
            {
                // If any parsing or file reading exception occurs, just ignore it and return the collection.
            }
        }

        internal static List<IPAddress> ParseWinsServerAddressesFromSmbConfFile(string smbConfFilePath)
        {
            List<IPAddress> collection = new List<IPAddress>();
            try
            {
                if (File.Exists(smbConfFilePath)) // avoid an exception in most cases if path doesn't already exist
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
                    ReadOnlySpan<char> addressSpan = fileContents.AsSpan(labelIndex + label.Length, endOfLine - (labelIndex + label.Length));
                    IPAddress address = IPAddress.Parse(addressSpan);
                    collection.Add(address);
                }
            }
            catch
            {
                // If any parsing or file reading exception occurs, just ignore it and return the collection.
            }

            return collection;
        }

    }
}
