// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.base01.base01
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int j = 1;
        public Parent(int x = 0)
        {
            j = x;
        }
    }

    public class Derived : Parent
    {
        public Derived(int y = 0) : base(x: 0)
        {
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
            return p.j;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.base01a.base01a
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int j = 1;
        public Parent(dynamic x = null)
        {
            j = x;
        }
    }

    public class Derived : Parent
    {
        public Derived(dynamic y = null) : base(x: 0)
        {
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
            Derived p = new Derived();
            return p.j;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.base02.base02
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int j = 1;
        public Parent(int x = 0)
        {
            j = x;
        }
    }

    public class Derived : Parent
    {
        public Derived(int y = 0) : base()
        {
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
            return p.j;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.base02a.base02a
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int j = 1;
        public Parent(dynamic x = null)
        {
            j = x ?? 0;
        }
    }

    public class Derived : Parent
    {
        public Derived(dynamic y = null) : base()
        {
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
            Derived p = new Derived();
            return p.j;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.base03.base03
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int j = 1;
        public Parent(int x, int y = 0)
        {
            j = y;
        }
    }

    public class Derived : Parent
    {
        public Derived(int y = 0) : base(1, y: 0)
        {
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
            return p.j;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.base03a.base03a
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int j = 1;
        public Parent(dynamic x, dynamic y = null)
        {
            j = y;
        }
    }

    public class Derived : Parent
    {
        public Derived(dynamic y = null) : base(1, y: 0)
        {
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
            Derived p = new Derived();
            return p.j;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.indexer01.indexer01
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Named params on an indexer</Title>
    // <Description>Simple Declaration of an indexer with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(9,18\).*CS1066</Expects>
    public class Parent
    {
        public int this[int index = 1]
        {
            get
            {
                return 0;
            }

            set
            {
            }
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
            dynamic p = new Parent();
            return p[index: 1];
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.indexer01a.indexer01a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Named params on an indexer</Title>
    // <Description>Simple Declaration of an indexer with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(9,18\).*CS1066</Expects>
    public class Parent
    {
        public int this[dynamic index = null]
        {
            get
            {
                return 0;
            }

            set
            {
            }
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
            Parent p = new Parent();
            return p[index: 1];
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.indexer01b.indexer01b
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Named params on an indexer</Title>
    // <Description>Simple Declaration of an indexer with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(9,18\).*CS1066</Expects>
    public class Parent
    {
        public int this[dynamic index = null]
        {
            get
            {
                return 0;
            }

            set
            {
            }
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
            dynamic p = new Parent();
            return p[index: 1];
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.indexer01c.indexer01c
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Named params on an indexer</Title>
    // <Description>Simple Declaration of an indexer with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(9,18\).*CS1066</Expects>
    public class Parent
    {
        public int this[int index = 1]
        {
            get
            {
                return 0;
            }

            set
            {
            }
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
            Parent p = new Parent();
            dynamic d = 1;
            return p[index: d];
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.indexer02.indexer02
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Named params on an indexer</Title>
    // <Description>Simple Declaration of an indexer with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int this[int index = 1, bool b = true]
        {
            get
            {
                return 0;
            }

            set
            {
            }
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
            dynamic p = new Parent();
            return p[index: 1];
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.indexer02a.indexer02a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Named params on an indexer</Title>
    // <Description>Simple Declaration of an indexer with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int this[dynamic index = null, bool b = true]
        {
            get
            {
                return 0;
            }

            set
            {
            }
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
            Parent p = new Parent();
            return p[index: 1];
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.indexer02b.indexer02b
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Named params on an indexer</Title>
    // <Description>Simple Declaration of an indexer with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int this[dynamic index = null, bool b = true]
        {
            get
            {
                return 0;
            }

            set
            {
            }
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
            dynamic p = new Parent();
            return p[index: 1];
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.indexer02c.indexer02c
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Named params on an indexer</Title>
    // <Description>Simple Declaration of an indexer with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int this[int index = 1, bool b = true]
        {
            get
            {
                return 0;
            }

            set
            {
            }
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
            Parent p = new Parent();
            dynamic d = 1;
            return p[index: d];
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.indexer03.indexer03
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Named params on an indexer</Title>
    // <Description>Simple Declaration of an indexer with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int this[int index = 1, bool b = true]
        {
            get
            {
                if (b == false)
                    return 0;
                return 1;
            }

            set
            {
            }
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
            dynamic p = new Parent();
            return p[b: false, index: 1];
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.indexer03a.indexer03a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Named params on an indexer</Title>
    // <Description>Simple Declaration of an indexer with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int this[dynamic index = null, dynamic b = null]
        {
            get
            {
                if (b == false)
                    return 0;
                return 1;
            }

            set
            {
            }
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
            Parent p = new Parent();
            return p[b: false, index: 1];
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.indexer03b.indexer03b
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Named params on an indexer</Title>
    // <Description>Simple Declaration of an indexer with optional parameters</Description>
    // <Expects status=success></Expects
    // <Code>
    public class Parent
    {
        public int this[dynamic index = null, dynamic b = null]
        {
            get
            {
                if (b == false)
                    return 0;
                return 1;
            }

            set
            {
            }
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
            dynamic p = new Parent();
            return p[b: false, index: 1];
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.indexer03c.indexer03c
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Named params on an indexer</Title>
    // <Description>Simple Declaration of an indexer with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int this[int index = 1, bool b = true]
        {
            get
            {
                if (b == false)
                    return 0;
                return 1;
            }

            set
            {
            }
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
            Parent p = new Parent();
            dynamic b = false;
            dynamic d = 1;
            return p[b: b, index: d];
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.indexer09.indexer09
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Named params on an indexer</Title>
    // <Description>Simple Declaration of an indexer with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int this[int index = 1, bool b = true]
        {
            get
            {
                if (b == true)
                    return 0;
                return 1;
            }

            set
            {
            }
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
            dynamic p = new Parent();
            p[1] = 1;
            return p[1];
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.indexer09a.indexer09a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Named params on an indexer</Title>
    // <Description>Simple Declaration of an indexer with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int this[dynamic index = null, bool b = true]
        {
            get
            {
                if (b == true)
                    return 0;
                return 1;
            }

            set
            {
            }
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
            Parent p = new Parent();
            p[1] = 1;
            return p[1];
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.indexer09b.indexer09b
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Named params on an indexer</Title>
    // <Description>Simple Declaration of an indexer with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int this[dynamic index = null, bool b = true]
        {
            get
            {
                if (b == true)
                    return 0;
                return 1;
            }

            set
            {
            }
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
            dynamic p = new Parent();
            p[1] = 1;
            return p[1];
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.indexer09c.indexer09c
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Named params on an indexer</Title>
    // <Description>Simple Declaration of an indexer with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int this[int index = 1, bool b = true]
        {
            get
            {
                if (b == true)
                    return 0;
                return 1;
            }

            set
            {
            }
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
            Parent p = new Parent();
            dynamic d = 1;
            p[d] = d;
            return p[d];
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.indexer10.indexer10
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Named params on an indexer</Title>
    // <Description>Simple Declaration of an indexer with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int this[int index = 1, bool b = true]
        {
            get
            {
                if (b == false)
                    return 0;
                return 1;
            }

            set
            {
            }
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
            dynamic p = new Parent();
            p[index: 1] = 1;
            return p[b: false];
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.indexer10a.indexer10a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Named params on an indexer</Title>
    // <Description>Simple Declaration of an indexer with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int this[int index = 1, dynamic b = null]
        {
            get
            {
                if (b == false)
                    return 0;
                return 1;
            }

            set
            {
            }
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
            Parent p = new Parent();
            p[index: 1] = 1;
            return p[b: false];
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.indexer10b.indexer10b
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Named params on an indexer</Title>
    // <Description>Simple Declaration of an indexer with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int this[int index = 1, dynamic b = null]
        {
            get
            {
                if (b == false)
                    return 0;
                return 1;
            }

            set
            {
            }
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
            dynamic p = new Parent();
            p[index: 1] = 1;
            return p[b: false];
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.indexer10c.indexer10c
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Named params on an indexer</Title>
    // <Description>Simple Declaration of an indexer with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int this[int index = 1, bool b = true]
        {
            get
            {
                if (b == false)
                    return 0;
                return 1;
            }

            set
            {
            }
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
            Parent p = new Parent();
            dynamic d = 1;
            p[index: d] = d;
            dynamic b = false;
            return p[b: b];
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.indexer11.indexer11
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Named params on an indexer</Title>
    // <Description>Simple Declaration of an indexer with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int this[int index = 1, bool b = true]
        {
            get
            {
                if (b == true)
                    return 0;
                return 1;
            }

            set
            {
            }
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
            dynamic p = new Parent();
            p[index: 1, b: false] = 1;
            return p[1];
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.indexer11a.indexer11a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Named params on an indexer</Title>
    // <Description>Simple Declaration of an indexer with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int this[dynamic index = null, bool b = true]
        {
            get
            {
                if (b == true)
                    return 0;
                return 1;
            }

            set
            {
            }
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
            Parent p = new Parent();
            p[index: 1, b: false] = 1;
            return p[1];
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.indexer11b.indexer11b
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Named params on an indexer</Title>
    // <Description>Simple Declaration of an indexer with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int this[dynamic index = default(dynamic), bool b = true]
        {
            get
            {
                if (b == true)
                    return 0;
                return 1;
            }

            set
            {
            }
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
            dynamic p = new Parent();
            p[index: 1, b: false] = 1;
            return p[1];
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.indexer11c.indexer11c
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Named params on an indexer</Title>
    // <Description>Simple Declaration of an indexer with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int this[int index = 1, bool b = true]
        {
            get
            {
                if (b == true)
                    return 0;
                return 1;
            }

            set
            {
            }
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
            Parent p = new Parent();
            dynamic d = 1;
            dynamic b = false;
            p[index: d, b: b] = 1;
            return p[d];
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.indexer12.indexer12
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Named params on an indexer</Title>
    // <Description>Simple Declaration of an indexer with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int this[int index = 1, bool b = true]
        {
            get
            {
                if (b == true)
                    return 0;
                return 1;
            }

            set
            {
            }
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
            dynamic p = new Parent();
            p[index: 1, b: false] = 1;
            return p[index: 1];
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.indexer12a.indexer12a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Named params on an indexer</Title>
    // <Description>Simple Declaration of an indexer with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int this[int index = 1, dynamic b = null]
        {
            get
            {
                if (b == null)
                    return 0;
                return 1;
            }

            set
            {
            }
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
            Parent p = new Parent();
            p[index: 1, b: false] = 1;
            return p[index: 1];
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.indexer12b.indexer12b
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Named params on an indexer</Title>
    // <Description>Simple Declaration of an indexer with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int this[int index = 1, dynamic b = null]
        {
            get
            {
                if (b == null)
                    return 0;
                return 1;
            }

            set
            {
            }
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
            dynamic p = new Parent();
            p[index: 1, b: false] = 1;
            return p[index: 1];
        }
    }
    //</Code>
}


//
//namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.indexer12c.indexer12c
//{
//    using ManagedTests.DynamicCSharp.Test;
//    using ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.indexer12c.indexer12c;
//    // <Area>Declaration of Methods with Optional Parameters</Area>
//    // <Title>Named params on an indexer</Title>
//    // <Description>Simple Declaration of an indexer with optional parameters</Description>
//    // <Expects status=success></Expects>
//    // <Code>
//    public class Parent
//    {
//        public int this[int index = 1, bool b = true] { get { if (b == true) return 0; return 1; } set { } }
//    }
//    [TestClass]
//    public class Test
//    {
//        [Test]
//        [Priority(Priority.Priority2)]
//        public void DynamicCSharpRunTest() { Assert.AreEqual(0, MainMethod()); }
//        public static int MainMethod()
//        {
//            Parent p = new Parent();
//            dynamic d = 1;
//            dynamic b = false;
//            p[index: d, b: b] = 1;
//            return p[index: d];
//        }
//    }
//    //</Code>
//}
//
//
//namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.indexer13.indexer13
//{
//    using ManagedTests.DynamicCSharp.Test;
//    using ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.indexer13.indexer13;
//    // <Area>Declaration of Methods with Optional Parameters</Area>
//    // <Title>Named params on an indexer</Title>
//    // <Description>Simple Declaration of an indexer with optional parameters</Description>
//    // <Expects status=success></Expects>
//    // <Code>
//    public class Parent
//    {
//        public int this[int index = 1, bool b = true] { get { if (index == 0) return 0; return 1; } set { } }
//    }
//    [TestClass]
//    public class Test
//    {
//        [Test]
//        [Priority(Priority.Priority2)]
//        public void DynamicCSharpRunTest() { Assert.AreEqual(0, MainMethod()); }
//        public static int MainMethod()
//        {
//            dynamic p = new Parent();
//            p[index: 1] = p[0];
//            return p[0];
//        }
//    }
//    //</Code>
//}
//
//
//namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.indexer13a.indexer13a
//{
//    using ManagedTests.DynamicCSharp.Test;
//    using ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.indexer13a.indexer13a;
//    // <Area>Declaration of Methods with Optional Parameters</Area>
//    // <Title>Named params on an indexer</Title>
//    // <Description>Simple Declaration of an indexer with optional parameters</Description>
//    // <Expects status=success></Expects>
//    // <Code>
//    public class Parent
//    {
//        public int this[int index = 1, dynamic b = null] { get { if (index == 0) return 0; return 1; } set { } }
//    }
//    [TestClass]
//    public class Test
//    {
//        [Test]
//        [Priority(Priority.Priority2)]
//        public void DynamicCSharpRunTest() { Assert.AreEqual(0, MainMethod()); }
//        public static int MainMethod()
//        {
//            Parent p = new Parent();
//            p[index: 1] = p[0];
//            return p[0];
//        }
//    }
//    //</Code>
//}
//
//
//namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.indexer13b.indexer13b
//{
//    using ManagedTests.DynamicCSharp.Test;
//    using ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.indexer13b.indexer13b;
//    // <Area>Declaration of Methods with Optional Parameters</Area>
//    // <Title>Named params on an indexer</Title>
//    // <Description>Simple Declaration of an indexer with optional parameters</Description>
//    // <Expects status=success></Expects>
//    // <Code>
//    public class Parent
//    {
//        public int this[int index = 1, dynamic b = null] { get { if (index == 0) return 0; return 1; } set { } }
//    }
//    [TestClass]
//    public class Test
//    {
//        [Test]
//        [Priority(Priority.Priority2)]
//        public void DynamicCSharpRunTest() { Assert.AreEqual(0, MainMethod()); }
//        public static int MainMethod()
//        {
//            dynamic p = new Parent();
//            p[index: 1] = p[0];
//            return p[0];
//        }
//    }
//    //</Code>
//}
//
//
//namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.indexer13c.indexer13c
//{
//    using ManagedTests.DynamicCSharp.Test;
//    using ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.indexer13c.indexer13c;
//    // <Area>Declaration of Methods with Optional Parameters</Area>
//    // <Title>Named params on an indexer</Title>
//    // <Description>Simple Declaration of an indexer with optional parameters</Description>
//    // <Expects status=success></Expects>
//    // <Code>
//    public class Parent
//    {
//        public int this[int index = 1, bool b = true] { get { if (index == 0) return 0; return 1; } set { } }
//    }
//    [TestClass]
//    public class Test
//    {
//        [Test]
//        [Priority(Priority.Priority2)]
//        public void DynamicCSharpRunTest() { Assert.AreEqual(0, MainMethod()); }
//        public static int MainMethod()
//        {
//            Parent p = new Parent();
//            dynamic d = 1;
//            p[index: d] = p[0];
//            dynamic d1 = 0;
//            return p[d1];
//        }
//    }
//    //</Code>
//}
//

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.indexer14.indexer14
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Named params on an indexer</Title>
    // <Description>Simple Declaration of an indexer with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int this[int i = 1, double d = 1.0d]
        {
            get
            {
                if (d == 1.0)
                    return 0;
                return 1;
            }

            set
            {
            }
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
            dynamic p = new Parent();
            p[d: 1] = 1;
            return p[d: 1.0];
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.indexer14b.indexer14b
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Named params on an indexer</Title>
    // <Description>Simple Declaration of an indexer with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int this[int i = 1, double d = 1.0d]
        {
            get
            {
                if (d == 1.0)
                    return 0;
                return 1;
            }

            set
            {
            }
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
            dynamic p = new Parent();
            p[d: 1] = 1;
            return p[1];
        }
    }
    //</Code>
}


//
//namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.indexer15.indexer15
//{
//    using ManagedTests.DynamicCSharp.Test;
//    using ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.indexer15.indexer15;
//    // <Area>Declaration of Methods with Optional Parameters</Area>
//    // <Title>Named params on an indexer</Title>
//    // <Description>Simple Declaration of an indexer with optional parameters</Description>
//    // <Expects status=success></Expects>
//    // <Code>
//    public class Parent
//    {
//        public int this[int i = 1, decimal d = 1.0m] { get { if (d == 1.2m) return 0; return 1; } set { } }
//    }
//    [TestClass]
//    public class Test
//    {
//        [Test]
//        [Priority(Priority.Priority2)]
//        public void DynamicCSharpRunTest() { Assert.AreEqual(0, MainMethod()); }
//        public static int MainMethod()
//        {
//            dynamic p = new Parent();
//            p[d: 1] = 1;
//            return p[d: 1.2m];
//        }
//    }
//    //</Code>
//}
//
//
//namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.indexer15b.indexer15b
//{
//    using ManagedTests.DynamicCSharp.Test;
//    using ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.indexer15b.indexer15b;
//    // <Area>Declaration of Methods with Optional Parameters</Area>
//    // <Title>Named params on an indexer</Title>
//    // <Description>Simple Declaration of an indexer with optional parameters</Description>
//    // <Expects status=success></Expects>
//    // <Code>
//    public class Parent
//    {
//        public int this[int i = 1, decimal d = 1.2m] { get { if (d == 1.2m) return 0; return 1; } set { } }
//    }
//    [TestClass]
//    public class Test
//    {
//        [Test]
//        [Priority(Priority.Priority2)]
//        public void DynamicCSharpRunTest() { Assert.AreEqual(0, MainMethod()); }
//        public static int MainMethod()
//        {
//            dynamic p = new Parent();
//            p[d: 1] = 1;
//            return p[1];
//        }
//    }
//    //</Code>
//}
//

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.indexer16.indexer16
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Named params on an indexer</Title>
    // <Description>Simple Declaration of an indexer with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int this[int i = 1, bool? d = false]
        {
            get
            {
                if (d == true)
                    return 0;
                return 1;
            }

            set
            {
            }
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
            dynamic p = new Parent();
            p[d: false] = 1;
            return p[d: true];
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.indexer16b.indexer16b
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Named params on an indexer</Title>
    // <Description>Simple Declaration of an indexer with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int this[int i = 1, bool? d = false]
        {
            get
            {
                if (d == false)
                    return 0;
                return 1;
            }

            set
            {
            }
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
            dynamic p = new Parent();
            p[d: false] = 1;
            return p[1];
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.indexer17.indexer17
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Named params on an indexer</Title>
    // <Description>Simple Declaration of an indexer with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public dynamic this[int i = 1, bool b = true]
        {
            get
            {
                if (b == true)
                    return 0;
                return 1;
            }

            set
            {
            }
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
            Parent p = new Parent();
            p[b: false] = 1;
            return p[1];
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.indexer18.indexer18
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Named params on an indexer</Title>
    // <Description>Simple Declaration of an indexer with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int this[int i = 1, bool b = true]
        {
            get
            {
                if (b == false)
                    return 0;
                return 1;
            }

            set
            {
            }
        }
    }

    public class Child : Parent
    {
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
            dynamic p = new Child();
            p[b: false] = 1;
            return p[b: false];
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.indexer18b.indexer18b
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Named params on an indexer</Title>
    // <Description>Simple Declaration of an indexer with optional parameters</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int this[int i = 1, bool b = true]
        {
            get
            {
                if (b == false)
                    return 0;
                return 1;
            }

            set
            {
            }
        }
    }

    public class Child : Parent
    {
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
            dynamic p = new Child();
            p[b: false] = 1;
            return p[b: false];
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.indexer19.indexer19
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Named params on an indexer</Title>
    // <Description>Operations on indexers with pre/post increment</Description>
    // <Expects status=success></Expects>
    // <Code>

    public class Program
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic c1 = new C();
            var x = c1[x: 1, s: "foo"]++;
            if (c1.index == 2 && x == 1)
                return 0;
            return 1;
        }
    }

    public class C
    {
        public int index = 1;
        public int this[string s, int x]
        {
            get
            {
                return index;
            }

            set
            {
                index = value;
            }
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.indexer20.indexer20
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Named params on an indexer</Title>
    // <Description>Operations on indexers with pre/post increment</Description>
    // <Expects status=success></Expects>
    // <Code>

    public class Program
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic c1 = new C();
            var x = ++c1[x: 1, s: "foo"];
            if (c1.index == 2 && x == 2)
                return 0;
            return 1;
        }
    }

    public class C
    {
        public int index = 1;
        public int this[string s, int x]
        {
            get
            {
                return index;
            }

            set
            {
                index = value;
            }
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.indexer21.indexer21
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>Named params on an indexer</Title>
    // <Description>Operations on indexers with pre/post increment</Description>
    // <Expects status=success></Expects>
    // <Code>

    public class Program
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic c1 = new C();
            var x = ++c1[s: "foo", x: ++c1.index];
            if (c1.index == 5 && x == 3)
                return 0;
            return 1;
        }
    }

    public class C
    {
        public int index = 1;
        public int this[string s, int x]
        {
            get
            {
                return index;
            }

            set
            {
                index = value + x;
            }
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.out01.out01
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>calling with a ref parameter </Title>
    // <Description>Should be able to call a ref parameter</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(out int x)
        {
            x = 2;
            if (x == 2)
                return 0;
            return 1;
        }
    }

    public class Test
    {

        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic p = new Parent();
            int i = 2;
            return p.Foo(x: out i);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.out01b.out01b
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>calling with a ref parameter </Title>
    // <Description>Should be able to call a ref parameter</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(out int x)
        {
            x = 2;
            if (x == 2)
                return 0;
            return 1;
        }
    }

    public class Test
    {

        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic p = new Parent();
            dynamic i = 2;
            try
            {
                p.Foo(x: out i);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadArgTypes, e.Message, "Parent.Foo(out int)");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.out01c.out01c
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>calling with a ref parameter </Title>
    // <Description>Should be able to call a ref parameter</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(out dynamic x)
        {
            x = 2;
            if (x == 2)
                return 0;
            return 1;
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
            Parent p = new Parent();
            dynamic i = 2;
            return p.Foo(x: out i);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.ref01.ref01
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>calling with a ref parameter </Title>
    // <Description>Should be able to call a ref parameter</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(ref int x)
        {
            if (x == 2)
                return 0;
            return 1;
        }
    }

    public class Test
    {

        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic p = new Parent();
            int i = 2;
            return p.Foo(x: ref i);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.ref01b.ref01b
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>calling with a ref parameter </Title>
    // <Description>Should be able to call a ref parameter</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(ref int x)
        {
            if (x == 2)
                return 0;
            return 1;
        }
    }

    public class Test
    {

        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic p = new Parent();
            dynamic i = 2;
            try
            {
                p.Foo(x: ref i);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadArgTypes, e.Message, "Parent.Foo(ref int)");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.ref01c.ref01c
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>calling with a ref parameter </Title>
    // <Description>Should be able to call a ref parameter</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public int Foo(ref dynamic x)
        {
            if (x == 2)
                return 0;
            return 1;
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
            Parent p = new Parent();
            dynamic i = 2;
            return p.Foo(x: ref i);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.ref03.ref03
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>calling with a ref parameter </Title>
    // <Description>Should be able to call a ref parameter</Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public class Parent
    {
        public int Foo(
        [Optional]
        ref int x)
        {
            if (x == 2)
                return 1;
            return 1;
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
            dynamic p = new Parent();
            int i = 2;
            try
            {
                p.Foo(x: i);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadArgTypes, e.Message, "Parent.Foo(ref int)");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.ref03a.ref03a
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>calling with a ref parameter </Title>
    // <Description>Should be able to call a ref parameter</Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public class Parent
    {
        public int Foo(
        [Optional]
        ref int x)
        {
            if (x == 2)
                return 1;
            return 1;
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
            Parent p = new Parent();
            dynamic i = 2;
            try
            {
                p.Foo(x: i);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadArgTypes, e.Message, "Parent.Foo(ref int)");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.ref03b.ref03b
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>calling with a ref parameter </Title>
    // <Description>Should be able to call a ref parameter</Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public class Parent
    {
        public int Foo(
        [Optional]
        ref int x)
        {
            if (x == 2)
                return 1;
            return 1;
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
            dynamic p = new Parent();
            dynamic i = 2;
            try
            {
                p.Foo(x: i);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadArgTypes, e.Message, "Parent.Foo(ref int)");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.ref03c.ref03c
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>calling with a ref parameter </Title>
    // <Description>Should be able to call a ref parameter</Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public class Parent
    {
        public int Foo(
        [Optional]
        ref dynamic x)
        {
            if (x == 2)
                return 1;
            return 1;
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
            Parent p = new Parent();
            dynamic i = 2;
            try
            {
                p.Foo(x: i);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadArgTypes, e.Message, "Parent.Foo(ref object)");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.ref04.ref04
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>calling with a ref parameter </Title>
    // <Description>Should be able to call a ref parameter</Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public class Parent
    {
        public int Foo(
        [Optional]
        ref int x)
        {
            if (x == 2)
                return 0;
            return 1;
        }
    }

    public class Test
    {

        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic p = new Parent();
            int i = 2;
            return p.Foo(x: ref i);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.ref04b.ref04b
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>calling with a ref parameter </Title>
    // <Description>Should be able to call a ref parameter</Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public class Parent
    {
        public int Foo(
        [Optional]
        ref int x)
        {
            if (x == 2)
                return 0;
            return 1;
        }
    }

    public class Test
    {

        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic p = new Parent();
            dynamic i = 2;
            try
            {
                p.Foo(x: ref i);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadArgTypes, e.Message, "Parent.Foo(ref int)");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.ref04c.ref04c
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>calling with a ref parameter </Title>
    // <Description>Should be able to call a ref parameter</Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public class Parent
    {
        public int Foo(
        [Optional]
        ref dynamic x)
        {
            if (x == 2)
                return 0;
            return 1;
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
            Parent p = new Parent();
            dynamic i = 2;
            return p.Foo(x: ref i);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.ref05.ref05
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>calling with a ref parameter </Title>
    // <Description>Should be able to call a ref parameter</Description>
    // <Expects status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(21,7\).*CS0219</Expects>
    using System.Runtime.InteropServices;

    public class Parent
    {
        public int Foo(
        [Optional]
        ref int x)
        {
            if (x == 0)
                return 0;
            return 1;
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
            dynamic p = new Parent();
            int i = 2;
            try
            {
                p.Foo();
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadArgCount, e.Message, "Foo", "0");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.ref05b.ref05b
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>calling with a ref parameter </Title>
    // <Description>Should be able to call a ref parameter</Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public class Parent
    {
        public int Foo(
        [Optional]
        ref int x)
        {
            if (x == 0)
                return 0;
            return 1;
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
            dynamic p = new Parent();
            dynamic i = 2;
            try
            {
                p.Foo();
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadArgCount, e.Message, "Foo", "0");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.ref06.ref06
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>calling with a ref parameter </Title>
    // <Description>Should be able to call a ref parameter</Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public class Parent
    {
        public int Foo(
        [Optional, DefaultParameterValue(2)]
        ref int x)
        {
            if (x == 2)
                return 0;
            return 1;
        }
    }

    public class Test
    {

        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic p = new Parent();
            dynamic i = 2;
            try
            {
                p.Foo(ref i);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadArgTypes, e.Message, "Parent.Foo(ref int)");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.ref06b.ref06b
{
    // <Area>Declaration of Methods with Optional Parameters</Area>
    // <Title>calling with a ref parameter </Title>
    // <Description>Should be able to call a ref parameter</Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Runtime.InteropServices;

    public class Parent
    {
        public int Foo(
        [Optional, DefaultParameterValue(2)]
        ref int x)
        {
            if (x == 2)
                return 0;
            return 1;
        }
    }

    public class Test
    {

        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic p = new Parent();
            dynamic i = 2;
            try
            {
                p.Foo(ref i);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadArgTypes, e.Message, "Parent.Foo(ref int)");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.other.literals01.literals01
{
    // <Title> Passing literals to the binder using N&O</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int tests = 0, success = 0;
            var s = new Test();
            dynamic d = new Test();
            //converting null to string
            tests++;
            if (s.Foo(x: 1, y: null) == 2)
                success++; //this should compile
            tests++;
            try
            {
                if (d.Foo(x: 1, y: null) == 2)
                    success++;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
            {
            }

            //converting 0 to enum
            tests++;
            if (s.Foo(x: 1, y: 0) == 1)
                success++; //this should compile
            tests++;
            try
            {
                if (d.Foo(x: 1, y: 0) == 1)
                    success++;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
            {
            }

            //numeric conversions
            tests++;
            if (s.Bar(x: 1, y: 1) == 3)
                success++;
            tests++;
            try
            {
                if (d.Bar(x: 1, y: 0) == 3)
                    success++;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
            {
            }

            return tests == success ? 0 : 1;
        }

        public int Foo(int x, E y)
        {
            return 1;
        }

        public int Foo(int x, string y)
        {
            return 2;
        }

        public int Bar(int x, long y)
        {
            return 3;
        }
    }

    public enum E
    {
        E1,
        E2
    }
    // </Code>
}
