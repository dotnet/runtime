// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
