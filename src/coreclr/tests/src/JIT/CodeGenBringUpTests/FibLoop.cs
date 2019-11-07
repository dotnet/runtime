// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//


using System;
using System.Runtime.CompilerServices;
public class BringUpTest
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int FibLoop(int x)
    {
        int curr = 0;
        int next = 1;

        for (int i = 0; i < x; i++)
        {
            int temp = curr + next;
            curr = next;
            next = temp;
        }
        return curr;
    }

    public static int Main()
    {
        int y = FibLoop(7);
        if (y == 13) return Pass;
        else return Fail;
    }
}
