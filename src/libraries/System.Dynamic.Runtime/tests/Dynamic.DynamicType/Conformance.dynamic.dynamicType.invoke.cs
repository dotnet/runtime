// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.invoke.invoke001.invoke001
{
    // <Title> Fields and properties of dynamic type are invokeable</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public class Test
    {
        public static bool Called = false;
        public static void CallMe(int x)
        {
            if (x == 1)
                Test.Called = true;
        }
    }

    public class B
    {
        public dynamic Foo
        {
            get;
            set;
        }

        public dynamic Bar;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int tests = 0, success = 0;
            var y = new B();
            dynamic d = new B();
            //using the property
            tests++;
            Test.Called = false;
            y.Foo = (Action<int>)Test.CallMe;
            y.Foo(1);
            if (Test.Called == true)
                success++;
            tests++;
            Test.Called = false;
            d.Foo = (Action<int>)Test.CallMe;
            d.Foo(1);
            if (Test.Called == true)
                success++;
            //using the field
            tests++;
            Test.Called = false;
            y.Bar = (Action<int>)Test.CallMe;
            y.Bar(1);
            if (Test.Called == true)
                success++;
            tests++;
            Test.Called = false;
            d.Bar = (Action<int>)Test.CallMe;
            d.Bar(1);
            if (Test.Called == true)
                success++;
            return tests == success ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.invoke.invoke002.invoke002
{
    // <Title> Fields and properties of dynamic type are invokeable</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public class Test
    {
        public static bool Called = false;
        public static void CallMe(int x)
        {
            if (x == 1)
                Test.Called = true;
        }
    }

    public class B
    {
        public dynamic Foo = (Action<int>)Test.CallMe;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int tests = 0, success = 0;
            var y = new B();
            dynamic d = new B();
            //using the initialized field
            tests++;
            Test.Called = false;
            y.Foo = (Action<int>)Test.CallMe;
            y.Foo(1);
            if (Test.Called == true)
                success++;
            tests++;
            Test.Called = false;
            d.Foo = (Action<int>)Test.CallMe;
            d.Foo(1);
            if (Test.Called == true)
                success++;
            return tests == success ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.invoke.invoke003.invoke003
{
    // <Title> Fields and properties of dynamic type are invokeable</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public class Test
    {
        public static bool Called = false;
        public static void CallMe(int x)
        {
            if (x == 1)
                Test.Called = true;
        }
    }

    public class B
    {
        public dynamic Bar = (Action<int>)Test.CallMe;
        public event Action<int> Foo;


        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int tests = 0, success = 0;
            var y = new B();
            dynamic d = new B();
            //using the initialized field
            tests++;
            Test.Called = false;
            y.Foo += y.Bar;
            y.Foo(1);
            if (Test.Called == true)
                success++;
            tests++;
            Test.Called = false;
            d.Foo += y.Bar;
            d.Foo(1);
            if (Test.Called == true)
                success++;
            System.Console.WriteLine(tests == success);
            return tests == success ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.invoke.invoke004.invoke004
{
    // <Title> Fields and properties of dynamic type are invokeable</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(30,19\).*CS0108</Expects>
    //<Expects Status=warning>\(31,20\).*CS0108</Expects>
    using System;

    public class Test
    {
        public static int State = 0;
        public static void CallMe(int x)
        {
            Test.State = 1;
        }
    }

    public class Base
    {
        public void Foo(int x)
        {
            Test.State = 2;
        }

        public void Bar(int x)
        {
            Test.State = 3;
        }
    }

    public class B : Base
    {
        public object Foo = (Action<int>)Test.CallMe; //this is not considered invocable
        public dynamic Bar = (Action<int>)Test.CallMe; //this is considered invocable
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int tests = 0, success = 0;
            var y = new B();
            dynamic d = new B();
            tests++;
            Test.State = 0;
            y.Foo(1);
            if (Test.State == 2) //we should invoke the base method
                success++;
            tests++;
            Test.State = 0;
            d.Foo(1);
            if (Test.State == 2) //we should invoke the base method
                success++;
            tests++;
            Test.State = 0;
            y.Bar(1);
            if (Test.State == 1) //we should invoke the delegate
                success++;
            tests++;
            Test.State = 0;
            d.Bar(1);
            if (Test.State == 1) //we should invoke the delegate
                success++;
            return tests == success ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.invoke.invoke005.invoke005
{
    // <Title> Fields and properties of dynamic type are invokeable</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(30,19\).*CS0108</Expects>
    //<Expects Status=warning>\(31,20\).*CS0108</Expects>
    using System;

    public class Test
    {
        public static int State = 0;
        public static void CallMe(int x)
        {
            Test.State = 1;
        }
    }

    public class Base
    {
        public void Foo(int x)
        {
            Test.State = 2;
        }

        public void Bar(int x)
        {
            Test.State = 3;
        }
    }

    public class B : Base
    {
        public object Foo = (Action<int>)Test.CallMe; //this is not considered invocable
        public dynamic Bar = (Action<int>)Test.CallMe; //this is considered invocable
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int tests = 0, success = 0;
            var y = new B();
            dynamic d = new B();
            tests++;
            Test.State = 0;
            y.Foo(x: 1);
            if (Test.State == 2) //we should invoke the base method
                success++;
            tests++;
            Test.State = 0;
            d.Foo(x: 1);
            if (Test.State == 2) //we should invoke the base method
                success++;
            tests++;
            Test.State = 0;
            y.Bar(obj: 1);
            if (Test.State == 1) //we should invoke the delegate
                success++;
            tests++;
            Test.State = 0;
            d.Bar(obj: 1);
            if (Test.State == 1) //we should invoke the delegate
                success++;
            return tests == success ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.invoke.invoke006.invoke006
{
    // <Title> Fields and properties of dynamic type are invokeable</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;
    using System.Collections.Generic;

    public class Test
    {
        public static int State = 0;
        public static void CallMe(int x)
        {
            Test.State = 1;
        }
    }

    public class B
    {
        public List<dynamic> Foo = new List<object>()
        {
        (Action<int>)Test.CallMe, (Action<int>)(x => Test.CallMe(x))}

        ;
        public dynamic[] Bar = new dynamic[]
        {
        (Action<int>)Test.CallMe, (Action<int>)(x => Test.CallMe(x))}

        ;
        public dynamic this[int x]
        {
            get
            {
                return (Action<int>)Test.CallMe;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int tests = 0, success = 0;
            var y = new B();
            dynamic d = new B();
            tests++;
            Test.State = 0;
            y.Foo[0](obj: 1);
            if (Test.State == 1)
                success++;
            tests++;
            Test.State = 0;
            d.Foo[0](obj: 1);
            if (Test.State == 1)
                success++;
            tests++;
            Test.State = 0;
            y.Bar[1](obj: 1);
            if (Test.State == 1)
                success++;
            tests++;
            Test.State = 0;
            d.Bar[1](obj: 1);
            if (Test.State == 1)
                success++;
            tests++;
            Test.State = 0;
            y[0](54);
            if (Test.State == 1)
                success++;
            return tests == success ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.invoke.invoke007.invoke007
{
    // <Title> Make sure that the right member is picked </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(25,20\).*CS0649</Expects>

    public class A
    {
        public int Foo()
        {
            return 0;
        }

        public int Bar()
        {
            return 0;
        }
    }

    public class B : A
    {
        public new int Foo;
        public new int Bar
        {
            get;
            set;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int rez = 0;
            dynamic y = new B();
            rez += y.Foo();
            rez += y.Bar();
            return rez;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.invoke.invoke008.invoke008
{
    // <Title>Calling Invoke method</Title>
    // <Description></Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects status=success></Expects>
    // <Code>

    public class C
    {
        public int Invoke(object obj, params object[] args)
        {
            return 0;
        }
    }

    public class Program
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic foo = new object();
            C c = new C();
            return c.Invoke(foo, 5);
        }
    }
    //</Code>
}
