// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;
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

public class App
{
    [Fact]
    public static int TestEntryPoint()
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
