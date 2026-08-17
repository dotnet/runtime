// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Xunit;

namespace System.Net.NameResolution.Tests;

public class DnsMessageHeaderTests
{
    // Helper: writes header via DnsMessageWriter, returns the written bytes.
    private static byte[] WriteHeader(in DnsMessageHeader header)
    {
        Span<byte> buffer = stackalloc byte[512];
        DnsMessageWriter writer = new(buffer);
        Assert.True(writer.TryWriteHeader(in header));
        return buffer[..writer.BytesWritten].ToArray();
    }

    // Helper: writes header then reads it back via DnsMessageReader.
    private static DnsMessageHeader RoundTrip(in DnsMessageHeader header)
    {
        byte[] bytes = WriteHeader(in header);
        Assert.True(DnsMessageReader.TryCreate(bytes, out DnsMessageReader reader));
        return reader.Header;
    }

    [Fact]
    public void StandardQuery_SetsDefaults()
    {
        DnsMessageHeader header = new() { Id = 0x1234, Flags = DnsHeaderFlags.RecursionDesired, QuestionCount = 1 };

        Assert.Equal(0x1234, header.Id);
        Assert.False(header.IsResponse);
        Assert.Equal(DnsOpCode.Query, header.OpCode);
        Assert.Equal(DnsHeaderFlags.RecursionDesired, header.Flags);
        Assert.Equal(DnsResponseCode.NoError, header.ResponseCode);
        Assert.Equal(1, header.QuestionCount);
        Assert.Equal(0, header.AnswerCount);
        Assert.Equal(0, header.AuthorityCount);
        Assert.Equal(0, header.AdditionalCount);
    }

    [Fact]
    public void RoundTrip_StandardQuery()
    {
        DnsMessageHeader original = new() { Id = 0xABCD, Flags = DnsHeaderFlags.RecursionDesired, QuestionCount = 2 };
        DnsMessageHeader parsed = RoundTrip(in original);

        Assert.Equal(original.Id, parsed.Id);
        Assert.Equal(original.IsResponse, parsed.IsResponse);
        Assert.Equal(original.OpCode, parsed.OpCode);
        Assert.Equal(original.Flags, parsed.Flags);
        Assert.Equal(original.ResponseCode, parsed.ResponseCode);
        Assert.Equal(original.QuestionCount, parsed.QuestionCount);
        Assert.Equal(original.AnswerCount, parsed.AnswerCount);
        Assert.Equal(original.AuthorityCount, parsed.AuthorityCount);
        Assert.Equal(original.AdditionalCount, parsed.AdditionalCount);
    }

    [Fact]
    public void RoundTrip_ResponseWithAllFlags()
    {
        DnsHeaderFlags flags = DnsHeaderFlags.AuthoritativeAnswer | DnsHeaderFlags.RecursionDesired
            | DnsHeaderFlags.RecursionAvailable | DnsHeaderFlags.AuthenticData;

        DnsMessageHeader original = new()
        {
            Id = 0x5678,
            IsResponse = true,
            Flags = flags,
            QuestionCount = 1,
            AnswerCount = 3,
            AuthorityCount = 1,
            AdditionalCount = 2,
        };

        DnsMessageHeader parsed = RoundTrip(in original);

        Assert.True(parsed.IsResponse);
        Assert.Equal(flags, parsed.Flags);
        Assert.Equal(3, parsed.AnswerCount);
        Assert.Equal(1, parsed.AuthorityCount);
        Assert.Equal(2, parsed.AdditionalCount);
    }

    [Fact]
    public void RoundTrip_AllResponseCodes()
    {
        foreach (DnsResponseCode rcode in Enum.GetValues<DnsResponseCode>())
        {
            DnsMessageHeader original = new() { IsResponse = true, ResponseCode = rcode };
            DnsMessageHeader parsed = RoundTrip(in original);
            Assert.Equal(rcode, parsed.ResponseCode);
        }
    }

    [Fact]
    public void RoundTrip_OpCodes()
    {
        foreach (DnsOpCode opcode in Enum.GetValues<DnsOpCode>())
        {
            DnsMessageHeader original = new() { OpCode = opcode };
            DnsMessageHeader parsed = RoundTrip(in original);
            Assert.Equal(opcode, parsed.OpCode);
        }
    }

    [Fact]
    public void TryWriteHeader_BufferTooSmall_ReturnsFalse()
    {
        DnsMessageHeader header = new() { Id = 1, Flags = DnsHeaderFlags.RecursionDesired, QuestionCount = 1 };
        Span<byte> buffer = stackalloc byte[11]; // one byte short
        DnsMessageWriter writer = new(buffer);
        Assert.False(writer.TryWriteHeader(in header));
    }

    [Fact]
    public void TryCreate_BufferTooSmall_ReturnsFalse()
    {
        Span<byte> buffer = stackalloc byte[11];
        Assert.False(DnsMessageReader.TryCreate(buffer, out _));
    }

    [Fact]
    public void WireFormat_KnownBytes()
    {
        // Hand-crafted standard query: ID=0x1234, RD=1, QDCOUNT=1
        // Flags word: 0x0100 (RD bit at position 8)
        byte[] expected = [
            0x12, 0x34, // ID
            0x01, 0x00, // Flags: RD=1
            0x00, 0x01, // QDCOUNT=1
            0x00, 0x00, // ANCOUNT=0
            0x00, 0x00, // NSCOUNT=0
            0x00, 0x00, // ARCOUNT=0
        ];

        DnsMessageHeader header = new() { Id = 0x1234, Flags = DnsHeaderFlags.RecursionDesired, QuestionCount = 1 };
        byte[] written = WriteHeader(in header);
        Assert.Equal(expected, written);
    }

    [Fact]
    public void WireFormat_ResponseWithFlags()
    {
        // Response: QR=1, AA=1, RD=1, RA=1, RCODE=0
        // Flags word: 1_0000_1_0_1_1_0_0_0_0000 = 0x8580
        byte[] expected = [
            0x00, 0x01, // ID
            0x85, 0x80, // QR=1, AA=1, RD=1, RA=1
            0x00, 0x01, // QDCOUNT=1
            0x00, 0x02, // ANCOUNT=2
            0x00, 0x00, // NSCOUNT=0
            0x00, 0x00, // ARCOUNT=0
        ];

        DnsMessageHeader header = new()
        {
            Id = 1,
            IsResponse = true,
            Flags = DnsHeaderFlags.AuthoritativeAnswer | DnsHeaderFlags.RecursionDesired
                | DnsHeaderFlags.RecursionAvailable,
            QuestionCount = 1,
            AnswerCount = 2,
        };

        byte[] written = WriteHeader(in header);
        Assert.Equal(expected, written);
    }

    [Theory]
    [InlineData((byte)DnsHeaderFlags.AuthoritativeAnswer)]
    [InlineData((byte)DnsHeaderFlags.Truncation)]
    [InlineData((byte)DnsHeaderFlags.RecursionDesired)]
    [InlineData((byte)DnsHeaderFlags.RecursionAvailable)]
    [InlineData((byte)DnsHeaderFlags.AuthenticData)]
    [InlineData((byte)DnsHeaderFlags.CheckingDisabled)]
    public void RoundTrip_EachFlagIndividually(byte flagValue)
    {
        DnsHeaderFlags flag = (DnsHeaderFlags)flagValue;
        DnsMessageHeader original = new() { Flags = flag };
        DnsMessageHeader parsed = RoundTrip(in original);
        Assert.Equal(flag, parsed.Flags);
    }

    [Fact]
    public void RoundTrip_AllFlagsCombined()
    {
        DnsHeaderFlags allFlags = DnsHeaderFlags.AuthoritativeAnswer | DnsHeaderFlags.Truncation
            | DnsHeaderFlags.RecursionDesired | DnsHeaderFlags.RecursionAvailable
            | DnsHeaderFlags.AuthenticData | DnsHeaderFlags.CheckingDisabled;

        DnsMessageHeader original = new() { IsResponse = true, Flags = allFlags };
        DnsMessageHeader parsed = RoundTrip(in original);
        Assert.Equal(allFlags, parsed.Flags);
    }

    [Fact]
    public void TryWriteHeader_WritesExactly12Bytes()
    {
        DnsMessageHeader header = new() { Id = 1 };
        Span<byte> buffer = stackalloc byte[512];
        DnsMessageWriter writer = new(buffer);
        Assert.True(writer.TryWriteHeader(in header));
        Assert.Equal(12, writer.BytesWritten);
    }
}
