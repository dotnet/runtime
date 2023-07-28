// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_74126
{
    [Fact]
    public static int TestEntryPoint()
    {
        if (GetVtor(GetVtor2()) != GetVtor2())
        {
            return 101;
        }
        if (GetVtor(GetVtor3()) != GetVtor3())
        {
            return 102;
        }
        if (GetVtor(GetVtor4()) != GetVtor4())
        {
            return 103;
        }
        if (GetVtor(GetVtor64()) != GetVtor64())
        {
            return 104;
        }
        if (GetVtor(GetVtor128()) != GetVtor128())
        {
            return 105;
        }
        if (GetVtor(GetVtor256()) != GetVtor256())
        {
            return 106;
        }

        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector2 GetVtor2()
    {
        return new Vector2(1, 2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector3 GetVtor3()
    {
        return new Vector3(1, 2, 3);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector4 GetVtor4()
    {
        return new Vector4(1, 2, 3, 4);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector64<int> GetVtor64()
    {
        return Vector64.Create(1, 2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<int> GetVtor128()
    {
        return Vector128.Create(1, 2, 3, 4);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector256<int> GetVtor256()
    {
        return Vector256.Create(1, 2, 3, 4, 5, 6, 7, 8);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static T GetVtor<T>(T vtor)
    {
        return vtor;
    }
}
