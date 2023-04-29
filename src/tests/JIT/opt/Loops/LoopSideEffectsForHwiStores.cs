// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics.Arm;
using Xunit;

public unsafe class LoopSideEffectsForHwiStores
{
    [Fact]
    public static int TestEntryPoint()
    {
        static bool VerifyExpectedVtor(Vector128<int> a) => a.Equals(Vector128.Create(4));

        var a = new ClassWithVtor();

        fixed (Vector128<int>* p = &a.VtorField)
        {
            if (Sse2.IsSupported && !VerifyExpectedVtor(ProblemWithSse2(a, (byte*)p)))
            {
                System.Console.WriteLine("ProblemWithSse2 failed!");
                return 101;
            }

            if (AdvSimd.IsSupported && !VerifyExpectedVtor(ProblemWithAdvSimd(a, (byte*)p)))
            {
                System.Console.WriteLine("ProblemWithAdvSimd failed!");
                return 101;
            }
        }

        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static unsafe Vector128<int> ProblemWithSse2(ClassWithVtor a, byte* p)
    {
        Vector128<int> vtor = Vector128<int>.Zero;

        a.VtorField = Vector128.Create(1);
        a.VtorField = Sse2.Add(a.VtorField, a.VtorField);

        for (int i = 0; i < 10; i++)
        {
            vtor = Sse2.Add(vtor, Sse2.Add(a.VtorField, a.VtorField));
            Sse2.Store(p, Vector128<byte>.Zero);
        }

        return vtor;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static unsafe Vector128<int> ProblemWithAdvSimd(ClassWithVtor a, byte* p)
    {
        Vector128<int> vtor = Vector128<int>.Zero;

        a.VtorField = Vector128.Create(1);
        a.VtorField = AdvSimd.Add(a.VtorField, a.VtorField);

        for (int i = 0; i < 10; i++)
        {
            vtor = AdvSimd.Add(vtor, AdvSimd.Add(a.VtorField, a.VtorField));
            AdvSimd.Store(p, Vector128<byte>.Zero);
        }

        return vtor;
    }

    class ClassWithVtor
    {
        public Vector128<int> VtorField;
    }
}
