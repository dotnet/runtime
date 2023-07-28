// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//COMMAND LINE: csc /nologo /optimize- /debug- /w:0 bug.cs
using System;
using Xunit;
public class BB
{
    byte Method1(sbyte[,][][,] param2)
    {
        return new byte[][, ,] { }[0][Math.Sign(1), Math.Sign(1), Math.Min(0, 0)];
    }
    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            Console.WriteLine("Testing BB::Method1");
            new BB().Method1(
                (new sbyte[10, 10][,][,][][,])[9, 9][Math.Sign(10),
                    new int[] { 10, 10, 10 }[10]]
                 );
        }
        catch (Exception x) { }
        Console.WriteLine("Passed.");
        return 100;
    }
}
