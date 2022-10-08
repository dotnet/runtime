// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

public class Runtime_65694_2
{
    public static int Main()
    {
        var a = new StructWithObj { Obj = new object() };
        var c = new StructWithObj { Obj = new object() };

        return Problem(a, c).Obj == c.Obj ? 100 : 101;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static StructWithObj Problem(StructWithObj a, StructWithObj c)
    {
        StructWithObj b = a;

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void GcSafePoint() { GC.Collect(); }

        GcSafePoint();
        GcSafePoint();

        if (a.Obj == b.Obj)
        {
            b = c;
        }

        return b;
    }

    struct StructWithObj
    {
        public object Obj;
    }
}

