// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
struct AA
{
    static void f()
    {
        bool flag = false;
        if (flag)
        {
            while (flag)
            {
                while (flag) { }
            }
        }
        do { } while (flag);
    }
    static int Main()
    {
        f();
        return 100;
    }
}
