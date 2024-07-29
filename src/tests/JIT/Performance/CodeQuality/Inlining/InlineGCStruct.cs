// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// In CoreCLR String.Format(ref, ref) is small and readily inlined.
// The inline introduces a System.Parms GC struct local which is
// untracked and must be zero initialized in the prolog. When the
// inlined callsite is in a cold path, the inline hurts performance.
//
// There are two test methods below, one of which calls String.Format
// on a cold path and the other which has similar structure but
// does not call String.Format. Expectation is that they will have
// similar performance.
//
// See https://github.com/dotnet/runtime/issues/6796 for context.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Inlining
{
public class InlineGCStruct
{
#if DEBUG
    public const int Iterations = 1;
#else
    public const int Iterations = 350000000;
#endif

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int FastFunctionNotCallingStringFormat(int param)
    {
        if (param < 0)
        {
            throw new Exception(String.Format("We do not like the value {0:N0}.", param));
        }

        if (param == int.MaxValue)
        {
            throw new Exception(String.Format("{0:N0} is maxed out.", param));
        }

        if (param > int.MaxValue / 2)
        {
            throw new Exception(String.Format("We do not like the value {0:N0} either.", param));
        }

        return param * 2;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int FastFunctionNotHavingStringFormat(int param)
    {
        if (param < 0)
        {
            throw new ArgumentOutOfRangeException("param", "We do not like this value.");
        }

        if (param == int.MaxValue)
        {
            throw new ArgumentOutOfRangeException("param", "Maxed out.");
        }

        if (param > int.MaxValue / 2)
        {
            throw new ArgumentOutOfRangeException("param", "We do not like this value either.");
        }

        return param * 2;
    }

    public static bool WithoutFormatBase()
    {
        int result = 0;

        for (int i = 0; i < Iterations; i++)
        {
            result |= FastFunctionNotHavingStringFormat(11);
        }

        return (result == 22);
    }

    public static bool WithFormatBase()
    {
        int result = 0;

        for (int i = 0; i < Iterations; i++)
        {
            result |= FastFunctionNotCallingStringFormat(11);
        }

        return (result == 22);
    }

    [Fact]
    public static int TestEntryPoint()
    {
        bool withFormat = WithFormatBase();
        bool withoutFormat = WithoutFormatBase();

        return (withFormat && withoutFormat ? 100 : -1);
    }
}
}

