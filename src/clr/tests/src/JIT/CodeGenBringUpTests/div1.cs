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
        int result = div1(12, 4);
        if (result == 3)
            return Pass;
        else
            return Fail;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int div1(int a, int b)
    {

        return a / b;
    }

}

