// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using Xunit;

public unsafe class Runtime_76096
{
    [Fact]
    public static int TestEntryPoint()
    {
        int result = 100;
        if (ProblemWithMemoryNumbering())
        {
            result++;
        }
        if (ProblemWithRefArithmetic())
        {
            result++;
        }

        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ProblemWithMemoryNumbering(byte zero = 0)
    {
        TwoFieldStruct a = new TwoFieldStruct { FieldOne = { Value = 1 }, FieldTwo = 2 };

        ref int fieldOneRef = ref a.FieldOne.Addr();
        var fieldOne = fieldOneRef;

        Unsafe.InitBlock(&a, zero, (uint)sizeof(TwoFieldStruct));

        return fieldOne + fieldOneRef != 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ProblemWithRefArithmetic()
    {
        TwoFieldStruct a = new TwoFieldStruct { FieldOne = { Value = 1 }, FieldTwo = 2 };

        ref int fieldOneRef = ref a.FieldOne.Addr();
        ref int fieldTwoRef = ref Unsafe.Add(ref fieldOneRef, 1);
        a.FieldTwo = 0;

        return fieldTwoRef != 0;
    }
}

struct OneFieldStruct
{
    public int Value;

    [UnscopedRef]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public ref int Addr() => ref Value;
}

struct TwoFieldStruct
{
    public OneFieldStruct FieldOne;
    public int FieldTwo;
}
