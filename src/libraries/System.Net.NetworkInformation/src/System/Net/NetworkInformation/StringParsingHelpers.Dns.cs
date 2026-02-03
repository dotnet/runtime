// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;

namespace System.Net.NetworkInformation
{
    internal static partial class StringParsingHelpers
    {
        internal static string ParseDnsSuffixFromResolvConfFile(string data)
        {
            // Per resolv.conf(5), both "search" and "domain" keywords can specify DNS suffixes.
            // The "domain" directive is an obsolete name for "search" that handles one entry only.
            // If multiple instances of these keywords are present, the last instance wins.
            string? lastSearchSuffix = null;
            int lastSearchIndex = -1;

            string? lastDomainSuffix = null;
            int lastDomainIndex = -1;

            // Find all "search" entries and remember the last one
            int searchIndex = 0;
            while ((searchIndex = data.IndexOf("search", searchIndex, StringComparison.Ordinal)) != -1)
            {
                RowConfigReader rcr = new RowConfigReader(data.Substring(searchIndex));
                if (rcr.TryGetNextValue("search", out string? suffix))
                {
                    lastSearchSuffix = suffix;
                    lastSearchIndex = searchIndex;
                }
                searchIndex++;
            }

            // Find all "domain" entries and remember the last one
            int domainIndex = 0;
            while ((domainIndex = data.IndexOf("domain", domainIndex, StringComparison.Ordinal)) != -1)
            {
                RowConfigReader rcr = new RowConfigReader(data.Substring(domainIndex));
                if (rcr.TryGetNextValue("domain", out string? suffix))
                {
                    lastDomainSuffix = suffix;
                    lastDomainIndex = domainIndex;
                }
                domainIndex++;
            }

            // Return the value from whichever keyword appeared last
            if (lastSearchIndex >= 0 && lastSearchIndex > lastDomainIndex)
            {
                return lastSearchSuffix!;
            }
            else if (lastDomainIndex >= 0)
            {
                return lastDomainSuffix!;
            }

            return string.Empty;
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
