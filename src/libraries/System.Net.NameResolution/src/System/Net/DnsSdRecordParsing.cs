// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
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

        public static bool TryParseMx(DnsSdRecord record, out MxRecord parsed)
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

        public static bool TryParseTxt(DnsSdRecord record, out TxtRecord parsed)
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

        public static bool TryParseCName(DnsSdRecord record, out CNameRecord parsed)
        {
            if (TryParseDnsName(record.Data, out string name, out _))
            {
                parsed = new CNameRecord(name, TimeSpan.FromSeconds(record.Ttl));
                return true;
            }

            parsed = default;
            return false;
        }

        public static bool TryParsePtr(DnsSdRecord record, out PtrRecord parsed)
        {
            if (TryParseDnsName(record.Data, out string name, out _))
            {
                parsed = new PtrRecord(name, TimeSpan.FromSeconds(record.Ttl));
                return true;
            }

            parsed = default;
            return false;
        }

        public static bool TryParseNs(DnsSdRecord record, out NsRecord parsed)
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
    }
}
