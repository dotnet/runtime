// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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