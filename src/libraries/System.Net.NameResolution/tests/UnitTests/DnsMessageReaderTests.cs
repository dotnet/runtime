// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Net;
using Xunit;

namespace System.Net.NameResolution.Tests;

public class DnsMessageReaderTests
{
    // A complete DNS response for "example.com" A query:
    // Header: ID=0x1234, QR=1, RD=1, RA=1, QDCOUNT=1, ANCOUNT=1
    // Question: example.com IN A
    // Answer: example.com A 93.184.216.34 TTL=300
    // The answer name uses a compression pointer to offset 12 (the question name)
    private static readonly byte[] ExampleComAResponse =
    [
        // Header (12 bytes)
        0x12, 0x34,  // ID
        0x81, 0x80,  // Flags: QR=1, RD=1, RA=1
        0x00, 0x01,  // QDCOUNT=1
        0x00, 0x01,  // ANCOUNT=1
        0x00, 0x00,  // NSCOUNT=0
        0x00, 0x00,  // ARCOUNT=0

        // Question section:
        // example.com IN A
        0x07, (byte)'e', (byte)'x', (byte)'a', (byte)'m', (byte)'p', (byte)'l', (byte)'e',
        0x03, (byte)'c', (byte)'o', (byte)'m', 0x00,
        0x00, 0x01,  // QTYPE = A
        0x00, 0x01,  // QCLASS = IN

        // Answer section:
        // example.com (compression pointer to offset 12) A IN TTL=300 RDATA=93.184.216.34
        0xC0, 0x0C,  // Name: pointer to offset 12
        0x00, 0x01,  // TYPE = A
        0x00, 0x01,  // CLASS = IN
        0x00, 0x00, 0x01, 0x2C,  // TTL = 300
        0x00, 0x04,  // RDLENGTH = 4
        0x5D, 0xB8, 0xD8, 0x22,  // RDATA: 93.184.216.34
    ];

    [Fact]
    public void ParseHeader_CorrectFields()
    {
        DnsMessageReader.TryCreate(ExampleComAResponse, out var reader);

        Assert.Equal(0x1234, reader.Header.Id);
        Assert.True(reader.Header.IsResponse);
        Assert.Equal(DnsOpCode.Query, reader.Header.OpCode);
        Assert.True(reader.Header.Flags.HasFlag(DnsHeaderFlags.RecursionDesired));
        Assert.True(reader.Header.Flags.HasFlag(DnsHeaderFlags.RecursionAvailable));
        Assert.Equal(DnsResponseCode.NoError, reader.Header.ResponseCode);
        Assert.Equal(1, reader.Header.QuestionCount);
        Assert.Equal(1, reader.Header.AnswerCount);
        Assert.Equal(0, reader.Header.AuthorityCount);
        Assert.Equal(0, reader.Header.AdditionalCount);
    }

    [Fact]
    public void ParseQuestion_CorrectFields()
    {
        DnsMessageReader.TryCreate(ExampleComAResponse, out var reader);

        Assert.True(reader.TryReadQuestion(out var question));
        Assert.True(question.Name.Equals("example.com"));
        Assert.Equal(DnsRecordType.A, question.Type);
        Assert.Equal(DnsRecordClass.Internet, question.Class);
    }

    [Fact]
    public void ParseAnswer_ARecord()
    {
        DnsMessageReader.TryCreate(ExampleComAResponse, out var reader);

        // Skip question
        Assert.True(reader.TryReadQuestion(out _));

        // Read answer
        Assert.True(reader.TryReadRecord(out var record));
        Assert.True(record.Name.Equals("example.com"));
        Assert.Equal(DnsRecordType.A, record.Type);
        Assert.Equal(DnsRecordClass.Internet, record.Class);
        Assert.Equal(300u, record.TimeToLive);
        Assert.Equal(4, record.Data.Length);
        Assert.Equal(new byte[] { 0x5D, 0xB8, 0xD8, 0x22 }, record.Data.ToArray());
    }

    [Fact]
    public void ParseAnswer_NameUsesCompressionPointer()
    {
        DnsMessageReader.TryCreate(ExampleComAResponse, out var reader);
        reader.TryReadQuestion(out _);
        reader.TryReadRecord(out var record);

        // The answer name is a compression pointer to offset 12 (the question name)
        Assert.True(record.Name.Equals("example.com"));
        Assert.Equal("example.com", record.Name.ToString());
    }

    // Response with multiple answers: example.com CNAME + A
    private static readonly byte[] CnameAndAResponse =
    [
        // Header
        0x00, 0x01,  // ID=1
        0x81, 0x80,  // QR=1, RD=1, RA=1
        0x00, 0x01,  // QDCOUNT=1
        0x00, 0x02,  // ANCOUNT=2
        0x00, 0x00,  // NSCOUNT=0
        0x00, 0x00,  // ARCOUNT=0

        // Question: www.example.com A IN
        0x03, (byte)'w', (byte)'w', (byte)'w',
        0x07, (byte)'e', (byte)'x', (byte)'a', (byte)'m', (byte)'p', (byte)'l', (byte)'e',
        0x03, (byte)'c', (byte)'o', (byte)'m', 0x00,
        0x00, 0x01,  // QTYPE = A
        0x00, 0x01,  // QCLASS = IN

        // Answer 1: www.example.com CNAME example.com
        0xC0, 0x0C,  // pointer to offset 12 (www.example.com)
        0x00, 0x05,  // TYPE = CNAME
        0x00, 0x01,  // CLASS = IN
        0x00, 0x00, 0x00, 0x3C,  // TTL = 60
        0x00, 0x02,  // RDLENGTH = 2
        0xC0, 0x10,  // RDATA: pointer to offset 16 (example.com)

        // Answer 2: example.com A 93.184.216.34
        0xC0, 0x10,  // pointer to offset 16 (example.com)
        0x00, 0x01,  // TYPE = A
        0x00, 0x01,  // CLASS = IN
        0x00, 0x00, 0x01, 0x2C,  // TTL = 300
        0x00, 0x04,  // RDLENGTH = 4
        0x5D, 0xB8, 0xD8, 0x22,  // 93.184.216.34
    ];

    [Fact]
    public void ParseMultipleAnswers_CnameAndA()
    {
        DnsMessageReader.TryCreate(CnameAndAResponse, out var reader);

        // Skip question
        Assert.True(reader.TryReadQuestion(out var q));
        Assert.True(q.Name.Equals("www.example.com"));

        // CNAME answer
        Assert.True(reader.TryReadRecord(out var cname));
        Assert.Equal(DnsRecordType.CNAME, cname.Type);
        Assert.Equal(60u, cname.TimeToLive);
        Assert.True(cname.Name.Equals("www.example.com"));

        // The CNAME RDATA contains a compression pointer to "example.com"
        DnsEncodedName.TryParse(cname.Message, cname.DataOffset, out DnsEncodedName cnameTarget, out _);
        Assert.True(cnameTarget.Equals("example.com"));

        // A answer
        Assert.True(reader.TryReadRecord(out var a));
        Assert.Equal(DnsRecordType.A, a.Type);
        Assert.True(a.Name.Equals("example.com"));
        Assert.Equal(300u, a.TimeToLive);
    }

    // NXDOMAIN response
    private static readonly byte[] NxdomainResponse =
    [
        // Header: QR=1, RD=1, RA=1, RCODE=3 (NXDOMAIN)
        0x00, 0x02,  // ID=2
        0x81, 0x83,  // QR=1, RD=1, RA=1, RCODE=3
        0x00, 0x01,  // QDCOUNT=1
        0x00, 0x00,  // ANCOUNT=0
        0x00, 0x00,  // NSCOUNT=0
        0x00, 0x00,  // ARCOUNT=0

        // Question: nonexistent.example.com A IN
        0x0B, (byte)'n', (byte)'o', (byte)'n', (byte)'e', (byte)'x', (byte)'i',
              (byte)'s', (byte)'t', (byte)'e', (byte)'n', (byte)'t',
        0x07, (byte)'e', (byte)'x', (byte)'a', (byte)'m', (byte)'p', (byte)'l', (byte)'e',
        0x03, (byte)'c', (byte)'o', (byte)'m', 0x00,
        0x00, 0x01,  // QTYPE = A
        0x00, 0x01,  // QCLASS = IN
    ];

    [Fact]
    public void ParseNxdomain_ResponseCode()
    {
        DnsMessageReader.TryCreate(NxdomainResponse, out var reader);

        Assert.Equal(DnsResponseCode.NxDomain, reader.Header.ResponseCode);
        Assert.Equal(0, reader.Header.AnswerCount);

        Assert.True(reader.TryReadQuestion(out var q));
        Assert.True(q.Name.Equals("nonexistent.example.com"));

        // No records to read
        Assert.False(reader.TryReadRecord(out _));
    }

    [Fact]
    public void TryCreate_TooSmallBuffer_ReturnsFalse()
    {
        Assert.False(DnsMessageReader.TryCreate(new byte[11], out _));
    }

    [Fact]
    public void TryReadRecord_TruncatedRdata_ReturnsFalse()
    {
        // Take the valid response and truncate the RDATA
        byte[] truncated = ExampleComAResponse[..^2]; // cut off last 2 bytes of RDATA
        DnsMessageReader.TryCreate(truncated, out var reader);
        reader.TryReadQuestion(out _);
        Assert.False(reader.TryReadRecord(out _));
    }

    [Fact]
    public void TryReadQuestion_MalformedLabelLength_ReturnsFalse()
    {
        // Craft a message where the question name has a label length extending past buffer
        byte[] malformed =
        [
            0x00, 0x01, 0x01, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // header
            0xFF, // label length = 255, but no data follows
        ];
        DnsMessageReader.TryCreate(malformed, out var reader);
        Assert.False(reader.TryReadQuestion(out _));
    }

    [Fact]
    public void TryReadRecord_InvalidCompressionPointer_ReturnsFalse()
    {
        // Craft a message where a record name has a compression pointer to an out-of-bounds offset
        byte[] malformed =
        [
            0x00, 0x01, 0x81, 0x80, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, // header: ANCOUNT=1
            0xC0, 0xFF, // compression pointer to offset 255, way beyond buffer
            0x00, 0x01, // TYPE=A
            0x00, 0x01, // CLASS=IN
            0x00, 0x00, 0x00, 0x3C, // TTL=60
            0x00, 0x04, // RDLENGTH=4
            0x01, 0x02, 0x03, 0x04, // RDATA
        ];
        DnsMessageReader.TryCreate(malformed, out var reader);
        // The record name has an invalid pointer, so TryReadRecord fails
        Assert.False(reader.TryReadRecord(out _));
    }

    [Fact]
    public void RoundTrip_WriteThenRead()
    {
        // Build a query with the writer, then parse it with the reader
        Span<byte> buffer = stackalloc byte[512];
        DnsMessageWriter writer = new(buffer);

        DnsMessageHeader header = new() { Id = 0xBEEF, Flags = DnsHeaderFlags.RecursionDesired, QuestionCount = 2 };
        writer.TryWriteHeader(in header);

        Span<byte> nameBuf = stackalloc byte[DnsEncodedName.MaxEncodedLength];
        DnsEncodedName.TryEncode("example.com", nameBuf, out var name1, out _);
        writer.TryWriteQuestion(name1, DnsRecordType.A);

        DnsEncodedName.TryEncode("example.org", nameBuf, out var name2, out _);
        writer.TryWriteQuestion(name2, DnsRecordType.AAAA);

        // Now parse
        DnsMessageReader.TryCreate(buffer[..writer.BytesWritten], out var reader);
        Assert.Equal(0xBEEF, reader.Header.Id);
        Assert.False(reader.Header.IsResponse);
        Assert.Equal(2, reader.Header.QuestionCount);

        Assert.True(reader.TryReadQuestion(out var q1));
        Assert.True(q1.Name.Equals("example.com"));
        Assert.Equal(DnsRecordType.A, q1.Type);

        Assert.True(reader.TryReadQuestion(out var q2));
        Assert.True(q2.Name.Equals("example.org"));
        Assert.Equal(DnsRecordType.AAAA, q2.Type);
    }

    [Fact]
    public void TryCreate_ExactHeaderSize_Succeeds()
    {
        // A 12-byte buffer is the minimum valid header
        byte[] data = [0x00, 0x01, 0x81, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
        Assert.True(DnsMessageReader.TryCreate(data, out DnsMessageReader reader));
        Assert.Equal(0x0001, reader.Header.Id);
        Assert.True(reader.Header.IsResponse);
    }

    [Fact]
    public void TryReadQuestion_TruncatedTypeClass_ReturnsFalse()
    {
        // Header says QDCOUNT=1, valid name follows, but TYPE/CLASS bytes are missing
        byte[] data =
        [
            0x00, 0x01, 0x81, 0x80, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // header
            0x01, (byte)'a', 0x00, // name "a" (3 bytes), no TYPE/CLASS
        ];
        DnsMessageReader.TryCreate(data, out DnsMessageReader reader);
        Assert.False(reader.TryReadQuestion(out _));
    }

    [Fact]
    public void TryReadRecord_TruncatedFixedFields_ReturnsFalse()
    {
        // Header says ANCOUNT=1, valid name but TYPE/CLASS/TTL/RDLENGTH truncated
        byte[] data =
        [
            0x00, 0x01, 0x81, 0x80, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, // header
            0x01, (byte)'a', 0x00, // name "a" (3 bytes)
            0x00, 0x01, // TYPE=A, but missing CLASS/TTL/RDLENGTH
        ];
        DnsMessageReader.TryCreate(data, out DnsMessageReader reader);
        Assert.False(reader.TryReadRecord(out _));
    }

    [Fact]
    public void TryReadQuestion_AtEndOfBuffer_ReturnsFalse()
    {
        // Header says QDCOUNT=1, but no data follows after the 12-byte header
        byte[] data = [0x00, 0x01, 0x81, 0x80, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
        DnsMessageReader.TryCreate(data, out DnsMessageReader reader);
        Assert.False(reader.TryReadQuestion(out _));
    }

    [Fact]
    public void TryReadRecord_AtEndOfBuffer_ReturnsFalse()
    {
        // Header says ANCOUNT=1, but no data follows after the 12-byte header
        byte[] data = [0x00, 0x01, 0x81, 0x80, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00];
        DnsMessageReader.TryCreate(data, out DnsMessageReader reader);
        Assert.False(reader.TryReadRecord(out _));
    }

    [Fact]
    public void TryReadRecord_NoMoreRecords_ReturnsFalse()
    {
        // Parse a valid response, read the single answer, then try to read another
        DnsMessageReader.TryCreate(ExampleComAResponse, out DnsMessageReader reader);
        reader.TryReadQuestion(out _);
        Assert.True(reader.TryReadRecord(out _));
        Assert.False(reader.TryReadRecord(out _)); // no more records
    }
}
