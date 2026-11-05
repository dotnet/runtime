// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text;

namespace System.Net
{
    // Rdata returned by macOS mDNSResponder (DNSServiceQueryRecord).
    internal readonly struct DnsSdRecord
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

    internal delegate bool TryParseDnsSdRecord<TRecord>(DnsSdRecord record, out TRecord parsed);

    // Parses rdata returned by macOS mDNSResponder into typed DNS records. Kept separate
    // from DnsResolverPal.OSX so it can be unit-tested without reaching into PAL internals.
    internal static class DnsSdRecordParsing
    {
        public static bool TryParseAddress(DnsSdRecord record, out AddressRecord parsed)
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

        public static bool TryParseSrv(DnsSdRecord record, out SrvRecord parsed)
        {
            DnsRecord dnsRecord = ToDnsRecord(record);
            if (dnsRecord.TryParseSrvRecord(out DnsSrvRecordData srv))
            {
                parsed = new SrvRecord(
                    srv.Target.ToString(),
                    srv.Port,
                    srv.Priority,
                    srv.Weight,
                    TimeSpan.FromSeconds(record.Ttl),
                    // DNSServiceQueryRecord exposes only the queried record's rdata, not
                    // additional-section glue A/AAAA records.
                    null);
                return true;
            }

            parsed = default;
            return false;
        }

        public static bool TryParseMx(DnsSdRecord record, out MxRecord parsed)
        {
            DnsRecord dnsRecord = ToDnsRecord(record);
            if (dnsRecord.TryParseMxRecord(out DnsMxRecordData mx))
            {
                parsed = new MxRecord(mx.Exchange.ToString(), mx.Preference, TimeSpan.FromSeconds(record.Ttl));
                return true;
            }

            parsed = default;
            return false;
        }

        public static bool TryParseTxt(DnsSdRecord record, out TxtRecord parsed)
        {
            DnsRecord dnsRecord = ToDnsRecord(record);
            if (!dnsRecord.TryParseTxtRecord(out DnsTxtRecordData txt))
            {
                parsed = default;
                return false;
            }

            List<string> values = new();
            DnsTxtEnumerator enumerator = txt.EnumerateStrings();
            while (enumerator.MoveNext())
            {
                values.Add(Encoding.UTF8.GetString(enumerator.Current));
            }

            if (!enumerator.IsValid)
            {
                parsed = default;
                return false;
            }

            parsed = new TxtRecord(values, TimeSpan.FromSeconds(record.Ttl));
            return true;
        }

        public static bool TryParseCName(DnsSdRecord record, out CNameRecord parsed)
        {
            DnsRecord dnsRecord = ToDnsRecord(record);
            if (dnsRecord.TryParseCNameRecord(out DnsCNameRecordData cname))
            {
                parsed = new CNameRecord(cname.CName.ToString(), TimeSpan.FromSeconds(record.Ttl));
                return true;
            }

            parsed = default;
            return false;
        }

        public static bool TryParsePtr(DnsSdRecord record, out PtrRecord parsed)
        {
            DnsRecord dnsRecord = ToDnsRecord(record);
            if (dnsRecord.TryParsePtrRecord(out DnsPtrRecordData ptr))
            {
                parsed = new PtrRecord(ptr.Name.ToString(), TimeSpan.FromSeconds(record.Ttl));
                return true;
            }

            parsed = default;
            return false;
        }

        public static bool TryParseNs(DnsSdRecord record, out NsRecord parsed)
        {
            DnsRecord dnsRecord = ToDnsRecord(record);
            if (dnsRecord.TryParseNsRecord(out DnsNsRecordData ns))
            {
                parsed = new NsRecord(ns.Name.ToString(), TimeSpan.FromSeconds(record.Ttl));
                return true;
            }

            parsed = default;
            return false;
        }

        private static DnsRecord ToDnsRecord(DnsSdRecord record) =>
            new DnsRecord(default, (DnsRecordType)record.Type, DnsRecordClass.Internet,
                record.Ttl, record.Data, record.Data, 0);
    }
}
