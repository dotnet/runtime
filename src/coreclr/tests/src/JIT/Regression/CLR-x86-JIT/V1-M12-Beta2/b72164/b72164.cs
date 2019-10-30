// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
internal unsafe class bug1
{
    public static void Func1(double* a01)
    {
        Console.WriteLine("The result should be 12");
        Console.WriteLine(*a01 + (*a01 - (*a01 + -5.0)));
    }

    public static int Main()
    {
        double* a01 = stackalloc double[1];
        *a01 = 7;
        Func1(a01);
        return 100;
    }
}
