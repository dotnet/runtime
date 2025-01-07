// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Runtime.Intrinsics;
using Xunit;

public class Test27937
{
    static unsafe void calc(float* fa, float* fb)
    {
        float* pb = fb;
        float* eb = pb + 16;

        do
        {
            float* pa = fa;
            float* ea = pa + 16;
            var va = Vector128<float>.Zero;

            do
            {
                *pa = va.ToScalar();

                pa += Vector128<float>.Count;
                pb += Vector128<float>.Count;
            } while (pa < ea);

        } while (pb < eb);
    }

    [Fact]
    public static unsafe void TestEntryPoint()
    {
        float* a = stackalloc float[16];
        float* b = stackalloc float[16];

        calc(a, b);
    }
}
