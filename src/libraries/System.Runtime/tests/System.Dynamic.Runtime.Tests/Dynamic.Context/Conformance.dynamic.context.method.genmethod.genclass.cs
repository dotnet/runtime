// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.genclass.genclassgenmeth.genclassgenmeth;
using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.genclass.genclassgenmeth.genclassgenmeth
{
    public class C
    {
    }

    public interface I
    {
    }

    public class MemberClass<T>
    {
        #region Instance methods
        public U Method_ReturnsU<U>(U u)
        {
            return u;
        }

        public U Method_ReturnsU<U>(T t)
        {
            return default(U);
        }

        public U Method_ReturnsU<U, V>(T t, V v)
        {
            return default(U);
        }

        public T Method_ReturnsT<U>(params U[] d)
        {
            return default(T);
        }

        public T Method_ReturnsT<U>(float x, T t, U u)
        {
            return default(T);
        }

        public T Method_ReturnsT<U>(U u)
        {
            return default(T);
        }

        public T Method_ReturnsT<U, V>(out U u, ref V v)
        {
            u = default(U);
            return default(T);
        }

        public int Method_ReturnsInt(T t)
        {
            return 1;
        }

        public string Method_ReturnsString<U>(T t, U u)
        {
            return "foo";
        }

        public float Method_ReturnsFloat<U, V>(T t, dynamic d, U u)
        {
            return 3.4f;
        }

        public float Method_ReturnsFloat<U, V>(T t, dynamic d, U u, ref decimal dec)
        {
            dec = 3m;
            return 3.4f;
        }

        public dynamic Method_ReturnsDynamic(T t)
        {
            return t;
        }

        public dynamic Method_ReturnsDynamic<U>(T t, U u, dynamic d)
        {
            return u;
        }

        public dynamic Method_ReturnsDynamic<U, V>(T t, U u, V v, dynamic d)
        {
            return v;
        }

        #region Constraints on methods that have a new type parameter
        public U Method_ReturnsUConstraint<U>(U u) where U : class
        {
            return null;
        }

        public U Method_ReturnsUConstraint<U>(T t) where U : new()
        {
            return new U();
        }

        public U Method_ReturnsUConstraint<U, V>(T t, V v) where U : new() where V : U
        {
            return default(V);
        }

        public void Method_WithConstraints<U, V, W>(T t, U u, V v, W w) where U : struct where V : class where W : V
        {
        }

        public dynamic Method_ReturnsDynamicConstraint<U>(T t, U u, dynamic d) where U : new()
        {
            return new U();
        }

        public dynamic Method_ReturnsDynamicConstraint<U, V>(T t, U u, V v, dynamic d) where V : class
        {
            return u;
        }

        public float Method_ReturnsFloatConstraint<U, V>(T t, dynamic d, U u, ref decimal dec) where V : U
        {
            dec = 3m;
            return 3.4f;
        }

        public string Method_ReturnsStringConstraint<U, V>(T t, dynamic d, U u) where U : class where V : U
        {
            return "";
        }

        //These are negative methods... you should not be able to call them with the dynamic type because the dynamic type would not satisfy the constraints
        //you cannot call this like: Method_ReturnsUConstraint<dynamic>(d); because object does not derive from C
        public U Method_ReturnsUNegConstraint<U>(U u) where U : C
        {
            return null;
        }

        public T Method_ReturnsTNegConstraint<U>(U u) where U : struct
        {
            return default(T);
        }

        public float Method_ReturnsFloatNegConstraint<U, V>(T t, dynamic d, U u, ref decimal dec) where V : U where U : I
        {
            dec = 3m;
            return 3.4f;
        }

        #endregion
        #region Nested class
        public class NestedMemberClass<U>
        {
            public U Method_ReturnsU(T t)
            {
                return default(U);
            }

            public T Method_ReturnsT(params U[] d)
            {
                return default(T);
            }

            public T Method_ReturnsT<V>(out U u, ref V v)
            {
                u = default(U);
                return default(T);
            }

            public dynamic Method_ReturnsDynamic(T t, U u, dynamic d)
            {
                return u;
            }

            public dynamic Method_ReturnsDynamic<V>(T t, U u, V v, dynamic d)
            {
                return v;
            }

            public decimal Method_ReturnsDecimal<V>(T t, dynamic d, U u)
            {
                return 3.4m;
            }

            public byte Method_ReturnsByte<V>(T t, dynamic d, U u, ref double dec)
            {
                dec = 3;
                return 4;
            }
        }
        #endregion
        #endregion
    }

    public class MemberClassWithClassConstraint<T>
        where T : class
    {
        public int Method_ReturnsInt<U>() where U : T
        {
            return 1;
        }

        public T Method_ReturnsT<U, V>(decimal dec, dynamic d) where U : T where V : U
        {
            return null;
        }
    }

    public class MemberClassWithNewConstraint<T>
        where T : new()
    {
        public U Method_ReturnsU<U>() where U : T
        {
            return default(U);
        }

        public dynamic Method_ReturnsDynamic<V>(T u, V v) where V : T
        {
            return new T();
        }
    }

    public class MemberClassWithAnotherTypeConstraint<T, U>
        where T : U
    {
        public U Method_ReturnsU()
        {
            return default(U);
        }

        public dynamic Method_ReturnsDynamic<V>(int x, U u, V v) where V : U
        {
            return default(T);
        }
    }

    #region Negative tests - you should not be able to construct this with a dynamic object
    public class MemberClassWithUDClassConstraint<T>
        where T : C
    {
        public U Method_ReturnsU<U>() where U : T, new()
        {
            return new U();
        }
    }

    public class MemberClassWithStructConstraint<T>
        where T : struct
    {
        public U Method_ReturnsU<U>()
        {
            return default(U);
        }
    }

    public class MemberClassWithInterfaceConstraint<T>
        where T : I
    {
        public dynamic Method_ReturnsDynamic<U, V>(int x, U u, V v) where V : U where U : T
        {
            return default(T);
        }
    }
    #endregion
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.genclass.genclass001.genclass001
{
    // <Title> Tests generic class generic method used in static method body.</Title>
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
            if (Test.TestMethod() != 0)
                return 1;
            return 0;
        }

        public static int TestMethod()
        {
            dynamic mc = new MemberClass<int>();
            int ret = mc.Method_ReturnsT<string>(string.Empty, "ab", null);
            dynamic d1 = string.Empty;
            dynamic d2 = "ab";
            ret += mc.Method_ReturnsT(d1, d2, null);
            return ret;
        }
    } //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.genclass.genclass002.genclass002
{
    // <Title> Tests generic class generic method used inside #if, #else block.</Title>
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
            if (Test.TestMethod() != null)
                return 1;
            return 0;
        }

        public static object TestMethod()
        {
#if c1
dynamic mc = new MemberClass<int>();
return (Test)mc.Method_ReturnsUConstraint<Test>(new Test());
 #else
            dynamic mc = new MemberClass<int>();
            object ret = mc.Method_ReturnsUConstraint<string>("1");
            try
            {
                return (int)mc.Method_ReturnsUConstraint<string>(1);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.NewConstraintNotSatisfied, e.Message, "MemberClass<T>.Method_ReturnsUConstraint<U>(T)", "U", "string"))
                    return ret;
            }

            return new object();
#endif
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.genclass.genclass003.genclass003
{
    // <Title> Tests generic class generic method used in implicitly-typed variable initializer.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private static dynamic s_mc = new MemberClass<string>();
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            var loc = (int)s_mc.Method_ReturnsU<int>(null);
            if (loc != 0)
                return 1;
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.genclass.genclass006.genclass006
{
    // <Title> Tests generic class generic method used in implicit or explicit operator.</Title>
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
            dynamic dy1 = new MemberClass<int>();
            long result1 = (int)dy1.Method_ReturnsDynamic(1); //implicit
            dynamic dy2 = new MemberClass<long>();
            int result2 = (int)dy2.Method_ReturnsDynamic(1L); //explicit
            return (result1 == result2) ? 0 : 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.genclass.genclass007.genclass007
{
    // <Title> Tests generic class generic method used in implicit operator.</Title>
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
                dynamic dy = new MemberClass<InnerTest1>();
                return new InnerTest2()
                {
                    field = (int)dy.Method_ReturnsU<int>(t1.field + 1)
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
            dynamic dy = new MemberClass<InnerTest2>();
            InnerTest2 result1 = (InnerTest1)dy.Method_ReturnsDynamic<InnerTest1>(new InnerTest2()
            {
                field = 0
            }

            , new InnerTest1()
            {
                field = 10
            }

            , 0); //implicit
            return (result1.field == 11) ? 0 : 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.genclass.genclass008.genclass008
{
    // <Title> Tests generic class generic method used in foreach expression.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections;
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
            dynamic mc = new MemberClass<string>();
            List<string> list = new List<string>()
            {
            null, string.Empty, "Test"
            }

            ;
            List<string> list2 = new List<string>();
            foreach (dynamic s in (IEnumerable)mc.Method_ReturnsDynamic<int, IEnumerable>(null, 0, list, string.Empty))
            {
                list2.Add(s);
            }

            if (list2.Count == 3 && list2[0] == null && list2[1] == string.Empty && list2[2] == "Test")
            {
                return 0;
            }
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.genclass.genclass009.genclass009
{
    // <Title> Tests generic class generic method used in the foreach loop body.</Title>
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
            dynamic mc = new MemberClass<string>();
            var list1 = new List<int>()
            {
            0, 1, 2
            }

            ;
            var list2 = new List<int>();
            foreach (int s in list1)
            {
                list2.Add((int)mc.Method_ReturnsU<int, string>(null, null) + s);
            }

            if (list2.Count == 3 && list2[0] == 0 && list2[1] == 1 && list2[2] == 2)
            {
                return 0;
            }
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.genclass.genclass010.genclass010
{
    // <Title> Tests generic class generic method used in static method body.</Title>
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
            dynamic mc = new MemberClass<string>();
            if ((string)mc.Method_ReturnsT<long>(1.11f, string.Empty, 1L) == null)
                return 0;
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.genclass.genclass011.genclass011
{
    // <Title> Tests generic class generic method used in static method body.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test : I
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic mc = new MemberClass<int>();
            if ((int)mc.Method_ReturnsT<I>(new Test()) == 0)
                return 0;
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.genclass.genclass012.genclass012
{
    // <Title> Tests generic class generic method used in static method body.</Title>
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
            string u = "Test";
            int v = 10;
            dynamic mc = new MemberClass<long>();
            long result = (long)mc.Method_ReturnsT<string, int>(out u, ref v);
            if (result == 0 && u == null && v == 10)
                return 0;
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.genclass.genclass013.genclass013
{
    // <Title> Tests generic class generic method used in while/do expression</Title>
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
            dynamic mc = new MemberClass<int>();
            int[] array = new int[]
            {
            1, 2, 1, 3, 1
            }

            ;
            int i = 0;
            while (i < array.Length && array[i] >= (int)mc.Method_ReturnsInt(array[i]))
            {
                array[i] = (int)mc.Method_ReturnsInt(array[i]);
                i++;
            }

            while (i < array.Length)
            {
                if (array[i] != 1)
                    return 1;
            }

            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.genclass.genclass014.genclass014
{
    // <Title> Tests generic class generic method used in static method body.</Title>
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
            dynamic mc = new MemberClass<string>();
            float result = (float)mc.Method_ReturnsFloat<int, C>(null, new C(), 0);
            if (result == 3.4f)
                return 0;
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.genclass.genclass015.genclass015
{
    // <Title> Tests generic class generic method used in static method body.</Title>
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
            decimal dec = 0M;
            dynamic mc = new MemberClass<string>();
            float result = (float)mc.Method_ReturnsFloat<int, C>(null, new C(), 0, ref dec);
            if (result == 3.4f && dec == 3M)
                return 0;
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.genclass.genclass016.genclass016
{
    // <Title> Tests generic class generic method used in static method body.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private int _field;
        public Test()
        {
            _field = 10;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic mc = new MemberClass<string>();
            Test result = null;
            try
            {
                result = (Test)mc.Method_ReturnsUConstraint<Test>(null);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.AmbigCall, e.Message, "MemberClass<string>.Method_ReturnsUConstraint<Test>(Test)", "MemberClass<string>.Method_ReturnsUConstraint<Test>(string)"))
                    result = new Test()
                    {
                        _field = 10
                    }

                    ;
            }

            if (result != null && result._field == 10)
                return 0;
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.genclass.genclass017.genclass017
{
    // <Title> Tests generic class generic method used in static method body.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        internal int Field;
        public Test()
        {
            Field = 10;
        }

        public class InnerTest : Test
        {
            public InnerTest()
            {
                Field = 11;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic mc = new MemberClass<string>();
            Test result = (Test)mc.Method_ReturnsUConstraint<Test, InnerTest>(null, new InnerTest()
            {
                Field = 0
            }

            );
            if (result == null)
                return 0;
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.genclass.genclass018.genclass018
{
    // <Title> Tests generic class generic method used in static method body.</Title>
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
            dynamic mc = new MemberClass<int>();
            string result = "a";
            try
            {
                result = (string)mc.Method_ReturnsDynamicConstraint<string>(10, null, mc);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.NewConstraintNotSatisfied, e.Message, "MemberClass<T>.Method_ReturnsDynamicConstraint<U>(T, U, object)", "U", "string"))
                    result = string.Empty;
            }

            if (result == string.Empty)
                return 0;
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.genclass.genclass019.genclass019
{
    // <Title> Tests generic class generic method used in static method body.</Title>
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
            dynamic mc = new MemberClass<string>();
            dynamic mc2 = "Test2";
            dynamic mc3 = "Test3";
            string result = (string)mc.Method_ReturnsDynamicConstraint<string, C>(null, mc2, new C(), mc3);
            if (result == "Test2" && (string)mc3 == "Test3")
                return 0;
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.genclass.genclass020.genclass020
{
    // <Title> Tests generic class generic method used in static method body.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        internal int Field;
        public class InnerTest : Test
        {
        }


        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            decimal dec = 10M;
            dynamic mc = new MemberClass<Test>();
            Test mc2 = new Test()
            {
                Field = 2
            }

            ;
            dynamic dy2 = mc2;
            Test mc3 = new InnerTest()
            {
                Field = 3
            }

            ;
            float result = (float)mc.Method_ReturnsFloatConstraint<Test, InnerTest>(mc2, dy2, mc3, ref dec);
            if (dec == 3M && result == 3.4f)
                return 0;
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.genclass.genclass021.genclass021
{
    // <Title> Tests generic class generic method used in static method body.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        internal int Field;
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
            dynamic mc = new MemberClass<Test>();
            Test mc2 = new Test()
            {
                Field = 2
            }

            ;
            dynamic dy2 = mc2;
            Test mc3 = new InnerTest()
            {
                Field = 3
            }

            ;
            string result = (string)mc.Method_ReturnsStringConstraint<Test, InnerTest>(mc2, dy2, mc3);
            if (result == string.Empty)
                return 0;
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.genclass.genclass023.genclass023
{
    // <Title> Tests generic class generic method used in static method body.</Title>
    // <Description>
    // Negative: dynamic type would not satisfy the constraints.
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
            dynamic mc = new MemberClass<string>();
            Test t = new Test();
            dynamic dy = t;
            try
            {
                string result = (string)mc.Method_ReturnsTNegConstraint<Test>(dy);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.ValConstraintNotSatisfied, e.Message, "MemberClass<T>.Method_ReturnsTNegConstraint<U>(U)", "U", "Test"))
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.genclass.genclass024.genclass024
{
    // <Title> Tests generic class generic method used in static method body.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test : I
    {
        internal int Field;
        public class InnerTest : Test
        {
        }


        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic mc = new MemberClass<Test>();
            Test mc2 = new Test()
            {
                Field = 2
            }

            ;
            dynamic dy2 = mc2;
            Test mc3 = new InnerTest()
            {
                Field = 3
            }

            ;
            dynamic dy3 = mc3;
            decimal dec = 3;
            decimal result = (decimal)mc.Method_ReturnsFloatNegConstraint<Test, InnerTest>(dy2, dy2, dy3, ref dec);
            if (result != 3.4M)
                return 1;
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.genclass.genclass025.genclass025
{
    // <Title> Tests generic class generic method used in anonymous method.</Title>
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
            dynamic mc = new MemberClass<string>.NestedMemberClass<int>();
            Func<string, int> func = delegate (string arg)
            {
                return (int)mc.Method_ReturnsU(null);
            }

            ;
            if (func(null) == 0)
                return 0;
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.genclass.genclass026.genclass026
{
    // <Title> Tests generic class generic method used in lambda expression.</Title>
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
            dynamic mc = new MemberClass<string>.NestedMemberClass<int>();
            Func<int, int, string> func = (int arg1, int arg2) => (string)mc.Method_ReturnsT(arg1, arg1);
            if (func(1, 2) == null)
                return 0;
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.genclass.genclass027.genclass027
{
    // <Title> Tests generic class generic method used in static method body.</Title>
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
            dynamic mc = new MemberClass<string>.NestedMemberClass<int>();
            int u = 10;
            string v = "Test";
            string result = (string)mc.Method_ReturnsT<string>(out u, ref v);
            if (u == 0 && v == "Test" && result == null)
                return 0;
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.genclass.genclass028.genclass028
{
    // <Title> Tests generic class generic method used in volatile field initializer.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private dynamic _mc = new MemberClass<string>.NestedMemberClass<string>();
        private dynamic _dy = "Me";
        private volatile dynamic _field;
        public Test()
        {
            _field = _mc.Method_ReturnsDynamic(null, "Test", _dy);
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Test t = new Test();
            if ((string)t._field == "Test")
                return 0;
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.genclass.genclass030.genclass030
{
    // <Title> Tests generic class generic method used in static method body.</Title>
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
            dynamic mc = new MemberClass<Test>.NestedMemberClass<Test>();
            Test t = new Test();
            dynamic dy = t;
            decimal result = (decimal)mc.Method_ReturnsDecimal<int>(t, dy, t);
            if (result == 3.4M)
                return 0;
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.genclass.genclass031.genclass031
{
    // <Title> Tests generic class generic method used in static method body.</Title>
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
            double dec = 1.1D;
            dynamic mc = new MemberClass<Test>.NestedMemberClass<Test>();
            Test t = new Test();
            dynamic dy = t;
            byte result = (byte)mc.Method_ReturnsByte<int>(t, dy, t, ref dec);
            if (result == 4 && dec == 3)
                return 0;
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.genclass.genclass032.genclass032
{
    // <Title> Tests generic class generic method used in the default section statement list.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private static int s_field = 1;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic mc = new MemberClassWithClassConstraint<string>();
            int result = 0;
            switch (s_field)
            {
                case 0:
                    result = -1;
                    break;
                default:
                    result = (int)mc.Method_ReturnsInt<string>();
                    break;
            }

            if (result == 1)
                return 0;
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.genclass.genclass033.genclass033
{
    // <Title> Tests generic class generic method used in the switch section statement list.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private static int s_field = 0;
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
            dynamic mc = new MemberClassWithClassConstraint<Test>();
            Test result = new InnerTest();
            dynamic innertest = result;
            switch (s_field)
            {
                case 0:
                    result = (Test)mc.Method_ReturnsT<InnerTest, InnerTest>(3.2M, innertest);
                    break;
                default:
                    break;
            }

            if (result == null)
                return 0;
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.genclass.genclass034.genclass034
{
    // <Title> Tests generic class generic method used in static method body.</Title>
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
            dynamic mc = new MemberClassWithNewConstraint<Test>();
            InnerTest result = (InnerTest)mc.Method_ReturnsU<InnerTest>();
            if (result == null)
                return 0;
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.genclass.genclass035.genclass035
{
    // <Title> Tests generic class generic method used in static method body.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private int _field;
        public Test()
        {
            _field = 10;
        }

        public class InnerTest : Test
        {
            public InnerTest()
            {
                _field = 11;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic mc = new MemberClassWithNewConstraint<Test>();
            var t = new InnerTest()
            {
                _field = 0
            }

            ;
            Test result = (Test)mc.Method_ReturnsDynamic<InnerTest>(t, t);
            if (result._field == 10)
                return 0;
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.genclass.genclass036.genclass036
{
    // <Title> Tests generic class generic method used in static method body.</Title>
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
            dynamic mc = new MemberClassWithAnotherTypeConstraint<InnerTest, Test>();
            Test result = (Test)mc.Method_ReturnsU();
            if (result == null)
                return 0;
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.genclass.genclass037.genclass037
{
    // <Title> Tests generic class generic method used in static method body.</Title>
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

        public class InnerTest2 : Test
        {
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic mc = new MemberClassWithAnotherTypeConstraint<InnerTest, Test>();
            InnerTest result = (InnerTest)mc.Method_ReturnsDynamic<InnerTest2>(0, new Test(), new InnerTest2());
            if (result == null)
                return 0;
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.genclass.genclass038.genclass038
{
    // <Title> Tests generic class generic method used in static method body.</Title>
    // <Description>
    // Negative:dynamic type would not satisfy the constraints
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test : C
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic mc = new MemberClassWithUDClassConstraint<C>();
            try
            {
                dynamic result = mc.Method_ReturnsU<dynamic>();
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.GenericConstraintNotSatisfiedRefType, e.Message, "MemberClassWithUDClassConstraint<T>.Method_ReturnsU<U>()", "C", "U", "object"))
                    //    ex.Message.Contains("'MemberClassWithUDClassConstraint<T>.Method_ReturnsU<U>()'"))
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.genclass.genclass041.genclass041
{
    // <Title> Tests generic class generic method used in explicit operator.</Title>
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
                dynamic dy = new MemberClass<InnerTest1>();
                return new InnerTest2()
                {
                    field = (int)dy.Method_ReturnsU<int>(t1.field + 1)
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
            dynamic dy = new MemberClass<InnerTest2>();
            InnerTest2 result1 = (InnerTest2)((InnerTest1)dy.Method_ReturnsDynamic<InnerTest1>(new InnerTest2()
            {
                field = 0
            }

            , new InnerTest1()
            {
                field = 10
            }

            , 0)); //explicit
            return (result1.field == 11) ? 0 : 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.genclass.genclass003a.genclass003a
{
    // <Title> Tests generic class generic method used in implicitly-typed variable initializer.</Title>
    // <Description>
    // Type inference.
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private static dynamic s_mc = new MemberClass<string>();
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            try
            {
                var loc = s_mc.Method_ReturnsU(null);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.CantInferMethTypeArgs, e.Message, "MemberClass<string>.Method_ReturnsU<U>(string)"))
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.genclass.genclass007a.genclass007a
{
    // <Title> Tests generic class generic method used in implicit operator.</Title>
    // <Description>
    // Type inference.
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
                dynamic dy = new MemberClass<InnerTest1>();
                return new InnerTest2()
                {
                    field = dy.Method_ReturnsU(t1.field + 1)
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
            dynamic dy = new MemberClass<InnerTest2>();
            InnerTest2 result1 = dy.Method_ReturnsDynamic(new InnerTest2()
            {
                field = 0
            }

            , new InnerTest1()
            {
                field = 10
            }

            , 0); //implicit
            return (result1.field == 11) ? 0 : 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.genclass.genclass008a.genclass008a
{
    // <Title> Tests generic class generic method used in foreach expression.</Title>
    // <Description>
    // Type inference.
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
            dynamic mc = new MemberClass<string>();
            List<string> list = new List<string>()
            {
            null, string.Empty, "Test"
            }

            ;
            List<string> list2 = new List<string>();
            foreach (string s in mc.Method_ReturnsDynamic(null, 0, list, mc))
            {
                list2.Add(s);
            }

            if (list2.Count == 3 && list2[0] == null && list2[1] == string.Empty && list2[2] == "Test")
            {
                return 0;
            }
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.genclass.genclass010a.genclass010a
{
    // <Title> Tests generic class generic method used in static method body.</Title>
    // <Description>
    // Type inference.
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
            dynamic mc = new MemberClass<string>();
            if (mc.Method_ReturnsT(1.11f, string.Empty, 1L) == null)
                return 0;
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.genclass.genclass011a.genclass011a
{
    // <Title> Tests generic class generic method used in static method body.</Title>
    // <Description>
    // Type inference.
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test : I
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic mc = new MemberClass<int>();
            I i = new Test();
            dynamic d = i;
            if (mc.Method_ReturnsT(d) == 0)
                return 0;
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.genclass.genclass011b.genclass011b
{
    // <Title> Tests generic class generic method used in static method body.</Title>
    // <Description>
    // Type inference.
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test : I
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic mc = new MemberClass<int>();
            dynamic d = new Test();
            if (mc.Method_ReturnsT<I>(d) == 0)
                return 0;
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.genclass.genclass012a.genclass012a
{
    // <Title> Tests generic class generic method used in static method body.</Title>
    // <Description>
    // Type inference
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
            dynamic u = "Test";
            dynamic v = 10;
            dynamic mc = new MemberClass<long>();
            long result = mc.Method_ReturnsT(out u, ref v);
            if (result == 0 && u == null && v == 10)
            {
                return 0;
            }
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.genclass.genclass016a.genclass016a
{
    // <Title> Tests generic class generic method used in static method body.</Title>
    // <Description>
    // Type inference
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(13,9\).*CS0414</Expects>

    public class Test
    {
        private int _field;
        public Test()
        {
            _field = 10;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic mc = new MemberClass<string>();
            Test result = null;
            result = mc.Method_ReturnsUConstraint(result);
            if (result == null)
                return 0;
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.genclass.genclass019a.genclass019a
{
    // <Title> Tests generic class generic method used in static method body.</Title>
    // <Description>
    // Type inference
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
            dynamic mc = new MemberClass<string>();
            dynamic mc2 = "Test2";
            dynamic mc3 = "Test3";
            string result = mc.Method_ReturnsDynamicConstraint(null, mc2, new C(), mc3);
            if (result == "Test2" && mc3 == "Test3")
                return 0;
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.genclass.genclass023a.genclass023a
{
    // <Title> Tests generic class generic method used in static method body.</Title>
    // <Description>
    // Negative: dynamic type would not satisfy the constraints. Type inference
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
            dynamic mc = new MemberClass<string>();
            Test t = new Test();
            try
            {
                string result = mc.Method_ReturnsTNegConstraint(t); // not bind to any method.
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.ValConstraintNotSatisfied, e.Message, "MemberClass<T>.Method_ReturnsTNegConstraint<U>(U)", "U", "Test"))
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.genclass.genclass027a.genclass027a
{
    // <Title> Tests generic class generic method used in static method body.</Title>
    // <Description>Type inference
    //           out/ref need exact match
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
            dynamic mc = new MemberClass<string>.NestedMemberClass<int>();
            int u = 10;
            string v = "Test";
            string result = mc.Method_ReturnsT(out u, ref v);
            if (u == 0 && v == "Test" && result == null)
                return 0;
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.genclass.genclass029a.genclass029a
{
    // <Title> Tests generic class generic method used in static method body.</Title>
    // <Description>
    // Type inference
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
            dynamic mc = new MemberClass<string>.NestedMemberClass<string>();
            dynamic dy = "Me";
            dynamic result = mc.Method_ReturnsDynamic(null, "Test", 10, dy);
            if (result == 10)
                return 0;
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.genclass.genclass035a.genclass035a
{
    // <Title> Tests generic class generic method used in static method body.</Title>
    // <Description>
    // Type inference
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private int _field;
        public Test()
        {
            _field = 10;
        }

        public class InnerTest : Test
        {
            public InnerTest()
            {
                _field = 11;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic mc = new MemberClassWithNewConstraint<Test>();
            Test t = new InnerTest()
            {
                _field = 0
            }

            ;
            Test result = mc.Method_ReturnsDynamic(t, t);
            if (result._field == 10)
                return 0;
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.genclass.genclass037a.genclass037a
{
    // <Title> Tests generic class generic method used in static method body.</Title>
    // <Description>
    // Type inference
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public class InnerTest : Test
        {
        }

        public class InnerTest2 : Test
        {
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic mc = new MemberClassWithAnotherTypeConstraint<InnerTest, Test>();
            InnerTest result = (InnerTest)mc.Method_ReturnsDynamic(0, new Test(), new InnerTest2());
            if (result == null)
                return 0;
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.genclass.genclass041a.genclass041a
{
    // <Title> Tests generic class generic method used in explicit operator.</Title>
    // <Description>
    // Type inference
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
                dynamic dy = new MemberClass<InnerTest1>();
                return new InnerTest2()
                {
                    field = dy.Method_ReturnsU(t1.field + 1)
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
            dynamic dy = new MemberClass<InnerTest2>();
            InnerTest2 result1 = (InnerTest2)((InnerTest1)dy.Method_ReturnsDynamic(new InnerTest2()
            {
                field = 0
            }

            , new InnerTest1()
            {
                field = 10
            }

            , 0)); //explicit
            return (result1.field == 11) ? 0 : 1;
        }
    }
    //</Code>
}
