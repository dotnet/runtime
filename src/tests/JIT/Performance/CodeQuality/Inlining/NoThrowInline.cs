// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Inlining
{
public static class NoThrowInline
{
#if DEBUG
    public const int Iterations = 1;
#else
    public const int Iterations = 275000000;
#endif

    static void ThrowIfNull(string s)
    {
        if (s == null)
            ThrowArgumentNullException();
    }

    static void ThrowArgumentNullException()
    {
        throw new ArgumentNullException();
    }

    //
    // We expect ThrowArgumentNullException to not be inlined into Bench, the throw code is pretty
    // large and throws are extremly slow. However, we need to be careful not to degrade the
    // non-exception path performance by preserving registers across the call. For this the compiler
    // will have to understand that ThrowArgumentNullException never returns and omit the register
    // preservation code.
    //
    // For example, the Bench method below has 4 arguments (all passed in registers on x64) and fairly
    // typical argument validation code. If the compiler does not inline ThrowArgumentNullException
    // and does not make use of the "no return" information then all 4 register arguments will have
    // to be spilled and then reloaded. That would add 8 unnecessary memory accesses.
    //

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Bench(string a, string b, string c, string d)
    {
        ThrowIfNull(a);
        ThrowIfNull(b);
        ThrowIfNull(c);
        ThrowIfNull(d);

        return a.Length + b.Length + c.Length + d.Length;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        return (Bench("a", "bc", "def", "ghij") == 10) ? 100 : -1;
    }
}
}
