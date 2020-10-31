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
        private const int IcmpHeaderLengthInBytes = 8;
        private const int MinIpHeaderLengthInBytes = 20;
        private const int MaxIpHeaderLengthInBytes = 60;
        private static bool SendIpHeader => OperatingSystem.IsMacOS();
        private static bool NeedsConnect => OperatingSystem.IsLinux();
        [ThreadStatic]
        private static Random? t_idGenerator;

        private PingReply SendPingCore(IPAddress address, byte[] buffer, int timeout, PingOptions? options)
        {
            PingReply reply = RawSocketPermissions.CanUseRawSockets(address.AddressFamily) ?
                    SendIcmpEchoRequestOverRawSocket(address, buffer, timeout, options) :
                    SendWithPingUtility(address, buffer, timeout, options);
            return reply;
        }

        private async Task<PingReply> SendPingAsyncCore(IPAddress address, byte[] buffer, int timeout, PingOptions? options)
        {
            Task<PingReply> t = RawSocketPermissions.CanUseRawSockets(address.AddressFamily) ?
                    SendIcmpEchoRequestOverRawSocketAsync(address, buffer, timeout, options) :
                    SendWithPingUtilityAsync(address, buffer, timeout, options);

            PingReply reply = await t.ConfigureAwait(false);

            if (_canceled)
            {
                throw new OperationCanceledException();
            }

            return reply;
        }

        private unsafe SocketConfig GetSocketConfig(IPAddress address, byte[] buffer, int timeout, PingOptions? options)
        {
            // Use a random value as the identifier. This doesn't need to be perfectly random
            // or very unpredictable, rather just good enough to avoid unexpected conflicts.
            Random rand = t_idGenerator ??= new Random();
            ushort id = (ushort)rand.Next(ushort.MaxValue + 1);
            IpHeader iph = default;

            bool ipv4 = address.AddressFamily == AddressFamily.InterNetwork;
            bool sendIpHeader = ipv4 && options != null && SendIpHeader;

             if (sendIpHeader)
             {
                iph.VersionAndLength = 0x45;
                // On OSX this strangely must be host byte order.
                iph.TotalLength = (ushort)(sizeof(IpHeader) + checked(sizeof(IcmpHeader) +  buffer.Length));
                iph.Protocol = 1; // ICMP
                iph.Ttl = (byte)options!.Ttl;
                iph.Flags = (ushort)(options.DontFragment ? 0x4000 : 0);
#pragma warning disable 618
                iph.DestinationAddress = (uint)address.Address;
#pragma warning restore 618
                // No need to fill in SourceAddress or checksum.
                // If left blank, kernel will fill it in - at least on OSX.
             }

            return new SocketConfig(
                new IPEndPoint(address, 0), timeout, options,
                ipv4, ipv4 ? ProtocolType.Icmp : ProtocolType.IcmpV6, id,
                CreateSendMessageBuffer(iph, new IcmpHeader()
                {
                    Type = ipv4 ? (byte)IcmpV4MessageType.EchoRequest : (byte)IcmpV6MessageType.EchoRequest,
                    Identifier = id,
                }, buffer));
        }

        private Socket GetRawSocket(SocketConfig socketConfig)
        {
            IPEndPoint ep = (IPEndPoint)socketConfig.EndPoint;
            AddressFamily addrFamily = ep.Address.AddressFamily;

            Socket socket = new Socket(addrFamily, SocketType.Raw, socketConfig.ProtocolType);
            socket.ReceiveTimeout = socketConfig.Timeout;
            socket.SendTimeout = socketConfig.Timeout;
            if (addrFamily == AddressFamily.InterNetworkV6 && OperatingSystem.IsMacOS())
            {
                socket.DualMode = false;
            }

            if (socketConfig.Options != null && socketConfig.Options.Ttl > 0)
            {
                socket.Ttl = (short)socketConfig.Options.Ttl;
            }

            if (socketConfig.Options != null && addrFamily == AddressFamily.InterNetwork)
            {
                if (SendIpHeader)
                {
                    // some platforms like OSX don't support DontFragment so we construct IP header instead.
                    socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.HeaderIncluded, 1);
                }
                else
                {
                    socket.DontFragment = socketConfig.Options.DontFragment;
                }
            }

#pragma warning disable 618
            // Disable warning about obsolete property. We could use GetAddressBytes but that allocates.
            // IPv4 multicast address starts with 1110 bits so mask rest and test if we get correct value e.g. 0xe0.
            if (NeedsConnect && !ep.Address.IsIPv6Multicast && !(addrFamily == AddressFamily.InterNetwork && (ep.Address.Address & 0xf0) == 0xe0))
            {
                // If it is not multicast, use Connect to scope responses only to the target address.
                socket.Connect(socketConfig.EndPoint);
            }
#pragma warning restore 618

            return socket;
        }

        private bool TryGetPingReply(
            SocketConfig socketConfig, byte[] receiveBuffer, int bytesReceived, Stopwatch sw, ref int ipHeaderLength,
            [NotNullWhen(true)] out PingReply? reply)
        {
            byte type, code;
            reply = null;

            if (socketConfig.IsIpv4)
            {
                // Determine actual size of IP header
                byte ihl = (byte)(receiveBuffer[0] & 0x0f); // Internet Header Length
                ipHeaderLength = 4 * ihl;
                if (bytesReceived - ipHeaderLength < IcmpHeaderLengthInBytes)
                {
                    return false; // Not enough bytes to reconstruct actual IP header + ICMP header.
                }
            }

            int icmpHeaderOffset = ipHeaderLength;

            // Skip IP header.
            IcmpHeader receivedHeader = MemoryMarshal.Read<IcmpHeader>(receiveBuffer.AsSpan(icmpHeaderOffset));
            type = receivedHeader.Type;
            code = receivedHeader.Code;

            if (socketConfig.Identifier != receivedHeader.Identifier
                || type == (byte)IcmpV4MessageType.EchoRequest
                || type == (byte)IcmpV6MessageType.EchoRequest) // Echo Request, ignore
            {
                return false;
            }

            sw.Stop();
            long roundTripTime = sw.ElapsedMilliseconds;
            int dataOffset = ipHeaderLength + IcmpHeaderLengthInBytes;
            // We want to return a buffer with the actual data we sent out, not including the header data.
            byte[] dataBuffer = new byte[bytesReceived - dataOffset];
            Buffer.BlockCopy(receiveBuffer, dataOffset, dataBuffer, 0, dataBuffer.Length);

            IPStatus status = socketConfig.IsIpv4
                ? IcmpV4MessageConstants.MapV4TypeToIPStatus(type, code)
                : IcmpV6MessageConstants.MapV6TypeToIPStatus(type, code);

            IPAddress address = ((IPEndPoint)socketConfig.EndPoint).Address;
            reply = new PingReply(address, socketConfig.Options, status, roundTripTime, dataBuffer);
            return true;
        }

        private PingReply SendIcmpEchoRequestOverRawSocket(IPAddress address, byte[] buffer, int timeout, PingOptions? options)
        {
            SocketConfig socketConfig = GetSocketConfig(address, buffer, timeout, options);
            using (Socket socket = GetRawSocket(socketConfig))
            {
                int ipHeaderLength = socketConfig.IsIpv4 ? MinIpHeaderLengthInBytes : 0;
                try
                {
                    socket.SendTo(socketConfig.SendBuffer, SocketFlags.None, socketConfig.EndPoint);
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
                {
                    return CreateTimedOutPingReply();
                }

                byte[] receiveBuffer = new byte[MaxIpHeaderLengthInBytes + IcmpHeaderLengthInBytes + buffer.Length];

                long elapsed;
                Stopwatch sw = Stopwatch.StartNew();
                // Read from the socket in a loop. We may receive messages that are not echo replies, or that are not in response
                // to the echo request we just sent. We need to filter such messages out, and continue reading until our timeout.
                // For example, when pinging the local host, we need to filter out our own echo requests that the socket reads.
                while ((elapsed = sw.ElapsedMilliseconds) < timeout)
                {
                    int bytesReceived;
                    try
                    {
                        bytesReceived = socket.ReceiveFrom(receiveBuffer, SocketFlags.None, ref socketConfig.EndPoint);
                    }
                    catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
                    {
                        return CreateTimedOutPingReply();
                    }

                    if (bytesReceived - ipHeaderLength < IcmpHeaderLengthInBytes)
                    {
                        continue; // Not enough bytes to reconstruct IP header + ICMP header.
                    }

                    if (TryGetPingReply(socketConfig, receiveBuffer, bytesReceived, sw, ref ipHeaderLength, out PingReply? reply))
                    {
                        return reply;
                    }
                }

                // We have exceeded our timeout duration, and no reply has been received.
                return CreateTimedOutPingReply();
            }
        }

        private async Task<PingReply> SendIcmpEchoRequestOverRawSocketAsync(IPAddress address, byte[] buffer, int timeout, PingOptions? options)
        {
            SocketConfig socketConfig = GetSocketConfig(address, buffer, timeout, options);
            using (Socket socket = GetRawSocket(socketConfig))
            {
                int ipHeaderLength = socketConfig.IsIpv4 ? MinIpHeaderLengthInBytes : 0;

                await socket.SendToAsync(
                    new ArraySegment<byte>(socketConfig.SendBuffer),
                    SocketFlags.None, socketConfig.EndPoint)
                    .ConfigureAwait(false);

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
                        socketConfig.EndPoint);

                    var cts = new CancellationTokenSource();
                    Task finished = await Task.WhenAny(receiveTask, Task.Delay(timeout - (int)elapsed, cts.Token)).ConfigureAwait(false);
                    cts.Cancel();
                    if (finished != receiveTask)
                    {
                        return CreateTimedOutPingReply();
                    }

                    SocketReceiveFromResult receiveResult = receiveTask.GetAwaiter().GetResult();
                    int bytesReceived = receiveResult.ReceivedBytes;
                    if (bytesReceived - ipHeaderLength < IcmpHeaderLengthInBytes)
                    {
                        continue; // Not enough bytes to reconstruct IP header + ICMP header.
                    }

                    if (TryGetPingReply(socketConfig, receiveBuffer, bytesReceived, sw, ref ipHeaderLength, out PingReply? reply))
                    {
                        return reply;
                    }
                }

                // We have exceeded our timeout duration, and no reply has been received.
                return CreateTimedOutPingReply();
            }
        }

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

                var cts = new CancellationTokenSource();
                Task timeoutTask = Task.Delay(timeout, cts.Token);
                Task finished = await Task.WhenAny(processCompletion.Task, timeoutTask).ConfigureAwait(false);

                if (finished == timeoutTask)
                {
                    p.Kill();
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
                        return ParsePingUtilityOutput(address, output);
                    }
                    catch (Exception)
                    {
                        // If the standard output cannot be successfully parsed, throw a generic PingException.
                        throw new PingException(SR.net_ping);
                    }
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

        [StructLayout(LayoutKind.Sequential)]
        internal struct IpHeader
        {
            internal byte VersionAndLength;
            internal byte Tos;
            internal ushort TotalLength;

            internal ushort Identifier;
            internal ushort Flags;

            internal byte Ttl;
            internal byte Protocol;
            internal ushort HeaderChecksum;

            internal uint SourceAddress;
            internal uint DestinationAddress;
        };

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

        // Since this is private should be safe to trust that the calling code
        // will behave. To get a little performance boost raw fields are exposed
        // and no validation is performed.
        private class SocketConfig
        {
            public SocketConfig(EndPoint endPoint, int timeout, PingOptions? options, bool isIPv4, ProtocolType protocolType, ushort id, byte[] sendBuffer)
            {
                EndPoint = endPoint;
                Timeout = timeout;
                Options = options;
                IsIpv4 = isIPv4;
                ProtocolType = protocolType;
                Identifier = id;
                SendBuffer = sendBuffer;
            }

            public EndPoint EndPoint;
            public readonly int Timeout;
            public readonly PingOptions? Options;
            public readonly ushort Identifier;
            public readonly bool IsIpv4;
            public readonly ProtocolType ProtocolType;
            public readonly byte[] SendBuffer;
        }

        private static unsafe byte[] CreateSendMessageBuffer(IpHeader ipHeader, IcmpHeader icmpHeader, byte[] payload)
        {
            int icmpHeaderSize = sizeof(IcmpHeader);
            int offset = 0;
            int packetSize = ipHeader.TotalLength != 0 ? ipHeader.TotalLength : checked(icmpHeaderSize + payload.Length);
            byte[] result = new byte[packetSize];

            if (ipHeader.TotalLength != 0)
            {
                int ipHeaderSize = sizeof(IpHeader);
                new Span<byte>(&ipHeader, sizeof(IpHeader)).CopyTo(result);
                offset = ipHeaderSize;
            }

            //byte[] result = new byte[headerSize + payload.Length];
            Marshal.Copy(new IntPtr(&icmpHeader), result, offset, icmpHeaderSize);
            payload.CopyTo(result, offset + icmpHeaderSize);

            // offset now still points to beginning of ICMP header.
            ushort checksum = ComputeBufferChecksum(result.AsSpan().Slice(offset));
            // Jam the checksum into the buffer.
            result[offset + 2] = (byte)(checksum >> 8);
            result[offset + 3] = (byte)(checksum & (0xFF));

            return result;
        }

        private static ushort ComputeBufferChecksum(ReadOnlySpan<byte> buffer)
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
