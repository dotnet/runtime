// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

// Test that memory liveness does not miss a memory use (in the form of an OBJ/BLK).

using System;
using System.Runtime.Intrinsics;
using System.Runtime.CompilerServices;
using Xunit;

public unsafe class ObjBlkLiveness
{
    [Fact]
    public static int TestEntryPoint()
    {
        var a = Vector128<int>.Zero;
        return Problem(&a, 1) ? 100 : 101;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Problem(Vector128<int>* p, int b)
    {
        var a = 2 * *p;

        if (b is 1)
        {
            *p = Vector128<int>.AllBitsSet;
        }

        if (a + *p == Vector128<int>.Zero)
        {
            return false;
        }

        return true;
    }
}
