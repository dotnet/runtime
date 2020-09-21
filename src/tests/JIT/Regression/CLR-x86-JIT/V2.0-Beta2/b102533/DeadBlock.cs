// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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
