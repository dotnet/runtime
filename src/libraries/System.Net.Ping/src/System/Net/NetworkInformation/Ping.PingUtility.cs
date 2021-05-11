// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.NetworkInformation
{
    public partial class Ping
    {
        private Process GetPingProcess(IPAddress address, byte[] buffer, int timeout, PingOptions? options)
        {
            bool isIpv4 = address.AddressFamily == AddressFamily.InterNetwork;
            string? pingExecutable = isIpv4 ? UnixCommandLinePing.Ping4UtilityPath : UnixCommandLinePing.Ping6UtilityPath;
            if (pingExecutable == null)
            {
                throw new PlatformNotSupportedException(SR.net_ping_utility_not_found);
            }

            UnixCommandLinePing.PingFragmentOptions fragmentOption = UnixCommandLinePing.PingFragmentOptions.Default;
            if (options != null && address.AddressFamily == AddressFamily.InterNetwork)
            {
                fragmentOption = options.DontFragment ? UnixCommandLinePing.PingFragmentOptions.Do : UnixCommandLinePing.PingFragmentOptions.Dont;
            }

            string processArgs = UnixCommandLinePing.ConstructCommandLine(buffer.Length, timeout, address.ToString(), isIpv4, options?.Ttl ?? 0, fragmentOption);

            ProcessStartInfo psi = new ProcessStartInfo(pingExecutable, processArgs);
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            // Set LC_ALL=C to make sure to get ping output which is not affected by locale environment variables such as LANG and LC_MESSAGES.
            psi.EnvironmentVariables["LC_ALL"] = "C";
            return new Process() { StartInfo = psi };
        }

        private PingReply SendWithPingUtility(IPAddress address, byte[] buffer, int timeout, PingOptions? options)
        {
            using (Process p = GetPingProcess(address, buffer, timeout, options))
            {
                p.Start();
                if (!p.WaitForExit(timeout) || p.ExitCode == 1 || p.ExitCode == 2)
                {
                    return CreateTimedOutPingReply();
                }

                try
                {
                    string output = p.StandardOutput.ReadToEnd();
                    return ParsePingUtilityOutput(address, output);
                }
                catch (Exception)
                {
                    // If the standard output cannot be successfully parsed, throw a generic PingException.
                    throw new PingException(SR.net_ping);
                }
            }
        }

        private async Task<PingReply> SendWithPingUtilityAsync(IPAddress address, byte[] buffer, int timeout, PingOptions? options)
        {
            using (Process p = GetPingProcess(address, buffer, timeout, options))
            {
                var processCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                p.EnableRaisingEvents = true;
                p.Exited += (s, e) => processCompletion.SetResult();
                p.Start();

                try
                {
                    await processCompletion.Task.WaitAsync(TimeSpan.FromMilliseconds(timeout)).ConfigureAwait(false);
                }
                catch (TimeoutException)
                {
                    p.Kill();
                    return CreateTimedOutPingReply();
                }

                if (p.ExitCode == 1 || p.ExitCode == 2)
                {
                    // Throw timeout for known failure return codes from ping functions.
                    return CreateTimedOutPingReply();
                }

                try
                {
                    string output = await p.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
                    return ParsePingUtilityOutput(address, output);
                }
                catch (Exception)
                {
                    // If the standard output cannot be successfully parsed, throw a generic PingException.
                    throw new PingException(SR.net_ping);
                }
            }
        }

        private PingReply ParsePingUtilityOutput(IPAddress address, string output)
        {
            long rtt = UnixCommandLinePing.ParseRoundTripTime(output);
            return new PingReply(
                address,
                null, // Ping utility cannot accommodate these, return null to indicate they were ignored.
                IPStatus.Success,
                rtt,
                Array.Empty<byte>()); // Ping utility doesn't deliver this info.
        }
    }
}
