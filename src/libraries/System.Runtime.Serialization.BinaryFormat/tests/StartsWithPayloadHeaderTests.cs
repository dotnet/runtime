﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace System.Runtime.Serialization.BinaryFormat.Tests;

public class StartsWithPayloadHeaderTests
{
    public static IEnumerable<object[]> GetArguments()
    {
       // too few bytes
       yield return new object[]
       {
            new byte[] { (byte)RecordType.SerializedStreamHeader, (byte)RecordType.MessageEnd },
            false
       };

        using MemoryStream memoryStream = new MemoryStream();
        BinaryWriter writer = new BinaryWriter(memoryStream, Encoding.UTF8);
        writer.Write((byte)RecordType.SerializedStreamHeader);
        writer.Write(1); // root id
        writer.Write(2); // header id
        writer.Write(1); // major version
        writer.Write(0); // minor version
        writer.Write((byte)RecordType.MessageEnd);
        byte[] smallestValid = memoryStream.ToArray();

        yield return new object[] { smallestValid, true };
        yield return new object[] { smallestValid.Skip(1).ToArray(), false };
        yield return new object[] { smallestValid.Take(smallestValid.Length / 2).ToArray(), false };
    }

    [Theory]
    [MemberData(nameof(GetArguments))]
    public void StartsWithPayloadHeader_ReturnsExpectedResult(byte[] bytes, bool expected)
    {
        Assert.Equal(expected, PayloadReader.StartsWithPayloadHeader(bytes));

        using MemoryStream stream = new MemoryStream(bytes);
        Assert.Equal(expected, PayloadReader.StartsWithPayloadHeader(stream));
    }
}
