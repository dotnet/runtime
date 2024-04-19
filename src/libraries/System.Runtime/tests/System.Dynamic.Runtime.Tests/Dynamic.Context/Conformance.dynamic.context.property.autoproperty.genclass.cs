// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.autoproperty.genclass.genclassautoprop.genclassautoprop;
using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.autoproperty.genclass.genclassautoprop.genclassautoprop
{
    public class MyClass
    {
        public int Field = 0;
    }

    public struct MyStruct
    {
        public int Number;
    }

    public enum MyEnum
    {
        First = 1,
        Second = 2,
        Third = 3
    }

    public class MemberClass<T>
    {
        public string Property_string
        {
            set;
            get;
        }

        public float?[] Property_FloatNullArr
        {
            get;
            set;
        }

        public dynamic Property_Dynamic
        {
            get;
            set;
        }

        public T Property_T
        {
            get;
            set;
        }
        // Move declarations to the call site
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.autoproperty.genclass.genclass001.genclass001
{
    // <Title> Tests generic class auto property used in anonymous method.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            MemberClass<int> mc = new MemberClass<int>();
            mc.Property_string = "Test";
            dynamic dy = mc;
            Func<string> func = delegate ()
            {
                return dy.Property_string;
            }

            ;
            if (func() == "Test")
                return 0;
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.autoproperty.genclass.genclass002.genclass002
{
    // <Title> Tests generic class auto property used in query expression.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System.Linq;
    using System.Collections.Generic;

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            var list = new List<string>()
            {
            null, "b", null, "a"
            }

            ;
            var mc = new MemberClass<string>();
            mc.Property_Dynamic = "a";
            dynamic dy = mc;
            var result = list.Where(p => p == (string)dy.Property_Dynamic).ToList();
            if (result.Count == 1 && result[0] == "a")
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.autoproperty.genclass.genclass003.genclass003
{
    // <Title> Tests generic class auto property used in collection initializer.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            MemberClass<string> mc = new MemberClass<string>();
            mc.Property_string = "Test1";
            mc.Property_T = "Test2";
            mc.Property_Dynamic = "Test3";
            dynamic dy = mc;
            List<string> list = new List<string>()
            {
                (string)dy.Property_string,
                (string)dy.Property_T,
                (string)dy.Property_Dynamic
            }

            ;
            if (list.Count == 3 && list[0] == "Test1" && list[1] == "Test2" && list[2] == "Test3")
                return 0;
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.autoproperty.genclass.genclass005.genclass005
{
    // <Title> Tests generic class auto property used in lambda.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public class Test
    {
        private int _field;

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            var mc = new MemberClass<Test>();
            dynamic dy = mc;
            dy.Property_T = new Test()
            {
                _field = 10
            }

            ;
            Func<int, int, Test> func = (int arg1, int arg2) => dy.Property_T;
            if (func(1, 2)._field == 10)
                return 0;
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.autoproperty.genclass.genclass006.genclass006
{
    // <Title> Tests generic class auto property used in using block.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System.IO;

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            MemberClass<bool> mc = new MemberClass<bool>();
            mc.Property_string = "abc";
            mc.Property_T = true;
            dynamic dy = mc;
            string result = null;
            using (MemoryStream sm = new MemoryStream())
            {
                using (StreamWriter sw = new StreamWriter(sm))
                {
                    sw.WriteLine((string)dy.Property_string);
                    sw.Flush();
                    sm.Position = 0;
                    using (StreamReader sr = new StreamReader(sm, (bool)dy.Property_T))
                    {
                        result = sr.ReadToEnd();
                    }
                }
            }

            if (result.Trim() == "abc")
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.autoproperty.genclass.genclass007.genclass007
{
    // <Title> Tests generic class auto property used inside #if, #else block.</Title>
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
            return Test.TestMethod() == 1 ? 0 : 1;
        }

        public static int TestMethod()
        {
#if c1
MemberClass<int> mc = new MemberClass<int>();
mc.Property_T = 0;
dynamic dy = mc;
return dy.Property_T;
 #else
            MemberClass<int> mc = new MemberClass<int>();
            mc.Property_T = 1;
            dynamic dy = mc;
            return dy.Property_T;
#endif
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.autoproperty.genclass.genclass008.genclass008
{
    // <Title> Tests generic class auto property used in arguments to method invocation.</Title>
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
            Test t = new Test();
            var mc = new MemberClass<int>();
            mc.Property_T = 2;
            mc.Property_string = "a";
            dynamic dy = mc;
            int result1 = t.TestMethod(dy.Property_T);
            int result2 = Test.TestMethod(dy.Property_string);
            if (result1 == 0 && result2 == 0)
                return 0;
            else
                return 1;
        }

        public int TestMethod(int i)
        {
            if (i == 2)
            {
                return 0;
            }
            else
            {
                return 1;
            }
        }

        public static int TestMethod(string s)
        {
            if (s == "a")
            {
                return 0;
            }
            else
            {
                return 1;
            }
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.autoproperty.genclass.genclass009.genclass009
{
    // <Title> Tests generic class auto property used in implicitly-typed variable initializer.</Title>
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
            var mc = new MemberClass<string>();
            mc.Property_Dynamic = 10;
            mc.Property_string = null;
            mc.Property_T = string.Empty;
            dynamic dy = mc;
            var result = new object[]
            {
            dy.Property_Dynamic, dy.Property_string, dy.Property_T
            }

            ;
            if (result.Length == 3 && (int)result[0] == 10 && result[1] == null && (string)result[2] == string.Empty)
                return 0;
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.autoproperty.genclass.genclass010.genclass010
{
    // <Title> Tests generic class auto property used in implicit operator.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public class InnerTest1
        {
            public int field;
            public static implicit operator InnerTest2(InnerTest1 t1)
            {
                MemberClass<object> mc = new MemberClass<object>();
                mc.Property_Dynamic = t1.field;
                dynamic dy = mc;
                return new InnerTest2()
                {
                    field = dy.Property_Dynamic
                }

                ;
            }
        }

        public class InnerTest2
        {
            public int field;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            InnerTest1 t1 = new InnerTest1()
            {
                field = 10
            }

            ;
            InnerTest2 result1 = t1; //implicit
            InnerTest2 result2 = (InnerTest2)t1; //explicit
            return (result1.field == 10 && result2.field == 10) ? 0 : 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.autoproperty.genclass.genclass012.genclass012
{
    // <Title> Tests generic class auto property used in extension method body.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public string Field;

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Test t = "abc".ExReturnTest();
            if (t.Field == "abc")
                return 0;
            return 1;
        }
    }

    public static class Extension
    {
        public static Test ExReturnTest(this string s)
        {
            var mc = new MemberClass<string>();
            mc.Property_T = s;
            dynamic dy = mc;
            return new Test()
            {
                Field = dy.Property_T
            }

            ;
        }
    }
    //</Code>
}
