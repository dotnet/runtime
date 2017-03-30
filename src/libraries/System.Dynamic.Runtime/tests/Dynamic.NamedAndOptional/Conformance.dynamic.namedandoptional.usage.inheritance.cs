// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.explicit02.explicit02
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(14,17\).*CS1066</Expects>
    //<Expects Status=warning>\(14,26\).*CS1066</Expects>
    public interface Parent
    {
        int Foo(int x = 1, int y = 1);
    }

    public class Derived : Parent
    {
        int Parent.Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            dynamic p = new Derived();
            return (p as Parent).Foo(x: 2, y: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.explicit02a.explicit02a
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(14,17\).*CS1066</Expects>
    //<Expects Status=warning>\(14,26\).*CS1066</Expects>
    public interface Parent
    {
        int Foo(int x = 1, int y = 1);
    }

    public class Derived : Parent
    {
        int Parent.Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            Derived p = new Derived();
            dynamic x = 2;
            dynamic y = 2;
            return (p as Parent).Foo(x: x, y: y);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.explicit02b.explicit02b
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(14,17\).*CS1066</Expects>
    //<Expects Status=warning>\(14,26\).*CS1066</Expects>
    public interface Parent
    {
        int Foo(int x = 1, dynamic y = null);
    }

    public class Derived : Parent
    {
        int Parent.Foo(int i = 1, dynamic j = null)
        {
            if (i == 2 && j == 2)
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
            Derived p = new Derived();
            return (p as Parent).Foo(x: 2, y: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.explicit02c.explicit02c
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(14,17\).*CS1066</Expects>
    //<Expects Status=warning>\(14,26\).*CS1066</Expects>
    public interface Parent
    {
        int Foo(int x = 1, int y = 1);
    }

    public class Derived : Parent
    {
        int Parent.Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            dynamic p = new Derived();
            dynamic x = 2;
            dynamic y = 2;
            return (p as Parent).Foo(x: x, y: y);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.explicit03.explicit03
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(14,17\).*CS1066</Expects>
    //<Expects Status=warning>\(14,26\).*CS1066</Expects>
    public interface Parent
    {
        int Foo(int x = 1, int y = 1);
    }

    public class Derived : Parent
    {
        int Parent.Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            dynamic p = new Derived();
            try
            {
                p.Foo(j: 2, i: 2);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.NoSuchMember, e.Message, "Derived", "Foo");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.explicit03c.explicit03c
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(14,17\).*CS1066</Expects>
    //<Expects Status=warning>\(14,26\).*CS1066</Expects>
    public interface Parent
    {
        int Foo(int x = 1, int y = 1);
    }

    public class Derived : Parent
    {
        int Parent.Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            dynamic p = new Derived();
            dynamic j = 2;
            dynamic i = 2;
            try
            {
                p.Foo(j: j, i: i);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.NoSuchMember, e.Message, "Derived", "Foo");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.explicit04.explicit04
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(14,17\).*CS1066</Expects>
    //<Expects Status=warning>\(14,26\).*CS1066</Expects>
    public interface Parent
    {
        int Foo(int x = 1, int y = 1);
    }

    public class Derived : Parent
    {
        int Parent.Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            dynamic p = new Derived();
            try
            {
                p.Foo(j: 2, i: 2);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.NoSuchMember, e.Message, "Derived", "Foo");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.explicit04c.explicit04c
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(14,17\).*CS1066</Expects>
    //<Expects Status=warning>\(14,26\).*CS1066</Expects>
    public interface Parent
    {
        int Foo(int x = 1, int y = 1);
    }

    public class Derived : Parent
    {
        int Parent.Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            dynamic p = new Derived();
            dynamic j = 2;
            dynamic i = 2;
            try
            {
                p.Foo(j: j, i: i);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.NoSuchMember, e.Message, "Derived", "Foo");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.explicit05.explicit05
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(14,17\).*CS1066</Expects>
    //<Expects Status=warning>\(14,26\).*CS1066</Expects>
    public interface Parent
    {
        int Foo(int x = 1, int y = 1);
    }

    public class Derived : Parent
    {
        int Parent.Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            dynamic p = new Derived() as Parent;
            try
            {
                p.Foo(x: 2, y: 2);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.NoSuchMember, e.Message, "Derived", "Foo");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.explicit05a.explicit05a
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(14,17\).*CS1066</Expects>
    //<Expects Status=warning>\(14,26\).*CS1066</Expects>
    public interface Parent
    {
        int Foo(int x = 1, int y = 1);
    }

    public class Derived : Parent
    {
        int Parent.Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            Parent p = new Derived();
            dynamic x = 2;
            dynamic y = 2;
            return p.Foo(x: x, y: y);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.explicit05b.explicit05b
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(14,17\).*CS1066</Expects>
    //<Expects Status=warning>\(14,33\).*CS1066</Expects>
    public interface Parent
    {
        int Foo(dynamic x = null, dynamic y = null);
    }

    public class Derived : Parent
    {
        int Parent.Foo(dynamic i = null, dynamic j = null)
        {
            if (i == 2 && j == 2)
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
            Parent p = new Derived();
            return p.Foo(x: 2, y: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.explicit05c.explicit05c
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(14,17\).*CS1066</Expects>
    //<Expects Status=warning>\(14,26\).*CS1066</Expects>
    public interface Parent
    {
        int Foo(int x = 1, int y = 1);
    }

    public class Derived : Parent
    {
        int Parent.Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            dynamic p = new Derived() as Parent;
            dynamic x = 2;
            dynamic y = 2;
            try
            {
                p.Foo(x: x, y: y);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.NoSuchMember, e.Message, "Derived", "Foo");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit01.inherit01
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int x = 1, int y = 1)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public override int Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            dynamic p = new Derived();
            return p.Foo(i: 2, j: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit01a.inherit01a
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int x = 1, int y = 1)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public override int Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            Derived p = new Derived();
            dynamic i = 2;
            dynamic j = 2;
            return p.Foo(i: i, j: j);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit01b.inherit01b
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(dynamic x = null, int y = 1)
        {
            return 1;
        }
    }

    public class Derived : Parent
    {
        public override int Foo(dynamic i = null, int j = 1)
        {
            if (i == 2 && j == 2)
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
            Derived p = new Derived();
            return p.Foo(i: 2, j: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit01c.inherit01c
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int x = 1, int y = 1)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public override int Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            dynamic p = new Derived();
            dynamic i = 2;
            dynamic j = 2;
            return p.Foo(i: i, j: j);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit01d.inherit01d
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int x = 1, int y = 1)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public override int Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
                return 0;
            return 1;
        }

        public int Method()
        {
            dynamic i = 2;
            dynamic j = 2;
            return Foo(i: i, j: j);
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
            return p.Method();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit02.inherit02
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int x = 1, int y = 1)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public override int Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            dynamic p = new Derived();
            try
            {
                p.Foo(x: 2, y: 2);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadNamedArgument, e.Message, "Foo", "x");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit02c.inherit02c
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int x = 1, int y = 1)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public override int Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            dynamic p = new Derived();
            dynamic x = 2;
            dynamic y = 2;
            try
            {
                p.Foo(x: x, y: y);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadNamedArgument, e.Message, "Foo", "x");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit03.inherit03
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int x = 1, int y = 1)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public override int Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            dynamic p = new Derived() as Parent;
            try
            {
                p.Foo(x: 2, y: 2);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadNamedArgument, e.Message, "Foo", "x");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit03a.inherit03a
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int x = 1, int y = 1)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public override int Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            Parent p = new Derived();
            dynamic x = 2;
            dynamic y = 2;
            return p.Foo(x: x, y: y);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit03c.inherit03c
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int x = 1, int y = 1)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public override int Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            dynamic p = new Derived() as Parent;
            dynamic x = 2;
            dynamic y = 2;
            try
            {
                p.Foo(x: x, y: y);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadNamedArgument, e.Message, "Foo", "x");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit03d.inherit03d
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int x = 1, int y = 1)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }

        public int Method()
        {
            dynamic x = 2;
            dynamic y = 2;
            return Foo(x: x, y: y);
        }
    }

    public class Derived : Parent
    {
        public override int Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            dynamic p = new Derived() as Parent;
            return p.Method();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit04.inherit04
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int x = 1, int y = 1)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public override int Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            dynamic p = new Derived() as Parent;
            return p.Foo(i: 2, j: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit04c.inherit04c
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int x = 1, int y = 1)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public override int Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            dynamic p = new Derived() as Parent;
            dynamic i = 2;
            dynamic j = 2;
            return p.Foo(i: i, j: j);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit04d.inherit04d
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int x = 1, int y = 1)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public override int Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
                return 0;
            return 1;
        }

        public int Method()
        {
            dynamic i = 2;
            dynamic j = 2;
            return Foo(i: i, j: j);
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
            dynamic p = new Derived() as Parent;
            return p.Method(); //dynamic will choose the runtype type Derived to invoke the method.
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit05.inherit05
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int x = 1, int y = 1)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public new int Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            dynamic p = new Derived() as Parent;
            return p.Foo(i: 2, j: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit05b.inherit05b
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int x = 1, dynamic y = null)
        {
            if (x == 2 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public new int Foo(int i = 1, dynamic j = null)
        {
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
            Parent p = new Derived();
            return p.Foo(x: 2, y: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit05c.inherit05c
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int x = 1, int y = 1)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public new int Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            dynamic p = new Derived() as Parent;
            dynamic i = 2;
            dynamic j = 2;
            return p.Foo(i: i, j: j);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit05d.inherit05d
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int x = 1, int y = 1)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public new int Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
                return 0;
            return 1;
        }

        public int Method()
        {
            dynamic i = 2;
            dynamic j = 2;
            return Foo(i: i, j: j);
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
            dynamic p = new Derived() as Parent;
            return p.Method();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit06.inherit06
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int x = 1, int y = 1)
        {
            if (x == 2 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public new int Foo(int i = 1, int j = 1)
        {
            if (i == 3 && j == 2)
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
            dynamic p = new Derived();
            return p.Foo(x: 2, y: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit06a.inherit06a
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int x = 1, int y = 1)
        {
            if (x == 2 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public new int Foo(int i = 1, int j = 1)
        {
            if (i == 3 && j == 2)
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
            Parent p = new Derived();
            dynamic x = 2;
            dynamic y = 2;
            return p.Foo(x: x, y: y);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit06c.inherit06c
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int x = 1, int y = 1)
        {
            if (x == 2 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public new int Foo(int i = 1, int j = 1)
        {
            if (i == 3 && j == 2)
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
            dynamic p = new Derived();
            dynamic x = 2;
            dynamic y = 2;
            return p.Foo(x: x, y: y);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit06d.inherit06d
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int x = 1, int y = 1)
        {
            if (x == 2 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public new int Foo(int i = 1, int j = 1)
        {
            if (i == 3 && j == 2)
                return 0;
            return 1;
        }

        public int MyProperty
        {
            get
            {
                dynamic x = 2;
                dynamic y = 2;
                return Foo(x: x, y: y);
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
            dynamic p = new Derived();
            return p.MyProperty;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit07.inherit07
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int x = 1, int y = 1)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public new int Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            dynamic p = new Derived();
            return p.Foo(x: 3, y: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit07a.inherit07a
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int x = 1, int y = 1)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public new int Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            Derived p = new Derived();
            dynamic x = 3;
            dynamic y = 2;
            return p.Foo(x: x, y: y);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit07b.inherit07b
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(dynamic x = null, dynamic y = null)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public new int Foo(dynamic i = null, dynamic j = null)
        {
            if (i == 2 && j == 2)
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
            Derived p = new Derived();
            return p.Foo(x: 3, y: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit07c.inherit07c
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int x = 1, int y = 1)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public new int Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            dynamic p = new Derived();
            dynamic x = 3;
            dynamic y = 2;
            return p.Foo(x: x, y: y);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit07d.inherit07d
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int x = 1, int y = 1)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public new int Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
                return 0;
            return 1;
        }

        public int this[int? i]
        {
            get
            {
                dynamic x = 3;
                dynamic y = 2;
                return Foo(x: x, y: y);
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
            dynamic p = new Derived();
            return p[null];
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit08.inherit08
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int x = 1, int y = 1)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public new int Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            dynamic p = new Derived();
            return p.Foo(i: 2, j: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit08a.inherit08a
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int x = 1, int y = 1)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public new int Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            Derived p = new Derived();
            dynamic i = 2;
            dynamic j = 2;
            return p.Foo(i: i, j: j);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit08b.inherit08b
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int x = 1, dynamic y = null)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public new int Foo(int i = 1, dynamic j = null)
        {
            if (i == 2 && j == 2)
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
            Derived p = new Derived();
            return p.Foo(i: 2, j: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit08c.inherit08c
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int x = 1, int y = 1)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public new int Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            dynamic p = new Derived();
            dynamic i = 2;
            dynamic j = 2;
            return p.Foo(i: i, j: j);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit08d.inherit08d
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int x = 1, int y = 1)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public new int Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
                return 0;
            return 1;
        }

        public static int Method()
        {
            dynamic p = new Derived();
            dynamic i = 2;
            dynamic j = 2;
            return p.Foo(i: i, j: j);
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
            return Derived.Method();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit09.inherit09
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int x = 1, int y = 1)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public override int Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            dynamic p = new Derived();
            try
            {
                p.Foo(i: 2, y: 2);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadNamedArgument, e.Message, "Foo", "y");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit09c.inherit09c
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int x = 1, int y = 1)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public override int Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            dynamic p = new Derived();
            dynamic i = 2;
            dynamic y = 2;
            try
            {
                p.Foo(i: i, y: y);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadNamedArgument, e.Message, "Foo", "y");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit11.inherit11
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int x = 1, int y = 1)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public override int Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            dynamic p = new Derived();
            return ((Parent)p).Foo(x: 2, y: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit11a.inherit11a
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int x = 1, int y = 1)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public override int Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            Derived p = new Derived();
            dynamic x = 2;
            dynamic y = 2;
            return ((Parent)p).Foo(x: x, y: y);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit11b.inherit11b
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int x = 1, dynamic y = null)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public override int Foo(int i = 1, dynamic j = null)
        {
            if (i == 2 && j == 2)
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
            Derived p = new Derived();
            return ((Parent)p).Foo(x: 2, y: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit11c.inherit11c
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int x = 1, int y = 1)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public override int Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            dynamic p = new Derived();
            dynamic x = 2;
            dynamic y = 2;
            return ((Parent)p).Foo(x: x, y: y);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit13.inherit13
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int x = 1, int y = 1)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public new int Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            dynamic p = new Derived();
            return ((Parent)p).Foo(y: 2, x: 3);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit13a.inherit13a
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int x = 1, int y = 1)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public new int Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            Derived p = new Derived();
            dynamic x = 3;
            dynamic y = 2;
            return ((Parent)p).Foo(y: y, x: x);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit13b.inherit13b
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(dynamic x = null, dynamic y = null)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public new int Foo(dynamic i = null, dynamic j = null)
        {
            if (i == 2 && j == 2)
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
            Derived p = new Derived();
            return ((Parent)p).Foo(y: 2, x: 3);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit13c.inherit13c
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int x = 1, int y = 1)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public new int Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            dynamic p = new Derived();
            dynamic x = 3;
            dynamic y = 2;
            return ((Parent)p).Foo(y: y, x: x);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit14.inherit14
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type - New disambiguation reasoning</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int x = 1, int y = 1)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public new int Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            dynamic p = new Derived();
            return ((Derived)p).Foo(y: 2, x: 3);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit14a.inherit14a
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type - New disambiguation reasoning</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int x = 1, int y = 1)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public new int Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            Parent p = new Derived();
            dynamic x = 3;
            dynamic y = 2;
            return ((Derived)p).Foo(y: y, x: x);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit14b.inherit14b
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type - New disambiguation reasoning</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(dynamic x = null, int y = 1)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public new int Foo(dynamic i = null, int j = 1)
        {
            if (i == 2 && j == 2)
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
            Parent p = new Derived();
            return ((Derived)p).Foo(y: 2, x: 3);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit14c.inherit14c
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type - New disambiguation reasoning</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int x = 1, int y = 1)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public new int Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            dynamic p = new Derived();
            dynamic x = 3;
            dynamic y = 2;
            return ((Derived)p).Foo(y: y, x: x);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit15.inherit15
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int x = 1, int y = 1)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public new int Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            dynamic p = new Derived();
            return ((Derived)p).Foo(i: 2, j: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit15a.inherit15a
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int x = 1, int y = 1)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public new int Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            Parent p = new Derived();
            dynamic i = 2;
            dynamic j = 2;
            return ((Derived)p).Foo(i: i, j: j);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit15b.inherit15b
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int x = 1, dynamic y = null)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public new int Foo(int i = 1, dynamic j = null)
        {
            if (i == 2 && j == 2)
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
            Parent p = new Derived();
            return ((Derived)p).Foo(i: 2, j: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit15c.inherit15c
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int x = 1, int y = 1)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public new int Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            dynamic p = new Derived();
            dynamic i = 2;
            dynamic j = 2;
            return ((Derived)p).Foo(i: i, j: j);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit16.inherit16
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int x = 1, int y = 1)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public override int Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            dynamic p = new Derived();
            return ((Derived)p).Foo(i: 2, j: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit16a.inherit16a
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int x = 1, int y = 1)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public override int Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            Parent p = new Derived();
            dynamic i = 2;
            dynamic j = 2;
            return ((Derived)p).Foo(i: i, j: j);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit16b.inherit16b
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(dynamic x = null, int y = 1)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public override int Foo(dynamic i = null, int j = 1)
        {
            if (i == 2 && j == 2)
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
            Parent p = new Derived();
            return ((Derived)p).Foo(i: 2, j: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit16c.inherit16c
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int x = 1, int y = 1)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public override int Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            dynamic p = new Derived();
            dynamic i = 2;
            dynamic j = 2;
            return ((Derived)p).Foo(i: i, j: j);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit19.inherit19
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int x = 1, int y = 1)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public override int Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            dynamic p = new Derived();
            return (p as Parent).Foo(x: 2, y: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit19a.inherit19a
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int x = 1, int y = 1)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public override int Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            Derived p = new Derived();
            dynamic x = 2;
            dynamic y = 2;
            return (p as Parent).Foo(x: x, y: y);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit19b.inherit19b
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(dynamic x = null, dynamic y = null)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public override int Foo(dynamic i = null, dynamic j = null)
        {
            if (i == 2 && j == 2)
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
            Derived p = new Derived();
            return (p as Parent).Foo(x: 2, y: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit19c.inherit19c
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int x = 1, int y = 1)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public override int Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            dynamic p = new Derived();
            dynamic x = 2;
            dynamic y = 2;
            return (p as Parent).Foo(x: x, y: y);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit21.inherit21
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int x = 1, int y = 1)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public new int Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            dynamic p = new Derived();
            return (p as Parent).Foo(x: 3, y: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit21a.inherit21a
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int x = 1, int y = 1)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public new int Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            Derived p = new Derived();
            dynamic x = 3;
            dynamic y = 2;
            return (p as Parent).Foo(x: x, y: y);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit21b.inherit21b
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(dynamic x = null, int y = 1)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public new int Foo(dynamic i = null, int j = 1)
        {
            if (i == 2 && j == 2)
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
            Derived p = new Derived();
            return (p as Parent).Foo(x: 3, y: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.inherit21c.inherit21c
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public class Parent
    {
        public virtual int Foo(int x = 1, int y = 1)
        {
            if (x == 3 && y == 2)
                return 0;
            return 1;
        }
    }

    public class Derived : Parent
    {
        public new int Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            dynamic p = new Derived();
            dynamic x = 3;
            dynamic y = 2;
            return (p as Parent).Foo(x: x, y: y);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.multi01.multi01
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public interface Parent
    {
        int Foo(int x = 1, int y = 1);
    }

    public interface Parent2
    {
        int Foo(int x2 = 1, int y2 = 1);
    }

    public class Derived : Parent, Parent2
    {
        public int Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            dynamic p = new Derived();
            return p.Foo(j: 2, i: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.multi01a.multi01a
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public interface Parent
    {
        int Foo(int x = 1, int y = 1);
    }

    public interface Parent2
    {
        int Foo(int x2 = 1, int y2 = 1);
    }

    public class Derived : Parent, Parent2
    {
        public int Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            Derived p = new Derived();
            dynamic j = 2;
            dynamic i = 2;
            return p.Foo(j: j, i: i);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.multi01b.multi01b
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public interface Parent
    {
        int Foo(dynamic x = null, int y = 1);
    }

    public interface Parent2
    {
        int Foo(dynamic x2 = null, int y2 = 1);
    }

    public class Derived : Parent, Parent2
    {
        public int Foo(dynamic i = null, int j = 1)
        {
            if (i == 2 && j == 2)
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
            Derived p = new Derived();
            return p.Foo(j: 2, i: 2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.multi01c.multi01c
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public interface Parent
    {
        int Foo(int x = 1, int y = 1);
    }

    public interface Parent2
    {
        int Foo(int x2 = 1, int y2 = 1);
    }

    public class Derived : Parent, Parent2
    {
        public int Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            dynamic p = new Derived();
            dynamic j = 2;
            dynamic i = 2;
            return p.Foo(j: j, i: i);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.multi02.multi02
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public interface Parent
    {
        int Foo(int x = 1, int y = 1);
    }

    public interface Parent2
    {
        int Foo(int x2 = 1, int y2 = 1);
    }

    public class Derived : Parent, Parent2
    {
        public int Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            dynamic p = new Derived();
            try
            {
                p.Foo(x: 2, y: 2);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadNamedArgument, e.Message, "Foo", "x");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.multi02c.multi02c
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public interface Parent
    {
        int Foo(int x = 1, int y = 1);
    }

    public interface Parent2
    {
        int Foo(int x2 = 1, int y2 = 1);
    }

    public class Derived : Parent, Parent2
    {
        public int Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            dynamic p = new Derived();
            dynamic x = 2;
            dynamic y = 2;
            try
            {
                p.Foo(x: x, y: y);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadNamedArgument, e.Message, "Foo", "x");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.multi03.multi03
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public interface Parent
    {
        int Foo(int x = 1, int y = 1);
    }

    public interface Parent2
    {
        int Foo(int x2 = 1, int y2 = 1);
    }

    public class Derived : Parent, Parent2
    {
        public int Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            dynamic p = new Derived();
            try
            {
                p.Foo(x2: 2, y2: 2);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadNamedArgument, e.Message, "Foo", "x2");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.multi03c.multi03c
{
    // <Area>Use of Named parameters</Area>
    // <Title>Use of Named Parameters</Title>
    // <Description>Parameters names are based off the most derived type</Description>
    // <Expects status=success></Expects>
    // <Code>
    public interface Parent
    {
        int Foo(int x = 1, int y = 1);
    }

    public interface Parent2
    {
        int Foo(int x2 = 1, int y2 = 1);
    }

    public class Derived : Parent, Parent2
    {
        public int Foo(int i = 1, int j = 1)
        {
            if (i == 2 && j == 2)
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
            dynamic p = new Derived();
            dynamic x2 = 2;
            dynamic y2 = 2;
            try
            {
                p.Foo(x2: x2, y2: y2);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadNamedArgument, e.Message, "Foo", "x2");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.namedandoptional.usage.inheritance.integeregererface01.integeregererface01
{
    //<Area>N&O</Area>
    //<Title></Title>
    //<Description>The implement in class do not have default value while the declare in interface has default value</Description>
    //<Related Bugs></Related Bugs>
    //<Expects Status=success></Expects>
    //<Code>
    public interface IA
    {
        int Foo(int x = 0);
    }

    public class A : IA
    {
        public int Foo(int x)
        {
            return x;
        }

        private static int Bar<T>(T x) where T : A, IA
        {
            return x.Foo();
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            return Bar(new A());
        }
    }
    //</Code>
}
