// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Xunit;

public class SetError
{
    [Fact]
    public static void SetErrorThrowsOnNull()
    {
        TextWriter savedError = Console.Error;        
        try
        {
            Assert.Throws<ArgumentNullException>(() => Console.SetError(null));
        }
        finally
        {
            Console.SetError(savedError);
        }
    }

    [Fact]
    public static void SetErrorRead()
    {
        Helpers.SetAndReadHelper(tw => Console.SetError(tw), () => Console.Error, sr => sr.ReadLine());
    }

    [Fact]
    public static void SetErrorReadToEnd()
    {
        Helpers.SetAndReadHelper(tw => Console.SetError(tw), () => Console.Error, sr => sr.ReadToEnd());
    }
}
