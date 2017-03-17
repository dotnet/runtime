// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.cnst.cnst01.cnst01
{
    // <Title>Dynamic & const</Title>
    // <Description>default(dynamic) can be declared as const</Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private const object d00 = default(dynamic);
        private const dynamic d10 = default(dynamic);
        private const dynamic d20 = default(object);
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            const object d01 = default(dynamic);
            const dynamic d11 = default(dynamic);
            const dynamic d21 = default(object);
            dynamic b0 = (d00 == null) && (d10 == null) && (d20 == null);
            dynamic b1 = (d01 == null) && (d11 == null) && (d21 == null);
            dynamic b2 = d.Method();
            dynamic b3 = d[""];
            dynamic b4 = MyProperty;
            return (b0 && b1 && b2 && b3 && b4) ? 0 : 1;
        }

        public bool Method()
        {
            const object d02 = default(dynamic);
            const dynamic d12 = default(dynamic);
            const dynamic d22 = default(object);
            return (d02 == null) && (d12 == null) && (d22 == null);
        }

        public bool this[string m]
        {
            get
            {
                const object d03 = default(dynamic);
                const dynamic d13 = default(dynamic);
                const dynamic d23 = default(object);
                return (d03 == null) && (d13 == null) && (d23 == null);
            }
        }

        public static bool MyProperty
        {
            get
            {
                const object d04 = default(dynamic);
                const dynamic d14 = default(dynamic);
                const dynamic d24 = default(object);
                return (d04 == null) && (d14 == null) && (d24 == null);
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.cnst.cnst04.cnst04
{
    // <Title>Dynamic & const</Title>
    // <Description>const value as the method parameter</Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public class Test
    {
        private const Test d0 = default(Test);
        private const string d1 = "";
        private const sbyte d2 = 10;
        private const byte d3 = 10;
        private const short d4 = 10;
        private const ushort d5 = 10;
        private const int d6 = 10;
        private const uint d7 = 10;
        private const long d8 = 10;
        private const ulong d9 = 10;
        private const char d10 = 'a';
        private const float d11 = 12.15f;
        private const double d12 = 12.15;
        private const decimal d13 = 12.15M;
        private const bool d14 = true;
        private const MyEnum d15 = MyEnum.Second;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic b = Method(d0) //not check the type. dynamic null doesn't have type
 && Method(d1, typeof(string)) && Method(d2, typeof(sbyte)) && Method(d3, typeof(byte)) && Method(d4, typeof(short)) && Method(d5, typeof(ushort)) && Method(d6, typeof(int)) && Method(d7, typeof(uint)) && Method(d8, typeof(long)) && Method(d9, typeof(ulong)) && Method(d10, typeof(char)) && Method(d11, typeof(float)) && Method(d12, typeof(double)) && Method(d13, typeof(decimal)) && Method(d14, typeof(bool)) && Method(d15, typeof(MyEnum));
            return b ? 0 : 1;
        }

        public static bool Method(dynamic d)
        {
            //dynamic null doesn't have type. Special check for reference type.
            return null == d;
        }

        public static bool Method(dynamic d, Type t)
        {
            if (d.GetType() == t)
                return true;
            return false;
        }
    }

    public struct MyStruct
    {
        public int Field;
    }

    public enum MyEnum
    {
        First,
        Second,
        Third
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.cnst.cnst05.cnst05
{
    // <Title>Dynamic & const</Title>
    // <Description>const value as the method parameter</Description>
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
            const Test d0 = default(Test);
            const string d1 = "";
            const sbyte d2 = 10;
            const byte d3 = 10;
            const short d4 = 10;
            const ushort d5 = 10;
            const int d6 = 10;
            const uint d7 = 10;
            const long d8 = 10;
            const ulong d9 = 10;
            const char d10 = 'a';
            const float d11 = 12.15f;
            const double d12 = 12.15;
            const decimal d13 = 12.15M;
            const bool d14 = true;
            const MyEnum d15 = MyEnum.Second;
            dynamic d = new Test();
            dynamic b = d.Method<int, string>(d0) //not check the type. dynamic null doesn't have type
 && d.Method<string>(d, d1) && d.Method<sbyte>(d, d2) && d.Method<byte>(d, d3) && d.Method<short>(d, d4) && d.Method<ushort>(d, d5) && d.Method<int>(d, d6) && d.Method<uint>(d, d7) && d.Method<long>(d, d8) && d.Method<ulong>(d, d9) && d.Method<char>(d, d10) && d.Method<float>(d, d11) && d.Method<double>(d, d12) && d.Method<decimal>(d, d13) && d.Method<bool>(d, d14) && d.Method<MyEnum>(d, d15);
            return b ? 0 : 1;
        }

        public bool Method<V, U>(dynamic d)
        {
            //dynamic null doesn't have type. Special check for reference type.
            return null == d;
        }

        // Only care about the second parameter.
        public bool Method<T>(dynamic s, dynamic d)
        {
            if (d.GetType() == typeof(T))
                return true;
            return false;
        }
    }

    public enum MyEnum
    {
        First,
        Second,
        Third
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.cnst.readonly01.readonly01
{
    // <Title>Dynamic & static readonly</Title>
    // <Description>default(dynamic) in static readonly</Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public int Field;
        private static readonly object s_d0 = default(dynamic);
        private static readonly dynamic s_d1 = default(dynamic);
        private static readonly dynamic s_d2 = default(object);
        private static readonly dynamic s_d3 = new Test()
        {
            Field = 10
        }

        ;
        private static readonly dynamic s_d4 = new MyStruct()
        {
            Field = 10
        }

        ;
        private static readonly dynamic s_d5 = MyEnum.Second;
        private static readonly dynamic s_d6 = 10;
        private static readonly dynamic s_d7 = "A";
        private static readonly dynamic s_d8 = null;
        private static readonly dynamic s_d9 = (byte)15;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic b = (s_d0 == null) && (null == s_d1) && (null == s_d2) && (typeof(Test) == s_d3.GetType() && 10 == s_d3.Field) && (typeof(MyStruct) == s_d4.GetType() && 10 == s_d4.Field) && (typeof(MyEnum) == s_d5.GetType() && MyEnum.Second == s_d5) && 10 == s_d6 && "A" == s_d7 && null == s_d8 && 15 == s_d9;
            return b ? 0 : 1;
        }
    }

    public struct MyStruct
    {
        public int Field;
    }

    public enum MyEnum
    {
        First,
        Second,
        Third
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.cnst.readonly01a.readonly01a
{
    // <Title>Dynamic & static readonly</Title>
    // <Description>default(dynamic) in static readonly</Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

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
        public readonly object d0;
        public readonly dynamic d1;
        public readonly dynamic d2;
        public readonly dynamic d3;
        public readonly dynamic d4;
        public readonly dynamic d5;
        public readonly dynamic d6;
        public readonly dynamic d7;
        public readonly dynamic d8;
        public readonly dynamic d9;
        public Test(object o0, dynamic o1, object o2, MyClass o3, MyStruct o4, MyEnum o5, int o6, string o7, dynamic o8, byte o9)
        {
            d0 = o0;
            d1 = o1;
            d2 = o2;
            d3 = o3;
            d4 = o4;
            d5 = o5;
            d6 = o6;
            d7 = o7;
            d8 = o8;
            d9 = o9;
        }

        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test(default(dynamic), default(dynamic), default(object), new MyClass()
            {
                Field = 10
            }

            , new MyStruct()
            {
                Field = 10
            }

            , MyEnum.Second, 10, "A", null, (byte)15);
            dynamic b = (d.d0 == null) && (null == d.d1) && (null == d.d2) && (typeof(MyClass) == d.d3.GetType() && 10 == d.d3.Field) && (typeof(MyStruct) == d.d4.GetType() && 10 == d.d4.Field) && (typeof(MyEnum) == d.d5.GetType() && MyEnum.Second == d.d5) && 10 == d.d6 && "A" == d.d7 && null == d.d8 && 15 == d.d9;
            return b ? 0 : 1;
        }
    }

    public class MyClass
    {
        public int Field;
    }

    public struct MyStruct
    {
        public int Field;
    }

    public enum MyEnum
    {
        First,
        Second,
        Third
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.cnst.readonly02.readonly02
{
    // <Title>Dynamic & readonly</Title>
    // <Description>default(dynamic) in static readonly</Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System;

    public class Test
    {
        public int Field;
        private static readonly dynamic s_d = new Test();
        private static readonly object s_d0 = default(dynamic);
        private static readonly dynamic s_d1 = default(dynamic);
        private static readonly dynamic s_d2 = default(object);
        private static readonly dynamic s_d3 = new Test()
        {
            Field = 10
        }

        ;
        private static readonly dynamic s_d4 = new MyStruct()
        {
            Field = 10
        }

        ;
        private static readonly dynamic s_d5 = MyEnum.Second;
        private static readonly dynamic s_d6 = 10;
        private static readonly dynamic s_d7 = "A";
        private static readonly dynamic s_d8 = null;
        private static readonly dynamic s_d9 = (sbyte)15;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic b = s_d[s_d0] && s_d[s_d1] && s_d[s_d2] && s_d[s_d3, typeof(Test)] && s_d[s_d4, typeof(MyStruct)] && s_d[s_d5, typeof(MyEnum)] && s_d[s_d6, typeof(int)] && s_d[s_d7, typeof(string)] && s_d[s_d8] && s_d[s_d9, typeof(sbyte)];
            return b ? 0 : 1;
        }

        public bool this[dynamic d]
        {
            get
            {
                if (null == d)
                    return true;
                return false;
            }
        }

        public bool this[dynamic d, Type t]
        {
            get
            {
                if (t == d.GetType())
                    return true;
                return false;
            }
        }
    }

    public struct MyStruct
    {
        public int Field;
    }

    public enum MyEnum
    {
        First,
        Second,
        Third
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.cnst.readonly03.readonly03
{
    // <Title>Dynamic & readonly</Title>
    // <Description>default(dynamic) in static readonly</Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System;

    public class Test
    {
        public int Field;
        private static readonly dynamic s_d = new Test();
        public readonly object d0 = default(dynamic);
        public readonly dynamic d1 = default(dynamic);
        public readonly dynamic d2 = default(object);
        public readonly dynamic d3 = new MyClass()
        {
            Field = 10
        }

        ;
        public readonly dynamic d4 = new MyStruct()
        {
            Field = 10
        }

        ;
        public readonly dynamic d5 = MyEnum.Second;
        public readonly dynamic d6 = 10;
        public readonly dynamic d7 = "A";
        public readonly dynamic d8 = null;
        public readonly dynamic d9 = (sbyte)15;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic b = s_d[s_d.d0] && s_d[s_d.d1] && s_d[s_d.d2] && s_d[s_d.d3, typeof(MyClass)] && s_d[s_d.d4, typeof(MyStruct)] && s_d[s_d.d5, typeof(MyEnum)] && s_d[s_d.d6, typeof(int)] && s_d[s_d.d7, typeof(string)] && s_d[s_d.d8] && s_d[s_d.d9, typeof(sbyte)];
            return b ? 0 : 1;
        }

        public bool this[dynamic d]
        {
            get
            {
                if (null == d)
                    return true;
                return false;
            }
        }

        public bool this[dynamic d, Type t]
        {
            get
            {
                if (t == d.GetType())
                    return true;
                return false;
            }
        }
    }

    public struct MyStruct
    {
        public int Field;
    }

    public enum MyEnum
    {
        First,
        Second,
        Third
    }

    public class MyClass
    {
        public int Field;
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.cnst.readonly04.readonly04
{
    // <Title>Dynamic & readonly</Title>
    // <Description>readonly value can NOT be modify.</Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private static readonly dynamic s_d = new Test();
        public readonly object d0 = default(dynamic);
        public readonly dynamic d1 = default(dynamic);
        public readonly dynamic d2 = default(object);
        public readonly dynamic d3 = new MyClass()
        {
            Field = 10
        }

        ;
        public readonly dynamic d4 = new MyStruct()
        {
            Field = 10
        }

        ;
        public readonly dynamic d5 = MyEnum.Second;
        public readonly dynamic d6 = 10;
        public readonly dynamic d7 = "A";
        public readonly dynamic d8 = null;
        public readonly dynamic d9 = (sbyte)15;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            bool ret = true;
            bool isPass = true;
            try
            {
                s_d.d0 = new object();
                isPass &= false;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                ret = ErrorVerifier.Verify(ErrorMessageId.AssgReadonly, ex.Message);
                if (ret)
                    isPass &= true;
                else
                    isPass &= false;
            }

            try
            {
                s_d.d1 = new object();
                isPass &= false;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                ret = ErrorVerifier.Verify(ErrorMessageId.AssgReadonly, ex.Message);
                if (ret)
                    isPass &= true;
                else
                    isPass &= false;
            }

            try
            {
                s_d.d2 = new object();
                isPass &= false;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                ret = ErrorVerifier.Verify(ErrorMessageId.AssgReadonly, ex.Message);
                if (ret)
                    isPass &= true;
                else
                    isPass &= false;
            }

            try
            {
                s_d.d3 = new MyClass();
                isPass &= false;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                ret = ErrorVerifier.Verify(ErrorMessageId.AssgReadonly, ex.Message);
                if (ret)
                    isPass &= true;
                else
                    isPass &= false;
            }

            try
            {
                s_d.d3.Field = 20;
                if (s_d.d3.Field == 20)
                    isPass &= true; //reference type!
                else
                    isPass &= false;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
            {
                isPass &= false;
            }

            try
            {
                s_d.d4 = new MyStruct();
                isPass &= false;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                ret = ErrorVerifier.Verify(ErrorMessageId.AssgReadonly, ex.Message);
                if (ret)
                    isPass &= true;
                else
                    isPass &= false;
            }

            try
            {
                s_d.d4.Field = 20;
                if (s_d.d4.Field == 20)
                    isPass &= true; //value type!
                else
                    isPass &= false;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
            {
                isPass &= false;
            }

            try
            {
                s_d.d5 = MyEnum.First;
                isPass &= false;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                ret = ErrorVerifier.Verify(ErrorMessageId.AssgReadonly, ex.Message);
                if (ret)
                    isPass &= true;
                else
                    isPass &= false;
            }

            try
            {
                s_d.d6 = 0;
                isPass &= false;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                ret = ErrorVerifier.Verify(ErrorMessageId.AssgReadonly, ex.Message);
                if (ret)
                    isPass &= true;
                else
                    isPass &= false;
            }

            try
            {
                s_d.d7 = "";
                isPass &= false;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                ret = ErrorVerifier.Verify(ErrorMessageId.AssgReadonly, ex.Message);
                if (ret)
                    isPass &= true;
                else
                    isPass &= false;
            }

            try
            {
                s_d.d8 = new object();
                isPass &= false;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                ret = ErrorVerifier.Verify(ErrorMessageId.AssgReadonly, ex.Message);
                if (ret)
                    isPass &= true;
                else
                    isPass &= false;
            }

            try
            {
                s_d.d9 = "";
                isPass &= false;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                ret = ErrorVerifier.Verify(ErrorMessageId.AssgReadonly, ex.Message);
                if (ret)
                    isPass &= true;
                else
                    isPass &= false;
            }

            return isPass ? 0 : 1;
        }
    }

    public struct MyStruct
    {
        public int Field;
    }

    public enum MyEnum
    {
        First,
        Second,
        Third
    }

    public class MyClass
    {
        public int Field;
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.cnst.readonly04a.readonly04a
{
    // <Title>Dynamic & readonly</Title>
    // <Description>readonly value can NOT be modify.</Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

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
        public readonly object d0;
        public readonly dynamic d1;
        public readonly dynamic d2;
        public readonly dynamic d3;
        public readonly dynamic d4;
        public readonly dynamic d5;
        public readonly dynamic d6;
        public readonly dynamic d7;
        public readonly dynamic d8;
        public readonly dynamic d9;
        public Test(object o0, dynamic o1, dynamic o2, MyClass o3, MyStruct o4, MyEnum o5, int o6, string o7, object o8, sbyte o9)
        {
            d0 = o0;
            d1 = o1;
            d2 = o2;
            d3 = o3;
            d4 = o4;
            d5 = o5;
            d6 = o6;
            d7 = o7;
            d8 = o8;
            d9 = o9;
        }

        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test(default(dynamic), default(dynamic), default(object), new MyClass()
            {
                Field = 10
            }

            , new MyStruct()
            {
                Field = 10
            }

            , MyEnum.Second, 10, "A", null, (sbyte)15);
            bool isPass = true;
            bool ret = true;
            try
            {
                d.d0 = new object();
                isPass &= false;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                ret = ErrorVerifier.Verify(ErrorMessageId.AssgReadonly, ex.Message);
                if (ret)
                    isPass &= true;
                else
                    isPass &= false;
            }

            try
            {
                d.d1 = new object();
                isPass &= false;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                ret = ErrorVerifier.Verify(ErrorMessageId.AssgReadonly, ex.Message);
                if (ret)
                    isPass &= true;
                else
                    isPass &= false;
            }

            try
            {
                d.d2 = new object();
                isPass &= false;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                ret = ErrorVerifier.Verify(ErrorMessageId.AssgReadonly, ex.Message);
                if (ret)
                    isPass &= true;
                else
                    isPass &= false;
            }

            try
            {
                d.d3 = new MyClass();
                isPass &= false;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                ret = ErrorVerifier.Verify(ErrorMessageId.AssgReadonly, ex.Message);
                if (ret)
                    isPass &= true;
                else
                    isPass &= false;
            }

            try
            {
                d.d3.Field = 20;
                if (d.d3.Field == 20)
                    isPass &= true; //reference type!
                else
                    isPass &= false;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                ret = ErrorVerifier.Verify(ErrorMessageId.AssgReadonly, ex.Message);
                if (ret)
                    isPass &= true;
                else
                    isPass &= false;
            }

            try
            {
                d.d4 = new MyStruct();
                isPass &= false;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                ret = ErrorVerifier.Verify(ErrorMessageId.AssgReadonly, ex.Message);
                if (ret)
                    isPass &= true;
                else
                    isPass &= false;
            }

            try
            {
                d.d4.Field = 20;
                if (d.d4.Field == 20)
                    isPass &= true; //value type!  
                else
                    isPass &= false;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                ret = ErrorVerifier.Verify(ErrorMessageId.AssgReadonly, ex.Message);
                if (ret)
                    isPass &= true;
                else
                    isPass &= false;
            }

            try
            {
                d.d5 = MyEnum.First;
                isPass &= false;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                ret = ErrorVerifier.Verify(ErrorMessageId.AssgReadonly, ex.Message);
                if (ret)
                    isPass &= true;
                else
                    isPass &= false;
            }

            try
            {
                d.d6 = 0;
                isPass &= false;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                ret = ErrorVerifier.Verify(ErrorMessageId.AssgReadonly, ex.Message);
                if (ret)
                    isPass &= true;
                else
                    isPass &= false;
            }

            try
            {
                d.d7 = "";
                isPass &= false;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                ret = ErrorVerifier.Verify(ErrorMessageId.AssgReadonly, ex.Message);
                if (ret)
                    isPass &= true;
                else
                    isPass &= false;
            }

            try
            {
                d.d8 = new object();
                isPass &= false;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                ret = ErrorVerifier.Verify(ErrorMessageId.AssgReadonly, ex.Message);
                if (ret)
                    isPass &= true;
                else
                    isPass &= false;
            }

            try
            {
                d.d9 = "";
                isPass &= false;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                ret = ErrorVerifier.Verify(ErrorMessageId.AssgReadonly, ex.Message);
                if (ret)
                    isPass &= true;
                else
                    isPass &= false;
            }

            return isPass ? 0 : 1;
        }
    }

    public struct MyStruct
    {
        public int Field;
    }

    public enum MyEnum
    {
        First,
        Second,
        Third
    }

    public class MyClass
    {
        public int Field;
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.cnst.readonly06.readonly06
{
    // <Title>Dynamic & readonly</Title>
    // <Description>The field of readonly struct can NOT be modify.</Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static readonly dynamic d0 = new MyClass()
        {
            Field = 9
        }

        ;
        public static readonly dynamic d1 = new MyStruct()
        {
            Field = 9
        }

        ;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            bool isPass = true;
            bool ret = true;
            // Field
            d0.Field = 10;
            if (d0.Field != 10)
                isPass &= false;
            d1.Field = 10;
            if (d1.Field != 10)
                isPass &= false;
            // Property
            d0.MyProperty1 = 0;
            if (d0.Field != 12)
                isPass &= false;
            d1.MyProperty1 = 0;
            if (d1.Field != 12)
                isPass &= false;
            // Indexer
            d0[0] = 0;
            if (d0.Field != 14)
                isPass &= false;
            d1[0] = 0;
            if (d1.Field != 14)
                isPass &= false;
            //private property set
            try
            {
                d0.MyProperty2 = 0;
                isPass &= false;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                ret = ErrorVerifier.Verify(ErrorMessageId.InaccessibleSetter, ex.Message, "MyClass.MyProperty2");
                if (!ret)
                {
                    System.Console.WriteLine("error message is wrong");
                    return 1;
                }

                if (d0.MyProperty2 != 14)
                    isPass &= false;
                else
                    isPass &= true;
            }

            try
            {
                d1.MyProperty2 = 0;
                isPass &= false;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                ret = ErrorVerifier.Verify(ErrorMessageId.InaccessibleSetter, ex.Message, "MyStruct.MyProperty2");
                if (!ret)
                {
                    System.Console.WriteLine("error message is wrong");
                    return 1;
                }

                if (d1.MyProperty2 != 14)
                    isPass &= false;
                else
                    isPass &= true;
            }

            //private indexer set
            try
            {
                d0['a'] = 0;
                isPass &= false;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                ret = ErrorVerifier.Verify(ErrorMessageId.InaccessibleSetter, ex.Message, "MyClass.this[char]");
                if (!ret)
                {
                    System.Console.WriteLine("error message is wrong");
                    return 1;
                }

                if (d0['a'] != 14)
                    isPass &= false;
                else
                    isPass &= true;
            }

            try
            {
                d1['a'] = 0;
                isPass &= false;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                ret = ErrorVerifier.Verify(ErrorMessageId.InaccessibleSetter, ex.Message, "MyStruct.this[char]");
                if (!ret)
                {
                    System.Console.WriteLine("error message is wrong");
                    return 1;
                }

                if (d1['a'] != 14)
                    isPass &= false;
                else
                    isPass &= true;
            }

            return isPass ? 0 : 1;
        }
    }

    public struct MyStruct
    {
        public int Field;
        public int MyProperty1
        {
            set
            {
                Field = 12;
            }
        }

        public int MyProperty2
        {
            get
            {
                return Field;
            }

            private set
            {
                Field = 13;
            }
        }

        public int this[int a]
        {
            set
            {
                Field = 14;
            }
        }

        public int this[char a]
        {
            get
            {
                return Field;
            }

            private set
            {
                Field = 15;
            }
        }
    }

    public class MyClass
    {
        public int Field;
        public int MyProperty1
        {
            set
            {
                Field = 12;
            }
        }

        public int MyProperty2
        {
            get
            {
                return Field;
            }

            private set
            {
                Field = 13;
            }
        }

        public int this[int a]
        {
            set
            {
                Field = 14;
            }
        }

        public int this[char a]
        {
            get
            {
                return Field;
            }

            private set
            {
                Field = 15;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.cnst.readonly07.readonly07
{
    // <Title>Dynamic & readonly</Title>
    // <Description>The field of readonly struct can NOT be modify.</Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static readonly dynamic d1 = new MyStruct()
        {
            Array = new int[2],
            MC = new MyClass()
        }

        ;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            bool isPass = true;
            bool ret = true;
            // Can do the array accessor.
            d1.Array[1] = 10;
            if (d1.Array[1] != 10)
                isPass &= false;
            // Field
            d1.MC.Field = 10;
            if (d1.MC.Field != 10)
                isPass &= false;
            // Property
            d1.MC.MyProperty1 = 0;
            if (d1.MC.Field != 12)
                isPass &= false;
            // Indexer
            d1.MC[0] = 0;
            if (d1.MC.Field != 14)
                isPass &= false;
            //private property set
            try
            {
                d1.MC.MyProperty2 = 0;
                isPass &= false;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                ret = ErrorVerifier.Verify(ErrorMessageId.InaccessibleSetter, ex.Message, "MyClass.MyProperty2");
                if (!ret)
                {
                    System.Console.WriteLine("error message is wrong");
                    return 1;
                }

                if (d1.MC.MyProperty2 != 14)
                    isPass &= false;
                else
                    isPass &= true;
            }

            //private indexer set
            try
            {
                d1.MC['a'] = 0;
                isPass &= false;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                ret = ErrorVerifier.Verify(ErrorMessageId.InaccessibleSetter, ex.Message, "MyClass.this[char]");
                if (!ret)
                {
                    System.Console.WriteLine("error message is wrong");
                    return 1;
                }

                if (d1.MC['a'] != 14)
                    isPass &= false;
                else
                    isPass &= true;
            }


            d1.MC = null;
            if (d1.MC != null)
                isPass &= false;
            return isPass ? 0 : 1;
        }
    }

    public struct MyStruct
    {
        public int[] Array;
        public MyClass MC;
    }

    public class MyClass
    {
        public int Field;
        public int MyProperty1
        {
            set
            {
                Field = 12;
            }
        }

        public int MyProperty2
        {
            get
            {
                return Field;
            }

            private set
            {
                Field = 13;
            }
        }

        public int this[int a]
        {
            set
            {
                Field = 14;
            }
        }

        public int this[char a]
        {
            get
            {
                return Field;
            }

            private set
            {
                Field = 15;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.cnst.readonly08.readonly08
{
    // <Title>Dynamic & readonly</Title>
    // <Description>The field of readonly struct can NOT be modify.Struct inner struct.</Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static readonly dynamic d1 = new MyStruct()
        {
            MS = new MyStruct2()
            {
                Field = 9
            }
        }

        ;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            bool isPass = true;
            bool ret = true;
            // Field
            d1.MS.Field = 10;
            if (d1.MS.Field != 9)
                isPass &= false;
            // Property
            d1.MS.MyProperty1 = 0;
            if (d1.MS.Field != 9)
                isPass &= false;
            // Indexer
            d1.MS[0] = 0;
            if (d1.MS.Field != 9)
                isPass &= false;
            //private property set
            try
            {
                d1.MS.MyProperty2 = 0;
                isPass &= false;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                ret = ErrorVerifier.Verify(ErrorMessageId.InaccessibleSetter, ex.Message, "MyStruct2.MyProperty2");
                if (!ret)
                {
                    System.Console.WriteLine("error message is wrong");
                    return 1;
                }

                if (d1.MS.MyProperty2 != 9)
                    isPass &= false;
                else
                    isPass &= true;
            }

            //private indexer set
            try
            {
                d1.MS['a'] = 0;
                isPass &= false;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                ret = ErrorVerifier.Verify(ErrorMessageId.InaccessibleSetter, ex.Message, "MyStruct2.this[char]");
                if (!ret)
                {
                    System.Console.WriteLine("error message is wrong");
                    return 1;
                }

                if (d1.MS['a'] != 9)
                    isPass &= false;
                else
                    isPass &= true;
            }

            d1.MS = new MyStruct2();
            if (d1.MS.Field != 0)
                isPass &= false;
            return isPass ? 0 : 1;
        }
    }

    public struct MyStruct
    {
        public MyStruct2 MS;
    }

    public struct MyStruct2
    {
        public int Field;
        public int MyProperty1
        {
            set
            {
                Field = 12;
            }
        }

        public int MyProperty2
        {
            get
            {
                return Field;
            }

            private set
            {
                Field = 13;
            }
        }

        public int this[int a]
        {
            set
            {
                Field = 14;
            }
        }

        public int this[char a]
        {
            get
            {
                return Field;
            }

            private set
            {
                Field = 15;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.cnst.readonly09.readonly09
{
    // <Title>Dynamic & readonly</Title>
    // <Description>The field of readonly struct can NOT be modify.</Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static readonly dynamic d1 = new MyClass()
        {
            Array = new int[2],
            MS = new MyStruct()
            {
                Field = 9
            }
        }

        ;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            bool isPass = true;
            bool ret = true;
            // Can do the array accessor.
            d1.Array[1] = 10;
            if (d1.Array[1] != 10)
                isPass &= false;
            // inner struct field can not be modify.
            // Field
            d1.MS.Field = 10;
            if (d1.MS.Field != 9)
                isPass &= false;
            // Property
            d1.MS.MyProperty1 = 0;
            if (d1.MS.Field != 9)
                isPass &= false;
            // Indexer
            d1.MS[0] = 0;
            if (d1.MS.Field != 9)
                isPass &= false;
            //private property set
            try
            {
                d1.MS.MyProperty2 = 0;
                isPass &= false;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                ret = ErrorVerifier.Verify(ErrorMessageId.InaccessibleSetter, ex.Message, "MyStruct.MyProperty2");
                if (!ret)
                {
                    System.Console.WriteLine("error message is wrong");
                    return 1;
                }

                if (d1.MS.MyProperty2 != 9)
                    isPass &= false;
                else
                    isPass &= true;
            }

            //private indexer set
            try
            {
                d1.MS['a'] = 0;
                isPass &= false;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                ret = ErrorVerifier.Verify(ErrorMessageId.InaccessibleSetter, ex.Message, "MyStruct.this[char]");
                if (!ret)
                {
                    System.Console.WriteLine("error message is wrong");
                    return 1;
                }

                if (d1.MS['a'] != 9)
                    isPass &= false;
                else
                    isPass &= true;
            }

            //should not error here!
            d1.MS = new MyStruct();
            if (d1.MS.Field != 0)
                isPass &= false;
            return isPass ? 0 : 1;
        }
    }

    public class MyClass
    {
        public int[] Array;
        public MyStruct MS;
    }

    public struct MyStruct
    {
        public int Field;
        public int MyProperty1
        {
            set
            {
                Field = 12;
            }
        }

        public int MyProperty2
        {
            get
            {
                return Field;
            }

            private set
            {
                Field = 13;
            }
        }

        public int this[int a]
        {
            set
            {
                Field = 14;
            }
        }

        public int this[char a]
        {
            get
            {
                return Field;
            }

            private set
            {
                Field = 15;
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.statements.cnst.readonly10.readonly10
{
    // <Title>Dynamic & readonly</Title>
    // <Description>The field of readonly struct can be evaluated in member initializer.</Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public struct Struc
    {
        public int i;
    }

    public class C
    {
        public readonly dynamic str = new Struc();
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            C c = new C
            {
                str =
            {
            i = 1
            }
            }

            ;
            return 0;
        }
    }
    // <Code>
}
