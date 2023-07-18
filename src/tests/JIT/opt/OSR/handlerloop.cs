// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

// OSR can't bail us out of a loop in a handler
//
public class OSRHandlerLoop
{
    [Fact]
    public static int TestEntryPoint()
    {
        int result = 0;
        int expected = 0;
        try
        {
            result++;
            expected = 704982705;
        }
        finally
        {
            for (int i = 0; i < 100_000; i++)
            {
                result += i;
            }
        }

        Console.WriteLine($"{result} expected {expected}");

        return (result == expected) ? 100 : -1;
    }
}
