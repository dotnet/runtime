// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Runtime.CompilerServices;

public unsafe class Runtime_70824
{
    public static int Main()
    {
        long lng = 2;
        float flt = 3;
        Vector2 vtor = new Vector2(4, 5);
        float[] arr = new float[] { 6, 7 };

        if (ProblemWithTypes(&lng, &flt, vtor))
        {
            return 101;
        }
        if (ProblemWithAddrs(&flt, arr, vtor))
        {
            return 102;
        }

        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ProblemWithTypes(long* p1, float* pF, Vector2 vtor)
    {
        *pF = vtor.X;
        *p1 = (long)*pF;

        return *p1 != (long)*pF;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ProblemWithAddrs(float* pF, float[] arr, Vector2 vtor)
    {
        *pF = vtor.X;
        arr[0] = vtor.Y;

        arr[1] = vtor.X;
        *pF = vtor.Y;

        return arr[0] != vtor.Y || *pF != vtor.Y;
    }
}
