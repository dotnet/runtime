// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Net
{
    /// <summary>An A or AAAA record resolved from DNS, with TTL.</summary>
    public readonly struct AddressRecord
    {
        public IPAddress Address { get; }
        public TimeSpan Ttl { get; }

        internal AddressRecord(IPAddress address, TimeSpan ttl)
        {
            Address = address;
            Ttl = ttl;
        }
    }

    /// <summary>An SRV record (RFC 2782) with optional inlined address records from the additional section.</summary>
    public readonly struct SrvRecord
    {
        private readonly IReadOnlyList<AddressRecord>? _addresses;

        public string Target { get; }
        [CLSCompliant(false)]
        public ushort Port { get; }
        [CLSCompliant(false)]
        public ushort Priority { get; }
        [CLSCompliant(false)]
        public ushort Weight { get; }
        public TimeSpan Ttl { get; }
        public IReadOnlyList<AddressRecord> Addresses => _addresses ?? Array.Empty<AddressRecord>();

        internal SrvRecord(string target, ushort port, ushort priority, ushort weight, TimeSpan ttl, IReadOnlyList<AddressRecord>? addresses)
        {
            Target = target;
            Port = port;
            Priority = priority;
            Weight = weight;
            Ttl = ttl;
            _addresses = addresses;
        }
    }

    /// <summary>An MX record (RFC 1035 §3.3.9).</summary>
    public readonly struct MxRecord
    {
        public string Exchange { get; }
        [CLSCompliant(false)]
        public ushort Preference { get; }
        public TimeSpan Ttl { get; }

        internal MxRecord(string exchange, ushort preference, TimeSpan ttl)
        {
            Exchange = exchange;
            Preference = preference;
            Ttl = ttl;
        }
    }

    /// <summary>A TXT record (RFC 1035 §3.3.14). One record may carry multiple character-strings.</summary>
    public readonly struct TxtRecord
    {
        private readonly IReadOnlyList<string>? _values;

        public IReadOnlyList<string> Values => _values ?? Array.Empty<string>();
        public TimeSpan Ttl { get; }

        internal TxtRecord(IReadOnlyList<string> values, TimeSpan ttl)
        {
            _values = values;
            Ttl = ttl;
        }
    }

    /// <summary>A CNAME record (RFC 1035 §3.3.1).</summary>
    public readonly struct CNameRecord
    {
        public string CanonicalName { get; }
        public TimeSpan Ttl { get; }

        internal CNameRecord(string canonicalName, TimeSpan ttl)
        {
            CanonicalName = canonicalName;
            Ttl = ttl;
        }
    }

    /// <summary>A PTR record (RFC 1035 §3.3.12).</summary>
    public readonly struct PtrRecord
    {
        public string Name { get; }
        public TimeSpan Ttl { get; }

        internal PtrRecord(string name, TimeSpan ttl)
        {
            Name = name;
            Ttl = ttl;
        }
    }

    /// <summary>An NS record (RFC 1035 §3.3.11).</summary>
    public readonly struct NsRecord
    {
        public string Name { get; }
        public TimeSpan Ttl { get; }

        internal NsRecord(string name, TimeSpan ttl)
        {
            Name = name;
            Ttl = ttl;
        }
    }
}
