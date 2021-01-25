// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.evnt.minuseql.dlgate001.dlgate001
{
    // <Area> Dynamic -- compound operator</Area>
    // <Title> compound operator +=/-= on event </Title>
    // <Description>
    // rhs is delegate creation expression : with event accessor and rhs inside the invocation list
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public delegate int Dele(int i);
    public class C
    {
        public event Dele E;
        public static int Foo1(int i)
        {
            return i + 1;
        }

        public static int Foo2(int i)
        {
            return i + 2;
        }

        public static int Foo3(int i)
        {
            return i + 3;
        }

        public C()
        {
            E += C.Foo1;
            E += C.Foo2;
        }


        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic c = new C();
            c.E -= (Dele)C.Foo2;
            var length = c.E.GetInvocationList().Length;
            var result = c.DoEvent(9);
            if (result != 10 && length != 1)
                return 1;
            c.E -= new Dele(C.Foo1);
            if (c.E != null)
                return 1;
            return 0;
        }

        public int DoEvent(int arg)
        {
            return this.E(arg);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.evnt.minuseql.dlgate002.dlgate002
{
    // <Area> Dynamic -- compound operator</Area>
    // <Title> compound operator +=/-= on event </Title>
    // <Description>
    // rhs is delegate creation expression : without event accessor and rhs inside the invocation list
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public delegate int Dele(int i);
    public class C
    {
        public Dele E;
        public static int Foo1(int i)
        {
            return i + 1;
        }

        public static int Foo2(int i)
        {
            return i + 2;
        }

        public static int Foo3(int i)
        {
            return i + 3;
        }

        public C()
        {
            E += C.Foo1;
            E += C.Foo2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic c = new C();
            c.E -= (Dele)C.Foo2;
            var length = c.E.GetInvocationList().Length;
            var result = c.DoEvent(9);
            if (result != 10 && length != 1)
                return 1;
            c.E -= new Dele(C.Foo1);
            if (c.E != null)
                return 1;
            return 0;
        }

        public int DoEvent(int arg)
        {
            return this.E(arg);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.evnt.minuseql.dlgate003.dlgate003
{
    // <Area> Dynamic -- compound operator</Area>
    // <Title> compound operator +=/-= on event </Title>
    // <Description>
    // rhs is delegate creation expression : with event accessor and rhs inside the invocation list
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public delegate int Dele(int i);
    public class C
    {
        public event Dele E;
        public static int Foo1(int i)
        {
            return i + 1;
        }

        public static int Foo2(int i)
        {
            return i + 2;
        }

        public static int Foo3(int i)
        {
            return i + 3;
        }

        public C()
        {
            E += C.Foo1;
            E += C.Foo2;
        }


        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic c = new C();
            c.E -= (Dele)C.Foo3;
            var length = c.E.GetInvocationList().Length;
            var result = c.DoEvent(9);
            if (result != 11 && length != 2)
                return 1;
            c.E -= new Dele(delegate (int i)
            {
                return i + 2;
            }

            );
            length = c.E.GetInvocationList().Length;
            result = c.DoEvent(9);
            if (result != 11 && length != 2)
                return 1;
            return 0;
        }

        public int DoEvent(int arg)
        {
            return this.E(arg);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.evnt.minuseql.dlgate004.dlgate004
{
    // <Area> Dynamic -- compound operator</Area>
    // <Title> compound operator +=/-= on event </Title>
    // <Description>
    // rhs is delegate creation expression : without event accessor and rhs outside the invocation list
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public delegate int Dele(int i);
    public class C
    {
        public Dele E;
        public static int Foo1(int i)
        {
            return i + 1;
        }

        public static int Foo2(int i)
        {
            return i + 2;
        }

        public static int Foo3(int i)
        {
            return i + 3;
        }

        public C()
        {
            E += C.Foo1;
            E += C.Foo2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic c = new C();
            c.E -= (Dele)C.Foo3;
            var length = c.E.GetInvocationList().Length;
            var result = c.DoEvent(9);
            if (result != 11 && length != 2)
                return 1;
            c.E -= new Dele(x => x + 2);
            length = c.E.GetInvocationList().Length;
            result = c.DoEvent(9);
            if (result != 11 && length != 2)
                return 1;
            return 0;
        }

        public int DoEvent(int arg)
        {
            return this.E(arg);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.evnt.minuseql.dynamic001.dynamic001
{
    // <Area> Dynamic -- compound operator</Area>
    // <Title> compound operator +=/-= on event </Title>
    // <Description>
    // rhs is dynamic runtime delegate: with event accessor and rhs is inside the invocation list
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public delegate int Dele(int i);
    public class C
    {
        public event Dele E;
        public static int Foo1(int i)
        {
            return i + 1;
        }

        public static int Foo2(int i)
        {
            return i + 2;
        }


        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new C();
            d.E += (Dele)C.Foo1;
            d.E += (Dele)C.Foo2;
            dynamic c = new C();
            c.E += (Dele)C.Foo2;
            d.E -= c.E;
            var length = d.E.GetInvocationList().Length;
            var result = d.DoEvent(9);
            if (result != 10 && length != 1)
                return 1;
            return 0;
        }

        public int DoEvent(int arg)
        {
            return this.E(arg);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.evnt.minuseql.dynamic002.dynamic002
{
    // <Area> Dynamic -- compound operator</Area>
    // <Title> compound operator +=/-= on event </Title>
    // <Description>
    // rhs is dynamic runtime delegate: without event accessor and rhs is inside the invocation list
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public delegate int Dele(int i);
    public class C
    {
        public Dele E;
        public static int Foo1(int i)
        {
            return i + 1;
        }

        public static int Foo2(int i)
        {
            return i + 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new C();
            d.E += (Dele)C.Foo1;
            d.E += (Dele)C.Foo2;
            dynamic c = new C();
            c.E += (Dele)C.Foo2;
            d.E -= c.E;
            var length = d.E.GetInvocationList().Length;
            var result = d.DoEvent(9);
            if (result != 10 && length != 1)
                return 1;
            return 0;
        }

        public int DoEvent(int arg)
        {
            return this.E(arg);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.evnt.minuseql.dynamic003.dynamic003
{
    // <Area> Dynamic -- compound operator</Area>
    // <Title> compound operator +=/-= on event </Title>
    // <Description>
    // rhs is dynamic runtime delegate: with event accessor and rhs is outside the invocation list
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public delegate int Dele(int i);
    public class C
    {
        public event Dele E;
        public static int Foo1(int i)
        {
            return i + 1;
        }

        public static int Foo2(int i)
        {
            return i + 2;
        }

        public static int Foo3(int i)
        {
            return i + 2;
        }


        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new C();
            d.E += (Dele)C.Foo1;
            d.E += (Dele)C.Foo2;
            dynamic c = new C();
            c.E += (Dele)C.Foo3;
            d.E -= c.E;
            var length = d.E.GetInvocationList().Length;
            var result = d.DoEvent(9);
            if (result != 11 && length != 2)
                return 1;
            return 0;
        }

        public int DoEvent(int arg)
        {
            return this.E(arg);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.evnt.minuseql.dynamic004.dynamic004
{
    // <Area> Dynamic -- compound operator</Area>
    // <Title> compound operator +=/-= on event </Title>
    // <Description>
    // rhs is dynamic runtime delegate: without event accessor and rhs is outside the invocation list
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public delegate int Dele(int i);
    public class C
    {
        public Dele E;
        public static int Foo1(int i)
        {
            return i + 1;
        }

        public static int Foo2(int i)
        {
            return i + 2;
        }

        public static int Foo3(int i)
        {
            return i + 2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new C();
            d.E += (Dele)C.Foo1;
            d.E += (Dele)C.Foo2;
            dynamic c = new C();
            c.E += (Dele)C.Foo3;
            d.E -= c.E;
            var length = d.E.GetInvocationList().Length;
            var result = d.DoEvent(9);
            if (result != 11 && length != 2)
                return 1;
            return 0;
        }

        public int DoEvent(int arg)
        {
            return this.E(arg);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.evnt.minuseql.dynamic005.dynamic005
{
    // <Area> Dynamic -- compound operator</Area>
    // <Title> compound operator +=/-= on event </Title>
    // <Description>
    // lhs is static typed and rhs is dynamic runtime delegate: with event accessor and rhs is inside the invocation list
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public delegate int Dele(int i);
    public class C
    {
        public static int Foo1(int i)
        {
            return i + 1;
        }

        public static int Foo2(int i)
        {
            return i + 2;
        }

        public static int Foo3(int i)
        {
            return i + 2;
        }

        public event Dele E;


        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            C d = new C();
            dynamic c = new C();
            c.E += (Dele)C.Foo2;
            d.E += (Dele)C.Foo1;
            d.E += (Dele)C.Foo2;
            d.E -= c.E;
            var length = d.E.GetInvocationList().Length;
            var result = d.DoEvent(9);
            if (result != 10 && length != 1)
                return 1;
            return 0;
        }

        public int DoEvent(int arg)
        {
            return this.E(arg);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.evnt.minuseql.dynamic006.dynamic006
{
    // <Area> Dynamic -- compound operator</Area>
    // <Title> compound operator +=/-= on event </Title>
    // <Description>
    // lhs is static typed and rhs is dynamic runtime delegate: without event accessor and rhs is inside the invocation list
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public delegate int Dele(int i);
    public class C
    {
        public static int Foo1(int i)
        {
            return i + 1;
        }

        public static int Foo2(int i)
        {
            return i + 2;
        }

        public static int Foo3(int i)
        {
            return i + 2;
        }

        public Dele E;

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            C d = new C();
            dynamic c = new C();
            c.E += (Dele)C.Foo2;
            d.E += (Dele)C.Foo1;
            d.E += (Dele)C.Foo2;
            d.E -= c.E;
            var length = d.E.GetInvocationList().Length;
            var result = d.DoEvent(9);
            if (result != 10 && length != 1)
                return 1;
            return 0;
        }

        public int DoEvent(int arg)
        {
            return this.E(arg);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.evnt.minuseql.dynamic007.dynamic007
{
    // <Area> Dynamic -- compound operator</Area>
    // <Title> compound operator +=/-= on event </Title>
    // <Description>
    // lhs is static typed and rhs is dynamic runtime delegate: with event accessor and rhs is outside the invocation list
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public delegate int Dele(int i);
    public class C
    {
        public static int Foo1(int i)
        {
            return i + 1;
        }

        public static int Foo2(int i)
        {
            return i + 2;
        }

        public static int Foo3(int i)
        {
            return i + 2;
        }

        public event Dele E;


        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            C d = new C();
            dynamic c = new C();
            c.E += (Dele)C.Foo3;
            d.E += (Dele)C.Foo1;
            d.E += (Dele)C.Foo2;
            d.E -= c.E;
            var length = d.E.GetInvocationList().Length;
            var result = d.DoEvent(9);
            if (result != 11 && length != 2)
                return 1;
            return 0;
        }

        public int DoEvent(int arg)
        {
            return this.E(arg);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.evnt.minuseql.dynamic008.dynamic008
{
    // <Area> Dynamic -- compound operator</Area>
    // <Title> compound operator +=/-= on event </Title>
    // <Description>
    // lhs is static typed and rhs is dynamic runtime delegate: without event accessor and rhs is outside the invocation list
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public delegate int Dele(int i);
    public class C
    {
        public static int Foo1(int i)
        {
            return i + 1;
        }

        public static int Foo2(int i)
        {
            return i + 2;
        }

        public static int Foo3(int i)
        {
            return i + 2;
        }

        public Dele E;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            C d = new C();
            dynamic c = new C();
            c.E += (Dele)C.Foo3;
            d.E += (Dele)C.Foo1;
            d.E += (Dele)C.Foo2;
            d.E -= c.E;
            var length = d.E.GetInvocationList().Length;
            var result = d.DoEvent(9);
            if (result != 11 && length != 2)
                return 1;
            return 0;
        }

        public int DoEvent(int arg)
        {
            return this.E(arg);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.evnt.minuseql.fieldproperty001.fieldproperty001
{
    // <Area> Dynamic -- compound operator</Area>
    // <Title> compound operator +=/-= on event </Title>
    // <Description>
    // rhs is field/property of delegate type : with event accessor and field/property is inside the invocation list
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public delegate int Dele(int i);
    public class C
    {
        public Dele field;
        public Dele Field
        {
            get
            {
                return field;
            }

            set
            {
                field = value;
            }
        }

        public event Dele E;
        public C()
        {
            field = C.Foo1;
            E += C.Foo1;
            E += C.Foo2;
        }

        public static int Foo1(int i)
        {
            return i + 1;
        }

        public static int Foo2(int i)
        {
            return i + 2;
        }

        public static int Foo3(int i)
        {
            return i + 3;
        }

        public static int Foo(int i)
        {
            return i;
        }


        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new C();
            C c = new C();
            d.E -= c.field;
            var length = d.E.GetInvocationList().Length;
            var result = d.DoEvent(9);
            if (result != 11 && length != 1)
                return 1;
            c.field = C.Foo2;
            d.E -= c.Field;
            if (d.E != null)
                return 1;
            return 0;
        }

        public int DoEvent(int arg)
        {
            return this.E(arg);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.evnt.minuseql.fieldproperty002.fieldproperty002
{
    // <Area> Dynamic -- compound operator</Area>
    // <Title> compound operator +=/-= on event </Title>
    // <Description>
    // rhs is field/property of delegate type : without event accessor and field/property is inside the invocation list
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public delegate int Dele(int i);
    public class C
    {
        public Dele field;
        public Dele Field
        {
            get
            {
                return field;
            }

            set
            {
                field = value;
            }
        }

        public Dele E;
        public C()
        {
            field = C.Foo1;
            E += C.Foo1;
            E += C.Foo2;
        }

        public static int Foo1(int i)
        {
            return i + 1;
        }

        public static int Foo2(int i)
        {
            return i + 2;
        }

        public static int Foo3(int i)
        {
            return i + 3;
        }

        public static int Foo(int i)
        {
            return i;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new C();
            C c = new C();
            d.E -= c.field;
            var length = d.E.GetInvocationList().Length;
            var result = d.DoEvent(9);
            if (result != 11 && length != 1)
                return 1;
            c.field = C.Foo2;
            d.E -= c.Field;
            if (d.E != null)
                return 1;
            return 0;
        }

        public int DoEvent(int arg)
        {
            return this.E(arg);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.evnt.minuseql.fieldproperty003.fieldproperty003
{
    // <Area> Dynamic -- compound operator</Area>
    // <Title> compound operator +=/-= on event </Title>
    // <Description>
    // rhs is field/property of delegate type : with event accessor and field/property is outside the invocation list
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public delegate int Dele(int i);
    public class C
    {
        public Dele field;
        public Dele Field
        {
            get
            {
                return field;
            }

            set
            {
                field = value;
            }
        }

        public event Dele E;
        public C()
        {
            field = C.Foo3;
            E += C.Foo1;
            E += C.Foo2;
        }

        public static int Foo1(int i)
        {
            return i + 1;
        }

        public static int Foo2(int i)
        {
            return i + 2;
        }

        public static int Foo3(int i)
        {
            return i + 3;
        }

        public static int Foo(int i)
        {
            return i;
        }


        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new C();
            C c = new C();
            d.E -= c.field;
            var length = d.E.GetInvocationList().Length;
            var result = d.DoEvent(9);
            if (result != 11 && length != 2)
                return 1;
            c.field = new Dele(x => x + 2);
            d.E -= c.Field;
            length = d.E.GetInvocationList().Length;
            if (length != 2)
                return 1;
            return 0;
        }

        public int DoEvent(int arg)
        {
            return this.E(arg);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.evnt.minuseql.fieldproperty004.fieldproperty004
{
    // <Area> Dynamic -- compound operator</Area>
    // <Title> compound operator +=/-= on event </Title>
    // <Description>
    // rhs is field/property of delegate type : without event accessor and field/property is outside the invocation list
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public delegate int Dele(int i);
    public class C
    {
        public Dele field;
        public Dele Field
        {
            get
            {
                return field;
            }

            set
            {
                field = value;
            }
        }

        public Dele E;
        public C()
        {
            field = C.Foo3;
            E += C.Foo1;
            E += C.Foo2;
        }

        public static int Foo1(int i)
        {
            return i + 1;
        }

        public static int Foo2(int i)
        {
            return i + 2;
        }

        public static int Foo3(int i)
        {
            return i + 3;
        }

        public static int Foo(int i)
        {
            return i;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new C();
            C c = new C();
            d.E -= c.field;
            var length = d.E.GetInvocationList().Length;
            var result = d.DoEvent(9);
            if (result != 11 && length != 2)
                return 1;
            c.field = new Dele(x => x + 2);
            d.E -= c.Field;
            length = d.E.GetInvocationList().Length;
            if (length != 2)
                return 1;
            return 0;
        }

        public int DoEvent(int arg)
        {
            return this.E(arg);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.evnt.minuseql.null001.null001
{
    // <Area> Dynamic -- compound operator</Area>
    // <Title> compound operator +=/-= on event </Title>
    // <Description>
    // Negative: rhs is null literal: lhs is not runtime null.
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public delegate int Dele(int i);
    public class C
    {
        public event Dele E;
        public static int Foo(int i)
        {
            return i;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new C();
            d.E += (Dele)C.Foo;
            int result = 0;
            try
            {
                d.E -= null;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // should not
            {
                result += 1;
            }

            return result;
        }

        public int DoEvent(int arg)
        {
            return this.E(arg);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.evnt.minuseql.null002.null002
{
    // <Area> Dynamic -- compound operator</Area>
    // <Title> compound operator +=/-= on event </Title>
    // <Description>
    // Negative: rhs is null literal: lhs is runtime null.
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public delegate int Dele(int i);
    public class C
    {
        public event Dele E;
        public static int Foo(int i)
        {
            return i;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new C();
            int result = 0;
            try
            {
                d.E -= null;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException) // should not
            {
                result += 1;
            }

            return result;
        }

        public int DoEvent(int arg)
        {
            return this.E(arg);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.evnt.minuseql.return001.return001
{
    // <Area> Dynamic -- compound operator</Area>
    // <Title> compound operator +=/-= on event </Title>
    // <Description>
    // rhs is delegate invocation return delegate : with event accessor and rhs is in invocation list
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public delegate int Dele(int i);
    public class C
    {
        public static int Foo1(int i)
        {
            return i + 1;
        }

        public static int Foo2(int i)
        {
            return i + 2;
        }

        public static int Foo3(int i)
        {
            return i + 3;
        }

        public static Dele Bar()
        {
            return C.Foo2;
        }

        public C()
        {
            E += C.Foo1;
            E += C.Foo2;
        }

        public event Dele E;


        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new C();
            d.E -= C.Bar();
            var length = d.E.GetInvocationList().Length;
            var result = d.DoEvent(9);
            if (result != 10 && length != 1)
                return 1;
            return 0;
        }

        public int DoEvent(int arg)
        {
            return this.E(arg);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.evnt.minuseql.return002.return002
{
    // <Area> Dynamic -- compound operator</Area>
    // <Title> compound operator +=/-= on event </Title>
    // <Description>
    // rhs is delegate invocation return delegate : without event accessor and rhs is in invocation list
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public delegate int Dele(int i);
    public class C
    {
        public static int Foo1(int i)
        {
            return i + 1;
        }

        public static int Foo2(int i)
        {
            return i + 2;
        }

        public static int Foo3(int i)
        {
            return i + 3;
        }

        public static Dele Bar()
        {
            return C.Foo2;
        }

        public C()
        {
            E += C.Foo1;
            E += C.Foo2;
        }

        public Dele E;

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new C();
            d.E -= C.Bar();
            var length = d.E.GetInvocationList().Length;
            var result = d.DoEvent(9);
            if (result != 10 && length != 1)
                return 1;
            return 0;
        }

        public int DoEvent(int arg)
        {
            return this.E(arg);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.evnt.minuseql.return003.return003
{
    // <Area> Dynamic -- compound operator</Area>
    // <Title> compound operator +=/-= on event </Title>
    // <Description>
    // rhs is delegate invocation return delegate : with event accessor and rhs is outside the invocation list
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public delegate int Dele(int i);
    public class C
    {
        public static int Foo1(int i)
        {
            return i + 1;
        }

        public static int Foo2(int i)
        {
            return i + 2;
        }

        public static int Foo3(int i)
        {
            return i + 3;
        }

        public static Dele Bar()
        {
            return C.Foo3;
        }

        public C()
        {
            E += C.Foo1;
            E += C.Foo2;
        }

        public event Dele E;


        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new C();
            d.E -= C.Bar();
            var length = d.E.GetInvocationList().Length;
            var result = d.DoEvent(9);
            if (result != 11 && length != 2)
                return 1;
            return 0;
        }

        public int DoEvent(int arg)
        {
            return this.E(arg);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.evnt.minuseql.return004.return004
{
    // <Area> Dynamic -- compound operator</Area>
    // <Title> compound operator +=/-= on event </Title>
    // <Description>
    // rhs is delegate invocation return delegate : without event accessor and rhs is outside the invocation list
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public delegate int Dele(int i);
    public class C
    {
        public static int Foo1(int i)
        {
            return i + 1;
        }

        public static int Foo2(int i)
        {
            return i + 2;
        }

        public static int Foo3(int i)
        {
            return i + 3;
        }

        public static Dele Bar()
        {
            return C.Foo3;
        }

        public C()
        {
            E += C.Foo1;
            E += C.Foo2;
        }

        public Dele E;

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new C();
            d.E -= C.Bar();
            var length = d.E.GetInvocationList().Length;
            var result = d.DoEvent(9);
            if (result != 11 && length != 2)
                return 1;
            return 0;
        }

        public int DoEvent(int arg)
        {
            return this.E(arg);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.evnt.minuseql.return005.return005
{
    // <Area> Dynamic -- compound operator</Area>
    // <Title> compound operator +=/-= on event </Title>
    // <Description>
    // rhs is delegate invocation return delegate : with event accessor and rhs is in invocation list
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public delegate int Dele(int i);
    public class C
    {
        public static int Foo1(int i)
        {
            return i + 1;
        }

        public static int Foo2(int i)
        {
            return i + 2;
        }

        public static int Foo3(int i)
        {
            return i + 3;
        }

        public static Dele Bar()
        {
            return new Dele(x => x + 2);
        }

        public C()
        {
            E += C.Foo1;
            E += C.Foo2;
        }

        public event Dele E;


        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new C();
            d.E -= C.Bar();
            var length = d.E.GetInvocationList().Length;
            var result = d.DoEvent(9);
            if (result != 11 && length != 2)
                return 1;
            return 0;
        }

        public int DoEvent(int arg)
        {
            return this.E(arg);
        }
    }
    // </Code>
}
