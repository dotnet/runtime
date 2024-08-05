// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

// This takes the address of, gets the size of, or declares a pointer to a managed type
#pragma warning disable CS8500

public unsafe class Runtime_102577
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Problem(void* pDst, void* pSrc, object* pObj)
    {
        Unsafe.CopyBlock(pDst, pSrc, 1000);
        StructWithManyObjs x = default;
        if (pDst != null)
        {
            x.ObjFour = *pObj;
            *(StructWithManyObjs*)pDst = x;
        }
    }

    private struct StructWithManyObjs
    {
        public object ObjOne;
        public object ObjTwo;
        public object ObjThree;
        public object ObjFour;
        public object ObjFive;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        object x = null;
        byte* p = stackalloc byte[1000];
        Random.Shared.NextBytes(new Span<byte>(p, 1000));
        Problem(p, p, &x);
    }
}
