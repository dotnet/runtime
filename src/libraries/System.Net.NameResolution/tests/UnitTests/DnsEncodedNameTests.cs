// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Xunit;

namespace System.Net.NameResolution.Tests;

public class DnsEncodedNameTests
{
    [Theory]
    [InlineData("example.com", new byte[] { 7, (byte)'e', (byte)'x', (byte)'a', (byte)'m', (byte)'p', (byte)'l', (byte)'e', 3, (byte)'c', (byte)'o', (byte)'m', 0 })]
    [InlineData("a.b", new byte[] { 1, (byte)'a', 1, (byte)'b', 0 })]
    public void TryCreate_ValidName_ProducesExpectedBytes(string name, byte[] expected)
    {
        Span<byte> buffer = stackalloc byte[DnsEncodedName.MaxEncodedLength];
        OperationStatus status = DnsEncodedName.TryEncode(name, buffer, out _, out int bytesWritten);

        Assert.Equal(OperationStatus.Done, status);
        Assert.Equal(expected.Length, bytesWritten);
        Assert.True(buffer[..bytesWritten].SequenceEqual(expected));
    }

    [Theory]
    [InlineData("")]    // empty → root
    [InlineData(".")]   // explicit root
    public void TryCreate_Root_ProducesSingleZeroByte(string name)
    {
        Span<byte> buffer = stackalloc byte[DnsEncodedName.MaxEncodedLength];
        OperationStatus status = DnsEncodedName.TryEncode(name, buffer, out _, out int bytesWritten);

        Assert.Equal(OperationStatus.Done, status);
        Assert.Equal(1, bytesWritten);
        Assert.Equal(0, buffer[0]);
    }

    [Fact]
    public void TryCreate_TrailingDot_SameAsWithout()
    {
        Span<byte> buf1 = stackalloc byte[DnsEncodedName.MaxEncodedLength];
        Span<byte> buf2 = stackalloc byte[DnsEncodedName.MaxEncodedLength];

        DnsEncodedName.TryEncode("example.com", buf1, out _, out int len1);
        DnsEncodedName.TryEncode("example.com.", buf2, out _, out int len2);

        Assert.Equal(len1, len2);
        Assert.True(buf1[..len1].SequenceEqual(buf2[..len2]));
    }

    [Fact]
    public void TryCreate_LabelTooLong_ReturnsInvalidData()
    {
        string longLabel = new string('a', 64) + ".com"; // 64 > 63 max
        Span<byte> buffer = stackalloc byte[DnsEncodedName.MaxEncodedLength];
        OperationStatus status = DnsEncodedName.TryEncode(longLabel, buffer, out _, out _);
        Assert.Equal(OperationStatus.InvalidData, status);
    }

    [Fact]
    public void TryCreate_MaxLengthLabel_Succeeds()
    {
        string maxLabel = new string('a', 63) + ".com";
        Span<byte> buffer = stackalloc byte[DnsEncodedName.MaxEncodedLength];
        OperationStatus status = DnsEncodedName.TryEncode(maxLabel, buffer, out _, out _);
        Assert.Equal(OperationStatus.Done, status);
    }

    [Fact]
    public void TryCreate_ConsecutiveDots_ReturnsInvalidData()
    {
        Span<byte> buffer = stackalloc byte[DnsEncodedName.MaxEncodedLength];
        OperationStatus status = DnsEncodedName.TryEncode("example..com", buffer, out _, out _);
        Assert.Equal(OperationStatus.InvalidData, status);
    }

    [Fact]
    public void TryCreate_NameTooLong_ReturnsInvalidData()
    {
        // Build a name that exceeds 255 wire-format bytes
        // Each "a." label takes 3 bytes (1 length + 1 char + will get a dot separator)
        // 63 labels of "aaa" = 63 * (1+3) + 1 root = 253 bytes — just fits
        // Add one more to overflow
        string name = string.Join(".", Enumerable.Repeat("aaaa", 64));
        Span<byte> buffer = stackalloc byte[512]; // oversized buffer
        OperationStatus status = DnsEncodedName.TryEncode(name, buffer, out _, out _);
        Assert.Equal(OperationStatus.InvalidData, status);
    }

    [Fact]
    public void TryCreate_DestinationTooSmall_ReturnsDestinationTooSmall()
    {
        Span<byte> buffer = stackalloc byte[5]; // too small for "example.com"
        OperationStatus status = DnsEncodedName.TryEncode("example.com", buffer, out _, out _);
        Assert.Equal(OperationStatus.DestinationTooSmall, status);
    }

    [Theory]
    [InlineData("example.com", "example.com", true)]
    [InlineData("example.com", "EXAMPLE.COM", true)]
    [InlineData("example.com", "Example.Com", true)]
    [InlineData("example.com", "example.com.", true)]  // trailing dot ignored
    [InlineData("example.com", "example.org", false)]
    [InlineData("example.com", "example", false)]
    [InlineData("a.b.c", "a.b.c", true)]
    [InlineData("a.b.c", "a.b", false)]
    public void Equals_CaseInsensitiveComparison(string create, string compare, bool expected)
    {
        Span<byte> buffer = stackalloc byte[DnsEncodedName.MaxEncodedLength];
        DnsEncodedName.TryEncode(create, buffer, out var name, out _);
        Assert.Equal(expected, name.Equals(compare));
    }

    [Fact]
    public void TryDecode_ProducesDottedString()
    {
        Span<byte> nameBuffer = stackalloc byte[DnsEncodedName.MaxEncodedLength];
        DnsEncodedName.TryEncode("example.com", nameBuffer, out var name, out _);

        Span<char> chars = stackalloc char[64];
        Assert.True(name.TryDecode(chars, out int written));
        Assert.Equal("example.com", new string(chars[..written]));
    }

    [Fact]
    public void TryDecode_Root_ProducesSingleDot()
    {
        Span<byte> nameBuffer = stackalloc byte[DnsEncodedName.MaxEncodedLength];
        DnsEncodedName.TryEncode(".", nameBuffer, out var name, out _);

        Span<char> chars = stackalloc char[64];
        Assert.True(name.TryDecode(chars, out int written));
        Assert.Equal(1, written);
        Assert.Equal('.', chars[0]);
    }

    [Fact]
    public void TryDecode_DestinationTooSmall_ReturnsFalse()
    {
        Span<byte> nameBuffer = stackalloc byte[DnsEncodedName.MaxEncodedLength];
        DnsEncodedName.TryEncode("example.com", nameBuffer, out var name, out _);

        Span<char> chars = stackalloc char[5]; // too small
        Assert.False(name.TryDecode(chars, out _));
    }

    [Fact]
    public void GetFormattedLength_ReturnsCorrectLength()
    {
        Span<byte> nameBuffer = stackalloc byte[DnsEncodedName.MaxEncodedLength];
        DnsEncodedName.TryEncode("example.com", nameBuffer, out var name, out _);
        Assert.Equal("example.com".Length, name.GetFormattedLength());
    }

    [Theory]
    [InlineData("")]
    [InlineData(".")]
    public void GetFormattedLength_Root_ReturnsOne(string input)
    {
        Span<byte> nameBuffer = stackalloc byte[DnsEncodedName.MaxEncodedLength];
        DnsEncodedName.TryEncode(input, nameBuffer, out var name, out _);
        int length = name.GetFormattedLength();
        Assert.Equal(1, length);

        // Verify that GetFormattedLength is sufficient for TryDecode
        Span<char> decoded = stackalloc char[length];
        Assert.True(name.TryDecode(decoded, out int written));
        Assert.Equal(1, written);
        Assert.Equal('.', decoded[0]);
    }

    [Fact]
    public void ToString_ReturnsFormattedName()
    {
        Span<byte> nameBuffer = stackalloc byte[DnsEncodedName.MaxEncodedLength];
        DnsEncodedName.TryEncode("example.com", nameBuffer, out var name, out _);
        Assert.Equal("example.com", name.ToString());
    }

    [Fact]
    public void ToString_Root_ReturnsDot()
    {
        Span<byte> nameBuffer = stackalloc byte[DnsEncodedName.MaxEncodedLength];
        DnsEncodedName.TryEncode(".", nameBuffer, out var name, out _);
        Assert.Equal(".", name.ToString());
    }

    [Fact]
    public void EnumerateLabels_ReturnsAllLabels()
    {
        Span<byte> nameBuffer = stackalloc byte[DnsEncodedName.MaxEncodedLength];
        DnsEncodedName.TryEncode("a.bb.ccc", nameBuffer, out var name, out _);

        List<string> labels = new();
        foreach (ReadOnlySpan<byte> label in name.EnumerateLabels())
            labels.Add(Encoding.ASCII.GetString(label));

        Assert.Equal(["a", "bb", "ccc"], labels);
    }

    [Fact]
    public void EnumerateLabels_Root_ReturnsNoLabels()
    {
        Span<byte> nameBuffer = stackalloc byte[DnsEncodedName.MaxEncodedLength];
        DnsEncodedName.TryEncode(".", nameBuffer, out var name, out _);

        List<string> labels = new();
        foreach (ReadOnlySpan<byte> label in name.EnumerateLabels())
            labels.Add(Encoding.ASCII.GetString(label));

        Assert.Empty(labels);
    }

    [Fact]
    public void CompressionPointer_FollowedCorrectly()
    {
        // Simulate a DNS message where a name uses a compression pointer:
        // Offset 0: \x07example\x03com\x00  (example.com, 13 bytes)
        // Offset 13: \x03www\xC0\x00        (www + pointer to offset 0 = www.example.com)
        byte[] message =
        [
            7, (byte)'e', (byte)'x', (byte)'a', (byte)'m', (byte)'p', (byte)'l', (byte)'e',
            3, (byte)'c', (byte)'o', (byte)'m', 0,
            3, (byte)'w', (byte)'w', (byte)'w', 0xC0, 0x00
        ];

        Assert.True(DnsEncodedName.TryParse(message, 13, out DnsEncodedName name, out _));
        Assert.True(name.Equals("www.example.com"));
        Assert.Equal("www.example.com", name.ToString());
    }

    [Fact]
    public void CompressionPointer_MidName()
    {
        // Offset 0: \x03com\x00  (com, 5 bytes)
        // Offset 5: \x03foo\xC0\x00  (foo + pointer to offset 0 = foo.com)
        byte[] message =
        [
            3, (byte)'c', (byte)'o', (byte)'m', 0,
            3, (byte)'f', (byte)'o', (byte)'o', 0xC0, 0x00
        ];

        Assert.True(DnsEncodedName.TryParse(message, 5, out DnsEncodedName name, out _));
        Assert.True(name.Equals("foo.com"));
    }

    [Fact]
    public void TryParse_FlatName_BytesConsumedMatchesEncodedLength()
    {
        Span<byte> buffer = stackalloc byte[DnsEncodedName.MaxEncodedLength];
        DnsEncodedName.TryEncode("example.com", buffer, out _, out int bytesWritten);
        Assert.True(DnsEncodedName.TryParse(buffer, 0, out _, out int consumed));
        Assert.Equal(bytesWritten, consumed);
    }

    [Fact]
    public void TryParse_WithCompressionPointer_BytesConsumedIsPointerSize()
    {
        // Name at offset 13: \x03www\xC0\x00  — 6 bytes consumed (1+3 label + 2 pointer)
        byte[] message =
        [
            7, (byte)'e', (byte)'x', (byte)'a', (byte)'m', (byte)'p', (byte)'l', (byte)'e',
            3, (byte)'c', (byte)'o', (byte)'m', 0,
            3, (byte)'w', (byte)'w', (byte)'w', 0xC0, 0x00
        ];

        Assert.True(DnsEncodedName.TryParse(message, 13, out _, out int consumed));
        Assert.Equal(6, consumed);
    }

    [Fact]
    public void CompressionPointer_SelfReferencing_TryParseFails()
    {
        // Pointer at offset 0 that points to itself
        byte[] message = [0xC0, 0x00];
        Assert.False(DnsEncodedName.TryParse(message, 0, out _, out _));
    }

    [Fact]
    public void CompressionPointer_ForwardPointer_TryParseFails()
    {
        // Pointer at offset 0 that points forward to offset 2 (past itself, but within buffer)
        // Offset 2 has another pointer back to offset 0 → loop
        byte[] message = [0xC0, 0x02, 0xC0, 0x00];
        Assert.False(DnsEncodedName.TryParse(message, 0, out _, out _));
    }

    [Fact]
    public void CompressionPointer_ChainedPointers_ResolvesCorrectly()
    {
        // Chained backwards pointers: offset 5 → offset 3 → offset 0 → label "a" + root
        byte[] message = [0x01, (byte)'a', 0x00, 0xC0, 0x00, 0xC0, 0x03];
        Assert.True(DnsEncodedName.TryParse(message, 5, out DnsEncodedName name, out _));
        Assert.True(name.Equals("a"));
    }

    [Fact]
    public void CompressionPointer_OutOfBounds_TryParseFails()
    {
        // Pointer to offset 0xFF, far beyond the 4-byte buffer
        byte[] message = [0xC0, 0xFF, 0x00, 0x00];
        Assert.False(DnsEncodedName.TryParse(message, 0, out _, out _));
    }

    [Fact]
    public void CompressionPointer_ForwardJump_TryParseFails()
    {
        // Forward pointer: offset 0 points to offset 2 (forward, not allowed)
        byte[] message = [0xC0, 0x02, 0x01, (byte)'a', 0x00];
        Assert.False(DnsEncodedName.TryParse(message, 0, out _, out _));
    }

    [Fact]
    public void CompressionPointer_SelfJump_TryParseFails()
    {
        // Self-referencing pointer: offset 0 points to offset 0
        byte[] message = [0xC0, 0x00, 0x00];
        Assert.False(DnsEncodedName.TryParse(message, 0, out _, out _));
    }

    [Fact]
    public void TryCreate_TrailingDoubleDot_ReturnsInvalidData()
    {
        Span<byte> nameBuf = stackalloc byte[DnsEncodedName.MaxEncodedLength];
        OperationStatus status = DnsEncodedName.TryEncode("vp..", nameBuf, out _, out _);
        Assert.Equal(OperationStatus.InvalidData, status);
    }

    [Fact]
    public void TryCreate_NullCharsAndConsecutiveDots_ReturnsInvalidData()
    {
        Span<char> nameChars = ['\0', '\0', '\0', '\0', 'p', '.', '.'];
        Span<byte> nameBuf = stackalloc byte[DnsEncodedName.MaxEncodedLength];
        OperationStatus status = DnsEncodedName.TryEncode(nameChars, nameBuf, out _, out _);
        Assert.Equal(OperationStatus.InvalidData, status);
    }

    [Fact]
    public void TryParse_ValidRootName_Succeeds()
    {
        byte[] buffer = [0x00];
        Assert.True(DnsEncodedName.TryParse(buffer, 0, out DnsEncodedName name, out int consumed));
        Assert.Equal(1, consumed);
        Assert.Equal(".", name.ToString());
    }

    [Fact]
    public void TryParse_ValidFlatName_Succeeds()
    {
        byte[] buffer = [3, (byte)'w', (byte)'w', (byte)'w', 7, (byte)'e', (byte)'x', (byte)'a', (byte)'m', (byte)'p', (byte)'l', (byte)'e', 3, (byte)'c', (byte)'o', (byte)'m', 0];
        Assert.True(DnsEncodedName.TryParse(buffer, 0, out DnsEncodedName name, out int consumed));
        Assert.Equal(buffer.Length, consumed);
        Assert.Equal("www.example.com", name.ToString());
    }

    [Fact]
    public void TryParse_AtOffset_Succeeds()
    {
        // "com" starts at offset 12 in a typical message; simulate with padding
        byte[] buffer = new byte[5 + 3 + 3 + 1]; // 5 bytes padding + 3-label "com" + root
        buffer[5] = 3;
        buffer[6] = (byte)'c';
        buffer[7] = (byte)'o';
        buffer[8] = (byte)'m';
        buffer[9] = 0;
        Assert.True(DnsEncodedName.TryParse(buffer, 5, out DnsEncodedName name, out int consumed));
        Assert.Equal(5, consumed);
        Assert.Equal("com", name.ToString());
    }

    [Fact]
    public void TryParse_WithCompressionPointer_Succeeds()
    {
        // Buffer: "com\0" at offset 0, then a pointer to offset 0 at offset 4
        byte[] buffer = [3, (byte)'c', (byte)'o', (byte)'m', 0, 0xC0, 0x00];
        Assert.True(DnsEncodedName.TryParse(buffer, 5, out DnsEncodedName name, out int consumed));
        Assert.Equal(2, consumed); // compression pointer is 2 bytes
        Assert.Equal("com", name.ToString());
    }

    [Fact]
    public void TryParse_Truncated_ReturnsFalse()
    {
        // Label says length 5 but buffer only has 3 more bytes
        byte[] buffer = [5, (byte)'a', (byte)'b'];
        Assert.False(DnsEncodedName.TryParse(buffer, 0, out _, out _));
    }

    [Fact]
    public void TryParse_LabelTooLong_ReturnsFalse()
    {
        // Label length byte > 63 and not a pointer (0x40..0xBF range)
        byte[] buffer = [0x50, 0x00];
        Assert.False(DnsEncodedName.TryParse(buffer, 0, out _, out _));
    }

    [Fact]
    public void TryParse_NegativeOffset_ReturnsFalse()
    {
        byte[] buffer = [0x00];
        Assert.False(DnsEncodedName.TryParse(buffer, -1, out _, out _));
    }

    [Fact]
    public void TryParse_OffsetBeyondBuffer_ReturnsFalse()
    {
        byte[] buffer = [0x00];
        Assert.False(DnsEncodedName.TryParse(buffer, 5, out _, out _));
    }

    [Fact]
    public void TryParse_EmptyBuffer_ReturnsFalse()
    {
        Assert.False(DnsEncodedName.TryParse(ReadOnlySpan<byte>.Empty, 0, out _, out _));
    }

    [Fact]
    public void TryParse_CompressionPointerLoop_Fails()
    {
        // Two pointers that point at each other: offset 0 → offset 2 → offset 0
        // TryParse now rejects this because forward jumps are not allowed.
        byte[] buffer = [0xC0, 0x02, 0xC0, 0x00];
        Assert.False(DnsEncodedName.TryParse(buffer, 0, out _, out _));
    }

    [Fact]
    public void TryParse_PointerAtEndOfBuffer_ReturnsFalse()
    {
        // Compression pointer with only 1 byte (missing second byte)
        byte[] buffer = [0xC0];
        Assert.False(DnsEncodedName.TryParse(buffer, 0, out _, out _));
    }

    [Fact]
    public void TryParse_LabelExtendsPastBuffer_ReturnsFalse()
    {
        // Label says 5 bytes but buffer only has 2 more bytes after length
        byte[] buffer = [0x05, (byte)'a', (byte)'b'];
        Assert.False(DnsEncodedName.TryParse(buffer, 0, out _, out _));
    }

    [Fact]
    public void TryEncode_RootName_DestinationTooSmall_ReturnsError()
    {
        Span<byte> buffer = Span<byte>.Empty; // 0 bytes — can't even fit root
        OperationStatus status = DnsEncodedName.TryEncode(".", buffer, out _, out _);
        Assert.Equal(OperationStatus.DestinationTooSmall, status);
    }

    [Fact]
    public void TryEncode_DestinationExactlyFitsRootTerminator()
    {
        // Name "a" needs 3 bytes: \x01a\x00. Provide exactly 3 bytes.
        Span<byte> buffer = stackalloc byte[3];
        OperationStatus status = DnsEncodedName.TryEncode("a", buffer, out _, out int written);
        Assert.Equal(OperationStatus.Done, status);
        Assert.Equal(3, written);
    }

    [Fact]
    public void TryEncode_DestinationTooSmallForRootTerminator()
    {
        // Name "a" needs 3 bytes: \x01a\x00. Only provide 2 bytes.
        Span<byte> buffer = stackalloc byte[2];
        OperationStatus status = DnsEncodedName.TryEncode("a", buffer, out _, out _);
        Assert.Equal(OperationStatus.DestinationTooSmall, status);
    }

    [Fact]
    public void TryEncode_NonAsciiCharacter_ConvertedToAce()
    {
        Span<byte> buffer = stackalloc byte[DnsEncodedName.MaxEncodedLength];
        OperationStatus status = DnsEncodedName.TryEncode("café.test", buffer, out DnsEncodedName name, out _);
        Assert.Equal(OperationStatus.Done, status);
        // café → xn--caf-dma in ACE
        Assert.True(name.Equals("xn--caf-dma.test"));
    }

    [Fact]
    public void Equals_DifferentLabelCount_ReturnsFalse()
    {
        Span<byte> buffer = stackalloc byte[DnsEncodedName.MaxEncodedLength];
        DnsEncodedName.TryEncode("a.b.c", buffer, out DnsEncodedName name, out _);
        Assert.False(name.Equals("a.b"));
        Assert.False(name.Equals("a.b.c.d"));
    }

    [Fact]
    public void Equals_EmptyString_MatchesRoot()
    {
        Span<byte> buffer = stackalloc byte[1];
        DnsEncodedName.TryEncode(".", buffer, out DnsEncodedName name, out _);
        Assert.True(name.Equals(""));
        Assert.True(name.Equals("."));
    }

    [Fact]
    public void TryParse_CompressionPointer_BytesConsumedIsTwo()
    {
        // "com\0" at offset 0, then pointer at offset 4
        byte[] buffer = [3, (byte)'c', (byte)'o', (byte)'m', 0, 0xC0, 0x00];
        Assert.True(DnsEncodedName.TryParse(buffer, 5, out _, out int consumed));
        // Compression pointer consumes 2 bytes
        Assert.Equal(2, consumed);
    }

    // === IDN (Internationalized Domain Name) Tests ===

    [Theory]
    [InlineData("münchen.de", "xn--mnchen-3ya.de")]
    [InlineData("例え.jp", "xn--r8jz45g.jp")]
    [InlineData("café.test", "xn--caf-dma.test")]
    [InlineData("домен.рф", "xn--d1acufc.xn--p1ai")]
    public void TryEncode_IdnName_ProducesAceWireFormat(string unicode, string expectedAce)
    {
        Span<byte> buffer = stackalloc byte[DnsEncodedName.MaxEncodedLength];
        OperationStatus status = DnsEncodedName.TryEncode(unicode, buffer, out DnsEncodedName name, out _);
        Assert.Equal(OperationStatus.Done, status);
        Assert.True(name.Equals(expectedAce));
    }

    [Theory]
    [InlineData("münchen.de")]
    [InlineData("例え.jp")]
    [InlineData("café.test")]
    public void TryEncode_IdnName_RoundTripsViaToString(string unicode)
    {
        Span<byte> buffer = stackalloc byte[DnsEncodedName.MaxEncodedLength];
        OperationStatus status = DnsEncodedName.TryEncode(unicode, buffer, out DnsEncodedName name, out _);
        Assert.Equal(OperationStatus.Done, status);
        Assert.Equal(unicode, name.ToString());
    }

    [Theory]
    [InlineData("münchen.de")]
    [InlineData("café.test")]
    public void TryDecode_IdnName_ProducesUnicode(string unicode)
    {
        Span<byte> buffer = stackalloc byte[DnsEncodedName.MaxEncodedLength];
        DnsEncodedName.TryEncode(unicode, buffer, out DnsEncodedName name, out _);

        Span<char> decoded = stackalloc char[256];
        Assert.True(name.TryDecode(decoded, out int written));
        Assert.Equal(unicode, new string(decoded[..written]));
    }

    [Theory]
    [InlineData("münchen.de")]
    [InlineData("café.test")]
    public void Equals_IdnName_MatchesUnicode(string unicode)
    {
        Span<byte> buffer = stackalloc byte[DnsEncodedName.MaxEncodedLength];
        DnsEncodedName.TryEncode(unicode, buffer, out DnsEncodedName name, out _);
        Assert.True(name.Equals(unicode));
    }

    [Fact]
    public void Equals_IdnName_CaseInsensitive()
    {
        Span<byte> buffer = stackalloc byte[DnsEncodedName.MaxEncodedLength];
        DnsEncodedName.TryEncode("münchen.de", buffer, out DnsEncodedName name, out _);
        // ACE form comparison is case-insensitive
        Assert.True(name.Equals("XN--MNCHEN-3YA.DE"));
    }

    [Fact]
    public void TryEncode_MixedAsciiAndIdn_Succeeds()
    {
        // "www" is ASCII, "münchen" is IDN, "de" is ASCII
        Span<byte> buffer = stackalloc byte[DnsEncodedName.MaxEncodedLength];
        OperationStatus status = DnsEncodedName.TryEncode("www.münchen.de", buffer, out DnsEncodedName name, out _);
        Assert.Equal(OperationStatus.Done, status);
        Assert.Equal("www.münchen.de", name.ToString());
        Assert.True(name.Equals("www.xn--mnchen-3ya.de"));
    }

    [Fact]
    public void TryEncode_AceNamePassesThrough()
    {
        // Already-ACE input should pass through unchanged
        Span<byte> buffer = stackalloc byte[DnsEncodedName.MaxEncodedLength];
        OperationStatus status = DnsEncodedName.TryEncode("xn--mnchen-3ya.de", buffer, out DnsEncodedName name, out _);
        Assert.Equal(OperationStatus.Done, status);
        Assert.Equal("münchen.de", name.ToString());
    }

    [Fact]
    public void GetFormattedLength_IdnName_ReturnsUnicodeLength()
    {
        Span<byte> buffer = stackalloc byte[DnsEncodedName.MaxEncodedLength];
        DnsEncodedName.TryEncode("münchen.de", buffer, out DnsEncodedName name, out _);
        // "münchen.de" is 10 chars, not "xn--mnchen-3ya.de" (18 chars)
        Assert.Equal("münchen.de".Length, name.GetFormattedLength());
    }

    [Fact]
    public void TryEncode_IdnWithTrailingDot_Succeeds()
    {
        Span<byte> buffer = stackalloc byte[DnsEncodedName.MaxEncodedLength];
        OperationStatus status = DnsEncodedName.TryEncode("münchen.de.", buffer, out DnsEncodedName name, out _);
        Assert.Equal(OperationStatus.Done, status);
        Assert.Equal("münchen.de", name.ToString());
    }

    [Fact]
    public void Equals_InvalidUnicode_ReturnsFalse()
    {
        Span<byte> buffer = stackalloc byte[DnsEncodedName.MaxEncodedLength];
        DnsEncodedName.TryEncode("example.com", buffer, out DnsEncodedName name, out _);
        // Lone surrogate — invalid Unicode, can't convert to ACE
        Assert.False(name.Equals("\uD800.com"));
    }
}
