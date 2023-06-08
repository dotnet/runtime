// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

public class X
{
    static short sh_8712 = 8712;
    static short sh_m973 = -973;
    static ushort us_8712 = 8712;
    static ushort us_973 = 973;

    [Fact]
    public static int TestEntryPoint()
    {
        short sh3 = (short)(sh_8712 * sh_m973);
        ushort us3 = (ushort)(us_8712 * us_973);

        Console.WriteLine("Shorts:");
        Console.WriteLine(sh_8712);
        Console.WriteLine(sh_m973);
        Console.WriteLine(sh3);

        Console.WriteLine("UShorts:");
        Console.WriteLine(us_8712);
        Console.WriteLine(us_973);
        Console.WriteLine(us3);
        return 100;
    }
}
