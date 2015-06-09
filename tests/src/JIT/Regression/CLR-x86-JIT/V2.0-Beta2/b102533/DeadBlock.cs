// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
public struct AA
{
    public static void f()
    {
        while (App.flag)
        {
            bool a = true;
            while (a)
            {
                if (a)
                    break;
                else
                {
                    if (a)
                    {
                    }
                }
            }
            a = false;
            do
            {
            }
            while (a);

            // stop the loop
            App.flag = false;
        }
    }

}

class App
{
    static int Main()
    {
        try
        {
            AA.f();
        }
        catch (Exception x)
        {
            Console.WriteLine("Exception handled: " + x.ToString());
        }
        Console.WriteLine("Passed.");
        return 100;
    }
    public static bool flag = true;
}
