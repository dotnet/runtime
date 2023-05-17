// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.IO;
using System.Text;

namespace System.Net.NetworkInformation
{
    internal static class UnixCommandLinePing
    {
        // Ubuntu has ping under /bin, OSX under /sbin, ArchLinux under /usr/bin, Android under /system/bin, NixOS under /run/current-system/sw/bin.
        private static readonly string[] s_binFolders = { "/bin", "/sbin", "/usr/bin", "/system/bin", "/run/current-system/sw/bin" };
        private const string s_ipv4PingFile = "ping";
        private const string s_ipv6PingFile = "ping6";

        private static readonly string? s_discoveredPing4UtilityPath = GetPingUtilityPath(ipv4: true);
        private static readonly string? s_discoveredPing6UtilityPath = GetPingUtilityPath(ipv4: false);
        private static readonly Lazy<bool> s_isBusybox = new Lazy<bool>(() => IsBusyboxPing(s_discoveredPing4UtilityPath));

        // We don't want to pick up an arbitrary or malicious ping
        // command, so that's why we do the path probing ourselves.
        private static string? GetPingUtilityPath(bool ipv4)
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

        // Check if found ping is symlink to busybox like alpine /bin/ping -> /bin/busybox
        private static bool IsBusyboxPing(string? pingBinary)
        {
            if (pingBinary != null)
            {
                System.IO.FileSystemInfo? linkInfo = File.ResolveLinkTarget(pingBinary, returnFinalTarget: true);
                if (linkInfo?.Name.EndsWith("busybox", StringComparison.Ordinal) == true)
                {
                    return true;
                }
            }

            return false;
        }

        public enum PingFragmentOptions { Default, Do, Dont };

        /// <summary>
        /// The location of the IPv4 ping utility on the current machine.
        /// </summary>
        public static string? Ping4UtilityPath { get { return s_discoveredPing4UtilityPath; } }

        /// <summary>
        /// The location of the IPv6 ping utility on the current machine.
        /// </summary>
        public static string? Ping6UtilityPath { get { return s_discoveredPing6UtilityPath; } }

        /// <summary>
        /// Constructs command line arguments appropriate for the ping or ping6 utility.
        /// </summary>
        /// <param name="packetSize">The packet size to use in the ping. Exact packet payload cannot be specified.</param>
        /// <param name="timeout">The timeout to use in the ping, in milliseconds.</param>
        /// <param name="address">A string representation of the IP address to ping.</param>
        /// <param name="ipv4">true for ipv4; false for ipv6.</param>
        /// <param name="ttl">The time to live.</param>
        /// <param name="fragmentOption">Fragmentation options.</param>
        /// <returns>The constructed command line arguments, which can be passed to ping or ping6.</returns>
        public static string ConstructCommandLine(int packetSize, int timeout, string address, bool ipv4, int ttl = 0, PingFragmentOptions fragmentOption = PingFragmentOptions.Default)
        {
            var sb = new StringBuilder();
            sb.Append("-c 1"); // Just send a single ping ("count = 1")

            //if timeout is zero then some ping implementations can stuck infinitely if endpoint is unreachable
            if (timeout == 0)
                timeout = 1;

            // Pass timeout argument to ping utility
            // BusyBox, Linux: ping and ping6 requires -W flag which accepts timeout in SECONDS.
            // FreeBSD: ping requires -W flag which accepts timeout in MILLISECONDS;
            // ping6 requires -x which accepts timeout in MILLISECONDS
            // OSX: ping requires -W flag which accepts timeout in MILLISECONDS; ping6 doesn't support timeout
            if (OperatingSystem.IsFreeBSD())
            {
                // Syntax changed in FreeBSD 13.0 and options are not common for both address families
                if (ipv4 || Environment.OSVersion.Version.Major > 12)
                {
                    sb.Append(" -W ");
                }
                else
                {
                    sb.Append(" -x ");
                }
            }
            else if (OperatingSystem.IsMacOS())
            {
                if (ipv4)
                {
                    sb.Append(" -W ");
                }
                else
                {
                    goto skipped_timeout;
                }
            }
            else
            {
                sb.Append(" -W ");
                const int millisInSecond = 1000;
                timeout = Math.DivRem(timeout, millisInSecond, out int remainder);
                if (remainder != 0)
                {
                    timeout += 1;
                }
            }
            sb.Append(timeout);

        skipped_timeout:

            // The command-line flags for "Do-not-fragment" and "TTL" are not standard.
            // In fact, they are different even between ping and ping6 on the same machine.

            // The ping utility is not flexible enough to specify an exact payload.
            // But we can at least send the right number of bytes.

            if (ttl > 0)
            {
                if (OperatingSystem.IsFreeBSD() || OperatingSystem.IsMacOS())
                {
                    // OSX and FreeBSD use -h to set hop limit for IPv6 and -m ttl for IPv4
                    // Syntax changed in FreeBSD 13.0 and options are not common for both address families
                    if (ipv4 || (OperatingSystem.IsFreeBSD() && Environment.OSVersion.Version.Major > 12))
                    {
                        sb.Append(" -m ");
                    }
                    else
                    {
                        sb.Append(" -h ");
                    }
                }
                else
                {
                    // Linux uses -t ttl for both IPv4 & IPv6
                    sb.Append(" -t ");
                }

                sb.Append(ttl);
            }

            if (fragmentOption != PingFragmentOptions.Default)
            {
                if (OperatingSystem.IsFreeBSD() || OperatingSystem.IsMacOS())
                {
                    // The bit is off by default on OSX & FreeBSD
                    if (fragmentOption == PingFragmentOptions.Dont) {
                        sb.Append(" -D ");
                    }
                }
                else if (!s_isBusybox.Value)  // busybox implementation does not support fragmentation option.
                {
                    // Linux has three state option with default to use PMTU.
                    // When explicit option is used we set it explicitly to one or the other.
                    if (fragmentOption == PingFragmentOptions.Do) {
                        sb.Append(" -M do ");
                    } else {
                        sb.Append(" -M dont ");
                    }
                }
            }

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
            int msIndex = pingOutput.IndexOf("ms", afterTime, StringComparison.Ordinal);
            int numLength = msIndex - afterTime - 1;
            ReadOnlySpan<char> timeSubstring = pingOutput.AsSpan(afterTime, numLength);
            double parsedRtt = double.Parse(timeSubstring, provider: CultureInfo.InvariantCulture);
            return (long)Math.Round(parsedRtt);
        }
    }
}
