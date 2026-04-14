// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Test_bug603649_cs
{
public class foo
{
    private static object s_o = typeof(string);
    [Fact]
    [OuterLoop]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/122013", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot), nameof(TestLibrary.Utilities.Is32))]
    public static int TestEntryPoint()
    {
        bool f = typeof(string) == s_o as Type;
        Console.WriteLine(f);
        if (f) return 100; else return 101;
    }
}
}
