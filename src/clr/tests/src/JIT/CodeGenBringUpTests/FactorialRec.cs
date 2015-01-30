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
    public static int FactorialRec(int a)
    {
        Console.WriteLine(a);
        int result;
        if (a == 0)
            result = 1;
        else
        {
            result = a * FactorialRec(a - 1);
        }
        return result;
    }

    public static int Main()
    {
        int s = FactorialRec(5);
        if (s != 120) return Fail;
        return Pass;
    }
}
