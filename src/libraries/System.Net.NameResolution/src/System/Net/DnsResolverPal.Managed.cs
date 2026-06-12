// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net
{
    // Managed stub-resolver implementation of the DNS PAL for Unix platforms.
    //
    // Builds and parses DNS wire messages and talks to the configured servers over
    // UDP (with TCP fallback on truncation) using System.Net.Sockets. When no servers
    // are configured, the system servers from /etc/resolv.conf are used.
    //
    // Each entry point takes a `bool async` flag. When async is false the underlying
    // socket operations are issued synchronously (blocking) and the returned Task is
    // already completed, so the synchronous public entry points can unwrap it without
    // blocking a thread pool thread.
    internal static partial class DnsResolverPal
    {
        // Maximum UDP DNS message size without EDNS0 (RFC 1035 §4.2.1).
        private const int MaxUdpResponseSize = 512;

        // Initial buffer size for TCP responses; grown based on the 2-byte length prefix.
        private const int InitialTcpBufferSize = 4096;

        // Default per-attempt timeout and retry count (DnsResolverOptions exposes only Servers).
        private static readonly TimeSpan s_queryTimeout = TimeSpan.FromSeconds(3);
        private const int MaxRetries = 2;

        // ---- Public PAL entry points (one per record type) ----

        public static async Task<DnsResult<AddressRecord>> ResolveAddresses(IList<IPEndPoint> servers, bool async, string name, AddressFamily addressFamily, CancellationToken cancellationToken)
        {
            if (addressFamily == AddressFamily.Unspecified)
            {
                if (async)
                {
                    Task<DnsResult<AddressRecord>> aTask = QueryAddresses(servers, async: true, name, DnsRecordType.A, cancellationToken);
                    Task<DnsResult<AddressRecord>> aaaaTask = QueryAddresses(servers, async: true, name, DnsRecordType.AAAA, cancellationToken);
                    DnsResult<AddressRecord> aRes = await aTask.ConfigureAwait(false);
                    DnsResult<AddressRecord> aaaaRes = await aaaaTask.ConfigureAwait(false);
                    return MergeAddressResults(aRes, aaaaRes);
                }
                else
                {
                    DnsResult<AddressRecord> aRes = await QueryAddresses(servers, async: false, name, DnsRecordType.A, cancellationToken).ConfigureAwait(false);
                    DnsResult<AddressRecord> aaaaRes = await QueryAddresses(servers, async: false, name, DnsRecordType.AAAA, cancellationToken).ConfigureAwait(false);
                    return MergeAddressResults(aRes, aaaaRes);
                }
            }

            DnsRecordType qtype = AddressFamilyToQueryType(addressFamily);
            return await QueryAddresses(servers, async, name, qtype, cancellationToken).ConfigureAwait(false);
        }

        public static async Task<DnsResult<SrvRecord>> ResolveSrv(IList<IPEndPoint> servers, bool async, string name, CancellationToken cancellationToken)
        {
            DnsResponse response = await SendQuery(servers, async, name, DnsRecordType.SRV, cancellationToken).ConfigureAwait(false);
            try
            {
                return ParseSrv(response.Span);
            }
            finally
            {
                response.Dispose();
            }
        }

        public static async Task<DnsResult<MxRecord>> ResolveMx(IList<IPEndPoint> servers, bool async, string name, CancellationToken cancellationToken)
        {
            DnsResponse response = await SendQuery(servers, async, name, DnsRecordType.MX, cancellationToken).ConfigureAwait(false);
            try
            {
                return ParseMx(response.Span);
            }
            finally
            {
                response.Dispose();
            }
        }

        public static async Task<DnsResult<TxtRecord>> ResolveTxt(IList<IPEndPoint> servers, bool async, string name, CancellationToken cancellationToken)
        {
            DnsResponse response = await SendQuery(servers, async, name, DnsRecordType.TXT, cancellationToken).ConfigureAwait(false);
            try
            {
                return ParseTxt(response.Span);
            }
            finally
            {
                response.Dispose();
            }
        }

        public static async Task<DnsResult<CNameRecord>> ResolveCName(IList<IPEndPoint> servers, bool async, string name, CancellationToken cancellationToken)
        {
            DnsResponse response = await SendQuery(servers, async, name, DnsRecordType.CNAME, cancellationToken).ConfigureAwait(false);
            try
            {
                return ParseCName(response.Span);
            }
            finally
            {
                response.Dispose();
            }
        }

        public static async Task<DnsResult<PtrRecord>> ResolvePtr(IList<IPEndPoint> servers, bool async, string name, CancellationToken cancellationToken)
        {
            DnsResponse response = await SendQuery(servers, async, name, DnsRecordType.PTR, cancellationToken).ConfigureAwait(false);
            try
            {
                return ParsePtr(response.Span);
            }
            finally
            {
                response.Dispose();
            }
        }

        public static async Task<DnsResult<NsRecord>> ResolveNs(IList<IPEndPoint> servers, bool async, string name, CancellationToken cancellationToken)
        {
            DnsResponse response = await SendQuery(servers, async, name, DnsRecordType.NS, cancellationToken).ConfigureAwait(false);
            try
            {
                return ParseNs(response.Span);
            }
            finally
            {
                response.Dispose();
            }
        }

        private static async Task<DnsResult<AddressRecord>> QueryAddresses(IList<IPEndPoint> servers, bool async, string name, DnsRecordType qtype, CancellationToken cancellationToken)
        {
            DnsResponse response = await SendQuery(servers, async, name, qtype, cancellationToken).ConfigureAwait(false);
            try
            {
                return ParseAddresses(response.Span, qtype);
            }
            finally
            {
                response.Dispose();
            }
        }

        private static DnsRecordType AddressFamilyToQueryType(AddressFamily addressFamily) =>
            addressFamily switch
            {
                AddressFamily.InterNetwork => DnsRecordType.A,
                AddressFamily.InterNetworkV6 => DnsRecordType.AAAA,
                _ => throw new ArgumentException(SR.net_invalid_ip_addr, nameof(addressFamily)),
            };

        // ---- Response parsers ----

        private static DnsResult<AddressRecord> ParseAddresses(ReadOnlySpan<byte> response, DnsRecordType qtype)
        {
            DnsMessageReader reader = CreateReader(response);
            DnsMessageHeader header = reader.Header;
            if (header.ResponseCode != DnsResponseCode.NoError)
            {
                return new DnsResult<AddressRecord>(header.ResponseCode, null, ExtractNegativeCacheTtl(response));
            }

            SkipQuestions(ref reader);

            List<AddressRecord> records = new List<AddressRecord>();
            for (int i = 0; i < header.AnswerCount; i++)
            {
                DnsRecord record = ReadRecord(ref reader);
                if (qtype == DnsRecordType.A && record.TryParseARecord(out DnsARecordData a))
                {
                    records.Add(new AddressRecord(a.ToIPAddress(), TimeSpan.FromSeconds(record.TimeToLive)));
                }
                else if (qtype == DnsRecordType.AAAA && record.TryParseAAAARecord(out DnsAAAARecordData aaaa))
                {
                    records.Add(new AddressRecord(aaaa.ToIPAddress(), TimeSpan.FromSeconds(record.TimeToLive)));
                }
            }

            // NODATA: NoError with no matching records — extract negative TTL from SOA
            // in the authority section per RFC 2308 §5.
            TimeSpan negTtl = records.Count == 0 ? ExtractNegativeCacheTtl(response) : TimeSpan.Zero;
            return new DnsResult<AddressRecord>(DnsResponseCode.NoError, records, negTtl);
        }

        private static DnsResult<SrvRecord> ParseSrv(ReadOnlySpan<byte> response)
        {
            DnsMessageReader reader = CreateReader(response);
            DnsMessageHeader header = reader.Header;
            if (header.ResponseCode != DnsResponseCode.NoError)
            {
                return new DnsResult<SrvRecord>(header.ResponseCode, null, ExtractNegativeCacheTtl(response));
            }

            SkipQuestions(ref reader);

            // First pass: collect SRV answers (target names captured eagerly as strings).
            List<(string Target, ushort Port, ushort Priority, ushort Weight, uint Ttl)> srvs = new();
            for (int i = 0; i < header.AnswerCount; i++)
            {
                DnsRecord record = ReadRecord(ref reader);
                if (record.TryParseSrvRecord(out DnsSrvRecordData srv))
                {
                    srvs.Add((srv.Target.ToString(), srv.Port, srv.Priority, srv.Weight, record.TimeToLive));
                }
            }

            // Skip the authority section.
            SkipRecords(ref reader, header.AuthorityCount);

            // Gather additional-section A/AAAA glue addresses keyed by owner name.
            Dictionary<string, List<AddressRecord>>? glue = null;
            for (int i = 0; i < header.AdditionalCount; i++)
            {
                DnsRecord record = ReadRecord(ref reader);
                IPAddress? address = null;
                if (record.TryParseARecord(out DnsARecordData a))
                {
                    address = a.ToIPAddress();
                }
                else if (record.TryParseAAAARecord(out DnsAAAARecordData aaaa))
                {
                    address = aaaa.ToIPAddress();
                }

                if (address is not null)
                {
                    string owner = record.Name.ToString();
                    glue ??= new Dictionary<string, List<AddressRecord>>(StringComparer.OrdinalIgnoreCase);
                    if (!glue.TryGetValue(owner, out List<AddressRecord>? list))
                    {
                        list = new List<AddressRecord>();
                        glue[owner] = list;
                    }
                    list.Add(new AddressRecord(address, TimeSpan.FromSeconds(record.TimeToLive)));
                }
            }

            List<SrvRecord> records = new List<SrvRecord>(srvs.Count);
            foreach ((string target, ushort port, ushort priority, ushort weight, uint ttl) in srvs)
            {
                IReadOnlyList<AddressRecord>? attached = null;
                if (glue is not null && glue.TryGetValue(target, out List<AddressRecord>? list))
                {
                    attached = list;
                }
                records.Add(new SrvRecord(target, port, priority, weight, TimeSpan.FromSeconds(ttl), attached));
            }

            TimeSpan negTtl = records.Count == 0 ? ExtractNegativeCacheTtl(response) : TimeSpan.Zero;
            return new DnsResult<SrvRecord>(DnsResponseCode.NoError, records, negTtl);
        }

        private static DnsResult<TxtRecord> ParseTxt(ReadOnlySpan<byte> response)
        {
            DnsMessageReader reader = CreateReader(response);
            DnsMessageHeader header = reader.Header;
            if (header.ResponseCode != DnsResponseCode.NoError)
            {
                return new DnsResult<TxtRecord>(header.ResponseCode, null, ExtractNegativeCacheTtl(response));
            }

            SkipQuestions(ref reader);

            List<TxtRecord> records = new List<TxtRecord>();
            for (int i = 0; i < header.AnswerCount; i++)
            {
                DnsRecord record = ReadRecord(ref reader);
                if (record.TryParseTxtRecord(out DnsTxtRecordData txt))
                {
                    List<string> values = new List<string>();
                    foreach (ReadOnlySpan<byte> str in txt.EnumerateStrings())
                    {
                        values.Add(Encoding.UTF8.GetString(str));
                    }
                    records.Add(new TxtRecord(values, TimeSpan.FromSeconds(record.TimeToLive)));
                }
            }

            TimeSpan txtNegTtl = records.Count == 0 ? ExtractNegativeCacheTtl(response) : TimeSpan.Zero;
            return new DnsResult<TxtRecord>(DnsResponseCode.NoError, records, txtNegTtl);
        }

        private static DnsResult<MxRecord> ParseMx(ReadOnlySpan<byte> response)
        {
            DnsMessageReader reader = CreateReader(response);
            DnsMessageHeader header = reader.Header;
            if (header.ResponseCode != DnsResponseCode.NoError)
            {
                return new DnsResult<MxRecord>(header.ResponseCode, null, ExtractNegativeCacheTtl(response));
            }

            SkipQuestions(ref reader);

            List<MxRecord> records = new List<MxRecord>();
            for (int i = 0; i < header.AnswerCount; i++)
            {
                DnsRecord record = ReadRecord(ref reader);
                if (record.TryParseMxRecord(out DnsMxRecordData mx))
                {
                    records.Add(new MxRecord(mx.Exchange.ToString(), mx.Preference, TimeSpan.FromSeconds(record.TimeToLive)));
                }
            }

            TimeSpan mxNegTtl = records.Count == 0 ? ExtractNegativeCacheTtl(response) : TimeSpan.Zero;
            return new DnsResult<MxRecord>(DnsResponseCode.NoError, records, mxNegTtl);
        }

        private static DnsResult<CNameRecord> ParseCName(ReadOnlySpan<byte> response)
        {
            DnsMessageReader reader = CreateReader(response);
            DnsMessageHeader header = reader.Header;
            if (header.ResponseCode != DnsResponseCode.NoError)
            {
                return new DnsResult<CNameRecord>(header.ResponseCode, null, ExtractNegativeCacheTtl(response));
            }

            SkipQuestions(ref reader);

            List<CNameRecord> records = new List<CNameRecord>();
            for (int i = 0; i < header.AnswerCount; i++)
            {
                DnsRecord record = ReadRecord(ref reader);
                if (record.TryParseCNameRecord(out DnsCNameRecordData cname))
                {
                    records.Add(new CNameRecord(cname.CName.ToString(), TimeSpan.FromSeconds(record.TimeToLive)));
                }
            }

            TimeSpan cnameNegTtl = records.Count == 0 ? ExtractNegativeCacheTtl(response) : TimeSpan.Zero;
            return new DnsResult<CNameRecord>(DnsResponseCode.NoError, records, cnameNegTtl);
        }

        private static DnsResult<PtrRecord> ParsePtr(ReadOnlySpan<byte> response)
        {
            DnsMessageReader reader = CreateReader(response);
            DnsMessageHeader header = reader.Header;
            if (header.ResponseCode != DnsResponseCode.NoError)
            {
                return new DnsResult<PtrRecord>(header.ResponseCode, null, ExtractNegativeCacheTtl(response));
            }

            SkipQuestions(ref reader);

            List<PtrRecord> records = new List<PtrRecord>();
            for (int i = 0; i < header.AnswerCount; i++)
            {
                DnsRecord record = ReadRecord(ref reader);
                if (record.TryParsePtrRecord(out DnsPtrRecordData ptr))
                {
                    records.Add(new PtrRecord(ptr.Name.ToString(), TimeSpan.FromSeconds(record.TimeToLive)));
                }
            }

            TimeSpan ptrNegTtl = records.Count == 0 ? ExtractNegativeCacheTtl(response) : TimeSpan.Zero;
            return new DnsResult<PtrRecord>(DnsResponseCode.NoError, records, ptrNegTtl);
        }

        private static DnsResult<NsRecord> ParseNs(ReadOnlySpan<byte> response)
        {
            DnsMessageReader reader = CreateReader(response);
            DnsMessageHeader header = reader.Header;
            if (header.ResponseCode != DnsResponseCode.NoError)
            {
                return new DnsResult<NsRecord>(header.ResponseCode, null, ExtractNegativeCacheTtl(response));
            }

            SkipQuestions(ref reader);

            List<NsRecord> records = new List<NsRecord>();
            for (int i = 0; i < header.AnswerCount; i++)
            {
                DnsRecord record = ReadRecord(ref reader);
                if (record.TryParseNsRecord(out DnsNsRecordData ns))
                {
                    records.Add(new NsRecord(ns.Name.ToString(), TimeSpan.FromSeconds(record.TimeToLive)));
                }
            }

            TimeSpan nsNegTtl = records.Count == 0 ? ExtractNegativeCacheTtl(response) : TimeSpan.Zero;
            return new DnsResult<NsRecord>(DnsResponseCode.NoError, records, nsNegTtl);
        }

        private static DnsResult<AddressRecord> MergeAddressResults(DnsResult<AddressRecord> a, DnsResult<AddressRecord> b)
        {
            if (a.Records.Count > 0 || b.Records.Count > 0)
            {
                AddressRecord[] merged = new AddressRecord[a.Records.Count + b.Records.Count];
                int idx = 0;
                for (int i = 0; i < a.Records.Count; i++)
                {
                    merged[idx++] = a.Records[i];
                }
                for (int i = 0; i < b.Records.Count; i++)
                {
                    merged[idx++] = b.Records[i];
                }
                return new DnsResult<AddressRecord>(DnsResponseCode.NoError, merged, TimeSpan.Zero);
            }

            DnsResponseCode chosenRc = a.ResponseCode == DnsResponseCode.NxDomain || b.ResponseCode == DnsResponseCode.NxDomain
                ? DnsResponseCode.NxDomain
                : (a.ResponseCode != DnsResponseCode.NoError ? a.ResponseCode : b.ResponseCode);
            TimeSpan negTtl = a.NegativeCacheTtl > TimeSpan.Zero ? a.NegativeCacheTtl : b.NegativeCacheTtl;
            return new DnsResult<AddressRecord>(chosenRc, null, negTtl);
        }

        // Per RFC 2308 §5, the negative cache TTL is the minimum of the SOA record TTL
        // and the SOA MINIMUM field of the SOA record in the authority section.
        private static TimeSpan ExtractNegativeCacheTtl(ReadOnlySpan<byte> response)
        {
            DnsMessageReader reader = CreateReader(response);
            DnsMessageHeader header = reader.Header;

            SkipQuestions(ref reader);
            SkipRecords(ref reader, header.AnswerCount);

            for (int i = 0; i < header.AuthorityCount; i++)
            {
                DnsRecord record = ReadRecord(ref reader);
                if (record.TryParseSoaRecord(out DnsSoaRecordData soa))
                {
                    uint negTtl = Math.Min(record.TimeToLive, soa.MinimumTtl);
                    return TimeSpan.FromSeconds(negTtl);
                }
            }

            return TimeSpan.Zero;
        }

        // ---- Query engine ----

        private static async Task<DnsResponse> SendQuery(IList<IPEndPoint> servers, bool async, string name, DnsRecordType qtype, CancellationToken cancellationToken)
        {
            IReadOnlyList<IPEndPoint> serverList = GetServers(servers);
            Debug.Assert(serverList.Count > 0);

            byte[] queryBytes = ArrayPool<byte>.Shared.Rent(MaxUdpResponseSize);
            try
            {
                ushort queryId = (ushort)RandomNumberGenerator.GetInt32(ushort.MaxValue + 1);
                int queryLength = WriteQuery(queryId, name, qtype, queryBytes);
                ReadOnlyMemory<byte> query = queryBytes.AsMemory(0, queryLength);

                byte[] responseBuffer = ArrayPool<byte>.Shared.Rent(MaxUdpResponseSize);
                Exception? lastException = null;

                foreach (IPEndPoint server in serverList)
                {
                    for (int attempt = 0; attempt <= MaxRetries; attempt++)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            // Surface pre-flight cancellation as TaskCanceledException to match the
                            // Windows PAL (which completes via TaskCompletionSource.TrySetCanceled).
                            ArrayPool<byte>.Shared.Return(responseBuffer);
                            throw new TaskCanceledException();
                        }
                        try
                        {
                            int responseLength = async
                                ? await SendUdpQueryAsync(query, server, responseBuffer, cancellationToken).ConfigureAwait(false)
                                : SendUdpQuerySync(query, server, responseBuffer);

                            ResponseValidation validation = ValidateResponse(
                                responseBuffer.AsSpan(0, responseLength), queryId, name, qtype, out Exception? validationError);

                            if (validation == ResponseValidation.Retry)
                            {
                                lastException = validationError;
                                continue;
                            }

                            if (validation == ResponseValidation.TcpFallback)
                            {
                                (byte[]? tcpBuffer, int tcpLength, Exception? tcpError) = async
                                    ? await TryTcpFallbackAsync(query, server, cancellationToken).ConfigureAwait(false)
                                    : TryTcpFallbackSync(query, server);

                                if (tcpBuffer is not null)
                                {
                                    // Validate the TCP response (ID, QR bit, echoed question).
                                    ResponseValidation tcpValidation = ValidateResponse(
                                        tcpBuffer.AsSpan(0, tcpLength), queryId, name, qtype, out Exception? tcpValidationError);
                                    if (tcpValidation != ResponseValidation.Ok)
                                    {
                                        ArrayPool<byte>.Shared.Return(tcpBuffer);
                                        lastException = tcpValidationError ?? new InvalidDataException();
                                        continue;
                                    }

                                    ArrayPool<byte>.Shared.Return(responseBuffer);
                                    return new DnsResponse(tcpBuffer, tcpLength);
                                }

                                lastException = tcpError;
                                continue;
                            }

                            return new DnsResponse(responseBuffer, responseLength);
                        }
                        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                        {
                            ArrayPool<byte>.Shared.Return(responseBuffer);
                            throw;
                        }
                        catch (OperationCanceledException)
                        {
                            lastException = new TimeoutException();
                        }
                        catch (SocketException ex)
                        {
                            lastException = ex;
                        }
                        catch (IOException ex)
                        {
                            lastException = ex;
                        }
                    }
                }

                ArrayPool<byte>.Shared.Return(responseBuffer);

                if (lastException is not null)
                {
                    ExceptionDispatchInfo.Throw(lastException);
                }
                throw new TimeoutException();
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(queryBytes);
            }
        }

        private enum ResponseValidation
        {
            Ok,
            Retry,
            TcpFallback,
        }

        private static ResponseValidation ValidateResponse(
            ReadOnlySpan<byte> response, ushort expectedId, string expectedName, DnsRecordType expectedType,
            out Exception? error)
        {
            error = null;

            if (!DnsMessageHeader.TryRead(response, out DnsMessageHeader header))
            {
                error = new InvalidDataException();
                return ResponseValidation.Retry;
            }

            if (!header.IsResponse || header.Id != expectedId)
            {
                return ResponseValidation.Retry;
            }

            if (!ValidateResponseQuestion(response, header, expectedName, expectedType))
            {
                error = new InvalidDataException();
                return ResponseValidation.Retry;
            }

            if ((header.Flags & DnsHeaderFlags.Truncation) != 0)
            {
                return ResponseValidation.TcpFallback;
            }

            return ResponseValidation.Ok;
        }

        private static bool ValidateResponseQuestion(
            ReadOnlySpan<byte> response, DnsMessageHeader header, string expectedName, DnsRecordType expectedType)
        {
            if (header.QuestionCount != 1)
            {
                return false;
            }

            DnsMessageReader.TryCreate(response, out DnsMessageReader reader);
            if (!reader.TryReadQuestion(out DnsQuestion question))
            {
                return false;
            }

            return question.Type == expectedType && question.Name.Equals(expectedName);
        }

        private static unsafe int WriteQuery(ushort queryId, string name, DnsRecordType type, Span<byte> destination)
        {
            Span<byte> nameBuffer = stackalloc byte[DnsEncodedName.MaxEncodedLength];
            OperationStatus status = DnsEncodedName.TryEncode(name, nameBuffer, out DnsEncodedName encodedName, out _);
            if (status == OperationStatus.InvalidData)
            {
                throw new ArgumentException(SR.Format(SR.net_invalid_dns_name, name), nameof(name));
            }
            Debug.Assert(status == OperationStatus.Done);

            DnsMessageWriter writer = new DnsMessageWriter(destination);
            bool ok = writer.TryWriteHeader(new DnsMessageHeader { Id = queryId, Flags = DnsHeaderFlags.RecursionDesired, QuestionCount = 1 });
            Debug.Assert(ok);
            ok = writer.TryWriteQuestion(encodedName, type);
            Debug.Assert(ok);
            return writer.BytesWritten;
        }

        private static async Task<int> SendUdpQueryAsync(
            ReadOnlyMemory<byte> query, IPEndPoint server, byte[] responseBuffer, CancellationToken cancellationToken)
        {
            using Socket socket = new Socket(server.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(s_queryTimeout);

            await socket.ConnectAsync(server, timeoutCts.Token).ConfigureAwait(false);
            await socket.SendAsync(query, SocketFlags.None, timeoutCts.Token).ConfigureAwait(false);
            return await socket.ReceiveAsync(responseBuffer, SocketFlags.None, timeoutCts.Token).ConfigureAwait(false);
        }

        private static int SendUdpQuerySync(
            ReadOnlyMemory<byte> query, IPEndPoint server, byte[] responseBuffer)
        {
            using Socket socket = new Socket(server.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            socket.SendTimeout = (int)s_queryTimeout.TotalMilliseconds;
            socket.ReceiveTimeout = (int)s_queryTimeout.TotalMilliseconds;

            socket.Connect(server);
            socket.Send(query.Span, SocketFlags.None);
            return socket.Receive(responseBuffer, SocketFlags.None);
        }

        private static async Task<(byte[]? Buffer, int Length, Exception? Error)> TryTcpFallbackAsync(
            ReadOnlyMemory<byte> query, IPEndPoint server, CancellationToken cancellationToken)
        {
            try
            {
                (byte[] buffer, int length) = await SendTcpQueryAsync(query, server, cancellationToken).ConfigureAwait(false);
                return (buffer, length, null);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                return (null, 0, new TimeoutException());
            }
            catch (Exception ex) when (ex is SocketException or IOException)
            {
                return (null, 0, ex);
            }
        }

        private static (byte[]? Buffer, int Length, Exception? Error) TryTcpFallbackSync(
            ReadOnlyMemory<byte> query, IPEndPoint server)
        {
            try
            {
                (byte[] buffer, int length) = SendTcpQuerySync(query, server);
                return (buffer, length, null);
            }
            catch (Exception ex) when (ex is SocketException or IOException)
            {
                return (null, 0, ex);
            }
        }

        private static async Task<(byte[] Buffer, int Length)> SendTcpQueryAsync(
            ReadOnlyMemory<byte> query, IPEndPoint server, CancellationToken cancellationToken)
        {
            using Socket socket = new Socket(server.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(s_queryTimeout);

            await socket.ConnectAsync(server, timeoutCts.Token).ConfigureAwait(false);

            byte[] buffer = ArrayPool<byte>.Shared.Rent(InitialTcpBufferSize);
            try
            {
                BinaryPrimitives.WriteUInt16BigEndian(buffer, (ushort)query.Length);
                await SendExactAsync(socket, buffer.AsMemory(0, 2), timeoutCts.Token).ConfigureAwait(false);
                await SendExactAsync(socket, query, timeoutCts.Token).ConfigureAwait(false);

                await ReceiveExactAsync(socket, buffer.AsMemory(0, 2), timeoutCts.Token).ConfigureAwait(false);
                int responseLength = BinaryPrimitives.ReadUInt16BigEndian(buffer);

                if (responseLength > buffer.Length)
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                    buffer = ArrayPool<byte>.Shared.Rent(responseLength);
                }

                await ReceiveExactAsync(socket, buffer.AsMemory(0, responseLength), timeoutCts.Token).ConfigureAwait(false);
                return (buffer, responseLength);
            }
            catch
            {
                ArrayPool<byte>.Shared.Return(buffer);
                throw;
            }
        }

        private static (byte[] Buffer, int Length) SendTcpQuerySync(
            ReadOnlyMemory<byte> query, IPEndPoint server)
        {
            using Socket socket = new Socket(server.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.SendTimeout = (int)s_queryTimeout.TotalMilliseconds;
            socket.ReceiveTimeout = (int)s_queryTimeout.TotalMilliseconds;

            // Connect with explicit timeout to prevent unbounded blocking when
            // the server's TCP endpoint is unreachable.
            IAsyncResult ar = socket.BeginConnect(server, null, null);
            if (!ar.AsyncWaitHandle.WaitOne(s_queryTimeout))
            {
                socket.Close();
                throw new SocketException((int)SocketError.TimedOut);
            }
            socket.EndConnect(ar);

            byte[] buffer = ArrayPool<byte>.Shared.Rent(InitialTcpBufferSize);
            try
            {
                BinaryPrimitives.WriteUInt16BigEndian(buffer, (ushort)query.Length);
                SendExactSync(socket, buffer.AsSpan(0, 2));
                SendExactSync(socket, query.Span);

                ReceiveExactSync(socket, buffer.AsSpan(0, 2));
                int responseLength = BinaryPrimitives.ReadUInt16BigEndian(buffer);

                if (responseLength > buffer.Length)
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                    buffer = ArrayPool<byte>.Shared.Rent(responseLength);
                }

                ReceiveExactSync(socket, buffer.AsSpan(0, responseLength));
                return (buffer, responseLength);
            }
            catch
            {
                ArrayPool<byte>.Shared.Return(buffer);
                throw;
            }
        }

        private static async Task ReceiveExactAsync(Socket socket, Memory<byte> buffer, CancellationToken cancellationToken)
        {
            int totalReceived = 0;
            while (totalReceived < buffer.Length)
            {
                int received = await socket.ReceiveAsync(buffer[totalReceived..], SocketFlags.None, cancellationToken).ConfigureAwait(false);
                if (received == 0)
                {
                    ThrowMalformedResponse();
                }
                totalReceived += received;
            }
        }

        private static void ReceiveExactSync(Socket socket, Span<byte> buffer)
        {
            int totalReceived = 0;
            while (totalReceived < buffer.Length)
            {
                int received = socket.Receive(buffer.Slice(totalReceived), SocketFlags.None);
                if (received == 0)
                {
                    ThrowMalformedResponse();
                }
                totalReceived += received;
            }
        }

        private static async Task SendExactAsync(Socket socket, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            int totalSent = 0;
            while (totalSent < buffer.Length)
            {
                int sent = await socket.SendAsync(buffer[totalSent..], SocketFlags.None, cancellationToken).ConfigureAwait(false);
                if (sent == 0)
                {
                    throw new IOException();
                }
                totalSent += sent;
            }
        }

        private static void SendExactSync(Socket socket, ReadOnlySpan<byte> buffer)
        {
            int totalSent = 0;
            while (totalSent < buffer.Length)
            {
                int sent = socket.Send(buffer.Slice(totalSent), SocketFlags.None);
                if (sent == 0)
                {
                    throw new IOException();
                }
                totalSent += sent;
            }
        }

        private static IReadOnlyList<IPEndPoint> GetServers(IList<IPEndPoint> servers)
        {
            if (servers.Count > 0)
            {
                // A port of 0 means "use the default DNS port" (53).
                // Avoid allocating if all ports are already non-zero.
                bool needsNormalization = false;
                for (int i = 0; i < servers.Count; i++)
                {
                    if (servers[i].Port == 0)
                    {
                        needsNormalization = true;
                        break;
                    }
                }

                if (!needsNormalization)
                {
                    // The IList may already be an array or List; wrap in a read-only view.
                    if (servers is IReadOnlyList<IPEndPoint> readOnlyServers)
                    {
                        return readOnlyServers;
                    }
                    IPEndPoint[] copy = new IPEndPoint[servers.Count];
                    servers.CopyTo(copy, 0);
                    return copy;
                }

                IPEndPoint[] resolved = new IPEndPoint[servers.Count];
                for (int i = 0; i < servers.Count; i++)
                {
                    IPEndPoint server = servers[i];
                    resolved[i] = server.Port == 0 ? new IPEndPoint(server.Address, ResolvConf.DefaultDnsPort) : server;
                }
                return resolved;
            }

            List<IPEndPoint> systemServers = ResolvConf.GetNameServers();
            if (systemServers.Count > 0)
            {
                return systemServers;
            }

            return new IPEndPoint[] { new IPEndPoint(IPAddress.Loopback, ResolvConf.DefaultDnsPort) };
        }

        // ---- Message reading helpers ----

        private static DnsMessageReader CreateReader(ReadOnlySpan<byte> response)
        {
            if (!DnsMessageReader.TryCreate(response, out DnsMessageReader reader))
            {
                ThrowMalformedResponse();
            }
            return reader;
        }

        private static void SkipQuestions(ref DnsMessageReader reader)
        {
            for (int i = 0; i < reader.Header.QuestionCount; i++)
            {
                if (!reader.TryReadQuestion(out _))
                {
                    ThrowMalformedResponse();
                }
            }
        }

        private static DnsRecord ReadRecord(ref DnsMessageReader reader)
        {
            if (!reader.TryReadRecord(out DnsRecord record))
            {
                ThrowMalformedResponse();
            }
            return record;
        }

        private static void SkipRecords(ref DnsMessageReader reader, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (!reader.TryReadRecord(out _))
                {
                    ThrowMalformedResponse();
                }
            }
        }

        [DoesNotReturn]
        private static void ThrowMalformedResponse() =>
            throw new InvalidDataException();

        // Holds a response message buffer rented from the shared ArrayPool.
        private readonly struct DnsResponse : IDisposable
        {
            private readonly byte[] _buffer;
            private readonly int _length;

            public DnsResponse(byte[] buffer, int length)
            {
                _buffer = buffer;
                _length = length;
            }

            public ReadOnlySpan<byte> Span => _buffer.AsSpan(0, _length);

            public void Dispose() => ArrayPool<byte>.Shared.Return(_buffer);
        }
    }
}
