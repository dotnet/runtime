// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Xunit;

//
// System.Console BCL test cases
//
public class OpenStandardXXX
{
    [Fact]
    public static void OpenStandardXXX_NegativeTesting()
    {
        Console.WriteLine("Testing WriteTimeout with OpenStandardXXX");
        Stream[] streams = new Stream[] { Console.OpenStandardOutput(), Console.OpenStandardError() };
        int index = 0;
        foreach (Stream stream in streams)
        {
            Console.WriteLine(String.Format("Testing stream {0}({1})", stream, index++));
            TestOpenStandardXXX(stream);
        }
    }

    private static void TestOpenStandardXXX(Stream stream)
    {
        Console.WriteLine("Adding code coverage to type: {0}", stream.GetType());

        //Err001:__ConsoleStream.Length should throw NotSupportedException
        Assert.Throws<NotSupportedException>(() => { long x = stream.Length; });
        //Err002:__ConsoleStream.get_Position should throw NotSupportedException
        Assert.Throws<NotSupportedException>(() => { long x = stream.Position; });
        //Err003: __ConsoleStream.set_Position should throw NotSupportedException
        Assert.Throws<NotSupportedException>(() => stream.Position = 50L);
        //Err004:__ConsoleStream.SetLength should throw NotSupportedException
        Assert.Throws<NotSupportedException>(() => stream.SetLength(50L));

        try
        {
            stream.Flush();
        }
        catch (Exception ex)
        {
            Assert.True(false, String.Format("Err005: Wrong exception thrown: {0}", ex.GetType().Name));
        }

        //Read and write methods
        //Note that the stream we opened is StandardOutput and hence really dont support reading but the following is sufficient for the code coverage we are tryng to hit
        //Err006:__ConsoleStream.Read(null) should throw ArgumentNullException
        Assert.Throws<ArgumentNullException>(() => stream.Read(null, 0, 1));
        //Err007:__ConsoleStream.Read() should throw ArgumentOutOfRangeException
        Assert.Throws<ArgumentOutOfRangeException>(() => stream.Read(new Byte[] { 0, 1 }, -1, 0));
        //Err008:__ConsoleStream.Read() should throw ArgumentOutOfRangeException
        Assert.Throws<ArgumentOutOfRangeException>(() => stream.Read(new Byte[] { 0, 1 }, 0, -1));
        //Err009: __ConsoleStream.Read() should throw ArgumentException
        Assert.Throws<ArgumentException>(() => stream.Read(new Byte[] { 0, 1 }, 0, 50));
        //Err010:__ConsoleStream.Read() should throw NotSupportedException
        Assert.Throws<NotSupportedException>(() => stream.Read(new Byte[] { 0, 1 }, 0, 2));
        //Err011:__ConsoleStream.Write() should throw ArgumentNullException
        Assert.Throws<ArgumentNullException>(() => stream.Write(null, 0, 1));
        //Err011:__ConsoleStream.Write() should throw ArgumentOutOfRangeException
        Assert.Throws<ArgumentOutOfRangeException>(() => stream.Write(new Byte[] { 0, 1 }, -1, 0));
        //Err011:__ConsoleStream.Write() should throw ArgumentOutOfRangeException
        Assert.Throws<ArgumentOutOfRangeException>(() => stream.Write(new Byte[] { 0, 1 }, 0, -1));
        //Err012:__ConsoleStream.Write() should throw ArgumentException
        Assert.Throws<ArgumentException>(() => stream.Write(new Byte[] { 0, 1 }, 0, 50));
        //Err013:__ConsoleStream.Write() should throw NotSupportedException
        Assert.Throws<NotSupportedException>(() => stream.Seek(0L, SeekOrigin.Begin));

        //ReadFileNative
        try
        {
            stream.Write(new Byte[] { }, 0, 0);
        }
        catch (Exception ex)
        {
            Assert.True(false, String.Format("Err_014:Wrong exception thrown: {0}", ex));
        }

        try
        {
            stream.Write(new Byte[4], 0, 4);
        }
        catch (Exception ex)
        {
            Assert.True(false, String.Format("Err_015:Wrong exception thrown: {0}", ex));
        }

        //Close
        try
        {
            stream.Dispose();
        }
        catch (Exception ex)
        {
            Assert.True(false, String.Format("Err_016:Wrong exception thrown: {0}", ex));
        }

        //Err017:__ConsoleStream.Read() should throw NotSupportedException
        Assert.Throws<NotSupportedException>(() => stream.Read(new Byte[] { 0, 1 }, 0, 1));
        //Err018:__ConsoleStream.Write() should throw NotSupportedException
        Assert.Throws<NotSupportedException>(() => stream.Write(new Byte[] { 0, 1 }, 0, 1));
        //Err018:__ConsoleStream.Flush() should throw NotSupportedException
        Assert.Throws<ObjectDisposedException>(() => stream.Flush());
    }
}
