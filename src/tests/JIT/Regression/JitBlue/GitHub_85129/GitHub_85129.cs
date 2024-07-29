// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using Xunit;

public class Program
{
    [Fact]
    public static int TestEntryPoint()
    {

        Vector256<int> v256Shuffle = Vector256.Create(100, 101, 102, 103, 104, 105, 106, 107);
        Vector256<int> v256ShuffleExpectedResult = Vector256.Create(107, 105, 0, 101, 106, 104, 0, 100);
        Vector256<int> v256ShuffleActualResult = Vector256Shuffle(v256Shuffle);
        if(v256ShuffleExpectedResult != v256ShuffleActualResult)
        {
            return 1;
        }

        Vector512<int> v512Shuffle = Vector512.Create(100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115);
        Vector512<int> v512ShuffleExpectedResult = Vector512.Create(115, 113, 111, 0, 107, 105, 103, 101, 114, 112, 110, 108, 0, 104, 102, 100);
        Vector512<int> v512ShuffleActualResult = Vector512Shuffle(v512Shuffle);
        if (v512ShuffleExpectedResult != v512ShuffleActualResult)
        {
            return 1;
        }
        return 100;
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Vector256<int> Vector256Shuffle(Vector256<int> v1)
    {
        return Vector256.Shuffle(v1, Vector256.Create(7, 5, 132, 1, 6, 4, -3, 0));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static  Vector512<int> Vector512Shuffle(Vector512<int> v1)
    {
        return Vector512.Shuffle(v1, Vector512.Create(15, 13, 11, 99, 7, 5, 3, 1, 14, 12, 10, 8, -11, 4, 2, 0));
    }
}
