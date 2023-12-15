// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

public unsafe class ImageSharp_2117
{
    [Fact]
    public static int TestEntryPoint()
    {
        if (Sse.IsSupported)
        {
            Vector128<float> fnan = Vector128.Create(float.NaN);
            Vector128<float> res1 = Sse.Max(Sse.LoadVector128((float*)(&fnan)), Vector128<float>.Zero);
            Vector128<float> res2 = Sse.Min(Sse.LoadVector128((float*)(&fnan)), Vector128<float>.Zero);

            if (float.IsNaN(res1[0]) || float.IsNaN(res2[0]))
            {
                return 0;
            }
        }

        if (Sse2.IsSupported)
        {
            Vector128<double> dnan = Vector128.Create(double.NaN);
            Vector128<double> res3 = Sse2.Max(Sse2.LoadVector128((double*)(&dnan)), Vector128<double>.Zero);
            Vector128<double> res4 = Sse2.Min(Sse2.LoadVector128((double*)(&dnan)), Vector128<double>.Zero);

            if (double.IsNaN(res3[0]) || double.IsNaN(res4[0]))
            {
                return 0;
            }
        }

        return 100;
    }
}
