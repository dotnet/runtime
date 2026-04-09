// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;

public static class BasicTest
{
    public class MCJTestClass
    {
    }

    public struct MCJTestStruct
    {
    }

    public static int Main()
    {
        const int Pass = 100;

        ProfileOptimization.SetProfileRoot(Environment.CurrentDirectory);
        ProfileOptimization.StartProfile("profile.mcj");

        // Let multi-core JIT start jitting
        Thread.Sleep(100);

        MCJTestStruct s;
        PromoteToTier1(Foo, () => FooWithLoop(2), () => FooWithGeneric<int>(3),
                       () => FooWithGeneric<string>("MCJ"),
                       () => FooWithGeneric<MCJTestClass>(null),
                       () => FooWithGeneric<MCJTestStruct>(s),
                       () => FooWithGeneric<Regex>(null),
                       () => FooWithGeneric(RegexOptions.IgnoreCase));

        Foo();
        FooWithLoop(2);
        FooWithGeneric<int>(3);
        FooWithGeneric<string>("MCJ");
        FooWithGeneric<MCJTestClass>(null);
        FooWithGeneric<MCJTestStruct>(s);
        FooWithGeneric<Regex>(null);
        FooWithGeneric(RegexOptions.IgnoreCase);

        ProfileOptimization.StartProfile(null);

        return Pass;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Foo()
    {
        Foo2();
    }

    private static void Foo2()
    {
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int FooWithLoop(int n)
    {
        int sum = 0;
        for (int i = 0; i < n; ++i)
        {
            sum += i;
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void FooWithGeneric<T>(T n)
    {
        FooWithGeneric2();
    }

    private static void FooWithGeneric2()
    {
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void PromoteToTier1(params Action[] actions)
    {
        // Call the methods once to register a call each for call counting
        foreach (Action action in actions)
        {
            action();
        }

        // Allow time for call counting to begin
        Thread.Sleep(500);

        // Call the methods enough times to trigger tier 1 promotion
        for (int i = 0; i < 100; ++i)
        {
            foreach (Action action in actions)
            {
                action();
            }
        }

        // Allow time for the methods to be jitted at tier 1
        Thread.Sleep(500);
    }
}
