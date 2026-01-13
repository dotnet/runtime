// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq.Expressions;
using Xunit;

namespace TestLibrary;

public static class Expect
{
    public static void ExpectEqual<T>(Expression<Func<T>> expr, T expected, ref bool fail)
    {
        var compiled = expr.Compile();
        T actual = compiled();

        // Get just the expression body text
        string exprText = expr.Body.ToString();

        if (!Equals(actual, expected))
        {
            Console.WriteLine($"{exprText} = {actual}, expected {expected}");
            fail = true;
        }
    }
}
