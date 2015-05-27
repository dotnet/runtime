// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
