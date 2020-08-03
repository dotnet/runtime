// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

internal class Test
{
    private delegate object MyDeleg(string s);

    private static object f1(string s)
    {
        if (s == "test1")
            return 100;
        else
            return 1;
    }

    public static int Main()
    {
        MyDeleg d1 = new MyDeleg(f1);
        return Convert.ToInt32(d1("test1"));
    }
}
