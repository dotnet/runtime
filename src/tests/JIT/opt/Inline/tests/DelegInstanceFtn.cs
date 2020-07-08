// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

internal class Test
{
    private delegate object MyDeleg(string s);

    private object f2(string s)
    {
        if (s == "test2")
            return 100;
        else
            return 1;
    }

    public static int Main()
    {
        Test t = new Test();
        MyDeleg d2 = new MyDeleg(t.f2);
        return Convert.ToInt32(d2("test2"));
    }
}
