// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Runtime.CompilerServices;

public unsafe class Runtime_69232
{
    public static int Main()
    {
        return Problem(new(1, 1, 1, 1)) ? 101 : 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Problem(Vector4 vtor)
    {
        FakeVtor a = GetFakeVtor(vtor);

        return a.FirstFlt != 1;
    }

    private static FakeVtor GetFakeVtor(Vector4 v) => *(FakeVtor*)&v;

    struct FakeVtor
    {
        public float FirstFlt;
        public float SecondFlt;
        public float ThirdFlt;
        public float FourthFlt;
    }
}
