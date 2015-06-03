// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

public class Color
{
    [Fact]
    [PlatformSpecific(PlatformID.AnyUnix)]
    public static void RedirectedOutputDoesNotUseAnsiSequences()
    {
        // Make sure that redirecting to a memory stream causes Console not to write out the ANSI sequences
        MemoryStream data = new MemoryStream();
        TextWriter savedOut = Console.Out;
        try
        {
            Console.SetOut(new StreamWriter(data, new UTF8Encoding(false), 0x1000, leaveOpen: true) { AutoFlush = true });
            Console.Write('1');
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write('2');
            Console.BackgroundColor = ConsoleColor.Red;
            Console.Write('3');
            Console.ResetColor();
            Console.Write('4');
        }
        finally
        {
            Console.SetOut(savedOut);
        }

        const char Esc = (char)0x1B;
        Assert.Equal(0, Encoding.UTF8.GetString(data.ToArray()).ToCharArray().Count(c => c == Esc));
        Assert.Equal("1234", Encoding.UTF8.GetString(data.ToArray()));
    }

    //[Fact] // the CI system redirects stdout, so we can't easily test non-redirected behavior
    [PlatformSpecific(PlatformID.AnyUnix)]
    public static void NonRedirectedOutputDoesUseAnsiSequences()
    {
        // Make sure that when writing out to a UnixConsoleStream, the ANSI escape sequences are properly
        // written out.
        MemoryStream data = new MemoryStream();
        TextWriter savedOut = Console.Out;
        try
        {
            Console.SetOut(
                new InterceptStreamWriter(
                    Console.OpenStandardOutput(),
                    new StreamWriter(data, new UTF8Encoding(false), 0x1000, leaveOpen: true) { AutoFlush = true },
                    new UTF8Encoding(false), 0x1000, leaveOpen: true) { AutoFlush = true });
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.BackgroundColor = ConsoleColor.Red;
            Console.ResetColor();
        }
        finally
        {
            Console.SetOut(savedOut);
        }

        const char Esc = (char)0x1B;
        Assert.Equal(3, Encoding.UTF8.GetString(data.ToArray()).ToCharArray().Count(c => c == Esc));
    }

    private sealed class InterceptStreamWriter : StreamWriter
    {
        private readonly StreamWriter _wrappedWriter;

        public InterceptStreamWriter(Stream baseStream, StreamWriter wrappedWriter, Encoding encoding, int bufferSize, bool leaveOpen) : 
            base(baseStream, encoding, bufferSize, leaveOpen)
        {
            _wrappedWriter = wrappedWriter;
        }

        public override void Write(string value)
        {
            base.Write(value);
            _wrappedWriter.Write(value);
        }

        public override void Write(char value)
        {
            base.Write(value);
            _wrappedWriter.Write(value);
        }
    }

}
