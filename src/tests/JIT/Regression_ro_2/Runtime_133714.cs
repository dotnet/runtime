// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Runtime_133714;

public class Runtime_133714
{
    [Fact]
    public static void CatchObservesOriginalValue()
    {
        Assert.True(VerifyCatchObservesOriginalValue(null));
    }

    [ConditionalFact(typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNotMonoRuntime))]
    public static void FilterObservesOriginalValue()
    {
        Assert.True(VerifyFilterObservesOriginalValue(null));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static bool VerifyCatchObservesOriginalValue(Node? node)
    {
        S value = new() { A = 1, B = 2 };

        try
        {
            _ = node!.Get(ref value);
        }
        catch (NullReferenceException)
        {
            return (value.A == 1) && (value.B == 2);
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static bool VerifyFilterObservesOriginalValue(Node? node)
    {
        S value = new() { A = 1, B = 2 };

        try
        {
            throw new Exception();
        }
        catch (Exception) when (node!.Get(ref value) != 0)
        {
            return false;
        }
        catch (Exception)
        {
            return (value.A == 1) && (value.B == 2);
        }
    }

    private sealed class Node
    {
        private int _field = 9;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Get(ref S value)
        {
            value = default;
            return _field;
        }
    }

    private struct S
    {
        public int A;
        public int B;
    }
}
