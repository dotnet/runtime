// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

// Here we are testing that SSA and liveness agree on whether a dead
// partial store constitues a use (it does, in our model).

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

public class PartialDefLiveness
{
    [Fact]
    public static int TestEntryPoint()
    {
        // Just making sure we'll not hit any asserts in SSA.
        Problem();
        return 100;
    }

    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Problem()
    {
        Unsafe.SkipInit(out EnormousStruct a);
        // We expect liveness to fail to remove this dead store.
        a.Field = 1;
    }

    [StructLayout(LayoutKind.Explicit, Size = ushort.MaxValue + 1 + sizeof(int))]
    struct EnormousStruct
    {
        [FieldOffset(ushort.MaxValue + 1)]
        public int Field;
    }
}
