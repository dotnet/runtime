// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Xunit;

public class NegativeTesting
{
    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsInAppContainer))] // In appcontainer, the stream cannot be opened: there is no Console
    public static void OpenStandardOutNegativeTests_Uap()
    {
        Assert.Same(Stream.Null, Console.OpenStandardOutput());
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsInAppContainer))] // In appcontainer, the stream cannot be opened: there is no Console
    public static void OpenStandardErrorNegativeTests_Uap()
    {
        Assert.Same(Stream.Null, Console.OpenStandardError());
    }

    [Fact]
    public static void StreamNullTests()
    {
        Stream stream = Stream.Null;

        Assert.Equal(0L, stream.Length);

        Assert.Equal(0L, stream.Position);

        stream.Position = 50L;
        stream.SetLength(50L);
        Assert.Equal(0L, stream.Length);

        // Flushing a stream is fine.
        stream.Flush();

        //Read and write methods

        Assert.Equal(0, stream.Read(null, 0, 1));
        Assert.Equal(0, stream.Read(new byte[] { 0, 1 }, -1, 0));
        Assert.Equal(0, stream.Read(new byte[] { 0, 1 }, 0, -1));
        Assert.Equal(0, stream.Read(new byte[] { 0, 1 }, 0, 50));
        Assert.Equal(0, stream.Read(new byte[] { 0, 1 }, 0, 2));
        
        stream.Write(null, 0, 1);
        stream.Write(new byte[] { 0, 1 }, -1, 0);
        stream.Write(new byte[] { 0, 1 }, 0, -1);
        stream.Write(new byte[] { 0, 1 }, 0, 50);

        Assert.Equal(0L, stream.Seek(0L, SeekOrigin.Begin));

        // Null stream even ignores Dispose.
        stream.Dispose();

        Assert.Equal(0, stream.Read(new byte[] { 0, 1 }, 0, 1));
        stream.Write(new byte[] { 0, 1 }, 0, 1);
        stream.Flush();
    }
}
