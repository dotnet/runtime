// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

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
    [Fact]
    public static int TestEntryPoint()
    {
        ulong ul = 0;
        Method1(ref ul, __arglist());
        return 100;
    }
}
