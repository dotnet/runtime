// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Repro case for a bug involving hoisting of static field loads out of
// loops and (illegally) above the corresponding type initializer calls.

using System.Runtime.CompilerServices;
using Xunit;

namespace N
{
    struct WrappedInt
    {
        public int Value;

        public static WrappedInt Twenty = new WrappedInt() { Value = 20 };
    }
    public static class C
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int unwrap(WrappedInt wi) => wi.Value;

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int foo(int s, int n)
        {
            for (int i = 0; i < n; ++i)
            {
                s += unwrap(WrappedInt.Twenty);  // Loading WrappedInt.Twenty must happen after calling the cctor
            }

            return s;
        }

        [Fact]
        public static int TestEntryPoint()
        {
            return foo(20, 4);
        }
    }
}
