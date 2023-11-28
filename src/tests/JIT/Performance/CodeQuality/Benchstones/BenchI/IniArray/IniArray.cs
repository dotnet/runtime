// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Benchstone.BenchI
{
public static class IniArray
{

#if DEBUG
    public const int Iterations = 1;
#else
    public const int Iterations = 10000000;
#endif

    const int Allotted = 16;
    static volatile object VolatileObject;

    static void Escape(object obj) {
        VolatileObject = obj;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Bench() {
        char[] workarea = new char[Allotted];
        for (int i = 0; i < Iterations; i++) {
            for (int j = 0; j < Allotted; j++) {
                workarea[j] = ' ';
            }
        }
        Escape(workarea);
        return true;
    }

    static bool TestBase() {
        bool result = Bench();
        return result;
    }

    [Fact]
    public static int TestEntryPoint() {
        bool result = TestBase();
        return (result ? 100 : -1);
    }
}
}
