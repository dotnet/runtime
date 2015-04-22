// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

class Test
{
    delegate object MyDeleg(string s);

    object f2(string s)
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