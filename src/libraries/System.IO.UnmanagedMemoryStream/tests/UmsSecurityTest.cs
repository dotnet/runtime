// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.IO.Tests;
using Xunit;

public class UmsSecurityTests
{
    [Fact]
    public static unsafe void ChangePositionViaPointer()
    {
        byte[] data = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 127, 255 };
        fixed (byte* bytePtr = data)
        {
            using var stream = new UnmanagedMemoryStream(bytePtr, data.Length, data.Length, FileAccess.ReadWrite);

            // Make sure the position pointer is where we set it to be
            Assert.Equal(expected: (IntPtr)bytePtr, actual: (IntPtr)stream.PositionPointer);

            // Make sure that moving earlier than the beginning of the stream throws
            Assert.Throws<IOException>(() =>
            {
                stream.PositionPointer = stream.PositionPointer - 1;
            });

            // Make sure that moving later than the length can be done but then
            // fails appropriately during reads and writes, and that the stream's
            // data is still intact after the fact
            stream.PositionPointer = bytePtr + data.Length;
            VerifyNothingCanBeReadOrWritten(stream, data);
            CheckStreamIntegrity(stream, data);
        }
    }

    [Fact]
    public static unsafe void OverflowPositionPointer()
    {
        using var ums = new UnmanagedMemoryStream((byte*)0x40000000, 0xB8000000);

        ums.PositionPointer = (byte*)0xF0000000;
        Assert.Equal(0xB0000000, ums.Position);

        if (IntPtr.Size == 4)
        {
            ums.PositionPointer = (byte*)ulong.MaxValue;
            Assert.Equal(uint.MaxValue - 0x40000000, ums.Position);
        }
        else
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ums.PositionPointer = (byte*)ulong.MaxValue);
        }
    }

    static void VerifyNothingCanBeReadOrWritten(UnmanagedMemoryStream stream, byte[] data)
    {
        // No Read
        int count = stream.Read(data, 0, data.Length);
        Assert.Equal(0, count);
        Assert.Equal(-1, stream.ReadByte());

        // No write
        Assert.Throws<NotSupportedException>(() => stream.Write(data, 0, data.Length)); // Stream does not support writing.
        Assert.Throws<NotSupportedException>(() => stream.WriteByte(byte.MaxValue)); // Stream does not support writing.
    }

    private static void CheckStreamIntegrity(UnmanagedMemoryStream stream, byte[] originalData)
    {
        stream.Position = 0;
        byte[] streamData = new byte[originalData.Length];
        int value = stream.Read(streamData, 0, streamData.Length);

        Assert.Equal(originalData.Length, value);
        AssertExtensions.SequenceEqual(originalData, streamData);

        stream.Position = 0;
    }
}
