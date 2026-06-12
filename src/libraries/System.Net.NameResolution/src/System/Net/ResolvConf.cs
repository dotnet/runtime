// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;

namespace System.Net
{
    // Parses the system DNS server configuration from /etc/resolv.conf (RFC-style
    // "nameserver" directives). Used when DnsResolverOptions.Servers is empty.
    internal static class ResolvConf
    {
        private const string ResolvConfPath = "/etc/resolv.conf";
        internal const int DefaultDnsPort = 53;

        public static List<IPEndPoint> GetNameServers()
        {
            try
            {
                using StreamReader reader = new StreamReader(ResolvConfPath);
                return Parse(reader);
            }
            catch (IOException)
            {
                return new List<IPEndPoint>();
            }
            catch (UnauthorizedAccessException)
            {
                return new List<IPEndPoint>();
            }
        }

        // Parses "nameserver <address>" directives from a resolv.conf-formatted stream.
        // Lines beginning with '#' or ';' are comments. Any text following the address
        // on a nameserver line is ignored.
        internal static List<IPEndPoint> Parse(TextReader reader)
        {
            List<IPEndPoint> servers = new List<IPEndPoint>();

            string? line;
            while ((line = reader.ReadLine()) is not null)
            {
                ReadOnlySpan<char> span = line.AsSpan().Trim();
                if (span.IsEmpty || span[0] == '#' || span[0] == ';')
                {
                    continue;
                }

                const string Directive = "nameserver";
                if (!span.StartsWith(Directive, StringComparison.Ordinal))
                {
                    continue;
                }

                ReadOnlySpan<char> rest = span[Directive.Length..];
                if (rest.IsEmpty || (rest[0] != ' ' && rest[0] != '\t'))
                {
                    continue;
                }

                rest = rest.TrimStart();

                // The address is the first whitespace-delimited token; ignore anything after it.
                int ws = rest.IndexOfAny(' ', '\t');
                if (ws >= 0)
                {
                    rest = rest[..ws];
                }

                if (IPAddress.TryParse(rest, out IPAddress? address))
                {
                    servers.Add(new IPEndPoint(address, DefaultDnsPort));
                }
            }

            return servers;
        }
    }
}
