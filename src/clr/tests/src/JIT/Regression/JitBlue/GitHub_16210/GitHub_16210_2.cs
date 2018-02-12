// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

 public struct float4
{
    public float4(float x, float y, float z, float w)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.w = w;
    }
    
    public float x;
    public float y;
    public float z;
    public float w;
}

class X
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static float E(ref float p, int i)
    {
        return Unsafe.Add(ref p, i);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static float P(int i)
    {
        var test = new float4(5, 4, 3, 2);
        return E(ref test.x, i);
    }

    static int Main(string[] args)
    {
        float v0 = P(0);
        float v1 = P(1);
        float v2 = P(2);
        float v3 = P(3);

        Console.WriteLine($"v0={v0} v1={v1} v2={v2} v3={v3}");
        bool ok = (v0 == 5 && v1 == 4 && v2 == 3 && v3 == 2);
        return (ok ? 100 : 0);
    }
}
