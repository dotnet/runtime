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
            string? dnsSuffix = null;

            // Process the file using RowConfigReader. It validates that keys are at line start
            // and followed by whitespace. We interleave searches for both keywords to ensure
            // we process them in file order and the last match wins.
            int currentIndex = 0;

            while (currentIndex < data.Length)
            {
                // Find the next occurrence of either keyword in the raw data
                int searchIndex = data.IndexOf("search", currentIndex, StringComparison.Ordinal);
                int domainIndex = data.IndexOf("domain", currentIndex, StringComparison.Ordinal);

                // Determine which keyword appears next (if any)
                int nextIndex;
                string nextKeyword;

                if (searchIndex == -1 && domainIndex == -1)
                {
                    break; // No more occurrences of either keyword
                }
                else if (searchIndex == -1)
                {
                    nextIndex = domainIndex;
                    nextKeyword = "domain";
                }
                else if (domainIndex == -1)
                {
                    nextIndex = searchIndex;
                    nextKeyword = "search";
                }
                else
                {
                    // Both found - process whichever comes first
                    nextIndex = Math.Min(searchIndex, domainIndex);
                    nextKeyword = searchIndex < domainIndex ? "search" : "domain";
                }

                // Use RowConfigReader to validate and extract the value.
                // This handles validation that the key is at line start and followed by whitespace.
                RowConfigReader lineReader = new RowConfigReader(data.Substring(nextIndex));
                if (lineReader.TryGetNextValue(nextKeyword, out string? suffix))
                {
                    dnsSuffix = suffix;
                }

                currentIndex = nextIndex + 1;
            }

            return dnsSuffix ?? string.Empty;
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
