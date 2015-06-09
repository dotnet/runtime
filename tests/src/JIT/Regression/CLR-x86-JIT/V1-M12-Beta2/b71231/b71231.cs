// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

class X
{
    static short sh_8712 = 8712;
    static short sh_m973 = -973;
    static ushort us_8712 = 8712;
    static ushort us_973 = 973;

    public static int Main()
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
