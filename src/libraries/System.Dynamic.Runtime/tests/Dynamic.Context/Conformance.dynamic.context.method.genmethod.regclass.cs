// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.regclass.regclassgenmeth.regclassgenmeth;
using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.regclass.regclassgenmeth.regclassgenmeth
{
    using System.Collections.Generic;

    public class C
    {
    }

    public interface I
    {
    }

    public class MemberClass
    {
        #region Instance methods
        public T Method_ReturnsT<T>()
        {
            return default(T);
        }

        public T Method_ReturnsT<T>(T t)
        {
            return t;
        }

        public T Method_ReturnsT<T, U>(out T t)
        {
            t = default(T);
            return t;
        }

        public T Method_ReturnsT<T>(ref T t)
        {
            t = default(T);
            return t;
        }

        public T Method_ReturnsT<T>(params T[] t)
        {
            return default(T);
        }

        public int M<T>(T t) where T : IEnumerable<T>
        {
            return 3;
        }

        public T Method_ReturnsT<T, U>(params U[] d)
        {
            return default(T);
        }

        public T Method_ReturnsT<T, U>(float x, T t, U u)
        {
            return default(T);
        }

        public T Method_ReturnsT<T, U>(U u)
        {
            return default(T);
        }

        public T Method_ReturnsT<T, U, V>(out U u, ref V v)
        {
            u = default(U);
            return default(T);
        }

        public int Method_ReturnsInt<T>(T t)
        {
            return 1;
        }

        public string Method_ReturnsString<T, U>(T t, U u)
        {
            return "foo";
        }

        public float Method_ReturnsFloat<T, U, V>(T t, dynamic d, U u)
        {
            return 3.4f;
        }

        public float Method_ReturnsFloat<T, U, V>(T t, dynamic d, U u, ref decimal dec)
        {
            dec = 3m;
            return 3.4f;
        }

        public dynamic Method_ReturnsDynamic<T>(T t)
        {
            return t;
        }

        public dynamic Method_ReturnsDynamic<T, U>(T t, U u, dynamic d)
        {
            return u;
        }

        public dynamic Method_ReturnsDynamic<T, U, V>(T t, U u, V v, dynamic d)
        {
            return v;
        }

        #region Constraints on methods that have a new type parameter
        public U Method_ReturnsUConstraint<U>(U u) where U : class
        {
            return null;
        }

        public U Method_ReturnsUConstraint<T, U>(T t) where U : new()
        {
            return new U();
        }

        public U Method_ReturnsUConstraint<T, U, V>(T t, V v) where U : new() where V : U, new()
        {
            return new V();
        }

        public dynamic Method_ReturnsDynamicConstraint<T, U>(T t, U u, dynamic d) where U : new()
        {
            return new U();
        }

        public dynamic Method_ReturnsDynamicConstraint<T, U, V>(T t, U u, V v, dynamic d) where V : class
        {
            return u;
        }

        public float Method_ReturnsFloatConstraint<T, U, V>(T t, dynamic d, U u, ref decimal dec) where V : U
        {
            dec = 3m;
            return 3.4f;
        }

        public string Method_ReturnsStringConstraint<T, U, V>(T t, dynamic d, U u) where U : class where V : U
        {
            return "";
        }

        //These are negative methods... you should not be able to call them with the dynamic type because the dynamic type would not satisfy the constraints
        //you cannot call this like: Method_ReturnsUConstraint<dynamic>(d); because object does not derive from C
        public U Method_ReturnsUNegConstraint<U>(U u) where U : C
        {
            return null;
        }

        public T Method_ReturnsTNegConstraint<T, U>(U u) where U : struct
        {
            return default(T);
        }

        public float Method_ReturnsFloatNegConstraint<T, U, V>(T t, dynamic d, U u, ref decimal dec) where V : U where U : I
        {
            dec = 3m;
            return 3.4f;
        }

        #endregion
        #region Nested class
        public class NestedMemberClass<U>
        {
            public U Method_ReturnsU(U t)
            {
                return default(U);
            }

            public U Method_ReturnsT(params U[] d)
            {
                return default(U);
            }

            public U Method_ReturnsT<V>(out U u, ref V v)
            {
                u = default(U);
                return default(U);
            }

            public dynamic Method_ReturnsDynamic(U t, U u, dynamic d)
            {
                return u;
            }

            public dynamic Method_ReturnsDynamic<V>(U t, U u, V v, dynamic d)
            {
                return v;
            }

            public decimal Method_ReturnsDecimal<V>(U t, dynamic d, U u)
            {
                return 3.4m;
            }

            public byte Method_ReturnsByte<V>(U t, dynamic d, U u, ref double dec)
            {
                dec = 3;
                return 4;
            }
        }
        #endregion
        #endregion
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.regclass.regclass001.regclass001
{
    // <Title> Tests regular class generic method used in regular method body.</Title>
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
            if (t.TestMethod())
                return 1;
            return 0;
        }

        public bool TestMethod()
        {
            dynamic mc = new MemberClass();
            return (bool)mc.Method_ReturnsT<bool>();
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.regclass.regclass002.regclass002
{
    // <Title> Tests regular class generic method used in variable initializer.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private static dynamic s_mc = new MemberClass();
        private int _loc = (int)s_mc.Method_ReturnsT<int, string>(string.Empty, null, "abc");
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Test t = new Test();
            if (t._loc != 0)
                return 1;
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.regclass.regclass003.regclass003
{
    // <Title> Tests regular class generic method used in indexer body.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public class Test
    {
        private Dictionary<int, string> _dic = new Dictionary<int, string>();
        private MemberClass _mc = new MemberClass();
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            if (TestSet() == 0 && TestGet() == 0)
                return 0;
            else
                return 1;
        }

        private static int TestSet()
        {
            Test t = new Test();
            t[0] = "Test0";
            t[1] = null;
            t[2] = "Test2";
            if (t._dic[0] == "Test0" && t._dic[1] == null && t._dic[2] == "Test2")
                return 0;
            else
                return 1;
        }

        private static int TestGet()
        {
            Test t = new Test();
            t._dic[2] = "Test0";
            t._dic[1] = null;
            t._dic[0] = "Test2";
            if (t[2] == "Test0" && t[1] == null && t[0] == "Test2")
                return 0;
            else
                return 1;
        }

        public string this[int i]
        {
            set
            {
                dynamic dy = _mc;
                _dic.Add((int)dy.Method_ReturnsT(i), value);
            }

            get
            {
                dynamic dy = _mc;
                return _dic[(int)dy.Method_ReturnsT(i)];
            }
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.regclass.regclass004.regclass004
{
    // <Title> Tests regular class generic method used in static constructor.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private static int s_field = 10;
        private static int s_result = -1;
        public Test()
        {
            dynamic dy = new MemberClass();
            string s = "Foo";
            Test.s_result = dy.Method_ReturnsT<int, int, string>(out s_field, ref s);
        }

        
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Test t = new Test();
            if (Test.s_field == 0 && Test.s_result == 0)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.regclass.regclass005.regclass005
{
    // <Title> Tests regular class generic method used in variable named dynamic.</Title>
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
            int i = 1;
            dynamic dynamic = new MemberClass();
            int result = dynamic.Method_ReturnsT<int>(ref i);
            if (i == 0 && result == 0)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.regclass.regclass006.regclass006
{

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
            return t1.TestMethod<long>(1L) == 0 ? 0 : 1;
        }

        public T TestMethod<T>(T t)
        {
            dynamic dy = new MemberClass();
            return (T)dy.Method_ReturnsT<T>(t, t, t);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.regclass.regclass008.regclass008
{
    // <Title> Tests regular class generic method used in method body of method with conditional attribute.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private static int s_result = 0;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Test t1 = new Test();
            t1.TestMethod();
            return s_result;
        }

        [System.Diagnostics.Conditional("c1")]
        public void TestMethod()
        {
            dynamic dy = new MemberClass();
            s_result = (int)dy.Method_ReturnsT<int, string>(null) + 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.regclass.regclass009.regclass009
{
    // <Title> Tests regular class generic method used in arguments to method invocation.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections;
    using System.Collections.Generic;

    public class Test
    {
        public class InnerTest : IEnumerable<InnerTest>
        {
            public int Field;
            #region IEnumerable<InnerTest> Members
            public IEnumerator<InnerTest> GetEnumerator()
            {
                return new InnerTestEnumerator();
            }

            #endregion
            #region IEnumerable Members
            IEnumerator IEnumerable.GetEnumerator()
            {
                return new InnerTestEnumerator();
            }
            #endregion
        }

        public class InnerTestEnumerator : IEnumerator<InnerTest>, IEnumerator
        {
            private InnerTest[] _list = new InnerTest[]
            {
            new InnerTest()
            {
            Field = 0
            }

            , new InnerTest()
            {
            Field = 1
            }

            , null
            }

            ;
            private int _index = -1;
            #region IEnumerator<InnerTest> Members
            public InnerTest Current
            {
                get
                {
                    return _list[_index];
                }
            }

            #endregion
            #region IDisposable Members
            public void Dispose()
            {
                // Empty.
            }

            #endregion
            #region IEnumerator Members
            object IEnumerator.Current
            {
                get
                {
                    return _list[_index];
                }
            }

            public bool MoveNext()
            {
                _index++;
                return _index >= _list.Length ? false : true;
            }

            public void Reset()
            {
                _index = -1;
            }
            #endregion
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Test t = new Test();
            dynamic mc = new MemberClass();
            int result = t.Method((int)mc.M<InnerTest>(new InnerTest()));
            return result == 3 ? 0 : 1;
        }

        public int Method(int value)
        {
            return value;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.regclass.regclass010.regclass010
{
    // <Title> Tests regular class generic method used in implicitly-typed variable initializer.</Title>
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
            dynamic mc = new MemberClass();
            string u = string.Empty;
            string v = string.Empty;
            var result = (int)mc.Method_ReturnsT<int, string, string>(out u, ref v);
            if (result == 0 && u == null && v == string.Empty)
                return 0;
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.regclass.regclass011.regclass011
{
    // <Title> Tests regular class generic method used in implicitly-typed variable initializer.</Title>
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
            dynamic mc = new MemberClass();
            var tc = new
            {
                A1 = (int)mc.Method_ReturnsInt<int>(0),
                A2 = (string)mc.Method_ReturnsString<int, int>(1, 2)
            }

            ;
            if (tc != null && tc.A1 == 1 && tc.A2 == "foo")
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.regclass.regclass012.regclass012
{
    // <Title> Tests regular class generic method used in property-set body.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private float _field;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Test t = new Test();
            t.TestProperty = 1.1f;
            if (t.TestProperty == 3.4f)
                return 0;
            return 1;
        }

        public float TestProperty
        {
            set
            {
                dynamic mc = new MemberClass();
                dynamic mv = value;
                _field = (float)mc.Method_ReturnsFloat<float, string, string>(value, mv, null);
            }

            get
            {
                return _field;
            }
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.regclass.regclass013.regclass013
{
    // <Title> Tests regular class generic method used in constructor.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private float _filed;
        private decimal _dec;
        public Test()
        {
            dynamic mc = new MemberClass();
            _filed = mc.Method_ReturnsFloat<string, long, int>(null, mc, 1L, ref _dec);
        }

        
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Test t = new Test();
            if (t._filed == 3.4f && t._dec == 3M)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.regclass.regclass014.regclass014
{
    // <Title> Tests regular class generic method used in operator.</Title>
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
            dynamic mc = new MemberClass();
            int result = (int)mc.Method_ReturnsDynamic<int>(7) % (int)mc.Method_ReturnsDynamic<int>(5);
            if ((int)mc.Method_ReturnsDynamic<int>(99) != 99)
                return 1;
            if (result == 2)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.regclass.regclass015.regclass015
{
    // <Title> Tests regular class generic method used in null coalescing operator.</Title>
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
            dynamic mc = new MemberClass();
            string result = (string)mc.Method_ReturnsDynamic<int, string>(10, null, mc) ?? string.Empty;
            if (result == string.Empty)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.regclass.regclass016.regclass016
{
    // <Title> Tests regular class generic method used in query expression.</Title>
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
            dynamic mc = new MemberClass();
            string a = "a";
            dynamic da = a;
            var result = list.Where(p => p == (string)mc.Method_ReturnsDynamic<string, int, string>(null, 10, a, da)).ToList();
            if (result.Count == 1 && result[0] == "a")
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.regclass.regclass017.regclass017
{
    // <Title> Tests regular class generic method used in short-circuit boolean expression.</Title>
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
            dynamic dy = new MemberClass.NestedMemberClass<string>();
            int loopCount = 0;
            while ((int)dy.Method_ReturnsDynamic<int>(null, null, loopCount, dy) < 10)
            {
                loopCount++;
            }

            return loopCount - 10;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.regclass.regclass018.regclass018
{
    // <Title> Tests regular class generic method used in destructor.</Title>
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
        private static string s_field;
        public static object locker = new object();
        ~Test()
        {
            lock (locker)
            {
                dynamic mc = new MemberClass.NestedMemberClass<Test>();
                s_field = mc.Method_ReturnsDynamic<string>(null, null, "Test", mc);
            }
        }

        private static int Verify()
        {
            lock (Test.locker)
            {
                if (Test.s_field != "Test")
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
            Test.s_field = "Field";
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



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.regclass.regclass019.regclass019
{
    // <Title> Tests regular class generic method used in static method body.</Title>
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
            dynamic dy = new MemberClass();
            var result = (object)dy.Method_ReturnsUConstraint<Test>(new Test());
            return result == null ? 0 : 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.regclass.regclass020.regclass020
{
    // <Title> Tests regular class generic method used in static method body.</Title>
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
            dynamic dy = new MemberClass();
            var result = (Test)dy.Method_ReturnsUConstraint<int, Test>(4);
            if (result != null && result._field == 10)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.regclass.regclass021.regclass021
{
    // <Title> Tests regular class generic method used in static method body.</Title>
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
            dynamic dy = new MemberClass();
            var result = (Test)dy.Method_ReturnsUConstraint<string, Test, InnerTest>(null, new InnerTest()
            {
                _field = 0
            }

            );
            if (result.GetType() == typeof(InnerTest) && result._field == 11)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.regclass.regclass022.regclass022
{
    // <Title> Tests regular class generic method used in static method body.</Title>
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
            dynamic dy = new MemberClass();
            dynamic result = dy.Method_ReturnsDynamicConstraint<int, Test>(1, new Test()
            {
                _field = 0
            }

            , dy);
            if (((Test)result)._field == 10)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.regclass.regclass023.regclass023
{
    // <Title> Tests regular class generic method used in static method body.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(12,9\).*CS0414</Expects>

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
            dynamic dy = new MemberClass();
            dynamic s = "Test";
            dynamic result = dy.Method_ReturnsDynamicConstraint<string, string, string>((string)s, (string)s, (string)s, s);
            if (((string)result) == "Test")
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.regclass.regclass024.regclass024
{
    // <Title> Tests regular class generic method used in static method body.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(12,9\).*CS0414</Expects>

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

        
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic dy = new MemberClass();
            Test t = new Test();
            dynamic it = new InnerTest();
            decimal dec = 0M;
            float result = (float)dy.Method_ReturnsFloatConstraint<Test, Test, InnerTest>(t, it, t, ref dec);
            if (result == 3.4f && dec == 3M)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.regclass.regclass025.regclass025
{
    // <Title> Tests regular class generic method used in static method body.</Title>
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
            dynamic dy = new MemberClass();
            dynamic t = new Test();
            try
            {
                dynamic result = dy.Method_ReturnsUNegConstraint<Test>(t);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.GenericConstraintNotSatisfiedRefType, e.Message, "MemberClass.Method_ReturnsUNegConstraint<U>(U)", "C", "U", "Test"))
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.regclass.regclass027.regclass027
{
    // <Title> Tests regular class generic method used in static method body.</Title>
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
            dynamic dy = new MemberClass();
            MyTest mt = new MyTest();
            dynamic d = mt;
            decimal dec = 0M;
            try
            {
                float result = (float)dy.Method_ReturnsFloatNegConstraint<Test, dynamic, DerivedMyTest>(null, d, d, ref dec);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (!ErrorVerifier.Verify(ErrorMessageId.GenericConstraintNotSatisfiedRefType, e.Message, "MemberClass.Method_ReturnsFloatNegConstraint<T,U,V>(T, object, U, ref decimal)", "I", "U", "object"))
                    return 1;
            }

            return 0;
        }
    }

    public class MyTest : I
    {
    }

    public class DerivedMyTest : MyTest
    {
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.method.genmethod.regclass.regclass028.regclass028
{
    // <Title> Tests regular class generic method used in static method body.</Title>
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
            dynamic dy = new MemberClass.NestedMemberClass<string>();
            string result = (string)dy.Method_ReturnsU("0");
            if (result == null)
                return 0;
            return 1;
        }
    }
    //</Code>
}
