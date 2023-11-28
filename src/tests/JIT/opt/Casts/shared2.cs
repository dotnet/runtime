// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

interface IBar {}
interface IFoo<T> {}
class C : IBar {}
class C<T> : IFoo<T> {}
struct S {}
struct SBar : IBar {}

// More tests for shared types passing through compareTypesForCast

public class X
{
    static int _errors;

    static void IsTrue(bool expression, [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
    {
        if (!expression)
        {
            Console.WriteLine($"{file}:L{line} test failed (expected: true).");
            _errors++;
        }
    }

    static void IsFalse(bool expression, [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
    {
        if (expression)
        {
            Console.WriteLine($"{file}:L{line} test failed (expected: false).");
            _errors++;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool A1<T>()
    {
        return typeof(IFoo<string>).IsAssignableFrom(typeof(T));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool A2<T>()
    {
        return typeof(IBar).IsAssignableFrom(typeof(T));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool I1<T>(T t)
    {
        return t is IFoo<string>;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool I2<T>(T t)
    {
        return t is IBar;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool C1<T>(T t)
    {
        return (t as IFoo<string>) != null;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool C2<T>(T t)
    {
        return (t as IBar) != null;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        var c = new C();
        var ci = new C<int>();
        var cs = new C<string>();
        var s = new S();
        var sb = new SBar();

        IsTrue(A1<IFoo<string>>());
        IsFalse(A1<IFoo<int>>());
        IsFalse(A1<IBar>());
        IsFalse(A1<S>());
        IsFalse(A1<SBar>());

        IsTrue(I1(cs));
        IsFalse(I1(ci));
        IsFalse(I1(c));
        IsFalse(I1(s));
        IsFalse(I1(sb));

        IsTrue(C1(cs));
        IsFalse(C1(ci));
        IsFalse(C1(c));
        IsFalse(C1(s));
        IsFalse(C1(sb));

        IsFalse(A2<IFoo<string>>());
        IsFalse(A2<IFoo<int>>());
        IsTrue(A2<IBar>());
        IsFalse(A2<S>());
        IsTrue(A2<SBar>());

        IsFalse(I2(cs));
        IsFalse(I2(ci));
        IsTrue(I2(c));
        IsFalse(I2(s));
        IsTrue(I2(sb));

        IsFalse(C2(cs));
        IsFalse(C2(ci));
        IsTrue(C2(c));
        IsFalse(C2(s));
        IsTrue(C2(sb));

        if (_errors == 0) Console.WriteLine("Passed");
        return _errors > 0 ? -1 : 100;
    }
}
