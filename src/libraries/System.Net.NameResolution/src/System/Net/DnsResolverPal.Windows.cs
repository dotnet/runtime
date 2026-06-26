// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net
{
    // Windows DNS resolver implementation backed by the Win32 DnsQueryEx API.
    internal static partial class DnsResolverPal
    {
        // ---- Public PAL entry points (one per record type) ----
        //
        // Each method takes a `bool async` flag controlling whether the underlying
        // DnsQueryEx call is issued asynchronously (via the completion-callback state
        // machine) or synchronously (inline on the calling thread). When async is
        // false the returned Task is already completed, so the synchronous public
        // entry points can safely unwrap it without blocking a thread.

        public static async Task<DnsResult<AddressRecord>> ResolveAddresses(IPEndPoint[] servers, bool async, string name, AddressFamily addressFamily, CancellationToken cancellationToken)
        {
            if (addressFamily == AddressFamily.Unspecified)
            {
                if (async)
                {
                    // Issue A and AAAA in parallel; merge results.
                    Task<DnsResult<AddressRecord>> aTask = QueryAddresses(servers, async: true, name, Interop.Dnsapi.DNS_TYPE_A, cancellationToken);
                    Task<DnsResult<AddressRecord>> aaaaTask = QueryAddresses(servers, async: true, name, Interop.Dnsapi.DNS_TYPE_AAAA, cancellationToken);
                    DnsResult<AddressRecord> aRes = await aTask.ConfigureAwait(false);
                    DnsResult<AddressRecord> aaaaRes = await aaaaTask.ConfigureAwait(false);
                    return MergeAddressResults(aRes, aaaaRes);
                }
                else
                {
                    // Synchronous: query A then AAAA sequentially.
                    DnsResult<AddressRecord> aRes = await QueryAddresses(servers, async: false, name, Interop.Dnsapi.DNS_TYPE_A, cancellationToken).ConfigureAwait(false);
                    DnsResult<AddressRecord> aaaaRes = await QueryAddresses(servers, async: false, name, Interop.Dnsapi.DNS_TYPE_AAAA, cancellationToken).ConfigureAwait(false);
                    return MergeAddressResults(aRes, aaaaRes);
                }
            }

            ushort qtype = AddressFamilyToQueryType(addressFamily);
            return await QueryAddresses(servers, async, name, qtype, cancellationToken).ConfigureAwait(false);
        }

        public static async Task<DnsResult<SrvRecord>> ResolveSrv(IPEndPoint[] servers, bool async, string name, CancellationToken cancellationToken)
        {
            using DnsQueryRawResult raw = await DnsQueryEx(servers, async, name, Interop.Dnsapi.DNS_TYPE_SRV, cancellationToken).ConfigureAwait(false);
            return ParseSrv(raw);
        }

        public static Task<DnsResult<MxRecord>> ResolveMx(IPEndPoint[] servers, bool async, string name, CancellationToken cancellationToken)
            => QuerySimple(servers, async, name, Interop.Dnsapi.DNS_TYPE_MX, cancellationToken, s_parseMx);

        public static Task<DnsResult<CNameRecord>> ResolveCName(IPEndPoint[] servers, bool async, string name, CancellationToken cancellationToken)
            => QuerySimple(servers, async, name, Interop.Dnsapi.DNS_TYPE_CNAME, cancellationToken, s_parseCName);

        public static Task<DnsResult<PtrRecord>> ResolvePtr(IPEndPoint[] servers, bool async, string name, CancellationToken cancellationToken)
            => QuerySimple(servers, async, name, Interop.Dnsapi.DNS_TYPE_PTR, cancellationToken, s_parsePtr);

        public static Task<DnsResult<NsRecord>> ResolveNs(IPEndPoint[] servers, bool async, string name, CancellationToken cancellationToken)
            => QuerySimple(servers, async, name, Interop.Dnsapi.DNS_TYPE_NS, cancellationToken, s_parseNs);

        public static async Task<DnsResult<TxtRecord>> ResolveTxt(IPEndPoint[] servers, bool async, string name, CancellationToken cancellationToken)
        {
            using DnsQueryRawResult raw = await DnsQueryEx(servers, async, name, Interop.Dnsapi.DNS_TYPE_TEXT, cancellationToken).ConfigureAwait(false);
            return ParseTxt(raw);
        }

        // ---- Per-record-type selectors (shared by all record types) ----
        // The bodies dereference PCWSTR fields, so the methods are unsafe; caching the
        // delegates avoids re-allocating one per query.

        private static readonly Func<Interop.Dnsapi.DNS_RECORD_HEADER, IntPtr, MxRecord> s_parseMx = ParseMxRecord;
        private static readonly Func<Interop.Dnsapi.DNS_RECORD_HEADER, IntPtr, CNameRecord> s_parseCName = ParseCNameRecord;
        private static readonly Func<Interop.Dnsapi.DNS_RECORD_HEADER, IntPtr, PtrRecord> s_parsePtr = ParsePtrRecord;
        private static readonly Func<Interop.Dnsapi.DNS_RECORD_HEADER, IntPtr, NsRecord> s_parseNs = ParseNsRecord;

        private static unsafe MxRecord ParseMxRecord(Interop.Dnsapi.DNS_RECORD_HEADER hdr, IntPtr dataPtr)
        {
            ref readonly Interop.Dnsapi.DNS_MX_DATA data = ref AsStruct<Interop.Dnsapi.DNS_MX_DATA>(dataPtr);
            return new MxRecord(PtrToString(data.pNameExchange) ?? string.Empty, data.wPreference, TimeSpan.FromSeconds(hdr.dwTtl));
        }

        private static unsafe CNameRecord ParseCNameRecord(Interop.Dnsapi.DNS_RECORD_HEADER hdr, IntPtr dataPtr)
        {
            ref readonly Interop.Dnsapi.DNS_CNAME_DATA data = ref AsStruct<Interop.Dnsapi.DNS_CNAME_DATA>(dataPtr);
            return new CNameRecord(PtrToString(data.pNameHost) ?? string.Empty, TimeSpan.FromSeconds(hdr.dwTtl));
        }

        private static unsafe PtrRecord ParsePtrRecord(Interop.Dnsapi.DNS_RECORD_HEADER hdr, IntPtr dataPtr)
        {
            ref readonly Interop.Dnsapi.DNS_PTR_DATA data = ref AsStruct<Interop.Dnsapi.DNS_PTR_DATA>(dataPtr);
            return new PtrRecord(PtrToString(data.pNameHost) ?? string.Empty, TimeSpan.FromSeconds(hdr.dwTtl));
        }

        private static unsafe NsRecord ParseNsRecord(Interop.Dnsapi.DNS_RECORD_HEADER hdr, IntPtr dataPtr)
        {
            ref readonly Interop.Dnsapi.DNS_NS_DATA data = ref AsStruct<Interop.Dnsapi.DNS_NS_DATA>(dataPtr);
            return new NsRecord(PtrToString(data.pNameHost) ?? string.Empty, TimeSpan.FromSeconds(hdr.dwTtl));
        }

        private static ushort AddressFamilyToQueryType(AddressFamily addressFamily) =>
            addressFamily switch
            {
                AddressFamily.InterNetwork => Interop.Dnsapi.DNS_TYPE_A,
                AddressFamily.InterNetworkV6 => Interop.Dnsapi.DNS_TYPE_AAAA,
                _ => throw new ArgumentException(SR.net_dns_unsupported_address_family, nameof(addressFamily)),
            };

        // ---- Query wrappers (issue the query, then parse the record list) ----

        private static async Task<DnsResult<AddressRecord>> QueryAddresses(IPEndPoint[] servers, bool async, string name, ushort qtype, CancellationToken cancellationToken)
        {
            using DnsQueryRawResult raw = await DnsQueryEx(servers, async, name, qtype, cancellationToken).ConfigureAwait(false);
            return ParseAddresses(raw, qtype);
        }

        private static async Task<DnsResult<TRecord>> QuerySimple<TRecord>(IPEndPoint[] servers, bool async, string name, ushort qtype, CancellationToken cancellationToken,
            Func<Interop.Dnsapi.DNS_RECORD_HEADER, IntPtr, TRecord> selector)
        {
            using DnsQueryRawResult raw = await DnsQueryEx(servers, async, name, qtype, cancellationToken).ConfigureAwait(false);
            return ParseSimple(raw, qtype, selector);
        }

        // ---- Record-list parsers ----

        private static unsafe DnsResult<AddressRecord> ParseAddresses(DnsQueryRawResult raw, ushort qtype)
        {
            if (raw.ResponseCode != DnsResponseCode.NoError)
            {
                return new DnsResult<AddressRecord>(raw.ResponseCode, null, raw.NegativeCacheTtl);
            }

            List<AddressRecord> records = new();
            for (IntPtr cur = raw.RecordsHead; cur != IntPtr.Zero; )
            {
                ref readonly Interop.Dnsapi.DNS_RECORD_HEADER hdr = ref AsStruct<Interop.Dnsapi.DNS_RECORD_HEADER>(cur);
                uint section = hdr.Flags & Interop.Dnsapi.DNSREC_SECTION_MASK;
                if (hdr.wType == qtype && section == Interop.Dnsapi.DNSREC_ANSWER)
                {
                    IntPtr dataPtr = cur + sizeof(Interop.Dnsapi.DNS_RECORD_HEADER);
                    if (TryParseAddress(hdr.wType, dataPtr, out IPAddress? address))
                    {
                        records.Add(new AddressRecord(address!, TimeSpan.FromSeconds(hdr.dwTtl)));
                    }
                }
                cur = hdr.pNext;
            }

            return new DnsResult<AddressRecord>(DnsResponseCode.NoError, records, raw.NegativeCacheTtl);
        }

        private static unsafe DnsResult<SrvRecord> ParseSrv(DnsQueryRawResult raw)
        {
            if (raw.ResponseCode != DnsResponseCode.NoError)
            {
                return new DnsResult<SrvRecord>(raw.ResponseCode, null, raw.NegativeCacheTtl);
            }

            // Gather additional-section A/AAAA records by name so we can attach them.
            Dictionary<string, List<AddressRecord>>? glue = null;
            ParseAdditionalAddresses(raw.RecordsHead, ref glue);

            List<SrvRecord> records = new();
            for (IntPtr cur = raw.RecordsHead; cur != IntPtr.Zero; )
            {
                ref readonly Interop.Dnsapi.DNS_RECORD_HEADER hdr = ref AsStruct<Interop.Dnsapi.DNS_RECORD_HEADER>(cur);
                uint section = hdr.Flags & Interop.Dnsapi.DNSREC_SECTION_MASK;
                if (hdr.wType == Interop.Dnsapi.DNS_TYPE_SRV && section == Interop.Dnsapi.DNSREC_ANSWER)
                {
                    IntPtr dataPtr = cur + sizeof(Interop.Dnsapi.DNS_RECORD_HEADER);
                    ref readonly Interop.Dnsapi.DNS_SRV_DATA data = ref AsStruct<Interop.Dnsapi.DNS_SRV_DATA>(dataPtr);
                    string target = PtrToString(data.pNameTarget) ?? string.Empty;
                    List<AddressRecord>? attached = null;
                    glue?.TryGetValue(target, out attached);
                    records.Add(new SrvRecord(target, data.wPort, data.wPriority, data.wWeight, TimeSpan.FromSeconds(hdr.dwTtl), attached));
                }
                cur = hdr.pNext;
            }

            return new DnsResult<SrvRecord>(DnsResponseCode.NoError, records, raw.NegativeCacheTtl);
        }

        private static unsafe DnsResult<TxtRecord> ParseTxt(DnsQueryRawResult raw)
        {
            if (raw.ResponseCode != DnsResponseCode.NoError)
            {
                return new DnsResult<TxtRecord>(raw.ResponseCode, null, raw.NegativeCacheTtl);
            }

            List<TxtRecord> records = new();
            for (IntPtr cur = raw.RecordsHead; cur != IntPtr.Zero; )
            {
                ref readonly Interop.Dnsapi.DNS_RECORD_HEADER hdr = ref AsStruct<Interop.Dnsapi.DNS_RECORD_HEADER>(cur);
                uint section = hdr.Flags & Interop.Dnsapi.DNSREC_SECTION_MASK;
                if (hdr.wType == Interop.Dnsapi.DNS_TYPE_TEXT && section == Interop.Dnsapi.DNSREC_ANSWER)
                {
                    IntPtr dataPtr = cur + sizeof(Interop.Dnsapi.DNS_RECORD_HEADER);
                    // DNS_TXT_DATA: uint dwStringCount; followed by array of PCWSTR.
                    uint count = (uint)Marshal.ReadInt32(dataPtr);
                    int ptrSize = IntPtr.Size;
                    IntPtr stringsPtr = dataPtr + sizeof(uint);
                    if (ptrSize > sizeof(uint))
                    {
                        // Round up to pointer alignment.
                        long aligned = ((long)stringsPtr + (ptrSize - 1)) & ~(long)(ptrSize - 1);
                        stringsPtr = checked((nint)aligned);
                    }
                    string[] values = new string[count];
                    for (int i = 0; i < count; i++)
                    {
                        IntPtr strPtr = Marshal.ReadIntPtr(stringsPtr, i * ptrSize);
                        values[i] = PtrToString(strPtr) ?? string.Empty;
                    }
                    records.Add(new TxtRecord(values, TimeSpan.FromSeconds(hdr.dwTtl)));
                }
                cur = hdr.pNext;
            }

            return new DnsResult<TxtRecord>(DnsResponseCode.NoError, records, raw.NegativeCacheTtl);
        }

        private static unsafe DnsResult<TRecord> ParseSimple<TRecord>(DnsQueryRawResult raw, ushort qtype,
            Func<Interop.Dnsapi.DNS_RECORD_HEADER, IntPtr, TRecord> selector)
        {
            if (raw.ResponseCode != DnsResponseCode.NoError)
            {
                return new DnsResult<TRecord>(raw.ResponseCode, null, raw.NegativeCacheTtl);
            }

            List<TRecord> records = new();
            for (IntPtr cur = raw.RecordsHead; cur != IntPtr.Zero; )
            {
                ref readonly Interop.Dnsapi.DNS_RECORD_HEADER hdr = ref AsStruct<Interop.Dnsapi.DNS_RECORD_HEADER>(cur);
                uint section = hdr.Flags & Interop.Dnsapi.DNSREC_SECTION_MASK;
                if (hdr.wType == qtype && section == Interop.Dnsapi.DNSREC_ANSWER)
                {
                    IntPtr dataPtr = cur + sizeof(Interop.Dnsapi.DNS_RECORD_HEADER);
                    records.Add(selector(hdr, dataPtr));
                }
                cur = hdr.pNext;
            }

            return new DnsResult<TRecord>(DnsResponseCode.NoError, records, raw.NegativeCacheTtl);
        }

        private static DnsResult<AddressRecord> MergeAddressResults(DnsResult<AddressRecord> a, DnsResult<AddressRecord> b)
        {
            if (a.Records.Count > 0 || b.Records.Count > 0)
            {
                AddressRecord[] merged = [.. a.Records, .. b.Records];
                TimeSpan mergedTtl = MinNonZero(a.NegativeCacheTtl, b.NegativeCacheTtl);
                return new DnsResult<AddressRecord>(DnsResponseCode.NoError, merged, mergedTtl);
            }

            DnsResponseCode chosenRc = a.ResponseCode == DnsResponseCode.NxDomain || b.ResponseCode == DnsResponseCode.NxDomain
                ? DnsResponseCode.NxDomain
                : (a.ResponseCode != DnsResponseCode.NoError ? a.ResponseCode : b.ResponseCode);
            TimeSpan negTtl = MinNonZero(a.NegativeCacheTtl, b.NegativeCacheTtl);
            return new DnsResult<AddressRecord>(chosenRc, null, negTtl);
        }

        // Returns the smaller of two non-zero negative-cache TTLs, or zero if neither is positive.
        private static TimeSpan MinNonZero(TimeSpan x, TimeSpan y)
        {
            if (x <= TimeSpan.Zero)
            {
                return y > TimeSpan.Zero ? y : TimeSpan.Zero;
            }

            if (y <= TimeSpan.Zero)
            {
                return x;
            }

            return x < y ? x : y;
        }

        private static bool TryParseAddress(ushort recordType, IntPtr dataPtr, out IPAddress? address)
        {
            if (recordType == Interop.Dnsapi.DNS_TYPE_A)
            {
                // DNS_A_DATA holds the IPv4 address as a uint already in network byte
                // order, which is exactly the layout the IPAddress(long) ctor expects.
                ref readonly Interop.Dnsapi.DNS_A_DATA data = ref AsStruct<Interop.Dnsapi.DNS_A_DATA>(dataPtr);
                address = new IPAddress((long)data.IpAddress);
                return true;
            }

            if (recordType == Interop.Dnsapi.DNS_TYPE_AAAA)
            {
                // DNS_AAAA_DATA holds the 16 raw IPv6 address bytes.
                ref readonly Interop.Dnsapi.DNS_AAAA_DATA data = ref AsStruct<Interop.Dnsapi.DNS_AAAA_DATA>(dataPtr);
                address = new IPAddress((ReadOnlySpan<byte>)data.Ip6Address);
                return true;
            }

            address = null;
            return false;
        }

        private static unsafe void ParseAdditionalAddresses(IntPtr head, ref Dictionary<string, List<AddressRecord>>? glue)
        {
            for (IntPtr cur = head; cur != IntPtr.Zero; )
            {
                ref readonly Interop.Dnsapi.DNS_RECORD_HEADER hdr = ref AsStruct<Interop.Dnsapi.DNS_RECORD_HEADER>(cur);
                uint section = hdr.Flags & Interop.Dnsapi.DNSREC_SECTION_MASK;
                bool isAddress = hdr.wType == Interop.Dnsapi.DNS_TYPE_A || hdr.wType == Interop.Dnsapi.DNS_TYPE_AAAA;
                if (section == Interop.Dnsapi.DNSREC_ADDITIONAL && isAddress)
                {
                    IntPtr dataPtr = cur + sizeof(Interop.Dnsapi.DNS_RECORD_HEADER);
                    if (TryParseAddress(hdr.wType, dataPtr, out IPAddress? address))
                    {
                        string name = PtrToString(hdr.pName) ?? string.Empty;
                        glue ??= new Dictionary<string, List<AddressRecord>>(StringComparer.OrdinalIgnoreCase);
                        List<AddressRecord> list = CollectionsMarshal.GetValueRefOrAddDefault(glue, name, out _) ??= new List<AddressRecord>();
                        list.Add(new AddressRecord(address!, TimeSpan.FromSeconds(hdr.dwTtl)));
                    }
                }
                cur = hdr.pNext;
            }
        }

        // ---- Core DnsQueryEx wrapper ----

        private static Task<DnsQueryRawResult> DnsQueryEx(IPEndPoint[] servers, bool async, string name, ushort queryType, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<DnsQueryRawResult>(cancellationToken);
            }

            if (async)
            {
                DnsQueryAsyncState state = new DnsQueryAsyncState(servers, name, queryType, cancellationToken);
                return state.StartAsync();
            }

            // Synchronous: the result is produced inline, so the returned Task is
            // already completed and the sync entry points unwrap it without blocking.
            return Task.FromResult(DnsQueryExSync(servers, name, queryType, cancellationToken));
        }

        private static string? PtrToString(IntPtr p) =>
            p == IntPtr.Zero ? null : Marshal.PtrToStringUni(p);

        private static unsafe string? PtrToString(char* p) =>
            p == null ? null : new string(p);

        // Reinterprets an unmanaged pointer as a readonly reference to a struct, avoiding the
        // marshalling copy of Marshal.PtrToStructure. The single pointer cast is confined here.
        private static unsafe ref readonly T AsStruct<T>(IntPtr ptr) where T : unmanaged =>
            ref Unsafe.AsRef<T>((void*)ptr);

        // ---- Asynchronous DnsQueryEx state machine ----

        /// <summary>
        /// Holds the unmanaged state for a single DnsQueryEx invocation, including
        /// the request/result/cancel structures, the pinned query name, and the
        /// completion TaskCompletionSource.
        /// </summary>
        private sealed unsafe class DnsQueryAsyncState
        {
            private readonly TaskCompletionSource<DnsQueryRawResult> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
            private readonly string _name;
            private readonly ushort _queryType;
            private readonly CancellationToken _cancellationToken;
            private readonly IPEndPoint[] _servers;

            private GCHandle<DnsQueryAsyncState> _selfHandle;
            private char* _namePtr;
            private Interop.Dnsapi.DNS_QUERY_REQUEST* _requestPtr;
            private Interop.Dnsapi.DNS_QUERY_RESULT* _resultPtr;
            private Interop.Dnsapi.DNS_QUERY_CANCEL* _cancelPtr;
            private Interop.Dnsapi.DNS_ADDR_ARRAY* _serverListPtr;
            private CancellationTokenRegistration _ctReg;
            private bool _completed;

            public DnsQueryAsyncState(IPEndPoint[] servers, string name, ushort queryType, CancellationToken cancellationToken)
            {
                _servers = servers;
                _name = name;
                _queryType = queryType;
                _cancellationToken = cancellationToken;
            }

            public Task<DnsQueryRawResult> StartAsync()
            {
                ValidateServerPorts(_servers);

                int status;
                try
                {
                    _namePtr = (char*)Marshal.StringToHGlobalUni(_name);

                    _resultPtr = (Interop.Dnsapi.DNS_QUERY_RESULT*)Marshal.AllocHGlobal(sizeof(Interop.Dnsapi.DNS_QUERY_RESULT));
                    NativeMemory.Clear(_resultPtr, (nuint)sizeof(Interop.Dnsapi.DNS_QUERY_RESULT));
                    _resultPtr->Version = Interop.Dnsapi.DNS_QUERY_REQUEST_VERSION1;

                    _cancelPtr = (Interop.Dnsapi.DNS_QUERY_CANCEL*)Marshal.AllocHGlobal(sizeof(Interop.Dnsapi.DNS_QUERY_CANCEL));
                    NativeMemory.Clear(_cancelPtr, (nuint)sizeof(Interop.Dnsapi.DNS_QUERY_CANCEL));

                    _selfHandle = new GCHandle<DnsQueryAsyncState>(this);

                    _requestPtr = (Interop.Dnsapi.DNS_QUERY_REQUEST*)Marshal.AllocHGlobal(sizeof(Interop.Dnsapi.DNS_QUERY_REQUEST));
                    NativeMemory.Clear(_requestPtr, (nuint)sizeof(Interop.Dnsapi.DNS_QUERY_REQUEST));
                    _requestPtr->Version = Interop.Dnsapi.DNS_QUERY_REQUEST_VERSION1;
                    _requestPtr->QueryName = _namePtr;
                    _requestPtr->QueryType = _queryType;
                    _requestPtr->QueryOptions = Interop.Dnsapi.DNS_QUERY_STANDARD;
                    _requestPtr->InterfaceIndex = 0;
                    _requestPtr->pQueryCompletionCallback = &QueryCompletionCallback;
                    _requestPtr->pQueryContext = GCHandle<DnsQueryAsyncState>.ToIntPtr(_selfHandle);

                    if (_servers is { Length: > 0 })
                    {
                        BuildAddrArray(_servers, out IntPtr serverList);
                        _serverListPtr = (Interop.Dnsapi.DNS_ADDR_ARRAY*)serverList;
                        _requestPtr->pDnsServerList = _serverListPtr;
                    }

                    status = Interop.Dnsapi.DnsQueryEx(_requestPtr, _resultPtr, _cancelPtr);
                }
                catch
                {
                    FreeAll();
                    throw;
                }

                if (status == Interop.Dnsapi.DNS_REQUEST_PENDING)
                {
                    // The query is now in-flight and the native runtime owns the request/result/
                    // cancel buffers until the completion callback fires. Register cancellation
                    // OUTSIDE the try/catch above: if registration throws (e.g. the
                    // CancellationTokenSource was already disposed) we must NOT free the native
                    // state here, because the pending query still references it — the completion
                    // callback will free everything when it eventually runs.
                    if (_cancellationToken.CanBeCanceled)
                    {
                        _ctReg = _cancellationToken.UnsafeRegister(static (s, _) =>
                        {
                            DnsQueryAsyncState st = (DnsQueryAsyncState)s!;
                            st.CancelAndAbort();
                        }, this);
                    }
                }
                else
                {
                    // Synchronous completion. The callback was NOT invoked; we complete inline.
                    CompleteFromResult(status);
                }

                return _tcs.Task;
            }

            private void CancelAndAbort()
            {
                if (_cancelPtr != null)
                {
                    Interop.Dnsapi.DnsCancelQuery(_cancelPtr);
                }
            }

            /// <summary>
            /// Invoked from either the native callback or the sync completion path.
            /// Parses the QueryStatus and pQueryRecords from the result struct,
            /// completes the TCS, and frees state.
            /// </summary>
            internal void CompleteFromResult(int status)
            {
                if (Interlocked.Exchange(ref _completed, true))
                {
                    return;
                }

                try
                {
                    _ctReg.Dispose();

                    IntPtr records = _resultPtr->pQueryRecords;

                    if (_cancellationToken.IsCancellationRequested)
                    {
                        if (records != IntPtr.Zero)
                        {
                            Interop.Dnsapi.DnsFree(records, Interop.Dnsapi.DnsFreeRecordList);
                        }
                        _tcs.TrySetCanceled(_cancellationToken);
                        return;
                    }

                    DnsResponseCode rc = MapWindowsErrorToResponseCode(status);

                    // Extract the negative-cache TTL from an authority-section SOA when present.
                    // This covers both NXDOMAIN and NODATA (the latter maps to NoError but can
                    // still carry an authority SOA); the helper returns zero when no SOA is found.
                    TimeSpan negativeTtl = ExtractNegativeCacheTtl(records);

                    _tcs.TrySetResult(new DnsQueryRawResult(rc, records, negativeTtl));
                }
                catch (Exception ex)
                {
                    _tcs.TrySetException(ex);
                }
                finally
                {
                    FreeAll();
                }
            }

            private void FreeAll()
            {
                if (_namePtr != null)
                {
                    Marshal.FreeHGlobal((IntPtr)_namePtr);
                    _namePtr = null;
                }
                if (_requestPtr != null)
                {
                    Marshal.FreeHGlobal((IntPtr)_requestPtr);
                    _requestPtr = null;
                }
                if (_resultPtr != null)
                {
                    Marshal.FreeHGlobal((IntPtr)_resultPtr);
                    _resultPtr = null;
                }
                if (_cancelPtr != null)
                {
                    Marshal.FreeHGlobal((IntPtr)_cancelPtr);
                    _cancelPtr = null;
                }
                if (_serverListPtr != null)
                {
                    Marshal.FreeHGlobal((IntPtr)_serverListPtr);
                    _serverListPtr = null;
                }
                if (_selfHandle.IsAllocated)
                {
                    _selfHandle.Dispose();
                }
            }
        }

        // Native completion callback, invoked by DnsQueryEx on a thread pool thread.
        // Marked [UnmanagedCallersOnly] so it can be passed directly as a function
        // pointer without an intermediate marshalled delegate.
#pragma warning disable CS3016 // Arrays as attribute arguments is not CLS-compliant
        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
#pragma warning restore CS3016
        private static unsafe void QueryCompletionCallback(nint pQueryContext, nint pQueryResults)
        {
            try
            {
                DnsQueryAsyncState state = GCHandle<DnsQueryAsyncState>.FromIntPtr(pQueryContext).Target;

                // pQueryResults points to the same DNS_QUERY_RESULT we passed in.
                Interop.Dnsapi.DNS_QUERY_RESULT* res = (Interop.Dnsapi.DNS_QUERY_RESULT*)pQueryResults;
                state.CompleteFromResult(res->QueryStatus);
            }
            catch (Exception ex)
            {
                // Never allow exceptions to propagate into native code.
                Debug.Fail($"Unexpected exception in DnsQueryEx completion callback: {ex}");
            }
        }

        // DnsQueryEx always queries DNS servers on the standard port 53 and requires the
        // sockaddr port field passed to the API to be left as 0; supplying any other
        // non-zero port results in ERROR_INVALID_PARAMETER. We accept either 0 ("use the
        // default port") or 53 (the port DnsQueryEx will actually use) and normalize both
        // to 0 when building the native server list (see WriteSockAddr). Any other port
        // cannot be honored on Windows and is rejected here.
        private static void ValidateServerPorts(IPEndPoint[] servers)
        {
            foreach (IPEndPoint ep in servers)
            {
                if (ep.Port != 0 && ep.Port != 53)
                {
                    throw new PlatformNotSupportedException(SR.net_dns_custom_port_not_supported);
                }
            }
        }

        // Synchronous DnsQueryEx invocation. By omitting the completion callback the
        // API executes the query inline on the calling thread and returns the result
        // directly, so no GCHandle / TaskCompletionSource bookkeeping is required.
        private static unsafe DnsQueryRawResult DnsQueryExSync(IPEndPoint[] servers, string name, ushort queryType, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ValidateServerPorts(servers);

            IntPtr namePtr = IntPtr.Zero;
            IntPtr serverListPtr = IntPtr.Zero;
            try
            {
                namePtr = Marshal.StringToHGlobalUni(name);

                Interop.Dnsapi.DNS_QUERY_RESULT result = default;
                result.Version = Interop.Dnsapi.DNS_QUERY_REQUEST_VERSION1;

                Interop.Dnsapi.DNS_QUERY_REQUEST request = default;
                request.Version = Interop.Dnsapi.DNS_QUERY_REQUEST_VERSION1;
                request.QueryName = (char*)namePtr;
                request.QueryType = queryType;
                request.QueryOptions = Interop.Dnsapi.DNS_QUERY_STANDARD;
                // No completion callback => synchronous execution.

                if (servers is { Length: > 0 })
                {
                    BuildAddrArray(servers, out serverListPtr);
                    request.pDnsServerList = (Interop.Dnsapi.DNS_ADDR_ARRAY*)serverListPtr;
                }

                // A null cancel handle is valid for synchronous queries.
                int status = Interop.Dnsapi.DnsQueryEx(&request, &result, null);

                IntPtr records = result.pQueryRecords;

                if (cancellationToken.IsCancellationRequested)
                {
                    if (records != IntPtr.Zero)
                    {
                        Interop.Dnsapi.DnsFree(records, Interop.Dnsapi.DnsFreeRecordList);
                    }
                    throw new OperationCanceledException(cancellationToken);
                }

                DnsResponseCode rc = MapWindowsErrorToResponseCode(status);

                // Extract the negative-cache TTL from an authority-section SOA when present.
                // This covers both NXDOMAIN and NODATA (the latter maps to NoError but can
                // still carry an authority SOA); the helper returns zero when no SOA is found.
                TimeSpan negativeTtl = ExtractNegativeCacheTtl(records);

                return new DnsQueryRawResult(rc, records, negativeTtl);
            }
            finally
            {
                if (namePtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(namePtr);
                }
                if (serverListPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(serverListPtr);
                }
            }
        }

        private static unsafe void BuildAddrArray(IPEndPoint[] servers, out IntPtr arrayPtr)
        {
            int count = servers.Length;

            // DnsQueryEx encodes a single address family for the whole array, so all
            // endpoints must share one. Reject mixed IPv4/IPv6 lists up front instead
            // of producing an inconsistent DNS_ADDR_ARRAY.
            AddressFamily family = servers[0].AddressFamily;
            for (int i = 1; i < count; i++)
            {
                if (servers[i].AddressFamily != family)
                {
                    throw new ArgumentException(SR.net_dns_mixed_address_families, nameof(DnsResolverOptions.Servers));
                }
            }

            int headerSize = sizeof(Interop.Dnsapi.DNS_ADDR_ARRAY);
            int addrSize = sizeof(Interop.Dnsapi.DNS_ADDR);
            int totalSize = checked(headerSize + addrSize * count);

            arrayPtr = Marshal.AllocHGlobal(totalSize);

            // Wrap the unmanaged buffer in a span and populate it without pointer arithmetic.
            Span<byte> buffer = new Span<byte>((void*)arrayPtr, totalSize);
            buffer.Clear();

            ref Interop.Dnsapi.DNS_ADDR_ARRAY arr = ref MemoryMarshal.AsRef<Interop.Dnsapi.DNS_ADDR_ARRAY>(buffer);
            arr.MaxCount = (uint)count;
            arr.AddrCount = (uint)count;
            arr.Family = (ushort)(family == AddressFamily.InterNetwork ? Interop.Dnsapi.AF_INET : Interop.Dnsapi.AF_INET6);

            for (int i = 0; i < count; i++)
            {
                WriteSockAddr(buffer.Slice(headerSize + (i * addrSize), addrSize), servers[i].Address);
            }
        }

        // Writes a SOCKADDR_IN or SOCKADDR_IN6 representation into the destination buffer.
        // The buffer must be at least 28 bytes (sizeof sockaddr_in6).
        private static void WriteSockAddr(Span<byte> dest, IPAddress address)
        {
            // DnsQueryEx always queries DNS servers on port 53 and requires the sockaddr
            // port field to be left as 0, so we build the SOCKADDR from a port-0 endpoint.
            // Taking an IPAddress (rather than the caller's mutable IPEndPoint) also ensures
            // we can never accidentally serialize a non-default port. SocketAddressPal lays
            // out the platform SOCKADDR (family, port, address, and for IPv6 the flow info
            // and scope id), so there's no need to write the bytes by hand.
            SocketAddress socketAddress = new IPEndPoint(address, 0).Serialize();
            socketAddress.Buffer.Span.Slice(0, socketAddress.Size).CopyTo(dest);
        }

        private static DnsResponseCode MapWindowsErrorToResponseCode(int status) =>
            status switch
            {
                Interop.Dnsapi.ERROR_SUCCESS => DnsResponseCode.NoError,
                Interop.Dnsapi.DNS_INFO_NO_RECORDS => DnsResponseCode.NoError, // NODATA: name exists but no records of requested type
                Interop.Dnsapi.DNS_ERROR_RCODE_NAME_ERROR => DnsResponseCode.NxDomain,
                Interop.Dnsapi.DNS_ERROR_RCODE_FORMAT_ERROR => DnsResponseCode.FormatError,
                Interop.Dnsapi.DNS_ERROR_RCODE_SERVER_FAILURE => DnsResponseCode.ServerFailure,
                Interop.Dnsapi.DNS_ERROR_RCODE_NOT_IMPLEMENTED => DnsResponseCode.NotImplemented,
                Interop.Dnsapi.DNS_ERROR_RCODE_REFUSED => DnsResponseCode.Refused,
                _ => DnsResponseCode.ServerFailure,
            };

        private static unsafe TimeSpan ExtractNegativeCacheTtl(IntPtr head)
        {
            // Walk the record list looking for an SOA in the authority section.
            for (IntPtr cur = head; cur != IntPtr.Zero; )
            {
                ref readonly Interop.Dnsapi.DNS_RECORD_HEADER hdr = ref AsStruct<Interop.Dnsapi.DNS_RECORD_HEADER>(cur);
                uint section = hdr.Flags & Interop.Dnsapi.DNSREC_SECTION_MASK;
                if (hdr.wType == Interop.Dnsapi.DNS_TYPE_SOA && section == Interop.Dnsapi.DNSREC_AUTHORITY)
                {
                    IntPtr dataPtr = cur + sizeof(Interop.Dnsapi.DNS_RECORD_HEADER);
                    ref readonly Interop.Dnsapi.DNS_SOA_DATA soa = ref AsStruct<Interop.Dnsapi.DNS_SOA_DATA>(dataPtr);
                    // RFC 2308 §5: negative cache TTL = min(SOA TTL, SOA MINIMUM)
                    uint negTtl = Math.Min(hdr.dwTtl, soa.dwDefaultTtl);
                    return TimeSpan.FromSeconds(negTtl);
                }
                cur = hdr.pNext;
            }
            return TimeSpan.Zero;
        }

        // ---- Raw query result returned by the low-level helpers ----

        private readonly struct DnsQueryRawResult : IDisposable
        {
            public DnsResponseCode ResponseCode { get; }
            public IntPtr RecordsHead { get; }
            public TimeSpan NegativeCacheTtl { get; }

            public DnsQueryRawResult(DnsResponseCode responseCode, IntPtr recordsHead, TimeSpan negativeCacheTtl)
            {
                ResponseCode = responseCode;
                RecordsHead = recordsHead;
                NegativeCacheTtl = negativeCacheTtl;
            }

            public void Dispose()
            {
                if (RecordsHead != IntPtr.Zero)
                {
                    Interop.Dnsapi.DnsFree(RecordsHead, Interop.Dnsapi.DnsFreeRecordList);
                }
            }
        }
    }
}
