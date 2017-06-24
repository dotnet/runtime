// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Diagnostics;
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
        private const int IcmpHeaderLengthInBytes = 8;
        private const int MinIpHeaderLengthInBytes = 20;
        private const int MaxIpHeaderLengthInBytes = 60;
        [ThreadStatic]
        private static Random t_idGenerator;

        private async Task<PingReply> SendPingAsyncCore(IPAddress address, byte[] buffer, int timeout, PingOptions options)
        {
            try
            {
                Task<PingReply> t = RawSocketPermissions.CanUseRawSockets(address.AddressFamily) ?
                    SendIcmpEchoRequestOverRawSocket(address, buffer, timeout, options) :
                    SendWithPingUtility(address, buffer, timeout, options);
                PingReply reply = await t.ConfigureAwait(false);
                if (_canceled)
                {
                    throw new OperationCanceledException();
                }
                return reply;
            }
            finally
            {
                Finish();
            }
        }

        private async Task<PingReply> SendIcmpEchoRequestOverRawSocket(IPAddress address, byte[] buffer, int timeout, PingOptions options)
        {
            EndPoint endPoint = new IPEndPoint(address, 0);

            bool isIpv4 = address.AddressFamily == AddressFamily.InterNetwork;
            ProtocolType protocolType = isIpv4 ? ProtocolType.Icmp : ProtocolType.IcmpV6;

            // Use a random value as the identifier.  This doesn't need to be perfectly random
            // or very unpredictable, rather just good enough to avoid unexpected conflicts.
            Random rand = t_idGenerator ?? (t_idGenerator = new Random());
            ushort identifier = (ushort)rand.Next((int)ushort.MaxValue + 1);

            IcmpHeader header = new IcmpHeader()
            {
                Type = isIpv4 ? (byte)IcmpV4MessageType.EchoRequest : (byte)IcmpV6MessageType.EchoRequest,
                Code = 0,
                HeaderChecksum = 0,
                Identifier = identifier,
                SequenceNumber = 0,
            };

            byte[] sendBuffer = CreateSendMessageBuffer(header, buffer);

            using (Socket socket = new Socket(address.AddressFamily, SocketType.Raw, protocolType))
            {
                socket.ReceiveTimeout = timeout;
                socket.SendTimeout = timeout;
                // Setting Socket.DontFragment and .Ttl is not supported on Unix, so ignore the PingOptions parameter.

                int ipHeaderLength = isIpv4 ? MinIpHeaderLengthInBytes : 0;
                await socket.SendToAsync(new ArraySegment<byte>(sendBuffer), SocketFlags.None, endPoint).ConfigureAwait(false);
                byte[] receiveBuffer = new byte[MaxIpHeaderLengthInBytes + IcmpHeaderLengthInBytes + buffer.Length];

                long elapsed;
                Stopwatch sw = Stopwatch.StartNew();
                // Read from the socket in a loop. We may receive messages that are not echo replies, or that are not in response
                // to the echo request we just sent. We need to filter such messages out, and continue reading until our timeout.
                // For example, when pinging the local host, we need to filter out our own echo requests that the socket reads.
                while ((elapsed = sw.ElapsedMilliseconds) < timeout)
                {
                    Task<SocketReceiveFromResult> receiveTask = socket.ReceiveFromAsync(
                                                                    new ArraySegment<byte>(receiveBuffer),
                                                                    SocketFlags.None,
                                                                    endPoint);
                    var cts = new CancellationTokenSource();
                    Task finished = await Task.WhenAny(receiveTask, Task.Delay(timeout - (int)elapsed, cts.Token)).ConfigureAwait(false);
                    cts.Cancel();
                    if (finished != receiveTask)
                    {
                        sw.Stop();
                        return CreateTimedOutPingReply();
                    }

                    SocketReceiveFromResult receiveResult = receiveTask.GetAwaiter().GetResult();
                    int bytesReceived = receiveResult.ReceivedBytes;
                    if (bytesReceived - ipHeaderLength < IcmpHeaderLengthInBytes)
                    {
                        continue; // Not enough bytes to reconstruct IP header + ICMP header.
                    }

                    byte type, code;
                    unsafe
                    {
                        fixed (byte* bytesPtr = &receiveBuffer[0])
                        {
                            if (isIpv4)
                            {
                                // Determine actual size of IP header
                                byte ihl = (byte)(bytesPtr[0] & 0x0f); // Internet Header Length
                                ipHeaderLength = 4 * ihl;
                                if (bytesReceived - ipHeaderLength < IcmpHeaderLengthInBytes)
                                {
                                    continue; // Not enough bytes to reconstruct actual IP header + ICMP header.
                                }
                            }

                            int icmpHeaderOffset = ipHeaderLength;
                            IcmpHeader receivedHeader = *((IcmpHeader*)(bytesPtr + icmpHeaderOffset)); // Skip IP header.
                            type = receivedHeader.Type;
                            code = receivedHeader.Code;

                            if (identifier != receivedHeader.Identifier
                                || type == (byte)IcmpV4MessageType.EchoRequest
                                || type == (byte)IcmpV6MessageType.EchoRequest) // Echo Request, ignore
                            {
                                continue;
                            }
                        }
                    }

                    sw.Stop();
                    long roundTripTime = sw.ElapsedMilliseconds;
                    int dataOffset = ipHeaderLength + IcmpHeaderLengthInBytes;
                    // We want to return a buffer with the actual data we sent out, not including the header data.
                    byte[] dataBuffer = new byte[bytesReceived - dataOffset];
                    Buffer.BlockCopy(receiveBuffer, dataOffset, dataBuffer, 0, dataBuffer.Length);

                    IPStatus status = isIpv4
                                        ? IcmpV4MessageConstants.MapV4TypeToIPStatus(type, code)
                                        : IcmpV6MessageConstants.MapV6TypeToIPStatus(type, code);

                    return new PingReply(address, options, status, roundTripTime, dataBuffer);
                }

                // We have exceeded our timeout duration, and no reply has been received.
                sw.Stop();
                return CreateTimedOutPingReply();
            }
        }

        private async Task<PingReply> SendWithPingUtility(IPAddress address, byte[] buffer, int timeout, PingOptions options)
        {
            bool isIpv4 = address.AddressFamily == AddressFamily.InterNetwork;
            string pingExecutable = isIpv4 ? UnixCommandLinePing.Ping4UtilityPath : UnixCommandLinePing.Ping6UtilityPath;
            if (pingExecutable == null)
            {
                throw new PlatformNotSupportedException(SR.net_ping_utility_not_found);
            }

            string processArgs = UnixCommandLinePing.ConstructCommandLine(buffer.Length, address.ToString(), isIpv4);
            ProcessStartInfo psi = new ProcessStartInfo(pingExecutable, processArgs);
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            Process p = new Process() { StartInfo = psi };

            var processCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            p.EnableRaisingEvents = true;
            p.Exited += (s, e) => processCompletion.SetResult(true);
            p.Start();
            var cts = new CancellationTokenSource();
            Task timeoutTask = Task.Delay(timeout, cts.Token);

            Task finished = await Task.WhenAny(processCompletion.Task, timeoutTask).ConfigureAwait(false);
            if (finished == timeoutTask && !p.HasExited)
            {
                // Try to kill the ping process if it didn't return. If it is already in the process of exiting, a Win32Exception will be thrown.
                try
                {
                    p.Kill();
                }
                catch (Win32Exception) { }
                return CreateTimedOutPingReply();
            }
            else
            {
                cts.Cancel();
                if (p.ExitCode == 1 || p.ExitCode == 2)
                {
                    // Throw timeout for known failure return codes from ping functions.
                    return CreateTimedOutPingReply();
                }

                try
                {
                    string output = await p.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
                    long rtt = UnixCommandLinePing.ParseRoundTripTime(output);
                    return new PingReply(
                            address,
                            null, // Ping utility cannot accommodate these, return null to indicate they were ignored.
                            IPStatus.Success,
                            rtt,
                            Array.Empty<byte>()); // Ping utility doesn't deliver this info.
                }
                catch (Exception)
                {
                    // If the standard output cannot be successfully parsed, throw a generic PingException.
                    throw new PingException(SR.net_ping);
                }
            }
        }

        private PingReply CreateTimedOutPingReply()
        {
            // Documentation indicates that you should only pay attention to the IPStatus value when
            // its value is not "Success", but the rest of these values match that of the Windows implementation.
            return new PingReply(new IPAddress(0), null, IPStatus.TimedOut, 0, Array.Empty<byte>());
        }

#if DEBUG
        static Ping()
        {
            Debug.Assert(Marshal.SizeOf<IcmpHeader>() == 8, "The size of an ICMP Header must be 8 bytes.");
        }
#endif

        // Must be 8 bytes total.
        [StructLayout(LayoutKind.Sequential)]
        internal struct IcmpHeader
        {
            public byte Type;
            public byte Code;
            public ushort HeaderChecksum;
            public ushort Identifier;
            public ushort SequenceNumber;
        }

        private static unsafe byte[] CreateSendMessageBuffer(IcmpHeader header, byte[] payload)
        {
            int headerSize = sizeof(IcmpHeader);
            byte[] result = new byte[headerSize + payload.Length];
            Marshal.Copy(new IntPtr(&header), result, 0, headerSize);
            payload.CopyTo(result, headerSize);
            ushort checksum = ComputeBufferChecksum(result);
            // Jam the checksum into the buffer.
            result[2] = (byte)(checksum >> 8);
            result[3] = (byte)(checksum & (0xFF));

            return result;
        }

        private static ushort ComputeBufferChecksum(byte[] buffer)
        {
            // This is using the "deferred carries" approach outlined in RFC 1071.
            uint sum = 0;
            for (int i = 0; i < buffer.Length; i += 2)
            {
                // Combine each pair of bytes into a 16-bit number and add it to the sum
                ushort element0 = (ushort)((buffer[i] << 8) & 0xFF00);
                ushort element1 = (i + 1 < buffer.Length)
                                    ? (ushort)(buffer[i + 1] & 0x00FF)
                                    : (ushort)0; // If there's an odd number of bytes, pad by one octet of zeros.
                ushort combined = (ushort)(element0 | element1);
                sum += (uint)combined;
            }

            // Add back the "carry bits" which have risen to the upper 16 bits of the sum.
            while ((sum >> 16) != 0)
            {
                var partialSum = sum & 0xFFFF;
                var carries = sum >> 16;
                sum = partialSum + carries;
            }

            return unchecked((ushort)~sum);
        }
    }
}
