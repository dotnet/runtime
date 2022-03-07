// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;

[Collection(nameof(DisableParallelization))] // Console.Out is a static singleton, other tests should not be using it at the same time
public class SetOut
{
    [Fact]
    public static void SetOutThrowsOnNull()
    {
        TextWriter savedOut = Console.Out;
        try
        {
            Assert.Throws<ArgumentNullException>(() => Console.SetOut(null));
        }
        finally
        {
            Console.SetOut(savedOut);
        }
    }

    [Fact]
    public static void SetOutReadLine()
    {
        Helpers.SetAndReadHelper(tw => Console.SetOut(tw), () => Console.Out, sr => sr.ReadLine());
    }

    [Fact]
    public static void SetOutReadToEnd()
    {
        Helpers.SetAndReadHelper(tw => Console.SetOut(tw), () => Console.Out, sr => sr.ReadToEnd());
    }
}
