// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Runtime.CompilerServices;

class MainApp
{


    // [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static void Foo_NoInline(string s)
    {
        Console.WriteLine(s);
        s = "New string";
        Console.WriteLine(s);
    }

    public static int Main()
    {
        try
        {
            string orig = "Original string";
            Console.WriteLine(orig);
            Foo_NoInline(orig);

            return 100;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return 666;
        }
    }

}


