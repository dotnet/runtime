// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.IO.Tests;
using Xunit;

public class UmsSecurityTests
{
    [Fact]
    public static void ChangePositionViaPointer()
    {
        byte[] data = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 127, 255 };
        unsafe
        {
            fixed (byte* bytePtr = data)
            {
                using (var stream = new UnmanagedMemoryStream(bytePtr, data.Length, data.Length, FileAccess.ReadWrite))
                {
                    // Make sure the position pointer is where we set it to be
                    Assert.Equal(expected: (IntPtr)bytePtr, actual: (IntPtr)stream.PositionPointer);

                    // Make sure that moving earlier than the beginning of the stream throws
                    Assert.Throws<IOException>(() => {
                        stream.PositionPointer = stream.PositionPointer - 1;
                    });

                    // Make sure that moving later than the length can be done but then
                    // fails appropriately during reads and writes, and that the stream's
                    // data is still intact after the fact
                    stream.PositionPointer = bytePtr + data.Length;
                    VerifyNothingCanBeReadOrWritten(stream, data);
                    CheckStreamIntegrity(stream, data);
                }
            } // fixed
        }
    }

    static void VerifyNothingCanBeReadOrWritten(UnmanagedMemoryStream stream, Byte[] data)
    {
        // No Read
        int count = stream.Read(data, 0, data.Length);
        Assert.Equal(0, count);
        Assert.Equal(-1, stream.ReadByte());

        // No write
        Assert.Throws<NotSupportedException>(() => stream.Write(data, 0, data.Length)); // Stream does not support writing.
        Assert.Throws<NotSupportedException>(() => stream.WriteByte(Byte.MaxValue)); // Stream does not support writing.
    }

    public static void CheckStreamIntegrity(UnmanagedMemoryStream stream, Byte[] originalData)
    {
        stream.Position = 0;
        Byte[] streamData = new Byte[originalData.Length];
        int value = stream.Read(streamData, 0, streamData.Length);
        Assert.Equal(originalData.Length, value);

        for (int i = 0; i < originalData.Length; i++)
            Assert.Equal(originalData[i], streamData[i]);

        stream.Position = 0;
    }
}
