// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

public class Runtime_87116
{
    [Fact]
    public static int Test()
    {
        return TryVectorAdd(1, 2, 1 + 2) ? 100 : 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool TryVectorAdd(float a, float b, float c)
    {
        Vector128<float> A = Vector128.Create(a);
        Vector128<float> B = Vector128.Create(b);

        Vector128<float> C = A + B;
        return C == Vector128.Create(c);
    }
}
