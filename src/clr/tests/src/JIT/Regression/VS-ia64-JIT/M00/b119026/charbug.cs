// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

class test
{
    static sbyte si8;
    static char sc;
    static int Main()
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
