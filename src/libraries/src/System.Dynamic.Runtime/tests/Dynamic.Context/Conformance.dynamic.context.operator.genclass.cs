// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.genclass.genclassoperate.genclassoperate;
using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.genclass.genclassoperate.genclassoperate
{
    public class MyClass
    {
        public int Field = 0;
    }

    public class MyClass<T>
    {
        public int Field = 0;
        //    public static MyEnum?[] operator -(MyClass p1, dynamic[] p2) { return new MyEnum?[] { MyEnum.First, null }; }
        public static T operator ^(MyClass<T> p1, float p2)
        {
            return default(T);
        }

        public static T operator ^(MyClass<T> p1, T p2)
        {
            return p2;
        }

        public static MyClass<T>[] operator /(MyClass<T> p1, float? p2)
        {
            return new MyClass<T>[]
            {
            null, new MyClass<T>()
            {
            Field = 4
            }
            }

            ;
        }

        public static MyStruct?[] operator <=(MyClass<T> p1, int p2)
        {
            return new MyStruct?[]
            {
            null, new MyStruct()
            {
            Number = int.MinValue
            }
            }

            ;
        }

        public static MyStruct?[] operator >=(MyClass<T> p1, int p2)
        {
            return new MyStruct?[]
            {
            null, new MyStruct()
            {
            Number = int.MaxValue
            }
            }

            ;
        }

        public static decimal[] operator -(dynamic p1, MyClass<T> p2)
        {
            return new decimal[]
            {
            decimal.MaxValue
            }

            ;
        }

        public static string operator *(dynamic[] p1, MyClass<T> p2)
        {
            return string.Empty;
        }

        public static dynamic[] operator &(MyClass<T> p1, string p2)
        {
            return new dynamic[]
            {
            p1
            }

            ;
        }

        public static dynamic[] operator -(MyClass<T> p1)
        {
            return new dynamic[]
            {
            p1
            }

            ;
        }

        public static MyClass<T> operator --(MyClass<T> p1)
        {
            return new MyClass<T>()
            {
                Field = 4
            }

            ;
        }

        // CS1964 -> negative
        // public static implicit operator MyClass<T>(dynamic p1) { return new MyClass<T>() { Field = 4 }; }
        // public static implicit operator dynamic(MyClass<T> p1) { return p1; }
        public static implicit operator MyStruct[] (MyClass<T> p1)
        {
            return new MyStruct[]
            {
            new MyStruct()
            {
            Number = 4
            }
            }

            ;
        }

        public static explicit operator MyClass<T>(MyStruct?[] p1)
        {
            return new MyClass<T>()
            {
                Field = 3
            }

            ;
        }
    }

    public class MemberClassMultipleParams<T, U, V>
    {
        public int Field;
        public static MemberClassMultipleParams<T, U, V> operator >>(MemberClassMultipleParams<T, U, V> p1, int p2)
        {
            return new MemberClassMultipleParams<T, U, V>();
        }

        public static dynamic[] operator &(MemberClassMultipleParams<T, U, V> p1, string p2)
        {
            return new dynamic[]
            {
            p1
            }

            ;
        }

        public static bool operator true(MemberClassMultipleParams<T, U, V> p1)
        {
            return false;
        }

        public static bool operator false(MemberClassMultipleParams<T, U, V> p1)
        {
            return true;
        }

        public static explicit operator MemberClassMultipleParams<T, U, V>(U p1)
        {
            return new MemberClassMultipleParams<T, U, V>()
            {
                Field = 4
            }

            ;
        }

        public static implicit operator MemberClassMultipleParams<T, U, V>(double[] p1)
        {
            return new MemberClassMultipleParams<T, U, V>()
            {
                Field = 4
            }

            ;
        }
    }

    public class MemberClassWithClassConstraint<T>
        where T : class
    {
        public int Field;
        public static decimal[] operator |(bool? p1, MemberClassWithClassConstraint<T> p2)
        {
            return new decimal[]
            {
            decimal.MaxValue
            }

            ;
        }

        public static MyEnum? operator <(byte?[] p1, MemberClassWithClassConstraint<T> p2)
        {
            return MyEnum.First;
        }

        public static MyEnum? operator >(byte?[] p1, MemberClassWithClassConstraint<T> p2)
        {
            return MyEnum.Third;
        }
    }

    public class MemberClassWithNewConstraint<T>
        where T : new()
    {
        public static int Status;
        public static bool? operator !(MemberClassWithNewConstraint<T> p1)
        {
            return true;
        }

        public static string operator ~(MemberClassWithNewConstraint<T> p1)
        {
            return string.Empty;
        }

        public static MemberClassWithNewConstraint<T> operator ++(MemberClassWithNewConstraint<T> p1)
        {
            return new MemberClassWithNewConstraint<T>();
        }
    }

    public class MemberClassWithAnotherTypeConstraint<T, U>
        where T : U
    {
        public static implicit operator MyStruct[] (MemberClassWithAnotherTypeConstraint<T, U> p1)
        {
            return new MyStruct[]
            {
            new MyStruct()
            {
            Number = 4
            }
            }

            ;
        }

        public static implicit operator int? (MemberClassWithAnotherTypeConstraint<T, U> p1)
        {
            return int.MinValue;
        }

        public static explicit operator object[] (MemberClassWithAnotherTypeConstraint<T, U> p1)
        {
            return new object[]
            {
            p1
            }

            ;
        }

        public static explicit operator MyEnum[] (MemberClassWithAnotherTypeConstraint<T, U> p1)
        {
            return new MyEnum[]
            {
            MyEnum.First
            }

            ;
        }
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
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.genclass.genclass001.genclass001
{
    // <Title> Tests generic class operator used in static method.</Title>
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
            MyClass<int> mc = new MyClass<int>();
            dynamic dy = mc;
            int result = dy ^ 1.2f;
            if (result == 0)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.genclass.genclass002.genclass002
{
    // <Title> Tests generic class operator used in regular method body.</Title>
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
            return new Test().TestMethod();
        }

        private int TestMethod()
        {
            MyClass<string> mc = new MyClass<string>();
            dynamic dy = mc;
            string result = dy ^ string.Empty;
            if (result == string.Empty)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.genclass.genclass003.genclass003
{
    // <Title> Tests generic class operator used in variable initializer.</Title>
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
            MyClass<Test> mc = new MyClass<Test>();
            dynamic dy = mc;
            float? p2 = 1.33f;
            // MyClass<Test>[]
            dynamic[] result = dy / p2;
            if (result.Length == 2 && result[0] == null && result[1].GetType() == typeof(MyClass<Test>) && result[1].Field == 4)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.genclass.genclass005.genclass005
{
    // <Title> Tests generic class operator used in lock expression.</Title>
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
            MyClass<string> mc = new MyClass<string>();
            dynamic dy = mc;
            MyStruct?[] result;
            lock (dy <= 20)
            {
                result = dy <= 20;
            }

            if (result.Length == 2 && result[0] == null && result[1].Value.Number == int.MinValue)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.genclass.genclass006.genclass006
{
    // <Title> Tests generic class operator used in the for loop initializer.</Title>
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
            MyClass<int> mc = new MyClass<int>();
            dynamic dy = mc;
            MyStruct[][] result = new MyStruct[10][];
            for (int i = (dy ^ 1.30f); i < 10; i++)
            {
                result[i] = dy;
            }

            for (int i = 0; i < 10; i++)
            {
                MyStruct[] m = result[i];
                if (m.Length != 1 && m[0].Number != 4)
                    return 1;
            }

            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.genclass.genclass007.genclass007
{
    // <Title> Tests generic class operator used in the for-condition.</Title>
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
            MyClass<int> mc = new MyClass<int>();
            dynamic dy = mc;
            MyClass<int>[] result = new MyClass<int>[10];
            for (int i = 9; i >= (dy ^ 1.30f); i--)
            {
                result[i] = --dy;
            }

            for (int i = 0; i < 10; i++)
            {
                if (result[i].Field != 4)
                {
                    return 1;
                }
            }

            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.genclass.genclass008.genclass008
{
    // <Title> Tests generic class operator used in the for-iterator.</Title>
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
            MyClass<int> mc = new MyClass<int>()
            {
                Field = 10
            }

            ;
            dynamic dy = mc;
            dynamic[] result = null;
            int index = 0;
            for (; dy.Field != 4; dy--)
            {
                result = -dy;
                index++;
            }

            if (index == 1 && result.Length == 1 && result[0].Field == 10)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.genclass.genclass009.genclass009
{
    // <Title> Tests generic class operator used in the foreach expression.</Title>
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
            MyClass<int> mc = new MyClass<int>()
            {
                Field = 10
            }

            ;
            dynamic dy = mc;
            int index = 0;
            foreach (var m in dy & "Test")
            {
                index++;
                if (m.Field != 10)
                    return 1;
            }

            if (index != 1)
                return 1;
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.genclass.genclass010.genclass010
{
    // <Title> Tests generic class operator used in generic method body.</Title>
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
            return t.TestMethod<int, string, Test>();
        }

        private int TestMethod<T, U, V>()
        {
            MemberClassMultipleParams<T, U, V> mc = new MemberClassMultipleParams<T, U, V>()
            {
                Field = -1
            }

            ;
            dynamic dy = mc;
            return (dy >> -1).Field == 0 ? 0 : 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.genclass.genclass011.genclass011
{
    // <Title> Tests generic class operator used in extension method body.</Title>
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
            return 10.ExReturnTest();
        }
    }

    public static class Extension
    {
        public static int ExReturnTest(this int p)
        {
            var mc = new MyClass<string>();
            dynamic dy = mc;
            MyStruct?[] result = dy >= int.MaxValue;
            if (result.Length == 2 && result[0] == null && result[1].Value.Number == int.MaxValue)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.genclass.genclass012.genclass012
{
    // <Title> Tests generic class operator used in arguments to method invocation.</Title>
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
            MyClass<string> mc = new MyClass<string>();
            dynamic dy = mc;
            Test t = new Test();
            return t.TestMethod(dy - dy);
        }

        public int TestMethod(decimal[] da)
        {
            foreach (var d in da)
            {
                ;
            }

            if (da.Length == 1 && da[0] == decimal.MaxValue)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.genclass.genclass013.genclass013
{
    // <Title> Tests generic class operator used in implicitly-typed variable initializer.</Title>
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
            MemberClassMultipleParams<string, Test, int> mc = new MemberClassMultipleParams<string, Test, int>()
            {
                Field = 10
            }

            ;
            dynamic dy = mc;
            string s = null;
            var result = dy & s;
            if (result.Length == 1 && result[0].GetType() == typeof(MemberClassMultipleParams<string, Test, int>) && result[0].Field == 10)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.genclass.genclass014.genclass014
{
    // <Title> Tests generic class operator used in static method.</Title>
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
            MemberClassMultipleParams<int, int, int> mc = new MemberClassMultipleParams<int, int, int>()
            {
                Field = 10
            }

            ;
            dynamic dy = mc;
            bool isHit = false;
            if (dy)
            {
                isHit = true;
            }

            if (!isHit)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.genclass.genclass015.genclass015
{
    // <Title> Tests generic class operator used in member initializer of object initializer.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private dynamic _field;

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Test t = new Test()
            {
                _field = (MemberClassMultipleParams<int, int, int>)1
            }

            ;
            if (t._field.GetType() == typeof(MemberClassMultipleParams<int, int, int>) && t._field.Field == 4)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.genclass.genclass016.genclass016
{
    // <Title> Tests generic class operator used in member initializer of anonymous type.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private static dynamic s_dy;

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            MemberClassWithClassConstraint<MyClass> mc = new MemberClassWithClassConstraint<MyClass>();
            s_dy = mc;
            bool? b = false;
            byte?[] p = null;
            var result = new
            {
                A = b | s_dy,
                B = p < s_dy
            }

            ;
            if (result.A.Length == 1 && result.A[0] == decimal.MaxValue && result.B == MyEnum.First)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.genclass.genclass017.genclass017
{
    // <Title> Tests generic class operator used in static variable.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private static dynamic s_dy = new MyClass<Test>()
        {
            Field = 10
        }

        ; //implicit operator.
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            if (Test.s_dy.Field == 10)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.genclass.genclass018.genclass018
{
    // <Title> Tests generic class operator used in property-get body.</Title>
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
            if (t.MyProp.Field == 3)
                return 0;
            return 1;
        }

        public dynamic MyProp
        {
            get
            {
                MyStruct?[] p1 = null;
                dynamic result = (MyClass<Test>)p1;
                return result;
            }
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.genclass.genclass019.genclass019
{
    // <Title> Tests generic class operator used in property-set body.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System;

    public class Test
    {
        private static dynamic s_result;

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Test.MyProp = null;
            Type[] t = s_result.GetType().GenericTypeArguments;
            if (t.Length != 3 || t[0] != typeof(int) || t[1] != typeof(string) || t[1] != typeof(string))
                return 1;
            if (s_result.Field == 4)
                return 0;
            return 1;
        }

        public static dynamic MyProp
        {
            set
            {
                double[] d = new double[]
                {
                double.Epsilon, double.MaxValue
                }

                ;
                s_result = (MemberClassMultipleParams<int, string, string>)d;
            }
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.genclass.genclass020.genclass020
{
    // <Title> Tests generic class operator used in indexer body.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System;

    public class Test
    {
        private static MemberClassWithClassConstraint<MyClass<MyClass>> s_mc = new MemberClassWithClassConstraint<MyClass<MyClass>>();
        private MyEnum? _field;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Test t = new Test();
            t[10] = null;
            if (t._field != MyEnum.First)
                return 1;
            MyEnum? f = t[int.MinValue];
            if (f != MyEnum.Third)
                return 1;
            return 0;
        }

        public MyEnum? this[int i]
        {
            set
            {
                dynamic dy = s_mc;
                byte?[] p1 = new byte?[0];
                _field = p1 < dy;
            }

            get
            {
                dynamic dy = s_mc;
                byte?[] p1 = new byte?[]
                {
                byte.MaxValue, byte.MinValue
                }

                ;
                return p1 > dy;
            }
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.genclass.genclass021.genclass021
{
    // <Title> Tests generic class operator used in iterator.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections;

    public class Test
    {
        private static int s_a = 0;
        private static MemberClassWithNewConstraint<MyClass> s_mc = new MemberClassWithNewConstraint<MyClass>();
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int index = 0;
            foreach (bool s in Test.Increment())
            {
                if (!s)
                    return 1;
                index++;
            }

            if (index == 3)
                return 0;
            return 1;
        }

        public static IEnumerable Increment()
        {
            dynamic dy = s_mc;
            while (s_a < 3)
            {
                s_a++;
                yield return !dy;
            }
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.genclass.genclass022.genclass022
{
    // <Title> Tests generic class operator used in collection initializer list.</Title>
    // <Description> TODO: not implement IEnumerable so can't do object init-er </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
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
            dynamic dy1 = new MemberClassWithNewConstraint<MemberClassWithNewConstraint<MyClass>>();
            MemberClassWithNewConstraint<MemberClassWithNewConstraint<MyClass>>.Status = 3;
            dynamic dy2 = new MemberClassWithNewConstraint<MemberClassWithNewConstraint<MyClass>>();
            MemberClassWithNewConstraint<MemberClassWithNewConstraint<MyClass>>.Status = 4;
            var list = new List<MemberClassWithNewConstraint<MemberClassWithNewConstraint<MyClass>>>()
            {
            dy1++, ++dy2, default (MemberClassWithNewConstraint<MemberClassWithNewConstraint<MyClass>>)}

            ;
            if (list.Count == 3) // TODO: (Status is static) -> && list[0].Status == 3 && list[1].Status == 0 && list[2] == null)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.genclass.genclass024.genclass024
{
    // <Title> Tests generic class operator used in this-argument of extension method.</Title>
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
            MemberClassWithNewConstraint<MyClass> mc = new MemberClassWithNewConstraint<MyClass>();
            dynamic dy = mc;
            return ((string)(~dy)).Method();
        }
    }

    public static class Extension
    {
        public static int Method(this string s)
        {
            if (s == string.Empty)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.genclass.genclass026.genclass026
{
    // <Title> Tests generic class operator used in variable named dynamic.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public class InnerTest : Test
        {
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            MemberClassWithAnotherTypeConstraint<InnerTest, Test> mc = new MemberClassWithAnotherTypeConstraint<InnerTest, Test>();
            dynamic dy = mc;
            dynamic dynamic = (MyStruct[])dy;
            if (dynamic.Length == 1 && dynamic[0].Number == 4)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.genclass.genclass027.genclass027
{
    // <Title> Tests generic class operator used in query expression.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Linq;

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            MemberClassWithNewConstraint<Test> mc = new MemberClassWithNewConstraint<Test>();
            dynamic dy = mc;
            string[] array = new string[]
            {
            null, string.Empty, string.Empty, null, "Test", "a"
            }

            ;
            var result = array.Where(p => p == ~dy).ToArray();
            if (result.Length == 2 && result[0] == string.Empty && result[1] == string.Empty)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.genclass.genclass028.genclass028
{
    // <Title> Tests generic class operator used in null coalescing expression.</Title>
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
            MyClass<string> mc = new MyClass<string>();
            dynamic dy = mc;
            string p = null;
            string result = (dy ^ p) ?? "Test";
            if (result == "Test")
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.genclass.genclass030.genclass030
{
    // <Title> Tests generic class operator used in ctor.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private int? _field;
        public Test()
        {
            dynamic dy = new MemberClassWithAnotherTypeConstraint<string, string>();
            _field = dy;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Test t = new Test();
            if (t._field == int.MinValue)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.genclass.genclass031.genclass031
{
    // <Title> Tests generic class operator used in checked.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(24,9\).*CS0162</Expects>
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
            try
            {
                dynamic dy = new MemberClassWithAnotherTypeConstraint<string, string>();
                int result2 = checked(((int?)dy).Value - 1);
                return 1;
            }
            catch (OverflowException)
            {
                return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.genclass.genclass032.genclass032
{
    // <Title> Tests generic class operator used in + operator.</Title>
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
            MemberClassWithNewConstraint<MyClass> mc = new MemberClassWithNewConstraint<MyClass>();
            dynamic dy = mc;
            string result = ~dy + "Test";
            if (result == "Test")
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.genclass.errorverifier.errorverifier
{
    public enum ErrorElementId
    {
        None,
        SK_METHOD, // method
        SK_CLASS, // type
        SK_NAMESPACE, // namespace
        SK_FIELD, // field
        SK_PROPERTY, // property
        SK_UNKNOWN, // element
        SK_VARIABLE, // variable
        SK_EVENT, // event
        SK_TYVAR, // type parameter
        SK_ALIAS, // using alias
        ERRORSYM, // <error>
        NULL, // <null>
        GlobalNamespace, // <global namespace>
        MethodGroup, // method group
        AnonMethod, // anonymous method
        Lambda, // lambda expression
        AnonymousType, // anonymous type
    }

    public enum ErrorMessageId
    {
        None,
        BadBinaryOps, // Operator '{0}' cannot be applied to operands of type '{1}' and '{2}'
        IntDivByZero, // Division by constant zero
        BadIndexLHS, // Cannot apply indexing with [] to an expression of type '{0}'
        BadIndexCount, // Wrong number of indices inside []; expected '{0}'
        BadUnaryOp, // Operator '{0}' cannot be applied to operand of type '{1}'
        NoImplicitConv, // Cannot implicitly convert type '{0}' to '{1}'
        NoExplicitConv, // Cannot convert type '{0}' to '{1}'
        ConstOutOfRange, // Constant value '{0}' cannot be converted to a '{1}'
        AmbigBinaryOps, // Operator '{0}' is ambiguous on operands of type '{1}' and '{2}'
        AmbigUnaryOp, // Operator '{0}' is ambiguous on an operand of type '{1}'
        ValueCantBeNull, // Cannot convert null to '{0}' because it is a non-nullable value type
        WrongNestedThis, // Cannot access a non-static member of outer type '{0}' via nested type '{1}'
        NoSuchMember, // '{0}' does not contain a definition for '{1}'
        ObjectRequired, // An object reference is required for the non-static field, method, or property '{0}'
        AmbigCall, // The call is ambiguous between the following methods or properties: '{0}' and '{1}'
        BadAccess, // '{0}' is inaccessible due to its protection level
        MethDelegateMismatch, // No overload for '{0}' matches delegate '{1}'
        AssgLvalueExpected, // The left-hand side of an assignment must be a variable, property or indexer
        NoConstructors, // The type '{0}' has no constructors defined
        BadDelegateConstructor, // The delegate '{0}' does not have a valid constructor
        PropertyLacksGet, // The property or indexer '{0}' cannot be used in this context because it lacks the get accessor
        ObjectProhibited, // Member '{0}' cannot be accessed with an instance reference; qualify it with a type name instead
        AssgReadonly, // A readonly field cannot be assigned to (except in a constructor or a variable initializer)
        RefReadonly, // A readonly field cannot be passed ref or out (except in a constructor)
        AssgReadonlyStatic, // A static readonly field cannot be assigned to (except in a static constructor or a variable initializer)
        RefReadonlyStatic, // A static readonly field cannot be passed ref or out (except in a static constructor)
        AssgReadonlyProp, // Property or indexer '{0}' cannot be assigned to -- it is read only
        AbstractBaseCall, // Cannot call an abstract base member: '{0}'
        RefProperty, // A property or indexer may not be passed as an out or ref parameter
        ManagedAddr, // Cannot take the address of, get the size of, or declare a pointer to a managed type ('{0}')
        FixedNotNeeded, // You cannot use the fixed statement to take the address of an already fixed expression
        UnsafeNeeded, // Dynamic calls cannot be used in conjunction with pointers
        BadBoolOp, // In order to be applicable as a short circuit operator a user-defined logical operator ('{0}') must have the same return type as the type of its 2 parameters
        MustHaveOpTF, // The type ('{0}') must contain declarations of operator true and operator false
        CheckedOverflow, // The operation overflows at compile time in checked mode
        ConstOutOfRangeChecked, // Constant value '{0}' cannot be converted to a '{1}' (use 'unchecked' syntax to override)
        AmbigMember, // Ambiguity between '{0}' and '{1}'
        SizeofUnsafe, // '{0}' does not have a predefined size, therefore sizeof can only be used in an unsafe context (consider using System.Runtime.InteropServices.Marshal.SizeOf)
        FieldInitRefNonstatic, // A field initializer cannot reference the non-static field, method, or property '{0}'
        CallingFinalizeDepracated, // Destructors and object.Finalize cannot be called directly. Consider calling IDisposable.Dispose if available.
        CallingBaseFinalizeDeprecated, // Do not directly call your base class Finalize method. It is called automatically from your destructor.
        BadCastInFixed, // The right hand side of a fixed statement assignment may not be a cast expression
        NoImplicitConvCast, // Cannot implicitly convert type '{0}' to '{1}'. An explicit conversion exists (are you missing a cast?)
        InaccessibleGetter, // The property or indexer '{0}' cannot be used in this context because the get accessor is inaccessible
        InaccessibleSetter, // The property or indexer '{0}' cannot be used in this context because the set accessor is inaccessible
        BadArity, // Using the generic {1} '{0}' requires '{2}' type arguments
        BadTypeArgument, // The type '{0}' may not be used as a type argument
        TypeArgsNotAllowed, // The {1} '{0}' cannot be used with type arguments
        HasNoTypeVars, // The non-generic {1} '{0}' cannot be used with type arguments
        NewConstraintNotSatisfied, // '{2}' must be a non-abstract type with a public parameterless constructor in order to use it as parameter '{1}' in the generic type or method '{0}'
        GenericConstraintNotSatisfiedRefType, // The type '{3}' cannot be used as type parameter '{2}' in the generic type or method '{0}'. There is no implicit reference conversion from '{3}' to '{1}'.
        GenericConstraintNotSatisfiedNullableEnum, // The type '{3}' cannot be used as type parameter '{2}' in the generic type or method '{0}'. The nullable type '{3}' does not satisfy the constraint of '{1}'.
        GenericConstraintNotSatisfiedNullableInterface, // The type '{3}' cannot be used as type parameter '{2}' in the generic type or method '{0}'. The nullable type '{3}' does not satisfy the constraint of '{1}'. Nullable types can not satisfy any interface constraints.
        GenericConstraintNotSatisfiedTyVar, // The type '{3}' cannot be used as type parameter '{2}' in the generic type or method '{0}'. There is no boxing conversion or type parameter conversion from '{3}' to '{1}'.
        GenericConstraintNotSatisfiedValType, // The type '{3}' cannot be used as type parameter '{2}' in the generic type or method '{0}'. There is no boxing conversion from '{3}' to '{1}'.
        TypeVarCantBeNull, // Cannot convert null to type parameter '{0}' because it could be a non-nullable value type. Consider using 'default({0})' instead.
        BadRetType, // '{1} {0}' has the wrong return type
        CantInferMethTypeArgs, // The type arguments for method '{0}' cannot be inferred from the usage. Try specifying the type arguments explicitly.
        MethGrpToNonDel, // Cannot convert method group '{0}' to non-delegate type '{1}'. Did you intend to invoke the method?
        RefConstraintNotSatisfied, // The type '{2}' must be a reference type in order to use it as parameter '{1}' in the generic type or method '{0}'
        ValConstraintNotSatisfied, // The type '{2}' must be a non-nullable value type in order to use it as parameter '{1}' in the generic type or method '{0}'
        CircularConstraint, // Circular constraint dependency involving '{0}' and '{1}'
        BaseConstraintConflict, // Type parameter '{0}' inherits conflicting constraints '{1}' and '{2}'
        ConWithValCon, // Type parameter '{1}' has the 'struct' constraint so '{1}' cannot be used as a constraint for '{0}'
        AmbigUDConv, // Ambiguous user defined conversions '{0}' and '{1}' when converting from '{2}' to '{3}'
        PredefinedTypeNotFound, // Predefined type '{0}' is not defined or imported
        PredefinedTypeBadType, // Predefined type '{0}' is declared incorrectly
        BindToBogus, // '{0}' is not supported by the language
        CantCallSpecialMethod, // '{0}': cannot explicitly call operator or accessor
        BogusType, // '{0}' is a type not supported by the language
        MissingPredefinedMember, // Missing compiler required member '{0}.{1}'
        LiteralDoubleCast, // Literal of type double cannot be implicitly converted to type '{1}'; use an '{0}' suffix to create a literal of this type
        UnifyingInterfaceInstantiations, // '{0}' cannot implement both '{1}' and '{2}' because they may unify for some type parameter substitutions
        ConvertToStaticClass, // Cannot convert to static type '{0}'
        GenericArgIsStaticClass, // '{0}': static types cannot be used as type arguments
        PartialMethodToDelegate, // Cannot create delegate from method '{0}' because it is a partial method without an implementing declaration
        IncrementLvalueExpected, // The operand of an increment or decrement operator must be a variable, property or indexer
        NoSuchMemberOrExtension, // '{0}' does not contain a definition for '{1}' and no extension method '{1}' accepting a first argument of type '{0}' could be found (are you missing a using directive or an assembly reference?)
        ValueTypeExtDelegate, // Extension methods '{0}' defined on value type '{1}' cannot be used to create delegates
        BadArgCount, // No overload for method '{0}' takes '{1}' arguments
        BadArgTypes, // The best overloaded method match for '{0}' has some invalid arguments
        BadArgType, // Argument '{0}': cannot convert from '{1}' to '{2}'
        RefLvalueExpected, // A ref or out argument must be an assignable variable
        BadProtectedAccess, // Cannot access protected member '{0}' via a qualifier of type '{1}'; the qualifier must be of type '{2}' (or derived from it)
        BindToBogusProp2, // Property, indexer, or event '{0}' is not supported by the language; try directly calling accessor methods '{1}' or '{2}'
        BindToBogusProp1, // Property, indexer, or event '{0}' is not supported by the language; try directly calling accessor method '{1}'
        BadDelArgCount, // Delegate '{0}' does not take '{1}' arguments
        BadDelArgTypes, // Delegate '{0}' has some invalid arguments
        AssgReadonlyLocal, // Cannot assign to '{0}' because it is read-only
        RefReadonlyLocal, // Cannot pass '{0}' as a ref or out argument because it is read-only
        ReturnNotLValue, // Cannot modify the return value of '{0}' because it is not a variable
        BadArgExtraRef, // Argument '{0}' should not be passed with the '{1}' keyword
        // DelegateOnConditional, // Cannot create delegate with '{0}' because it has a Conditional attribute (REMOVED)
        BadArgRef, // Argument '{0}' must be passed with the '{1}' keyword
        AssgReadonly2, // Members of readonly field '{0}' cannot be modified (except in a constructor or a variable initializer)
        RefReadonly2, // Members of readonly field '{0}' cannot be passed ref or out (except in a constructor)
        AssgReadonlyStatic2, // Fields of static readonly field '{0}' cannot be assigned to (except in a static constructor or a variable initializer)
        RefReadonlyStatic2, // Fields of static readonly field '{0}' cannot be passed ref or out (except in a static constructor)
        AssgReadonlyLocalCause, // Cannot assign to '{0}' because it is a '{1}'
        RefReadonlyLocalCause, // Cannot pass '{0}' as a ref or out argument because it is a '{1}'
        ThisStructNotInAnonMeth, // Anonymous methods, lambda expressions, and query expressions inside structs cannot access instance members of 'this'. Consider copying 'this' to a local variable outside the anonymous method, lambda expression or query expression and using the local instead.
        DelegateOnNullable, // Cannot bind delegate to '{0}' because it is a member of 'System.Nullable<T>'
        BadCtorArgCount, // '{0}' does not contain a constructor that takes '{1}' arguments
        BadExtensionArgTypes, // '{0}' does not contain a definition for '{1}' and the best extension method overload '{2}' has some invalid arguments
        BadInstanceArgType, // Instance argument: cannot convert from '{0}' to '{1}'
        BadArgTypesForCollectionAdd, // The best overloaded Add method '{0}' for the collection initializer has some invalid arguments
        InitializerAddHasParamModifiers, // The best overloaded method match '{0}' for the collection initializer element cannot be used. Collection initializer 'Add' methods cannot have ref or out parameters.
        NonInvocableMemberCalled, // Non-invocable member '{0}' cannot be used like a method.
        NamedArgumentSpecificationBeforeFixedArgument, // Named argument specifications must appear after all fixed arguments have been specified
        BadNamedArgument, // The best overload for '{0}' does not have a parameter named '{1}'
        BadNamedArgumentForDelegateInvoke, // The delegate '{0}' does not have a parameter named '{1}'
        DuplicateNamedArgument, // Named argument '{0}' cannot be specified multiple times
        NamedArgumentUsedInPositional, // Named argument '{0}' specifies a parameter for which a positional argument has already been given
    }

    public enum RuntimeErrorId
    {
        None,
        // RuntimeBinderInternalCompilerException
        InternalCompilerError, // An unexpected exception occurred while binding a dynamic operation
        // ArgumentException
        BindRequireArguments, // Cannot bind call with no calling object
        // RuntimeBinderException
        BindCallFailedOverloadResolution, // Overload resolution failed
        // ArgumentException
        BindBinaryOperatorRequireTwoArguments, // Binary operators must be invoked with two arguments
        // ArgumentException
        BindUnaryOperatorRequireOneArgument, // Unary operators must be invoked with one argument
        // RuntimeBinderException
        BindPropertyFailedMethodGroup, // The name '{0}' is bound to a method and cannot be used like a property
        // RuntimeBinderException
        BindPropertyFailedEvent, // The event '{0}' can only appear on the left hand side of += or -=
        // RuntimeBinderException
        BindInvokeFailedNonDelegate, // Cannot invoke a non-delegate type
        // ArgumentException
        BindImplicitConversionRequireOneArgument, // Implicit conversion takes exactly one argument
        // ArgumentException
        BindExplicitConversionRequireOneArgument, // Explicit conversion takes exactly one argument
        // ArgumentException
        BindBinaryAssignmentRequireTwoArguments, // Binary operators cannot be invoked with one argument
        // RuntimeBinderException
        BindBinaryAssignmentFailedNullReference, // Cannot perform member assignment on a null reference
        // RuntimeBinderException
        NullReferenceOnMemberException, // Cannot perform runtime binding on a null reference
        // RuntimeBinderException
        BindCallToConditionalMethod, // Cannot dynamically invoke method '{0}' because it has a Conditional attribute
        // RuntimeBinderException
        BindToVoidMethodButExpectResult, // Cannot implicitly convert type 'void' to 'object'
        // EE?
        EmptyDynamicView, // No further information on this object could be discovered
        // MissingMemberException
        GetValueonWriteOnlyProperty, // Write Only properties are not supported
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.genclass.genericuserconversion002.genericuserconversion002
{
    // <Area> User-defined conversions </Area>
    // <Title> User defined conversions </Title>
    // <Description>
    // Ambiguous user defined conversion (dynamic case)
    // </Description>
    //<RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class A
    {
    }

    public class B : A
    {
    }

    public class C
    {
        public static implicit operator B(C s)
        {
            System.Console.WriteLine(1);
            return new B();
        }
    }

    public class D : C
    {
        public static implicit operator A(D s)
        {
            System.Console.WriteLine(2);
            return new B();
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            var retval = 1; // failure
            dynamic e2 = new D();
            try
            {
                B b2 = (B)e2; // CS0457: Ambiguous user defined conversions 'D.implicit operator A(D)' and 'C.implicit operator B(C)'
                              //         when converting from 'D' to 'B'
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                // Only match the expected error string
                {
                    retval = 0;
                }
            }

            return retval;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.genclass.genericuserconversion004.genericuserconversion004
{
    // <Area> User-defined conversions </Area>
    // <Title> User defined conversions </Title>
    // <Description>
    // Ambiguous user defined conversion (dynamic case)
    // </Description>
    //<RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class A
    {
        public class B : A
        {
        }
    }

    public class C
    {
        public static implicit operator A.B(C s)
        {
            return new A.B();
        }
    }

    public class D : C
    {
        public static implicit operator A(D s)
        {
            return new A.B();
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            var retval = 1; // failure
            dynamic e2 = new D();
            try
            {
                A.B b2 = (A.B)e2; // CS0457: Ambiguous user defined conversions 'D.implicit operator A(D)' and 'C.implicit operator B(C)'
                                  //         when converting from 'D' to 'B'
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                // Only match the expected error string
                {
                    retval = 0;
                }
            }

            return retval;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.genclass.genericuserconversion006.genericuserconversion006
{
    // <Area> User-defined conversions </Area>
    // <Title> User defined conversions </Title>
    // <Description>
    // Ambiguous user defined conversion (dynamic case)
    // </Description>
    //<RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    //<Expects Status=warning>\(30,13\).*CS0219</Expects>
    // <Code>

    public class A
    {
        public static implicit operator A(D s)
        {
            return new A();
        }
    }

    public class B : A
    {
        public static implicit operator B(C s)
        {
            return new B();
        }
    }

    public class C
    {
    }

    public class D : C
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            var retval = 1; // failure
            var errorString = @"Ambiguous user defined conversions 'A.implicit operator A(D)' and 'B.implicit operator B(C)' when converting from 'D' to 'B'";
            dynamic e2 = new D();
            try
            {
                B b2 = (B)e2; // CS0457: Ambiguous user defined conversions 'A.implicit operator A(D)' and 'B.implicit operator B(C)'
                              //         when converting from 'D' to 'B'
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                // Only match the expected error string
                {
                    retval = 0;
                }
            }

            return retval;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.genclass.genericuserconversion008.genericuserconversion008
{
    // <Area> User-defined conversions </Area>
    // <Title> User defined conversions </Title>
    // <Description>
    // Ambiguous user defined conversion (static case)
    // </Description>
    //<RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    //<Expects Status=warning>\(40,16\).*CS0168</Expects>
    // <Code>

    public class A<T>
    {
    }

    public class B<T> : A<T>
    {
    }

    public class C<T>
    {
        public static implicit operator B<T>(C<T> s)
        {
            System.Console.WriteLine(1);
            return new B<T>();
        }
    }

    public class D<T> : C<T>
    {
        public static implicit operator A<T>(D<T> s)
        {
            System.Console.WriteLine(2);
            return new B<T>();
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
            int retval = 3;
            string errorString;
            dynamic e1 = new D<int>();
            try
            {
                var b1 = (B<int>)e1; // CS0457: Ambiguous user defined conversions 'D<int>.implicit operator A<int>(D<int>)' and 'C<int>.implicit operator B<int>(C<int>)'
                                     //         when converting from 'D<int>' to 'B<int>'
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                // Only match the expected error string
                {
                    retval--;
                }
            }

            dynamic e2 = new D<object>();
            try
            {
                var b2 = (B<object>)e2; // CS0457: Ambiguous user defined conversions 'D<object>.implicit operator A<object>(D<object>)' and 'C<object>.implicit operator B<object>(C<object>)'
                                        //         when converting from 'D<object>' to 'B<object>'
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                // Only match the expected error string
                {
                    retval--;
                }
            }

            dynamic e3 = new D<dynamic>();
            try
            {
                var b3 = (B<dynamic>)e3; // CS0457: Ambiguous user defined conversions 'D<dynamic>.implicit operator A<dynamic>(D<dynamic>)' and 'C<dynamic>.implicit operator B<dynamic>(C<dynamic>)'
                                         //         when converting from 'D<dynamic>' to 'B<dynamic>'
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                // Only match the expected error string
                {
                    retval--;
                }
            }

            return retval == 0 ? retval : 1;
        }
    }
    // </Code>
}
