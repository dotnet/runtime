// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
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
        // ---- Resolve*Core methods (called from cross-platform DnsResolver) ----
        //
        // Each method takes a `bool async` flag controlling whether the underlying
        // DnsQueryEx call is issued asynchronously (via the completion-callback state
        // machine) or synchronously (inline on the calling thread). When async is
        // false the returned Task is already completed, so the synchronous public
        // entry points can safely unwrap it without blocking a thread.

        private async Task<DnsResult<AddressRecord>> ResolveAddressesCore(bool async, string name, AddressFamily addressFamily, CancellationToken cancellationToken)
        {
            if (addressFamily == AddressFamily.Unspecified)
            {
                if (async)
                {
                    // Issue A and AAAA in parallel; merge results.
                    Task<DnsResult<AddressRecord>> aTask = QueryAddresses(async: true, name, Interop.Dnsapi.DNS_TYPE_A, cancellationToken);
                    Task<DnsResult<AddressRecord>> aaaaTask = QueryAddresses(async: true, name, Interop.Dnsapi.DNS_TYPE_AAAA, cancellationToken);
                    DnsResult<AddressRecord> aRes = await aTask.ConfigureAwait(false);
                    DnsResult<AddressRecord> aaaaRes = await aaaaTask.ConfigureAwait(false);
                    return MergeAddressResults(aRes, aaaaRes);
                }
                else
                {
                    // Synchronous: query A then AAAA sequentially.
                    DnsResult<AddressRecord> aRes = await QueryAddresses(async: false, name, Interop.Dnsapi.DNS_TYPE_A, cancellationToken).ConfigureAwait(false);
                    DnsResult<AddressRecord> aaaaRes = await QueryAddresses(async: false, name, Interop.Dnsapi.DNS_TYPE_AAAA, cancellationToken).ConfigureAwait(false);
                    return MergeAddressResults(aRes, aaaaRes);
                }
            }

            ushort qtype = AddressFamilyToQueryType(addressFamily);
            return await QueryAddresses(async, name, qtype, cancellationToken).ConfigureAwait(false);
        }

        private async Task<DnsResult<SrvRecord>> ResolveSrvCore(bool async, string name, CancellationToken cancellationToken)
        {
            DnsQueryRawResult raw = await DnsQueryEx(async, name, Interop.Dnsapi.DNS_TYPE_SRV, cancellationToken).ConfigureAwait(false);
            try
            {
                return ParseSrv(raw);
            }
            finally
            {
                raw.Dispose();
            }
        }

        private Task<DnsResult<MxRecord>> ResolveMxCore(bool async, string name, CancellationToken cancellationToken)
            => QuerySimple(async, name, Interop.Dnsapi.DNS_TYPE_MX, cancellationToken, s_parseMx);

        private Task<DnsResult<CNameRecord>> ResolveCNameCore(bool async, string name, CancellationToken cancellationToken)
            => QuerySimple(async, name, Interop.Dnsapi.DNS_TYPE_CNAME, cancellationToken, s_parseCName);

        private Task<DnsResult<PtrRecord>> ResolvePtrCore(bool async, string name, CancellationToken cancellationToken)
            => QuerySimple(async, name, Interop.Dnsapi.DNS_TYPE_PTR, cancellationToken, s_parsePtr);

        private Task<DnsResult<NsRecord>> ResolveNsCore(bool async, string name, CancellationToken cancellationToken)
            => QuerySimple(async, name, Interop.Dnsapi.DNS_TYPE_NS, cancellationToken, s_parseNs);

        private async Task<DnsResult<TxtRecord>> ResolveTxtCore(bool async, string name, CancellationToken cancellationToken)
        {
            DnsQueryRawResult raw = await DnsQueryEx(async, name, Interop.Dnsapi.DNS_TYPE_TEXT, cancellationToken).ConfigureAwait(false);
            try
            {
                return ParseTxt(raw);
            }
            finally
            {
                raw.Dispose();
            }
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

        private async Task<DnsResult<AddressRecord>> QueryAddresses(bool async, string name, ushort qtype, CancellationToken cancellationToken)
        {
            DnsQueryRawResult raw = await DnsQueryEx(async, name, qtype, cancellationToken).ConfigureAwait(false);
            try
            {
                return ParseAddresses(raw, qtype);
            }
            finally
            {
                raw.Dispose();
            }
        }

        private async Task<DnsResult<TRecord>> QuerySimple<TRecord>(bool async, string name, ushort qtype, CancellationToken cancellationToken,
            Func<Interop.Dnsapi.DNS_RECORD_HEADER, IntPtr, TRecord> selector)
        {
            DnsQueryRawResult raw = await DnsQueryEx(async, name, qtype, cancellationToken).ConfigureAwait(false);
            try
            {
                return ParseSimple(raw, qtype, selector);
            }
            finally
            {
                raw.Dispose();
            }
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
                    IntPtr dataPtr = cur + Marshal.SizeOf<Interop.Dnsapi.DNS_RECORD_HEADER>();
                    if (TryParseAddress(hdr.wType, dataPtr, out IPAddress? address))
                    {
                        records.Add(new AddressRecord(address!, TimeSpan.FromSeconds(hdr.dwTtl)));
                    }
                }
                cur = hdr.pNext;
            }

            return new DnsResult<AddressRecord>(DnsResponseCode.NoError, records, TimeSpan.Zero);
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
                    IntPtr dataPtr = cur + Marshal.SizeOf<Interop.Dnsapi.DNS_RECORD_HEADER>();
                    Interop.Dnsapi.DNS_SRV_DATA data = Marshal.PtrToStructure<Interop.Dnsapi.DNS_SRV_DATA>(dataPtr);
                    string target = PtrToString(data.pNameTarget) ?? string.Empty;
                    IReadOnlyList<AddressRecord>? attached = null;
                    if (glue != null && glue.TryGetValue(target, out List<AddressRecord>? list))
                    {
                        attached = list;
                    }
                    records.Add(new SrvRecord(target, data.wPort, data.wPriority, data.wWeight, TimeSpan.FromSeconds(hdr.dwTtl), attached));
                }
                cur = hdr.pNext;
            }

            return new DnsResult<SrvRecord>(DnsResponseCode.NoError, records, TimeSpan.Zero);
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
                    IntPtr dataPtr = cur + Marshal.SizeOf<Interop.Dnsapi.DNS_RECORD_HEADER>();
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

            return new DnsResult<TxtRecord>(DnsResponseCode.NoError, records, TimeSpan.Zero);
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
                    IntPtr dataPtr = cur + Marshal.SizeOf<Interop.Dnsapi.DNS_RECORD_HEADER>();
                    records.Add(selector(hdr, dataPtr));
                }
                cur = hdr.pNext;
            }

            return new DnsResult<TRecord>(DnsResponseCode.NoError, records, TimeSpan.Zero);
        }

        private static DnsResult<AddressRecord> MergeAddressResults(DnsResult<AddressRecord> a, DnsResult<AddressRecord> b)
        {
            if (a.Records.Count > 0 || b.Records.Count > 0)
            {
                AddressRecord[] merged = new AddressRecord[a.Records.Count + b.Records.Count];
                int idx = 0;
                for (int i = 0; i < a.Records.Count; i++) merged[idx++] = a.Records[i];
                for (int i = 0; i < b.Records.Count; i++) merged[idx++] = b.Records[i];
                return new DnsResult<AddressRecord>(DnsResponseCode.NoError, merged, TimeSpan.Zero);
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
                uint ip = (uint)Marshal.ReadInt32(dataPtr);
                address = new IPAddress(ip);
                return true;
            }
            if (recordType == Interop.Dnsapi.DNS_TYPE_AAAA)
            {
                byte[] bytes = new byte[16];
                Marshal.Copy(dataPtr, bytes, 0, 16);
                address = new IPAddress(bytes);
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
                    IntPtr dataPtr = cur + Marshal.SizeOf<Interop.Dnsapi.DNS_RECORD_HEADER>();
                    if (TryParseAddress(hdr.wType, dataPtr, out IPAddress? address))
                    {
                        string name = PtrToString(hdr.pName) ?? string.Empty;
                        glue ??= new Dictionary<string, List<AddressRecord>>(StringComparer.OrdinalIgnoreCase);
                        if (!glue.TryGetValue(name, out List<AddressRecord>? list))
                        {
                            list = new List<AddressRecord>();
                            glue[name] = list;
                        }
                        list.Add(new AddressRecord(address!, TimeSpan.FromSeconds(hdr.dwTtl)));
                    }
                }
                cur = hdr.pNext;
            }
        }

        // ---- Core DnsQueryEx wrapper ----

        private unsafe Task<DnsQueryRawResult> DnsQueryEx(bool async, string name, ushort queryType, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<DnsQueryRawResult>(cancellationToken);
            }

            if (async)
            {
                DnsQueryAsyncState state = new DnsQueryAsyncState(_options.Servers, name, queryType, cancellationToken);
                return state.StartAsync();
            }

            // Synchronous: the result is produced inline, so the returned Task is
            // already completed and the sync entry points unwrap it without blocking.
            return Task.FromResult(DnsQueryExSync(name, queryType, cancellationToken));
        }

        private static unsafe string? PtrToString(IntPtr p) =>
            p == IntPtr.Zero ? null : Marshal.PtrToStringUni(p);

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
