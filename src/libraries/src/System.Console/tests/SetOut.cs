// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Xunit;

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
