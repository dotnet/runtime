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

        int x = 13;
        int result = AndRef(15, ref x);

        if (result == 13)
            return Pass;
        else
            return Fail;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)] 
    public static int AndRef(int x, ref int a)
    {
        x &= a;
        return x;
    }
    
}

