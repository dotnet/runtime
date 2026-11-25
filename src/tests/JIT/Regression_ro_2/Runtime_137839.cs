// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Xunit;
using TestLibrary;

namespace Runtime_137839;

public class Runtime_137839
{
    // Same as Runtime_122099, but [UnscopedRef] is on the property rather than on its getter.
    [ActiveIssue("https://github.com/dotnet/runtime/issues/122099", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoRuntime))]
    [Fact]
    public static int TestEntryPoint()
    {
        ref int r = ref EscapeRef();
        r = 42;
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
    private static ref int BoxedRefEscape(Struct1 x) => ref ((I1)x).Prop;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Clobber()
    {
        Span<int> spam = stackalloc int[64];
        for (int i = 0; i < spam.Length; i++)
        {
            spam[i] = unchecked((int)0xDEADBEEF);
        }
        if (spam[0] != unchecked((int)0xDEADBEEF))
        {
            throw new Exception();
        }
    }

    public interface I1
    {
        [UnscopedRef]
        ref int Prop { get; }
    }

    public struct Struct1 : I1
    {
        public int Field;

        [UnscopedRef]
        public ref int Prop
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get => ref Field;
        }
    }
}
