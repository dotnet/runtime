// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using Xunit;

public class Program_85129
{
    public static int Main()
    {
        Vector256<int> v256Shuffle = Vector256.Create(100, 101, 102, 103, 104, 105, 106, 107);
        Vector256<int> v256ShuffleExpectedResult = Vector256.Create(107, 105, 0, 101, 106, 104, 0, 100);
        Vector256<int> v256ShuffleActualResult = Vector256Shuffle(v256Shuffle);
        if(v256ShuffleExpectedResult != v256ShuffleActualResult)
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
}
