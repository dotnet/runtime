// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public unsafe class Runtime_77773
{
    [Fact]
    public static int TestEntryPoint()
    {
        var s1 = new StructWithField { Field = 1 };
        var s2 = new StructWithField { Field = 2 };

        if (ProblemWithDirectUse(&s1, &s2, 0))
        {
            return 101;
        }
        if (ProblemWithCopyPropagation(&s1, &s2, 0))
        {
            return 102;
        }

        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ProblemWithDirectUse(StructWithField* pS1, StructWithField* pS2, nint idx)
    {
        return &pS1[idx].Field == &pS2[idx].Field;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ProblemWithCopyPropagation(StructWithField* pS1, StructWithField* pS2, nint idx)
    {
        int* pIdx = &pS1[idx].Field;
        JitUse(pIdx);
        *pIdx = Ind(&pS2[idx].Field);

        return pS1->Field != pS2->Field;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Ind(int* p) => *p;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void JitUse(int* p) { }

    struct StructWithField
    {
        public int Field;
    }
}
