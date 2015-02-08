// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Xunit;

public class NegativeTesting
{
    [Fact]
    public static void OpenStandardOutNegativeTests()
    {
        NegativeConsoleOutputTests(Console.OpenStandardOutput());
    }

    [Fact]
    public static void OpenStandardErrorNegativeTests()
    {
        NegativeConsoleOutputTests(Console.OpenStandardError());
    }

    private static void NegativeConsoleOutputTests(Stream stream)
    {
        //ConsoleStream.Length should throw NotSupportedException
        Assert.Throws<NotSupportedException>(() => { long x = stream.Length; });

        //ConsoleStream.get_Position should throw NotSupportedException
        Assert.Throws<NotSupportedException>(() => { long x = stream.Position; });
        
        //ConsoleStream.set_Position should throw NotSupportedException
        Assert.Throws<NotSupportedException>(() => stream.Position = 50L);
        
        //ConsoleStream.SetLength should throw NotSupportedException
        Assert.Throws<NotSupportedException>(() => stream.SetLength(50L));

        // Flushing a stream is fine.
        stream.Flush();

        //Read and write methods

        //ConsoleStream.Read(null) should throw ArgumentNullException
        Assert.Throws<ArgumentNullException>(() => stream.Read(null, 0, 1));
        
        //ConsoleStream.Read() should throw ArgumentOutOfRangeException
        Assert.Throws<ArgumentOutOfRangeException>(() => stream.Read(new byte[] { 0, 1 }, -1, 0));
        
        //ConsoleStream.Read() should throw ArgumentOutOfRangeException
        Assert.Throws<ArgumentOutOfRangeException>(() => stream.Read(new byte[] { 0, 1 }, 0, -1));
        
        //ConsoleStream.Read() should throw ArgumentException
        Assert.Throws<ArgumentException>(() => stream.Read(new byte[] { 0, 1 }, 0, 50));
        
        //ConsoleStream.Read() should throw NotSupportedException
        Assert.Throws<NotSupportedException>(() => stream.Read(new byte[] { 0, 1 }, 0, 2));
        
        //ConsoleStream.Write() should throw ArgumentNullException
        Assert.Throws<ArgumentNullException>(() => stream.Write(null, 0, 1));
        
        //ConsoleStream.Write() should throw ArgumentOutOfRangeException
        Assert.Throws<ArgumentOutOfRangeException>(() => stream.Write(new byte[] { 0, 1 }, -1, 0));
        
        //ConsoleStream.Write() should throw ArgumentOutOfRangeException
        Assert.Throws<ArgumentOutOfRangeException>(() => stream.Write(new byte[] { 0, 1 }, 0, -1));

        //ConsoleStream.Write() should throw ArgumentException
        Assert.Throws<ArgumentException>(() => stream.Write(new byte[] { 0, 1 }, 0, 50));
        
        //ConsoleStream.Write() should throw NotSupportedException
        Assert.Throws<NotSupportedException>(() => stream.Seek(0L, SeekOrigin.Begin));

        // Close the stream and make sure we can no longer read, write or flush
        stream.Dispose();

        //ConsoleStream.Read() should throw NotSupportedException
        Assert.Throws<NotSupportedException>(() => stream.Read(new byte[] { 0, 1 }, 0, 1));

        //ConsoleStream.Write() should throw NotSupportedException
        Assert.Throws<NotSupportedException>(() => stream.Write(new byte[] { 0, 1 }, 0, 1));
        
        //ConsoleStream.Flush() should throw NotSupportedException
        Assert.Throws<ObjectDisposedException>(() => stream.Flush());
    }
}
