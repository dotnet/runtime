// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regclassoperate.regclassoperate;
using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regclassoperate.regclassoperate
{
    public class MyClass
    {
        public int Field = 0;
        public static MyEnum?[] operator -(MyClass p1, dynamic[] p2)
        {
            return new MyEnum?[]
            {
            MyEnum.First, null
            }

            ;
        }

        public static MyStruct operator ^(MyClass p1, float p2)
        {
            return new MyStruct()
            {
                Number = 4
            }

            ;
        }

        public static MyClass[] operator /(MyClass p1, float? p2)
        {
            return new MyClass[]
            {
            new MyClass()
            {
            Field = 1
            }

            , new MyClass()
            {
            Field = 4
            }
            }

            ;
        }

        public static MyStruct?[] operator <=(MyClass p1, int p2)
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

        public static MyStruct?[] operator >=(MyClass p1, int p2)
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

        public static MyClass operator >>(MyClass p1, int p2)
        {
            return new MyClass()
            {
                Field = int.MinValue
            }

            ;
        }

        public static bool operator +(MyClass p1, int?[] p2)
        {
            return false;
        }

        public static bool? operator &(bool p1, MyClass p2)
        {
            return true;
        }

        public static decimal[] operator |(bool? p1, MyClass p2)
        {
            return new decimal[]
            {
            decimal.MaxValue
            }

            ;
        }

        public static MyEnum? operator <(byte?[] p1, MyClass p2)
        {
            return MyEnum.First;
        }

        public static MyEnum? operator >(byte?[] p1, MyClass p2)
        {
            return MyEnum.Third;
        }

        public static bool operator <(MyClass p1, int p2)
        {
            return p1.Field < p2;
        }

        public static bool operator >(MyClass p1, int p2)
        {
            return p1.Field > p2;
        }

        public static float operator ^(decimal? p1, MyClass p2)
        {
            return float.Epsilon;
        }

        public static byte operator +(decimal[] p1, MyClass p2)
        {
            return byte.MinValue;
        }

        public static decimal[] operator -(dynamic p1, MyClass p2)
        {
            return new decimal[]
            {
            decimal.MaxValue
            }

            ;
        }

        public static string operator *(dynamic[] p1, MyClass p2)
        {
            return string.Empty;
        }

        public static MyStruct[] operator |(MyClass p1, short[] p2)
        {
            return new MyStruct[]
            {
            new MyStruct()
            {
            Number = 3
            }
            }

            ;
        }

        public static dynamic[] operator &(MyClass p1, string p2)
        {
            return new dynamic[]
            {
            p1
            }

            ;
        }

        public static int?[] operator -(MyClass p1, ulong[] p2)
        {
            return new int?[]
            {
            null, int.MinValue
            }

            ;
        }

        public static MyEnum operator +(MyClass p1)
        {
            return MyEnum.First;
        }

        public static bool? operator !(MyClass p1)
        {
            return true;
        }

        public static string operator ~(MyClass p1)
        {
            return string.Empty;
        }

        public static MyClass operator ++(MyClass p1)
        {
            return new MyClass()
            {
                Field = 4
            }

            ;
        }

        public static bool operator true(MyClass p1)
        {
            return false;
        }

        public static bool operator false(MyClass p1)
        {
            return true;
        }

        public static MyClass operator |(MyClass p1, MyClass p2)
        {
            return p2;
        }

        public static dynamic[] operator -(MyClass p1)
        {
            return new dynamic[]
            {
            p1
            }

            ;
        }

        public static MyClass operator --(MyClass p1)
        {
            return new MyClass()
            {
                Field = 4
            }

            ;
        }

        public static explicit operator MyClass(char p1)
        {
            return new MyClass()
            {
                Field = 4
            }

            ;
        }

        public static explicit operator MyClass(char? p1)
        {
            return new MyClass()
            {
                Field = 4
            }

            ;
        }

        public static implicit operator MyClass(double[] p1)
        {
            return new MyClass()
            {
                Field = 4
            }

            ;
        }

        // public static implicit operator MyClass(dynamic p1) { return new MyClass() { Field = 4 }; }
        public static implicit operator MyClass(dynamic[] p1)
        {
            return new MyClass()
            {
                Field = 4
            }

            ;
        }

        public static implicit operator float (MyClass p1)
        {
            return float.Epsilon;
        }

        public static implicit operator int?[] (MyClass p1)
        {
            return null;
        }

        // public static implicit operator dynamic(MyClass p1) { return p1; }
        public static implicit operator MyStruct[] (MyClass p1)
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

        public static implicit operator int? (MyClass p1)
        {
            return int.MinValue;
        }

        public static explicit operator object[] (MyClass p1)
        {
            return new object[]
            {
            p1
            }

            ;
        }

        public static explicit operator MyEnum[] (MyClass p1)
        {
            return new MyEnum[]
            {
            MyEnum.First
            }

            ;
        }

        public static implicit operator short[] (MyClass p1)
        {
            return new short[]
            {
            short.MaxValue
            }

            ;
        }

        public static explicit operator MyClass(MyStruct?[] p1)
        {
            return new MyClass()
            {
                Field = 3
            }

            ;
        }
    }

    public struct MyStruct
    {
        public int Number;
        public static decimal? operator !=(MyStruct p1, dynamic p2)
        {
            return decimal.MinValue;
        }

        public static decimal? operator ==(MyStruct p1, dynamic p2)
        {
            return decimal.MaxValue;
        }

        // required
        public override bool Equals(object d)
        {
            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        // ---
        public static MyEnum?[] operator <<(MyStruct p1, int p2)
        {
            return new MyEnum?[]
            {
            null, MyEnum.First
            }

            ;
        }

        public static MyEnum? operator +(MyStruct p1, MyClass[] p2)
        {
            return null;
        }

        public static dynamic operator +(MyStruct p1, MyEnum p2)
        {
            return true;
        }

        public static MyClass operator |(MyStruct p1, MyEnum[] p2)
        {
            return new MyClass()
            {
                Field = int.MinValue
            }

            ;
        }

        public static MyEnum operator +(char p1, MyStruct p2)
        {
            return MyEnum.First;
        }

        public static int? operator ^(char? p1, MyStruct p2)
        {
            return int.MaxValue;
        }

        public static byte operator &(double[] p1, MyStruct p2)
        {
            return byte.MaxValue;
        }

        public static MyStruct? operator /(MyEnum?[] p1, MyStruct p2)
        {
            return new MyStruct
            {
                Number = 5
            }

            ;
        }

        public static double[] operator <(MyStruct?[] p1, MyStruct p2)
        {
            return new double[]
            {
            double.MinValue
            }

            ;
        }

        public static double[] operator >(MyStruct?[] p1, MyStruct p2)
        {
            return new double[]
            {
            double.MaxValue
            }

            ;
        }

        public static object[] operator |(MyStruct p1, MyStruct? p2)
        {
            return new object[]
            {
            p1, p2
            }

            ;
        }

        public static MyEnum[] operator &(MyStruct p1, MyStruct[] p2)
        {
            return new MyEnum[]
            {
            MyEnum.First, MyEnum.Third
            }

            ;
        }

        public static float? operator %(MyStruct p1, object[] p2)
        {
            return float.Epsilon;
        }

        public static object[] operator -(MyStruct p1)
        {
            return new object[]
            {
            p1
            }

            ;
        }

        public static MyStruct operator ++(MyStruct p1)
        {
            return new MyStruct()
            {
                Number = 4
            }

            ;
        }

        public static dynamic operator ~(MyStruct p1)
        {
            return p1;
        }

        public static bool operator true(MyStruct p1)
        {
            return false;
        }

        public static bool operator false(MyStruct p1)
        {
            return true;
        }

        public static decimal? operator +(MyStruct p1)
        {
            return decimal.MaxValue;
        }

        public static byte operator !(MyStruct p1)
        {
            return byte.MinValue;
        }

        public static implicit operator MyStruct(bool p1)
        {
            return new MyStruct()
            {
                Number = 3
            }

            ;
        }

        public static implicit operator MyStruct(bool? p1)
        {
            return new MyStruct()
            {
                Number = 4
            }

            ;
        }

        public static implicit operator MyStruct(byte p1)
        {
            return new MyStruct()
            {
                Number = 5
            }

            ;
        }

        public static implicit operator MyStruct(byte?[] p1)
        {
            return new MyStruct()
            {
                Number = 6
            }

            ;
        }

        public static explicit operator MyStruct(decimal? p1)
        {
            return new MyStruct()
            {
                Number = 7
            }

            ;
        }

        public static explicit operator MyStruct(decimal[] p1)
        {
            return new MyStruct()
            {
                Number = 8
            }

            ;
        }

        public static implicit operator MyStruct(MyEnum? p1)
        {
            return new MyStruct()
            {
                Number = 9
            }

            ;
        }

        public static implicit operator MyStruct(MyEnum?[] p1)
        {
            return new MyStruct()
            {
                Number = 10
            }

            ;
        }

        public static explicit operator MyClass[] (MyStruct p1)
        {
            return new MyClass[]
            {
            new MyClass()
            {
            Field = 3
            }
            }

            ;
        }

        public static implicit operator ulong[] (MyStruct p1)
        {
            return new ulong[]
            {
            ulong.MaxValue
            }

            ;
        }

        public static explicit operator int (MyStruct p1)
        {
            return int.MinValue;
        }

        public static implicit operator string (MyStruct p1)
        {
            return string.Empty;
        }

        // public static explicit operator dynamic(MyStruct p1) { return p1; }
        public static implicit operator float? (MyStruct p1)
        {
            return null;
        }

        public static implicit operator MyEnum(MyStruct p1)
        {
            return MyEnum.First;
        }

        public static implicit operator dynamic[] (MyStruct p1)
        {
            return new dynamic[]
            {
            p1
            }

            ;
        }
    }

    public enum MyEnum
    {
        First = 1,
        Second = 2,
        Third = 3
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regclass001.regclass001
{
    // <Title> Tests regular class operator used in regular method body.</Title>
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
            MyClass mc = new MyClass();
            dynamic dy = mc;
            dynamic[] p2 = new dynamic[]
            {
            dy, dy, null
            }

            ;
            MyEnum?[] result = dy - p2;
            if (result.Length == 2 && result[0] == MyEnum.First && result[1] == null)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regclass002.regclass002
{
    // <Title> Tests regular class operator used in static method body.</Title>
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
            return TestMethod(new MyClass());
        }

        public static int TestMethod(MyClass mc)
        {
            dynamic dy = mc;
            float p2 = 1.33f;
            MyStruct ms = dy ^ p2;
            if (ms.Number == 4)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regclass002a.regclass002a
{
    // <Title> Tests regular class operator used in static method body.</Title>
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
            return TestMethod(new MyClass());
        }

        public static int TestMethod(MyClass mc)
        {
            dynamic dy = mc;
            MyStruct ms = dy ^ 1.33f;
            if (ms.Number == 4)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regclass003.regclass003
{
    // <Title> Tests regular class operator used in generic method body.</Title>
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
            return TestMethod<string, int>(new MyClass());
        }

        public static int TestMethod<U, V>(MyClass mc)
        {
            dynamic dy = mc;
            bool? p1 = null;
            decimal[] result = p1 | dy;
            if (result.Length == 1 && result[0] == decimal.MaxValue)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regclass004.regclass004
{
    // <Title> Tests regular class operator used in extension method body.</Title>
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
            MyClass mc = new MyClass();
            MyEnum me = mc.ExReturnMyEnum();
            if (me == MyEnum.First)
                return 0;
            return 1;
        }
    }

    public static class Extension
    {
        public static MyEnum ExReturnMyEnum(this MyClass mc)
        {
            dynamic dy = mc;
            return +dy;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regclass007.regclass007
{
    // <Title> Tests regular class operator used in arguments to method invocation.</Title>
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
            MyClass mc = new MyClass();
            dynamic dy = mc;
            float? p2 = null;
            Test t = new Test();
            return t.TestMethod(dy / p2);
        }

        public int TestMethod(MyClass[] mca)
        {
            foreach (var mc in mca)
            {
                //System.Console.WriteLine(mc);
            }

            if (mca.Length == 2 && mca[mca.Length - 1].Field == 4)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regclass007a.regclass007a
{
    // <Title> Tests regular class operator used in arguments to method invocation.</Title>
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
            MyClass mc = new MyClass();
            dynamic dy = mc;
            Test t = new Test();
            return t.TestMethod(dy / null);
        }

        public int TestMethod(MyClass[] mca)
        {
            foreach (var mc in mca)
            {
                //System.Console.WriteLine(mc);
            }

            if (mca.Length == 2 && mca[mca.Length - 1].Field == 4)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regclass008.regclass008
{
    // <Title> Tests regular class operator used in arguments to method invocation.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private delegate int MyDec(MyStruct?[] msa);
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            MyClass mc = new MyClass();
            dynamic dy = mc;
            int p2 = 1;
            Test t = new Test();
            MyDec md = new MyDec(t.TestMethod);
            // public static MyStruct?[] operator <=(MyClass p1, int p2) { return new MyStruct?[] { null, new MyStruct() { Number = int.MinValue } }; }
            return md(mc <= p2);
        }

        public int TestMethod(MyStruct?[] msa)
        {
            foreach (var mc in msa)
            {
                //System.Console.WriteLine(mc);
            }

            // op == is overridden
            if (msa.Length == 2 && (!msa[0].HasValue) && (msa[1].HasValue && (msa[1].Value == null) == decimal.MaxValue))
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regclass008a.regclass008a
{
    // <Title> Tests regular class operator used in arguments to method invocation.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private delegate int MyDec(MyStruct?[] msa);
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            MyClass mc = new MyClass();
            dynamic dy = mc;
            Test t = new Test();
            MyDec md = new MyDec(t.TestMethod);
            // public static MyStruct?[] operator <=(MyClass p1, int p2) { return new MyStruct?[] { null, new MyStruct() { Number = int.MinValue } }; }
            return md(mc <= 1);
        }

        public int TestMethod(MyStruct?[] msa)
        {
            foreach (var mc in msa)
            {
                //System.Console.WriteLine(mc);
            }

            // op == is overridden
            if (msa.Length == 2 && (!msa[0].HasValue) && (msa[1].HasValue && (msa[1].Value == null) == decimal.MaxValue))
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regclass009.regclass009
{
    // <Title> Tests regular class operator used in implicitly-typed variable initializer.</Title>
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
            MyClass mc = new MyClass();
            dynamic dy = mc;
            var result = dy >= int.MaxValue;
            if (result.Length == 2 && !((MyStruct?)result[0]).HasValue && ((MyStruct?)result[1]).Value.Number == int.MaxValue)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regclass009a.regclass009a
{
    // <Title> Tests regular class operator used in implicitly-typed variable initializer.</Title>
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
            MyClass mc = new MyClass();
            dynamic dy = mc;
            var result = dy >= 2147483647;
            if (result.Length == 2 && !((MyStruct?)result[0]).HasValue && ((MyStruct?)result[1]).Value.Number == int.MaxValue)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regclass010.regclass010
{
    // <Title> Tests regular class operator used in array initializer.</Title>
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
            MyClass mc = new MyClass();
            dynamic dy = mc;
            MyClass[] result = new MyClass[]
            {
            dy >> 10
            }

            ;
            if (result.Length == 1 && result[0].Field == int.MinValue)
                return 0;
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regclass011.regclass011
{
    // <Title> Tests regular class operator used in implicitly-typed array initializer.</Title>
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
            MyClass mc = new MyClass();
            dynamic dy = mc;
            var result = new MyClass[]
            {
            dy >> 10
            }

            ;
            if (result.Length == 1 && result[0].Field == int.MinValue)
                return 0;
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regclass012.regclass012
{
    // <Title> Tests regular class operator used in member initializer of object initializer.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public class InnerTest
        {
            public bool? Field;
            public byte MyProp
            {
                get;
                set;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            MyClass mc = new MyClass();
            dynamic dy = mc;
            int?[] p2 = new int?[]
            {
            int.MinValue, null
            }

            ;
            decimal[] p1 = null;
            var result = new InnerTest()
            {
                Field = dy + p2,
                MyProp = p1 + dy
            }

            ;
            if (result.Field == false && result.MyProp == byte.MinValue)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regclass013.regclass013
{
    // <Title> Tests regular class operator used in member initializer of anonymous type.</Title>
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
            MyClass mc = new MyClass();
            dynamic dy = mc;
            int?[] p2 = new int?[]
            {
            int.MinValue, null
            }

            ;
            decimal[] p1 = null;
            var result = new
            {
                Field = dy + p2,
                MyProp = p1 + dy
            }

            ;
            if (result.Field == false && result.MyProp == byte.MinValue)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regclass014.regclass014
{
    // <Title> Tests regular class operator used in static variable.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private static dynamic s_dy = new MyClass();
        private static bool? s_result = default(bool) & s_dy;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            if (Test.s_result == true)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regclass015.regclass015
{
    // <Title> Tests regular class operator used in property get.</Title>
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
            if (t.MyProp == MyEnum.Third)
                return 0;
            return 1;
        }

        public MyEnum? MyProp
        {
            get
            {
                MyClass mc = new MyClass();
                dynamic dy = mc;
                byte?[] p1 = new byte?[]
                {
                1, null
                }

                ;
                return p1 > dy;
            }
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regclass016.regclass016
{
    // <Title> Tests regular class operator used in property set.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private static MyEnum? s_result;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Test.MyProp = null;
            if (s_result == MyEnum.Third)
                return 0;
            return 1;
        }

        public static MyEnum? MyProp
        {
            set
            {
                MyClass mc = new MyClass();
                dynamic dy = mc;
                byte?[] p1 = new byte?[]
                {
                1, null
                }

                ;
                s_result = p1 > dy;
            }
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regclass017.regclass017
{
    // <Title> Tests regular class operator used in indexer body.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private static MyClass s_mc = new MyClass();
        private float _field;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Test t = new Test();
            t[10] = 1.23f;
            if (t._field != float.Epsilon)
                return 1;
            float f = t[int.MinValue];
            if (f != float.Epsilon)
                return 1;
            return 0;
        }

        public float this[int i]
        {
            set
            {
                dynamic dy = s_mc;
                decimal? p1 = decimal.MaxValue;
                _field = p1 ^ dy;
            }

            get
            {
                dynamic dy = s_mc;
                decimal? p1 = decimal.MinValue;
                return p1 ^ dy;
            }
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regclass018.regclass018
{
    // <Title> Tests regular class operator used in iterator.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections;

    public class Test
    {
        private static int s_a = 0;
        private static MyClass s_mc = new MyClass();
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int index = 0;
            foreach (string s in Test.Increment())
            {
                if (s != string.Empty)
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
                yield return ~dy;
            }
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regclass019.regclass019
{
    // <Title> Tests regular class operator used in collection initializer list.</Title>
    // <Description>
    // </Description>
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
            dynamic dy1 = new MyClass()
            {
                Field = 3
            }

            ;
            dynamic dy2 = new MyClass()
            {
                Field = 3
            }

            ;
            double[] ds = null;
            List<MyClass> list = new List<MyClass>()
            {
            dy1++, ++dy2, ds
            }

            ;
            if (list.Count == 3 && list[0].Field == 3 && list[1].Field == 4 && list[2].Field == 4)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regclass020.regclass020
{
    // <Title> Tests regular class operator used in ctor.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private MyClass _result;
        public Test()
        {
            dynamic dy1 = new MyClass()
            {
                Field = 1
            }

            ;
            dynamic dy2 = new MyClass()
            {
                Field = 2
            }

            ;
            _result = dy1 && dy2; //T.false(x) ? x : T.&(x, y)
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Test t = new Test();
            if (t._result.Field == 1)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regclass021.regclass021
{
    // <Title> Tests regular class operator used in static ctor.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private static MyClass s_result;
        static Test()
        {
            dynamic dy1 = new MyClass()
            {
                Field = 1
            }

            ;
            dynamic dy2 = new MyClass()
            {
                Field = 2
            }

            ;
            s_result = dy1 || dy2; //T.true(x) ? x : T.|(x, y),
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            if (Test.s_result.Field == 2)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regclass022.regclass022
{
    // <Title> Tests regular class operator used in checked and unchecked.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(32,9\).*CS0162</Expects>
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
            MyClass mc = new MyClass();
            dynamic dy = mc;
            ulong[] p2 = new ulong[0];
            int result1 = unchecked((int)(dy - p2)[1] - 1);
            if (result1 != 2147483647)
                return 1;
            try
            {
                int result2 = checked((int)(dy - p2)[1] - 1);
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regclass024.regclass024
{
    // <Title> Tests regular class operator used in short-circuit boolean expression.</Title>
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
            int index = 0;
            while (true)
            {
                MyClass mc = new MyClass();
                dynamic dy = mc;
                decimal[] result = dy - mc;
                if (result.Length != 1 || result[0] != decimal.MaxValue)
                    return 1;
                if (index++ == 10)
                    break;
            }

            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regclass025.regclass025
{
    // <Title> Tests regular class operator used in dtor.</Title>
    // <Description>
    // On IA64 the GC.WaitForPendingFinalizers() does not actually work...
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System;
    using System.Runtime.CompilerServices;

    public class Test
    {
        private static MyEnum[] s_field = null;
        public static object locker = new object();
        ~Test()
        {
            lock (locker)
            {
                MyClass mc = new MyClass();
                dynamic dy = mc;
                s_field = (MyEnum[])dy;
            }
        }

        private static int Verify()
        {
            lock (Test.locker)
            {
                if (Test.s_field == null)
                {
                    System.Console.WriteLine("Failed: the finalizer hasn't executed!");
                    return 2;
                }

                if (Test.s_field.Length != 1 || Test.s_field[0] != MyEnum.First)
                {
                    return 1;
                }
            }

            return 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void RequireLifetimesEnded()
        {
            Test t = new Test();
            Test.s_field = null;
            GC.KeepAlive(t);
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            RequireLifetimesEnded();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            // If move the code in Verify() to here, the finalizer will only be executed after exited Main
            return Verify();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regclass026.regclass026
{
    // <Title> Tests regular class operator used in unsafe.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    //using System;
    //[TestClass]public class Test
    //{
    //[Test][Priority(Priority.Priority2)]public void DynamicCSharpRunTest(){Assert.AreEqual(0, MainMethod());} public static unsafe int MainMethod()
    //{
    //char value = 'a';
    //char* valuePtr = &value;
    //dynamic dy = (MyClass)(*valuePtr);
    //if (dy.Field == 4)
    //return 0;
    //return 1;
    //}
    //}
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regclass027.regclass027
{
    // <Title> Tests regular class operator used in foreach expression.</Title>
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
            MyClass mc = new MyClass();
            dynamic dy = mc;
            short[] p2 = new short[]
            {
            1, 2, 3, 4
            }

            ;
            int index = 0;
            foreach (MyStruct i in dy | p2)
            {
                if (i.Number != 3)
                    return 1;
                index++;
            }

            if (index == 1)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regclass028.regclass028
{
    // <Title> Tests regular class operator used in for loop initializer.</Title>
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
            MyClass mc = new MyClass();
            dynamic dy = mc;
            int?[] result = new int?[10];
            int index = 0;
            for (int i = (new dynamic[]
            {
            dy
            }

            * dy).Length; i < 10; i++)
            {
                result = dy;
                index++;
            }

            if (index == 10 && result == null)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regclass029.regclass029
{
    // <Title> Tests regular class operator used in lock expression.</Title>
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
            MyClass mc = new MyClass()
            {
                Field = 10
            }

            ;
            dynamic dy = mc;
            dynamic[] result = null;
            lock (--dy) // set Field=4
            {
                result = -dy;
            }

            if (result.Length == 1 && ((MyClass)result[0]).Field == 4)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regclass030.regclass030
{
    // <Title> Tests regular class operator used in generic method body.</Title>
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
            return new Test().TestMethod<Test>();
        }

        private int TestMethod<T>()
        {
            char? p1 = null;
            dynamic myclass = (MyClass)p1;
            if (myclass.Field == 4)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regclass031.regclass031
{
    // <Title> Tests regular class operator used in while loop body.</Title>
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
            dynamic dy = new double[]
            {
            double.Epsilon, double.NaN
            }

            ;
            int index = 0;
            MyClass result = default(MyClass);
            while (index < 10)
            {
                result = dy;
                index++;
            }

            if (result.Field == 4)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regclass032.regclass032
{
    // <Title> Tests regular class operator used in + operator.</Title>
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
            MyClass mc = new MyClass();
            dynamic dy = mc;
            string result = ~dy + "Test";
            if (result == "Test")
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regclass034.regclass034
{
    // <Title> Tests regular class operator used in the for-condition.</Title>
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
            MyClass mc = new MyClass();
            dynamic dy = mc;
            MyClass[] array = new MyClass[]
            {
            new MyClass()
            {
            Field = 0
            }

            , new MyClass()
            {
            Field = 1
            }

            , new MyClass()
            {
            Field = 2
            }
            }

            ;
            int index = 0;
            for (int i = 0; (dynamic)array[i] < 2; i++)
            {
                index++;
            }

            if (index == 2)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regclass035.regclass035
{
    // <Title> Tests regular class operator used in the for-iterator.</Title>
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
            MyClass mc = new MyClass();
            dynamic dy = mc;
            int index = 10;
            for (int i = 0; i < 10; i = i + (dy & string.Empty).Length)
            {
                dynamic[] p1 = null;
                MyClass myclass = p1;
                if (myclass.Field != 4)
                    return 1;
                index++;
            }

            if (index == 20)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regclass035a.regclass035a
{
    // <Title> Tests regular class operator used in the for-iterator.</Title>
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
            MyClass mc = new MyClass();
            dynamic dy = mc;
            int index = 10;
            for (int i = 0; i < 10; i = i + (dy & "").Length)
            {
                dynamic[] p1 = null;
                MyClass myclass = p1;
                if (myclass.Field != 4)
                    return 1;
                index++;
            }

            if (index == 20)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regclass036.regclass036
{
    // <Title> Tests regular class operator used in while/do expression.</Title>
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
            MyClass mc = new MyClass();
            dynamic dy = mc;
            int?[] p2 = new int?[0];
            int index = 0;
            do
            {
                index++;
            }
            while (dy + p2);
            if (index == 1)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regclass038.regclass038
{
    // <Title> Tests regular class operator used in anonymous type.</Title>
    // <Description>
    // anonymous type inside a query expression that introduces dynamic variables.
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
            List<float> list = new List<float>()
            {
            float.Epsilon, 1.2f, 1.33f, float.NaN
            }

            ;
            dynamic dy = new MyClass();
            var result = list.Where(p => p == (float)dy).Select(p => new
            {
                A = (MyStruct[])dy,
                B = (int?)dy
            }

            ).ToList();
            if (result.Count != 1)
                return 1;
            MyStruct[] msa = result[0].A;
            int? i = result[0].B;
            if (msa.Length == 1 && msa[0].Number == 4 && i.Value == int.MinValue)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regclass039.regclass039
{
    // <Title> Tests regular class operator used in object initializer inside a collection initializer.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public class Test
    {
        private object[] _field1;
        private dynamic _field2;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic dy = new MyClass()
            {
                Field = 10
            }

            ;
            MyStruct?[] p1 = null;
            List<Test> list = new List<Test>()
            {
            new Test()
            {
            _field1 = (object[])dy, _field2 = (MyClass)p1
            }
            }

            ;
            if (list.Count != 1)
                return 1;
            Test t = list[0];
            if (t._field1.Length == 1 && ((MyClass)t._field1[0]).Field == 10 && t._field2.Field == 3)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regstrct001.regstrct001
{
    // <Title> Tests regular struct operator used in null coalescing operator.</Title>
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
            MyStruct mc = new MyStruct();
            dynamic dy = mc;
            MyClass[] p2 = new MyClass[1];
            MyEnum? result = (dy + p2) ?? MyEnum.Third;
            if (result == MyEnum.Third)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regstrct002.regstrct002
{
    // <Title> Tests regular struct operator used in query expression.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Linq;
    using System.Collections.Generic;

    public class Test
    {
        private decimal? _field1;
        private int? _field2;
        private byte _field3;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            var list = new List<Test>()
            {
            new Test()
            {
            _field1 = decimal.MinValue, _field2 = int.MaxValue, _field3 = byte.MaxValue
            }

            , new Test(), new Test()
            {
            _field1 = null, _field2 = null, _field3 = 0
            }

            , new Test()
            {
            _field1 = decimal.MinValue, _field2 = int.MaxValue, _field3 = 10
            }

            , }

            ;
            MyStruct mc = new MyStruct();
            dynamic dy = mc;
            char? c = 'a';
            double[] d = new double[0];
            var result = list.Where(p => p._field1 == ((decimal?)(mc != dy)).Value && p._field2 == ((int?)(c ^ dy)).Value).Where(p => p._field3 == (d & dy)).Select(p => p._field3).ToArray();
            if (result.Length == 1 && result[0] == byte.MaxValue)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regstrct003.regstrct003
{
    // <Title> Tests regular struct operator used in ternary operator expression.</Title>
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
            MyStruct mc = new MyStruct();
            dynamic dy = mc;
            int result = (dy + MyEnum.Second) ? 0 : 1;
            return result;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regstrct004.regstrct004
{
    // <Title> Tests regular struct operator used inside #if, #else block.</Title>
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
            MyStruct mc = new MyStruct();
            dynamic dy = mc;
            MyStruct?[] p1 = new MyStruct?[]
            {
            new MyStruct()
            {
            Number = 10
            }
            }

            ;
            double[] result;
#if c1
result = p1 < dy;
 #else
            result = p1 > dy;
#endif
            if (result.Length == 1 && result[0] == double.MaxValue)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regstrct005.regstrct005
{
    // <Title> Tests regular struct operator used in try/catch/finally.</Title>
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
            MyStruct mc = new MyStruct();
            dynamic dy = mc;
            MyEnum me = default(MyEnum);
            try
            {
                MyEnum?[] result = dy << int.MaxValue;
                if (result.Length != 2 || result[0] != null || result[1] != MyEnum.First)
                    return 1;
                throw new TimeoutException(dy);
            }
            catch (TimeoutException e)
            {
                if (e.Message != string.Empty)
                    return 1;
            }
            finally
            {
                me = dy;
            }

            if (me == MyEnum.First)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regstrct006.regstrct006
{
    // <Title> Tests regular struct operator used in static ctor.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private static MyClass s_myclass;
        private static MyEnum s_myenum;
        static Test()
        {
            MyStruct mc = new MyStruct();
            dynamic dy = mc;
            MyEnum[] p2 = new MyEnum[0];
            s_myclass = dy | p2;
            s_myenum = 'a' + dy;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            if (s_myclass.Field == int.MinValue && s_myenum == MyEnum.First)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regstrct007.regstrct007
{
    // <Title> Tests regular struct operator used in variable named dynamic.</Title>
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
            MyStruct mc = new MyStruct();
            dynamic dynamic = mc;
            MyEnum?[] p1 = new MyEnum?[10];
            MyStruct? ms = p1 / dynamic;
            if (ms.Value.Number == 5)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regstrct008.regstrct008
{
    // <Title> Tests regular struct operator used in this-argument of extension method.</Title>
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
            MyStruct mc = new MyStruct();
            dynamic dy = mc;
            return ((MyEnum[])(dy & new MyStruct[]
            {
            mc
            }

            )).Method();
        }
    }

    public static class Extension
    {
        public static int Method(this MyEnum[] mea)
        {
            if (mea.Length == 2 && mea[0] == MyEnum.First && mea[1] == MyEnum.Third)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regstrct009.regstrct009
{
    // <Title> Tests regular struct operator used in anonymous method.</Title>
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
            MyStruct mc = new MyStruct()
            {
                Number = 10
            }

            ;
            dynamic dy = mc;
            Func<MyStruct?, object[]> func = delegate (MyStruct? p2)
           {
               return dy | p2;
           }

            ;
            MyEnum?[] p1 = new MyEnum?[10];
            object[] result = func(p1 / dy);
            if (result.Length != 2)
                return 1;
            if (((MyStruct)result[0]).Number == 10 && ((MyStruct?)result[1]).Value.Number == 5)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regstrct010.regstrct010
{
    // <Title> Tests regular struct operator used in query expression.</Title>
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
            MyStruct mc = new MyStruct()
            {
                Number = 10
            }

            ;
            dynamic dy = mc;
            int[] array = new int[]
            {
            int.MaxValue, int.MinValue, int.MinValue, 0
            }

            ;
            var result = array.Where(p => p == (int)dy).ToArray();
            if (result.Length == 2 && result[0] == int.MinValue && result[1] == int.MinValue)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regstrct011.regstrct011
{
    // <Title> Tests regular struct operator used in lambda expression.</Title>
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
            MyStruct mc = new MyStruct();
            dynamic dy = mc;
            Func<dynamic, decimal?> func = (dynamic p) => (dy == p);
            decimal? result = func(dy);
            if (result == decimal.MaxValue)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regstrct012.regstrct012
{
    // <Title> Tests regular struct operator used in field initializer outside of ctor.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private static dynamic s_dy = new MyStruct();
        private ulong[] _result = s_dy;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Test t = new Test();
            if (t._result.Length == 1 && t._result[0] == ulong.MaxValue)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regstrct013.regstrct013
{
    // <Title> Tests regular struct operator used in volatile field initializer.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private static dynamic s_dy = new MyStruct()
        {
            Number = 5
        }

        ;
        private volatile byte _result = !s_dy;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Test t = new Test();
            if (t._result == byte.MinValue)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regstrct014.regstrct014
{
    // <Title> Tests regular struct operator used in extension method body.</Title>
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
            MyStruct mc = new MyStruct();
            float? f = mc.ExReturnFloat();
            if (f == null)
                return 0;
            return 1;
        }
    }

    public static class Extension
    {
        public static float? ExReturnFloat(this MyStruct mc)
        {
            dynamic dy = mc;
            float? result = dy;
            return result;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regstrct016.regstrct016
{
    // <Title> Tests regular struct operator used in iterator that calls to a lambda expression.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System;
    using System.Collections;

    public class Test
    {
        private static int s_num = 0;
        private static dynamic s_dy = new MyStruct();
        private static dynamic[] s_array = new dynamic[]
        {
        new MyStruct()
        {
        Number = 1
        }

        , new MyStruct()
        {
        Number = 2
        }

        , new MyStruct()
        {
        Number = 3
        }
        };

        
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            foreach (MyStruct m in t.Increment())
            {
                if (m.Number != 4)
                    return 1;
            }

            return 0;
        }

        public IEnumerable Increment()
        {
            while (s_num < 3)
            {
                Func<MyStruct, MyStruct> func = (MyStruct p1) => ++p1;
                yield return func(s_array[s_num++]);
            }
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regstrct018.regstrct018
{
    // <Title> Tests regular struct operator used in arguments to method invocation.</Title>
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
            MyStruct mc = new MyStruct();
            dynamic dy = mc;
            object[] p2 = new object[10];
            return Test.TestMethod(dy % p2);
        }

        public static int TestMethod(float? f)
        {
            if (f.Value == float.Epsilon)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regstrct019.regstrct019
{
    // <Title> Tests regular struct operator used in implicitly-typed variable initializer.</Title>
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
            dynamic dy1 = new MyStruct()
            {
                Number = 1
            }

            ;
            dynamic dy2 = new MyStruct()
            {
                Number = 2
            }

            ;
            var result = dy1 && dy2; //T.false(x) ? x : T.&(x, y)
            if (result.Number == 1)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regstrct020.regstrct020
{
    // <Title> Tests regular struct operator used in array initializer.</Title>
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
            dynamic dy = new MyStruct()
            {
                Number = 100
            }

            ;
            dynamic[] array = new dynamic[]
            {
            ~dy, +dy
            }

            ;
            if (array.Length != 2)
                return 1;
            MyStruct m1 = array[0];
            decimal? m2 = array[1];
            if (m1.Number == 100 && m2 == decimal.MaxValue)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regstrct021.regstrct021
{
    // <Title> Tests regular struct operator used in implicitly-typed array initializer.</Title>
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
            dynamic dy = new MyStruct()
            {
                Number = 100
            }

            ;
            var array = new dynamic[]
            {
            ~dy, +dy
            }

            ;
            if (array.Length != 2)
                return 1;
            MyStruct m1 = array[0];
            decimal? m2 = array[1];
            if (m1.Number == 100 && m2 == decimal.MaxValue)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regstrct022.regstrct022
{
    // <Title> Tests regular struct operator used in property get.</Title>
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
            if (t.MyProp.Number == 10)
                return 0;
            return 1;
        }

        public dynamic MyProp
        {
            get
            {
                MyStruct mc = new MyStruct()
                {
                    Number = 10
                }

                ;
                dynamic result = (dynamic)mc;
                return result;
            }
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regstrct024.regstrct024
{
    // <Title> Tests regular struct operator used in static method body.</Title>
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
            MyStruct mc = new MyStruct()
            {
                Number = 10
            }

            ;
            dynamic dy = mc;
            object[] result = -dy;
            if (result.Length == 1 && ((MyStruct)result[0]).Number == 10)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regstrct025.regstrct025
{
    // <Title> Tests regular struct operator used in indexer body.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private dynamic _result;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Test t = new Test();
            dynamic dy = t;
            t[10] = dy;
            if (t._result.Number != 3)
                return 1;
            dy = t[int.MinValue];
            if (dy.Number != 4)
                return 1;
            return 0;
        }

        public dynamic this[int i]
        {
            set
            {
                bool p = false;
                _result = (MyStruct)p;
            }

            get
            {
                bool? p = null;
                _result = (MyStruct)p;
                return _result;
            }
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regstrct026.regstrct026
{
    // <Title> Tests regular struct operator used in iterator.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections;

    public class Test
    {
        private static int s_a = 0;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int index = 0;
            foreach (dynamic s in Test.Increment())
            {
                if (s.Number != 5)
                    return 1;
                index++;
            }

            if (index == 3)
                return 0;
            return 1;
        }

        public static IEnumerable Increment()
        {
            byte p1 = byte.MaxValue;
            while (s_a < 3)
            {
                s_a++;
                dynamic result = (MyStruct)p1;
                yield return result;
            }
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regstrct027.regstrct027
{
    // <Title> Tests regular struct operator used in ctor.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private dynamic _result;
        public Test()
        {
            byte?[] p1 = null;
            _result = (MyStruct)p1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Test t = new Test();
            if (t._result.Number == 6)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regstrct028.regstrct028
{
    // <Title> Tests regular struct operator used in ctor.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private static dynamic s_result;
        static Test()
        {
            decimal? p1 = null;
            s_result = (MyStruct)p1;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            if (Test.s_result.Number == 7)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regstrct029.regstrct029
{
    // <Title> Tests regular struct operator used in dtor.</Title>
    // <Description>
    // On IA64 the GC.WaitForPendingFinalizers() does not actually work...
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System;
    using System.Runtime.CompilerServices;

    public class Test
    {
        private static dynamic s_field;
        public static object locker = new object();
        ~Test()
        {
            lock (locker)
            {
                decimal[] p1 = new decimal[20];
                s_field = (MyStruct)p1;
            }
        }

        private static int Verify()
        {
            lock (Test.locker)
            {
                if ((object)(Test.s_field) == null)
                {
                    System.Console.WriteLine("Failed: the finalizer hasn't executed!");
                    return 2;
                }

                if (Test.s_field.Number != 8)
                {
                    return 1;
                }
            }

            return 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void RequireLifetimesEnded()
        {
            Test t = new Test();
            Test.s_field = null;
            GC.KeepAlive(t);
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            RequireLifetimesEnded();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            // If move the code in Verify() to here, the finalizer will only be executed after exited Main
            return Verify();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regstrct030.regstrct030
{
    // <Title> Tests regular struct operator used in for body.</Title>
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
            dynamic[] array = new dynamic[10];
            for (int i = 0; i < array.Length; i++)
            {
                MyEnum? p1 = null;
                array[i] = (MyStruct)p1;
            }

            for (int i = 0; i < array.Length; i++)
            {
                if (array[i].Number != 9)
                    return 1;
            }

            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regstrct031.regstrct031
{
    // <Title> Tests regular struct operator used in while\do body.</Title>
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
            dynamic[] array = new dynamic[10];
            int i = 0;
            do
            {
                MyEnum?[] p1 = new MyEnum?[]
                {
                null, MyEnum.Third
                }

                ;
                array[i] = (MyStruct)p1;
            }
            while (++i < array.Length);
            i = 0;
            while (true)
            {
                if (array[i++].Number != 10)
                    return 1;
                if (i >= array.Length)
                    break;
            }

            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regstrct032.regstrct032
{
    // <Title> Tests regular struct operator used in foreach expression.</Title>
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
            dynamic dy = new MyStruct()
            {
                Number = 10
            }

            ;
            int index = 0;
            foreach (var m in (MyClass[])dy)
            {
                index++;
                if (m.Field != 3)
                    return 1;
            }

            if (index != 1)
                return 1;
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regstrctoperate.regstrctoperate
{
    public struct MyClass
    {
        public int Field;
        public static MyEnum?[] operator -(MyClass p1, dynamic[] p2)
        {
            return new MyEnum?[]
            {
            MyEnum.First, null
            }

            ;
        }

        public static MyStruct operator ^(MyClass p1, float p2)
        {
            return new MyStruct()
            {
                Number = 4
            }

            ;
        }

        public static MyClass[] operator /(MyClass p1, float? p2)
        {
            return new MyClass[]
            {
            new MyClass()
            {
            Field = 1
            }

            , new MyClass()
            {
            Field = 4
            }
            }

            ;
        }

        public static MyStruct?[] operator <=(MyClass p1, int p2)
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

        public static MyStruct?[] operator >=(MyClass p1, int p2)
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

        public static MyClass operator >>(MyClass p1, int p2)
        {
            return new MyClass()
            {
                Field = int.MinValue
            }

            ;
        }

        public static bool operator +(MyClass p1, int?[] p2)
        {
            return false;
        }

        public static bool? operator &(bool p1, MyClass p2)
        {
            return true;
        }

        public static decimal[] operator |(bool? p1, MyClass p2)
        {
            return new decimal[]
            {
            decimal.MaxValue
            }

            ;
        }

        public static MyEnum? operator <(byte?[] p1, MyClass p2)
        {
            return MyEnum.First;
        }

        public static MyEnum? operator >(byte?[] p1, MyClass p2)
        {
            return MyEnum.Third;
        }

        public static float operator ^(decimal? p1, MyClass p2)
        {
            return float.Epsilon;
        }

        public static byte operator +(decimal[] p1, MyClass p2)
        {
            return byte.MinValue;
        }

        public static decimal[] operator -(dynamic p1, MyClass p2)
        {
            return new decimal[]
            {
            decimal.MaxValue
            }

            ;
        }

        public static string operator *(dynamic[] p1, MyClass p2)
        {
            return string.Empty;
        }

        public static MyStruct[] operator |(MyClass p1, short[] p2)
        {
            return new MyStruct[]
            {
            new MyStruct()
            {
            Number = 3
            }
            }

            ;
        }

        public static dynamic[] operator &(MyClass p1, string p2)
        {
            return new dynamic[]
            {
            p1
            }

            ;
        }

        public static int?[] operator -(MyClass p1, ulong[] p2)
        {
            return new int?[]
            {
            null, int.MinValue
            }

            ;
        }

        public static bool operator <(MyClass p1, int p2)
        {
            return p1.Field < p2;
        }

        public static bool operator >(MyClass p1, int p2)
        {
            return p1.Field > p2;
        }

        public static MyEnum operator +(MyClass p1)
        {
            return MyEnum.First;
        }

        public static bool? operator !(MyClass p1)
        {
            return true;
        }

        public static string operator ~(MyClass p1)
        {
            return string.Empty;
        }

        public static MyClass operator ++(MyClass p1)
        {
            return new MyClass()
            {
                Field = 4
            }

            ;
        }

        public static bool operator true(MyClass p1)
        {
            return false;
        }

        public static bool operator false(MyClass p1)
        {
            return true;
        }

        public static MyClass operator |(MyClass p1, MyClass p2)
        {
            return p2;
        }

        public static dynamic[] operator -(MyClass p1)
        {
            return new dynamic[]
            {
            p1
            }

            ;
        }

        public static MyClass operator --(MyClass p1)
        {
            return new MyClass()
            {
                Field = 4
            }

            ;
        }

        public static explicit operator MyClass(char p1)
        {
            return new MyClass()
            {
                Field = 4
            }

            ;
        }

        public static explicit operator MyClass(char? p1)
        {
            return new MyClass()
            {
                Field = 4
            }

            ;
        }

        public static implicit operator MyClass(double[] p1)
        {
            return new MyClass()
            {
                Field = 4
            }

            ;
        }

        public static implicit operator MyClass(dynamic[] p1)
        {
            return new MyClass()
            {
                Field = 4
            }

            ;
        }

        public static implicit operator float (MyClass p1)
        {
            return float.Epsilon;
        }

        public static implicit operator int?[] (MyClass p1)
        {
            return null;
        }

        // public static implicit operator dynamic(MyClass p1) { return p1; }
        public static implicit operator MyStruct[] (MyClass p1)
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

        public static implicit operator int? (MyClass p1)
        {
            return int.MinValue;
        }

        public static explicit operator object[] (MyClass p1)
        {
            return new object[]
            {
            p1
            }

            ;
        }

        public static explicit operator MyEnum[] (MyClass p1)
        {
            return new MyEnum[]
            {
            MyEnum.First
            }

            ;
        }

        public static implicit operator short[] (MyClass p1)
        {
            return new short[]
            {
            short.MaxValue
            }

            ;
        }

        public static explicit operator MyClass(MyStruct?[] p1)
        {
            return new MyClass()
            {
                Field = 3
            }

            ;
        }
    }

    public struct MyStruct
    {
        public int Number;
        public static decimal? operator !=(MyStruct p1, dynamic p2)
        {
            return decimal.MinValue;
        }

        public static decimal? operator ==(MyStruct p1, dynamic p2)
        {
            return decimal.MaxValue;
        }

        // required
        public override bool Equals(object d)
        {
            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        // ---
        public static MyEnum?[] operator <<(MyStruct p1, int p2)
        {
            return new MyEnum?[]
            {
            null, MyEnum.First
            }

            ;
        }

        public static MyEnum? operator +(MyStruct p1, MyClass[] p2)
        {
            return null;
        }

        public static dynamic operator +(MyStruct p1, MyEnum p2)
        {
            return true;
        }

        public static MyClass operator |(MyStruct p1, MyEnum[] p2)
        {
            return new MyClass()
            {
                Field = int.MinValue
            }

            ;
        }

        public static MyEnum operator +(char p1, MyStruct p2)
        {
            return MyEnum.First;
        }

        public static int? operator ^(char? p1, MyStruct p2)
        {
            return int.MaxValue;
        }

        public static byte operator &(double[] p1, MyStruct p2)
        {
            return byte.MaxValue;
        }

        public static MyStruct? operator /(MyEnum?[] p1, MyStruct p2)
        {
            return new MyStruct
            {
                Number = 5
            }

            ;
        }

        public static double[] operator <(MyStruct?[] p1, MyStruct p2)
        {
            return new double[]
            {
            double.MinValue
            }

            ;
        }

        public static double[] operator >(MyStruct?[] p1, MyStruct p2)
        {
            return new double[]
            {
            double.MaxValue
            }

            ;
        }

        public static object[] operator |(MyStruct p1, MyStruct? p2)
        {
            return new object[]
            {
            p1, p2
            }

            ;
        }

        public static MyEnum[] operator &(MyStruct p1, MyStruct[] p2)
        {
            return new MyEnum[]
            {
            MyEnum.First, MyEnum.Third
            }

            ;
        }

        public static float? operator %(MyStruct p1, object[] p2)
        {
            return float.Epsilon;
        }

        public static object[] operator -(MyStruct p1)
        {
            return new object[]
            {
            p1
            }

            ;
        }

        public static MyStruct operator ++(MyStruct p1)
        {
            return new MyStruct()
            {
                Number = 4
            }

            ;
        }

        public static dynamic operator ~(MyStruct p1)
        {
            return p1;
        }

        public static bool operator true(MyStruct p1)
        {
            return false;
        }

        public static bool operator false(MyStruct p1)
        {
            return true;
        }

        public static decimal? operator +(MyStruct p1)
        {
            return decimal.MaxValue;
        }

        public static byte operator !(MyStruct p1)
        {
            return byte.MinValue;
        }

        public static implicit operator MyStruct(bool p1)
        {
            return new MyStruct()
            {
                Number = 3
            }

            ;
        }

        public static implicit operator MyStruct(bool? p1)
        {
            return new MyStruct()
            {
                Number = 4
            }

            ;
        }

        public static implicit operator MyStruct(byte p1)
        {
            return new MyStruct()
            {
                Number = 5
            }

            ;
        }

        public static implicit operator MyStruct(byte?[] p1)
        {
            return new MyStruct()
            {
                Number = 6
            }

            ;
        }

        public static explicit operator MyStruct(decimal? p1)
        {
            return new MyStruct()
            {
                Number = 7
            }

            ;
        }

        public static explicit operator MyStruct(decimal[] p1)
        {
            return new MyStruct()
            {
                Number = 8
            }

            ;
        }

        public static implicit operator MyStruct(MyEnum? p1)
        {
            return new MyStruct()
            {
                Number = 9
            }

            ;
        }

        public static implicit operator MyStruct(MyEnum?[] p1)
        {
            return new MyStruct()
            {
                Number = 10
            }

            ;
        }

        public static explicit operator MyClass[] (MyStruct p1)
        {
            return new MyClass[]
            {
            new MyClass()
            {
            Field = 3
            }
            }

            ;
        }

        public static implicit operator ulong[] (MyStruct p1)
        {
            return new ulong[]
            {
            ulong.MaxValue
            }

            ;
        }

        public static explicit operator int (MyStruct p1)
        {
            return int.MinValue;
        }

        public static implicit operator string (MyStruct p1)
        {
            return string.Empty;
        }

        // public static explicit operator dynamic(MyStruct p1) { return p1; }
        public static implicit operator float? (MyStruct p1)
        {
            return null;
        }

        public static implicit operator MyEnum(MyStruct p1)
        {
            return MyEnum.First;
        }

        public static implicit operator dynamic[] (MyStruct p1)
        {
            return new dynamic[]
            {
            p1
            }

            ;
        }
    }

    public enum MyEnum
    {
        First = 1,
        Second = 2,
        Third = 3
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regstrct023.regstrct023
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.regstrctoperate.regstrctoperate;
    // <Title> Tests regular struct operator used in property set.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private dynamic[] _result;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Test t = new Test();
            t.MyProp = null;
            if (t._result.Length == 1 && t._result[0].Number == 10)
                return 0;
            return 1;
        }

        public dynamic[] MyProp
        {
            set
            {
                MyStruct mc = new MyStruct()
                {
                    Number = 10
                }

                ;
                _result = mc;
            }
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.regclass.void001.void001
{
    // <Title> Tests void typed dynamic expression in operators </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(22,14\).*CS0184</Expects>

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            int flag = 1;
            dynamic dobj = new Test();
            var a = "";
            dynamic da = a;
            if ((Foo(a) is object) != false)
                return 1;
            flag = 1;
            try
            {
                if ((Foo(da) is object) == false)
                    flag = 0;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(RuntimeErrorId.BindToVoidMethodButExpectResult, ex.Message))
                {
                    flag = 0;
                }
            }

            result += flag;
            flag = 1;
            try
            {
                if ((dobj.Foo2() is object) == false)
                    flag = 0;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(RuntimeErrorId.BindToVoidMethodButExpectResult, ex.Message))
                {
                    flag = 0;
                }
            }

            result += flag;
            flag = 1;
            try
            {
                var temp = (Foo(da) as object);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(RuntimeErrorId.BindToVoidMethodButExpectResult, ex.Message))
                {
                    flag = 0;
                }
            }

            result += flag;
            flag = 1;
            try
            {
                var temp = (dobj.Foo2() as object);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(RuntimeErrorId.BindToVoidMethodButExpectResult, ex.Message))
                {
                    flag = 0;
                }
            }

            result += flag;
            flag = 1;
            try
            {
                var temp = (dobj.Foo2() + 2);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(RuntimeErrorId.BindToVoidMethodButExpectResult, ex.Message))
                {
                    flag = 0;
                }
            }

            result += flag;
            return result;
        }

        public static void Foo(string x)
        {
        }

        public void Foo2()
        {
        }
    }
    // </Code>
}
