// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

internal class Foo : IDisposable
{
    public void Dispose()
    {
    }
}

public class GitHub_18332
{
    // In Aargh there is a finally with two distinct exit paths.
    // Finally cloning may choose the non-fall through ("wibble") exit
    // path to clone, and then will try to incorrectly arrange for
    // that path to become the fall through.
    public static string Aargh()
    {
        using (var foo = new Foo())
        {
            foreach (var i in new List<int>())
            {
                try
                {
                    Console.WriteLine("here");
                }
                catch (Exception)
                {
                    return "wibble";
                }
            }
            
            foreach (var i in new List<int>())
            {
            }
        }
        
        return "wobble";
    }
    
    [Fact]
    public static int TestEntryPoint()
    {
        string expected = "wobble";
        string actual = Aargh();
        if (actual != expected)
        {
            Console.WriteLine($"FAIL: Aargh() returns '{actual}' expected '{expected}'");
            return 0;
        }
        return 100;
    }
}
