// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

public class Bug
{
    internal void Func(ref String str)
    {
        Console.WriteLine(str.ToString());
        str = "Abc";
    }

    internal void run()
    {
        String[] str = new String[10];
        str[0] = "DEF";
        Func(ref str[0]);
    }

    [Fact]
    public static int TestEntryPoint()
    {
        (new Bug()).run();
        Console.WriteLine("Passed");
        return 100;
    }
}
