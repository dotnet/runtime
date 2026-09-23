// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net
{
    // macOS DNS resolver implementation. Queries without explicit servers use
    // DNSServiceQueryRecord so macOS resolver policy remains authoritative; queries
    // with explicit servers use the unchanged managed PAL overloads. The array overloads
    // ensure DnsResolver calls route here first; casting selects the managed IList overload.
    internal static partial class DnsResolverPal
    {
        public static Task<DnsResult<AddressRecord>> ResolveAddresses(IPEndPoint[] servers, bool async, string name, AddressFamily addressFamily, CancellationToken cancellationToken)
            => servers.Length == 0
                ? Query<AddressRecord>(servers, async, name, AddressFamilyToQueryType(addressFamily), cancellationToken, DnsSdRecordParsing.TryParseAddress)
                : ResolveAddresses((IList<IPEndPoint>)servers, async, name, addressFamily, cancellationToken);

        public static Task<DnsResult<SrvRecord>> ResolveSrv(IPEndPoint[] servers, bool async, string name, CancellationToken cancellationToken)
            => servers.Length == 0
                ? Query<SrvRecord>(servers, async, name, Interop.Dnssd.kDNSServiceType_SRV, cancellationToken, DnsSdRecordParsing.TryParseSrv)
                : ResolveSrv((IList<IPEndPoint>)servers, async, name, cancellationToken);

        public static Task<DnsResult<MxRecord>> ResolveMx(IPEndPoint[] servers, bool async, string name, CancellationToken cancellationToken)
            => servers.Length == 0
                ? Query<MxRecord>(servers, async, name, Interop.Dnssd.kDNSServiceType_MX, cancellationToken, DnsSdRecordParsing.TryParseMx)
                : ResolveMx((IList<IPEndPoint>)servers, async, name, cancellationToken);

        public static Task<DnsResult<TxtRecord>> ResolveTxt(IPEndPoint[] servers, bool async, string name, CancellationToken cancellationToken)
            => servers.Length == 0
                ? Query<TxtRecord>(servers, async, name, Interop.Dnssd.kDNSServiceType_TXT, cancellationToken, DnsSdRecordParsing.TryParseTxt)
                : ResolveTxt((IList<IPEndPoint>)servers, async, name, cancellationToken);

        public static Task<DnsResult<CNameRecord>> ResolveCName(IPEndPoint[] servers, bool async, string name, CancellationToken cancellationToken)
            => servers.Length == 0
                ? Query<CNameRecord>(servers, async, name, Interop.Dnssd.kDNSServiceType_CNAME, cancellationToken, DnsSdRecordParsing.TryParseCName)
                : ResolveCName((IList<IPEndPoint>)servers, async, name, cancellationToken);

        public static Task<DnsResult<PtrRecord>> ResolvePtr(IPEndPoint[] servers, bool async, string name, CancellationToken cancellationToken)
            => servers.Length == 0
                ? Query<PtrRecord>(servers, async, name, Interop.Dnssd.kDNSServiceType_PTR, cancellationToken, DnsSdRecordParsing.TryParsePtr)
                : ResolvePtr((IList<IPEndPoint>)servers, async, name, cancellationToken);

        public static Task<DnsResult<NsRecord>> ResolveNs(IPEndPoint[] servers, bool async, string name, CancellationToken cancellationToken)
            => servers.Length == 0
                ? Query<NsRecord>(servers, async, name, Interop.Dnssd.kDNSServiceType_NS, cancellationToken, DnsSdRecordParsing.TryParseNs)
                : ResolveNs((IList<IPEndPoint>)servers, async, name, cancellationToken);

        private static ushort AddressFamilyToQueryType(AddressFamily addressFamily) =>
            addressFamily switch
            {
                AddressFamily.InterNetwork => Interop.Dnssd.kDNSServiceType_A,
                AddressFamily.InterNetworkV6 => Interop.Dnssd.kDNSServiceType_AAAA,
                _ => throw new ArgumentException(SR.net_dns_unsupported_address_family, nameof(addressFamily)),
            };

        private static Task<DnsResult<TRecord>> Query<TRecord>(
            IPEndPoint[] servers,
            bool async,
            string name,
            ushort queryType,
            CancellationToken cancellationToken,
            TryParseDnsSdRecord<TRecord> tryParse)
        {
            ValidateServers(servers);

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<DnsResult<TRecord>>(cancellationToken);
            }

            return QueryCore(name, queryType, async, cancellationToken, tryParse);
        }

        // The Linux managed PAL owns the DNS UDP/TCP socket end-to-end and reads bytes via
        // Socket.ReceiveAsync; async there is a plain socket read. Here mDNSResponder (a system
        // daemon) owns the socket. Its client library exposes only an fd via DNSServiceRefSockFD,
        // and the actual DNS wire read + parse + callback dispatch happen inside
        // DNSServiceProcessResult. So on async we await readability on the fd and then call
        // DNSServiceProcessResult synchronously to consume it. DNSServiceProcessResult blocks
        // until data is available for synchronous callers, so no separate polling is needed.
        private static async Task<DnsResult<TRecord>> QueryCore<TRecord>(
            string name,
            ushort queryType,
            bool async,
            CancellationToken cancellationToken,
            TryParseDnsSdRecord<TRecord> tryParse)
        {
            DnsSdQueryResult raw = await QueryRecord(name, queryType, async, cancellationToken).ConfigureAwait(false);
            return BuildResult(raw, queryType, tryParse);
        }

        private static DnsResult<TRecord> BuildResult<TRecord>(
            DnsSdQueryResult raw,
            ushort queryType,
            TryParseDnsSdRecord<TRecord> tryParse)
        {
            if (raw.ResponseCode != DnsResponseCode.NoError)
            {
                return new DnsResult<TRecord>(raw.ResponseCode, null, TimeSpan.Zero);
            }

            List<TRecord> records = new();
            foreach (DnsSdRecord rawRecord in raw.Records)
            {
                if (rawRecord.Type == queryType && tryParse(rawRecord, out TRecord parsed))
                {
                    records.Add(parsed);
                }
            }

            return new DnsResult<TRecord>(DnsResponseCode.NoError, records, TimeSpan.Zero);
        }

        private static async Task<DnsSdQueryResult> QueryRecord(string name, ushort queryType, bool async, CancellationToken cancellationToken)
        {
            DnsSdQueryState state = new(queryType);
            GCHandle<DnsSdQueryState> stateHandle = new(state);
            SafeDnsServiceHandle? dnsService = null;

            try
            {
                int status = StartQuery(name, queryType, stateHandle, out dnsService);
                if (status != Interop.Dnssd.kDNSServiceErr_NoError)
                {
                    return DnsSdQueryResult.FromStatus(status);
                }

                int fileDescriptor = Interop.Dnssd.DNSServiceRefSockFD(dnsService);
                if (fileDescriptor < 0)
                {
                    return DnsSdQueryResult.FromStatus(Interop.Dnssd.kDNSServiceErr_DefunctConnection);
                }

                using DnsSocket? readinessSocket = async ? new DnsSocket((IntPtr)fileDescriptor) : null;
                byte[] readinessBuffer = new byte[1];

                while (!state.IsComplete)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (async)
                    {
                        await readinessSocket!.ReceiveAsync(readinessBuffer, peek: true, cancellationToken).ConfigureAwait(false);
                    }
                    int processStatus = Interop.Dnssd.DNSServiceProcessResult(dnsService);
                    if (processStatus != Interop.Dnssd.kDNSServiceErr_NoError)
                    {
                        state.SetError(processStatus);
                    }
                }

                return state.ToResult();
            }
            finally
            {
                dnsService?.Dispose();
                stateHandle.Dispose();
            }
        }

        private static unsafe int StartQuery(string name, ushort queryType, GCHandle<DnsSdQueryState> stateHandle, out SafeDnsServiceHandle serviceRef) =>
            Interop.Dnssd.DNSServiceQueryRecord(
                out serviceRef,
                flags: Interop.Dnssd.kDNSServiceFlagsReturnIntermediates | Interop.Dnssd.kDNSServiceFlagsTimeout,
                interfaceIndex: 0,
                fullname: name,
                rrtype: queryType,
                rrclass: Interop.Dnssd.kDNSServiceClass_IN,
                callBack: &QueryRecordCallback,
                context: GCHandle<DnsSdQueryState>.ToIntPtr(stateHandle));

#pragma warning disable CS3016 // Arrays as attribute arguments is not CLS-compliant
        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
#pragma warning restore CS3016
        private static unsafe void QueryRecordCallback(
            IntPtr sdRef,
            uint flags,
            uint interfaceIndex,
            int errorCode,
            byte* fullname,
            ushort rrtype,
            ushort rrclass,
            ushort rdlen,
            void* rdata,
            uint ttl,
            IntPtr context)
        {
            DnsSdQueryState? state = null;
            try
            {
                state = GCHandle<DnsSdQueryState>.FromIntPtr(context).Target;
                state.OnRecord(flags, interfaceIndex, errorCode, rrtype, rrclass, rdlen, rdata, ttl);
            }
            catch (Exception ex)
            {
                state?.SetException(ex);
            }
        }

        private readonly struct DnsSdQueryResult
        {
            public DnsResponseCode ResponseCode { get; }
            public IReadOnlyList<DnsSdRecord> Records { get; }

            public DnsSdQueryResult(DnsResponseCode responseCode, IReadOnlyList<DnsSdRecord> records)
            {
                ResponseCode = responseCode;
                Records = records;
            }

            public static DnsSdQueryResult FromStatus(int status) =>
                new(MapDnsServiceErrorToResponseCode(status), Array.Empty<DnsSdRecord>());
        }

        private sealed unsafe class DnsSdQueryState
        {
            private readonly ushort _requestedType;
            private readonly List<DnsSdRecord> _records = new();
            private int _status = Interop.Dnssd.kDNSServiceErr_NoError;
            private Exception? _exception;

            public DnsSdQueryState(ushort requestedType)
            {
                _requestedType = requestedType;
            }

            public bool IsComplete { get; private set; }

            public void SetError(int status)
            {
                _status = status;
                IsComplete = true;
            }

            public void SetException(Exception exception)
            {
                _exception ??= exception;
                IsComplete = true;
            }

            public void OnRecord(uint flags, uint interfaceIndex, int errorCode, ushort rrtype, ushort rrclass, ushort rdlen, void* rdata, uint ttl)
            {
                if (errorCode != Interop.Dnssd.kDNSServiceErr_NoError)
                {
                    SetError(errorCode);
                    return;
                }

                if (rrclass != Interop.Dnssd.kDNSServiceClass_IN || rrtype != _requestedType)
                {
                    return;
                }

                if ((flags & Interop.Dnssd.kDNSServiceFlagsAdd) != 0 && rdata != null)
                {
                    // Best-effort TTL: DNS-SD may return the original TTL for cached answers.
                    _records.Add(new DnsSdRecord(rrtype, new ReadOnlySpan<byte>(rdata, rdlen).ToArray(), ttl, interfaceIndex));
                }

                if ((flags & Interop.Dnssd.kDNSServiceFlagsMoreComing) == 0)
                {
                    IsComplete = true;
                }
            }

            public DnsSdQueryResult ToResult()
            {
                Exception? exception = _exception;
                if (exception is not null)
                {
                    ExceptionDispatchInfo.Throw(exception);
                }

                DnsResponseCode responseCode = MapDnsServiceErrorToResponseCode(_status);

                return new DnsSdQueryResult(responseCode, _records);
            }
        }


        private static DnsResponseCode MapDnsServiceErrorToResponseCode(int status) =>
            status switch
            {
                Interop.Dnssd.kDNSServiceErr_NoError => DnsResponseCode.NoError,
                Interop.Dnssd.kDNSServiceErr_NoSuchName => DnsResponseCode.NxDomain,
                // DNSServiceQueryRecord reports NODATA as NoSuchRecord, and mDNSResponder
                // also uses that code for NXDOMAIN in practice. The callback does not expose
                // the authority section needed to distinguish them, so surface the collapsed
                // negative result as a successful response with no records.
                Interop.Dnssd.kDNSServiceErr_NoSuchRecord => DnsResponseCode.NoError,
                // With kDNSServiceFlagsTimeout, DNSServiceQueryRecord uses Timeout as the
                // terminal callback when the query times out.
                Interop.Dnssd.kDNSServiceErr_Timeout => DnsResponseCode.ServerFailure,
                Interop.Dnssd.kDNSServiceErr_BadParam => DnsResponseCode.FormatError,
                Interop.Dnssd.kDNSServiceErr_Unsupported => DnsResponseCode.NotImplemented,
                Interop.Dnssd.kDNSServiceErr_Refused => DnsResponseCode.Refused,
                Interop.Dnssd.kDNSServiceErr_PolicyDenied => DnsResponseCode.Refused,
                Interop.Dnssd.kDNSServiceErr_NotPermitted => DnsResponseCode.Refused,
                _ => DnsResponseCode.ServerFailure,
            };
    }

}
