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
        // ---- Public Resolve*Core methods (called from cross-platform DnsResolver) ----

        private async Task<DnsResult<AddressRecord>> ResolveAddressesCoreAsync(string name, AddressFamily addressFamily, CancellationToken cancellationToken)
        {
            if (addressFamily == AddressFamily.Unspecified)
            {
                // Issue A and AAAA in parallel; merge results.
                Task<DnsResult<AddressRecord>> aTask = QueryAddressesAsync(name, Interop.Dnsapi.DNS_TYPE_A, cancellationToken);
                Task<DnsResult<AddressRecord>> aaaaTask = QueryAddressesAsync(name, Interop.Dnsapi.DNS_TYPE_AAAA, cancellationToken);
                DnsResult<AddressRecord> aRes = await aTask.ConfigureAwait(false);
                DnsResult<AddressRecord> aaaaRes = await aaaaTask.ConfigureAwait(false);
                return MergeAddressResults(aRes, aaaaRes);
            }

            ushort qtype = addressFamily switch
            {
                AddressFamily.InterNetwork => Interop.Dnsapi.DNS_TYPE_A,
                AddressFamily.InterNetworkV6 => Interop.Dnsapi.DNS_TYPE_AAAA,
                _ => throw new ArgumentException(SR.net_invalid_ip_addr, nameof(addressFamily)),
            };
            return await QueryAddressesAsync(name, qtype, cancellationToken).ConfigureAwait(false);
        }

        private async Task<DnsResult<SrvRecord>> ResolveSrvCoreAsync(string name, CancellationToken cancellationToken)
        {
            DnsQueryRawResult raw = await DnsQueryExAsync(name, Interop.Dnsapi.DNS_TYPE_SRV, cancellationToken).ConfigureAwait(false);
            try
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
            finally
            {
                raw.Dispose();
            }
        }

        private Task<DnsResult<MxRecord>> ResolveMxCoreAsync(string name, CancellationToken cancellationToken)
            => QuerySimpleAsync<MxRecord>(name, Interop.Dnsapi.DNS_TYPE_MX, cancellationToken, static (hdr, dataPtr) =>
            {
                Interop.Dnsapi.DNS_MX_DATA data = Marshal.PtrToStructure<Interop.Dnsapi.DNS_MX_DATA>(dataPtr);
                return new MxRecord(PtrToString(data.pNameExchange) ?? string.Empty, data.wPreference, TimeSpan.FromSeconds(hdr.dwTtl));
            });

        private Task<DnsResult<CNameRecord>> ResolveCNameCoreAsync(string name, CancellationToken cancellationToken)
            => QuerySimpleAsync<CNameRecord>(name, Interop.Dnsapi.DNS_TYPE_CNAME, cancellationToken, static (hdr, dataPtr) =>
            {
                Interop.Dnsapi.DNS_CNAME_DATA data = Marshal.PtrToStructure<Interop.Dnsapi.DNS_CNAME_DATA>(dataPtr);
                return new CNameRecord(PtrToString(data.pNameHost) ?? string.Empty, TimeSpan.FromSeconds(hdr.dwTtl));
            });

        private Task<DnsResult<PtrRecord>> ResolvePtrCoreAsync(string name, CancellationToken cancellationToken)
            => QuerySimpleAsync<PtrRecord>(name, Interop.Dnsapi.DNS_TYPE_PTR, cancellationToken, static (hdr, dataPtr) =>
            {
                Interop.Dnsapi.DNS_PTR_DATA data = Marshal.PtrToStructure<Interop.Dnsapi.DNS_PTR_DATA>(dataPtr);
                return new PtrRecord(PtrToString(data.pNameHost) ?? string.Empty, TimeSpan.FromSeconds(hdr.dwTtl));
            });

        private Task<DnsResult<NsRecord>> ResolveNsCoreAsync(string name, CancellationToken cancellationToken)
            => QuerySimpleAsync<NsRecord>(name, Interop.Dnsapi.DNS_TYPE_NS, cancellationToken, static (hdr, dataPtr) =>
            {
                Interop.Dnsapi.DNS_NS_DATA data = Marshal.PtrToStructure<Interop.Dnsapi.DNS_NS_DATA>(dataPtr);
                return new NsRecord(PtrToString(data.pNameHost) ?? string.Empty, TimeSpan.FromSeconds(hdr.dwTtl));
            });

        private async Task<DnsResult<TxtRecord>> ResolveTxtCoreAsync(string name, CancellationToken cancellationToken)
        {
            DnsQueryRawResult raw = await DnsQueryExAsync(name, Interop.Dnsapi.DNS_TYPE_TEXT, cancellationToken).ConfigureAwait(false);
            try
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
            finally
            {
                raw.Dispose();
            }
        }

        // ---- Helpers for address parsing ----

        private async Task<DnsResult<AddressRecord>> QueryAddressesAsync(string name, ushort qtype, CancellationToken cancellationToken)
        {
            DnsQueryRawResult raw = await DnsQueryExAsync(name, qtype, cancellationToken).ConfigureAwait(false);
            try
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
            finally
            {
                raw.Dispose();
            }
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

        // ---- Generic single-record-type parser ----

        private async Task<DnsResult<TRecord>> QuerySimpleAsync<TRecord>(string name, ushort qtype, CancellationToken cancellationToken,
            Func<Interop.Dnsapi.DNS_RECORD_HEADER, IntPtr, TRecord> selector)
        {
            DnsQueryRawResult raw = await DnsQueryExAsync(name, qtype, cancellationToken).ConfigureAwait(false);
            try
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
            finally
            {
                raw.Dispose();
            }
        }

        // ---- Core DnsQueryEx async wrapper ----

        private unsafe Task<DnsQueryRawResult> DnsQueryExAsync(string name, ushort queryType, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<DnsQueryRawResult>(cancellationToken);
            }

            DnsQueryAsyncState state = new DnsQueryAsyncState(_options.Servers, name, queryType, cancellationToken);
            return state.StartAsync();
        }

        private static unsafe string? PtrToString(IntPtr p) =>
            p == IntPtr.Zero ? null : Marshal.PtrToStringUni(p);

        // ---- Raw query result returned by the low-level helper ----

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
