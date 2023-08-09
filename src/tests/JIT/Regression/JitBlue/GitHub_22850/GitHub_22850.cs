// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

public static class GitHub_22850
{
    [Fact]
    public static int TestEntryPoint()
    {
        return test128((byte)90) ? 100 : -1;
    }

    static unsafe bool test128(int i)
    {
        Vector128<int> v = Vector128.Create(i);
        return MyEquals(ref v, Vector128.Create(i));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool MyEquals(ref Vector128<int> left, Vector128<int> right)
    {
        if (Sse2.IsSupported)
        {
            Vector128<byte> result = MyCompareEqual(left.AsByte(), right.AsByte());
            return Sse2.MoveMask(result) == 0b1111_1111_1111_1111; // We have one bit per element
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Vector128<byte> MyCompareEqual(this Vector128<byte> left, Vector128<byte> right)
    {
        return Sse2.CompareEqual(left, right);
    }
}


