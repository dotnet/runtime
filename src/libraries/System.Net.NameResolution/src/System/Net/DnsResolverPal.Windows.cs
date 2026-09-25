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
            for (IntPtr cur = raw.RecordsHead; cur != IntPtr.Zero;)
            {
                ref readonly Interop.Dnsapi.DNS_RECORD_HEADER hdr = ref AsStruct<Interop.Dnsapi.DNS_RECORD_HEADER>(cur);
                uint section = hdr.Flags & Interop.Dnsapi.DNSREC_SECTION_MASK;
                // Network answers arrive in the ANSWER section, but locally-resolved names (e.g. "localhost"
                // or hosts-file entries) are surfaced by DnsQueryEx in the QUESTION section instead. An
                // address query only ever returns records for the queried name, so accept both sections to
                // avoid silently dropping the loopback/hosts results.
                if (hdr.wType == qtype && (section == Interop.Dnsapi.DNSREC_ANSWER || section == Interop.Dnsapi.DNSREC_QUESTION))
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
            for (IntPtr cur = raw.RecordsHead; cur != IntPtr.Zero;)
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
            for (IntPtr cur = raw.RecordsHead; cur != IntPtr.Zero;)
            {
                ref readonly Interop.Dnsapi.DNS_RECORD_HEADER hdr = ref AsStruct<Interop.Dnsapi.DNS_RECORD_HEADER>(cur);
                uint section = hdr.Flags & Interop.Dnsapi.DNSREC_SECTION_MASK;
                if (hdr.wType == Interop.Dnsapi.DNS_TYPE_TEXT && section == Interop.Dnsapi.DNSREC_ANSWER)
                {
                    IntPtr dataPtr = cur + sizeof(Interop.Dnsapi.DNS_RECORD_HEADER);
                    ref readonly Interop.Dnsapi.DNS_TXT_DATA data = ref AsStruct<Interop.Dnsapi.DNS_TXT_DATA>(dataPtr);
                    // DNS_TXT_DATA: uint dwStringCount; followed by array of PCWSTR.
                    int count = (int)data.dwStringCount;
                    IntPtr stringsPtr = dataPtr + sizeof(Interop.Dnsapi.DNS_TXT_DATA);
                    if (IntPtr.Size > sizeof(Interop.Dnsapi.DNS_TXT_DATA))
                    {
                        // Round up to pointer alignment.
                        long aligned = ((long)stringsPtr + (IntPtr.Size - 1)) & ~(long)(IntPtr.Size - 1);
                        stringsPtr = checked((nint)aligned);
                    }
                    string[] values = new string[count];
                    for (int i = 0; i < count; i++)
                    {
                        IntPtr strPtr = Marshal.ReadIntPtr(stringsPtr, i * IntPtr.Size);
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
            for (IntPtr cur = raw.RecordsHead; cur != IntPtr.Zero;)
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
            for (IntPtr cur = head; cur != IntPtr.Zero;)
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

        // Windows 10 and earlier have a DnsQueryEx bug: an asynchronous query that the OS can
        // satisfy synchronously (most notably "localhost") completes inline reporting success but
        // returns no records. The behavior is fixed in Windows 11 / Windows Server 2025 (build
        // 22000+). See https://dblohm7.ca/blog/2022/05/06/dnsqueryex-needs-love/.
        private static readonly bool s_asyncSyncCompletionBug = !OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000);

        // Cache the local hostname to avoid a native gethostname call on every query.
        // null means the lookup failed at startup; we treat that as "use synchronous path" (safe fallback).
        private static readonly Lazy<string?> s_hostName = new Lazy<string?>(() =>
        {
            try { return NameResolutionPal.GetHostName(); }
            catch (SocketException) { return null; }
        });

        private static bool IsSynchronouslyCompletingQueryName(string name)
        {
            if (IPAddress.IsValid(name))
            {
                return true;
            }

            ReadOnlySpan<char> normalizedName = name.AsSpan().TrimEnd('.');

            if (normalizedName.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                normalizedName.Equals("loopback", StringComparison.OrdinalIgnoreCase) ||
                normalizedName.Equals("..DnsServers", StringComparison.OrdinalIgnoreCase) ||
                normalizedName.Equals("..localmachine", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            string? hostName = s_hostName.Value;
            // If hostname lookup failed, use the synchronous path as a safe fallback.
            return hostName is null || normalizedName.Equals(hostName, StringComparison.OrdinalIgnoreCase);
        }

        private static Task<DnsQueryRawResult> DnsQueryEx(IPEndPoint[] servers, bool async, string name, ushort queryType, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<DnsQueryRawResult>(cancellationToken);
            }

            if (async && s_asyncSyncCompletionBug)
            {
                // On affected Windows versions a small set of special names always trips the
                // synchronous-completion bug, so resolve them on the synchronous path up front and
                // route every other asynchronous query through a wrapper that retries synchronously
                // if it hits the bug.
                if (IsSynchronouslyCompletingQueryName(name))
                {
                    async = false;
                }
                else
                {
                    return DnsQueryExAsyncWithSyncFallback(servers, name, queryType, cancellationToken);
                }
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

        // Works around the Windows 10 DnsQueryEx bug (see s_asyncSyncCompletionBug): if the
        // asynchronous call completes synchronously reporting success but without any records,
        // re-issue the query on the synchronous code path, which returns the records correctly.
        private static async Task<DnsQueryRawResult> DnsQueryExAsyncWithSyncFallback(IPEndPoint[] servers, string name, ushort queryType, CancellationToken cancellationToken)
        {
            DnsQueryAsyncState state = new DnsQueryAsyncState(servers, name, queryType, cancellationToken);
            DnsQueryRawResult result = await state.StartAsync().ConfigureAwait(false);

            if (state.CompletedSynchronously && result.ResponseCode == DnsResponseCode.NoError && result.RecordsHead == IntPtr.Zero)
            {
                result.Dispose();
                return DnsQueryExSync(servers, name, queryType, cancellationToken);
            }

            return result;
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

        // Bundles the three fixed-size native structures DnsQueryEx needs into a single block so
        // one SafeHandle can own their lifetime. The query name and server-list buffers are
        // variable-length, so they are allocated separately and freed by the handle as well.
        [StructLayout(LayoutKind.Sequential)]
        private struct DnsQueryBuffers
        {
            public Interop.Dnsapi.DNS_QUERY_REQUEST Request;
            public Interop.Dnsapi.DNS_QUERY_RESULT Result;
            public Interop.Dnsapi.DNS_QUERY_CANCEL Cancel;
        }

        // Owns the native buffers backing a single asynchronous DnsQueryEx call. SafeHandle
        // reference counting is what makes cancellation thread-safe: DnsCancelQuery runs under
        // DangerousAddRef so the Cancel buffer cannot be freed while a cancel call is in flight,
        // and once the completion path disposes the handle any later cancellation attempt observes
        // a closed handle and becomes a no-op instead of dereferencing freed memory.
        private sealed unsafe class DnsQuerySafeHandle : SafeHandle
        {
            private IntPtr _namePtr;
            private IntPtr _serverListPtr;

            public DnsQuerySafeHandle()
                : base(IntPtr.Zero, ownsHandle: true)
            {
            }

            public override bool IsInvalid => handle == IntPtr.Zero;

            public DnsQueryBuffers* Buffers => (DnsQueryBuffers*)handle;

            public void Allocate(string name, IPEndPoint[] servers, ushort queryType,
                delegate* unmanaged[Stdcall]<nint, nint, void> completionCallback, IntPtr context)
            {
                DnsQueryBuffers* buffers = (DnsQueryBuffers*)NativeMemory.AllocZeroed((nuint)sizeof(DnsQueryBuffers));
                SetHandle((IntPtr)buffers);

                _namePtr = Marshal.StringToHGlobalUni(name);

                buffers->Result.Version = Interop.Dnsapi.DNS_QUERY_REQUEST_VERSION1;

                buffers->Request.Version = Interop.Dnsapi.DNS_QUERY_REQUEST_VERSION1;
                buffers->Request.QueryName = (char*)_namePtr;
                buffers->Request.QueryType = queryType;
                buffers->Request.QueryOptions = Interop.Dnsapi.DNS_QUERY_STANDARD;
                buffers->Request.pQueryCompletionCallback = completionCallback;
                buffers->Request.pQueryContext = context;

                if (servers is { Length: > 0 })
                {
                    BuildAddrArray(servers, out _serverListPtr);
                    buffers->Request.pDnsServerList = (Interop.Dnsapi.DNS_ADDR_ARRAY*)_serverListPtr;
                }
            }

            protected override bool ReleaseHandle()
            {
                if (_namePtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(_namePtr);
                }
                if (_serverListPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(_serverListPtr);
                }
                NativeMemory.Free((void*)handle);
                return true;
            }
        }

        /// <summary>
        /// Holds the managed state for a single asynchronous DnsQueryEx invocation: the native
        /// buffers (via <see cref="DnsQuerySafeHandle"/>), the GC handle handed to the native
        /// completion callback, the cancellation registration, and the completion source.
        /// </summary>
        private sealed unsafe class DnsQueryAsyncState
        {
            private readonly TaskCompletionSource<DnsQueryRawResult> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
            private readonly string _name;
            private readonly ushort _queryType;
            private readonly CancellationToken _cancellationToken;
            private readonly IPEndPoint[] _servers;
            private readonly object _lock = new();

            private DnsQuerySafeHandle _handle = null!;
            private GCHandle<DnsQueryAsyncState> _selfHandle;
            private CancellationTokenRegistration _ctReg;
            private bool _completed;

            // Set when DnsQueryEx returned a result inline instead of going pending. Read after the
            // returned task completes to detect the Windows 10 synchronous-completion bug.
            public bool CompletedSynchronously { get; private set; }

            public DnsQueryAsyncState(IPEndPoint[] servers, string name, ushort queryType, CancellationToken cancellationToken)
            {
                _servers = servers;
                _name = name;
                _queryType = queryType;
                _cancellationToken = cancellationToken;
            }

            public Task<DnsQueryRawResult> StartAsync()
            {
                _handle = new DnsQuerySafeHandle();
                _selfHandle = new GCHandle<DnsQueryAsyncState>(this);

                int status;
                try
                {
                    _handle.Allocate(_name, _servers, _queryType, &QueryCompletionCallback,
                        GCHandle<DnsQueryAsyncState>.ToIntPtr(_selfHandle));

                    DnsQueryBuffers* buffers = _handle.Buffers;
                    status = Interop.Dnsapi.DnsQueryEx(&buffers->Request, &buffers->Result, &buffers->Cancel);
                }
                catch
                {
                    _handle.Dispose();
                    _selfHandle.Dispose();
                    throw;
                }

                if (status == Interop.Dnsapi.DNS_REQUEST_PENDING)
                {
                    // The query is in-flight; the native runtime owns the buffers until the
                    // completion callback fires. Register cancellation, then publish the
                    // registration under _lock and check whether completion already ran (it may
                    // race ahead on another thread the instant DnsQueryEx returned PENDING). If it
                    // did, we dispose the registration here; otherwise CompleteFromResult disposes
                    // it. Either way the SafeHandle keeps the Cancel buffer alive across any
                    // DnsCancelQuery call, so cancellation can never touch freed memory.
                    CancellationTokenRegistration registration = _cancellationToken.UnsafeRegister(static (s, _) =>
                    {
                        ((DnsQueryAsyncState)s!).CancelAndAbort();
                    }, this);

                    bool alreadyCompleted;
                    lock (_lock)
                    {
                        _ctReg = registration;
                        alreadyCompleted = _completed;
                    }

                    if (alreadyCompleted)
                    {
                        registration.Dispose();
                    }
                }
                else
                {
                    // Synchronous completion: the callback was NOT invoked; we complete inline.
                    CompletedSynchronously = true;
                    CompleteFromResult(status);
                }

                return _tcs.Task;
            }

            private void CancelAndAbort()
            {
                bool addedRef = false;
                try
                {
                    // AddRef pins the native buffers for the duration of the cancel call. If the
                    // completion path has already disposed the handle, AddRef throws and we treat
                    // the cancellation as a no-op.
                    _handle.DangerousAddRef(ref addedRef);
                    Interop.Dnsapi.DnsCancelQuery(&_handle.Buffers->Cancel);
                }
                catch (ObjectDisposedException)
                {
                    // Completion already disposed the handle and freed the buffers; nothing left
                    // to cancel.
                }
                finally
                {
                    if (addedRef)
                    {
                        _handle.DangerousRelease();
                    }
                }
            }

            /// <summary>
            /// Invoked exactly once, from either the native completion callback or the inline
            /// synchronous-completion path. Parses the QueryStatus and pQueryRecords from the
            /// result struct, completes the TCS, and releases all native state.
            /// </summary>
            internal void CompleteFromResult(int status)
            {
                CancellationTokenRegistration registration;
                lock (_lock)
                {
                    _completed = true;
                    registration = _ctReg;
                }

                // Dispose the registration outside the lock. This also waits for any in-flight
                // CancelAndAbort to finish before we dispose the handle below, so DnsCancelQuery
                // can never observe a freed buffer.
                registration.Dispose();

                try
                {
                    IntPtr records = _handle.Buffers->Result.pQueryRecords;

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

                    // Best-effort negative-cache TTL from an authority-section SOA. Note that
                    // DnsQueryEx returns no records at all for negative responses (empty answer
                    // section), so in practice this yields zero for NXDOMAIN/NODATA on Windows;
                    // it still applies for the rare positive response that carries an SOA.
                    TimeSpan negativeTtl = ExtractNegativeCacheTtl(records);

                    _tcs.TrySetResult(new DnsQueryRawResult(rc, records, negativeTtl));
                }
                catch (Exception ex)
                {
                    _tcs.TrySetException(ex);
                }
                finally
                {
                    _handle.Dispose();
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

        // Validates that the caller-specified DNS servers can be honored by DnsQueryEx.
        // Both restrictions are specific to the Windows DnsQueryEx API, so they live in
        // the PAL rather than the cross-platform constructor:
        //  * DnsQueryEx always queries DNS servers on the standard port 53 and requires
        //    the sockaddr port field passed to the API to be left as 0; supplying any
        //    other non-zero port results in ERROR_INVALID_PARAMETER. We accept either 0
        //    ("use the default port") or 53 (the port DnsQueryEx will actually use) and
        //    normalize both to 0 when building the native server list (see WriteSockAddr).
        //    Any other port cannot be honored on Windows and is rejected here.
        //  * DnsQueryEx encodes a single address family for the whole DNS_ADDR_ARRAY, so
        //    all endpoints must share one family; mixed IPv4/IPv6 lists are rejected.
        public static void ValidateServers(IPEndPoint[] servers)
        {
            if (servers.Length == 0)
            {
                return;
            }

            AddressFamily family = servers[0].AddressFamily;
            foreach (IPEndPoint ep in servers)
            {
                if (ep.Port != 0 && ep.Port != 53)
                {
                    throw new PlatformNotSupportedException(SR.net_dns_custom_port_not_supported);
                }

                if (ep.AddressFamily != family)
                {
                    throw new ArgumentException(SR.net_dns_mixed_address_families, nameof(DnsResolverOptions.Servers));
                }
            }
        }

        // Synchronous DnsQueryEx invocation. By omitting the completion callback the
        // API executes the query inline on the calling thread and returns the result
        // directly, so no GCHandle / TaskCompletionSource bookkeeping is required.
        private static unsafe DnsQueryRawResult DnsQueryExSync(IPEndPoint[] servers, string name, ushort queryType, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

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

                // Best-effort negative-cache TTL from an authority-section SOA. Note that
                // DnsQueryEx returns no records at all for negative responses (empty answer
                // section), so in practice this yields zero for NXDOMAIN/NODATA on Windows;
                // it still applies for the rare positive response that carries an SOA.
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

            // DnsQueryEx encodes a single address family for the whole array. The caller
            // (DnsResolver constructor, via ValidateServers) has already rejected mixed
            // IPv4/IPv6 lists, so it is safe to take the family from the first endpoint.
            AddressFamily family = servers[0].AddressFamily;

            int headerSize = sizeof(Interop.Dnsapi.DNS_ADDR_ARRAY);
            int addrSize = sizeof(Interop.Dnsapi.DNS_ADDR);
            int totalSize = checked(headerSize + addrSize * count);

            arrayPtr = Marshal.AllocHGlobal(totalSize);

            // Wrap the unmanaged buffer in a span and populate it without pointer arithmetic.
            Span<byte> buffer = new Span<byte>((void*)arrayPtr, totalSize);
            buffer.Clear();

            ref Interop.Dnsapi.DNS_ADDR_ARRAY arr = ref MemoryMarshal.AsRef<Interop.Dnsapi.DNS_ADDR_ARRAY>(buffer);
            arr.MaxCount = (uint)totalSize;
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

        // Extracts the negative-cache TTL from an authority-section SOA, per RFC 2308 §5.
        // Returns TimeSpan.Zero when no such SOA is present. In practice DnsQueryEx drops
        // the authority section for negative responses (see callers), so this is best-effort;
        // a future Windows-11+ path using DnsQueryRaw could parse the raw message instead.
        private static unsafe TimeSpan ExtractNegativeCacheTtl(IntPtr head)
        {
            // Walk the record list looking for an SOA in the authority section.
            for (IntPtr cur = head; cur != IntPtr.Zero;)
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
