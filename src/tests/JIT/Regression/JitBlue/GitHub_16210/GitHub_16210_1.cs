// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

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
    
    public unsafe float this[int index]
    {
        get
        {
            fixed (float* array = &x) { return array[index]; }
        }
        set
        {
            fixed (float* array = &x) { array[index] = value; }
        }
    }
}

public class X
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static float P(int i)
    {
        var test = new float4(5, 4, 3, 2);
        return test[i];
    }

    [Fact]
    public static int TestEntryPoint()
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
