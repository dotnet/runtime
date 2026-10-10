// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;

namespace System.Net.NetworkInformation
{
    internal static partial class StringParsingHelpers
    {
        // Returns null when the file does not exist. A missing parent directory (e.g. no /etc)
        // surfaces as DirectoryNotFoundException rather than FileNotFoundException.
        internal static string? ReadResolvConfFile(string filePath)
        {
            try
            {
                return File.ReadAllText(filePath);
            }
            catch (Exception e) when (e is FileNotFoundException or DirectoryNotFoundException)
            {
                return null;
            }
        }

        internal static string ParseDnsSuffixFromResolvConfFile(string data)
        {
            RowConfigReader rcr = new RowConfigReader(data);

            return rcr.TryGetNextValue("search", out string? dnsSuffix) ? dnsSuffix : string.Empty;
        }

        internal static List<IPAddress> ParseDnsAddressesFromResolvConfFile(string data)
        {
            // Parse /etc/resolv.conf for all of the "nameserver" entries.
            // These are the DNS servers the machine is configured to use.
            // On OSX, this file is not directly used by most processes for DNS
            // queries/routing, but it is automatically generated instead, with
            // the machine's DNS servers listed in it.
            RowConfigReader rcr = new RowConfigReader(data);
            List<IPAddress> addresses = new List<IPAddress>();

            while (rcr.TryGetNextValue("nameserver", out string? addressString))
            {
                if (IPAddress.TryParse(addressString, out IPAddress? parsedAddress))
                {
                    addresses.Add(parsedAddress);
                }
            }

            return addresses;
        }
    }
}
