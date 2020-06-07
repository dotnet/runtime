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
    public static float FPMul(float x, float y) { return x*y; }

    public static int Main()
    {
        float y = FPMul(7f, 9f);
        Console.WriteLine(y);
        if (System.Math.Abs(y-63f) <= Single.Epsilon) return Pass;
        else return Fail;
    }
}
