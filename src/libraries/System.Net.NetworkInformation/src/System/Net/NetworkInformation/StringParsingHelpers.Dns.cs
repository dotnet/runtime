// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.IO;

namespace System.Net.NetworkInformation
{
    internal static partial class StringParsingHelpers
    {
        private static readonly SearchValues<string> s_searchOrDomain = SearchValues.Create(["search", "domain"], StringComparison.Ordinal);

        internal static string ParseDnsSuffixFromResolvConfFile(string data)
        {
            // Per resolv.conf(5), both "search" and "domain" keywords can specify DNS suffixes.
            // The "domain" directive is an obsolete name for "search" that handles one entry only.
            // If multiple instances of these keywords are present, the last instance wins.
            string? dnsSuffix = null;
            ReadOnlySpan<char> remaining = data;

            // Process the file using RowConfigReader. It validates that keys are at line start
            // and followed by whitespace. We search for both keywords and process each match
            // to find the last valid occurrence.
            while (!remaining.IsEmpty)
            {
                // Find the next occurrence of either keyword
                int nextIndex = remaining.IndexOfAny(s_searchOrDomain);
                if (nextIndex < 0)
                {
                    break;
                }

                string nextKeyword = remaining[nextIndex] == 'd' ? "domain" : "search";

                // Use RowConfigReader to validate and extract the value.
                // This handles validation that the key is at line start and followed by whitespace.
                RowConfigReader lineReader = new RowConfigReader(remaining.Slice(nextIndex));
                if (lineReader.TryGetNextValue(nextKeyword, out ReadOnlySpan<char> suffix))
                {
                    // Don't break here - per resolv.conf(5), the last instance wins,
                    // so we need to continue searching for more occurrences.
                    dnsSuffix = suffix.ToString();
                }

                remaining = remaining.Slice(nextIndex + 1);
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

            while (rcr.TryGetNextValue("nameserver", out ReadOnlySpan<char> addressSpan))
            {
                if (IPAddress.TryParse(addressSpan, out IPAddress? parsedAddress))
                {
                    addresses.Add(parsedAddress);
                }
            }

            return addresses;
        }
    }
}
