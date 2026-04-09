// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


namespace DevDiv_876169_ro;

using System;
using Xunit;

public class Repro
{
    [OuterLoop]
    [Fact]
    public static void TestEntryPoint()
    {
        //We used to incorrectly generate an infinite loop by
        //emitting a jump instruction to itself
        //The correct behaviour would be to immediately exit the loop

        int i = 0;
        while (i < 0 || i < 1)
        {
            i++;
        }
        Console.WriteLine("PASS!");
    }
}
