// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public static class Runtime_101175
{
    [Fact]
    public static void Test()
    {
        Activator.CreateInstance(typeof(Foo<>).MakeGenericType(typeof(object)));
    }

    class Foo<T>
    {
        public Foo()
        {
            if (new T[0].ToString() != "System.Object[]")
                throw new Exception();
        }
    }
}
