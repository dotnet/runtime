// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;

[Collection(nameof(DisableParallelization))]
public class BeepByBell
{
    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [PlatformSpecific(TestPlatforms.Windows)]
    public void BeepDoesNotWriteToRedirectedOut(bool redirectOut, bool redirectError)
    {
        using var streamOut = new MemoryStream();
        using var streamErr = new MemoryStream();
        using var writerOut = new StreamWriter(streamOut);
        using var writerErr = new StreamWriter(streamErr);

        TextWriter originalOut = Console.Out;
        TextWriter originalErr = Console.Error;

        try
        {
            if (redirectOut)
            {
                Console.SetOut(writerOut);
            }

            if (redirectError)
            {
                Console.SetOut(writerErr);
            }

            Console.Beep();
            Assert.Equal(0, streamOut.Length);
            Assert.Equal(0, streamErr.Length);
        }
        finally
        {
            if (redirectOut)
            {
                Console.SetOut(originalOut);
            }

            if (redirectError)
            {
                Console.SetOut(originalErr);
            }
        }
    }
}
