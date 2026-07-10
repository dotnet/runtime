// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.XUnitExtensions;

namespace System.Net.NameResolution.Tests
{
    /// <summary>
    /// A minimal in-process DNS server for testing. Listens on the provided port
    /// and responds with preconfigured answers based on the query name and type.
    /// Self-contained: does not depend on any production DNS message types.
    /// </summary>
    internal sealed class LoopbackDnsServer : IAsyncDisposable
    {
        private readonly Socket _udp;
        private readonly Socket _tcp;
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _udpListenTask;
        private readonly Task _tcpListenTask;
        private readonly ConcurrentDictionary<(string Name, DnsRecordType Type), ResponseBuilder> _responses = new();
        private int _requestCount;
        private int _tcpRequestCount;

        public IPEndPoint EndPoint { get; }

        public int RequestCount => _requestCount;

        public int TcpRequestCount => _tcpRequestCount;

        private LoopbackDnsServer(Socket udp, Socket tcp, IPEndPoint endPoint)
        {
            _udp = udp;
            _tcp = tcp;
            EndPoint = endPoint;
            _udpListenTask = ListenUdpAsync(_cts.Token);
            _tcpListenTask = ListenTcpAsync(_cts.Token);
        }

        public static LoopbackDnsServer Start(int port = 0)
        {
            // Start with TCP socket to get a free port, then bind UDP to the same port. This order is
            // more likely to succeed than the reverse, since UDP is connectionless and the socket does
            // not have a TIME_WAIT state.

            Socket tcp = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                tcp.Bind(new IPEndPoint(IPAddress.Loopback, port));
                tcp.Listen();
            }
            catch (SocketException ex)
            {
                tcp.Dispose();
                throw new SkipTestException(
                    $"Unable to bind loopback DNS TCP port {port}; another DNS server may be running ({ex.SocketErrorCode}).");
            }

            IPEndPoint ep = (IPEndPoint)tcp.LocalEndPoint!;
            Socket udp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            if (OperatingSystem.IsWindows())
            {
                // Disable ICMP "port unreachable" surfacing as WSAECONNRESET on ReceiveFrom
                const int SIO_UDP_CONNRESET = -1744830452;
                udp.IOControl(SIO_UDP_CONNRESET, new byte[] { 0 }, null);
            }
            try
            {
                udp.Bind(new IPEndPoint(IPAddress.Loopback, ep.Port));
            }
            catch (SocketException ex)
            {
                udp.Dispose();
                tcp.Dispose();
                throw new SkipTestException(
                    $"Unable to bind loopback DNS UDP port {ep.Port}; another DNS server may be running ({ex.SocketErrorCode}).");
            }

            return new LoopbackDnsServer(udp, tcp, ep);
        }

        public void ClearResponses() => _responses.Clear();

        public void AddResponse(string name, DnsRecordType type, Func<DnsResponseBuilder, DnsResponseBuilder> configure)
        {
            _responses[(name.ToLowerInvariant(), type)] = (queryId, qName, _) =>
                configure(DnsResponseBuilder.For(queryId, qName, type)).Build();
        }

        public void AddRawResponse(string name, DnsRecordType type, Func<ushort, byte[]> rawFactory)
        {
            _responses[(name.ToLowerInvariant(), type)] = (queryId, _, _) => rawFactory(queryId);
        }

        public delegate byte[] ResponseBuilder(ushort queryId, byte[] questionName, bool isTcp);

        private async Task ListenUdpAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    byte[] buffer = new byte[4096];
                    SocketReceiveFromResult result = await _udp.ReceiveFromAsync(
                        buffer, SocketFlags.None, new IPEndPoint(IPAddress.Loopback, 0), ct);
                    byte[] query = buffer[..result.ReceivedBytes];
                    EndPoint remote = result.RemoteEndPoint;
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            Interlocked.Increment(ref _requestCount);

                            byte[] response = ProcessQuery(query);
                            if (response.Length > 0)
                            {
                                await _udp.SendToAsync(response, SocketFlags.None, remote, ct);
                            }
                        }
                        catch (OperationCanceledException) { }
                        catch (ObjectDisposedException) { }
                        catch (SocketException) { }
                    });
                }
            }
            catch (OperationCanceledException) { }
            catch (ObjectDisposedException) { }
            catch (Exception ex)
            {
                Debug.Fail($"Unexpected exception in UDP listener: {ex}");
            }
        }

        private async Task ListenTcpAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    Socket client = await _tcp.AcceptAsync(ct);
                    _ = Task.Run(() => HandleTcpClientAsync(client, ct));
                }
            }
            catch (OperationCanceledException) { }
            catch (ObjectDisposedException) { }
            catch (Exception ex)
            {
                Debug.Fail($"Unexpected exception in TCP listener: {ex}");
            }
        }

        private async Task HandleTcpClientAsync(Socket client, CancellationToken ct)
        {
            try
            {
                using (client)
                {
                    byte[] lengthBuf = new byte[2];
                    if (!await ReadExactlyAsync(client, lengthBuf, ct))
                    {
                        return;
                    }

                    int queryLength = BinaryPrimitives.ReadUInt16BigEndian(lengthBuf);
                    byte[] query = new byte[queryLength];
                    if (!await ReadExactlyAsync(client, query, ct))
                    {
                        return;
                    }

                    Interlocked.Increment(ref _requestCount);
                    Interlocked.Increment(ref _tcpRequestCount);

                    byte[] response = ProcessQuery(query, isTcp: true);
                    if (response.Length > 0)
                    {
                        byte[] responseLengthBuf = new byte[2];
                        BinaryPrimitives.WriteUInt16BigEndian(responseLengthBuf, (ushort)response.Length);
                        await client.SendAsync(responseLengthBuf, SocketFlags.None, ct);
                        await client.SendAsync(response, SocketFlags.None, ct);
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (ObjectDisposedException) { }
            catch (SocketException) { }
        }

        private static async Task<bool> ReadExactlyAsync(Socket socket, byte[] buffer, CancellationToken ct)
        {
            int read = 0;
            while (read < buffer.Length)
            {
                int n = await socket.ReceiveAsync(buffer.AsMemory(read), SocketFlags.None, ct);
                if (n == 0)
                {
                    return false;
                }
                read += n;
            }
            return true;
        }

        private byte[] ProcessQuery(byte[] query, bool isTcp = false)
        {
            if (query.Length < 12)
            {
                return [];
            }

            ushort queryId = BinaryPrimitives.ReadUInt16BigEndian(query);
            ushort qdCount = BinaryPrimitives.ReadUInt16BigEndian(query.AsSpan(4));

            if (qdCount < 1)
            {
                return DnsResponseBuilder.For(queryId, [], default)
                    .ResponseCode(DnsResponseCode.FormatError)
                    .SkipQuestion()
                    .Build();
            }

            int pos = 12;
            int nameStart = pos;

            while (pos < query.Length)
            {
                byte b = query[pos];
                if (b == 0) { pos++; break; }
                if ((b & 0xC0) == 0xC0) { pos += 2; break; }
                pos += 1 + b;
            }

            byte[] questionName = query[nameStart..pos];

            if (pos + 4 > query.Length)
            {
                return DnsResponseBuilder.For(queryId, questionName, default)
                    .ResponseCode(DnsResponseCode.FormatError)
                    .Build();
            }

            DnsRecordType qType = (DnsRecordType)BinaryPrimitives.ReadUInt16BigEndian(query.AsSpan(pos));
            string nameStr = DecodeName(query, nameStart);

            if (_responses.TryGetValue((nameStr.ToLowerInvariant(), qType), out ResponseBuilder? builder))
            {
                return builder(queryId, questionName, isTcp);
            }

            // Default: NXDOMAIN
            return DnsResponseBuilder.For(queryId, questionName, qType)
                .ResponseCode(DnsResponseCode.NxDomain)
                .Build();
        }

        private static string DecodeName(byte[] message, int offset)
        {
            StringBuilder sb = new();
            int pos = offset;
            while (pos < message.Length)
            {
                byte len = message[pos];
                if (len == 0)
                {
                    break;
                }
                if ((len & 0xC0) == 0xC0)
                {
                    pos = ((len & 0x3F) << 8) | message[pos + 1];
                    continue;
                }
                pos++;
                if (sb.Length > 0)
                {
                    sb.Append('.');
                }
                sb.Append(Encoding.ASCII.GetString(message, pos, len));
                pos += len;
            }
            return sb.ToString();
        }

        public async ValueTask DisposeAsync()
        {
            _cts.Cancel();
            _udp.Dispose();
            _tcp.Dispose();
            try { await _udpListenTask; } catch { }
            try { await _tcpListenTask; } catch { }
            _cts.Dispose();
        }
    }
}
