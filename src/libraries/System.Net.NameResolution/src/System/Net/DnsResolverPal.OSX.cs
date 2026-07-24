// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.Net
{
    // macOS DNS resolver implementation. Queries without explicit servers use
    // DNSServiceQueryRecord so macOS resolver policy remains authoritative; queries
    // with explicit servers use the unchanged managed PAL overloads. The array overloads
    // ensure DnsResolver calls route here first; casting selects the managed IList overload.
    internal static partial class DnsResolverPal
    {
        private const int PollTimeoutMilliseconds = 100;

        public static Task<DnsResult<AddressRecord>> ResolveAddresses(IPEndPoint[] servers, bool async, string name, AddressFamily addressFamily, CancellationToken cancellationToken)
            => servers.Length == 0
                ? Query<AddressRecord>(servers, async, name, AddressFamilyToQueryType(addressFamily), cancellationToken, TryParseAddress)
                : ResolveAddresses((IList<IPEndPoint>)servers, async, name, addressFamily, cancellationToken);

        public static Task<DnsResult<SrvRecord>> ResolveSrv(IPEndPoint[] servers, bool async, string name, CancellationToken cancellationToken)
            => servers.Length == 0
                ? Query<SrvRecord>(servers, async, name, Interop.Dnssd.kDNSServiceType_SRV, cancellationToken, TryParseSrv)
                : ResolveSrv((IList<IPEndPoint>)servers, async, name, cancellationToken);

        public static Task<DnsResult<MxRecord>> ResolveMx(IPEndPoint[] servers, bool async, string name, CancellationToken cancellationToken)
            => servers.Length == 0
                ? Query<MxRecord>(servers, async, name, Interop.Dnssd.kDNSServiceType_MX, cancellationToken, TryParseMx)
                : ResolveMx((IList<IPEndPoint>)servers, async, name, cancellationToken);

        public static Task<DnsResult<TxtRecord>> ResolveTxt(IPEndPoint[] servers, bool async, string name, CancellationToken cancellationToken)
            => servers.Length == 0
                ? Query<TxtRecord>(servers, async, name, Interop.Dnssd.kDNSServiceType_TXT, cancellationToken, TryParseTxt)
                : ResolveTxt((IList<IPEndPoint>)servers, async, name, cancellationToken);

        public static Task<DnsResult<CNameRecord>> ResolveCName(IPEndPoint[] servers, bool async, string name, CancellationToken cancellationToken)
            => servers.Length == 0
                ? Query<CNameRecord>(servers, async, name, Interop.Dnssd.kDNSServiceType_CNAME, cancellationToken, TryParseCName)
                : ResolveCName((IList<IPEndPoint>)servers, async, name, cancellationToken);

        public static Task<DnsResult<PtrRecord>> ResolvePtr(IPEndPoint[] servers, bool async, string name, CancellationToken cancellationToken)
            => servers.Length == 0
                ? Query<PtrRecord>(servers, async, name, Interop.Dnssd.kDNSServiceType_PTR, cancellationToken, TryParsePtr)
                : ResolvePtr((IList<IPEndPoint>)servers, async, name, cancellationToken);

        public static Task<DnsResult<NsRecord>> ResolveNs(IPEndPoint[] servers, bool async, string name, CancellationToken cancellationToken)
            => servers.Length == 0
                ? Query<NsRecord>(servers, async, name, Interop.Dnssd.kDNSServiceType_NS, cancellationToken, TryParseNs)
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
            TryParseRecord<TRecord> tryParse)
        {
            ValidateServers(servers);

            if (name.Contains('\0'))
            {
                throw new ArgumentException(SR.net_hostname_invalid_character, nameof(name));
            }

            return async
                ? Task.Run(() => QueryCore(name, queryType, cancellationToken, tryParse), cancellationToken)
                : Task.FromResult(QueryCore(name, queryType, cancellationToken, tryParse));
        }

        private static DnsResult<TRecord> QueryCore<TRecord>(
            string name,
            ushort queryType,
            CancellationToken cancellationToken,
            TryParseRecord<TRecord> tryParse)
        {
            DnsSdQueryResult raw = QueryRecord(name, queryType, cancellationToken);
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

        private static unsafe DnsSdQueryResult QueryRecord(string name, ushort queryType, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            DnsSdQueryState state = new(queryType);
            GCHandle stateHandle = GCHandle.Alloc(state);
            IntPtr serviceRef = IntPtr.Zero;

            try
            {
                int status = Interop.Dnssd.DNSServiceQueryRecord(
                    out serviceRef,
                    flags: Interop.Dnssd.kDNSServiceFlagsReturnIntermediates | Interop.Dnssd.kDNSServiceFlagsTimeout,
                    interfaceIndex: 0,
                    fullname: name,
                    rrtype: queryType,
                    rrclass: Interop.Dnssd.kDNSServiceClass_IN,
                    callBack: &QueryRecordCallback,
                    context: GCHandle.ToIntPtr(stateHandle));

                if (status != Interop.Dnssd.kDNSServiceErr_NoError)
                {
                    return DnsSdQueryResult.FromStatus(status);
                }

                using SafeDnsServiceHandle dnsService = new(serviceRef);
                serviceRef = IntPtr.Zero;

                int fileDescriptor = Interop.Dnssd.DNSServiceRefSockFD(dnsService.DangerousGetHandle());
                if (fileDescriptor < 0)
                {
                    return DnsSdQueryResult.FromStatus(Interop.Dnssd.kDNSServiceErr_DefunctConnection);
                }

                using SafeFileHandle fileHandle = new((IntPtr)fileDescriptor, ownsHandle: false);

                while (!state.IsComplete)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    Interop.Error error = Interop.Sys.Poll(fileHandle, Interop.PollEvents.POLLIN, PollTimeoutMilliseconds, out Interop.PollEvents triggered);
                    if (error == Interop.Error.EINTR)
                    {
                        continue;
                    }

                    if (error != Interop.Error.SUCCESS)
                    {
                        return DnsSdQueryResult.FromStatus(Interop.Dnssd.kDNSServiceErr_Unknown);
                    }

                    if ((triggered & (Interop.PollEvents.POLLERR | Interop.PollEvents.POLLHUP | Interop.PollEvents.POLLNVAL)) != 0)
                    {
                        return DnsSdQueryResult.FromStatus(Interop.Dnssd.kDNSServiceErr_DefunctConnection);
                    }

                    if ((triggered & Interop.PollEvents.POLLIN) != 0)
                    {
                        status = Interop.Dnssd.DNSServiceProcessResult(dnsService.DangerousGetHandle());
                        if (status != Interop.Dnssd.kDNSServiceErr_NoError)
                        {
                            state.SetError(status);
                        }
                    }
                }

                return state.ToResult();
            }
            finally
            {
                if (serviceRef != IntPtr.Zero)
                {
                    Interop.Dnssd.DNSServiceRefDeallocate(serviceRef);
                }

                stateHandle.Free();
            }
        }

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
                state = (DnsSdQueryState)GCHandle.FromIntPtr(context).Target!;
                state.OnRecord(flags, interfaceIndex, errorCode, rrtype, rrclass, rdlen, rdata, ttl);
            }
            catch (Exception ex)
            {
                state?.SetException(ex);
            }
        }

        private delegate bool TryParseRecord<TRecord>(DnsSdRecord record, out TRecord parsed);

        private static bool TryParseAddress(DnsSdRecord record, out AddressRecord parsed)
        {
            if (record.Data.Length == 4 || record.Data.Length == 16)
            {
                IPAddress address = new IPAddress(record.Data);
                if (address.IsIPv6LinkLocal)
                {
                    address.ScopeId = record.InterfaceIndex;
                }

                parsed = new AddressRecord(address, TimeSpan.FromSeconds(record.Ttl));
                return true;
            }

            parsed = default;
            return false;
        }

        private static bool TryParseSrv(DnsSdRecord record, out SrvRecord parsed)
        {
            ReadOnlySpan<byte> data = record.Data;
            if (data.Length >= 7 && TryParseDnsName(data.Slice(6), out string target, out _))
            {
                parsed = new SrvRecord(
                    target,
                    BinaryPrimitives.ReadUInt16BigEndian(data.Slice(4, 2)),
                    BinaryPrimitives.ReadUInt16BigEndian(data.Slice(0, 2)),
                    BinaryPrimitives.ReadUInt16BigEndian(data.Slice(2, 2)),
                    TimeSpan.FromSeconds(record.Ttl),
                    // DNSServiceQueryRecord exposes only the queried record's rdata, not
                    // additional-section glue A/AAAA records.
                    null);
                return true;
            }

            parsed = default;
            return false;
        }

        private static bool TryParseMx(DnsSdRecord record, out MxRecord parsed)
        {
            ReadOnlySpan<byte> data = record.Data;
            if (data.Length >= 3 && TryParseDnsName(data.Slice(2), out string exchange, out _))
            {
                parsed = new MxRecord(exchange, BinaryPrimitives.ReadUInt16BigEndian(data.Slice(0, 2)), TimeSpan.FromSeconds(record.Ttl));
                return true;
            }

            parsed = default;
            return false;
        }

        private static bool TryParseTxt(DnsSdRecord record, out TxtRecord parsed)
        {
            ReadOnlySpan<byte> data = record.Data;
            List<string> values = new();
            int offset = 0;

            while (offset < data.Length)
            {
                int length = data[offset++];
                if (length > data.Length - offset)
                {
                    parsed = default;
                    return false;
                }

                values.Add(Encoding.UTF8.GetString(data.Slice(offset, length)));
                offset += length;
            }

            parsed = new TxtRecord(values, TimeSpan.FromSeconds(record.Ttl));
            return true;
        }

        private static bool TryParseCName(DnsSdRecord record, out CNameRecord parsed)
        {
            if (TryParseDnsName(record.Data, out string name, out _))
            {
                parsed = new CNameRecord(name, TimeSpan.FromSeconds(record.Ttl));
                return true;
            }

            parsed = default;
            return false;
        }

        private static bool TryParsePtr(DnsSdRecord record, out PtrRecord parsed)
        {
            if (TryParseDnsName(record.Data, out string name, out _))
            {
                parsed = new PtrRecord(name, TimeSpan.FromSeconds(record.Ttl));
                return true;
            }

            parsed = default;
            return false;
        }

        private static bool TryParseNs(DnsSdRecord record, out NsRecord parsed)
        {
            if (TryParseDnsName(record.Data, out string name, out _))
            {
                parsed = new NsRecord(name, TimeSpan.FromSeconds(record.Ttl));
                return true;
            }

            parsed = default;
            return false;
        }

        private static bool TryParseDnsName(ReadOnlySpan<byte> data, out string name, out int bytesConsumed)
        {
            StringBuilder builder = new();
            int offset = 0;

            while (offset < data.Length)
            {
                byte length = data[offset++];
                if (length == 0)
                {
                    name = builder.Length == 0 ? "." : builder.ToString();
                    bytesConsumed = offset;
                    return true;
                }

                if ((length & 0xC0) != 0 || length > 63 || length > data.Length - offset)
                {
                    break;
                }

                if (builder.Length != 0)
                {
                    builder.Append('.');
                }

                builder.Append(Encoding.UTF8.GetString(data.Slice(offset, length)));
                offset += length;
            }

            name = string.Empty;
            bytesConsumed = 0;
            return false;
        }

        private readonly struct DnsSdRecord
        {
            public ushort Type { get; }
            public byte[] Data { get; }
            public uint Ttl { get; }
            public uint InterfaceIndex { get; }

            public DnsSdRecord(ushort type, byte[] data, uint ttl, uint interfaceIndex)
            {
                Type = type;
                Data = data;
                Ttl = ttl;
                InterfaceIndex = interfaceIndex;
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

        private sealed class SafeDnsServiceHandle : SafeHandle
        {
            public SafeDnsServiceHandle(IntPtr handle)
                : base(IntPtr.Zero, ownsHandle: true)
            {
                SetHandle(handle);
            }

            public override bool IsInvalid => handle == IntPtr.Zero;

            protected override bool ReleaseHandle()
            {
                Interop.Dnssd.DNSServiceRefDeallocate(handle);
                return true;
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
