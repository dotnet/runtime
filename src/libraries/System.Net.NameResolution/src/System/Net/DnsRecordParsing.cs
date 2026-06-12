// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;

namespace System.Net
{
    // Typed RDATA accessors over parsed DNS records.

    internal readonly ref struct DnsARecordData
    {
        public ReadOnlySpan<byte> AddressBytes { get; }

        internal DnsARecordData(ReadOnlySpan<byte> addressBytes)
        {
            AddressBytes = addressBytes;
        }

        public IPAddress ToIPAddress() => new IPAddress(AddressBytes);
    }

    internal readonly ref struct DnsAAAARecordData
    {
        public ReadOnlySpan<byte> AddressBytes { get; }

        internal DnsAAAARecordData(ReadOnlySpan<byte> addressBytes)
        {
            AddressBytes = addressBytes;
        }

        public IPAddress ToIPAddress() => new IPAddress(AddressBytes);
    }

    internal readonly ref struct DnsCNameRecordData
    {
        public DnsEncodedName CName { get; }

        internal DnsCNameRecordData(DnsEncodedName cname)
        {
            CName = cname;
        }
    }

    internal readonly ref struct DnsMxRecordData
    {
        public ushort Preference { get; }
        public DnsEncodedName Exchange { get; }

        internal DnsMxRecordData(ushort preference, DnsEncodedName exchange)
        {
            Preference = preference;
            Exchange = exchange;
        }
    }

    internal readonly ref struct DnsSrvRecordData
    {
        public ushort Priority { get; }
        public ushort Weight { get; }
        public ushort Port { get; }
        public DnsEncodedName Target { get; }

        internal DnsSrvRecordData(ushort priority, ushort weight, ushort port, DnsEncodedName target)
        {
            Priority = priority;
            Weight = weight;
            Port = port;
            Target = target;
        }
    }

    internal readonly ref struct DnsSoaRecordData
    {
        public DnsEncodedName PrimaryNameServer { get; }
        public DnsEncodedName ResponsibleMailbox { get; }
        public uint SerialNumber { get; }
        public uint RefreshInterval { get; }
        public uint RetryInterval { get; }
        public uint ExpireLimit { get; }
        public uint MinimumTtl { get; }

        internal DnsSoaRecordData(DnsEncodedName primaryNameServer, DnsEncodedName responsibleMailbox,
            uint serialNumber, uint refreshInterval, uint retryInterval,
            uint expireLimit, uint minimumTtl)
        {
            PrimaryNameServer = primaryNameServer;
            ResponsibleMailbox = responsibleMailbox;
            SerialNumber = serialNumber;
            RefreshInterval = refreshInterval;
            RetryInterval = retryInterval;
            ExpireLimit = expireLimit;
            MinimumTtl = minimumTtl;
        }
    }

    internal readonly ref struct DnsTxtRecordData
    {
        private readonly ReadOnlySpan<byte> _data;

        internal DnsTxtRecordData(ReadOnlySpan<byte> data)
        {
            _data = data;
        }

        public DnsTxtEnumerator EnumerateStrings() => new DnsTxtEnumerator(_data);
    }

    internal ref struct DnsTxtEnumerator
    {
        private ReadOnlySpan<byte> _remaining;
        private ReadOnlySpan<byte> _current;

        internal DnsTxtEnumerator(ReadOnlySpan<byte> data)
        {
            _remaining = data;
            _current = default;
        }

        public readonly ReadOnlySpan<byte> Current => _current;

        public bool MoveNext()
        {
            if (_remaining.Length == 0)
            {
                return false;
            }

            int len = _remaining[0];
            if (1 + len > _remaining.Length)
            {
                return false;
            }

            _current = _remaining.Slice(1, len);
            _remaining = _remaining[(1 + len)..];
            return true;
        }

        public readonly DnsTxtEnumerator GetEnumerator() => this;
    }

    internal readonly ref struct DnsPtrRecordData
    {
        public DnsEncodedName Name { get; }

        internal DnsPtrRecordData(DnsEncodedName name)
        {
            Name = name;
        }
    }

    internal readonly ref struct DnsNsRecordData
    {
        public DnsEncodedName Name { get; }

        internal DnsNsRecordData(DnsEncodedName name)
        {
            Name = name;
        }
    }

    internal static class DnsRecordExtensions
    {
        public static bool TryParseARecord(this DnsRecord record, out DnsARecordData result)
        {
            result = default;
            if (record.Type != DnsRecordType.A || record.Data.Length != 4)
            {
                return false;
            }
            result = new DnsARecordData(record.Data);
            return true;
        }

        public static bool TryParseAAAARecord(this DnsRecord record, out DnsAAAARecordData result)
        {
            result = default;
            if (record.Type != DnsRecordType.AAAA || record.Data.Length != 16)
            {
                return false;
            }
            result = new DnsAAAARecordData(record.Data);
            return true;
        }

        public static bool TryParseCNameRecord(this DnsRecord record, out DnsCNameRecordData result)
        {
            result = default;
            if (record.Type != DnsRecordType.CNAME || record.Data.Length == 0)
            {
                return false;
            }
            if (!DnsEncodedName.TryParse(record.Message, record.DataOffset, out DnsEncodedName cname, out _))
            {
                return false;
            }
            result = new DnsCNameRecordData(cname);
            return true;
        }

        public static bool TryParseMxRecord(this DnsRecord record, out DnsMxRecordData result)
        {
            result = default;
            if (record.Type != DnsRecordType.MX || record.Data.Length < 3)
            {
                return false;
            }
            ushort preference = BinaryPrimitives.ReadUInt16BigEndian(record.Data);
            if (!DnsEncodedName.TryParse(record.Message, record.DataOffset + 2, out DnsEncodedName exchange, out _))
            {
                return false;
            }
            result = new DnsMxRecordData(preference, exchange);
            return true;
        }

        public static bool TryParseSrvRecord(this DnsRecord record, out DnsSrvRecordData result)
        {
            result = default;
            if (record.Type != DnsRecordType.SRV || record.Data.Length < 7)
            {
                return false;
            }
            ushort priority = BinaryPrimitives.ReadUInt16BigEndian(record.Data);
            ushort weight = BinaryPrimitives.ReadUInt16BigEndian(record.Data[2..]);
            ushort port = BinaryPrimitives.ReadUInt16BigEndian(record.Data[4..]);
            if (!DnsEncodedName.TryParse(record.Message, record.DataOffset + 6, out DnsEncodedName target, out _))
            {
                return false;
            }
            result = new DnsSrvRecordData(priority, weight, port, target);
            return true;
        }

        public static bool TryParseSoaRecord(this DnsRecord record, out DnsSoaRecordData result)
        {
            result = default;
            if (record.Type != DnsRecordType.SOA || record.Data.Length < 22)
            {
                return false;
            }

            if (!DnsEncodedName.TryParse(record.Message, record.DataOffset, out DnsEncodedName mname, out int mnameLen))
            {
                return false;
            }

            if (!DnsEncodedName.TryParse(record.Message, record.DataOffset + mnameLen, out DnsEncodedName rname, out int rnameLen))
            {
                return false;
            }

            ReadOnlySpan<byte> fixedData = record.Data[(mnameLen + rnameLen)..];
            if (fixedData.Length < 20)
            {
                return false;
            }

            result = new DnsSoaRecordData(mname, rname,
                BinaryPrimitives.ReadUInt32BigEndian(fixedData),
                BinaryPrimitives.ReadUInt32BigEndian(fixedData[4..]),
                BinaryPrimitives.ReadUInt32BigEndian(fixedData[8..]),
                BinaryPrimitives.ReadUInt32BigEndian(fixedData[12..]),
                BinaryPrimitives.ReadUInt32BigEndian(fixedData[16..]));
            return true;
        }

        public static bool TryParseTxtRecord(this DnsRecord record, out DnsTxtRecordData result)
        {
            result = default;
            if (record.Type != DnsRecordType.TXT || record.Data.Length == 0)
            {
                return false;
            }
            result = new DnsTxtRecordData(record.Data);
            return true;
        }

        public static bool TryParsePtrRecord(this DnsRecord record, out DnsPtrRecordData result)
        {
            result = default;
            if (record.Type != DnsRecordType.PTR || record.Data.Length == 0)
            {
                return false;
            }
            if (!DnsEncodedName.TryParse(record.Message, record.DataOffset, out DnsEncodedName ptr, out _))
            {
                return false;
            }
            result = new DnsPtrRecordData(ptr);
            return true;
        }

        public static bool TryParseNsRecord(this DnsRecord record, out DnsNsRecordData result)
        {
            result = default;
            if (record.Type != DnsRecordType.NS || record.Data.Length == 0)
            {
                return false;
            }
            if (!DnsEncodedName.TryParse(record.Message, record.DataOffset, out DnsEncodedName ns, out _))
            {
                return false;
            }
            result = new DnsNsRecordData(ns);
            return true;
        }
    }
}
