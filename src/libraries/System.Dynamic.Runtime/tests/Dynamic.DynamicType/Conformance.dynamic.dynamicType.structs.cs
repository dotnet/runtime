// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.strcts.strct001.strct001
{
    // <Title>Structs</Title>
    // <Description>
    //  is by design because of "d.s.Field = 4;" equals as "dynamic d2 = d.s; d2.Field = 4;" and
    // "object d2 = d.s; d2.Field = 4;", the modification occurs on the boxed object, so the origin value wasn't changed.
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
#pragma warning disable 0649
    public struct S
    {
        public int Field;
    }

    public class C
    {
        public S s;
        public S prop
        {
            get;
            set;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            int rez = 0;
            dynamic d = new S();
            d.Field = 3;
            if (d.Field != 3)
                rez += 1;
            //Struct as a field
            d = new C();
            d.s = default(S);
            d.s.Field = 4;
            //
            if (d.s.Field != 0)
                rez += 1;
            //Struct as a property
            d.prop = default(S);
            d.prop.Field = 5;
            //
            if (d.prop.Field != 0)
                rez += 1;
            return rez == 0 ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.strcts.strct002.strct002
{
    // <Title>Structs</Title>
    // <Description>
    //  is by design because of "d.s.Field = 4;" equals as "dynamic d2 = d.s; d2.Field = 4;" and
    // "object d2 = d.s; d2.Field = 4;", the modification occurs on the boxed object, so the origin value wasn't changed.
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
#pragma warning disable 0649
    public struct S
    {
        public int Field;
    }

    public class C<T>
    {
        public T s;
        public T prop
        {
            get;
            set;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            int rez = 0;
            //Struct as a field
            dynamic d = new C<S>();
            d.s = default(S);
            d.s.Field = 4;
            //
            if (d.s.Field != 0)
                rez += 1;
            //Struct as a property
            d.prop = default(S);
            d.prop.Field = 5;
            //
            if (d.prop.Field != 0)
                rez += 1;
            return rez == 0 ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.strcts.strct003.strct003
{
    // <Title>Structs</Title>
    // <Description>
    //  is by design because of "d.s.Field = 4;" equals as "dynamic d2 = d.s; d2.Field = 4;" and
    // "object d2 = d.s; d2.Field = 4;", the modification occurs on the boxed object, so the origin value wasn't changed.
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
#pragma warning disable 0649
    public struct S
    {
        public int Field;
        public struct S2
        {
            public float Foo;
        }

        public S2 Field2;
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            int rez = 0;
            dynamic d = new S();
            d.Field2 = default(S.S2);
            d.Field2.Foo = 4;
            //
            if (d.Field2.Foo != 0)
                rez++;
            return rez == 0 ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.strcts.strct004.strct004
{
    // <Title>Structs</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public struct S
    {
        public int x;
        public void Set(int val)
        {
            this.x = val;
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
            S dy = new S();
            dynamic val = 1;
            dy.Set(val);
            if (dy.x != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.strcts.strct005.strct005
{
    public class DynamicTest
    {
        public struct MyStruct
        {
            public int M(int p)
            {
                return p;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic x = 0;
            return new MyStruct().M(x);
        }
    }
    // </Code>
}
