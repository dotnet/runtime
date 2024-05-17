using System.Buffers.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace System.Runtime.Serialization.BinaryFormat.Tests;

public class ContainsBinaryFormatterPayloadTests
{
    public static IEnumerable<object[]> GetArguments()
    {
        // too few bytes
        yield return new object[]
        {
            new byte[] { (byte)RecordType.SerializedStreamHeader, (byte)RecordType.MessageEnd },
            false
        };

        byte[] smallestValid = new byte[2 + (sizeof(int) * 4)];

        smallestValid[0] = (byte)RecordType.SerializedStreamHeader;
        Assert.True(Utf8Formatter.TryFormat(1, smallestValid.AsSpan(1 + (sizeof(int) * 0)), out _)); // root id
        Assert.True(Utf8Formatter.TryFormat(2, smallestValid.AsSpan(1 + (sizeof(int) * 1)), out _)); // header id
        Assert.True(Utf8Formatter.TryFormat(1, smallestValid.AsSpan(1 + (sizeof(int) * 2)), out _)); // major version
        Assert.True(Utf8Formatter.TryFormat(0, smallestValid.AsSpan(1 + (sizeof(int) * 3)), out _)); // minor version
        smallestValid[smallestValid.Length - 1] = (byte)RecordType.MessageEnd;

        yield return new object[]{ smallestValid, true };
        yield return new object[] { smallestValid.Skip(1).ToArray(), false };
        yield return new object[] { smallestValid.Take(smallestValid.Length / 2).ToArray(), false };
    }

    [Theory]
    [MemberData(nameof(GetArguments))]
    public void ContainsBinaryFormatterPayload_ReturnsExpectedResult(byte[] bytes, bool expected)
    {
        Assert.Equal(expected, PayloadReader.ContainsBinaryFormatterPayload(bytes));

        using MemoryStream stream = new MemoryStream(bytes);
        Assert.Equal(expected, PayloadReader.ContainsBinaryFormatterPayload(stream));
    }
}
