// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

public class Runtime_83387
{
    [MethodImpl(MethodImplOptions.NoOptimization)]
    [Fact]
    public static int TestEntryPoint()
    {
        (ushort A, ushort R) c = (1, 65535);
        Vector128<uint> v1 = Vector128.Create((uint)100);
        v1 = v1 * c.A;
        return (int)v1.ToScalar();
    }
}
