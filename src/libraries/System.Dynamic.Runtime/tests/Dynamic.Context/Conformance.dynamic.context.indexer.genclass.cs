// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclassregindexer.genclassregindexer
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclassregindexer.genclassregindexer;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass001.genclass001;
    using System;
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
        public static int Status;
        public bool? this[string p1, float p2, short[] p3]
        {
            get
            {
                MemberClass<T>.Status = 1;
                return null;
            }

            set
            {
                MemberClass<T>.Status = 2;
            }
        }

        public byte this[dynamic[] p1, ulong[] p2, dynamic p3]
        {
            get
            {
                MemberClass<T>.Status = 1;
                return (byte)3;
            }

            set
            {
                MemberClass<T>.Status = 2;
            }
        }

        public dynamic this[MyClass p1, char? p2, MyEnum[] p3]
        {
            get
            {
                MemberClass<T>.Status = 1;
                return p1;
            }

            set
            {
                MemberClass<T>.Status = 2;
            }
        }

        public dynamic[] this[MyClass p1, MyStruct? p2, MyEnum[] p3]
        {
            get
            {
                MemberClass<T>.Status = 1;
                return new dynamic[]
                {
                p1, p2
                }

                ;
            }

            set
            {
                MemberClass<T>.Status = 2;
            }
        }

        public double[] this[float p1]
        {
            get
            {
                MemberClass<T>.Status = 1;
                return new double[]
                {
                1.4, double.Epsilon, double.NaN
                }

                ;
            }

            set
            {
                MemberClass<T>.Status = 2;
            }
        }

        public dynamic this[int?[] p1]
        {
            get
            {
                MemberClass<T>.Status = 1;
                return p1;
            }

            set
            {
                MemberClass<T>.Status = 2;
            }
        }

        public MyClass[] this[MyEnum p1]
        {
            get
            {
                MemberClass<T>.Status = 1;
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
                MemberClass<T>.Status = 2;
            }
        }

        public T this[string p1]
        {
            get
            {
                MemberClass<T>.Status = 1;
                return default(T);
            }

            set
            {
                MemberClass<T>.Status = 2;
            }
        }

        public MyClass this[string p1, T p2]
        {
            get
            {
                MemberClass<T>.Status = 1;
                return new MyClass();
            }

            set
            {
                MemberClass<T>.Status = 2;
            }
        }

        public dynamic this[T p1]
        {
            get
            {
                MemberClass<T>.Status = 1;
                return default(T);
            }

            set
            {
                MemberClass<T>.Status = 2;
            }
        }

        public T this[dynamic p1, T p2]
        {
            get
            {
                MemberClass<T>.Status = 1;
                return default(T);
            }

            set
            {
                MemberClass<T>.Status = 2;
            }
        }

        public T this[T p1, short p2, dynamic p3, string p4]
        {
            get
            {
                MemberClass<T>.Status = 1;
                return default(T);
            }

            set
            {
                MemberClass<T>.Status = 2;
            }
        }
    }

    public class MemberClassMultipleParams<T, U, V>
    {
        public static int Status;
        public T this[V v, U u]
        {
            get
            {
                MemberClassMultipleParams<T, U, V>.Status = 1;
                return default(T);
            }

            set
            {
                MemberClassMultipleParams<T, U, V>.Status = 2;
            }
        }
    }

    public class MemberClassWithClassConstraint<T>
        where T : class
    {
        public static int Status;
        public int this[int x]
        {
            get
            {
                MemberClassWithClassConstraint<T>.Status = 3;
                return 1;
            }

            set
            {
                MemberClassWithClassConstraint<T>.Status = 4;
            }
        }

        public T this[decimal dec, dynamic d]
        {
            get
            {
                MemberClassWithClassConstraint<T>.Status = 1;
                return null;
            }

            set
            {
                MemberClassWithClassConstraint<T>.Status = 2;
            }
        }
    }

    public class MemberClassWithNewConstraint<T>
        where T : new()
    {
        public static int Status;
        public dynamic this[T t]
        {
            get
            {
                MemberClassWithNewConstraint<T>.Status = 1;
                return new T();
            }

            set
            {
                MemberClassWithNewConstraint<T>.Status = 2;
            }
        }
    }

    public class MemberClassWithAnotherTypeConstraint<T, U>
        where T : U
    {
        public static int Status;
        public U this[dynamic d]
        {
            get
            {
                MemberClassWithAnotherTypeConstraint<T, U>.Status = 3;
                return default(U);
            }

            set
            {
                MemberClassWithAnotherTypeConstraint<T, U>.Status = 4;
            }
        }

        public dynamic this[int x, U u, dynamic d]
        {
            get
            {
                MemberClassWithAnotherTypeConstraint<T, U>.Status = 1;
                return default(T);
            }

            set
            {
                MemberClassWithAnotherTypeConstraint<T, U>.Status = 2;
            }
        }
    }

    #region Negative tests - you should not be able to construct this with a dynamic object
    public class C
    {
    }

    public interface I
    {
    }

    public class MemberClassWithUDClassConstraint<T>
        where T : C, new()
    {
        public static int Status;
        public C this[T t]
        {
            get
            {
                MemberClassWithUDClassConstraint<T>.Status = 1;
                return new T();
            }

            set
            {
                MemberClassWithUDClassConstraint<T>.Status = 2;
            }
        }
    }

    public class MemberClassWithStructConstraint<T>
        where T : struct
    {
        public static int Status;
        public dynamic this[int x]
        {
            get
            {
                MemberClassWithStructConstraint<T>.Status = 1;
                return x;
            }

            set
            {
                MemberClassWithStructConstraint<T>.Status = 2;
            }
        }
    }

    public class MemberClassWithInterfaceConstraint<T>
        where T : I
    {
        public static int Status;
        public dynamic this[int x, T v]
        {
            get
            {
                MemberClassWithInterfaceConstraint<T>.Status = 1;
                return default(T);
            }

            set
            {
                MemberClassWithInterfaceConstraint<T>.Status = 2;
            }
        }
    }
    #endregion
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass001.genclass001
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclassregindexer.genclassregindexer;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass001.genclass001;
    // <Title> Tests generic class indexer used in + operator.</Title>
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
            MemberClass<int> mc = new MemberClass<int>();
            dynamic dy = mc;
            string p1 = null;
            int result = dy[string.Empty] + dy[p1] + 1;
            if (result == 1)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass002.genclass002
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclassregindexer.genclassregindexer;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass002.genclass002;
    // <Title> Tests generic class indexer used in implicit operator.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(15,20\).*CS0649</Expects>
    //<Expects Status=warning>\(29,20\).*CS0649</Expects>
    using System;

    public class Test
    {
        public class InnerTest1
        {
            public int field;
            public static implicit operator InnerTest2(InnerTest1 t1)
            {
                MemberClass<InnerTest1> mc = new MemberClass<InnerTest1>();
                dynamic dy = mc;
                InnerTest1 p2 = t1;
                return dy[dy, p2];
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
            InnerTest1 t1 = new InnerTest1();
            InnerTest2 result1 = t1; //implicit
            return (result1 == null && MemberClass<InnerTest1>.Status == 1) ? 0 : 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass003.genclass003
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclassregindexer.genclassregindexer;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass003.genclass003;
    // <Title> Tests generic class indexer used in implicit operator.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System;

    public class Test
    {
        public class InnerTest1
        {
            public byte field;
            public static explicit operator InnerTest1(byte t1)
            {
                MemberClass<int> mc = new MemberClass<int>();
                dynamic dy = mc;
                dynamic[] p1 = null;
                ulong[] p2 = null;
                dynamic p3 = null;
                dy[p1, p2, p3] = (byte)10;
                return new InnerTest1()
                {
                    field = dy[p1, p2, p3]
                }

                ;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            byte b = 1;
            InnerTest1 result = (InnerTest1)b;
            if (result.field == 3 && MemberClass<int>.Status == 1)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass005.genclass005
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclassregindexer.genclassregindexer;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass005.genclass005;
    // <Title> Tests generic class indexer used in using block and using expression.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System;
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
            var mc = new MemberClass<string>();
            dynamic dy = mc;
            var mc2 = new MemberClass<bool>();
            dynamic dy2 = mc2;
            string result = null;
            using (MemoryStream sm = new MemoryStream())
            {
                using (StreamWriter sw = new StreamWriter(sm))
                {
                    sw.Write((string)(dy[string.Empty] ?? "Test"));
                    sw.Flush();
                    sm.Position = 0;
                    using (StreamReader sr = new StreamReader(sm, (bool)dy2[false]))
                    {
                        result = sr.ReadToEnd();
                    }
                }
            }

            if (result == "Test" && MemberClass<string>.Status == 1 && MemberClass<bool>.Status == 1)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass006.genclass006
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclassregindexer.genclassregindexer;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass006.genclass006;
    // <Title> Tests generic class indexer used in the for-condition.</Title>
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
            MemberClassMultipleParams<int, string, Test> mc = new MemberClassMultipleParams<int, string, Test>();
            dynamic dy = mc;
            string u = null;
            Test v = null;
            int index = 10;
            for (int i = 10; i > dy[v, u]; i--)
            {
                index--;
            }

            //
            int ret = M();
            if (index == 0 && MemberClassMultipleParams<int, string, Test>.Status == 1)
                return ret;
            return 1;
        }

        private static int M()
        {
            MemberClassWithClassConstraint<Test> mc = new MemberClassWithClassConstraint<Test>();
            dynamic dy = mc;
            int index = 0;
            for (int i = 0; i < 10; i = i + dy[i])
            {
                dy[i] = i;
                index++;
            }

            if (index == 10 && MemberClassWithClassConstraint<Test>.Status == 3)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass008.genclass008
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclassregindexer.genclassregindexer;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass008.genclass008;
    // <Title> Tests generic class indexer used in the while/do expression.</Title>
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
            MemberClass<int> mc = new MemberClass<int>();
            dynamic dy = mc;
            string p1 = null;
            float p2 = 1.23f;
            short[] p3 = null;
            int index = 0;
            do
            {
                index++;
                if (index == 10)
                    break;
            }
            while (dy[p1, p2, p3] ?? true);
            if (index == 10 && MemberClass<int>.Status == 1)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass009.genclass009
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclassregindexer.genclassregindexer;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass009.genclass009;
    // <Title> Tests generic class indexer used in switch expression.</Title>
    // <Description> Won't fix: no dynamic in switch expression </Description>
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
            MemberClass<int> mc = new MemberClass<int>();
            dynamic dy = mc;
            bool isChecked = false;
            switch ((int)dy["Test"])
            {
                case 0:
                    isChecked = true;
                    break;
                default:
                    break;
            }

            if (isChecked && MemberClass<int>.Status == 1)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass010.genclass010
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclassregindexer.genclassregindexer;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass010.genclass010;
    // <Title> Tests generic class indexer used in switch section statement list.</Title>
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
            MemberClass<int> mc = new MemberClass<int>();
            dynamic dy = mc;
            string a = "Test";
            MyClass p1 = new MyClass()
            {
                Field = 10
            }

            ;
            char? p2 = null;
            MyEnum[] p3 = new MyEnum[]
            {
            MyEnum.Second
            }

            ;
            dynamic result = null;
            switch (a)
            {
                case "Test":
                    dy[p1, p2, p3] = 10;
                    result = dy[p1, p2, p3];
                    break;
                default:
                    break;
            }

            if (((MyClass)result).Field == 10)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass011.genclass011
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclassregindexer.genclassregindexer;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass011.genclass011;
    // <Title> Tests generic class indexer used in switch default section statement list.</Title>
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
            MemberClass<int> mc = new MemberClass<int>();
            dynamic dy = mc;
            string a = "Test1";
            MyClass p1 = new MyClass()
            {
                Field = 10
            }

            ;
            MyStruct? p2 = new MyStruct()
            {
                Number = 11
            }

            ;
            MyEnum[] p3 = new MyEnum[10];
            dynamic[] result = null;
            switch (a)
            {
                case "Test":
                    break;
                default:
                    result = dy[p1, p2, p3];
                    dy[p1, p2, p3] = new dynamic[10];
                    break;
            }

            if (result.Length != 2 && MemberClass<int>.Status != 2)
                return 1;
            if (((MyClass)result[0]).Field == 10 && ((MyStruct)result[1]).Number == 11)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass014.genclass014
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclassregindexer.genclassregindexer;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass014.genclass014;
    // <Title> Tests generic class indexer used in try/catch/finally.</Title>
    // <Description>
    // try/catch/finally that uses an anonymous method and refer two dynamic parameters.
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
            dynamic dy = new MemberClass<int>();
            dynamic result = -1;
            try
            {
                Func<string, dynamic> func = delegate (string x)
                {
                    throw new TimeoutException(dy[x].ToString());
                }

                ;
                result = func("Test");
                return 1;
            }
            catch (TimeoutException e)
            {
                if (e.Message != "0")
                    return 1;
            }
            finally
            {
                result = dy[new int?[3]];
            }

            if (result.Length == 3 && result[0] == null && result[1] == null && result[2] == null && MemberClass<int>.Status == 1)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass015.genclass015
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclassregindexer.genclassregindexer;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass015.genclass015;
    // <Title> Tests generic class indexer used in iterator that calls to a lambda expression.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System;
    using System.Collections;

    public class Test
    {
        private static dynamic s_dy = new MemberClass<string>();
        private static int s_num = 0;

        [Fact(Skip = "870811")]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new Test();
            foreach (MyClass[] me in t.Increment())
            {
                if (MemberClass<string>.Status != 1 || me.Length != 2)
                    return 1;
            }

            return 0;
        }

        public IEnumerable Increment()
        {
            while (s_num++ < 4)
            {
                Func<MyEnum, MyClass[]> func = (MyEnum p1) => s_dy[p1];
                yield return func((MyEnum)s_num);
            }
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass016.genclass016
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclassregindexer.genclassregindexer;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass016.genclass016;
    // <Title> Tests generic class indexer used in object initializer inside a collection initializer.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public class Test
    {
        private string _field = string.Empty;

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic dy = new MemberClassWithClassConstraint<string>();
            decimal dec = 123M;
            List<Test> list = new List<Test>()
            {
                new Test()
                {
                    _field = dy[dec, dy]
                }
            };

            if (list.Count == 1 && list[0]._field == null && MemberClassWithClassConstraint<string>.Status == 1)
                return 0;
            else
                return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass017.genclass017
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclassregindexer.genclassregindexer;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass017.genclass017;
    // <Title> Tests generic class indexer used in anonymous type.</Title>
    // <Description>
    // anonymous type inside a query expression that introduces dynamic variables.
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System;
    using System.Linq;
    using System.Collections;
    using System.Collections.Generic;

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
            List<string> list = new List<string>()
            {
            "0", "4", null, "6", "4", "4", null
            }

            ;
            // string s = "test";
            dynamic dy = new MemberClassWithAnotherTypeConstraint<string, string>();
            dynamic dy2 = new MemberClassWithNewConstraint<Test>();
            Test t = new Test()
            {
                _field = 1
            }

            ;
            var result = list.Where(p => p == dy[dy2]).Select(p => new
            {
                A = dy2[t],
                B = dy[dy2]
            }

            ).ToList();
            if (result.Count == 2 && MemberClassWithAnotherTypeConstraint<string, string>.Status == 3 && MemberClassWithNewConstraint<Test>.Status == 1)
            {
                foreach (var m in result)
                {
                    if (((Test)m.A)._field != 10 || m.B != null)
                    {
                        return 1;
                    }
                }

                return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass018.genclass018
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclassregindexer.genclassregindexer;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass018.genclass018;
    // <Title> Tests generic class indexer used in static generic method body.</Title>
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
            return TestMethod<Test>();
        }

        private static int TestMethod<T>()
        {
            MemberClassWithNewConstraint<MyClass> mc = new MemberClassWithNewConstraint<MyClass>();
            dynamic dy = mc;
            MyClass p1 = null;
            dy[p1] = dy;
            if (MemberClassWithNewConstraint<MyClass>.Status != 2)
                return 1;
            dynamic p2 = dy[p1];
            if (p2.GetType() == typeof(MyClass) && MemberClassWithNewConstraint<MyClass>.Status == 1)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass020.genclass020
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclassregindexer.genclassregindexer;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass020.genclass020;
    // <Title> Tests generic class indexer used in static method body.</Title>
    // <Description>
    // Negative
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System;

    public class Test : C
    {
        private int _field;
        public Test()
        {
            _field = 11;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            MemberClassWithUDClassConstraint<Test> mc = new MemberClassWithUDClassConstraint<Test>();
            dynamic dy = mc;
            Test t = null;
            dy[t] = new C();
            if (MemberClassWithUDClassConstraint<Test>.Status != 2)
                return 1;
            t = new Test()
            {
                _field = 10
            }

            ;
            Test result = (Test)dy[t];
            if (result._field != 11 || MemberClassWithUDClassConstraint<Test>.Status != 1)
                return 1;
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass021.genclass021
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclassregindexer.genclassregindexer;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass021.genclass021;
    // <Title> Tests generic class indexer used in static method body.</Title>
    // <Description>
    // Negative
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
            MemberClassWithStructConstraint<char> mc = new MemberClassWithStructConstraint<char>();
            dynamic dy = mc;
            dy[int.MinValue] = new Test();
            if (MemberClassWithStructConstraint<char>.Status != 2)
                return 1;
            dynamic result = dy[int.MaxValue];
            if (result != int.MaxValue || MemberClassWithStructConstraint<char>.Status != 1)
                return 1;
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass022.genclass022
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclassregindexer.genclassregindexer;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.indexer.genclass.genclass022.genclass022;
    // <Title> Tests generic class indexer used in static method body.</Title>
    // <Description>
    // Negative
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System;

    public class Test : I
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
            MemberClassWithInterfaceConstraint<InnerTest> mc = new MemberClassWithInterfaceConstraint<InnerTest>();
            dynamic dy = mc;
            dy[int.MinValue, new InnerTest()] = new Test();
            if (MemberClassWithInterfaceConstraint<InnerTest>.Status != 2)
                return 1;
            dynamic result = dy[int.MaxValue, null];
            if (result != null || MemberClassWithInterfaceConstraint<InnerTest>.Status != 1)
                return 1;
            return 0;
        }
    }
    //</Code>
}