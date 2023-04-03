// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

public unsafe class Runtime_73821
{
    public struct S
    {
        public int F;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static S Test1(int val)
    {
        S s;
        int size = sizeof(S);
        Unsafe.CopyBlockUnaligned(&s, &val, (uint)size);
        return s;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Test2(int val)
    {
        int val2;
        int size = sizeof(int);
        Unsafe.CopyBlockUnaligned(&val2, &val, (uint)size);
        return val2;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        return Test1(33).F + Test2(67);
    }
}
