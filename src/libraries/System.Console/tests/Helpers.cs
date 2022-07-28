// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text;
using Xunit;

static class Helpers
{
    public static bool IsConsoleInSupported =>
        !PlatformDetection.IsAndroid && !PlatformDetection.IsiOS && !PlatformDetection.IsMacCatalyst && !PlatformDetection.IstvOS && !PlatformDetection.IsBrowser;

    public static void SetAndReadHelper(Action<TextWriter> setHelper, Func<TextWriter> getHelper, Func<StreamReader, string> readHelper)
    {
        const string TestString = "Test";

        TextWriter oldWriterToRestore = getHelper();
        Assert.NotNull(oldWriterToRestore);

        try
        {
            var memStream = new MemoryStream();
            var sw = new StreamWriter(memStream);
            setHelper(sw);

            TextWriter newStream = getHelper();
            Assert.NotNull(newStream);
            newStream.Write(TestString);
            newStream.Flush();

            memStream.Seek(0, SeekOrigin.Begin);
            Assert.Equal(TestString, readHelper(new StreamReader(memStream)));
        }
        finally
        {
            setHelper(oldWriterToRestore);
        }
    }

    public static void RunInRedirectedOutput(Action<MemoryStream> command)
    {
        // Make sure that redirecting to a memory stream causes no special writing to the stream when using Console.CursorVisible
        MemoryStream data = new MemoryStream();
        TextWriter savedOut = Console.Out;
        try
        {
            Console.SetOut(new StreamWriter(data, new UTF8Encoding(false), 0x1000, leaveOpen: true) { AutoFlush = true });
            command(data);
        }
        finally
        {
            Console.SetOut(savedOut);
        }
    }

    public static void RunInNonRedirectedOutput(Action<MemoryStream> command)
    {
        // Make sure that when writing out to a UnixConsoleStream
        // written out.
        MemoryStream data = new MemoryStream();
        TextWriter savedOut = Console.Out;
        try
        {
            Console.SetOut(
                new InterceptStreamWriter(
                    Console.OpenStandardOutput(),
                    new StreamWriter(data, new UTF8Encoding(false), 0x1000, leaveOpen: true) { AutoFlush = true },
                    new UTF8Encoding(false), 0x1000, leaveOpen: true)
                { AutoFlush = true });
            command(data);
        }
        finally
        {
            Console.SetOut(savedOut);
        }
    }
}
