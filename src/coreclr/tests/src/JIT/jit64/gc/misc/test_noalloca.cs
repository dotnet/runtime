// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

class Test
{
#pragma warning disable 0414
    static int x = 25;
#pragma warning restore 0414

    public static int Main()
    {
        String s1 = "a";
        String s2 = "b";
        String s3 = "c";
        String s4 = "d";
        String s5 = "e";
        String s6 = "f";
        String s7 = "g";
        String s8 = "h";
        String s9 = "i";
        String s10 = "j";

        foo(s1, s2, s3, s4, s5, s6, s7, s8, s9, s10);

        //		int * px = stackalloc int[x];

        foo(s1, s2, s3, s4, s5, s6, s7, s8, s9, s10);
        return (100);
    }

    public static void foo(String s1, String s2, String s3, String s4, String s5, String s6, String s7, String s8, String s9, String s10)
    {
        Console.WriteLine(s10);
    }
}
