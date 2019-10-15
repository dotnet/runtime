// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.unsfe.basic.anonytype01.anonytype01
{
    unsafe // <Area> dynamic in unsafe code </Area>
           // <Title> unsafe type </Title>
           // <Description>
           // anonymous type
           // </Description>
           //<Expects Status=success></Expects>
           // <Code>
           //<Expects Status=warning>\(12,17\).*CS0649</Expects>
public class UC
    {
        public int* p;
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
            dynamic x = new
            {
                P = new UC()
            }

            ;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.unsfe.basic.array01.array01
{
    unsafe // <Area> dynamic in unsafe code </Area>
           // <Title> unsafe type </Title>
           // <Description>
           // array initializer : unsafe array initializer with dynamic
           // </Description>
           //<Expects Status=success></Expects>
           // <Code>
           //<Expects Status=warning>\(12,17\).*CS0649</Expects>
public class US
    {
        public int* p;
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
            dynamic d1 = new US();
            dynamic d2 = new US();
            US[] array =
            {
            d1, d2, new US()}

            ;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.unsfe.basic.array02.array02
{
    unsafe // <Area> dynamic in unsafe code </Area>
           // <Title> unsafe type </Title>
           // <Description>
           // array initializer : dynamic array initializer with unsafe
           // </Description>
           //<Expects Status=success></Expects>
           // <Code>
           //<Expects Status=warning>\(12,17\).*CS0649</Expects>
public class US
    {
        public int* p;
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
            dynamic[] array =
            {
            new US(), new US()}

            ;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.unsfe.basic.attribute01.attribute01
{
    // <Area> dynamic in unsafe code </Area>
    // <Title> unsafe type </Title>
    // <Description>
    // attribute
    // </Description>
    //<Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(20,17\).*CS0649</Expects>
    using System;

    [AttributeUsage(AttributeTargets.Class)]
    public class MyAttr : System.Attribute
    {
    }

    [MyAttr]
    public unsafe class US
    {
        public int* p;
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
            dynamic d1 = new US();
            US u = d1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.unsfe.basic.collection01.collection01
{
    // <Area> dynamic in unsafe code </Area>
    // <Title> unsafe type </Title>
    // <Description>
    // collection initializer : dynamic collection initializer with unsafe
    // </Description>
    //<Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(14,17\).*CS0649</Expects>
    using System.Collections.Generic;

    public unsafe class US
    {
        public int* p;
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
            List<dynamic> col = new List<dynamic>
            {
            new US(), new US()}

            ;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.unsfe.basic.collection02.collection02
{
    // <Area> dynamic in unsafe code </Area>
    // <Title> unsafe type </Title>
    // <Description>
    // collection initializer : unsafe type collection initializer with dynamic
    // </Description>
    //<Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(14,17\).*CS0649</Expects>
    using System.Collections.Generic;

    public unsafe class US
    {
        public int* p;
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
            dynamic d = new US();
            List<US> col = new List<US>
            {
            d, d
            }

            ;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.unsfe.basic.collection03.collection03
{
    // <Area> dynamic in unsafe code </Area>
    // <Title> unsafe type </Title>
    // <Description>
    // collection initializer : unsafe type collection initializer with dynamic
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(17,17\).*CS0649</Expects>
    using System.Collections.Generic;
    using Microsoft.CSharp.RuntimeBinder;

    public unsafe class US
    {
        public int* p;
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
            dynamic d1 = 1;
            dynamic d2 = "hi";
            try
            {
                List<US> col = new List<US>
                {
                d1, d2
                }

                ;
            }
            catch (RuntimeBinderException ex)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadArgTypes, ex.Message, "System.Collections.Generic.List<US>.Add(US)");
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.unsfe.basic.ctor01.ctor01
{
    // <Area> dynamic in unsafe code </Area>
    // <Title> unsafe type </Title>
    // <Description>
    // ctor - pointer as arg
    // </Description>
    //<Expects Status=success></Expects>
    // <Code>
    public unsafe class C
    {
        public int* p;
        public C(int* q)
        {
            p = q;
        }
    }

    public unsafe class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            //int num = 5;
            //int* p = &num;
            //dynamic d = new C(p);
            return 0;
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.unsfe.basic.ctor02.ctor02
{
    // <Area> dynamic in unsafe code </Area>
    // <Title> unsafe type </Title>
    // <Description>
    // ctor - dynamic as arg
    // </Description>
    //<Expects Status=success></Expects>
    // <Code>
    public unsafe class C
    {
        public dynamic p;
        public C(dynamic q)
        {
            p = q;
        }
    }

    public unsafe class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            int num = 5;
            dynamic d = new C(num);
            return 0;
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.unsfe.basic.ctor03.ctor03
{
    // <Area> dynamic in unsafe code </Area>
    // <Title> unsafe type </Title>
    // <Description>
    // ctor - mixed dynamic and pointer as arg
    // </Description>
    //<Expects Status=success></Expects>
    // <Code>
    public unsafe class C
    {
        public dynamic d;
        public int* p;
        public C(dynamic x, int* y)
        {
            d = x;
            p = y;
        }
    }

    public unsafe class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            int num = 5;
            int* p = &num;
            dynamic d = new C(num, p);
            return 0;
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.unsfe.basic.dlgate01.dlgate01
{
    // <Area> dynamic in unsafe code </Area>
    // <Title> unsafe type</Title>
    // <Description>
    // delegate
    // </Description>
    //<Expects Status=success></Expects>
    // <Code>
    internal unsafe delegate void Foo(int* p);
    public unsafe class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = (Foo)Test.Bar;
            return 0;
        }

        public static void Bar(int* q)
        {
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.unsfe.basic.dtor01.dtor01
{
    // <Area> dynamic in unsafe code </Area>
    // <Title> unsafe type </Title>
    // <Description>
    // dtor
    // </Description>
    //<Expects Status=success></Expects>
    // <Code>
    public class C
    {
        unsafe ~C()
        {
            int num = 5;
            int* ptr = &num;
        }
    }

    public unsafe class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new C();
            return 0;
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.unsfe.basic.evnt01.evnt01
{
    // <Area> dynamic in unsafe code </Area>
    // <Title> unsafe type</Title>
    // <Description>
    // delegate
    // </Description>
    //<Expects Status=success></Expects>
    // <Code>
    internal unsafe delegate void Foo(int* p);
    public unsafe class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = (Foo)Test.Bar;
            return 0;
        }

        public static void Bar(int* q)
        {
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.unsfe.basic.explicit01.explicit01
{
    unsafe // <Area> dynamic in unsafe code </Area>
           // <Title> unsafe type </Title>
           // <Description>
           // explicit conversion
           // </Description>
           //<Expects Status=success></Expects>
           // <Code>
           //<Expects Status=warning>\(12,17\).*CS0649</Expects>
public class US
    {
        public int* p;
        public static explicit operator int (US u)
        {
            return 1;
        }

        public static explicit operator US(int i)
        {
            return new US();
        }
    }

    public unsafe class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            US u = new US();
            dynamic d = (int)u;
            dynamic x = u;
            int i = (int)x;
            if (i != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.unsfe.basic.expressiontree01.expressiontree01
{
    // <Area> dynamic in unsafe code </Area>
    // <Title> unsafe type </Title>
    // <Description>
    // expression tree
    // </Description>
    //<Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(15,17\).*CS0649</Expects>
    using System;
    using System.Linq.Expressions;

    public unsafe struct UC
    {
        public int* p;
    }

    public unsafe class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            Expression<Func<dynamic, UC>> f = x => new UC();
            dynamic dyn = 10;
            f.Compile()(dyn);
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.unsfe.basic.field01.field01
{
    // <Area> dynamic in unsafe code </Area>
    // <Title> unsafe type </Title>
    // <Description>
    // filed (static & non-static)
    // </Description>
    //<Expects Status=success></Expects>
    // <Code>
    public class C
    {
        public unsafe int* p;
        public static unsafe char* q;
    }

    public unsafe class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new C();
            return 0;
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.unsfe.basic.fieldinit01.fieldinit01
{
    unsafe // <Area> dynamic in unsafe code </Area>
           // <Title>unsafe context</Title>
           // <Description>
           // dynamic in field initializer
           // </Description>
           // <RelatedBug></RelatedBug>
           //<Expects Status=success></Expects>
           // <Code>
public class C
    {
        public dynamic field = 10;
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
            C c = new C();
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.unsfe.basic.implicit01.implicit01
{
    unsafe // <Area> dynamic in unsafe code </Area>
           // <Title> unsafe type </Title>
           // <Description>
           // implicit conversion
           // </Description>
           //<Expects Status=success></Expects>
           // <Code>
           //<Expects Status=warning>\(12,17\).*CS0649</Expects>
public class US
    {
        public int* p;
        public static implicit operator int (US u)
        {
            return 1;
        }

        public static implicit operator US(int i)
        {
            return new US();
        }
    }

    public unsafe class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            US u = new US();
            dynamic x = u;
            int i = x;
            if (i != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.unsfe.basic.indexer02.indexer02
{
    // <Area> dynamic in unsafe code </Area>
    // <Title> unsafe type </Title>
    // <Description>
    // indexer - dynamic as index
    // </Description>
    //<Expects Status=success></Expects>
    // <Code>
    public class C
    {
        public const int field = 10;
        public unsafe int* this[int[] index]
        {
            get
            {
                fixed (int* p = index)
                {
                    return p;
                }
            }
        }
    }

    public static class D
    {
        public static int field = 1;
    }

    public unsafe class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            C d = new C();
            int[] array = new[]
            {
            1, 2, 3
            }

            ;
            int* x = ((C)d)[array];
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.unsfe.basic.indexer04.indexer04
{
    // <Area> dynamic in unsafe code </Area>
    // <Title> unsafe type </Title>
    // <Description>
    // indexer - pointer as return value
    // </Description>
    //<Expects Status=success></Expects>
    // <Code>
    public class Unsafe
    {
        public unsafe int* this[int index]
        {
            get
            {
                int temp = 10;
                return &temp;
            }

            set
            {
            }
        }
    }

    public unsafe class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new Unsafe();
            bool ret = true;
            try
            {
                var p = d[1];
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                ret = ErrorVerifier.Verify(ErrorMessageId.UnsafeNeeded, ex.Message);
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.unsfe.basic.integeregererface02.integeregererface02
{
    // <Area> dynamic in unsafe code </Area>
    // <Title> unsafe type</Title>
    // <Description>
    // interface - method with dynamic
    // </Description>
    //<Expects Status=success></Expects>
    // <Code>
    public unsafe interface IF
    {
        void Foo(dynamic p);
    }

    public unsafe class C : IF
    {
        public void Foo(dynamic p)
        {
        }
    }

    public unsafe class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            IF i = new C();
            dynamic d = i;
            d.Foo(i);
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.unsfe.basic.lambda01.lambda01
{
    // <Area> dynamic in unsafe code </Area>
    // <Title> unsafe type </Title>
    // <Description>
    // lambda expression
    // </Description>
    //<Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(14,17\).*CS0649</Expects>
    using System;

    public unsafe class UC
    {
        public int* p;
    }

    public unsafe class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            Func<dynamic, UC> f1 = x => new UC();
            f1(1);
            Func<UC, dynamic> f2 = x => x;
            f2(new UC());
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.unsfe.basic.lambda02.lambda02
{
    // <Area> dynamic in unsafe code </Area>
    // <Title> unsafe type </Title>
    // <Description>
    // lambda expression
    // </Description>
    //<Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(14,17\).*CS0649</Expects>
    using System;

    public unsafe class UC
    {
        public int* p;
    }

    public unsafe class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            Func<int, int> f = x =>
            {
                int* p = &x;
                return *p;
            }

            ;
            dynamic dyn = 10;
            int result = f(dyn);
            if (result == 10)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.unsfe.basic.method02.method02
{
    // <Area> dynamic in unsafe code </Area>
    // <Title> unsafe type </Title>
    // <Description>
    // method (static & non-static) - dynamic as arg
    // </Description>
    //<Expects Status=success></Expects>
    // <Code>
    public class C
    {
        public unsafe int Foo(dynamic p)
        {
            return 1;
        }

        public static unsafe int Bar(dynamic p)
        {
            return 2;
        }
    }

    public unsafe class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new C();
            int num = 5;
            int result = d.Foo(num) + C.Bar(d);
            if (result != 3)
                return 1;
            return 0;
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.unsfe.basic.method05.method05
{
    // <Area> dynamic in unsafe code </Area>
    // <Title> unsafe type </Title>
    // <Description>
    // method (static & non-static) - dynamic as return type
    // </Description>
    //<Expects Status=success></Expects>
    // <Code>
    public class C
    {
        public static int field = 10;
        public unsafe dynamic Foo()
        {
            return 1;
        }

        public static unsafe dynamic Bar()
        {
            return 2;
        }
    }

    public unsafe class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new C();
            d.Foo();
            C.Bar();
            return 0;
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.unsfe.basic.method07.method07
{
    unsafe // <Area> dynamic in unsafe code </Area>
           // <Title> unsafe type </Title>
           // <Description>
           // method  - unsafe type as arg : extension method
           // </Description>
           //<Expects Status=success></Expects>
           // <Code>
           //<Expects Status=warning>\(12,17\).*CS0649</Expects>
public class US
    {
        public int* ptr;
    }

    public static class Ext
    {
        public static void Foo(this US u, dynamic d)
        {
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
            US u = new US();
            u.Foo(u);
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.unsfe.basic.method08.method08
{
    // <Area> dynamic in unsafe code </Area>
    // <Title> unsafe type </Title>
    // <Description>
    // method  - OPTIONAL param
    // </Description>
    // <RelatedBug></RelatedBug>
    //<Expects Status=success></Expects>
    // <Code>
    using Microsoft.CSharp.RuntimeBinder;

    public unsafe class Test
    {
        public void Foo(void* ptr = null)
        {
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new Test();
            bool ret = true;
            try
            {
                d.Foo();
            }
            catch (RuntimeBinderException ex)
            {
                ret = ErrorVerifier.Verify(ErrorMessageId.UnsafeNeeded, ex.Message);
                if (ret)
                    return 0;
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.unsfe.basic.objinit01.objinit01
{
    // <Area> dynamic in unsafe code </Area>
    // <Title> unsafe type </Title>
    // <Description>
    // object initializer
    // </Description>
    //<Expects Status=success></Expects>
    // <Code>

    public unsafe class US
    {
        public int* p;
    }

    public unsafe class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            int num = 10;
            dynamic u = new US
            {
                p = &num
            }

            ;
            return 0;
        }
    }
    // </Code>
}
