// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Xunit;
using TestLibrary;

namespace Runtime_122099;

public class Runtime_122099
{
    // Verifies that an interface call on a boxed value type whose target returns an
    // `[UnscopedRef]` ref to an instance field keeps the boxed receiver alive long
    // enough for the returned ref to remain valid after the call returns.
    [ActiveIssue("https://github.com/dotnet/runtime/issues/122099", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoRuntime))]
    [Fact]
    public static int TestEntryPoint()
    {
        ref int r = ref EscapeRef();
        r = 42;

        // Reuse the same stack region the callees occupied.
        Clobber();

        return r == 42 ? 100 : 101;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static ref int EscapeRef()
    {
        Struct1 s = new Struct1 { Field = 7 };
        return ref BoxedRefEscape(s);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static ref int BoxedRefEscape(Struct1 x)
    {
        return ref ((I1)x).Method1();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Clobber()
    {
        Span<int> spam = stackalloc int[64];
        for (int i = 0; i < spam.Length; i++)
        {
            spam[i] = unchecked((int)0xDEADBEEF);
        }
        // Prevent the stackalloc from being elided.
        if (spam[0] != unchecked((int)0xDEADBEEF))
        {
            throw new Exception();
        }
    }
}

public interface I1
{
    [UnscopedRef]
    ref int Method1();
}

public struct Struct1 : I1
{
    public int Field;

    [UnscopedRef]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public ref int Method1() => ref Field;
}

