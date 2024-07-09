// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public static class Runtime_100466
{
    [Fact]
    public static int TestBoxingDoesNotTriggerStaticTypeInitializers()
    {
        Foo foo = new Foo();
        ((object)foo).ToString();
        return s_cctorTriggered ? -1 : 100;
    }

    [Fact]
    public static int TestNullableBoxingDoesNotTriggerStaticTypeInitializers()
    {
        FooNullable? nullable = new FooNullable();
        ((object)nullable).ToString();
        return s_cctorTriggeredNullable ? -1 : 100;
    }

    private static bool s_cctorTriggered;
    private static bool s_cctorTriggeredNullable;

    private struct Foo
    {
        static Foo() => s_cctorTriggered = true;
    }

    private struct FooNullable
    {
        static FooNullable() => s_cctorTriggeredNullable = true;
    }
}
