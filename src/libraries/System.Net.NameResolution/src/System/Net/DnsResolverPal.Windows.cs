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

        public static async Task<DnsResult<AddressRecord>> ResolveAddresses(IList<IPEndPoint> servers, bool async, string name, AddressFamily addressFamily, CancellationToken cancellationToken)
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

        public static async Task<DnsResult<SrvRecord>> ResolveSrv(IList<IPEndPoint> servers, bool async, string name, CancellationToken cancellationToken)
        {
            using DnsQueryRawResult raw = await DnsQueryEx(servers, async, name, Interop.Dnsapi.DNS_TYPE_SRV, cancellationToken).ConfigureAwait(false);
            return ParseSrv(raw);
        }

        public static Task<DnsResult<MxRecord>> ResolveMx(IList<IPEndPoint> servers, bool async, string name, CancellationToken cancellationToken)
            => QuerySimple(servers, async, name, Interop.Dnsapi.DNS_TYPE_MX, cancellationToken, s_parseMx);

        public static Task<DnsResult<CNameRecord>> ResolveCName(IList<IPEndPoint> servers, bool async, string name, CancellationToken cancellationToken)
            => QuerySimple(servers, async, name, Interop.Dnsapi.DNS_TYPE_CNAME, cancellationToken, s_parseCName);

        public static Task<DnsResult<PtrRecord>> ResolvePtr(IList<IPEndPoint> servers, bool async, string name, CancellationToken cancellationToken)
            => QuerySimple(servers, async, name, Interop.Dnsapi.DNS_TYPE_PTR, cancellationToken, s_parsePtr);

        public static Task<DnsResult<NsRecord>> ResolveNs(IList<IPEndPoint> servers, bool async, string name, CancellationToken cancellationToken)
            => QuerySimple(servers, async, name, Interop.Dnsapi.DNS_TYPE_NS, cancellationToken, s_parseNs);

        public static async Task<DnsResult<TxtRecord>> ResolveTxt(IList<IPEndPoint> servers, bool async, string name, CancellationToken cancellationToken)
        {
            using DnsQueryRawResult raw = await DnsQueryEx(servers, async, name, Interop.Dnsapi.DNS_TYPE_TEXT, cancellationToken).ConfigureAwait(false);
            return ParseTxt(raw);
        }

        // ---- Per-record-type selectors (shared by all record types) ----

        private static readonly Func<Interop.Dnsapi.DNS_RECORD_HEADER, IntPtr, MxRecord> s_parseMx = static (hdr, dataPtr) =>
        {
            Interop.Dnsapi.DNS_MX_DATA data = Marshal.PtrToStructure<Interop.Dnsapi.DNS_MX_DATA>(dataPtr);
            return new MxRecord(PtrToString(data.pNameExchange) ?? string.Empty, data.wPreference, TimeSpan.FromSeconds(hdr.dwTtl));
        };

        private static readonly Func<Interop.Dnsapi.DNS_RECORD_HEADER, IntPtr, CNameRecord> s_parseCName = static (hdr, dataPtr) =>
        {
            Interop.Dnsapi.DNS_CNAME_DATA data = Marshal.PtrToStructure<Interop.Dnsapi.DNS_CNAME_DATA>(dataPtr);
            return new CNameRecord(PtrToString(data.pNameHost) ?? string.Empty, TimeSpan.FromSeconds(hdr.dwTtl));
        };

        private static readonly Func<Interop.Dnsapi.DNS_RECORD_HEADER, IntPtr, PtrRecord> s_parsePtr = static (hdr, dataPtr) =>
        {
            Interop.Dnsapi.DNS_PTR_DATA data = Marshal.PtrToStructure<Interop.Dnsapi.DNS_PTR_DATA>(dataPtr);
            return new PtrRecord(PtrToString(data.pNameHost) ?? string.Empty, TimeSpan.FromSeconds(hdr.dwTtl));
        };

        private static readonly Func<Interop.Dnsapi.DNS_RECORD_HEADER, IntPtr, NsRecord> s_parseNs = static (hdr, dataPtr) =>
        {
            Interop.Dnsapi.DNS_NS_DATA data = Marshal.PtrToStructure<Interop.Dnsapi.DNS_NS_DATA>(dataPtr);
            return new NsRecord(PtrToString(data.pNameHost) ?? string.Empty, TimeSpan.FromSeconds(hdr.dwTtl));
        };

        private static ushort AddressFamilyToQueryType(AddressFamily addressFamily) =>
            addressFamily switch
            {
                AddressFamily.InterNetwork => Interop.Dnsapi.DNS_TYPE_A,
                AddressFamily.InterNetworkV6 => Interop.Dnsapi.DNS_TYPE_AAAA,
                _ => throw new ArgumentException(SR.net_invalid_ip_addr, nameof(addressFamily)),
            };

        // ---- Query wrappers (issue the query, then parse the record list) ----

        private static async Task<DnsResult<AddressRecord>> QueryAddresses(IList<IPEndPoint> servers, bool async, string name, ushort qtype, CancellationToken cancellationToken)
        {
            using DnsQueryRawResult raw = await DnsQueryEx(servers, async, name, qtype, cancellationToken).ConfigureAwait(false);
            return ParseAddresses(raw, qtype);
        }

        private static async Task<DnsResult<TRecord>> QuerySimple<TRecord>(IList<IPEndPoint> servers, bool async, string name, ushort qtype, CancellationToken cancellationToken,
            Func<Interop.Dnsapi.DNS_RECORD_HEADER, IntPtr, TRecord> selector)
        {
            using DnsQueryRawResult raw = await DnsQueryEx(servers, async, name, qtype, cancellationToken).ConfigureAwait(false);
            return ParseSimple(raw, qtype, selector);
        }

        // ---- Record-list parsers ----

        private static DnsResult<AddressRecord> ParseAddresses(DnsQueryRawResult raw, ushort qtype)
        {
            if (raw.ResponseCode != DnsResponseCode.NoError)
            {
                return new DnsResult<AddressRecord>(raw.ResponseCode, null, raw.NegativeCacheTtl);
            }

            List<AddressRecord> records = new();
            for (IntPtr cur = raw.RecordsHead; cur != IntPtr.Zero; )
            {
                Interop.Dnsapi.DNS_RECORD_HEADER hdr = Marshal.PtrToStructure<Interop.Dnsapi.DNS_RECORD_HEADER>(cur);
                uint section = hdr.Flags & Interop.Dnsapi.DNSREC_SECTION_MASK;
                if (hdr.wType == qtype && section == Interop.Dnsapi.DNSREC_ANSWER)
                {
                    IntPtr dataPtr = cur + Unsafe.SizeOf<Interop.Dnsapi.DNS_RECORD_HEADER>();
                    if (TryParseAddress(hdr.wType, dataPtr, out IPAddress? address))
                    {
                        records.Add(new AddressRecord(address!, TimeSpan.FromSeconds(hdr.dwTtl)));
                    }
                }
                cur = hdr.pNext;
            }

            return new DnsResult<AddressRecord>(DnsResponseCode.NoError, records, raw.NegativeCacheTtl);
        }

        private static DnsResult<SrvRecord> ParseSrv(DnsQueryRawResult raw)
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
                Interop.Dnsapi.DNS_RECORD_HEADER hdr = Marshal.PtrToStructure<Interop.Dnsapi.DNS_RECORD_HEADER>(cur);
                uint section = hdr.Flags & Interop.Dnsapi.DNSREC_SECTION_MASK;
                if (hdr.wType == Interop.Dnsapi.DNS_TYPE_SRV && section == Interop.Dnsapi.DNSREC_ANSWER)
                {
                    IntPtr dataPtr = cur + Unsafe.SizeOf<Interop.Dnsapi.DNS_RECORD_HEADER>();
                    Interop.Dnsapi.DNS_SRV_DATA data = Marshal.PtrToStructure<Interop.Dnsapi.DNS_SRV_DATA>(dataPtr);
                    string target = PtrToString(data.pNameTarget) ?? string.Empty;
                    List<AddressRecord>? attached = null;
                    glue?.TryGetValue(target, out attached);
                    records.Add(new SrvRecord(target, data.wPort, data.wPriority, data.wWeight, TimeSpan.FromSeconds(hdr.dwTtl), attached));
                }
                cur = hdr.pNext;
            }

            return new DnsResult<SrvRecord>(DnsResponseCode.NoError, records, raw.NegativeCacheTtl);
        }

        private static DnsResult<TxtRecord> ParseTxt(DnsQueryRawResult raw)
        {
            if (raw.ResponseCode != DnsResponseCode.NoError)
            {
                return new DnsResult<TxtRecord>(raw.ResponseCode, null, raw.NegativeCacheTtl);
            }

            List<TxtRecord> records = new();
            for (IntPtr cur = raw.RecordsHead; cur != IntPtr.Zero; )
            {
                Interop.Dnsapi.DNS_RECORD_HEADER hdr = Marshal.PtrToStructure<Interop.Dnsapi.DNS_RECORD_HEADER>(cur);
                uint section = hdr.Flags & Interop.Dnsapi.DNSREC_SECTION_MASK;
                if (hdr.wType == Interop.Dnsapi.DNS_TYPE_TEXT && section == Interop.Dnsapi.DNSREC_ANSWER)
                {
                    IntPtr dataPtr = cur + Unsafe.SizeOf<Interop.Dnsapi.DNS_RECORD_HEADER>();
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

        private static DnsResult<TRecord> ParseSimple<TRecord>(DnsQueryRawResult raw, ushort qtype,
            Func<Interop.Dnsapi.DNS_RECORD_HEADER, IntPtr, TRecord> selector)
        {
            if (raw.ResponseCode != DnsResponseCode.NoError)
            {
                return new DnsResult<TRecord>(raw.ResponseCode, null, raw.NegativeCacheTtl);
            }

            List<TRecord> records = new();
            for (IntPtr cur = raw.RecordsHead; cur != IntPtr.Zero; )
            {
                Interop.Dnsapi.DNS_RECORD_HEADER hdr = Marshal.PtrToStructure<Interop.Dnsapi.DNS_RECORD_HEADER>(cur);
                uint section = hdr.Flags & Interop.Dnsapi.DNSREC_SECTION_MASK;
                if (hdr.wType == qtype && section == Interop.Dnsapi.DNSREC_ANSWER)
                {
                    IntPtr dataPtr = cur + Unsafe.SizeOf<Interop.Dnsapi.DNS_RECORD_HEADER>();
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
                TimeSpan mergedTtl = a.NegativeCacheTtl < b.NegativeCacheTtl ? a.NegativeCacheTtl : b.NegativeCacheTtl;
                return new DnsResult<AddressRecord>(DnsResponseCode.NoError, merged, mergedTtl);
            }

            DnsResponseCode chosenRc = a.ResponseCode == DnsResponseCode.NxDomain || b.ResponseCode == DnsResponseCode.NxDomain
                ? DnsResponseCode.NxDomain
                : (a.ResponseCode != DnsResponseCode.NoError ? a.ResponseCode : b.ResponseCode);
            TimeSpan negTtl = a.NegativeCacheTtl > TimeSpan.Zero ? a.NegativeCacheTtl : b.NegativeCacheTtl;
            return new DnsResult<AddressRecord>(chosenRc, null, negTtl);
        }

        private static bool TryParseAddress(ushort recordType, IntPtr dataPtr, out IPAddress? address)
        {
            if (recordType == Interop.Dnsapi.DNS_TYPE_A)
            {
                // DNS_A_DATA holds the IPv4 address as a uint already in network byte
                // order, which is exactly the layout the IPAddress(long) ctor expects.
                Interop.Dnsapi.DNS_A_DATA data = Marshal.PtrToStructure<Interop.Dnsapi.DNS_A_DATA>(dataPtr);
                address = new IPAddress((long)data.IpAddress);
                return true;
            }

            if (recordType == Interop.Dnsapi.DNS_TYPE_AAAA)
            {
                // DNS_AAAA_DATA holds the 16 raw IPv6 address bytes; the InlineArray
                // field exposes them as a span without any pointer arithmetic.
                Interop.Dnsapi.DNS_AAAA_DATA data = Marshal.PtrToStructure<Interop.Dnsapi.DNS_AAAA_DATA>(dataPtr);
                address = new IPAddress((ReadOnlySpan<byte>)data.Ip6Address);
                return true;
            }

            address = null;
            return false;
        }

        private static void ParseAdditionalAddresses(IntPtr head, ref Dictionary<string, List<AddressRecord>>? glue)
        {
            for (IntPtr cur = head; cur != IntPtr.Zero; )
            {
                Interop.Dnsapi.DNS_RECORD_HEADER hdr = Marshal.PtrToStructure<Interop.Dnsapi.DNS_RECORD_HEADER>(cur);
                uint section = hdr.Flags & Interop.Dnsapi.DNSREC_SECTION_MASK;
                bool isAddress = hdr.wType == Interop.Dnsapi.DNS_TYPE_A || hdr.wType == Interop.Dnsapi.DNS_TYPE_AAAA;
                if (section == Interop.Dnsapi.DNSREC_ADDITIONAL && isAddress)
                {
                    IntPtr dataPtr = cur + Unsafe.SizeOf<Interop.Dnsapi.DNS_RECORD_HEADER>();
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

        private static Task<DnsQueryRawResult> DnsQueryEx(IList<IPEndPoint> servers, bool async, string name, ushort queryType, CancellationToken cancellationToken)
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

        // Reinterprets native memory (records and result structures returned by
        // DnsQueryEx) as a managed read-only reference. The structures are blittable
        // and remain valid until the record list / result is freed, so this avoids the
        // marshalling copy of Marshal.PtrToStructure. The single pointer cast is
        // confined here so callers stay free of the 'unsafe' keyword.
        private static unsafe ref readonly T AsStruct<T>(IntPtr ptr) where T : unmanaged =>
            ref Unsafe.AsRef<T>((void*)ptr);

        // ---- Asynchronous DnsQueryEx state machine ----

        // Cached callback so we don't allocate a new delegate per query.
        private static readonly Interop.Dnsapi.DnsQueryCompletionRoutine s_completionCallback = QueryCompletionCallback;
        private static readonly IntPtr s_completionCallbackPtr =
            Marshal.GetFunctionPointerForDelegate(s_completionCallback);

        /// <summary>
        /// Holds the unmanaged state for a single DnsQueryEx invocation, including
        /// the request/result/cancel structures, the pinned query name, and the
        /// completion TaskCompletionSource.
        /// </summary>
        private sealed unsafe class DnsQueryAsyncState
        {
            private readonly TaskCompletionSource<DnsQueryRawResult> _tcs =
                new TaskCompletionSource<DnsQueryRawResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            private readonly string _name;
            private readonly ushort _queryType;
            private readonly CancellationToken _cancellationToken;
            private readonly IList<IPEndPoint> _servers;

            private GCHandle<DnsQueryAsyncState> _selfHandle;
            private IntPtr _namePtr;
            private IntPtr _requestPtr;
            private IntPtr _resultPtr;
            private IntPtr _cancelPtr;
            private IntPtr _serverListPtr;       // DNS_ADDR_ARRAY buffer
            private CancellationTokenRegistration _ctReg;
            private bool _completed;

            public DnsQueryAsyncState(IList<IPEndPoint> servers, string name, ushort queryType, CancellationToken cancellationToken)
            {
                _servers = servers;
                _name = name;
                _queryType = queryType;
                _cancellationToken = cancellationToken;
            }

            public Task<DnsQueryRawResult> StartAsync()
            {
                ValidateServerPorts(_servers);

                try
                {
                    _namePtr = Marshal.StringToHGlobalUni(_name);
                    _resultPtr = Marshal.AllocHGlobal(sizeof(Interop.Dnsapi.DNS_QUERY_RESULT));
                    NativeMemory.Clear((void*)_resultPtr, (nuint)sizeof(Interop.Dnsapi.DNS_QUERY_RESULT));
                    Interop.Dnsapi.DNS_QUERY_RESULT* result = (Interop.Dnsapi.DNS_QUERY_RESULT*)_resultPtr;
                    result->Version = Interop.Dnsapi.DNS_QUERY_REQUEST_VERSION1;

                    _cancelPtr = Marshal.AllocHGlobal(sizeof(Interop.Dnsapi.DNS_QUERY_CANCEL));
                    NativeMemory.Clear((void*)_cancelPtr, (nuint)sizeof(Interop.Dnsapi.DNS_QUERY_CANCEL));

                    _selfHandle = new GCHandle<DnsQueryAsyncState>(this);

                    _requestPtr = Marshal.AllocHGlobal(sizeof(Interop.Dnsapi.DNS_QUERY_REQUEST));
                    NativeMemory.Clear((void*)_requestPtr, (nuint)sizeof(Interop.Dnsapi.DNS_QUERY_REQUEST));
                    Interop.Dnsapi.DNS_QUERY_REQUEST* req = (Interop.Dnsapi.DNS_QUERY_REQUEST*)_requestPtr;
                    req->Version = Interop.Dnsapi.DNS_QUERY_REQUEST_VERSION1;
                    req->QueryName = _namePtr;
                    req->QueryType = _queryType;
                    req->QueryOptions = Interop.Dnsapi.DNS_QUERY_STANDARD;
                    req->InterfaceIndex = 0;
                    req->pQueryCompletionCallback = s_completionCallbackPtr;
                    req->pQueryContext = GCHandle<DnsQueryAsyncState>.ToIntPtr(_selfHandle);

                    if (_servers is { Count: > 0 })
                    {
                        BuildAddrArray(_servers, out _serverListPtr);
                        req->pDnsServerList = (Interop.Dnsapi.DNS_ADDR_ARRAY*)_serverListPtr;
                    }

                    int status = Interop.Dnsapi.DnsQueryEx(
                        (Interop.Dnsapi.DNS_QUERY_REQUEST*)_requestPtr,
                        (Interop.Dnsapi.DNS_QUERY_RESULT*)_resultPtr,
                        (Interop.Dnsapi.DNS_QUERY_CANCEL*)_cancelPtr);

                    if (status == Interop.Dnsapi.DNS_REQUEST_PENDING)
                    {
                        // Async. Register cancellation; the callback will free resources and complete the TCS.
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
                }
                catch
                {
                    FreeAll();
                    throw;
                }

                return _tcs.Task;
            }

            private void CancelAndAbort()
            {
                if (_cancelPtr != IntPtr.Zero)
                {
                    Interop.Dnsapi.DnsCancelQuery((Interop.Dnsapi.DNS_QUERY_CANCEL*)_cancelPtr);
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

                    Interop.Dnsapi.DNS_QUERY_RESULT result = Marshal.PtrToStructure<Interop.Dnsapi.DNS_QUERY_RESULT>(_resultPtr);
                    IntPtr records = result.pQueryRecords;

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
                if (_namePtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(_namePtr);
                    _namePtr = IntPtr.Zero;
                }
                if (_requestPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(_requestPtr);
                    _requestPtr = IntPtr.Zero;
                }
                if (_resultPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(_resultPtr);
                    _resultPtr = IntPtr.Zero;
                }
                if (_cancelPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(_cancelPtr);
                    _cancelPtr = IntPtr.Zero;
                }
                if (_serverListPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(_serverListPtr);
                    _serverListPtr = IntPtr.Zero;
                }
                if (_selfHandle.IsAllocated)
                {
                    _selfHandle.Dispose();
                }
            }
        }

        // Native callback. Marshaled to a function pointer once at startup.
        // We use a managed delegate (no UnmanagedCallersOnly) because callers
        // currently pass it via Marshal.GetFunctionPointerForDelegate.
        private static void QueryCompletionCallback(IntPtr pQueryContext, IntPtr pQueryResults)
        {
            try
            {
                DnsQueryAsyncState state = GCHandle<DnsQueryAsyncState>.FromIntPtr(pQueryContext).Target;

                // pQueryResults points to the same DNS_QUERY_RESULT we passed in.
                unsafe
                {
                    Interop.Dnsapi.DNS_QUERY_RESULT* res = (Interop.Dnsapi.DNS_QUERY_RESULT*)pQueryResults;
                    state.CompleteFromResult(res->QueryStatus);
                }
            }
            catch (Exception ex)
            {
                // Never allow exceptions to propagate into native code.
                Debug.Fail($"Unexpected exception in DnsQueryEx completion callback: {ex}");
            }
        }

        // DnsQueryEx only supports DNS servers reachable on the standard port 53.
        // The sockaddr port field passed to the API must be 0 (the API always
        // queries port 53); supplying any non-zero port - even 53 itself - results
        // in ERROR_INVALID_PARAMETER. We therefore reject any server endpoint that
        // requests a non-default port, since it cannot be honored on Windows.
        private static void ValidateServerPorts(IList<IPEndPoint> servers)
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
        private static unsafe DnsQueryRawResult DnsQueryExSync(IList<IPEndPoint> servers, string name, ushort queryType, CancellationToken cancellationToken)
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
                request.QueryName = namePtr;
                request.QueryType = queryType;
                request.QueryOptions = Interop.Dnsapi.DNS_QUERY_STANDARD;
                // No completion callback => synchronous execution.

                if (servers is { Count: > 0 })
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

        private static unsafe void BuildAddrArray(IList<IPEndPoint> servers, out IntPtr arrayPtr)
        {
            int count = servers.Count;

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
                WriteSockAddr(buffer.Slice(headerSize + (i * addrSize), addrSize), servers[i]);
            }
        }

        // Writes a SOCKADDR_IN or SOCKADDR_IN6 representation into the destination buffer.
        // The buffer must be at least 28 bytes (sizeof sockaddr_in6).
        private static void WriteSockAddr(Span<byte> dest, IPEndPoint endpoint)
        {
            // IPEndPoint.Serialize() yields the platform SOCKADDR layout (family, port,
            // address, and for IPv6 the flow info and scope id) via SocketAddressPal, so
            // there's no need to lay out the bytes by hand. DnsQueryEx always queries DNS
            // servers on port 53 and requires the sockaddr port field to be left as 0;
            // serializing a port-0 endpoint satisfies that.
            SocketAddress socketAddress = endpoint.Serialize();
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

        private static TimeSpan ExtractNegativeCacheTtl(IntPtr head)
        {
            // Walk the record list looking for an SOA in the authority section.
            for (IntPtr cur = head; cur != IntPtr.Zero; )
            {
                Interop.Dnsapi.DNS_RECORD_HEADER hdr = Marshal.PtrToStructure<Interop.Dnsapi.DNS_RECORD_HEADER>(cur);
                uint section = hdr.Flags & Interop.Dnsapi.DNSREC_SECTION_MASK;
                if (hdr.wType == Interop.Dnsapi.DNS_TYPE_SOA && section == Interop.Dnsapi.DNSREC_AUTHORITY)
                {
                    IntPtr dataPtr = cur + Unsafe.SizeOf<Interop.Dnsapi.DNS_RECORD_HEADER>();
                    Interop.Dnsapi.DNS_SOA_DATA soa = Marshal.PtrToStructure<Interop.Dnsapi.DNS_SOA_DATA>(dataPtr);
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
