// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.genclass.genclassregmeth.genclassregmeth;
using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.genclass.genclassregmeth.genclassregmeth
{
    public class C
    {
    }

    public interface I
    {
    }

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
        #region Instance methods
        public bool Method_ReturnBool()
        {
            return false;
        }

        public T Method_ReturnsT()
        {
            return default(T);
        }

        public T Method_ReturnsT(T t)
        {
            return t;
        }

        public T Method_ReturnsT(out T t)
        {
            t = default(T);
            return t;
        }

        public T Method_ReturnsT(ref T t, T tt)
        {
            t = default(T);
            return t;
        }

        public T Method_ReturnsT(params T[] t)
        {
            return default(T);
        }

        public T Method_ReturnsT(float x, T t)
        {
            return default(T);
        }

        public int Method_ReturnsInt(T t)
        {
            return 1;
        }

        public float Method_ReturnsFloat(T t, dynamic d)
        {
            return 3.4f;
        }

        public float Method_ReturnsFloat(T t, dynamic d, ref decimal dec)
        {
            dec = 3m;
            return 3.4f;
        }

        public dynamic Method_ReturnsDynamic(T t)
        {
            return t;
        }

        public dynamic Method_ReturnsDynamic(T t, dynamic d)
        {
            return t;
        }

        public dynamic Method_ReturnsDynamic(T t, int x, dynamic d)
        {
            return t;
        }

        // Multiple params
        // Constraints
        // Nesting
        #endregion
        #region Static methods
        public static bool? StaticMethod_ReturnBoolNullable()
        {
            return (bool?)false;
        }
        #endregion
    }

    public class MemberClassMultipleParams<T, U, V>
    {
        public T Method_ReturnsT(V v, U u)
        {
            return default(T);
        }
    }

    public class MemberClassWithClassConstraint<T>
        where T : class
    {
        public int Method_ReturnsInt()
        {
            return 1;
        }

        public T Method_ReturnsT(decimal dec, dynamic d)
        {
            return null;
        }
    }

    public class MemberClassWithNewConstraint<T>
        where T : new()
    {
        public T Method_ReturnsT()
        {
            return new T();
        }

        public dynamic Method_ReturnsDynamic(T t)
        {
            return new T();
        }
    }

    public class MemberClassWithAnotherTypeConstraint<T, U>
        where T : U
    {
        public U Method_ReturnsU(dynamic d)
        {
            return default(U);
        }

        public dynamic Method_ReturnsDynamic(int x, U u, dynamic d)
        {
            return default(T);
        }
    }

    #region Negative tests - you should not be able to construct this with a dynamic object
    public class MemberClassWithUDClassConstraint<T>
        where T : C, new()
    {
        public C Method_ReturnsC()
        {
            return new T();
        }
    }

    public class MemberClassWithStructConstraint<T>
        where T : struct
    {
        public dynamic Method_ReturnsDynamic(int x)
        {
            return x;
        }
    }

    public class MemberClassWithInterfaceConstraint<T>
        where T : I
    {
        public dynamic Method_ReturnsDynamic(int x, T v)
        {
            return default(T);
        }
    }
    #endregion
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.genclass.genclass001.genclass001
{
    // <Title> Tests generic class regular method used in regular method body.</Title>
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
            Test t = new Test();
            if (t.TestMethod() == true)
                return 1;
            return 0;
        }

        public bool TestMethod()
        {
            dynamic mc = new MemberClass<string>();
            return (bool)mc.Method_ReturnBool();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.genclass.genclass002.genclass002
{
    // <Title> Tests generic class regular method used in arguments of method invocation.</Title>
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
            Test t = new Test();
            dynamic mc1 = new MemberClass<int>();
            int result1 = t.TestMethod((int)mc1.Method_ReturnsT());
            dynamic mc2 = new MemberClass<string>();
            int result2 = Test.TestMethod((string)mc2.Method_ReturnsT());
            if (result1 == 0 && result2 == 0)
                return 0;
            else
                return 1;
        }

        public int TestMethod(int i)
        {
            if (i == default(int))
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
            if (s == default(string))
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.genclass.genclass003.genclass003
{
    // <Title> Tests generic class regular method used in static variable.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private static dynamic s_mc = new MemberClass<string>();
        private static float s_loc = (float)s_mc.Method_ReturnsFloat(null, 1);
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            if (s_loc != 3.4f)
                return 1;
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.genclass.genclass005.genclass005
{
    // <Title> Tests generic class regular method used in unsafe.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    //using System;
    //[TestClass]public class Test
    //{
    //[Test][Priority(Priority.Priority1)]public void DynamicCSharpRunTest(){Assert.AreEqual(0, MainMethod());} public static unsafe int MainMethod()
    //{
    //dynamic dy = new MemberClass<int>();
    //int value = 1;
    //int* valuePtr = &value;
    //int result = dy.Method_ReturnsT(out *valuePtr);
    //if (result == 0 && value == 0)
    //return 0;
    //return 1;
    //}
    //}
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.genclass.genclass006.genclass006
{
    // <Title> Tests generic class regular method used in generic method body.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {

        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Test t = new Test();
            string s1 = "";
            string s2 = "";
            string result = t.TestMethod<string>(ref s1, s2);
            if (result == null && s1 == null)
                return 0;
            return 1;
        }

        public T TestMethod<T>(ref T t1, T t2)
        {
            dynamic mc = new MemberClass<T>();
            return (T)mc.Method_ReturnsT(ref t1, t2);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.genclass.genclass008.genclass008
{
    // <Title> Tests generic class regular method used in extension method body.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public int Field = 10;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Test t = 1.ExReturnTest();
            if (t.Field == 1)
                return 0;
            return 1;
        }
    }

    public static class Extension
    {
        public static Test ExReturnTest(this int t)
        {
            dynamic dy = new MemberClass<int>();
            float x = 3.3f;
            int i = -1;
            return new Test()
            {
                Field = t + (int)dy.Method_ReturnsT(x, i)
            }

            ;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.genclass.genclass009.genclass009
{
    // <Title> Tests generic class regular method used in for loop.</Title>
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
            dynamic dy = new MemberClass<MyEnum>();
            int result = 0;
            for (int i = (int)dy.Method_ReturnsInt(MyEnum.Third); i < 10; i++)
            {
                result += (int)dy.Method_ReturnsInt((MyEnum)(i % 3 + 1));
            }

            if (result == 9)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.genclass.genclass009a.genclass009a
{
    // <Title> Tests generic class regular method used in for loop.</Title>
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
            dynamic dy = new MemberClass<MyEnum>();
            int result = 0;
            for (int i = dy.Method_ReturnsInt(MyEnum.Third); i < 10; i++)
            {
                result += dy.Method_ReturnsInt((MyEnum)(i % 3 + 1));
            }

            if (result == 9)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.genclass.genclass010.genclass010
{
    // <Title> Tests generic class regular method used in static constructor.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test : I
    {
        private static decimal s_dec = 0M;
        private static float s_result = 0f;
        static Test()
        {
            dynamic dy = new MemberClass<I>();
            s_result = (float)dy.Method_ReturnsFloat(new Test(), dy, ref s_dec);
        }


        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            if (Test.s_dec == 3M && Test.s_result == 3.4f)
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.genclass.genclass012.genclass012
{
    // <Title> Tests generic class regular method used in lambda.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
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
            dynamic dy = new MemberClass<string>();
            dynamic d1 = 3;
            dynamic d2 = "Test";
            Func<string, string, string> fun = (x, y) => (y + x.ToString());
            string result = fun((string)dy.Method_ReturnsDynamic("foo", d1), (string)dy.Method_ReturnsDynamic("bar", d2));
            if (result == "barfoo")
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.genclass.genclass013.genclass013
{
    // <Title> Tests generic class regular method used in array initializer list.</Title>
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
            dynamic dy = new MemberClass<string>();
            string[] array = new string[]
            {
            (string)dy.Method_ReturnsDynamic(string.Empty, 1, dy), (string)dy.Method_ReturnsDynamic(null, 0, dy), (string)dy.Method_ReturnsDynamic("a", -1, dy)}

            ;
            if (array.Length == 3 && array[0] == string.Empty && array[1] == null && array[2] == "a")
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.genclass.genclass014.genclass014
{
    // <Title> Tests generic class regular method used in null coalescing operator.</Title>
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
            dynamic dy = MemberClass<string>.StaticMethod_ReturnBoolNullable();
            if (!((bool?)dy ?? false))
                return 0;
            return 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.genclass.genclass015.genclass015
{
    // <Title> Tests generic class regular method used in static method body.</Title>
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
            dynamic dy = new MemberClassMultipleParams<MyEnum, MyStruct, MyClass>();
            MyEnum me = dy.Method_ReturnsT(null, new MyStruct());
            return (int)me;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.genclass.genclass016.genclass016
{
    // <Title> Tests generic class regular method used in query expression.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Linq;
    using System.Collections.Generic;

    public class Test
    {
        private class M
        {
            public int Field1;
            public int Field2;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic dy = new MemberClassWithClassConstraint<MyClass>();
            var list = new List<M>()
            {
            new M()
            {
            Field1 = 1, Field2 = 2
            }

            , new M()
            {
            Field1 = 2, Field2 = 1
            }
            }

            ;
            return list.Any(p => p.Field1 == (int)dy.Method_ReturnsInt()) ? 0 : 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.genclass.genclass017.genclass017
{
    // <Title> Tests generic class regular method used in ternary operator expression.</Title>
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
            dynamic dy = new MemberClassWithClassConstraint<string>();
            string result = (string)dy.Method_ReturnsT(decimal.MaxValue, dy) == null ? string.Empty : (string)dy.Method_ReturnsT(decimal.MaxValue, dy);
            return result == string.Empty ? 0 : 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.genclass.genclass018.genclass018
{
    // <Title> Tests generic class regular method used in lock expression.</Title>
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
            dynamic dy = new MemberClassWithNewConstraint<C>();
            int result = 1;
            lock (dy.Method_ReturnsT())
            {
                result = 0;
            }

            return result;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.genclass.genclass019.genclass019
{
    // <Title> Tests generic class regular method used in switch section statement list.</Title>
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
            dynamic dy1 = new MemberClassWithNewConstraint<C>();
            dynamic dy2 = new MemberClassWithAnotherTypeConstraint<int, int>();
            C result = new C();
            int result2 = -1;
            switch ((int)dy2.Method_ReturnsU(dy1)) // 0
            {
                case 0:
                    result = (C)dy1.Method_ReturnsDynamic(result); // called
                    break;
                default:
                    result2 = (int)dy2.Method_ReturnsDynamic(0, 0, dy2); // not called
                    break;
            }

            return (result.GetType() == typeof(C) && result2 == -1) ? 0 : 1;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.regmethod.genclass.implicit01.implicit01
{
    // <Title> Tests generic class regular method used in regular method body.</Title>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System;
    using System.Collections.Generic;

    public class Ta
    {
        public static int i = 2;
    }

    public class A<T>
    {
        public static implicit operator A<List<string>>(A<T> x)
        {
            return new A<List<string>>();
        }
    }

    public class B
    {
        public static void Foo<T>(A<List<T>> x, string y)
        {
            Ta.i--;
        }

        public static void Foo<T>(object x, string y)
        {
            Ta.i++;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            var x = new A<Action<object>>();
            Foo<string>(x, "");
            Foo<string>(x, (dynamic)"");
            return Ta.i;
        }
    }
}
