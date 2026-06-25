// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Net;
using Xunit;

namespace System.Net.NameResolution.Tests;

public class DnsMessageWriterTests
{
    [Fact]
    public void WriteStandardAQuery_ProducesExpectedBytes()
    {
        // Expected wire format for: query example.com A IN, ID=0x1234, RD=1
        byte[] expected =
        [
            // Header (12 bytes)
            0x12, 0x34,  // ID
            0x01, 0x00,  // Flags: RD=1
            0x00, 0x01,  // QDCOUNT=1
            0x00, 0x00,  // ANCOUNT=0
            0x00, 0x00,  // NSCOUNT=0
            0x00, 0x00,  // ARCOUNT=0
            // Question: example.com A IN
            0x07, (byte)'e', (byte)'x', (byte)'a', (byte)'m', (byte)'p', (byte)'l', (byte)'e',
            0x03, (byte)'c', (byte)'o', (byte)'m', 0x00,
            0x00, 0x01,  // QTYPE = A (1)
            0x00, 0x01,  // QCLASS = IN (1)
        ];

        Span<byte> buffer = stackalloc byte[512];
        DnsMessageWriter writer = new(buffer);

        DnsMessageHeader header = new() { Id = 0x1234, Flags = DnsHeaderFlags.RecursionDesired, QuestionCount = 1 };
        Assert.True(writer.TryWriteHeader(in header));

        Span<byte> nameBuffer = stackalloc byte[DnsEncodedName.MaxEncodedLength];
        Assert.Equal(OperationStatus.Done,
            DnsEncodedName.TryEncode("example.com", nameBuffer, out var name, out _));
        Assert.True(writer.TryWriteQuestion(name, DnsRecordType.A));

        Assert.Equal(expected.Length, writer.BytesWritten);
        Assert.True(buffer[..writer.BytesWritten].SequenceEqual(expected));
    }

    [Fact]
    public void WriteMultipleQuestions_ProducesCorrectOutput()
    {
        Span<byte> buffer = stackalloc byte[512];
        DnsMessageWriter writer = new(buffer);

        DnsMessageHeader header = new() { Id = 1, Flags = DnsHeaderFlags.RecursionDesired, QuestionCount = 2 };
        Assert.True(writer.TryWriteHeader(in header));

        Span<byte> nameBuffer = stackalloc byte[DnsEncodedName.MaxEncodedLength];

        DnsEncodedName.TryEncode("a.com", nameBuffer, out var name1, out _);
        Assert.True(writer.TryWriteQuestion(name1, DnsRecordType.A));

        DnsEncodedName.TryEncode("b.com", nameBuffer, out var name2, out _);
        Assert.True(writer.TryWriteQuestion(name2, DnsRecordType.AAAA));

        // Header(12) + Q1(1+1+3+1+3+1+4) + Q2 (same) = 12 + 11 + 11 = 34
        // name "a.com" = \x01a\x03com\x00 = 7 bytes, + 4 type/class = 11
        Assert.Equal(12 + 11 + 11, writer.BytesWritten);
    }

    [Fact]
    public void BufferTooSmall_ForHeader_ReturnsFalse()
    {
        Span<byte> buffer = stackalloc byte[11]; // 1 short
        DnsMessageWriter writer = new(buffer);
        DnsMessageHeader header = new() { Id = 1, Flags = DnsHeaderFlags.RecursionDesired, QuestionCount = 1 };
        Assert.False(writer.TryWriteHeader(in header));
        Assert.Equal(0, writer.BytesWritten);
    }

    [Fact]
    public void WriteQuestion_WithCompressedName_ExpandsPointers()
    {
        // Simulate a DnsEncodedName parsed from a response with a compression pointer
        byte[] message =
        [
            7, (byte)'e', (byte)'x', (byte)'a', (byte)'m', (byte)'p', (byte)'l', (byte)'e',
            3, (byte)'c', (byte)'o', (byte)'m', 0,
            3, (byte)'w', (byte)'w', (byte)'w', 0xC0, 0x00 // www + pointer to example.com
        ];
        Assert.True(DnsEncodedName.TryParse(message, 13, out DnsEncodedName compressedName, out _));

        Span<byte> buffer = stackalloc byte[512];
        DnsMessageWriter writer = new(buffer);
        DnsMessageHeader header = new() { Id = 1, Flags = DnsHeaderFlags.RecursionDesired, QuestionCount = 1 };
        Assert.True(writer.TryWriteHeader(in header));
        Assert.True(writer.TryWriteQuestion(compressedName, DnsRecordType.A));

        // Parse the written message and verify the name was expanded
        DnsMessageReader.TryCreate(buffer[..writer.BytesWritten], out var reader);
        Assert.True(reader.TryReadQuestion(out var q));
        Assert.True(q.Name.Equals("www.example.com"));

        // Verify no compression pointers in the output (flat encoding)
        // The name should be: \x03www\x07example\x03com\x00 = 17 bytes
        // Total: 12 header + 17 name + 4 type/class = 33
        Assert.Equal(33, writer.BytesWritten);
    }

    [Fact]
    public void BufferTooSmall_ForQuestion_ReturnsFalse()
    {
        Span<byte> buffer = stackalloc byte[14]; // header fits (12), question needs more
        DnsMessageWriter writer = new(buffer);

        DnsMessageHeader header = new() { Id = 1, Flags = DnsHeaderFlags.RecursionDesired, QuestionCount = 1 };
        Assert.True(writer.TryWriteHeader(in header));

        Span<byte> nameBuffer = stackalloc byte[DnsEncodedName.MaxEncodedLength];
        DnsEncodedName.TryEncode("example.com", nameBuffer, out var name, out _);
        Assert.False(writer.TryWriteQuestion(name, DnsRecordType.A));
        Assert.Equal(12, writer.BytesWritten); // only header was written
    }
}
