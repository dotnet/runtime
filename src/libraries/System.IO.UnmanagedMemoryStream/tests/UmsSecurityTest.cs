// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.IO.Tests;
using Xunit;

public class UmsSecurityTests
{
    [Fact]
    public static void ChangePositioViaPointer()
    {
        byte[] data = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 127, 255 };
        int positionPointerByteArrayNumber = 3;
        Byte[][] positionPointerByteArrays = new Byte[positionPointerByteArrayNumber][];

        unsafe
        {
            fixed (byte* bytePtr = data)
            {
                //Scenario 1:ST - change the position via the PositionPointer 
                using (var stream = new UnmanagedMemoryStream(bytePtr, data.Length, data.Length, FileAccess.ReadWrite))
                {
                    //we try positionPointerByteArrayNumber (10) different Byte[] arrays
                    for (int positionLoop = 0; positionLoop < positionPointerByteArrayNumber; positionLoop++)
                    {
                        //New Byte array
                        positionPointerByteArrays[positionLoop] = ArrayHelpers.CreateByteArray(length: 123, value: 24);
                        //change via PositionPointer
                        fixed (byte* invalidbytePtr = positionPointerByteArrays[positionLoop])
                        {
                            // This depends on the layout of pinned memory
                            Assert.True((long)invalidbytePtr > ((long)bytePtr + data.Length), String.Format("{0} > {1} + {2}", (long)invalidbytePtr, (long)bytePtr, data.Length));
                            // not throw currently
                            stream.PositionPointer = invalidbytePtr;
                            VerifyNothingCanBeReadOrWritten(stream, data);
                        }
                    }

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
