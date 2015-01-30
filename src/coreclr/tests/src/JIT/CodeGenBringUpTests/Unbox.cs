// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


using System;
using System.Runtime.CompilerServices;

public class BringUpTest
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    unsafe public static int Unbox(object o)
    {
        return (int)o;
    }

    public static int Main()
    {
        int r = 3;
        object o = r;
        int y = Unbox(o);
        if (y == 3) return Pass;
        else return Fail;
    }
}
