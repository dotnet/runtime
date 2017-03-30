// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.integeregererface.integeregererface01.integeregererface01
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params in an interface</Title>
    // <Description>Simple Declaration of a method with optional parameters in an interface</Description>
    // <Expects status=success></Expects>
    // <Code>
    public interface Parent
    {
        int Foo(dynamic x = null, dynamic y = null);
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.integeregererface.integeregererface05.integeregererface05
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params in an interface</Title>
    // <Description>Simple Declaration of a method with optional parameters in an interface.
    //  Multiple optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public interface Parent
    {
        int Foo(dynamic z, int x = 2, dynamic y = default(dynamic));
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.integeregererface.integeregererface12.integeregererface12
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a method with optional parameters. cast of an int to long</Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public interface Parent
    {
        int Foo(
        [Optional]
        dynamic i);
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.integeregererface.integeregererface13.integeregererface13
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a method with optional parameters. cast of an int to long</Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public interface Parent
    {
        int Foo(
        [Optional]
        dynamic i, [Optional]
        dynamic j, [Optional]
        float ? f, [Optional]
        decimal ? d);
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.integeregererface.integeregererface16.integeregererface16
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a Explicitly implemented interface</Description>
    // <Expects status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(15,17\).*CS1066</Expects>
    public interface Parent
    {
        int Foo(dynamic i = default(dynamic));
    }

    public class Derived : Parent
    {
        int Parent.Foo(dynamic i = default(object))
        {
            return 0;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic p = new Derived();
            return ((Parent)p).Foo();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.decl.integeregererface.integeregererface17.integeregererface17
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Declaration of Optional Params</Title>
    // <Description>Simple Declaration of a Explicitly implemented interface</Description>
    // <Expects status=success></Expects>
    // <Code>
    public interface Parent
    {
        int Foo(dynamic i = null);
    }

    public class Derived : Parent
    {
        int Parent.Foo(dynamic i)
        {
            return 0;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic p = new Derived();
            return ((Parent)p).Foo();
        }
    }
    //</Code>
}
