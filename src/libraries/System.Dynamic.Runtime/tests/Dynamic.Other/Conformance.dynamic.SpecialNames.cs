// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.SpecialNames.opspecial002
{
    // <Area>Dynamic bind to special name</Area>
    // <Title>get property and method in the same class</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>

    public struct Parent
    {
        public int result;
        public dynamic ProInt
        {
            set
            {
                result = value;
            }
        }

        public void set_ProInt(int value)
        {
            result = value + 1;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Parent();
            d.ProInt = 10;
            return d.result == 10 ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.SpecialNames.opspecial003
{
    // <Area>Dynamic bind to special name</Area>
    // <Title>get indexer and method in the same class</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>

    public class Test
    {
        public byte this[Test t]
        {
            get
            {
                return 0;
            }
        }

        public byte get_Item(dynamic a)
        {
            return 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            int index = 0;
            for (byte b = t[t]; b < 10; b++)
            {
                index++;
            }

            return index == 10 ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.SpecialNames.opspecial005
{
    // <Area>Dynamic bind to special name</Area>
    // <Title>add_, remove_ event and method in the same class</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>

    public delegate void MyDel(int i);
    public class Test
    {
        public int flag = 0;
        public event MyDel Foo
        {
            add
            {
                flag = 1;
            }

            remove
            {
                flag = 2;
            }
        }

        public void add_Foo(dynamic value)
        {
            flag = 3;
        }

        public void remove_Foo(dynamic value)
        {
            flag = 4;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Test t = new Test();
            dynamic d0 = (MyDel)Method;
            // was: CS0266: Cannot implicitly convert type 'dynamic' to 'MyDel'
            // now: pass - correct behavior
            t.Foo += d0;
            if (t.flag != 1)
            {
                System.Console.WriteLine("event add doesn't get called");
                return 1;
            }

            t.Foo -= d0;
            if (t.flag != 2)
            {
                System.Console.WriteLine("event remove doesn't get called");
                return 1;
            }

            return 0;
        }

        public static void Method(int i)
        {
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.SpecialNames.opspecial006
{
    // <Area>Dynamic bind to special name</Area>
    // <Title>access value__ in enum</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public class Test
    {
        public enum MyEnum
        {
            First,
            Second
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = MyEnum.Second;
            dynamic result = new List<int>()
            {
            d.value__
            }

            ;
            if (result.Count == 1 && result[0] == 1)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.SpecialNames.opspecial008
{
    // <Area>Dynamic bind to special name</Area>
    // <Title>op_Equality and op_Inequality in class</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(10,7\).*CS0660</Expects>
    //<Expects Status=warning>\(10,7\).*CS0661</Expects>

    public class Test
    {
        public static bool operator ==(Test p1, Test p2)
        {
            return true;
        }

        public static bool operator !=(Test p1, Test p2)
        {
            return true;
        }

        public static bool op_Equality(Test p1, int p2)
        {
            return false;
        }

        public static dynamic op_Inequality(dynamic p1, Test p2)
        {
            return false;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t1 = new Test();
            int t2 = 10;
            int num = 0;
            try
            {
                bool b = t1 == t2;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "==", "Test", "int");
                if (ret)
                    num++;
            }

            t1 = 10;
            dynamic t3 = new Test();
            try
            {
                bool b = t1 != t3;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "!=", "int", "Test");
                if (ret)
                    num++;
            }

            Test t4 = new Test();
            if (t3 != t4)
                num++;
            while (t3 == (dynamic)t4)
            {
                num++;
                break;
            }

            return num == 4 ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.SpecialNames.opspecial009
{
    // <Area>Dynamic bind to special name</Area>
    // <Title>op_Explicit and op_Implicit in class</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>

    public class Test
    {
        public int Field;
        public static explicit operator Test(int[] i)
        {
            return new Test()
            {
                Field = 10
            }

            ;
        }

        public static long op_Implicit(Test p1)
        {
            return p1.Field;
        }

        public static Test op_Explicit(object p1)
        {
            return new Test()
            {
                Field = 0
            }

            ;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            bool ret = true;
            dynamic d = new Test();
            try
            {
                long l = d;
                ret &= false;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                ret &= ErrorVerifier.Verify(ErrorMessageId.NoImplicitConv, e.Message, "Test", "long");
            }

            dynamic d1 = new int[0];
            Test t = (Test)d1;
            ret &= t.Field == 10;
            return ret ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.SpecialNames.opspecial011
{
    // <Area>Dynamic bind to special name</Area>
    // <Title>op_Addition in class</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>
    using System.Linq;
    using System.Collections.Generic;

    public class TestClass
    {
        [Fact]
        public void RunTest()
        {
            Test.DynamicCSharpRunTest();
        }
    }

    public struct Test
    {
        public int Field;
        public static Test op_Addition<T>(Test p1, T p2)
        {
            return new Test()
            {
                Field = p1.Field
            }

            ;
        }

        public static int op_Addition<T>(T p1, int p2)
        {
            return 0;
        }

        public static Test operator +(Test p1, Test p2)
        {
            return new Test()
            {
                Field = p1.Field + p2.Field
            }

            ;
        }

        public static long operator +(Test p1, int p2)
        {
            return p1.Field + p2;
        }

        public int Method()
        {
            dynamic t1 = new Test()
            {
                Field = 10
            }

            ;
            dynamic t2 = new Test()
            {
                Field = 11
            }

            ;
            List<int> list = new List<int>()
            {
            t1.Field, t2.Field, (t1 + t2).Field
            }

            ;
            var d = list.Where(p => p == (t1 + t2).Field).ToArray();
            dynamic t3 = new Test()
            {
                Field = 10
            }

            ;
            int p2 = 20;
            // Test + int => long
            var v = t3 + p2;
            if (d.Count() == 1 && d[0] == 21 && v == 30)
                return 0;
            return 1;
        }

        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            return d.Method();
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.SpecialNames.opspecial013
{
    // <Area>Dynamic bind to special name</Area>
    // <Title>op_GreaterThan, op_LessThan in class</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>

    public class Test
    {
        public int Field;
        public static dynamic operator >(Test p1, dynamic p2)
        {
            return 0;
        }

        public static dynamic operator <(Test p1, object p2)
        {
            return 0;
        }

        public static dynamic op_GreaterThan(Test p1, dynamic[] p2)
        {
            return 1;
        }

        public static dynamic op_LessThan(Test p1, int p2)
        {
            return 1;
        }

        public static int Method(dynamic i, dynamic j)
        {
            if (i == 0 && j == 0)
                return 0;
            return 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t1 = new Test()
            {
                Field = 10
            }

            ;
            dynamic t2 = 10;
            return Method(t1 < t2, t1 > t2);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.SpecialNames.opspecial014
{
    // <Area>Dynamic bind to special name</Area>
    // <Title>op_GreaterThanOrEqual, op_LessThanOrEqual in class</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>

    public class Test
    {
        public enum MyEnum
        {
            First,
            Second,
            Third
        }

        public int Field;
        public static dynamic operator >=(Test p1, dynamic p2)
        {
            return 0;
        }

        public static dynamic operator <=(Test p1, object p2)
        {
            return MyEnum.Second;
        }

        public static dynamic op_GreaterThanOrEqual(Test p1, dynamic[] p2)
        {
            return 1;
        }

        public static dynamic op_LessThanOrEqual(Test p1, int p2)
        {
            return MyEnum.Third;
        }

        public int Prop
        {
            get
            {
                dynamic t1 = new Test()
                {
                    Field = 10
                }

                ;
                dynamic t2 = 10;
                dynamic t = new[]
                {
                t1 >= t2, t1 <= t2
                }

                ;
                if (t.Length == 2 && t[0] == 0 && t[1] == MyEnum.Second)
                    return 0;
                return 1;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            return d.Prop;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.SpecialNames.opspecial015
{
    // <Area>Dynamic bind to special name</Area>
    // <Title>op_UnaryPlus, op_UnaryNegation in class</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>

    public class Test
    {
        public static dynamic operator +(Test p1)
        {
            return new long[]
            {
            1, 2, 3, 4
            }

            ;
        }

        public static dynamic operator -(Test p1)
        {
            return 0;
        }

        public static dynamic op_UnaryPlus<T>(Test p1)
        {
            return new string[]
            {
            null, string.Empty
            }

            ;
        }

        public static Test op_UnaryNegation(dynamic p1)
        {
            p1 = new Test();
            return p1;
        }

        public int Method<T>(T t)
        {
            dynamic d = new Test();
            dynamic index = -d;
            foreach (dynamic s in +d)
            {
                index++;
            }

            if (index == 4)
                return 0;
            return 1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            return d.Method(d);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.SpecialNames.opspecial016
{
    // <Area>Dynamic bind to special name</Area>
    // <Title>op_Multiply, op_Division, op_Modulus in class</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>

    public class Base
    {
        public static Test op_Multiply(Test t, dynamic p1)
        {
            p1 = new Test()
            {
                Field = -1
            }

            ;
            return p1;
        }

        public static dynamic op_Division(dynamic p1, Test t)
        {
            t = new Test()
            {
                Field = -1
            }

            ;
            return t;
        }

        public static Test op_Modulus(dynamic p1, dynamic t)
        {
            t = new Test()
            {
                Field = -1
            }

            ;
            return t;
        }
    }

    public class Test : Base
    {
        public int Field;
        public static Test operator *(Test t, dynamic p1)
        {
            p1 = new Test()
            {
                Field = 10
            }

            ;
            return p1;
        }

        public static dynamic operator /(dynamic p1, Test t)
        {
            t = new Test()
            {
                Field = 10
            }

            ;
            return t;
        }

        public static dynamic operator %(Test p1, dynamic t)
        {
            t = new Test()
            {
                Field = 10
            }

            ;
            return t;
        }

        public int this[dynamic x]
        {
            get
            {
                dynamic d = new Test() as Base;
                dynamic d0 = default(dynamic);
                return ((d * d0).Field + (d0 / d).Field + (d % d0).Field);
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            return d[d] == 30 ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.SpecialNames.opspecial017
{
    // <Area>Dynamic bind to special name</Area>
    // <Title>op_True, op_False, op_BitwiseAnd, op_BitwiseAnd in class</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>

    public class Test
    {
        public int Field;
        public static bool operator true(Test p1)
        {
            return true;
        }

        public static bool op_True(dynamic p1)
        {
            return false;
        }

        public static bool operator false(Test p1)
        {
            return false;
        }

        public static bool op_False(dynamic p1)
        {
            return true;
        }

        public static Test operator &(Test p1, Test p2)
        {
            return new Test()
            {
                Field = 3
            }

            ;
        }

        public static Test op_BitwiseAnd(dynamic p1, Test p2)
        {
            return new Test()
            {
                Field = 5
            }

            ;
        }

        public static Test operator |(Test p1, Test p2)
        {
            return new Test()
            {
                Field = 4
            }

            ;
        }

        public static Test op_BitwiseAnd(Test p1, dynamic p2)
        {
            return new Test()
            {
                Field = 6
            }

            ;
        }

        public static dynamic operator ^(dynamic p1, Test p2)
        {
            return new Test()
            {
                Field = 7
            }

            ;
        }

        public static Test op_ExclusiveOr(Test p1, dynamic p2)
        {
            return new Test()
            {
                Field = 8
            }

            ;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t1 = new Test()
            {
                Field = 1
            }

            ;
            dynamic t2 = new Test()
            {
                Field = 2
            }

            ;
            dynamic d1 = t1 && t2; //dy1 && dy2,   T.false(x) ? x : T.&(x, y)
            if (d1.Field != 3)
                return 1;
            dynamic d2 = t1 || t2; //dy1 || dy2, T.True(x) ? x : T.|(x, y)
            if (d2.Field != 1)
                return 1;
            dynamic d3 = t1 & t2;
            if (d3.Field != 3)
                return 1;
            dynamic d4 = t1 | t2;
            if (d4.Field != 4)
                return 1;
            dynamic d5 = t1 ^ t2;
            if (d5.Field != 7)
                return 1;
            bool isHit = false;
            if (t1)
                isHit = true;
            if (!isHit)
                return 1;
            isHit = false;
            while (t1)
            {
                isHit = true;
                break;
            }

            if (!isHit)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.SpecialNames.opspecial018
{
    // <Area>Dynamic bind to special name</Area>
    // <Title>op_LogicalNot in class</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>

    public class Base<T>
    {
        public static bool op_LogicalNot(T t)
        {
            return false;
        }
    }

    public class Test : Base<Test>
    {
        public int Field;
        public static bool operator !(Test t)
        {
            return true;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test() as Base<Test>;
            return !d ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.SpecialNames.opspecial019
{
    // <Area>Dynamic bind to special name</Area>
    // <Title>op_Increment, op_Increment in class</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>

    public class Base<T>
    {
        public static Test op_Increment<M>(T t)
        {
            return null;
        }

        public static Test op_Increment<M>(M t)
        {
            return null;
        }
    }

    public class Test : Base<Test>
    {
        public int Field;
        public static Test operator ++(Test t)
        {
            return new Test()
            {
                Field = t.Field + 1
            }

            ;
        }

        public static Test operator --(Test t)
        {
            return new Test()
            {
                Field = t.Field - 1
            }

            ;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test()
            {
                Field = 10
            }

            ;
            if ((d++).Field == 10 && (++d).Field == 12 && (d--).Field == 12 && (--d).Field == 10)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.SpecialNames.opspecial020
{
    // <Area>Dynamic bind to special name</Area>
    // <Title>op_OnesComplement in class</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>

    public abstract class Base
    {
        public static Test op_OnesComplement(Test t)
        {
            return t;
        }
    }

    public class Test : Base
    {
        public int Field;
        public dynamic d = default(dynamic);
        public static Test operator ~(Test t)
        {
            return new Test()
            {
                Field = -t.Field
            }

            ;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            d.d = new Test()
            {
                Field = 10
            }

            as Base;
            return (~(d.d)).Field == -10 ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.SpecialNames.opspecial021
{
    // <Area>Dynamic bind to special name</Area>
    // <Title>op_RightShift, op_LeftShift in class</Title>
    // <Description></Description>
    // <Expects status=success></Expects>
    // <Code>

    public partial class Test
    {
        public static Test op_RightShift(dynamic t, byte i)
        {
            return t;
        }

        public static Test op_LeftShift(Test t, byte i)
        {
            return t;
        }
    }

    public partial class Test
    {
        public int Field;
        public static Test operator >>(Test t, int i)
        {
            return new Test()
            {
                Field = i + t.Field
            }

            ;
        }

        public static Test operator <<(Test t, int i)
        {
            return new Test()
            {
                Field = t.Field - i
            }

            ;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test()
            {
                Field = 10
            }

            ;
            byte b = 10;
            if ((d >> b).Field != 20 || (d << b).Field != 0)
                return 1;
            return 0;
        }
    }
    // </Code>
}
