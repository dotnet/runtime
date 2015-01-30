// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


using System;
using System.Runtime.CompilerServices;

class child
{
    static int Main()
    {
        int b = 5;
        const int Pass = 100;
        const int Fail = -1;

        int result = divref(12, ref b);
        if (result == 2)
            return Pass;
        else
            return Fail;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int divref(int a, ref int b)
    {
        return a / b;
    }
}

