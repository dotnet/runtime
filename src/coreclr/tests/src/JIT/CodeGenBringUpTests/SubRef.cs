// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
        int result = SubRef(15, ref x);

        if (result == 2)
            return Pass;
        else
            return Fail;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)] 
    public static int SubRef(int x, ref int a)
    {
        x -= a;
        return x;
    }
    
}

