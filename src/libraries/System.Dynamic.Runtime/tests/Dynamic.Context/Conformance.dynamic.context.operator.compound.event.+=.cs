// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.evnt.pluseql.dlgate001.dlgate001
{
    // <Area> Dynamic -- compound operator</Area>
    // <Title> compound operator +=/-= on event </Title>
    // <Description>
    // rhs is delegate creation expression;
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
            dynamic c = new C();
            c.E += new Dele(C.Foo);
            if (c.DoEvent(9) != 9)
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.evnt.pluseql.dlgate002.dlgate002
{
    // <Area> Dynamic -- compound operator</Area>
    // <Title> compound operator +=/-= on event </Title>
    // <Description>
    // rhs is delegate creation expression; (no event accessor declaration)
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public delegate int Dele(int i);
    public class C
    {
        public Dele E;
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
            dynamic c = new C();
            c.E += new Dele(C.Foo);
            if (c.DoEvent(9) != 9)
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.evnt.pluseql.dynamic001.dynamic001
{
    // <Area> Dynamic -- compound operator</Area>
    // <Title> compound operator +=/-= on event </Title>
    // <Description>
    // rhs is dynamic runtime delegate: with event accessor
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


        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new C();
            dynamic c = new C();
            c.E += (Dele)C.Foo;
            d.E += c.E;
            if (d.DoEvent(9) != 9)
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.evnt.pluseql.dynamic002.dynamic002
{
    // <Area> Dynamic -- compound operator</Area>
    // <Title> compound operator +=/-= on event </Title>
    // <Description>
    // rhs is dynamic runtime delegate: without event accessor
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public delegate int Dele(int i);
    public class C
    {
        public Dele E;
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
            dynamic c = new C();
            c.E += (Dele)C.Foo;
            d.E += c.E;
            if (d.DoEvent(9) != 9)
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.evnt.pluseql.dynamic003.dynamic003
{
    // <Area> Dynamic -- compound operator</Area>
    // <Title> compound operator +=/-= on event </Title>
    // <Description>
    // lhs is static typed and rhs is dynamic runtime delegate: with event accessor
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public delegate int Dele(int i);
    public class C
    {
        public static int Foo(int i)
        {
            return i;
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
            c.E += (Dele)C.Foo;
            d.E += c.E;
            if (d.DoEvent(9) != 9)
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.evnt.pluseql.dynamic004.dynamic004
{
    // <Area> Dynamic -- compound operator</Area>
    // <Title> compound operator +=/-= on event </Title>
    // <Description>
    // lhs is static typed and rhs is dynamic runtime delegate: without event accessor
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public delegate int Dele(int i);
    public class C
    {
        public static int Foo(int i)
        {
            return i;
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
            c.E += (Dele)C.Foo;
            d.E += c.E;
            if (d.DoEvent(9) != 9)
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.evnt.pluseql.fieldproperty001.fieldproperty001
{
    // <Area> Dynamic -- compound operator</Area>
    // <Title> compound operator +=/-= on event </Title>
    // <Description>
    // rhs is field/property of delegate type : with event accessor
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
            field = C.Foo;
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
            //c.E += C.Foo;
            C c = new C();
            d.E += c.field;
            if (d.DoEvent(9) != 9)
                return 1;
            d.E -= c.field;
            d.E += c.Field;
            if (d.DoEvent(9) != 9)
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.evnt.pluseql.fieldproperty002.fieldproperty002
{
    // <Area> Dynamic -- compound operator</Area>
    // <Title> compound operator +=/-= on event </Title>
    // <Description>
    // rhs is field/property of delegate type : without event accessor
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
            field = C.Foo;
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
            //c.E += C.Foo;
            C c = new C();
            d.E += c.field;
            if (d.DoEvent(9) != 9)
                return 1;
            d.E -= c.field;
            d.E += c.Field;
            if (d.DoEvent(9) != 9)
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.evnt.pluseql.null001.null001
{
    // <Area> Dynamic -- compound operator</Area>
    // <Title> compound operator +=/-= on event </Title>
    // <Description>
    // Negative: rhs is null literal : lhs is not null
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
            dynamic c = new C();
            c.E += (Dele)C.Foo;
            c.E += null;
            return 0;
        }

        public int DoEvent(int arg)
        {
            return this.E(arg);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.evnt.pluseql.null002.null002
{
    // <Area> Dynamic -- compound operator</Area>
    // <Title> compound operator +=/-= on event </Title>
    // <Description>
    // Negative: rhs is null literal : lhs is  null
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
            dynamic c = new C();
            int result = 0;
            try
            {
                c.E += null;
                result += 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.AmbigBinaryOps, e.Message, "+=", "<null>", "<null>"))
                    return 1;
            }

            return 0;
        }

        public int DoEvent(int arg)
        {
            return this.E(arg);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.evnt.pluseql.return001.return001
{
    // <Area> Dynamic -- compound operator</Area>
    // <Title> compound operator +=/-= on event </Title>
    // <Description>
    // rhs is delegate invocation return delegate : with event accessor
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public delegate int Dele(int i);
    public class C
    {
        public static int Foo(int i)
        {
            return i;
        }

        public static Dele Bar()
        {
            return C.Foo;
        }

        public event Dele E;

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new C();
            d.E += C.Bar();
            if (d.DoEvent(9) != 9)
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.evnt.pluseql.return002.return002
{
    // <Area> Dynamic -- compound operator</Area>
    // <Title> compound operator +=/-= on event </Title>
    // <Description>
    // rhs is delegate invocation return delegate : without event accessor
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public delegate int Dele(int i);
    public class C
    {
        public static int Foo(int i)
        {
            return i;
        }

        public static Dele Bar()
        {
            return C.Foo;
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
            d.E += C.Bar();
            if (d.DoEvent(9) != 9)
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
