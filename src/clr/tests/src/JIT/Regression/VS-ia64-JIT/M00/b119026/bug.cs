// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

class test
{
    static short si16;
    static uint su32;
    static int Main()
    {
        si16 = -1;
        su32 = (uint)si16;
        System.Console.WriteLine(su32);
        if (su32 == uint.MaxValue)
            System.Console.WriteLine("Pass");
        else
            System.Console.WriteLine("Fail");
        short i16 = -1;
        uint u32 = (uint)i16;
        System.Console.WriteLine(u32);
        if (u32 == uint.MaxValue)
        {
            System.Console.WriteLine("Pass");
            return 100;
        }
        else
        {
            System.Console.WriteLine("Fail");
            return 100;
        }
    }
}
