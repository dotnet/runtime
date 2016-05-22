// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Text;

namespace System.Net.NetworkInformation
{
    internal static class UnixCommandLinePing
    {
        // Ubuntu has ping under /bin, OSX under /sbin, ArchLinux under /usr/bin.
        private static readonly string[] s_binFolders = { "/bin/", "/sbin/", "/usr/bin/" };
        private const string s_ipv4PingFile = "ping";
        private const string s_ipv6PingFile = "ping6";

        private static readonly string s_discoveredPing4UtilityPath = GetPingUtilityPath(ipv4: true);
        private static readonly string s_discoveredPing6UtilityPath = GetPingUtilityPath(ipv4: false);

        // We don't want to pick up an arbitrary or malicious ping
        // command, so that's why we do the path probing ourselves.
        private static string GetPingUtilityPath(bool ipv4)
        {
            string fileName = ipv4 ? s_ipv4PingFile : s_ipv6PingFile;
            foreach (string folder in s_binFolders)
            {
                string path = Path.Combine(folder, fileName);
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return null;
        }

        /// <summary>
        /// The location of the IPv4 ping utility on the current machine.
        /// </summary>
        public static string Ping4UtilityPath { get { return s_discoveredPing4UtilityPath; } }

        /// <summary>
        /// The location of the IPv6 ping utility on the current machine.
        /// </summary>
        public static string Ping6UtilityPath { get { return s_discoveredPing6UtilityPath; } }

        /// <summary>
        /// Constructs command line arguments appropriate for the ping or ping6 utility.
        /// </summary>
        /// <param name="packetSize">The packet size to use in the ping. Exact packet payload cannot be specified.</param>
        /// <param name="address">A string representation of the IP address to ping.</param>
        /// <returns>The constructed command line arguments, which can be passed to ping or ping6.</returns>
        public static string ConstructCommandLine(int packetSize, string address, bool ipv4)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("-c 1"); // Just send a single ping ("count = 1")

            // The command-line flags for "Do-not-fragment" and "TTL" are not standard.
            // In fact, they are different even between ping and ping6 on the same machine.

            // The ping utility is not flexible enough to specify an exact payload.
            // But we can at least send the right number of bytes.

            // ping and ping6 do not report timing information unless at least 16 bytes are sent.
            if (packetSize < 16)
            {
                packetSize = 16;
            }

            sb.Append(" -s ");
            sb.Append(packetSize);

            sb.Append(' ');
            sb.Append(address);

            return sb.ToString();
        }

        /// <summary>
        /// Parses the standard output of the ping utility, returning the round-trip time of the ping.
        /// </summary>
        /// <param name="pingOutput">The full standard output of a ping utility run.</param>
        /// <returns>The parsed round-trip time of a successful ping. Throws if parsing was unsuccessful.</returns>
        public static long ParseRoundTripTime(string pingOutput)
        {
            int timeIndex = pingOutput.IndexOf("time=", StringComparison.Ordinal);
            int afterTime = timeIndex + "time=".Length;
            int msIndex = pingOutput.IndexOf("ms", afterTime);
            int numLength = msIndex - afterTime - 1;
            string timeSubstring = pingOutput.Substring(afterTime, numLength);
            double parsedRtt = double.Parse(timeSubstring);
            return (long)Math.Round(parsedRtt);
        }
    }
}
