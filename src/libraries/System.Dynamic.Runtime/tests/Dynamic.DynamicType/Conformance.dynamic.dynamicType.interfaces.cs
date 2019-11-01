// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.integeregererfaces.integeregererface001.integeregererface001
{
    // <Title>Interfaces</Title>
    // <Description>covariance between object/dynamic - implicit/explicit implementations
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public interface I
    {
        int Foo(object o);
        int Bar(dynamic o);
    }

    public struct C : I
    {
        public int Foo(dynamic d)
        {
            Test.Status = 1;
            return 1;
        }

        public int Bar(object d)
        {
            Test.Status = 2;
            return 2;
        }
    }

    public class CExp : I
    {
        int I.Foo(object d)
        {
            Test.Status = 3;
            return 3;
        }

        int I.Bar(dynamic d)
        {
            Test.Status = 4;
            return 4;
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
            dynamic d = new C();
            I i = d;
            bool ret = 1 == i.Foo(2);
            ret &= 2 == i.Bar(null);
            d = new CExp();
            i = d;
            ret &= 3 == i.Foo(2);
            ret &= 4 == i.Bar(null);
            return ret ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.integeregererfaces.integeregererface007.integeregererface007
{
    // <Title>Interfaces</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public interface I
    {
        dynamic Foo();
    }

    public class C : I
    {
        public object Foo()
        {
            Test.Status = 1;
            return 1;
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
            I i = new C();
            i.Foo();
            if (Test.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.integeregererfaces.integeregererface008.integeregererface008
{
    // <Title>Interfaces</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public interface I
    {
        dynamic Foo();
    }

    public class C : I
    {
        object I.Foo()
        {
            Test.Status = 1;
            return 1;
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
            I i = new C();
            i.Foo();
            if (Test.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.integeregererfaces.integeregererface009.integeregererface009
{
    // <Title>Interfaces</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public interface I
    {
        object Foo();
    }

    public class C : I
    {
        dynamic I.Foo()
        {
            Test.Status = 1;
            return 1;
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
            I i = new C();
            i.Foo();
            if (Test.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.integeregererfaces.integeregererface010.integeregererface010
{
    // <Title>Interfaces</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public interface I
    {
        int Foo(object o, int x);
    }

    public class C : I
    {
        public int Foo(dynamic d, int x)
        {
            Test.Status = 1;
            return 1;
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
            I i = new C();
            i.Foo(2, 3);
            if (Test.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.integeregererfaces.integeregererface011.integeregererface011
{
    // <Title>Interfaces</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public interface I
    {
        List<dynamic> Foo();
    }

    public class C : I
    {
        public List<dynamic> Foo()
        {
            Test.Status = 1;
            return new List<dynamic>();
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
            I i = new C();
            var x = i.Foo();
            if (Test.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.integeregererfaces.integeregererface012.integeregererface012
{
    // <Title>Interfaces</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public interface I
    {
        List<dynamic> Foo();
    }

    public class C : I
    {
        List<dynamic> I.Foo()
        {
            Test.Status = 1;
            return new List<dynamic>();
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
            I i = new C();
            var x = i.Foo();
            if (Test.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.integeregererfaces.integeregererface013.integeregererface013
{
    // <Title>Interfaces</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public interface I
    {
        List<dynamic> Foo();
    }

    public class C : I
    {
        List<object> I.Foo()
        {
            Test.Status = 1;
            return new List<dynamic>();
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
            I i = new C();
            var x = i.Foo();
            if (Test.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.integeregererfaces.integeregererface014.integeregererface014
{
    // <Title>Interfaces</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public interface I
    {
        List<object> Foo();
    }

    public class C : I
    {
        List<dynamic> I.Foo()
        {
            Test.Status = 1;
            return new List<object>();
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
            I i = new C();
            var x = i.Foo();
            if (Test.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.integeregererfaces.integeregererface015.integeregererface015
{
    // <Title>Interfaces</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public interface I
    {
        void Foo(List<object> o);
    }

    public class C : I
    {
        void I.Foo(List<object> o)
        {
            Test.Status = 1;
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
            I i = new C();
            i.Foo(new List<dynamic>());
            if (Test.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.integeregererfaces.integeregererface016.integeregererface016
{
    // <Title>Interfaces</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public interface I
    {
        void Foo(List<dynamic> o);
    }

    public class C : I
    {
        void I.Foo(List<object> o)
        {
            Test.Status = 1;
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
            I i = new C();
            i.Foo(new List<dynamic>());
            if (Test.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}
