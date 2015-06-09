// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

//COMMAND LINE: csc /nologo /optimize- /debug- /w:0 bug.cs
using System;
public class BB
{
    byte Method1(sbyte[,][][,] param2)
    {
        return new byte[][, ,] { }[0][Math.Sign(1), Math.Sign(1), Math.Min(0, 0)];
    }
    static int Main()
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
