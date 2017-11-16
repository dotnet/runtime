// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.regclass.regclassregindexer.regclassregindexer;
using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.regclass.regclassregindexer.regclassregindexer
{
    using System;

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

    public class MemberClass
    {
        [ThreadStatic]
        public static int t_status;

        public bool? this[string p1, float p2, short[] p3]
        {
            get
            {
                MemberClass.t_status = 1;
                return null;
            }

            set
            {
                MemberClass.t_status = 2;
            }
        }

        public byte this[dynamic[] p1, ulong[] p2, dynamic p3]
        {
            get
            {
                MemberClass.t_status = 1;
                return (byte)3;
            }

            set
            {
                MemberClass.t_status = 2;
            }
        }

        public byte?[] this[MyClass p1, byte p2, decimal? p3]
        {
            get
            {
                MemberClass.t_status = 1;
                return new byte?[]
                {
                null, (byte)2
                }

                ;
            }

            private set
            {
                MemberClass.t_status = 2;
            }
        }

        public char this[bool? p1, float p2, MyEnum p3]
        {
            protected get
            {
                MemberClass.t_status = 1;
                return 'a';
            }

            set
            {
                MemberClass.t_status = 2;
            }
        }

        public char? this[MyStruct[] p1, int? p2, byte?[] p3]
        {
            get
            {
                MemberClass.t_status = 1;
                return null;
            }

            set
            {
                MemberClass.t_status = 2;
            }
        }

        public decimal? this[int p1, char? p2, float? p3]
        {
            get
            {
                MemberClass.t_status = 1;
                return null;
            }

            private set
            {
                MemberClass.t_status = 2;
            }
        }

        public decimal[] this[MyEnum? p1, MyEnum?[] p2, int?[] p3]
        {
            protected internal get
            {
                MemberClass.t_status = 1;
                return new decimal[]
                {
                1m, 0m
                }

                ;
            }

            set
            {
                MemberClass.t_status = 2;
            }
        }

        public double[] this[int p1, object[] p2, float? p3]
        {
            get
            {
                MemberClass.t_status = 1;
                return new double[]
                {
                1.4, double.Epsilon, double.NaN
                }

                ;
            }

            internal set
            {
                MemberClass.t_status = 2;
            }
        }

        public dynamic this[MyClass p1, char? p2, MyEnum[] p3]
        {
            get
            {
                MemberClass.t_status = 1;
                return p1;
            }

            set
            {
                MemberClass.t_status = 2;
            }
        }

        public dynamic[] this[MyClass p1, MyStruct? p2, MyEnum[] p3]
        {
            get
            {
                MemberClass.t_status = 1;
                return new dynamic[]
                {
                p1, p2
                }

                ;
            }

            set
            {
                MemberClass.t_status = 2;
            }
        }

        public float this[bool? p1, float p2, byte?[] p3]
        {
            get
            {
                MemberClass.t_status = 1;
                return float.NegativeInfinity;
            }

            set
            {
                MemberClass.t_status = 2;
            }
        }

        public float? this[decimal[] p1, double[] p2, dynamic p3]
        {
            get
            {
                MemberClass.t_status = 1;
                return float.NegativeInfinity;
            }

            set
            {
                MemberClass.t_status = 2;
            }
        }

        public int this[char p1, int? p2, MyEnum[] p3]
        {
            get
            {
                MemberClass.t_status = 1;
                return 0;
            }

            set
            {
                MemberClass.t_status = 2;
            }
        }

        public int? this[bool? p1, object[] p2, int?[] p3]
        {
            get
            {
                MemberClass.t_status = 1;
                return 4;
            }

            set
            {
                MemberClass.t_status = 2;
            }
        }

        public int?[] this[string p1, double[] p2, float? p3]
        {
            get
            {
                MemberClass.t_status = 1;
                return new int?[]
                {
                1, null, int.MinValue
                }

                ;
            }

            set
            {
                MemberClass.t_status = 2;
            }
        }

        public MyClass this[int p1, int? p2, dynamic p3]
        {
            get
            {
                MemberClass.t_status = 1;
                return new MyClass()
                {
                    Field = 3
                }

                ;
            }

            set
            {
                MemberClass.t_status = 2;
            }
        }

        public MyClass[] this[MyStruct[] p1, MyClass[] p2, dynamic[] p3]
        {
            get
            {
                MemberClass.t_status = 1;
                return new MyClass[]
                {
                null, new MyClass()
                {
                Field = 3
                }
                }

                ;
            }

            set
            {
                MemberClass.t_status = 2;
            }
        }

        public MyEnum this[char p1, object[] p2, MyStruct?[] p3]
        {
            get
            {
                MemberClass.t_status = 1;
                return MyEnum.Second;
            }

            set
            {
                MemberClass.t_status = 2;
            }
        }

        public MyEnum? this[MyStruct[] p1, object[] p2, short[] p3]
        {
            get
            {
                MemberClass.t_status = 1;
                return null;
            }

            set
            {
                MemberClass.t_status = 2;
            }
        }

        public MyEnum?[] this[bool? p1, byte p2, MyEnum p3]
        {
            get
            {
                MemberClass.t_status = 1;
                return new MyEnum?[]
                {
                null, MyEnum.Second
                }

                ;
            }

            set
            {
                MemberClass.t_status = 2;
            }
        }

        public MyEnum[] this[string p1, float p2, decimal? p3]
        {
            get
            {
                MemberClass.t_status = 1;
                return new MyEnum[]
                {
                MyEnum.Second, MyEnum.First
                }

                ;
            }

            set
            {
                MemberClass.t_status = 2;
            }
        }

        public MyStruct this[MyStruct p1, MyClass[] p2, short[] p3]
        {
            get
            {
                MemberClass.t_status = 1;
                return new MyStruct()
                {
                    Number = 4
                }

                ;
            }

            set
            {
                MemberClass.t_status = 2;
            }
        }

        public MyStruct? this[decimal[] p1, double[] p2, dynamic[] p3]
        {
            get
            {
                MemberClass.t_status = 1;
                return null;
            }

            set
            {
                MemberClass.t_status = 2;
            }
        }

        public MyStruct?[] this[string p1, double[] p2, decimal? p3]
        {
            get
            {
                MemberClass.t_status = 1;
                return new MyStruct?[]
                {
                null, new MyStruct()
                {
                Number = 4
                }
                }

                ;
            }

            set
            {
                MemberClass.t_status = 2;
            }
        }

        public MyStruct[] this[string p1, MyClass[] p2, dynamic[] p3]
        {
            get
            {
                MemberClass.t_status = 1;
                return new MyStruct[]
                {
                }

                ;
            }

            set
            {
                MemberClass.t_status = 2;
            }
        }

        public object[] this[bool? p1, int? p2, dynamic p3]
        {
            get
            {
                MemberClass.t_status = 1;
                return new object[]
                {
                p1, p2, p3
                }

                ;
            }

            set
            {
                MemberClass.t_status = 2;
            }
        }

        public short[] this[MyStruct p1, MyClass[] p2, decimal? p3]
        {
            get
            {
                MemberClass.t_status = 1;
                return new short[]
                {
                1
                }

                ;
            }

            set
            {
                MemberClass.t_status = 2;
            }
        }

        public string this[MyEnum? p1, int? p2, MyEnum[] p3]
        {
            get
            {
                MemberClass.t_status = 1;
                return string.Empty;
            }

            set
            {
                MemberClass.t_status = 2;
            }
        }

        public ulong[] this[MyEnum? p1, object[] p2, dynamic p3]
        {
            get
            {
                MemberClass.t_status = 1;
                return new ulong[]
                {
                ulong.MaxValue
                }

                ;
            }

            set
            {
                MemberClass.t_status = 2;
            }
        }

        public bool? this[int? p1]
        {
            get
            {
                MemberClass.t_status = 1;
                MemberClass.t_status = 1;
                return null;
            }

            set
            {
                MemberClass.t_status = 2;
            }
        }

        public byte this[MyStruct? p1]
        {
            get
            {
                MemberClass.t_status = 1;
                return (byte)3;
            }

            set
            {
                MemberClass.t_status = 2;
            }
        }

        public byte?[] this[MyClass[] p1]
        {
            get
            {
                MemberClass.t_status = 1;
                return new byte?[]
                {
                null, (byte)2
                }

                ;
            }

            set
            {
                MemberClass.t_status = 2;
            }
        }

        public char this[MyEnum? p1]
        {
            get
            {
                MemberClass.t_status = 1;
                return 'a';
            }

            set
            {
                MemberClass.t_status = 2;
            }
        }

        public char? this[MyStruct?[] p1]
        {
            get
            {
                MemberClass.t_status = 1;
                return null;
            }

            set
            {
                MemberClass.t_status = 2;
            }
        }

        public decimal? this[char p1]
        {
            get
            {
                MemberClass.t_status = 1;
                return null;
            }

            set
            {
                MemberClass.t_status = 2;
            }
        }

        public decimal[] this[bool? p1]
        {
            get
            {
                MemberClass.t_status = 1;
                return new decimal[]
                {
                1m, 0m
                }

                ;
            }

            set
            {
                MemberClass.t_status = 2;
            }
        }

        public double[] this[float p1]
        {
            get
            {
                MemberClass.t_status = 1;
                return new double[]
                {
                1.4, double.Epsilon, double.NaN
                }

                ;
            }

            set
            {
                MemberClass.t_status = 2;
            }
        }

        public dynamic this[int?[] p1]
        {
            get
            {
                MemberClass.t_status = 1;
                return p1;
            }

            set
            {
                MemberClass.t_status = 2;
            }
        }

        public dynamic[] this[MyEnum?[] p1]
        {
            get
            {
                MemberClass.t_status = 1;
                return new dynamic[]
                {
                p1
                }

                ;
            }

            set
            {
                MemberClass.t_status = 2;
            }
        }

        public float this[decimal? p1]
        {
            get
            {
                MemberClass.t_status = 1;
                return float.NegativeInfinity;
            }

            set
            {
                MemberClass.t_status = 2;
            }
        }

        public float? this[short[] p1]
        {
            get
            {
                MemberClass.t_status = 1;
                return float.PositiveInfinity;
            }

            set
            {
                MemberClass.t_status = 2;
            }
        }

        public int this[MyStruct p1]
        {
            get
            {
                MemberClass.t_status = 1;
                return 0;
            }

            set
            {
                MemberClass.t_status = 2;
            }
        }

        public int? this[double[] p1]
        {
            get
            {
                MemberClass.t_status = 1;
                return 4;
            }

            set
            {
                MemberClass.t_status = 2;
            }
        }

        public int?[] this[dynamic[] p1]
        {
            get
            {
                MemberClass.t_status = 1;
                return new int?[]
                {
                1, null, int.MinValue
                }

                ;
            }

            set
            {
                MemberClass.t_status = 2;
            }
        }

        public MyClass this[float? p1]
        {
            get
            {
                MemberClass.t_status = 1;
                return new MyClass()
                {
                    Field = 3
                }

                ;
            }

            set
            {
                MemberClass.t_status = 2;
            }
        }

        public MyClass[] this[MyEnum p1]
        {
            get
            {
                MemberClass.t_status = 1;
                return new MyClass[]
                {
                null, new MyClass()
                {
                Field = 3
                }
                }

                ;
            }

            set
            {
                MemberClass.t_status = 2;
            }
        }

        public MyEnum? this[MyEnum[] p1]
        {
            get
            {
                MemberClass.t_status = 1;
                return null;
            }

            set
            {
                MemberClass.t_status = 2;
            }
        }

        public MyEnum?[] this[MyClass p1]
        {
            get
            {
                MemberClass.t_status = 1;
                return new MyEnum?[]
                {
                null, MyEnum.Second
                }

                ;
            }

            set
            {
                MemberClass.t_status = 2;
            }
        }

        //  public MyEnum this[object[] p1] { get { MemberClass.Status = 1; return MyEnum.Second; } set { MemberClass.Status = 2; } }
        public MyEnum[] this[dynamic p1]
        {
            get
            {
                MemberClass.t_status = 1;
                return new MyEnum[]
                {
                MyEnum.Second, MyEnum.First
                }

                ;
            }

            set
            {
                MemberClass.t_status = 2;
            }
        }

        public MyStruct this[decimal[] p1]
        {
            get
            {
                MemberClass.t_status = 1;
                return new MyStruct()
                {
                    Number = 4
                }

                ;
            }

            set
            {
                MemberClass.t_status = 2;
            }
        }

        public MyStruct? this[byte p1]
        {
            get
            {
                MemberClass.t_status = 1;
                return null;
            }

            internal set
            {
                MemberClass.t_status = 2;
            }
        }

        public MyStruct?[] this[char? p1]
        {
            get
            {
                MemberClass.t_status = 1;
                return new MyStruct?[]
                {
                null, new MyStruct()
                {
                Number = 4
                }
                }

                ;
            }

            set
            {
                MemberClass.t_status = 2;
            }
        }

        public MyStruct[] this[byte?[] p1]
        {
            get
            {
                MemberClass.t_status = 1;
                return new MyStruct[]
                {
                }

                ;
            }

            set
            {
                MemberClass.t_status = 2;
            }
        }

        public object[] this[string p1]
        {
            get
            {
                MemberClass.t_status = 1;
                return new object[]
                {
                p1
                }

                ;
            }

            set
            {
                MemberClass.t_status = 2;
            }
        }

        public short[] this[MyStruct[] p1]
        {
            get
            {
                MemberClass.t_status = 1;
                return new short[]
                {
                1
                }

                ;
            }

            set
            {
                MemberClass.t_status = 2;
            }
        }

        public string this[int p1]
        {
            get
            {
                MemberClass.t_status = 1;
                return string.Empty;
            }

            private set
            {
                MemberClass.t_status = 2;
            }
        }

        public ulong[] this[ulong[] p1]
        {
            get
            {
                MemberClass.t_status = 1;
                return new ulong[]
                {
                ulong.MaxValue
                }

                ;
            }

            set
            {
                MemberClass.t_status = 2;
            }
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.regclass.regclass001.regclass001
{
    // <Title> Tests regular class indexer used in regular method body.</Title>
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
            return t.TestGetMethod(new MemberClass()) + t.TestSetMethod(new MemberClass()) == 0 ? 0 : 1;
        }

        public int TestGetMethod(MemberClass mc)
        {
            dynamic dy = mc;
            if (dy[string.Empty, 1.2f, new short[0]] != null && MemberClass.t_status != 1)
                return 1;
            else
                return 0;
        }

        public int TestSetMethod(MemberClass mc)
        {
            dynamic dy = mc;
            dy[string.Empty, 1.2f, new short[0]] = false;
            if (MemberClass.t_status != 2)
                return 1;
            else
                return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.regclass.regclass002.regclass002
{
    // <Title> Tests regular class indexer used in static method body.</Title>
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
            return Test.TestGetMethod(new MemberClass()) + Test.TestSetMethod(new MemberClass()) == 0 ? 0 : 1;
        }

        public static int TestGetMethod(MemberClass mc)
        {
            dynamic dy = mc;
            dynamic[] p1 = new object[]
            {
            1, null, string.Empty
            }

            ;
            ulong[] p2 = new ulong[1];
            dynamic p3 = dy;
            if (dy[p1, p2, p3] != 3 && MemberClass.t_status != 1)
                return 1;
            else
                return 0;
        }

        public static int TestSetMethod(MemberClass mc)
        {
            dynamic dy = mc;
            dynamic[] p1 = new object[]
            {
            1, null, string.Empty
            }

            ;
            ulong[] p2 = new ulong[1];
            dynamic p3 = dy;
            dy[p1, p2, p3] = (byte)4;
            if (MemberClass.t_status != 2)
                return 1;
            else
                return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.regclass.regclass003.regclass003
{
    // <Title> Tests regular class indexer used in generic method body.</Title>
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
            return t.TestGetMethod<int>(new MemberClass()) + t.TestSetMethod<int, int>(new MemberClass()) == 0 ? 0 : 1;
        }

        public int TestGetMethod<T>(MemberClass mc)
        {
            dynamic dy = mc;
            MyClass p1 = new MyClass();
            byte p2 = 11;
            decimal? p3 = null;
            byte?[] result = dy[p1, p2, p3];
            if (result.Length == 2 && result[0] == null && result[1] == 2 && MemberClass.t_status == 1)
                return 0;
            return 1;
        }

        public int TestSetMethod<U, V>(MemberClass mc)
        {
            dynamic dy = mc;
            MyClass p1 = new MyClass();
            byte p2 = 11;
            decimal? p3 = null;
            try
            {
                dy[p1, p2, p3] = null; //private
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.InaccessibleSetter, e.Message, "MemberClass.this[MyClass, byte, decimal?]"))
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.regclass.regclass004.regclass004
{
    // <Title> Tests regular class indexer used in extension method body.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test : MemberClass
    {
        public char Field;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            try
            {
                var t = MyEnum.Second.ExReturnTest().Field;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e) //The getter of MemberClass.this[bool?, float, MyEnum] is protected.
            {
                if (ErrorVerifier.Verify(ErrorMessageId.InaccessibleGetter, e.Message, "MemberClass.this[bool?, float, MyEnum]"))
                    return 0;
            }

            return 1;
        }
    }

    public static class Extension
    {
        public static Test ExReturnTest(this MyEnum me)
        {
            MemberClass mc = new MemberClass();
            dynamic dy = mc;
            bool? p1 = false;
            float p2 = 1.234f;
            return new Test()
            {
                Field = dy[p1, p2, me]
            }

            ;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.regclass.regclass007.regclass007
{
    // <Title> Tests regular class indexer used in variable initializer.</Title>
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
            MemberClass mc = new MemberClass();
            dynamic dy = mc;
            double[] result = dy[1, new object[1], 1.2f];
            if (result.Length == 3 && result[0] == 1.4 && result[1] == double.Epsilon && double.IsNaN(result[2]) && MemberClass.t_status == 1)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.regclass.regclass008.regclass008
{
    // <Title> Tests regular class indexer used in arguments to method invocation.</Title>
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
            MemberClass mc = new MemberClass();
            dynamic dy = mc;
            int result1 = t.TestMethod(dy['a', 10, new MyEnum[10]]);
            if (MemberClass.t_status != 1)
                return 1;
            MemberClass.t_status = 0;
            char? pp = null;
            int result2 = Test.TestMethod(dy[new MyClass()
            {
                Field = 10
            }

            , pp, new MyEnum[3]]);
            if (result1 == 0 && result2 == 0 && MemberClass.t_status == 1)
                return 0;
            else
                return 1;
        }

        public int TestMethod(int i)
        {
            if (i == 0)
            {
                return 0;
            }
            else
            {
                return 1;
            }
        }

        public static int TestMethod(MyClass m)
        {
            if (m.Field == 10)
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.regclass.regclass009.regclass009
{
    // <Title> Tests regular class indexer used in implicitly-typed variable initializer.</Title>
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
            MemberClass mc = new MemberClass();
            dynamic dy = mc;
            bool? p1 = false;
            float p2 = float.PositiveInfinity;
            byte?[] p3 = new byte?[]
            {
            null, 2, byte.MaxValue
            }

            ;
            var result = dy[p1, p2, p3];
            if (result == float.NegativeInfinity && MemberClass.t_status == 1)
                return 0;
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.regclass.regclass010.regclass010
{
    // <Title> Tests regular class indexer used in array initializer.</Title>
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
            MemberClass mc = new MemberClass();
            dynamic dy = mc;
            decimal[] p1 = new decimal[10];
            double[] p2 = new double[0];
            dynamic p3 = dy;
            float?[] result = new float?[]
            {
            dy[p1, p2, p3], dy[new short[2]]
            }

            ;
            if (result.Length == 2 && result[0] == float.NegativeInfinity && result[1] == float.PositiveInfinity && MemberClass.t_status == 1)
                return 0;
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.regclass.regclass011.regclass011
{
    // <Title> Tests regular class indexer used in implicitly-typed array initializer.</Title>
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
            MemberClass mc = new MemberClass();
            dynamic dy = mc;
            dynamic p3 = 1;
            float? p = 1.2f;
            var result = new MyClass[]
            {
            dy[1, 1, p3], dy[p]
            }

            ;
            if (result.Length == 2 && result[0].Field == 3 && result[1].Field == 3 && MemberClass.t_status == 1)
                return 0;
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.regclass.regclass012.regclass012
{
    // <Title> Tests regular class indexer used in member initializer of object initializer.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public class InnerTest
        {
            public int? Field;
            public MyEnum MyEnum
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
            Test t = new Test();
            MemberClass mc = new MemberClass();
            dynamic dy = mc;
            bool? p1 = false;
            object[] p2 = new object[]
            {
            null, 1, new Test()}

            ;
            int?[] p3 = new int?[2];
            var result = new InnerTest()
            {
                Field = dy[p1, p2, p3],
                MyEnum = dy['a', p2, new MyStruct?[4]]
            }

            ;
            if (result.Field == 4 && result.MyEnum == MyEnum.Second && MemberClass.t_status == 1)
                return 0;
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.regclass.regclass013.regclass013
{
    // <Title> Tests regular class indexer used in member initializer of anonymous type.</Title>
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
            MemberClass mc = new MemberClass();
            dynamic dy = mc;
            MyEnum? p1 = MyEnum.Second;
            ulong[] p2 = new ulong[]
            {
            1, 2, 3
            }

            ;
            var result = new
            {
                Field1 = dy[p1],
                Field2 = dy[p2]
            }

            ;
            if (result.Field1 == 'a' && result.Field2.Length == 1 && result.Field2[0] == ulong.MaxValue && MemberClass.t_status == 1)
                return 0;
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.regclass.regclass014.regclass014
{
    // <Title> Tests regular class indexer used in static variable.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class StaticTestClass
    {
        public static MyClass p1 = new MyClass();
        public static MyStruct? p2 = new MyStruct()
        {
            Number = 10
        }

        ;
        public static MyEnum[] p3 = new MyEnum[]
        {
        MyEnum.Second, MyEnum.Third
        }

        ;
        public static dynamic dy = new MemberClass();
    }

    public class Test
    {
        private static dynamic[] s_result = StaticTestClass.dy[StaticTestClass.p1, StaticTestClass.p2, StaticTestClass.p3];

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            if (s_result.Length == 2 && s_result[0] == StaticTestClass.p1 && ((MyStruct?)s_result[1]).Value.Number == StaticTestClass.p2.Value.Number && MemberClass.t_status == 1)
                return 0;
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.regclass.regclass015.regclass015
{
    // <Title> Tests regular class indexer used in property get.</Title>
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
            int?[] result = t.MyProperty;
            if (result.Length == 3 && result[0] == 1 && result[1] == null && result[2] == int.MinValue && MemberClass.t_status == 1)
                return 0;
            return 1;
        }

        public int?[] MyProperty
        {
            get
            {
                MemberClass mc = new MemberClass();
                dynamic dy = mc;
                string p1 = string.Empty;
                double[] p2 = new double[]
                {
                1.2, 2.4
                }

                ;
                float? p3 = 1.2f;
                return dy[p1, p2, p3];
            }
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.regclass.regclass016.regclass016
{
    // <Title> Tests regular class indexer used in property set.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private static MemberClass s_mc = new MemberClass();

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Test.MyProperty = "Test";
            if (MemberClass.t_status == 2)
                return 0;
            return 1;
        }

        public static string MyProperty
        {
            set
            {
                dynamic dy = s_mc;
                MyEnum? p1 = MyEnum.Third;
                int? p2 = int.MinValue;
                MyEnum[] p3 = new MyEnum[5];
                dy[p1, p2, p3] = value;
            }
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.regclass.regclass018.regclass018
{
    // <Title> Tests regular class indexer used in iterator.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections;

    public class Test
    {
        private static int s_a = 0;
        private static MemberClass s_mc = new MemberClass();

        
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int index = 0;
            dynamic dy = new Test();
            foreach (byte i in dy.Increment())
            {
                System.Console.WriteLine(i);
                index++;
            }

            if (index == 3 && MemberClass.t_status == 1)
                return 0;
            return 1;
        }

        public IEnumerable Increment()
        {
            dynamic dy = s_mc;
            MyStruct? p1 = new MyStruct()
            {
                Number = 10
            }

            ;
            while (s_a < 3)
            {
                s_a++;
                yield return dy[p1];
            }
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.regclass.regclass019.regclass019
{
    // <Title> Tests regular class indexer used in collection initializer list.</Title>
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
            MemberClass mc = new MemberClass();
            dynamic dy = mc;
            decimal[] p = null;
            List<MyStruct> list = new List<MyStruct>()
            {
                (MyStruct)dy[new decimal[0]],
                (MyStruct)dy[new decimal[1]],
                (MyStruct)dy[p]
            }

            ;
            if (list.Count == 3 && list[0].Number == 4 && list[1].Number == 4 && list[2].Number == 4 && MemberClass.t_status == 1)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.regclass.regclass020.regclass020
{
    // <Title> Tests regular class indexer used in ctor.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private MyClass[] _filed1;
        private MyEnum? _filed2;
        public Test()
        {
            MemberClass mc = new MemberClass();
            dynamic dy = mc;
            MyStruct[] p1 = new MyStruct[10];
            MyClass[] p2 = new MyClass[10];
            dynamic[] p3 = new dynamic[]
            {
            null
            }

            ;
            object[] p4 = new object[]
            {
            p1, p2
            }

            ;
            short[] p5 = new short[]
            {
            1, 2, 3
            }

            ;
            dy[p1, p2, p3] = new MyClass[]
            {
            new MyClass()}

            ;
            dy[p1, p4, p5] = MyEnum.Second;
            _filed1 = dy[p1, p2, p3];
            _filed2 = dy[p1, p4, p5];
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Test t = new Test();
            if (t._filed1.Length == 2 && t._filed1[0] == null && t._filed1[1].Field == 3 && t._filed2 == null && MemberClass.t_status == 1)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.regclass.regclass021.regclass021
{
    // <Title> Tests regular class indexer used in checked and unchecked.</Title>
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
            MemberClass mc = new MemberClass();
            dynamic dy = mc;
            int m1 = unchecked(dy[new double[3]] + int.MaxValue); // max + 4
            int m2 = checked(dy[new MyStruct()] + int.MaxValue);
            if (m1 == -2147483645 && m2 == int.MaxValue && MemberClass.t_status == 1) // 0xFFFFFFFF80000003
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.regclass.regclass022.regclass022
{
    // <Title> Tests regular class indexer used in null coalescing operator.</Title>
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
            MemberClass mc = new MemberClass();
            dynamic dy = mc;
            decimal[] p1 = new decimal[3];
            double[] p2 = new double[4];
            dynamic[] p3 = new dynamic[]
            {
            p1, p2, new Test(), 1
            }

            ;
            MyStruct? ms = dy[p1, p2, p3] ?? new MyStruct()
            {
                Number = 10
            }

            ;
            if (ms.Value.Number == 10 && MemberClass.t_status == 1)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.regclass.regclass023.regclass023
{
    // <Title> Tests regular class indexer used in query expression.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Linq;
    using System.Collections.Generic;

    public class Test
    {
        private MyStruct? _field1;
        private string _field2;
        private int _field3;

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
            _field1 = null, _field2 = string.Empty, _field3 = 1
            }

            , new Test(), new Test()
            {
            _field1 = null, _field2 = null, _field3 = 0
            }

            , new Test()
            {
            _field1 = new MyStruct(), _field2 = string.Empty, _field3 = 10
            }

            , }

            ;
            MemberClass mc = new MemberClass();
            dynamic dy = mc;
            byte p1 = 10;
            int p2 = p1;
            var result = list.Where(p => !p._field1.HasValue && !((MyStruct?)dy[p1]).HasValue && p._field2 == dy[p2]).Select(p => p._field3).Average();
            if (result == 1 && MemberClass.t_status == 1)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.regclass.regclass024.regclass024
{
    // <Title> Tests regular class indexer used in short-circuit boolean expression.</Title>
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
            MemberClass mc = new MemberClass();
            dynamic dy = mc;
            bool? p1 = false;
            byte p2 = 10;
            MyEnum p3 = MyEnum.Third;
            bool b = true;
            int index = 0;
            while (b)
            {
                dy[p1, p2, p3] = new MyEnum?[]
                {
                null
                }

                ;
                if (index++ >= 5)
                    b = false;
            }

            if (MemberClass.t_status != 2)
                return 1;
            b = true;
            index = 0;
            while (b)
            {
                MyEnum?[] result = dy[p1, p2, p3];
                if (result.Length != 2)
                {
                    return 1;
                }

                if (index++ >= 5)
                    b = false;
            }

            if (MemberClass.t_status != 1)
                return 1;
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.regclass.regclass025.regclass025
{
    // <Title> Tests regular class indexer used in ternary operator expression.</Title>
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
            MemberClass mc = new MemberClass();
            dynamic dy = mc;
            int? p1 = 10;
            dy[p1] = true;
            if (MemberClass.t_status != 2)
                return 1;
            var result0 = dy[p1];
            var result1 = result0 == null ? false : result0;
            if (result1 == false && MemberClass.t_status == 1)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.regclass.regclass027.regclass027
{
    // <Title> Tests regular class indexer used in try/catch/finally.</Title>
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
            MemberClass mc = new MemberClass();
            dynamic dy = mc;
            MyStruct p1 = new MyStruct()
            {
                Number = 10
            }

            ;
            MyClass[] p2 = new MyClass[]
            {
            new MyClass()}

            ;
            short[] p3 = new short[]
            {
            1, 2, 3
            }

            ;
            MyStruct ms = new MyStruct();
            try
            {
                MyStruct temp = dy[p1, p2, p3, p1, p2, p3];
                return 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (!ErrorVerifier.Verify(ErrorMessageId.BadArgCount, e.Message, "this", "6"))
                    return 1;
                ms = dy[p1, p2, p3];
                if (ms.Number != 4 || MemberClass.t_status != 1)
                    return 1;
            }
            finally
            {
                dy[p1, p2, p3] = ms;
            }

            if (MemberClass.t_status != 2)
                return 1;
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.regclass.regclass028.regclass028
{
    // <Title> Tests regular class indexer used in static ctor.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private static MyStruct?[] s_ms;
        static Test()
        {
            MemberClass mc = new MemberClass();
            dynamic dy = mc;
            string p1 = "Test";
            double[] p2 = new double[0];
            decimal? p3 = 12M;
            dy[p1, p2, p3] = s_ms;
            s_ms = dy[p1, p2, p3];
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            if (Test.s_ms.Length == 2 && Test.s_ms[0] == null && Test.s_ms[1].Value.Number == 4 && MemberClass.t_status == 1)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.regclass.regclass029.regclass029
{
    // <Title> Tests regular class indexer used in dtor.</Title>
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
        private static MyStruct[] s_field = null;
        public static object locker = new object();
        private static int memberClassStatus = 0;

        ~Test()
        {
            lock (locker)
            {
                MemberClass mc = new MemberClass();
                dynamic dy = new MemberClass();
                string p1 = string.Empty;
                MyClass[] p2 = new MyClass[3];
                dynamic[] p3 = new dynamic[]
                {
                p2
                }

                ;
                dy[p1, p2, p3] = null;
                s_field = dy[p1, p2, p3];

                memberClassStatus = MemberClass.t_status;
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

                if (Test.s_field.Length != 0 || Test.memberClassStatus != 1)
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
            // should finalize only after s_field is set to null.
            GC.KeepAlive(t);
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            RequireLifetimesEnded();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Assert.Equal(0, Verify());
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.regclass.regclass030.regclass030
{
    // <Title> Tests regular class indexer used in variable named dynamic.</Title>
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
            MemberClass mc = new MemberClass();
            dynamic dynamic = mc;
            object[] array = dynamic[true, 10, dynamic];

            if (array.Length == 3 && (bool?)array[0] == true && (int?)array[1] == 10 && array[2] == dynamic && MemberClass.t_status == 1)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.regclass.regclass032.regclass032
{
    // <Title> Tests regular class indexer used in this-argument of extension method.</Title>
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
            MemberClass mc = new MemberClass();
            dynamic dy = mc;
            MyStruct?[] p1 = new MyStruct?[]
            {
            new MyStruct()}

            ;
            char result = ((char?)dy[p1]).Method();
            if (result == '&' && MemberClass.t_status == 1)
                return 0;
            return 1;
        }
    }

    public static class Extension
    {
        public static char Method(this char? c)
        {
            return c ?? '&';
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.regclass.regclass034.regclass034
{
    // <Title> Tests regular class indexer used in anonymous method.</Title>
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
            MemberClass mc = new MemberClass();
            dynamic dy = mc;
            bool? b = null;
            Func<bool?, decimal[]> func = delegate (bool? p1)
          {
              dy[p1] = null;
              return dy[p1];
          }

            ;
            decimal[] result = dy[b];
            if (result.Length == 2 && result[0] == 1M && result[1] == 0M && MemberClass.t_status == 1)
                return 0;
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.regclass.regclass035.regclass035
{
    // <Title> Tests regular class indexer used in static method.</Title>
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
            MemberClass mc = new MemberClass();
            dynamic dy = mc;
            MyEnum? p1 = null;
            object[] p2 = null;
            dynamic p3 = null;
            ulong[] result = dy[p1, p2, p3];
            if (result.Length != 1 || result[0] != ulong.MaxValue || MemberClass.t_status != 1)
                return 1;
            dy[p1, p2, p3] = result;
            if (MemberClass.t_status != 2)
                return 1;
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.regclass.regclass037.regclass037
{
    // <Title> Tests regular class indexer used in lambda expression.</Title>
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
            MemberClass mc = new MemberClass();
            dynamic dy = mc;
            MyEnum?[] p1 = new MyEnum?[]
            {
            MyEnum.First, MyEnum.Third, null
            }

            ;
            Func<int, dynamic[]> func = (int arg1) => dy[p1];
            dynamic[] result = func(1);
            if (result.Length != 1 || MemberClass.t_status != 1)
                return 1;
            MyEnum?[] meArray = result[0];
            if (meArray[0] == MyEnum.First && meArray[1] == MyEnum.Third && meArray[2] == null)
                return 0;
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.regclass.regclass038.regclass038
{
    // <Title> Tests regular class indexer used in lambda expression.</Title>
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
            MemberClass mc = new MemberClass();
            dynamic dy = mc;
            char p1 = 'a';
            Func<char, decimal?> func = (char arg1) => dy[arg1];
            decimal? result = func(p1);
            if (result == null && MemberClass.t_status == 1)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.regclass.regclass039.regclass039
{
    // <Title> Tests regular class indexer used in field initializer outside of ctor.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private static dynamic s_dy = new MemberClass();
        private static double[] s_result = s_dy[1.2f];

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            if (s_result.Length == 3 && s_result[0] == 1.4 && s_result[1] == double.Epsilon && double.IsNaN(s_result[2]) && MemberClass.t_status == 1)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.regclass.regclass040.regclass040
{
    // <Title> Tests regular class indexer used in volatile field initializer.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private static dynamic s_dy = new MemberClass();
        private volatile dynamic _field = s_dy[new int?[10]];

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Test t = new Test();
            if (t._field.Length == 10 && MemberClass.t_status == 1)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.regclass.regclass041.regclass041
{
    // <Title> Tests regular class indexer used in foreach body.</Title>
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
            MemberClass mc = new MemberClass();
            dynamic dy = mc;
            dynamic[] p1 = new dynamic[]
            {
            dy, mc, dy
            }

            ;
            int index = 0;
            foreach (int? i in dy[p1])
            {
                index++;
            }

            if (index == 3 && MemberClass.t_status == 1)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.regclass.regclass042.regclass042
{
    // <Title> Tests regular class indexer used in for loop initializer.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test : MemberClass
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Test mc = new Test();
            dynamic dy = mc;
            MyStruct[] p1 = null;
            int index = 0;
            for (short i = dy[p1][0]; i < 3; i++) //getter is protected.
            {
                index++;
            }

            if (index == 2 && MemberClass.t_status == 1)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.regclass.regclass043.regclass043
{
    // <Title> Tests regular class indexer used in lock expression.</Title>
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
            MemberClass mc = new MemberClass();
            dynamic dy = mc;
            string p1 = string.Empty;
            bool isChecked = false;
            lock (dy[p1][0])
            {
                isChecked = true;
            }

            if (isChecked && MemberClass.t_status == 1)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.regclass.regclass044.regclass044
{
    // <Title> Tests regular class indexer used in lock expression body.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(21,14\).*CS0219</Expects>

    public class Test
    {
        private static object s_locker = new object();

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            MemberClass mc = new MemberClass();
            dynamic dy = mc;
            byte?[] p1 = new byte?[]
            {
            null, null
            }

            ;
            MyStruct[] ms = null;
            bool isGetException = false;
            lock (s_locker)
            {
                ms = dy[p1];
                // setter is internal, should support after dynamic privates check in.
                dy[p1] = new MyStruct[7];
            }

            if (ms.Length == 0 && MemberClass.t_status == 2)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.regclass.regclass045.regclass045
{
    // <Title> Tests regular class indexer used in generic method body.</Title>
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
            MemberClass mc = new MemberClass();
            dynamic dy = mc;
            char? p1 = null;
            MyStruct?[] result = dy[p1];
            if (result.Length != 2 || result[0] != null || result[1].Value.Number != 4 || MemberClass.t_status != 1)
                return 1;
            dy[p1] = result;
            if (MemberClass.t_status != 2)
                return 1;
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.regclass.regclass046.regclass046
{
    // <Title> Tests regular class indexer used in foreach loop body.</Title>
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
            MemberClass mc = new MemberClass();
            dynamic dy = mc;
            int index = 0;
            MyClass p1 = new MyClass()
            {
                Field = 1
            }

            ;
            MyClass[] result = null;
            foreach (MyEnum? m in dy[p1])
            {
                MyEnum value = m ?? default(MyEnum);
                dy[value] = result;
                result = dy[value];
                index++;
            }

            if (index == 2 && result.Length == 2 && result[0] == null && result[1].Field == 3 && MemberClass.t_status == 1)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.regclass.regclass047.regclass047
{
    // <Title> Tests regular class indexer used in while loop body.</Title>
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
            MemberClass mc = new MemberClass();
            dynamic dy = mc;
            int index = 0;
            MyEnum?[] array = new MyEnum?[10];
            for (int i = 0; i < array.Length; i++)
                array[i] = MyEnum.Second;
            while (index < 10)
            {
                array[index++] = dy[new MyEnum[index]];
                dy[new MyEnum[index]] = null;
            }

            if (MemberClass.t_status != 2)
                return 1;
            foreach (MyEnum? me in array)
                if (me != null)
                    return 1;
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.regclass.regclass048.regclass048
{
    // <Title> Tests IndexerNameAttribute applied to indexer. </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System.Runtime.CompilerServices;

    public class MyString
    {
        [IndexerName("Chars")]
        public char this[int x]
        {
            get
            {
                return 'a';
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            var s = new MyString();
            dynamic dobj = s;
            if (s[0] != 'a')
                return 1;
            int result = 0;
            int flag = 1;
            try
            {
                if (s[(dynamic)0] == 'a')
                    flag = 0;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadIndexLHS, ex.Message, "MyString"))
                {
                    flag = 0;
                }
            }

            result += flag;
            flag = 1;
            try
            {
                if (dobj[0] == 'a')
                    flag = 0;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadIndexLHS, ex.Message, "MyString"))
                {
                    flag = 0;
                }
            }

            result += flag;
            // error CS1061: 'MyString' does not contain a definition for 'Chars'
            // if (s.Chars[(dynamic)0] == 'a')
            //    flag = 0;
            flag = 1;
            try
            {
                if (dobj.Chars[0] == 'a')
                    flag = 0;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.NoSuchMember, ex.Message, "MyString", "Chars"))
                {
                    flag = 0;
                }
            }

            result += flag;
            return result;
        }
    }
    // </Code>
}
