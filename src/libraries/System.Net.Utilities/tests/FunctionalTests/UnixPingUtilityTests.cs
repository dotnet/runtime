using System.Diagnostics;
using System.Net.Sockets;

using Xunit;

namespace System.Net.NetworkInformation.Tests
{
    // Contains a few basic validation tests to ensure that the local machine's ping utility
    // supports the types of options we need to use and formats its output in the way
    // that we expect it to in order to provide un-privileged Ping support on Unix.
    public class UnixPingUtilityTests
    {
        private const int IcmpHeaderLengthInBytes = 8;

        [System.Runtime.InteropServices.DllImport("libc")]
        private static extern void printf(string msg);
        private static void Print(string msg) { printf(Environment.NewLine + msg); }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(50)]
        [InlineData(1000)]
        [PlatformSpecific(PlatformID.AnyUnix)]
        public static void PacketSizeIsRespected(int payloadSize)
        {
            IPAddress localAddress = TestSettings.GetLocalIPAddress().Result;
            bool ipv4 = localAddress.AddressFamily == AddressFamily.InterNetwork;
            string arguments = UnixCommandLinePing.ConstructCommandLine(payloadSize, localAddress.ToString(), ipv4);
            string utilityPath = (localAddress.AddressFamily == AddressFamily.InterNetwork)
                ? UnixCommandLinePing.Ping4UtilityPath
                : UnixCommandLinePing.Ping6UtilityPath;


            ProcessStartInfo psi = new ProcessStartInfo(utilityPath, arguments);
            psi.RedirectStandardError = true;
            psi.RedirectStandardOutput = true;
            Process p = Process.Start(psi);
            p.WaitForExit(TestSettings.PingTimeout);
            string pingOutput = p.StandardOutput.ReadToEnd();
            // Validate that the returned data size is correct.
            // It should be equal to the bytes we sent plus the size of the ICMP header.
            int receivedBytes = ParseReturnedPacketSize(pingOutput);
            int expected = payloadSize + IcmpHeaderLengthInBytes;
            if (payloadSize < 16)
            {
                expected += 16 - payloadSize;
            }

            Assert.Equal(expected, receivedBytes);

            // Validate that we only sent one ping with the "-c 1" argument.
            int numPingsSent = ParseNumPingsSent(pingOutput);
            Assert.Equal(1, numPingsSent);

            long rtt = UnixCommandLinePing.ParseRoundTripTime(pingOutput);
            Assert.InRange(rtt, 0, long.MaxValue);
        }

        private static int ParseReturnedPacketSize(string pingOutput)
        {
            int indexOfBytesFrom = pingOutput.IndexOf("bytes from");
            int previousNewLine = pingOutput.LastIndexOf(Environment.NewLine, indexOfBytesFrom);
            string number = pingOutput.Substring(previousNewLine + 1, indexOfBytesFrom - previousNewLine - 1);
            int parsedReceivedBytes;
            if (!int.TryParse(number, out parsedReceivedBytes))
            {
                throw new InvalidOperationException("Couldn't parse the ping utility's output.");
            }

            return parsedReceivedBytes;
        }

        private static int ParseNumPingsSent(string pingOutput)
        {
            int indexOfPacketsTransmitted = pingOutput.IndexOf("packets transmitted");
            int previousNewLine = pingOutput.LastIndexOf(Environment.NewLine, indexOfPacketsTransmitted);
            string number = pingOutput.Substring(previousNewLine + 1, indexOfPacketsTransmitted - previousNewLine - 1);

            int parsedNumPings;
            if (!int.TryParse(number, out parsedNumPings))
            {
                throw new InvalidOperationException("Couldn't parse the ping utility's output.");
            }

            return parsedNumPings;
        }
    }
}
