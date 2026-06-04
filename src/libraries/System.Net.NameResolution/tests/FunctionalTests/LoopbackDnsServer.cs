// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.XUnitExtensions;

namespace System.Net.NameResolution.Tests
{
    /// <summary>
    /// A minimal in-process DNS server for testing. Listens on the loopback DNS port (53)
    /// and responds with preconfigured answers based on the query name and type.
    /// Self-contained: does not depend on any production DNS message types.
    /// </summary>
    /// <remarks>
    /// Windows' <c>DnsQueryEx</c> only ever contacts custom DNS servers on the standard
    /// port 53 (the sockaddr port field must be 0), so the loopback server must bind 53.
    /// Binding a privileged-looking low port does not require elevation on Windows, but
    /// the port may already be in use (e.g. a local DNS service), in which case
    /// <see cref="Start"/> throws <see cref="SkipTestException"/> so the test is skipped
    /// rather than failed.
    /// </remarks>
    internal sealed class LoopbackDnsServer : IAsyncDisposable
    {
        // DnsQueryEx always queries DNS servers on the standard port 53.
        public const int DnsPort = 53;

        private readonly UdpClient _udp;
        private readonly TcpListener _tcp;
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _udpListenTask;
        private readonly Task _tcpListenTask;
        private readonly Dictionary<(string Name, DnsRecordType Type), ResponseBuilder> _responses = new();
        private int _requestCount;

        public IPEndPoint EndPoint { get; }

        public int RequestCount => _requestCount;

        public int TcpRequestCount { get; private set; }

        private LoopbackDnsServer(UdpClient udp, TcpListener tcp, IPEndPoint endPoint)
        {
            _udp = udp;
            _tcp = tcp;
            EndPoint = endPoint;
            _udpListenTask = ListenUdpAsync(_cts.Token);
            _tcpListenTask = ListenTcpAsync(_cts.Token);
        }

        public static LoopbackDnsServer Start()
        {
            UdpClient udp;
            try
            {
                udp = new UdpClient(new IPEndPoint(IPAddress.Loopback, DnsPort));
            }
            catch (SocketException ex)
            {
                throw new SkipTestException(
                    $"Unable to bind loopback DNS port {DnsPort}; another DNS server may be running ({ex.SocketErrorCode}).");
            }

            IPEndPoint ep = (IPEndPoint)udp.Client.LocalEndPoint!;
            TcpListener tcp = new(IPAddress.Loopback, ep.Port);
            try
            {
                tcp.Start();
            }
            catch (SocketException ex)
            {
                udp.Dispose();
                throw new SkipTestException(
                    $"Unable to bind loopback DNS TCP port {DnsPort}; another DNS server may be running ({ex.SocketErrorCode}).");
            }

            return new LoopbackDnsServer(udp, tcp, ep);
        }

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
                    UdpReceiveResult result = await _udp.ReceiveAsync(ct);
                    Interlocked.Increment(ref _requestCount);

                    byte[] response = ProcessQuery(result.Buffer);
                    if (response.Length > 0)
                    {
                        await _udp.SendAsync(response, result.RemoteEndPoint, ct);
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (ObjectDisposedException) { }
        }

        private async Task ListenTcpAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    TcpClient client = await _tcp.AcceptTcpClientAsync(ct);
                    _ = HandleTcpClientAsync(client, ct);
                }
            }
            catch (OperationCanceledException) { }
            catch (ObjectDisposedException) { }
        }

        private async Task HandleTcpClientAsync(TcpClient client, CancellationToken ct)
        {
            try
            {
                using (client)
                {
                    NetworkStream stream = client.GetStream();

                    byte[] lengthBuf = new byte[2];
                    if (!await ReadExactlyAsync(stream, lengthBuf, ct))
                    {
                        return;
                    }

                    int queryLength = BinaryPrimitives.ReadUInt16BigEndian(lengthBuf);
                    byte[] query = new byte[queryLength];
                    if (!await ReadExactlyAsync(stream, query, ct))
                    {
                        return;
                    }

                    Interlocked.Increment(ref _requestCount);
                    TcpRequestCount++;

                    byte[] response = ProcessQuery(query, isTcp: true);
                    if (response.Length > 0)
                    {
                        byte[] responseLengthBuf = new byte[2];
                        BinaryPrimitives.WriteUInt16BigEndian(responseLengthBuf, (ushort)response.Length);
                        await stream.WriteAsync(responseLengthBuf, ct);
                        await stream.WriteAsync(response, ct);
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (ObjectDisposedException) { }
            catch (IOException) { }
        }

        private static async Task<bool> ReadExactlyAsync(NetworkStream stream, byte[] buffer, CancellationToken ct)
        {
            int read = 0;
            while (read < buffer.Length)
            {
                int n = await stream.ReadAsync(buffer.AsMemory(read), ct);
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
                return DnsResponseBuilder.For(queryId, [], 0)
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
                return DnsResponseBuilder.For(queryId, questionName, 0)
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
            _tcp.Stop();
            try { await _udpListenTask; } catch { }
            try { await _tcpListenTask; } catch { }
            _cts.Dispose();
        }
    }
}
