// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Net
{
    /// <summary>Represents an A or AAAA record resolved from DNS, including its time-to-live.</summary>
    public readonly struct AddressRecord
    {
        /// <summary>Gets the resolved IP address.</summary>
        public IPAddress Address { get; }
        /// <summary>Gets the time-to-live (TTL) of the record.</summary>
        public TimeSpan Ttl { get; }

        internal AddressRecord(IPAddress address, TimeSpan ttl)
        {
            Address = address;
            Ttl = ttl;
        }
    }

    /// <summary>Represents an SRV record (RFC 2782), with optional inlined address records from the additional section.</summary>
    public readonly struct SrvRecord
    {
        private readonly IReadOnlyList<AddressRecord>? _addresses;

        /// <summary>Gets the domain name of the target host.</summary>
        public string Target { get; }
        /// <summary>Gets the port on the target host where the service is found.</summary>
        [CLSCompliant(false)]
        public ushort Port { get; }
        /// <summary>Gets the priority of the target host. Lower values are preferred.</summary>
        [CLSCompliant(false)]
        public ushort Priority { get; }
        /// <summary>Gets the relative weight for records with the same priority.</summary>
        [CLSCompliant(false)]
        public ushort Weight { get; }
        /// <summary>Gets the time-to-live (TTL) of the record.</summary>
        public TimeSpan Ttl { get; }
        /// <summary>Gets the address records for the target host that were included in the additional section of the response, if any.</summary>
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

    /// <summary>Represents an MX (mail exchange) record (RFC 1035 §3.3.9).</summary>
    public readonly struct MxRecord
    {
        /// <summary>Gets the domain name of the mail exchange host.</summary>
        public string Exchange { get; }
        /// <summary>Gets the preference of this mail exchange. Lower values are preferred.</summary>
        [CLSCompliant(false)]
        public ushort Preference { get; }
        /// <summary>Gets the time-to-live (TTL) of the record.</summary>
        public TimeSpan Ttl { get; }

        internal MxRecord(string exchange, ushort preference, TimeSpan ttl)
        {
            Exchange = exchange;
            Preference = preference;
            Ttl = ttl;
        }
    }

    /// <summary>Represents a TXT record (RFC 1035 §3.3.14). A single record may carry multiple character-strings.</summary>
    public readonly struct TxtRecord
    {
        private readonly IReadOnlyList<string>? _values;

        /// <summary>Gets the character-strings contained in the record.</summary>
        public IReadOnlyList<string> Values => _values ?? Array.Empty<string>();
        /// <summary>Gets the time-to-live (TTL) of the record.</summary>
        public TimeSpan Ttl { get; }

        internal TxtRecord(IReadOnlyList<string> values, TimeSpan ttl)
        {
            _values = values;
            Ttl = ttl;
        }
    }

    /// <summary>Represents a CNAME (canonical name) record (RFC 1035 §3.3.1).</summary>
    public readonly struct CNameRecord
    {
        /// <summary>Gets the canonical name for the queried name.</summary>
        public string CanonicalName { get; }
        /// <summary>Gets the time-to-live (TTL) of the record.</summary>
        public TimeSpan Ttl { get; }

        internal CNameRecord(string canonicalName, TimeSpan ttl)
        {
            CanonicalName = canonicalName;
            Ttl = ttl;
        }
    }

    /// <summary>Represents a PTR (pointer) record (RFC 1035 §3.3.12), typically used for reverse DNS lookups.</summary>
    public readonly struct PtrRecord
    {
        /// <summary>Gets the domain name the queried name points to.</summary>
        public string Name { get; }
        /// <summary>Gets the time-to-live (TTL) of the record.</summary>
        public TimeSpan Ttl { get; }

        internal PtrRecord(string name, TimeSpan ttl)
        {
            Name = name;
            Ttl = ttl;
        }
    }

    /// <summary>Represents an NS (name server) record (RFC 1035 §3.3.11).</summary>
    public readonly struct NsRecord
    {
        /// <summary>Gets the domain name of the authoritative name server.</summary>
        public string Name { get; }
        /// <summary>Gets the time-to-live (TTL) of the record.</summary>
        public TimeSpan Ttl { get; }

        internal NsRecord(string name, TimeSpan ttl)
        {
            Name = name;
            Ttl = ttl;
        }
    }
}
