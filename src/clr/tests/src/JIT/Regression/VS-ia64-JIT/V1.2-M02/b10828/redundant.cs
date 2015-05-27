// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
public class otherClass
{

    public class C
    {
        public static int w = foo(1);
        public static int x = foo(2);
        public static int y = foo(3);
        public static int z = foo(4);
    }

    public static int foo(int i) { if (i == 4) throw new System.Exception(); return i + 1; }
};

public class MyApp
{
    public static int Main()
    {
        int i = 2, j = 3, w;

        // not legal to hoist class init since there is a path
        // where class is not init.

        if (i == 2)
        {
            if (j == 3)
            {
                w = 0;
            }
            else
            {
                System.Console.WriteLine("initing class");
                w = otherClass.C.w;
            }
        }
        else
        {
            System.Console.WriteLine("initing class");
            w = otherClass.C.w + otherClass.C.w;
        }

        System.Console.WriteLine("w is {0}", w);
        try { otherClass.C.z = w; }
        catch (System.TypeInitializationException)
        {
            Console.WriteLine("System.TypeInitializationException caught as expected");
            Console.WriteLine("PASS");
            return 100;
        }
        return 1;
    }
}
