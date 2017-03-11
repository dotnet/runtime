// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.extractDynamic.extract001.extract001
{
    // <Title>Extract a dynamic element from a generic type</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public class A
    {
        public void Foo()
        {
            Test.Status = 1;
        }
    }

    public class B
    {
        public void Foo()
        {
            Test.Status = 2;
        }
    }

    public class Test
    {
        public static int Status;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            List<dynamic> myList = new List<dynamic>()
            {
            new A(), new B()}

            ;
            int i = 1;
            foreach (var item in myList)
            {
                item.Foo();
                if (i++ != Test.Status)
                    return 1;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.extractDynamic.extract002.extract002
{
    // <Title>Extract a dynamic element from a generic type</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public class A
    {
        public void Foo()
        {
            Test.Status = 1;
        }
    }

    public class B
    {
        public void Foo()
        {
            Test.Status = 2;
        }
    }

    public class Test
    {
        public static int Status;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            List<object> myList = new List<object>()
            {
            new A(), new B()}

            ;
            int i = 1;
            foreach (dynamic item in myList)
            {
                item.Foo();
                if (i++ != Test.Status)
                    return 1;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.extractDynamic.extract003.extract003
{
    // <Title>Extract a dynamic element from a generic type</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public class A
    {
        public void Foo()
        {
            Test.Status = 1;
        }
    }

    public class B
    {
        public void Foo()
        {
            Test.Status = 2;
        }
    }

    public class Test
    {
        public static int Status;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            List<dynamic> myList = new List<object>()
            {
            new A(), new B()}

            ;
            int i = 1;
            foreach (var item in myList)
            {
                item.Foo();
                if (i++ != Test.Status)
                    return 1;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.extractDynamic.extract004.extract004
{
    // <Title>Extract a dynamic element from a generic type</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public class A
    {
        public void Foo()
        {
            Test.Status = 1;
        }
    }

    public class B
    {
        public void Foo()
        {
            Test.Status = 2;
        }
    }

    public class Foo
    {
        public List<dynamic> GetList()
        {
            return new List<dynamic>()
            {
            new A(), new B()}

            ;
        }
    }

    public class Test
    {
        public static int Status;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            Foo f = new Foo();
            int i = 1;
            foreach (var item in f.GetList())
            {
                item.Foo();
                if (i++ != Test.Status)
                    return 1;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.extractDynamic.extract005.extract005
{
    // <Title>Extract a dynamic element from a generic type</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public class A
    {
        public void Foo()
        {
            Test.Status = 1;
        }
    }

    public class B
    {
        public void Foo()
        {
            Test.Status = 2;
        }
    }

    public class Foo
    {
        public List<dynamic> GetList()
        {
            return new List<object>()
            {
            new A(), new B()}

            ;
        }
    }

    public class Test
    {
        public static int Status;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            Foo f = new Foo();
            int i = 1;
            foreach (var item in f.GetList())
            {
                item.Foo();
                if (i++ != Test.Status)
                    return 1;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.extractDynamic.extract006.extract006
{
    // <Title>Extract a dynamic element from a generic type</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public class A
    {
        public void Foo()
        {
            Test.Status = 1;
        }
    }

    public class B
    {
        public void Foo()
        {
            Test.Status = 2;
        }
    }

    public class Foo
    {
        public IEnumerable<dynamic> GetList()
        {
            return new List<dynamic>()
            {
            new A(), new B()}

            ;
        }
    }

    public class Test
    {
        public static int Status;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            Foo f = new Foo();
            int i = 1;
            foreach (var item in f.GetList())
            {
                item.Foo();
                if (i++ != Test.Status)
                    return 1;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.extractDynamic.extract007.extract007
{
    // <Title>Extract a dynamic element from a generic type</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public class A
    {
        public void Foo()
        {
            Test.Status = 1;
        }
    }

    public class B
    {
        public void Foo()
        {
            Test.Status = 2;
        }
    }

    public class Foo
    {
        public IEnumerable<dynamic> GetList()
        {
            return new List<object>()
            {
            new A(), new B()}

            ;
        }
    }

    public class Test
    {
        public static int Status;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            Foo f = new Foo();
            int i = 1;
            foreach (var item in f.GetList())
            {
                item.Foo();
                if (i++ != Test.Status)
                    return 1;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.extractDynamic.extract008.extract008
{
    // <Title>Extract a dynamic element from a generic type</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public class A
    {
        public void Foo()
        {
            Test.Status = 1;
        }
    }

    public class Foo
    {
        public List<Dictionary<string, List<dynamic>>> GetSomething()
        {
            var list = new List<Dictionary<string, List<dynamic>>>();
            var dict = new Dictionary<string, List<dynamic>>();
            dict.Add("Test", new List<dynamic>()
            {
            new A()}

            );
            list.Add(dict);
            return list;
        }
    }

    public class Test
    {
        public static int Status;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            Foo f = new Foo();
            var list = f.GetSomething();
            list[0]["Test"][0].Foo();
            if (Test.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.extractDynamic.extract009.extract009
{
    // <Title>Extract a dynamic element from a generic type</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public class A
    {
        public void Foo()
        {
            Test.Status = 1;
        }
    }

    public class Foo
    {
        public List<Dictionary<string, List<dynamic>>> GetSomething()
        {
            var list = new List<Dictionary<string, List<object>>>();
            var dict = new Dictionary<string, List<object>>();
            dict.Add("Test", new List<object>()
            {
            new A()}

            );
            list.Add(dict);
            return list;
        }
    }

    public class Test
    {
        public static int Status;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            Foo f = new Foo();
            var list = f.GetSomething();
            list[0]["Test"][0].Foo();
            if (Test.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.extractDynamic.extract010.extract010
{
    // <Title>Extract a dynamic element from a generic type</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public class A
    {
        public void Foo()
        {
            Test.Status = 1;
        }
    }

    public class B
    {
        public void Foo()
        {
            Test.Status = 2;
        }
    }

    public class Foo
    {
        public List<Dictionary<dynamic, Dictionary<string, List<dynamic>>>> GetSomething()
        {
            var list = new List<Dictionary<dynamic, Dictionary<string, List<dynamic>>>>();
            var dict = new Dictionary<dynamic, Dictionary<string, List<dynamic>>>();
            var dict2 = new Dictionary<string, List<dynamic>>();
            list.Add(dict);
            dict.Add("bar", dict2);
            dict2.Add("foo", new List<dynamic>()
            {
            new A(), new B()}

            );
            return list;
        }
    }

    public class Test
    {
        public static int Status;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            Foo f = new Foo();
            var list = f.GetSomething();
            list[0]["bar"]["foo"][0].Foo();
            if (Test.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.generics.extractDynamic.extract011.extract011
{
    // <Title>Extract a dynamic element from a generic type</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class A
    {
        public void Foo()
        {
            Test.Status = 1;
        }
    }

    public class B
    {
        public void Foo()
        {
            Test.Status = 2;
        }
    }

    public class C<T>
    {
        public T t;
        public T getT()
        {
            return t;
        }

        public void Add(T tt)
        {
            t = tt;
        }
    }

    public class D<T, U>
    {
        public T t;
        public U u;
        public T getT()
        {
            return t;
        }

        public U getU()
        {
            return u;
        }

        public void Add(T tt, U uu)
        {
            t = tt;
            u = uu;
        }
    }

    public class Foo
    {
        public C<D<dynamic, D<string, C<dynamic>>>> Get()
        {
            var list = new C<D<dynamic, D<string, C<dynamic>>>>();
            var dict = new D<dynamic, D<string, C<dynamic>>>();
            var dict2 = new D<string, C<dynamic>>();
            var test = new C<dynamic>();
            test.Add(new A());
            list.Add(dict);
            dict.Add("bar", dict2);
            dict2.Add("foo", test);
            return list;
        }
    }

    public class Test
    {
        public static int Status;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            Foo f = new Foo();
            var list = f.Get();
            list.getT().getU().getU().getT().Foo();
            if (Test.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}
