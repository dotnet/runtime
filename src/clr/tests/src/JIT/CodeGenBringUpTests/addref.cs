// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


using System;
using System.Runtime.CompilerServices;

class child
{
    static int Main()
    {
        const int Pass = 100;
        const int Fail = -1;

        int x = 1;
        int result = addref(1, ref x);

        if (result == 2)
            return Pass;
        else
            return Fail;

    }
    [MethodImplAttribute(MethodImplOptions.NoInlining)] 
    public static int addref(int x, ref int a)
    {
        x += a;
        return x;
    }
    
}

