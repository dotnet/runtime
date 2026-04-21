// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Regression test: access checks triggered by the JIT for generic methods with
// constraints should not unnecessarily load types instantiated over type variables.
// The JIT uses the typical method definition (with formal type params) when doing
// security checks for generic callers, which was triggering constraint checks that
// loaded types like IList<TMethod> where TMethod is a type variable.

using System;
using System.Collections.Generic;
using Xunit;

public class TypeLoadWithGenericVars
{
    static void Method<TMethod1_1, TMethod1_2>() where TMethod1_1 : IList<TMethod1_2>
    {
        Method2<TMethod1_1, TMethod1_2>();
    }

    static void Method2<TMethod2_1, TMethod2_2>() where TMethod2_1 : IList<TMethod2_2>
    {
    }

    static void MethodA<T1, T2, T3>()
        where T1 : IList<T2>
        where T2 : IList<T3>
    {
        MethodB<T1, T2, T3>();
    }

    static void MethodB<U1, U2, U3>()
        where U1 : IList<U2>
        where U2 : IList<U3>
    {
    }

    [Fact]
    public static void TestEntryPoint()
    {
        // Call with concrete types that satisfy the constraints.
        // The JIT will use the typical method definition for access checks,
        // which previously caused unnecessary loading of IList<TMethod1_2>, etc.
        Method<List<int>, int>();
        Method<int[], int>();

        // Test with chained generic constraints
        MethodA<List<List<int>>, List<int>, int>();
    }
}
