// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

class Bug
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

    public static int Main(String[] args)
    {
        (new Bug()).run();
        Console.WriteLine("Passed");
        return 100;
    }
}
