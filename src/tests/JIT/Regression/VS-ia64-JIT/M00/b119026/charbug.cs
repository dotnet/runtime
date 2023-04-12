// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
public class test
{
    static sbyte si8;
    static char sc;
    [Fact]
    public static int TestEntryPoint()
    {
        sbyte i8 = -1;
        char c = (char)i8;
        System.Console.WriteLine("{0}: {1}", c, ((ushort)c));
        if (c == char.MaxValue)
            System.Console.WriteLine("Pass");
        else
            System.Console.WriteLine("Fail");
        si8 = -1;
        sc = (char)si8;
        System.Console.WriteLine("{0}: {1}", sc, ((ushort)sc));
        if (sc == char.MaxValue)
        {
            System.Console.WriteLine("Pass");
            return 100;
        }
        else
        {
            System.Console.WriteLine("Fail");
            return 1;
        }
    }
}
