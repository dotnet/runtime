// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Xunit;

namespace System.Net.NameResolution.Tests;

public class DnsRecordTypeTests
{
    // Helper: builds a minimal DNS response with a single answer record.
    // The question is "q.test" and the answer name uses a pointer to it.
    private static byte[] BuildResponse(DnsRecordType type, byte[] rdata, uint ttl = 300)
    {
        // Question name: q.test = \x01q\x04test\x00 (8 bytes)
        byte[] questionName = [0x01, (byte)'q', 0x04, (byte)'t', (byte)'e', (byte)'s', (byte)'t', 0x00];

        using MemoryStream ms = new();
        BinaryWriter bw = new(ms);

        // Header (12 bytes)
        bw.Write((byte)0x00); bw.Write((byte)0x01); // ID=1
        bw.Write((byte)0x81); bw.Write((byte)0x80); // QR=1, RD=1, RA=1
        bw.Write((byte)0x00); bw.Write((byte)0x01); // QDCOUNT=1
        bw.Write((byte)0x00); bw.Write((byte)0x01); // ANCOUNT=1
        bw.Write((byte)0x00); bw.Write((byte)0x00); // NSCOUNT=0
        bw.Write((byte)0x00); bw.Write((byte)0x00); // ARCOUNT=0

        // Question section
        bw.Write(questionName);
        bw.Write(BinaryPrimitives.ReverseEndianness((ushort)type));
        bw.Write(BinaryPrimitives.ReverseEndianness((ushort)1)); // CLASS=IN

        // Answer: pointer to offset 12 (question name)
        bw.Write((byte)0xC0); bw.Write((byte)0x0C);
        bw.Write(BinaryPrimitives.ReverseEndianness((ushort)type));
        bw.Write(BinaryPrimitives.ReverseEndianness((ushort)1)); // CLASS=IN
        bw.Write(BinaryPrimitives.ReverseEndianness(ttl));
        bw.Write(BinaryPrimitives.ReverseEndianness((ushort)rdata.Length));
        bw.Write(rdata);

        return ms.ToArray();
    }

    private static DnsRecord GetAnswerRecord(byte[] response)
    {
        Assert.True(DnsMessageReader.TryCreate(response, out var reader));
        Assert.True(reader.TryReadQuestion(out _));
        Assert.True(reader.TryReadRecord(out var record));
        return record;
    }

    [Fact]
    public void ARecord_ParsesCorrectly()
    {
        byte[] rdata = [192, 168, 1, 1];
        DnsRecord record = GetAnswerRecord(BuildResponse(DnsRecordType.A, rdata));

        Assert.True(record.TryParseARecord(out var a));
        Assert.Equal(rdata, a.AddressBytes.ToArray());

        IPAddress ip = a.ToIPAddress();
        Assert.Equal("192.168.1.1", ip.ToString());
    }

    [Fact]
    public void AAAARecord_ParsesCorrectly()
    {
        // ::1 in 16 bytes
        byte[] rdata = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1];
        DnsRecord record = GetAnswerRecord(BuildResponse(DnsRecordType.AAAA, rdata));

        Assert.True(record.TryParseAAAARecord(out var aaaa));
        Assert.Equal(rdata, aaaa.AddressBytes.ToArray());
        Assert.Equal("::1", aaaa.ToIPAddress().ToString());
    }

    [Fact]
    public void CNameRecord_ParsesCorrectly()
    {
        // RDATA: target.test = \x06target\x04test\x00
        byte[] rdata = [0x06, (byte)'t', (byte)'a', (byte)'r', (byte)'g', (byte)'e', (byte)'t',
                        0x04, (byte)'t', (byte)'e', (byte)'s', (byte)'t', 0x00];
        DnsRecord record = GetAnswerRecord(BuildResponse(DnsRecordType.CNAME, rdata));

        Assert.True(record.TryParseCNameRecord(out var cname));
        Assert.True(cname.CName.Equals("target.test"));
    }

    [Fact]
    public void MxRecord_ParsesCorrectly()
    {
        // RDATA: preference=10, exchange=mail.test
        byte[] rdata = [0x00, 0x0A, // preference=10
                        0x04, (byte)'m', (byte)'a', (byte)'i', (byte)'l',
                        0x04, (byte)'t', (byte)'e', (byte)'s', (byte)'t', 0x00];
        DnsRecord record = GetAnswerRecord(BuildResponse(DnsRecordType.MX, rdata));

        Assert.True(record.TryParseMxRecord(out var mx));
        Assert.Equal(10, mx.Preference);
        Assert.True(mx.Exchange.Equals("mail.test"));
    }

    [Fact]
    public void SrvRecord_ParsesCorrectly()
    {
        // RDATA: priority=10, weight=20, port=8080, target=srv.test
        byte[] rdata = [0x00, 0x0A, // priority=10
                        0x00, 0x14, // weight=20
                        0x1F, 0x90, // port=8080
                        0x03, (byte)'s', (byte)'r', (byte)'v',
                        0x04, (byte)'t', (byte)'e', (byte)'s', (byte)'t', 0x00];
        DnsRecord record = GetAnswerRecord(BuildResponse(DnsRecordType.SRV, rdata));

        Assert.True(record.TryParseSrvRecord(out var srv));
        Assert.Equal(10, srv.Priority);
        Assert.Equal(20, srv.Weight);
        Assert.Equal(8080, srv.Port);
        Assert.True(srv.Target.Equals("srv.test"));
    }

    [Fact]
    public void TxtRecord_SingleString()
    {
        byte[] rdata = [0x05, (byte)'h', (byte)'e', (byte)'l', (byte)'l', (byte)'o'];
        DnsRecord record = GetAnswerRecord(BuildResponse(DnsRecordType.TXT, rdata));

        Assert.True(record.TryParseTxtRecord(out var txt));

        List<string> strings = new();
        foreach (ReadOnlySpan<byte> s in txt.EnumerateStrings())
            strings.Add(Encoding.ASCII.GetString(s));

        Assert.Equal(["hello"], strings);
    }

    [Fact]
    public void TxtRecord_MultipleStrings()
    {
        byte[] rdata = [0x03, (byte)'a', (byte)'b', (byte)'c',
                        0x02, (byte)'d', (byte)'e'];
        DnsRecord record = GetAnswerRecord(BuildResponse(DnsRecordType.TXT, rdata));

        Assert.True(record.TryParseTxtRecord(out var txt));

        List<string> strings = new();
        foreach (ReadOnlySpan<byte> s in txt.EnumerateStrings())
            strings.Add(Encoding.ASCII.GetString(s));

        Assert.Equal(["abc", "de"], strings);
    }

    [Fact]
    public void PtrRecord_ParsesCorrectly()
    {
        // RDATA: host.test
        byte[] rdata = [0x04, (byte)'h', (byte)'o', (byte)'s', (byte)'t',
                        0x04, (byte)'t', (byte)'e', (byte)'s', (byte)'t', 0x00];
        DnsRecord record = GetAnswerRecord(BuildResponse(DnsRecordType.PTR, rdata));

        Assert.True(record.TryParsePtrRecord(out var ptr));
        Assert.True(ptr.Name.Equals("host.test"));
    }

    [Fact]
    public void NsRecord_ParsesCorrectly()
    {
        // RDATA: ns1.test
        byte[] rdata = [0x03, (byte)'n', (byte)'s', (byte)'1',
                        0x04, (byte)'t', (byte)'e', (byte)'s', (byte)'t', 0x00];
        DnsRecord record = GetAnswerRecord(BuildResponse(DnsRecordType.NS, rdata));

        Assert.True(record.TryParseNsRecord(out var ns));
        Assert.True(ns.Name.Equals("ns1.test"));
    }

    [Fact]
    public void SoaRecord_ParsesCorrectly()
    {
        // RDATA: mname=ns.test, rname=admin.test, serial=2024010101, refresh=3600, retry=900, expire=604800, minimum=86400
        byte[] mname = [0x02, (byte)'n', (byte)'s', 0x04, (byte)'t', (byte)'e', (byte)'s', (byte)'t', 0x00];
        byte[] rname = [0x05, (byte)'a', (byte)'d', (byte)'m', (byte)'i', (byte)'n', 0x04, (byte)'t', (byte)'e', (byte)'s', (byte)'t', 0x00];
        byte[] fixedFields = new byte[20];
        BinaryPrimitives.WriteUInt32BigEndian(fixedFields.AsSpan(0), 2024010101);
        BinaryPrimitives.WriteUInt32BigEndian(fixedFields.AsSpan(4), 3600);
        BinaryPrimitives.WriteUInt32BigEndian(fixedFields.AsSpan(8), 900);
        BinaryPrimitives.WriteUInt32BigEndian(fixedFields.AsSpan(12), 604800);
        BinaryPrimitives.WriteUInt32BigEndian(fixedFields.AsSpan(16), 86400);

        byte[] rdata = [.. mname, .. rname, .. fixedFields];
        DnsRecord record = GetAnswerRecord(BuildResponse(DnsRecordType.SOA, rdata));

        Assert.True(record.TryParseSoaRecord(out var soa));
        Assert.True(soa.PrimaryNameServer.Equals("ns.test"));
        Assert.True(soa.ResponsibleMailbox.Equals("admin.test"));
        Assert.Equal(2024010101u, soa.SerialNumber);
        Assert.Equal(3600u, soa.RefreshInterval);
        Assert.Equal(900u, soa.RetryInterval);
        Assert.Equal(604800u, soa.ExpireLimit);
        Assert.Equal(86400u, soa.MinimumTtl);
    }

    [Theory]
    [InlineData((ushort)DnsRecordType.A)]
    [InlineData((ushort)DnsRecordType.AAAA)]
    [InlineData((ushort)DnsRecordType.CNAME)]
    [InlineData((ushort)DnsRecordType.MX)]
    [InlineData((ushort)DnsRecordType.SRV)]
    [InlineData((ushort)DnsRecordType.TXT)]
    [InlineData((ushort)DnsRecordType.PTR)]
    [InlineData((ushort)DnsRecordType.NS)]
    public void TypeMismatch_ReturnsFalse(ushort actualTypeValue)
    {
        DnsRecordType actualType = (DnsRecordType)actualTypeValue;

        // Build a record with valid RDATA for its own type, then verify each TryParse*
        // succeeds only for the matching type and fails for every other type.
        DnsRecord record = GetAnswerRecord(BuildResponse(actualType, GetValidRData(actualType)));

        Assert.Equal(actualType == DnsRecordType.A, record.TryParseARecord(out _));
        Assert.Equal(actualType == DnsRecordType.AAAA, record.TryParseAAAARecord(out _));
        Assert.Equal(actualType == DnsRecordType.CNAME, record.TryParseCNameRecord(out _));
        Assert.Equal(actualType == DnsRecordType.MX, record.TryParseMxRecord(out _));
        Assert.Equal(actualType == DnsRecordType.SRV, record.TryParseSrvRecord(out _));
        Assert.Equal(actualType == DnsRecordType.TXT, record.TryParseTxtRecord(out _));
        Assert.Equal(actualType == DnsRecordType.PTR, record.TryParsePtrRecord(out _));
        Assert.Equal(actualType == DnsRecordType.NS, record.TryParseNsRecord(out _));

        // Returns valid RDATA for the given record type.
        static byte[] GetValidRData(DnsRecordType type) => type switch
        {
            DnsRecordType.A => [192, 168, 1, 1],
            DnsRecordType.AAAA => [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1],
            DnsRecordType.CNAME or DnsRecordType.PTR or DnsRecordType.NS =>
                [0x04, (byte)'h', (byte)'o', (byte)'s', (byte)'t', 0x04, (byte)'t', (byte)'e', (byte)'s', (byte)'t', 0x00],
            DnsRecordType.MX =>
                [0x00, 0x0A, 0x04, (byte)'m', (byte)'a', (byte)'i', (byte)'l', 0x04, (byte)'t', (byte)'e', (byte)'s', (byte)'t', 0x00],
            DnsRecordType.SRV =>
                [0x00, 0x0A, 0x00, 0x14, 0x1F, 0x90, 0x03, (byte)'s', (byte)'r', (byte)'v', 0x04, (byte)'t', (byte)'e', (byte)'s', (byte)'t', 0x00],
            DnsRecordType.TXT => [0x05, (byte)'h', (byte)'e', (byte)'l', (byte)'l', (byte)'o'],
            _ => throw new ArgumentOutOfRangeException(nameof(type)),
        };
    }

    [Fact]
    public void CNameRecord_WithCompressionPointer()
    {
        // Build a response where the CNAME RDATA uses a compression pointer
        // back to the question name ("q.test")
        byte[] rdata = [0xC0, 0x0C]; // pointer to offset 12 = question name
        DnsRecord record = GetAnswerRecord(BuildResponse(DnsRecordType.CNAME, rdata));

        Assert.True(record.TryParseCNameRecord(out var cname));
        Assert.True(cname.CName.Equals("q.test"));
    }

    // --- Malformed RDATA edge cases ---

    [Fact]
    public void ARecord_WrongLength_ReturnsFalse()
    {
        // A record requires exactly 4 bytes of RDATA
        byte[] rdata = [192, 168, 1]; // only 3 bytes
        DnsRecord record = GetAnswerRecord(BuildResponse(DnsRecordType.A, rdata));
        Assert.False(record.TryParseARecord(out _));
    }

    [Fact]
    public void AAAARecord_WrongLength_ReturnsFalse()
    {
        // AAAA record requires exactly 16 bytes of RDATA
        byte[] rdata = new byte[15]; // only 15 bytes
        DnsRecord record = GetAnswerRecord(BuildResponse(DnsRecordType.AAAA, rdata));
        Assert.False(record.TryParseAAAARecord(out _));
    }

    [Fact]
    public void CNameRecord_EmptyRdata_ReturnsFalse()
    {
        DnsRecord record = GetAnswerRecord(BuildResponse(DnsRecordType.CNAME, []));
        Assert.False(record.TryParseCNameRecord(out _));
    }

    [Fact]
    public void CNameRecord_MalformedName_ReturnsFalse()
    {
        // RDATA with invalid label length (0x50 = 80, exceeds max 63)
        byte[] rdata = [0x50, (byte)'a'];
        DnsRecord record = GetAnswerRecord(BuildResponse(DnsRecordType.CNAME, rdata));
        Assert.False(record.TryParseCNameRecord(out _));
    }

    [Fact]
    public void MxRecord_TooShortRdata_ReturnsFalse()
    {
        // MX requires at least 3 bytes (2 for preference + 1 for name)
        byte[] rdata = [0x00, 0x0A]; // only 2 bytes, no exchange name
        DnsRecord record = GetAnswerRecord(BuildResponse(DnsRecordType.MX, rdata));
        Assert.False(record.TryParseMxRecord(out _));
    }

    [Fact]
    public void MxRecord_MalformedExchangeName_ReturnsFalse()
    {
        // Preference + malformed name (label length exceeds remaining)
        byte[] rdata = [0x00, 0x0A, 0x50, (byte)'a'];
        DnsRecord record = GetAnswerRecord(BuildResponse(DnsRecordType.MX, rdata));
        Assert.False(record.TryParseMxRecord(out _));
    }

    [Fact]
    public void SrvRecord_TooShortRdata_ReturnsFalse()
    {
        // SRV requires at least 7 bytes (priority+weight+port = 6, + 1 for target name)
        byte[] rdata = [0x00, 0x0A, 0x00, 0x14, 0x1F, 0x90]; // only 6 bytes, no target
        DnsRecord record = GetAnswerRecord(BuildResponse(DnsRecordType.SRV, rdata));
        Assert.False(record.TryParseSrvRecord(out _));
    }

    [Fact]
    public void SrvRecord_MalformedTargetName_ReturnsFalse()
    {
        // Valid fixed fields but target name label extends past RDATA
        byte[] rdata = [0x00, 0x0A, 0x00, 0x14, 0x1F, 0x90, 0x50, (byte)'a'];
        DnsRecord record = GetAnswerRecord(BuildResponse(DnsRecordType.SRV, rdata));
        Assert.False(record.TryParseSrvRecord(out _));
    }

    [Fact]
    public void SoaRecord_TooShortRdata_ReturnsFalse()
    {
        // SOA requires at least 22 bytes
        byte[] rdata = new byte[21];
        DnsRecord record = GetAnswerRecord(BuildResponse(DnsRecordType.SOA, rdata));
        Assert.False(record.TryParseSoaRecord(out _));
    }

    [Fact]
    public void SoaRecord_MalformedMname_ReturnsFalse()
    {
        // SOA RDATA with invalid mname label (0x50 = 80, > 63 max)
        byte[] rdata = new byte[30];
        rdata[0] = 0x50; // invalid label length in mname
        DnsRecord record = GetAnswerRecord(BuildResponse(DnsRecordType.SOA, rdata));
        Assert.False(record.TryParseSoaRecord(out _));
    }

    [Fact]
    public void SoaRecord_MalformedRname_ReturnsFalse()
    {
        // Valid mname but malformed rname
        byte[] mname = [0x02, (byte)'n', (byte)'s', 0x00]; // ns.
        byte[] rdata = new byte[mname.Length + 30];
        mname.CopyTo(rdata, 0);
        rdata[mname.Length] = 0x50; // invalid label length in rname
        DnsRecord record = GetAnswerRecord(BuildResponse(DnsRecordType.SOA, rdata));
        Assert.False(record.TryParseSoaRecord(out _));
    }

    [Fact]
    public void SoaRecord_TruncatedFixedFields_ReturnsFalse()
    {
        // Valid mname and rname but not enough room for the 20 bytes of fixed fields
        byte[] mname = [0x02, (byte)'n', (byte)'s', 0x00];
        byte[] rname = [0x05, (byte)'a', (byte)'d', (byte)'m', (byte)'i', (byte)'n', 0x00];
        byte[] rdata = new byte[mname.Length + rname.Length + 10]; // only 10 bytes for fixed fields
        mname.CopyTo(rdata, 0);
        rname.CopyTo(rdata, mname.Length);
        DnsRecord record = GetAnswerRecord(BuildResponse(DnsRecordType.SOA, rdata));
        Assert.False(record.TryParseSoaRecord(out _));
    }

    [Fact]
    public void TxtRecord_EmptyRdata_ReturnsFalse()
    {
        DnsRecord record = GetAnswerRecord(BuildResponse(DnsRecordType.TXT, []));
        Assert.False(record.TryParseTxtRecord(out _));
    }

    [Fact]
    public void TxtRecord_TruncatedString_StopsEnumerating()
    {
        // String length byte says 10 but only 3 bytes remain
        byte[] rdata = [0x0A, (byte)'a', (byte)'b', (byte)'c'];
        DnsRecord record = GetAnswerRecord(BuildResponse(DnsRecordType.TXT, rdata));
        Assert.True(record.TryParseTxtRecord(out var txt));

        // Enumerator should return false (truncated string)
        DnsTxtEnumerator enumerator = txt.EnumerateStrings();
        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void PtrRecord_EmptyRdata_ReturnsFalse()
    {
        DnsRecord record = GetAnswerRecord(BuildResponse(DnsRecordType.PTR, []));
        Assert.False(record.TryParsePtrRecord(out _));
    }

    [Fact]
    public void PtrRecord_MalformedName_ReturnsFalse()
    {
        byte[] rdata = [0x50, (byte)'a']; // invalid label length
        DnsRecord record = GetAnswerRecord(BuildResponse(DnsRecordType.PTR, rdata));
        Assert.False(record.TryParsePtrRecord(out _));
    }

    [Fact]
    public void NsRecord_EmptyRdata_ReturnsFalse()
    {
        DnsRecord record = GetAnswerRecord(BuildResponse(DnsRecordType.NS, []));
        Assert.False(record.TryParseNsRecord(out _));
    }

    [Fact]
    public void NsRecord_MalformedName_ReturnsFalse()
    {
        byte[] rdata = [0x50, (byte)'a']; // invalid label length
        DnsRecord record = GetAnswerRecord(BuildResponse(DnsRecordType.NS, rdata));
        Assert.False(record.TryParseNsRecord(out _));
    }
}
