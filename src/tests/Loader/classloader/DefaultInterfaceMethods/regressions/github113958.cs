// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

// Github issue 113958, mainly a Mono issue having a generic
// interface including both private and virtual default methods.
// On Mono, private methods are not included in vtable,
// but code building the IMT slot, incorrectly counted private
// methods when calculating the vtable slot corresponding to a
// IMT slot. In this case, it ends up reading outside allocated
// memory, potentially causing a crash or an assert if read
// memory content ends up being a null pointer.

namespace GenericInterfaceDefaultVirutalMethodBug
{
    public class Program
    {
        [Fact]
        public static void TestEntryPoint()
        {
            var foo = new FooInt();
            string result = foo.Run();
            Assert.Equal("Method3", result);
        }
    }

    public interface IFoo<T>
    {
        private bool Method1() => false;
        private bool Method2() => false;
        string Method3() { return "Method3"; }
    }

    public class FooInt : IFoo<int>
    {
        public string Run()
        {
            return ((IFoo<int>)this).Method3();
        }
    }

}
