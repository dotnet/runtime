// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_64700
{
    private static StructWithVtors _structWithVtorsStatic;

    [Fact]
    public static int TestEntryPoint()
    {
        _structWithVtorsStatic = new StructWithVtors { StructWithOneVtor = { OneVtor = new Vector2(1, 0) } };

        if (ProblemWithCopyProp(0) != 0)
        {
            return 101;
        }

        if (ProblemWithLocalAssertionProp(new SmallerStruct[] { default }) != 1)
        {
            return 102;
        }

        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float ProblemWithCopyProp(float a)
    {
        ref var p1 = ref _structWithVtorsStatic.StructWithOneVtor.OneVtor;
        ref var p2 = ref _structWithVtorsStatic;

        if (_structWithVtorsStatic.StructWithOneVtor.OneVtor.X == 1)
        {
            p2.StructWithOneVtor.OneVtor = Vector2.Zero;
            if (_structWithVtorsStatic.StructWithOneVtor.OneVtor.X == 1)
            {
                a = 1;
            }
        }

        return a + p1.Y;
    }

    struct StructWithVtors
    {
        public StructWithOneVtor StructWithOneVtor;
    }

    struct StructWithOneVtor
    {
        public Vector2 OneVtor;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long ProblemWithLocalAssertionProp(SmallerStruct[] a)
    {
        ref var p1 = ref a[0];
        Use(ref p1);
        ref var p2 = ref p1.RegStruct;
        Use(ref p2);

        var t = p2.FirstLngValue;
        a[0].RegStruct.FirstLngValue = 1;

        return t + p2.FirstLngValue;
    }

    internal static void Use<T>(ref T arg) { }

    struct SmallerStruct
    {
        public RegStruct RegStruct;
    }

    struct RegStruct
    {
        public long FirstLngValue;
    }
}
