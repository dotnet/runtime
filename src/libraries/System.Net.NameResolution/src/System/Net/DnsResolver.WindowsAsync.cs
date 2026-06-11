// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net
{
    public sealed partial class DnsResolver
    {
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

            private GCHandle _selfHandle;
            private IntPtr _namePtr;
            private IntPtr _requestPtr;
            private IntPtr _resultPtr;
            private IntPtr _cancelPtr;
            private IntPtr _serverListPtr;       // DNS_ADDR_ARRAY buffer
            private CancellationTokenRegistration _ctReg;
            private int _completed; // 0 = pending, 1 = completed (callback or sync)

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

                    _selfHandle = GCHandle.Alloc(this);

                    _requestPtr = Marshal.AllocHGlobal(sizeof(Interop.Dnsapi.DNS_QUERY_REQUEST));
                    NativeMemory.Clear((void*)_requestPtr, (nuint)sizeof(Interop.Dnsapi.DNS_QUERY_REQUEST));
                    Interop.Dnsapi.DNS_QUERY_REQUEST* req = (Interop.Dnsapi.DNS_QUERY_REQUEST*)_requestPtr;
                    req->Version = Interop.Dnsapi.DNS_QUERY_REQUEST_VERSION1;
                    req->QueryName = _namePtr;
                    req->QueryType = _queryType;
                    req->QueryOptions = Interop.Dnsapi.DNS_QUERY_STANDARD;
                    req->InterfaceIndex = 0;
                    req->pQueryCompletionCallback = s_completionCallbackPtr;
                    req->pQueryContext = GCHandle.ToIntPtr(_selfHandle);

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
                if (Interlocked.Exchange(ref _completed, 1) != 0)
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

                    // For NXDOMAIN/NODATA, try to extract the negative-cache TTL from the
                    // SOA in the authority section if it's present in the record list.
                    TimeSpan negativeTtl = TimeSpan.Zero;
                    if (rc != DnsResponseCode.NoError || records == IntPtr.Zero)
                    {
                        negativeTtl = ExtractNegativeCacheTtl(records);
                    }

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
                    _selfHandle.Free();
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
                GCHandle handle = GCHandle.FromIntPtr(pQueryContext);
                DnsQueryAsyncState? state = handle.Target as DnsQueryAsyncState;
                if (state == null)
                {
                    return;
                }

                // pQueryResults points to the same DNS_QUERY_RESULT we passed in.
                unsafe
                {
                    Interop.Dnsapi.DNS_QUERY_RESULT* res = (Interop.Dnsapi.DNS_QUERY_RESULT*)pQueryResults;
                    state.CompleteFromResult(res->QueryStatus);
                }
            }
            catch
            {
                // Swallow — never allow exceptions to propagate into native code.
            }
        }

        // DnsQueryEx only supports DNS servers reachable on the standard port 53.
        // The sockaddr port field passed to the API must be 0 (the API always
        // queries port 53); supplying any non-zero port - even 53 itself - results
        // in ERROR_INVALID_PARAMETER. We therefore reject any server endpoint that
        // requests a non-default port, since it cannot be honored on Windows.
        private static void ValidateServerPorts(IList<IPEndPoint> servers)
        {
            if (servers is { Count: > 0 })
            {
                foreach (IPEndPoint ep in servers)
                {
                    if (ep.Port != 0 && ep.Port != 53)
                    {
                        throw new PlatformNotSupportedException(SR.net_dns_custom_port_not_supported);
                    }
                }
            }
        }

        // Synchronous DnsQueryEx invocation. By omitting the completion callback the
        // API executes the query inline on the calling thread and returns the result
        // directly, so no GCHandle / TaskCompletionSource bookkeeping is required.
        private unsafe DnsQueryRawResult DnsQueryExSync(string name, ushort queryType, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ValidateServerPorts(_options.Servers);

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

                if (_options.Servers is { Count: > 0 })
                {
                    BuildAddrArray(_options.Servers, out serverListPtr);
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

                // For NXDOMAIN/NODATA, try to extract the negative-cache TTL from the
                // SOA in the authority section if it's present in the record list.
                TimeSpan negativeTtl = TimeSpan.Zero;
                if (rc != DnsResponseCode.NoError || records == IntPtr.Zero)
                {
                    negativeTtl = ExtractNegativeCacheTtl(records);
                }

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
            int headerSize = sizeof(Interop.Dnsapi.DNS_ADDR_ARRAY);
            int addrSize = sizeof(Interop.Dnsapi.DNS_ADDR);
            int totalSize = headerSize + addrSize * count;

            arrayPtr = Marshal.AllocHGlobal(totalSize);
            NativeMemory.Clear((void*)arrayPtr, (nuint)totalSize);

            Interop.Dnsapi.DNS_ADDR_ARRAY* arr = (Interop.Dnsapi.DNS_ADDR_ARRAY*)arrayPtr;
            arr->MaxCount = (uint)count;
            arr->AddrCount = (uint)count;
            arr->Family = (ushort)(servers[0].AddressFamily == AddressFamily.InterNetwork ? Interop.Dnsapi.AF_INET : Interop.Dnsapi.AF_INET6);

            byte* addrBase = (byte*)arrayPtr + headerSize;
            for (int i = 0; i < count; i++)
            {
                IPEndPoint ep = servers[i];
                byte* sa = addrBase + (i * addrSize);
                WriteSockAddr(sa, ep);
            }
        }

        // Writes a SOCKADDR_IN or SOCKADDR_IN6 representation into the destination buffer.
        // The buffer must be at least 28 bytes (sizeof sockaddr_in6).
        private static unsafe void WriteSockAddr(byte* dest, IPEndPoint ep)
        {
            // DnsQueryEx always queries DNS servers on port 53 and requires the sockaddr
            // port field to be left as 0. Supplying a non-zero port (even 53) is rejected
            // with ERROR_INVALID_PARAMETER. Non-default ports are validated and rejected
            // earlier in StartAsync, so the port is always written as 0 here.
            if (ep.AddressFamily == AddressFamily.InterNetwork)
            {
                // sockaddr_in: ushort family, ushort port (net order), uint addr, 8 bytes zero
                *(ushort*)(dest + 0) = Interop.Dnsapi.AF_INET;
                // dest[2..3] (port) left zero
                Span<byte> addrBytes = stackalloc byte[4];
                ep.Address.TryWriteBytes(addrBytes, out _);
                dest[4] = addrBytes[0];
                dest[5] = addrBytes[1];
                dest[6] = addrBytes[2];
                dest[7] = addrBytes[3];
                // dest[8..15] left zero
            }
            else if (ep.AddressFamily == AddressFamily.InterNetworkV6)
            {
                // sockaddr_in6: ushort family, ushort port, uint flowinfo, 16-byte addr, uint scope_id
                *(ushort*)(dest + 0) = Interop.Dnsapi.AF_INET6;
                // dest[2..3] (port) left zero
                // flowinfo (dest[4..7]) left zero
                Span<byte> addrBytes = stackalloc byte[16];
                ep.Address.TryWriteBytes(addrBytes, out _);
                for (int i = 0; i < 16; i++)
                {
                    dest[8 + i] = addrBytes[i];
                }
                // scope_id (dest[24..27])
                uint scopeId = (uint)ep.Address.ScopeId;
                dest[24] = (byte)(scopeId & 0xff);
                dest[25] = (byte)((scopeId >> 8) & 0xff);
                dest[26] = (byte)((scopeId >> 16) & 0xff);
                dest[27] = (byte)((scopeId >> 24) & 0xff);
            }
            else
            {
                throw new ArgumentException(SR.net_invalid_ip_addr);
            }
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
                    IntPtr dataPtr = cur + Marshal.SizeOf<Interop.Dnsapi.DNS_RECORD_HEADER>();
                    Interop.Dnsapi.DNS_SOA_DATA soa = Marshal.PtrToStructure<Interop.Dnsapi.DNS_SOA_DATA>(dataPtr);
                    // RFC 2308 §5: negative cache TTL = min(SOA TTL, SOA MINIMUM)
                    uint negTtl = Math.Min(hdr.dwTtl, soa.dwDefaultTtl);
                    return TimeSpan.FromSeconds(negTtl);
                }
                cur = hdr.pNext;
            }
            return TimeSpan.Zero;
        }
    }
}
