// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;

public class CC
{
    static void Method1(ref ulong param1, __arglist)
    {
        bool a = false;
        while (a)
        {
            do
            {
#pragma warning disable 1717
                param1 = param1;
#pragma warning restore 1717
                while (a) { }
            } while (a);
        }
    }
    static int Main()
    {
        ulong ul = 0;
        Method1(ref ul, __arglist());
        return 100;
    }
}
