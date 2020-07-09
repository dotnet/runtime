// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;

public class BringUpTest
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static int ArrayExc()
    {
        int[] a = {1, 2, 3, 4};
        return a[5];
    }

    static int Main()
    {
        try
        {
            ArrayExc();
            return Fail;
        }
        catch (System.IndexOutOfRangeException)
        {
            // OK
        }
        return Pass;
    }
}
