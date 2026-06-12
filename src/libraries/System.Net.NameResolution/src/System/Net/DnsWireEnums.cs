// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net
{
    // DNS record TYPE values (RFC 1035 §3.2.2 and updates).
    internal enum DnsRecordType : ushort
    {
        A = 1,
        NS = 2,
        CNAME = 5,
        SOA = 6,
        PTR = 12,
        MX = 15,
        TXT = 16,
        AAAA = 28,
        SRV = 33,
        NAPTR = 35,
        OPT = 41,
        SVCB = 64,
        HTTPS = 65,
    }

    // DNS record CLASS values (RFC 1035 §3.2.4).
    internal enum DnsRecordClass : ushort
    {
        Internet = 1,
        Chaos = 3,
        Hesiod = 4,
        Any = 255,
    }

    // DNS OPCODE values (RFC 1035 §4.1.1 and updates).
    internal enum DnsOpCode : byte
    {
        Query = 0,
        InverseQuery = 1,
        Status = 2,
        Notify = 4,
        Update = 5,
    }

    // DNS header flag bits. The enum values are the wire bit positions shifted
    // right by 4 so the set fits in a byte; see DnsMessageHeader for the encoding.
    [Flags]
    internal enum DnsHeaderFlags : byte
    {
        None                = 0,
        AuthoritativeAnswer = 1 << 6,  // AA — wire bit 10
        Truncation          = 1 << 5,  // TC — wire bit 9
        RecursionDesired    = 1 << 4,  // RD — wire bit 8
        RecursionAvailable  = 1 << 3,  // RA — wire bit 7
        AuthenticData       = 1 << 1,  // AD — wire bit 5 (RFC 4035)
        CheckingDisabled    = 1 << 0,  // CD — wire bit 4 (RFC 4035)
    }
}
