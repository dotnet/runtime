// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_91505
{
    [Fact]
    public static void TestEntryPoint()
    {
        Test();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe long Test()
    {
        delegate*<void> ptr = &Foo;
        return *(long*)ptr;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Foo() {}
}
