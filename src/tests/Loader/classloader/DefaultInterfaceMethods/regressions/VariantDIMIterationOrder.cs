// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

namespace VariantDIMIterationOrder
{
    interface IFoo<out T>
    {
        Type Func() { return typeof(T); }
    }

    interface IMoreSpecific<out T> : IFoo<T>
    {
        Type IFoo<T>.Func() { return typeof(List<T>); }
    }

    class Base : IMoreSpecific<Base> { }
    class Derived : Base, IMoreSpecific<Derived> { }

    public class Tests
    {
        [Fact]
        public static void TestEntryPoint()
        {
            Type result = ((IFoo<object>)(new Derived())).Func();
            Assert.Equal(typeof(List<Derived>), result);
        }
    }
}
