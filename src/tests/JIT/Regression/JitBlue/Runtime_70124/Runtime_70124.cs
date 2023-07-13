// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_70124
{
    [Fact]
    public static int TestEntryPoint()
    {
        return Problem(Vector2.One, Vector2.One) != new Vector2(3, 3) ? 101 : 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector2 Problem(Vector2 a, Vector2 b)
    {
        CallForVtor2(a + b);

        return (a + b) + a;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CallForVtor2(Vector2 vtor) { }
}
