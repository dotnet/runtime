// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
