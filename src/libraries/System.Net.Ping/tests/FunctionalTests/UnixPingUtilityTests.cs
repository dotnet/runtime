// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    public class UnixPingUtilityTests
    {
        private const int IcmpHeaderLengthInBytes = 8;

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(50)]
        [InlineData(1000)]
        [PlatformSpecific(TestPlatforms.AnyUnix)] // Tests un-priviledged Ping support on Unix
        public static async Task PacketSizeIsRespected(int payloadSize)
        {
            IPAddress localAddress = await TestSettings.GetLocalIPAddressAsync();
            bool ipv4 = localAddress.AddressFamily == AddressFamily.InterNetwork;
            string arguments = UnixCommandLinePing.ConstructCommandLine(payloadSize, localAddress.ToString(), ipv4);
            string utilityPath = (localAddress.AddressFamily == AddressFamily.InterNetwork)
                ? UnixCommandLinePing.Ping4UtilityPath
                : UnixCommandLinePing.Ping6UtilityPath;

            var p = new Process();
            p.StartInfo.FileName = utilityPath;
            p.StartInfo.Arguments = arguments;
            p.StartInfo.UseShellExecute = false;
            
            p.StartInfo.RedirectStandardOutput = true;
            var stdOutLines = new List<string>();
            p.OutputDataReceived += new DataReceivedEventHandler(
                delegate (object sendingProcess, DataReceivedEventArgs outputLine) { stdOutLines.Add(outputLine.Data); }); 

            p.StartInfo.RedirectStandardError = true;
            var stdErrLines = new List<string>();
            p.ErrorDataReceived += new DataReceivedEventHandler(
                delegate (object sendingProcess, DataReceivedEventArgs errorLine) { stdErrLines.Add(errorLine.Data); }); 

            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

            // There are multiple issues with ping6 in macOS 10.12 (Sierra), see https://github.com/dotnet/corefx/issues/26358.
            bool isPing6OnMacSierra = utilityPath.Equals(UnixCommandLinePing.Ping6UtilityPath) &&
                    RuntimeInformation.IsOSPlatform(OSPlatform.OSX) &&
                    !PlatformDetection.IsMacOsHighSierraOrHigher;

            string pingOutput;
            if (!p.WaitForExit(TestSettings.PingTimeout))
            {
                // Workaround known issues with ping6 in macOS 10.12
                if (isPing6OnMacSierra)
                    return;

                pingOutput = string.Join("\n", stdOutLines);
                string stdErr = string.Join("\n", stdErrLines);
                throw new Exception(
                    $"[{utilityPath} {arguments}] process did not exit in {TestSettings.PingTimeout} ms.\nStdOut:[{pingOutput}]\nStdErr:[{stdErr}]");
            }

            // Ensure standard output and error are flushed
            p.WaitForExit();

            pingOutput = string.Join("\n", stdOutLines);
            var exitCode = p.ExitCode;
            if (exitCode != 0)
            {
                // Workaround known issues with ping6 in macOS 10.12
                if (isPing6OnMacSierra)
                    return;

                string stdErr = string.Join("\n", stdErrLines);
                throw new Exception(
                    $"[{utilityPath} {arguments}] process exit code is {exitCode}.\nStdOut:[{pingOutput}]\nStdErr:[{stdErr}]");
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
                    $"Parse error for [{utilityPath} {arguments}] process exit code is {exitCode}.\nStdOut:[{pingOutput}]\nStdErr:[{stdErr}]", e);
            }
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
