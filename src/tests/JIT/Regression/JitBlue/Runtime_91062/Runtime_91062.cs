// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Numerics;
using Xunit;

public class Runtime_91062
{
    [Fact]
    public static void TestEntryPoint()
    {
        Foo(default, default);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector2 Foo(Vector128<float> v1, Vector128<float> v2)
    {
        return Vector2.Lerp(default, default, Vector128.Dot(v1, v2));
    }
}
