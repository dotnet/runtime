// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

namespace IntelHardwareIntrinsicTest.SSE2;

partial class Program
{
    [Fact]
    public static void AlignVector128()
    {
        s_f = default; // avoid helper in Foo below
        Console.WriteLine(Foo(default, default));
    }

    private static Vector128<int> s_f;
    // The JIT was picking a simple rsp-based frame for this function and then
    // believed the second vector is 16-byte aligned when it is not.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Foo(S24 a, Vector128<int> looksAligned)
    {
        s_f = looksAligned;
        return 0;
    }

    private struct S24
    {
        public long A, B, C;
    }
}
