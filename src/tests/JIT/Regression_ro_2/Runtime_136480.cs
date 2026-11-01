// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Runtime_136480;

using System;
using System.Runtime.CompilerServices;
using Xunit;

// The first access to a [ThreadStatic] field of a class that is not yet initialized may run
// its static constructor, which can modify the heap. A read after the access must observe that effect.
public static class Runtime_136480
{
    static int s_counter;

    class PreciseInt
    {
        [ThreadStatic] public static int T;
        static PreciseInt() { s_counter++; }
    }

    class PreciseRef
    {
        [ThreadStatic] public static object T;
        static PreciseRef() { s_counter++; }
    }

    class PreciseLoop
    {
        [ThreadStatic] public static int T;
        static PreciseLoop() { s_counter++; }
    }

    class BeforeFieldInitInt
    {
        [ThreadStatic] public static int T;
        public static int Init = ++s_counter;
    }

    class BeforeFieldInitRef
    {
        [ThreadStatic] public static object T;
        public static int Init = ++s_counter;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static (int, int, int) TestPreciseInt()
    {
        int before = s_counter;
        int t = PreciseInt.T;
        return (before, t, s_counter);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static (int, object, int) TestPreciseRef()
    {
        int before = s_counter;
        object t = PreciseRef.T;
        return (before, t, s_counter);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static (int, int, int) TestPreciseLoop()
    {
        int before = s_counter;
        int sum = 0;
        for (int i = 0; i < 3; i++)
        {
            sum += PreciseLoop.T;
        }
        return (before, sum, s_counter);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static (int, int, int) TestBeforeFieldInitInt()
    {
        int before = s_counter;
        int t = BeforeFieldInitInt.T;
        return (before, t, s_counter);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static (int, object, int) TestBeforeFieldInitRef()
    {
        int before = s_counter;
        object t = BeforeFieldInitRef.T;
        return (before, t, s_counter);
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/136592", TestRuntimes.Mono)]
    public static void TestEntryPoint()
    {
        // Each case runs its own class constructor exactly once, so the counter advances by one per case.
        Assert.Equal((0, 0, 1), TestPreciseInt());
        Assert.Equal((1, null, 2), TestPreciseRef());
        Assert.Equal((2, 0, 3), TestPreciseLoop());

        // beforefieldinit constructors may run before the method does, so only the final value is fixed.
        var (_, intValue, intCounter) = TestBeforeFieldInitInt();
        Assert.Equal(0, intValue);
        Assert.Equal(4, intCounter);

        var (_, refValue, refCounter) = TestBeforeFieldInitRef();
        Assert.Null(refValue);
        Assert.Equal(5, refCounter);
    }
}
