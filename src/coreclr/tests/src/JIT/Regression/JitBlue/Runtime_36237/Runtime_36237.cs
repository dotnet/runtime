// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

struct S0
{
    public bool F7;
}

class C1
{
    public S0 F7;
}

public class Runtime_36237
{
    static C1 s_8;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test()
    {
        uint[] vr0 = default(uint[]);
        if (s_8.F7.F7)
        {
            vr0 = vr0;
        }
    }

    public static int Main()
    {
        try
        {
            Test();
        }
        catch (System.NullReferenceException)
        {
            return 100;
        }
        return -1;
    }
}
