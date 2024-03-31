// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

public static class Runtime_100466
{
    [Fact]
    public static int Test()
    {
        var foo = new Foo();
        return (Bar.X == 0) ? 100 : -1;
    }

    struct Foo
    {
        static Foo()
        {
            Bar.Set();
        }
    }

    struct Bar
    {
        public static int X;
        public static void Set()
        {
            X = 42;
        }
    }
}
