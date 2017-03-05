// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.regproperty.genclass.genclassregprop.genclassregprop;
using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.regproperty.genclass.genclassregprop.genclassregprop
{
    using System.Reflection;

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
        /// <summary>
        /// We use this to get the values we cannot get directly
        /// </summary>
        /// <param name = "target"></param>
        /// <param name = "name"></param>
        /// <returns></returns>
        public object GetPrivateValue(object target, string name)
        {
            var tip = target.GetType();
            var prop = tip.GetTypeInfo().GetDeclaredField(name);
            return prop.GetValue(target);
        }

        /// <summary>
        /// We use this to set the value we cannot set directly
        /// </summary>
        /// <param name = "target"></param>
        /// <param name = "name"></param>
        /// <param name = "value"></param>
        public void SetPrivateValue(object target, string name, object value)
        {
            var tip = target.GetType();
            var prop = tip.GetTypeInfo().GetDeclaredField(name);
            prop.SetValue(target, value);
        }

        public decimal[] myDecimalArr = new decimal[2];
        public dynamic myDynamic = new object();
        public T myT;
        public T Property_T
        {
            set
            {
                myT = value;
            }

            get
            {
                return myT;
            }
        }

        public decimal[] Property_decimalArr
        {
            protected internal set
            {
                myDecimalArr = value;
            }

            get
            {
                return myDecimalArr;
            }
        }

        public dynamic Property_dynamic
        {
            get
            {
                return myDynamic;
            }

            set
            {
                myDynamic = value;
            }
        }

        public static float myFloatStatic;
        public static T myTStatic;
        public static T Property_TStatic
        {
            set
            {
                myTStatic = value;
            }

            get
            {
                return myTStatic;
            }
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.regproperty.genclass.genclass001.genclass001
{
    // <Title> Tests generic class regular property used in regular method body.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
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
            Test t1 = new Test();
            return t1.TestGetMethod(new MemberClass<bool>()) + t1.TestSetMethod(new MemberClass<bool>()) == 0 ? 0 : 1;
        }

        public int TestGetMethod(MemberClass<bool> mc)
        {
            mc.Property_T = true;
            dynamic dy = mc;
            if (!(bool)dy.Property_T)
                return 1;
            else
                return 0;
        }

        public int TestSetMethod(MemberClass<bool> mc)
        {
            dynamic dy = mc;
            dy.Property_T = true;
            mc = dy; //mc might be a boxed struct
            if (!mc.Property_T)
                return 1;
            else
                return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.regproperty.genclass.genclass002.genclass002
{
    // <Title> Tests generic class regular property used in regular method body with conditional attribute.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private static int s_count = 0;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Test t1 = new Test();
            t1.TestGetMethod(new MemberClass<bool>());
            t1.TestSetMethod(new MemberClass<bool>());
            return s_count;
        }

        [System.Diagnostics.Conditional("c1")]
        public void TestGetMethod(MemberClass<bool> mc)
        {
            dynamic dy = mc;
            mc.Property_decimalArr = new decimal[1];
            if ((int)dy.Property_decimalArr.Length != 1)
                s_count++;
        }

        [System.Diagnostics.Conditional("c2")]
        public void TestSetMethod(MemberClass<bool> mc)
        {
            dynamic dy = mc;
            dy.Property_decimalArr = new decimal[]
            {
            0M, 1M
            }

            ;
            if (!((int)mc.Property_decimalArr.Length != 2))
                s_count++;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.regproperty.genclass.genclass003.genclass003
{
    // <Title> Tests generic class regular property used in member initializer of anonymous type.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
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
            MemberClass<string> mc = new MemberClass<string>();
            mc.myT = "Test";
            mc.myDecimalArr = new decimal[]
            {
            0M, 1M
            }

            ;
            dynamic dy = mc;
            var tc = new
            {
                A1 = (string)dy.Property_T,
                A2 = (decimal[])dy.Property_decimalArr,
                A3 = (object)dy.Property_dynamic
            }

            ;
            if (tc != null && mc.myT == tc.A1 && tc.A2[0] == 0M && tc.A2[1] == 1M && tc.A3 == mc.myDynamic)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.regproperty.genclass.genclass005.genclass005
{
    // <Title> Tests generic class regular property used in query expression.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
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
            MemberClass<string> mc = new MemberClass<string>();
            mc.myT = "a";
            dynamic dy = mc;
            var result = list.Where(p => p == (string)dy.Property_T).ToList();
            if (result.Count == 1 && result[0] == "a")
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.regproperty.genclass.genclass006.genclass006
{
    // <Title> Tests generic class regular property used in member initializer of object initializer.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private int _field1;
        private string _field2 = string.Empty;
        private MyEnum _field3;

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            MemberClass<string> mc1 = new MemberClass<string>();
            MemberClass<Test> mc2 = new MemberClass<Test>();
            MemberClass<MyStruct> mc3 = new MemberClass<MyStruct>();
            mc1.Property_dynamic = 10;
            mc3.Property_dynamic = MyEnum.Second;
            dynamic dy1 = mc1;
            dynamic dy2 = mc2;
            dynamic dy3 = mc3;
            var test = new Test()
            {
                _field1 = dy1.Property_dynamic,
                _field2 = dy2.Property_dynamic == null ? string.Empty : dy2.Property_dynamic.ToString(),
                _field3 = dy3.Property_dynamic
            }

            ;
            if (test._field1 == 10 && test._field2 != null && test._field3 == MyEnum.Second)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.regproperty.genclass.genclass007.genclass007
{
    // <Title> Tests generic class regular property used in explicit operator.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public class InnerTest1
        {
            public int field;
            public static explicit operator InnerTest2(InnerTest1 t1)
            {
                var dy = new MemberClass<InnerTest2>();
                dy.Property_T = new InnerTest2()
                {
                    field = t1.field + 1
                }

                ;
                return dy.Property_T;
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
            InnerTest1 t = new InnerTest1()
            {
                field = 20
            }

            ;
            InnerTest2 result = (InnerTest2)t; //explicit
            return (result.field == 21) ? 0 : 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.regproperty.genclass.genclass008.genclass008
{
    // <Title> Tests generic class regular property used in implicit operator.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public class InnerTest1
        {
            public int field;
            public static implicit operator InnerTest2(InnerTest1 t1)
            {
                var dy = new MemberClass<InnerTest2>();
                dy.Property_T = new InnerTest2()
                {
                    field = t1.field + 1
                }

                ;
                return dy.Property_T;
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
            InnerTest1 t = new InnerTest1()
            {
                field = 20
            }

            ;
            InnerTest2 result1 = (InnerTest2)t; //explicit
            InnerTest2 result2 = t; //implicit
            return (result1.field == 21 && result2.field == 21) ? 0 : 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.regproperty.genclass.genclass011.genclass011
{
    // <Title> Tests generic class regular property used in volatile field initializer.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private static MemberClass<MyClass> s_mc;
        private static dynamic s_dy;
        static Test()
        {
            s_mc = new MemberClass<MyClass>();
            s_mc.Property_dynamic = new MyClass()
            {
                Field = 10
            }

            ;
            s_dy = s_mc;
        }

        private volatile object _o = s_dy.Property_dynamic;

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Test t = new Test();
            if (t._o.GetType() == typeof(MyClass) && ((MyClass)t._o).Field == 10)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.property.regproperty.genclass.genclass012.genclass012
{
    // <Title> Tests generic class regular property used in volatile field initializer.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(17,16\).*CS0219</Expects>
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
            List<int> list = new List<int>()
            {
            0, 4, 1, 6, 4, 4, 5
            }

            ;
            string s = "test";
            var mc = new MemberClass<int>();
            mc.Property_T = 4;
            mc.Property_dynamic = "Test";
            dynamic dy = mc;
            var result = list.Where(p => p == (int)dy.Property_T).Select(p => new
            {
                A = dy.Property_T,
                B = dy.Property_dynamic
            }

            ).ToList();
            if (result.Count == 3)
            {
                foreach (var m in result)
                {
                    if ((int)m.A != 4 || m.A.GetType() != typeof(int) || (string)m.B != "Test" || m.B.GetType() != typeof(string))
                        return 1;
                }

                return 0;
            }

            return 1;
        }
    }
    //</Code>
}
