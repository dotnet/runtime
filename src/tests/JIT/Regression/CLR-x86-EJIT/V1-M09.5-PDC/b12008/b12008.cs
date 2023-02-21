// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;

public class Bug
{
    public void Func(ref String str)
    {
        Console.WriteLine(str.ToString());
        str = "Abc";
    }

    public void run()
    {
        String[] str = new String[10];
        str[0] = "DEF";
        Func(ref str[0]);
    }

    public static int Main()
    {
        (new Bug()).run();
        Console.WriteLine("Passed");
        return 100;
    }
}
