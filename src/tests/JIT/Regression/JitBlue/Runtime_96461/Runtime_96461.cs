// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

public class Runtime_96461
{
    [Fact]
    public static int TestEntryPoint()
    {
        Vector128<int> result = Unsafe.BitCast<Vector128<int>, Vector128<int>>(V());
        return result[0];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<int> V() => Vector128.Create(100);
}
