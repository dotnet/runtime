// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.NetworkInformation.Tests
{
    // Contains a few basic validation tests to ensure that the local machine's ping utility
    // supports the types of options we need to use and formats its output in the way
    // that we expect it to in order to provide un-privileged Ping support on Unix.
    [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst, "Ping process is not available on iOS/tvOS/MacCatalyst")]
    public class UnixPingUtilityTests
    {
        private const int IcmpHeaderLengthInBytes = 8;

        [Theory]
        [InlineData(0)]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(1500)]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public static void TimeoutIsRespected(int timeout)
        {
            Process p = ConstructPingProcess(IPAddress.Parse(TestSettings.UnreachableAddress), 50, timeout);
            //suppress Ping output to console/terminal stdout during test execution
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.RedirectStandardOutput = true;

            Stopwatch stopWatch = Stopwatch.StartNew();

            p.Start();
            p.WaitForExit();

            //ensure that the process takes longer than or equal to 'timeout'
            Assert.True(stopWatch.ElapsedMilliseconds >= timeout);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(50)]
        [InlineData(1000)]
        [PlatformSpecific(TestPlatforms.AnyUnix)] // Tests un-priviledged Ping support on Unix
        public static async Task PacketSizeIsRespected(int payloadSize)
        {
            var stdOutLines = new List<string>();
            var stdErrLines = new List<string>();

            Process p = ConstructPingProcess(await TestSettings.GetLocalIPAddressAsync(), payloadSize, 1000);
            p.StartInfo.RedirectStandardOutput = true;
            p.OutputDataReceived += delegate (object sendingProcess, DataReceivedEventArgs outputLine)
            {
                stdOutLines.Add(outputLine.Data);
            };

            p.StartInfo.RedirectStandardError = true;
            p.ErrorDataReceived += delegate (object sendingProcess, DataReceivedEventArgs errorLine)
            {
                stdErrLines.Add(errorLine.Data);
            };

            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

            string pingOutput;
            if (!p.WaitForExit(TestSettings.PingTimeout))
            {
                pingOutput = string.Join("\n", stdOutLines);
                string stdErr = string.Join("\n", stdErrLines);
                throw new Exception(
                    $"[{p.StartInfo.FileName} {p.StartInfo.Arguments}] process did not exit in {TestSettings.PingTimeout} ms.\nStdOut:[{pingOutput}]\nStdErr:[{stdErr}]");
            }

            // Ensure standard output and error are flushed
            p.WaitForExit();

            pingOutput = string.Join("\n", stdOutLines);
            var exitCode = p.ExitCode;
            if (exitCode != 0)
            {
                string stdErr = string.Join("\n", stdErrLines);
                throw new Exception(
                    $"[{p.StartInfo.FileName} {p.StartInfo.Arguments}] process exit code is {exitCode}.\nStdOut:[{pingOutput}]\nStdErr:[{stdErr}]");
            }

            try
            {
                // Validate that the returned data size is correct.
                // It should be equal to the bytes we sent plus the size of the ICMP header.
                int receivedBytes = ParseReturnedPacketSize(pingOutput);
                int expected = Math.Max(16, payloadSize) + IcmpHeaderLengthInBytes;
                Assert.Equal(expected, receivedBytes);

                // Validate that we only sent one ping with the "-c 1" argument.
                int numPingsSent = ParseNumPingsSent(pingOutput);
                Assert.Equal(1, numPingsSent);

                long rtt = UnixCommandLinePing.ParseRoundTripTime(pingOutput);
                Assert.InRange(rtt, 0, long.MaxValue);
            }
            catch (Exception e)
            {
                string stdErr = string.Join("\n", stdErrLines);
                throw new Exception(
                    $"Parse error for [{p.StartInfo.FileName} {p.StartInfo.Arguments}] process exit code is {exitCode}.\nStdOut:[{pingOutput}]\nStdErr:[{stdErr}]", e);
            }
        }

        private static Process ConstructPingProcess(IPAddress localAddress, int payloadSize, int timeout)
        {
            bool ipv4 = localAddress.AddressFamily == AddressFamily.InterNetwork;
            string arguments = UnixCommandLinePing.ConstructCommandLine(payloadSize, timeout, localAddress.ToString(), ipv4);
            string utilityPath = (localAddress.AddressFamily == AddressFamily.InterNetwork)
                ? UnixCommandLinePing.Ping4UtilityPath
                : UnixCommandLinePing.Ping6UtilityPath;

            var p = new Process();
            p.StartInfo.FileName = utilityPath;
            p.StartInfo.Arguments = arguments;
            p.StartInfo.UseShellExecute = false;

            return p;
        }

        private static int ParseReturnedPacketSize(string pingOutput)
        {
            int indexOfBytesFrom = pingOutput.IndexOf("bytes from");
            int previousNewLine = pingOutput.LastIndexOf(Environment.NewLine, indexOfBytesFrom);
            string number = pingOutput.Substring(previousNewLine + 1, indexOfBytesFrom - previousNewLine - 1);
            return int.Parse(number);
        }

        private static int ParseNumPingsSent(string pingOutput)
        {
            int indexOfPacketsTransmitted = pingOutput.IndexOf("packets transmitted");
            int previousNewLine = pingOutput.LastIndexOf(Environment.NewLine, indexOfPacketsTransmitted);
            string number = pingOutput.Substring(previousNewLine + 1, indexOfPacketsTransmitted - previousNewLine - 1);
            return int.Parse(number);
        }
    }
}
