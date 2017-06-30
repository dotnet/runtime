// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.anonmethod001.anonmethod001
{
    // <Title>Anonymous methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Bar
    {
        public int Foo()
        {
            return 0;
        }
    }

    public class Test
    {
        public delegate int Foo();
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new Bar();
            Foo del = delegate ()
            {
                return (int)d.Foo();
            }

            ;
            return del();
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.anonmethod002.anonmethod002
{
    // <Title>Anonymous methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Bar
    {
        public int Foo(int x)
        {
            return x;
        }
    }

    public class Test
    {
        public delegate int Foo(int x);
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new Bar();
            Foo del = delegate (int x)
            {
                return (int)d.Foo(x);
            }

            ;
            return del(0);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.anonmethod003.anonmethod003
{
    // <Title>Anonymous methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Bar
    {
        public int Foo(int x)
        {
            return x;
        }
    }

    public class Test
    {
        public delegate int Foo(dynamic x);
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new Bar();
            Foo del = delegate (dynamic x)
            {
                return (int)d.Foo(x);
            }

            ;
            return del(0);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.anonmethod004.anonmethod004
{
    // <Title>Anonymous methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using Microsoft.CSharp.RuntimeBinder;

    public class Bar
    {
        public int Foo(int x)
        {
            return x;
        }
    }

    public class Test
    {
        public delegate int Foo(dynamic x);
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new Bar();
            Foo del = delegate (object x)
            {
                return (int)d.Foo(x);
            }

            ;
            int rez;
            try
            {
                rez = del(0);
            }
            catch (RuntimeBinderException)
            {
                return 0;
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.anonmethod004b.anonmethod004b
{
    // <Title>Anonymous methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Bar
    {
        public int Foo(int x)
        {
            return x;
        }
    }

    public class Test
    {
        public delegate int Foo(dynamic x);
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new Bar();
            Foo del = delegate (dynamic x)
            {
                return (int)d.Foo(x);
            }

            ;
            return del(0);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.anonmethod005.anonmethod005
{
    // <Title>Anonymous methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Bar
    {
        public int Foo
        {
            get
            {
                return 0;
            }
        }
    }

    public class Test
    {
        public delegate int Foo();
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new Bar();
            Foo del = delegate ()
            {
                return (int)d.Foo;
            }

            ;
            return del();
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.anonmethod006.anonmethod006
{
    // <Title>Anonymous methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using Microsoft.CSharp.RuntimeBinder;

    public class Bar
    {
        public int Foo(int x)
        {
            return x;
        }
    }

    public delegate int Foo2(object x);
    public delegate int Bar2(dynamic d);
    public class Test
    {
        public delegate int Foo(dynamic x);
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new Bar();
            Foo del = delegate (dynamic x)
            {
                return (int)d.Foo(x);
            }

            ;
            int ret = del(0);
            Foo2 del22 = delegate (dynamic x)
            {
                Bar2 del2 = delegate (object o)
                {
                    return (int)d.Foo(o);
                }

                ;
                return del2(x);
            }

            ;
            try
            {
                ret += del22(0);
            }
            catch (RuntimeBinderException)
            {
                return 0;
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.anonmethod006b.anonmethod006b
{
    // <Title>Anonymous methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Bar
    {
        public int Foo(int x)
        {
            return x;
        }
    }

    public delegate int Foo2(object x);
    public delegate int Bar2(dynamic d);
    public class Test
    {
        public delegate int Foo(dynamic x);
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new Bar();
            Foo del = delegate (dynamic x)
            {
                return (int)d.Foo(x);
            }

            ;
            int ret = del(0);
            Foo2 del22 = delegate (dynamic x)
            {
                Bar2 del2 = delegate (dynamic o)
                {
                    return (int)d.Foo(o);
                }

                ;
                return del2(x);
            }

            ;
            ret += del22(0);
            return 0 == ret ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.anonmethod008.anonmethod008
{
    // <Title>Anonymous methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Bar
    {
        public int Foo(int x)
        {
            return x;
        }
    }

    public class Test
    {
        public delegate int Foo(object x);
        public delegate int Bar2(dynamic d);
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new Bar();
            try
            {
                Foo del = delegate (dynamic x)
                {
                    Bar2 del2 = delegate (object o)
                    {
                        return (int)d.Foo();
                    }

                    ;
                    return del2(x);
                }

                ;
                return del(0);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadArgCount, e.Message, "Foo", "0"))
                    return 0;
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.argument001.argument001
{
    // <Title>Argument to method invocation</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class MyClass
    {
        public void Foo(object o)
        {
            if ((int)o == 3)
                Test.Status = 1;
            else
                Test.Status = 2;
        }
    }

    public class Test
    {
        public static int Status;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            MyClass mc = new MyClass();
            dynamic d = 3;
            mc.Foo(d);
            if (Test.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.argument002.argument002
{
    // <Title>Argument to method invocation</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class MyClass
    {
        public void Foo(dynamic o)
        {
            if ((int)o == 3)
                Test.Status = 1;
            else
                Test.Status = 2;
        }
    }

    public class Test
    {
        public static int Status;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            MyClass mc = new MyClass();
            object d = 3;
            mc.Foo(d);
            if (Test.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.array001.array001
{
    // <Title>Special Array methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new[]
            {
            1, 2, 3
            }

            ;
            int result = 3;
            try
            {
                d.Address(1);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.NoSuchMember, e.Message, "System.Array", "Address"))
                    result--;
            }

            try
            {
                d.Get(1);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.NoSuchMember, e.Message, "System.Array", "Get"))
                    result--;
            }

            try
            {
                d.Set(1);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.NoSuchMember, e.Message, "System.Array", "Set"))
                    result--;
            }

            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.array002.array002
{
    // <Title>Special Array methods</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    public class Program
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d2 = new object();
            d2 = "adfa";
            try
            {
                d2[1] = 4;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.AssgReadonlyProp, e.Message, "string.this[int]"))
                    return 0;
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.array003.array003
{
    // <Title>Indexing in arrays using uints/ulongs</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    public class C
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int test = 0, success = 0;
            dynamic idx;
            var arr = new int[5];
            test++;
            idx = (uint)2;
            arr[idx] = 2;
            if (arr[idx] == 2)
                success++;
            test++;
            idx = (long)1;
            arr[idx] = 3;
            if (arr[idx] == 3)
                success++;
            test++;
            idx = (ulong)3;
            arr[idx] = 4;
            if (arr[idx] == 4)
                success++;
            return test == success ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.array004.array004
{
    // <Title>Indexing in arrays using uints/ulongs</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    public class C
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int test = 0, success = 0;
            dynamic idx;
            dynamic rez;
            test++;
            idx = (uint)2;
            rez = new int[idx];
            if (rez.Length == 2)
                success++;
            test++;
            idx = (long)1;
            rez = new string[idx];
            ;
            if (rez.Length == 1)
                success++;
            test++;
            idx = (ulong)3;
            rez = new decimal[idx];
            if (rez.Length == 3)
                success++;
            return test == success ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.array005.array005
{
    // <Title>Array indexers</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Program
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int rez = 0;
            dynamic d1;
            //test long
            d1 = 0L;
            rez += Program.Test(d1);
            //short
            short? s = 0;
            d1 = s;
            rez += Program.Test(d1);
            //byte
            byte b = 0;
            d1 = b;
            rez += Program.Test(d1);
            //char
            char c = (char)0;
            d1 = c;
            rez += Program.Test(d1);
            return rez;
        }

        public static int Test(dynamic d1)
        {
            int tests = 0, success = 0;
            dynamic arr = new int[]
            {
            100, 200, 300, 400, 500, 600
            }

            ;
            tests++;
            if (arr[d1] == 100)
                success++;
            tests++;
            if (arr[d1 + 4] == 500)
                success++;
            tests++;
            arr[d1] = 44;
            if (arr[d1] == 44)
                success++;
            tests++;
            arr[d1 + 5L] = 55;
            if (arr[d1 + 5] == 55)
                success++;
            tests++;
            arr = new int[3][];
            arr[0] = new int[]
            {
            1, 2, 3, 4, 5
            }

            ;
            if (arr[d1][d1 + (short)2] == 3)
                success++;
            tests++;
            arr = new int[2, 2]
            {
            {
            1, 2
            }

            , {
            3, 4
            }
            }

            ;
            if (arr[d1 + 0L, d1] == 1)
                success++;
            return tests - success; //should be 0 in case of success
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.array006.array006
{
    // <Title>Array indexers</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Program
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            //test ulong
            dynamic d1;
            int rez = 0;
            d1 = 0UL;
            rez += Program.Test(d1);
            return rez;
        }

        public static int Test(dynamic d1)
        {
            int tests = 0, success = 0;
            dynamic arr = new int[]
            {
            100, 200, 300, 400, 500, 600
            }

            ;
            tests++;
            if (arr[d1] == 100)
                success++;
            tests++;
            if (arr[d1 + 4] == 500)
                success++;
            tests++;
            arr[d1] = 44;
            if (arr[d1] == 44)
                success++;
            tests++;
            arr[d1 + 5] = 55;
            if (arr[d1 + 5] == 55)
                success++;
            tests++;
            arr = new int[3][];
            arr[0] = new int[]
            {
            1, 2, 3, 4, 5
            }

            ;
            if (arr[d1][d1 + (byte)2] == 3)
                success++;
            tests++;
            arr = new int[2, 2]
            {
            {
            1, 2
            }

            , {
            3, 4
            }
            }

            ;
            if (arr[d1 + 0, d1] == 1)
                success++;
            return tests - success; //should be 0 in case of success
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.array007.array007
{
    // <Title>Array indexers</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public class Program
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            //test long
            dynamic d1;
            int rez = 0;
            d1 = long.MaxValue;
            dynamic arr = new int[]
            {
            100, 200, 300, 400, 500, 600
            }

            ;
            try
            {
                if (arr[d1] == 100)
                    rez = 1;
            }
            catch (System.OverflowException)
            {
                rez = 0;
            }

            return rez;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.array008.array008
{
    // <Title>Array indexers</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Program
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int rez = 0;
            dynamic d = new Test();
            dynamic idx = 0L;
            try
            {
                d[idx] = 4;
                rez = 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadArgTypes, e.Message, "Test.this[int]"))
                    rez = 0;
                else
                    rez = 1;
            }

            try
            {
                var x = d[idx];
                rez = 1;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadArgTypes, e.Message, "Test.this[int]"))
                    rez = 0;
                else
                    rez = 1;
            }

            return rez;
        }
    }

    public class Test
    {
        public int this[int x]
        {
            get
            {
                return 1;
            }

            set
            {
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.array010.array010
{
    // <Title>Array - dynamic index</Title>
    // <Description> LINQ query mixed with dynamic returns incorrect result
    //      compiler seems to reuse the for-loop local iterator variable and its value in nest foreach loop if it's dynamic
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
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
            bool ret = true;
            var ary = new[]
            {
            1, 2, 3
            }

            ;
            for (int i = 0; i < ary.Length; i++)
            {
                ret &= (i == ary[(dynamic)i] - 1);
            }

            for (dynamic i = ary[0]; i <= ary.Length; i++)
            {
                ret &= (i == ary[i - 1]);
            }

            var dx = new Test();
            int id = 0;
            foreach (dynamic it in Iter())
            {
                ret &= (ary[id++] == ary[dx - it]);
            }

            var slist = new[]
            {
            "AAA", "BB", "C"
            }

            ;
            dynamic idx = -1;
            id = 0;
            while (++idx < slist.Length)
            {
                ret &= slist[id++] == slist[idx];
            }

            // works before fix
            var vlist = new[]
            {
            3, 2, 1
            }

            ;
            var list2 = new Dictionary<int, string>();
            list2.Add(2, "AAA");
            list2.Add(3, "BB");
            list2.Add(1, "C");
            id = 3;
            for (dynamic a = 3; a > 0; a--)
            {
                ret &= list2[a] == list2[id--];
            }

            ret &= new Test().Bug816133();
            return ret ? 0 : 1;
        }

        private static IEnumerable Iter()
        {
            yield return byte.MaxValue;
            yield return byte.MaxValue - 1;
            yield break;
        }

        public static implicit operator ushort (Test t)
        {
            return byte.MaxValue;
        }

        /// <summary>
        /// Expected: 1 2 1 2
        /// Actual:   2 0 1 2
        /// </summary>
        private bool Bug816133()
        {
            bool ret = true;
            int[] ary = new int[2];
            for (dynamic i = 1; i <= 2; i++)
                ary[i - 1] = i;
            int count = 1;
            foreach (int i in new Test().Iter512(ary))
            {
                ret &= count++ == i;
            }

            return ret;
        }

        // public method
        public IEnumerable Iter512(int[] t)
        {
            int index = 0;
            yield return t[index++];
            yield return t[index++];
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.arrayinit001.arrayinit001
{
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic[] d = new dynamic[]
            {
            1, null, "Foo"
            }

            ;
            if ((int)d[0] != 1)
                return 1;
            if (d[1] != null)
                return 1;
            if ((string)d[2] != "Foo")
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.arrayinit002.arrayinit002
{
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            object[] o = new object[]
            {
            1, null, "Foo"
            }

            ;
            dynamic[] d = o;
            if ((int)d[0] != 1)
                return 1;
            if (d[1] != null)
                return 1;
            if ((string)d[2] != "Foo")
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.arrayinit003.arrayinit003
{
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            object o = 3;
            dynamic[] d = new dynamic[]
            {
            o
            }

            ;
            if ((int)d[0] != 3)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.arrayinit004.arrayinit004
{
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            object o = 3;
            dynamic[] d = new object[]
            {
            o
            }

            ;
            if ((int)d[0] != 3)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.arrayinit005.arrayinit005
{
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            object o = 3;
            object[] d = new dynamic[]
            {
            o
            }

            ;
            if ((int)d[0] != 3)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.arrayinit006.arrayinit006
{
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            object o = 3;
            dynamic d = 4;
            object[] od = new dynamic[]
            {
            o, d
            }

            ;
            if ((int)od[0] != 3)
                return 1;
            if ((int)od[1] != 4)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.arrayinit007.arrayinit007
{
    // <Title>Array initializers</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class MainClass
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic attrs = new object[]
            {
            1, 2
            }

            ;
            dynamic dd = 1;
            var a = attrs[dd];
            if (a == 2)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.boxing001.boxing001
{
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            //int
            object o = int.MinValue;
            dynamic d = o;
            int i = (int)d;
            if (i != int.MinValue)
                return 1;
            //short
            o = short.MaxValue;
            d = o;
            short s = (short)d;
            if (s != short.MaxValue)
                return 1;
            //sbyte
            o = sbyte.MaxValue;
            d = o;
            sbyte sb = (sbyte)d;
            if (sb != sbyte.MaxValue)
                return 1;
            //long
            o = long.MaxValue;
            d = o;
            long l = (long)d;
            if (l != long.MaxValue)
                return 1;
            //byte
            o = byte.MaxValue;
            d = o;
            byte b = (byte)d;
            if (b != byte.MaxValue)
                return 1;
            //ushort
            o = ushort.MaxValue;
            d = o;
            ushort us = (ushort)d;
            if (us != ushort.MaxValue)
                return 1;
            //ulong
            o = ulong.MaxValue;
            d = o;
            ulong ul = (ulong)d;
            if (ul != ulong.MaxValue)
                return 1;
            //uint
            o = uint.MinValue;
            d = o;
            uint ui = (uint)o;
            if (ui != uint.MinValue)
                return 1;
            //char
            o = 'a';
            d = o;
            char c = (char)d;
            if (c != 'a')
                return 1;
            //float
            o = float.Epsilon;
            d = o;
            float f = (float)d;
            if (f != float.Epsilon)
                return 1;
            o = float.NegativeInfinity;
            d = o;
            f = (float)d;
            if (f != float.NegativeInfinity)
                return 1;
            //double
            o = double.MaxValue;
            d = o;
            double dd = (double)d;
            if (dd != double.MaxValue)
                return 1;
            //decimal
            o = decimal.MaxValue;
            d = o;
            decimal dc = (decimal)d;
            if (dc != decimal.MaxValue)
                return 1;
            //bool
            o = true;
            d = o;
            bool bo = (bool)d;
            if (bo != true)
                return 1;
            //UD enum
            o = myEnum.One;
            d = o;
            myEnum e = (myEnum)d;
            if (e != myEnum.One)
                return 1;
            //UD struct
            o = new myStruct()
            {
                field = 1
            }

            ;
            d = o;
            myStruct st = (myStruct)d;
            if (st.field != 1)
                return 1;
            return 0;
        }

        public enum myEnum
        {
            One
        }

        public struct myStruct
        {
            public int field;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.boxing002.boxing002
{
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            object o = null;
            dynamic d = null;
            //int
            d = int.MinValue;
            o = d;
            int i = (int)o;
            if (i != int.MinValue)
                return 1;
            //short
            d = short.MaxValue;
            o = d;
            short s = (short)o;
            if (s != short.MaxValue)
                return 1;
            //sbyte
            d = sbyte.MaxValue;
            o = d;
            sbyte sb = (sbyte)o;
            if (sb != sbyte.MaxValue)
                return 1;
            //long
            d = long.MaxValue;
            o = d;
            long l = (long)o;
            if (l != long.MaxValue)
                return 1;
            //byte
            d = byte.MaxValue;
            o = d;
            byte b = (byte)o;
            if (b != byte.MaxValue)
                return 1;
            //ushort
            d = ushort.MaxValue;
            o = d;
            ushort us = (ushort)o;
            if (us != ushort.MaxValue)
                return 1;
            //ulong
            d = ulong.MaxValue;
            o = d;
            ulong ul = (ulong)o;
            if (ul != ulong.MaxValue)
                return 1;
            //uint
            d = uint.MinValue;
            o = d;
            uint ui = (uint)o;
            if (ui != uint.MinValue)
                return 1;
            //char
            d = 'a';
            o = d;
            char c = (char)o;
            if (c != 'a')
                return 1;
            //float
            d = float.Epsilon;
            o = d;
            float f = (float)o;
            if (f != float.Epsilon)
                return 1;
            d = float.NegativeInfinity;
            o = d;
            f = (float)o;
            if (!float.IsNegativeInfinity(f))
                return 1;
            //double
            d = double.MaxValue;
            o = d;
            double dd = (double)o;
            if (dd != double.MaxValue)
                return 1;
            //decimal
            d = decimal.MaxValue;
            o = d;
            decimal dc = (decimal)o;
            if (dc != decimal.MaxValue)
                return 1;
            //bool
            d = true;
            o = d;
            bool bo = (bool)o;
            if (bo != true)
                return 1;
            //UD enum
            d = myEnum.One;
            o = d;
            myEnum e = (myEnum)o;
            if (e != myEnum.One)
                return 1;
            //UD struct
            d = new myStruct()
            {
                field = 1
            }

            ;
            o = d;
            myStruct st = (myStruct)o;
            if (st.field != 1)
                return 1;
            return 0;
        }

        public enum myEnum
        {
            One
        }

        public struct myStruct
        {
            public int field;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.boxing003.boxing003
{
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            //int
            object o = int.MinValue;
            dynamic d = o;
            if (!(d is int))
                return 1;
            //short
            o = short.MaxValue;
            d = o;
            if (!(d is short))
                return 1;
            //sbyte
            o = sbyte.MaxValue;
            d = o;
            if (!(d is sbyte))
                return 1;
            //long
            o = long.MaxValue;
            d = o;
            if (!(d is long))
                return 1;
            //byte
            o = byte.MaxValue;
            d = o;
            if (!(d is byte))
                return 1;
            //ushort
            o = ushort.MaxValue;
            d = o;
            if (!(d is ushort))
                return 1;
            //ulong
            o = ulong.MaxValue;
            d = o;
            if (!(d is ulong))
                return 1;
            //uint
            o = uint.MinValue;
            d = o;
            if (!(d is uint))
                return 1;
            //char
            o = 'a';
            d = o;
            if (!(d is char))
                return 1;
            //float
            o = float.Epsilon;
            d = o;
            if (!(d is float))
                return 1;
            //double
            o = double.MaxValue;
            d = o;
            if (!(d is double))
                return 1;
            //decimal
            o = decimal.MaxValue;
            d = o;
            if (!(d is decimal))
                return 1;
            //bool
            o = true;
            d = o;
            if (!(d is bool))
                return 1;
            //UD enum
            o = myEnum.One;
            d = o;
            if (!(d is myEnum))
                return 1;
            //UD struct
            o = new myStruct()
            {
                field = 1
            }

            ;
            d = o;
            if (!(d is myStruct))
                return 1;
            return 0;
        }

        public enum myEnum
        {
            One
        }

        public struct myStruct
        {
            public int field;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.boxing004.boxing004
{
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = null;
            //int
            d = int.MinValue;
            int i = (int)d;
            if (i != int.MinValue)
                return 1;
            //short
            d = short.MaxValue;
            short s = (short)d;
            if (s != short.MaxValue)
                return 1;
            //sbyte
            d = sbyte.MaxValue;
            sbyte sb = (sbyte)d;
            if (sb != sbyte.MaxValue)
                return 1;
            //long
            d = long.MaxValue;
            long l = (long)d;
            if (l != long.MaxValue)
                return 1;
            //byte
            d = byte.MaxValue;
            byte b = (byte)d;
            if (b != byte.MaxValue)
                return 1;
            //ushort
            d = ushort.MaxValue;
            ushort us = (ushort)d;
            if (us != ushort.MaxValue)
                return 1;
            //ulong
            d = ulong.MaxValue;
            ulong ul = (ulong)d;
            if (ul != ulong.MaxValue)
                return 1;
            //uint
            d = uint.MinValue;
            uint ui = (uint)d;
            if (ui != uint.MinValue)
                return 1;
            //char
            d = 'a';
            char c = (char)d;
            if (c != 'a')
                return 1;
            //float
            d = float.Epsilon;
            float f = (float)d;
            if (f != float.Epsilon)
                return 1;
            d = float.NegativeInfinity;
            f = (float)d;
            if (!float.IsNegativeInfinity(f))
                return 1;
            //double
            d = double.MaxValue;
            double dd = (double)d;
            if (dd != double.MaxValue)
                return 1;
            //decimal
            d = decimal.MaxValue;
            decimal dc = (decimal)d;
            if (dc != decimal.MaxValue)
                return 1;
            //bool
            d = true;
            bool bo = (bool)d;
            if (bo != true)
                return 1;
            //UD enum
            d = myEnum.One;
            myEnum e = (myEnum)d;
            if (e != myEnum.One)
                return 1;
            //UD struct
            d = new myStruct()
            {
                field = 1
            }

            ;
            myStruct st = (myStruct)d;
            if (st.field != 1)
                return 1;
            return 0;
        }

        public enum myEnum
        {
            One
        }

        public struct myStruct
        {
            public int field;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.boxing005.boxing005
{
    // <Title>Boxing and unboxing</Title>
    // <Description>
    // Boxing to an object and unboxing from a dynamic
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public struct myStruct
    {
        public void Foo()
        {
            Test.Status = 1;
        }
    }

    public class Test
    {
        public static int Status;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new myStruct();
            d.Foo();
            if (Test.Status != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.boxing006.boxing006
{
    // <Title>Boxing and unboxing</Title>
    // <Description>
    // Boxing to an object and unboxing from a dynamic
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public struct Value
    {
        public int X;
        public void MutateX(int x)
        {
            this.X = x;
        }
    }

    public class Program
    {
        
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Value s = default(Value);
            dynamic d = 5;
            s.MutateX(d);
            if (s.X != 5)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.cast002.cast002
{
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            int i1 = 10;
            int i2 = (dynamic)(-10);
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.collectioninit001.collectioninit001
{
    // <Title>Collection initializers</Title>
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
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            List<dynamic> myList = new List<dynamic>()
            {
            1, 2, "Fooo", long.MaxValue
            }

            ;
            if (myList.Count != 4)
                return 1;
            if ((int)myList[0] != 1)
                return 1;
            if ((int)myList[1] != 2)
                return 1;
            if ((string)myList[2] != "Fooo")
                return 1;
            if ((long)myList[3] != long.MaxValue)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.collectioninit002.collectioninit002
{
    // <Title>Collection initializers</Title>
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
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            object o = 2;
            dynamic d = o;
            List<dynamic> myList = new List<dynamic>()
            {
            o, d
            }

            ;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.collectioninit003.collectioninit003
{
    // <Title>Collection initializers</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public class Test
    {
        
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            List<dynamic> myList = new List<dynamic>()
            {
            new
            {
            Name = "Foo", Value = 3
            }
            }

            ;
            if ((int)myList[0].Value != 3)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.collectioninit004.collectioninit004
{
    // <Title>Collection initializers</Title>
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
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            List<dynamic> myList = new List<object>()
            {
            3
            }

            ;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.collectioninit005.collectioninit005
{
    // <Title>Collection initializers</Title>
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
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            List<object> myList = new List<dynamic>()
            {
            3
            }

            ;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.collectioninit006.collectioninit006
{
    // <Title>Collection initializers</Title>
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
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = 3;
            List<object> myList = new List<object>()
            {
            d
            }

            ;
            if ((int)myList[0] != 3)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.collectioninit007.collectioninit007
{
    // <Title>Collection initializers</Title>
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
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            object o = 3;
            List<dynamic> myList = new List<dynamic>()
            {
            o
            }

            ;
            if ((int)myList[0] != 3)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.collectioninit008.collectioninit008
{
    // <Title>Collection initializers</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;
    using System.Text;

    public class A
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic a = new A
            {
                S =
            {
            Length = 10
            }

            ,
                X =
            {
            1, 2, 3
            }
            }

            ;
            return 0;
        }

        public dynamic S = new StringBuilder();
        public dynamic X = new List<int>();
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.collectioninit009.collectioninit009
{
    // <Title>Collection initializers</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(15,9\).*CS0219</Expects>
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
            //array initializer
            dynamic d = 3;
            int rez = 0;
            Collect c = new Collect()
            {
            1, 3L, "goo", d
            }

            ;
            if (Collect.Status != 8)
                return 1;
            return 0;
        }
    }

    public class Collect : System.Collections.IEnumerable
    {
        public static int Status;
        public void Add(int x)
        {
            Collect.Status += 1;
        }

        public void Add(long x)
        {
            Collect.Status += 2;
        }

        public void Add(string x)
        {
            Collect.Status += 4;
        }

        public void Add(dynamic x)
        {
            Collect.Status += 8;
        }

        public System.Collections.IEnumerator GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.dlgate001.dlgate001
{
    public class Test
    {
        public delegate void Foo(object o);
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            Foo f = delegate (dynamic d)
            {
            }

            ;
            f(3);
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.dlgate002.dlgate002
{
    public class Test
    {
        public delegate void Foo(dynamic o);
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            Foo f = delegate (object d)
            {
            }

            ;
            f(2);
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.dlgate003.dlgate003
{
    // <Title>Delegates</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Bar
    {
        public int Foo()
        {
            return 0;
        }
    }

    public class Test
    {
        public delegate dynamic Foo();
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            Foo f = delegate ()
            {
                return new Bar();
            }

            ;
            return f().Foo();
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.dlgate004.dlgate004
{
    // <Title>Delegates</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Bar
    {
        public int Foo()
        {
            return 0;
        }
    }

    public class Test
    {
        public delegate dynamic Foo();
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            Foo f = delegate ()
            {
                return new Bar();
            }

            ;
            return (int)f().Foo();
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.dlgate005.dlgate005
{
    // <Title>Delegates</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Bar
    {
        public int Foo()
        {
            return 0;
        }
    }

    public class Test
    {
        public delegate void Foo(dynamic o);
        public static event Foo myEvent;
        private static int s_status;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            myEvent += myFoo;
            myEvent(new Bar());
            if (Test.s_status == 0)
                return 0;
            else
                return 1;
        }

        public static void myFoo(dynamic d)
        {
            Test.s_status = (int)d.Foo();
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.dlgate007.dlgate007
{
    // <Title>Delegates</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class Bar
    {
        public int Foo()
        {
            return 0;
        }
    }

    public class Test
    {
        public delegate void Foo(object o);
        public static event Foo myEvent;
        private static int s_status;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            myEvent += myFoo;
            myEvent(new Bar());
            if (Test.s_status == 0)
                return 0;
            else
                return 1;
        }

        public static void myFoo(dynamic d)
        {
            Test.s_status = (int)d.Foo();
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.dlgate008lib.dlgate008lib
{
    public class Test
    {
        public void M1(ref int p1)
        {
        }

        
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic dobj = new Test();
            int i1 = 10;
            int i2 = 20;
            dobj.M1(ref i1);
            dobj.M1(ref i2);
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.evnt001.evnt001
{
    // <Title>Delegates</Title>
    // <Description></Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public delegate void MyEventHandler1(object sender, EventArgs e);
    public delegate void MyEventHandler2(dynamic sender);
    public delegate void MyEventHandler3(dynamic d1, dynamic d2, EventArgs e);
    public class MyEvent
    {
        public event MyEventHandler1 myEvent1;
        internal event MyEventHandler2 myEvent2;
        public event MyEventHandler3 myEvent3;
        public void Fire1(EventArgs e)
        {
            if (myEvent1 != null)
                myEvent1(this, e);
        }

        internal void Fire2(EventArgs e)
        {
            if (myEvent2 != null)
                myEvent2(this);
        }

        internal void Fire3(EventArgs e)
        {
            if (myEvent3 != null)
                myEvent3(this, null, e);
        }
    }

    public class Test
    {
        private int _result = -1;
        public void EventReceiver1(dynamic sender, EventArgs e)
        {
            _result = 1;
        }

        private void EventReceiver2(object sender)
        {
            _result = 2;
        }

        internal void EventReceiver3(object d1, dynamic d2, EventArgs e)
        {
            _result = 3;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            // Console.ForegroundColor = ConsoleColor.White;
            // Console.BackgroundColor = ConsoleColor.DarkRed;
            Test t = new Test();
            return t.RunTest() ? 0 : 1;
        }

        public bool RunTest()
        {
            bool ret = true;
            MyEvent me = new MyEvent();
            // myEvent1 +1
            me.myEvent1 += new MyEventHandler1(EventReceiver1);
            me.Fire1(new EventArgs());
            ret = (1 == _result);
            // myEvent1 +1 => 2
            me.myEvent1 += new MyEventHandler1(EventReceiver1);
            me.Fire1(new EventArgs());
            ret = (1 == _result);
            // myEvent1 -1 => 1
            me.myEvent1 -= new MyEventHandler1(EventReceiver1);
            me.Fire1(new EventArgs());
            ret = (1 == _result);
            // myEvent1 -1 => 0
            _result = 0;
            me.myEvent1 -= new MyEventHandler1(EventReceiver1);
            me.Fire1(new EventArgs());
            ret = (0 == _result);
            me.myEvent2 += new MyEventHandler2(EventReceiver2);
            me.myEvent2 += new MyEventHandler2(EventReceiver2);
            me.myEvent2 += new MyEventHandler2(EventReceiver2);
            me.Fire2(new EventArgs());
            ret &= (2 == _result);
            me.myEvent2 -= new MyEventHandler2(EventReceiver2);
            me.myEvent2 -= new MyEventHandler2(EventReceiver2);
            me.Fire2(new EventArgs());
            ret &= (2 == _result);
            me.myEvent3 += new MyEventHandler3(EventReceiver3);
            me.Fire3(new EventArgs());
            ret &= (3 == _result);
            _result = 0;
            me.myEvent3 -= new MyEventHandler3(EventReceiver3);
            me.Fire3(new EventArgs());
            ret &= (0 == _result);
            return ret;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.evnt002.evnt002
{
    // <Title>Delegates</Title>
    // <Description></Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public delegate void MyEventHandler1(dynamic sender, EventArgs e);
    public delegate void MyEventHandler2(dynamic sender);
    public delegate void MyEventHandler3(object d1, dynamic d2, EventArgs e);
    abstract public class MyEvent1
    {
        abstract internal event MyEventHandler1 myEvent1;
        virtual internal event MyEventHandler2 myEvent2;
        public event MyEventHandler3 myEvent3;
        internal void Fire2(EventArgs e)
        {
            if (myEvent2 != null)
                myEvent2(this);
        }

        internal void Fire3(EventArgs e)
        {
            if (myEvent3 != null)
                myEvent3(this, null, e);
        }
    }

    public class MyEvent2 : MyEvent1
    {
        override internal event MyEventHandler1 myEvent1;
        new internal event MyEventHandler2 myEvent2;
        // public event MyEventHandler3 myEvent3;
        public void Fire1(EventArgs e)
        {
            if (myEvent1 != null)
                myEvent1(this, e);
        }

        new internal void Fire2(EventArgs e)
        {
            if (myEvent2 != null)
                myEvent2(this);
        }
    }

    public class Test
    {
        private int _result = -1;
        public void EventReceiver1(object sender, EventArgs e)
        {
            _result = 1;
        }

        private void EventReceiver21(dynamic sender)
        {
            _result = 21;
        }

        private void EventReceiver22(object sender)
        {
            _result = 22;
        }

        internal void EventReceiver3(dynamic d1, dynamic d2, EventArgs e)
        {
            _result = 3;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            // Console.ForegroundColor = ConsoleColor.White;
            // Console.BackgroundColor = ConsoleColor.DarkRed;
            Test t = new Test();
            return t.RunTest() ? 0 : 1;
        }

        public bool RunTest()
        {
            bool ret = true;
            MyEvent1 me1 = new MyEvent2();
            MyEvent2 me = new MyEvent2();
            // myEvent1 +1
            me.myEvent1 += new MyEventHandler1(EventReceiver1);
            me.Fire1(new EventArgs());
            ret = (1 == _result);
            // myEvent1 +1 => 2
            _result = 0;
            me.myEvent1 += new MyEventHandler1(EventReceiver1);
            me.Fire1(new EventArgs());
            ret = (1 == _result);
            // myEvent1 -1 => 1
            _result = 0;
            me.myEvent1 -= new MyEventHandler1(EventReceiver1);
            me.Fire1(new EventArgs());
            ret = (1 == _result);
            // myEvent1 -1 => 0
            _result = 0;
            me.myEvent1 -= new MyEventHandler1(EventReceiver1);
            me.Fire1(new EventArgs());
            ret = (0 == _result);
            me.myEvent2 += new MyEventHandler2(EventReceiver21);
            me.myEvent2 += new MyEventHandler2(EventReceiver21);
            me.myEvent2 += new MyEventHandler2(EventReceiver21);
            me.Fire2(new EventArgs());
            ret &= (21 == _result);
            _result = 0;
            me.myEvent2 -= new MyEventHandler2(EventReceiver21);
            me.myEvent2 -= new MyEventHandler2(EventReceiver21);
            me.Fire2(new EventArgs());
            ret &= (21 == _result);
            me1.myEvent2 += new MyEventHandler2(EventReceiver22);
            me1.myEvent2 += new MyEventHandler2(EventReceiver22);
            me1.Fire2(new EventArgs());
            ret &= (22 == _result);
            _result = 0;
            me1.myEvent2 -= new MyEventHandler2(EventReceiver22);
            me1.myEvent2 -= new MyEventHandler2(EventReceiver22);
            me1.Fire2(new EventArgs());
            ret &= (0 == _result);
            me.myEvent3 += new MyEventHandler3(EventReceiver3);
            me.Fire3(new EventArgs());
            ret &= (3 == _result);
            _result = 0;
            me.myEvent3 -= new MyEventHandler3(EventReceiver3);
            me.Fire3(new EventArgs());
            ret &= (0 == _result);
            return ret;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.evnt003.evnt003
{
    // <Title>Delegates</Title>
    // <Description></Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=warning>\(12,31\).*CS0067</Expects>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public class A
    {
        public event EventHandler E;
        public static void EventH(object sender, EventArgs e)
        {
        }

        
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic x = new A();
            x.E += new EventHandler(EventH);
            try
            {
                x.E.ToString();
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e) // Should Not Ex
            {
                return 1;
            }

            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.evnt004.evnt004
{
    // <Title>Delegates</Title>
    // <Description></Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=warning>\(12,31\).*CS0067</Expects>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public class A
    {
        public event EventHandler E;

        
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic x = new A();
            // what to expect:
            // We should disallow access to events as properties under all circumstances.
            // Then, the rule is simple: dynamic dispatch on a dynamic argument never resolves to a private member.
            try
            {
                x.E();
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(RuntimeErrorId.NullReferenceOnMemberException, e.Message))
                    return 0;
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.evnt006.evnt006
{
    // <Title>Delegates</Title>
    // <Description>Extended scenarios - event should not resolved as property </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=warning>\(22,33\).*CS0067</Expects>
    //<Expects Status=warning>\(23,34\).*CS0067</Expects>
    //<Expects Status=warning>\(24,38\).*CS0067</Expects>
    //<Expects Status=warning>\(25,39\).*CS0067</Expects>
    //<Expects Status=warning>\(26,42\).*CS0067</Expects>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    abstract public class A1
    {
        internal abstract event EventHandler E5;
    }

    public class A2 : A1
    {
        internal event EventHandler E1;
        protected event EventHandler E2;
        public static event EventHandler E3;
        public virtual event EventHandler E4;
        internal override event EventHandler E5;
        private event EventHandler E6;
        /// <summary>
        /// not related to prop, just more negative scenarios
        /// </summary>
        /// <returns></returns>
        public bool M1()
        {
            try
            {
                E6.ToString();
            }
            catch (System.NullReferenceException)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// not related to prop, just more negative scenarios
        /// </summary>
        /// <returns></returns>
        public bool M2()
        {
            try
            {
                E6(null, null);
            }
            catch (System.NullReferenceException)
            {
                return true;
            }

            return false;
        }

        
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new A2();
            bool ret = d.M1();
            ret &= d.M2();
            for (int i = 1; i < 7; i++)
            {
                ret &= CallEvent(d, i);
            }

            d = new A3();
            ret &= d.M3();
            for (int i = 1; i < 7; i++)
            {
                ret &= CallEvent(d, i);
            }

            return ret ? 0 : 1;
        }

        public static bool CallEvent(dynamic d, int count)
        {
            try
            {
                switch (count)
                {
                    case 1:
                        d.E1();
                        break;
                    case 2:
                        d.E2().ToString();
                        break;
                    case 3:
                        d.E3();
                        break;
                    case 4:
                        d.E4.ToString();
                        break;
                    case 5:
                        d.E5();
                        break;
                    case 6:
                        d.E6.ToString();
                        break;
                }
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                // new errMsg - Delegate 'EventHandler' does not take '0' arguments
                if (ErrorVerifier.Verify(ErrorMessageId.BadDelArgCount, e.Message, "EventHandler", "0") || ErrorVerifier.Verify(ErrorMessageId.ObjectProhibited, e.Message, "A2.E3") || ErrorVerifier.Verify(RuntimeErrorId.NullReferenceOnMemberException, e.Message))
                    return true;
            }

            return false;
        }
    }

    public class A3 : A2
    {
        private new event EventHandler E1;
        public override event EventHandler E4;
        /// <summary>
        /// not related to prop, just more negative scenarios
        /// </summary>
        /// <returns></returns>
        public bool M3()
        {
            bool ret = true;
            try
            {
                E1.ToString();
            }
            catch (System.NullReferenceException)
            {
                goto L1;
            }

            ret = false;
        L1:
            try
            {
                E1(null, null);
            }
            catch (System.NullReferenceException)
            {
                goto L2;
            }

            ret = false;
        L2:
            try
            {
                E4.ToString();
            }
            catch (System.NullReferenceException)
            {
                goto L3;
            }

            ret = false;
        L3:
            try
            {
                E4(null, null);
            }
            catch (System.NullReferenceException)
            {
                return ret;
            }

            return false;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.evnt008.evnt008
{
    // <Title>Event</Title>
    // <Description>Add null to an event</Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(13,23\).*CS0067</Expects>

    public class X
    {
        public delegate void MyEvent();
        public event MyEvent XX;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic x = new X();
            x.XX += null;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.explicit001.explicit001
{
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            object o = null;
            dynamic d = null;
            int x = int.MaxValue;
            o = (dynamic)x;
            if ((int)o != int.MaxValue)
                return 1;
            d = (object)x;
            if ((int)d != int.MaxValue)
                return 1;
            myClass c = new myClass()
            {
                Field = 2
            }

            ;
            o = (dynamic)c;
            if (((myClass)o).Field != 2)
                return 1;
            d = (object)c;
            if (((myClass)d).Field != 2)
                return 1;
            return 0;
        }

        public class myClass
        {
            public int Field;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.explicit002.explicit002
{
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = (dynamic)(object)(int)1;
            if ((int)d != 1)
                return 1;
            d = (object)(dynamic)1;
            if ((int)d != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.explicit003.explicit003
{
    // <Title>Boxing and unboxing</Title>
    // <Description>
    // Boxing to an object and unboxing from a dynamic
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class MyClass
    {
        public int Field;
        public static explicit operator MyClass(int x)
        {
            return new MyClass()
            {
                Field = x
            }

            ;
        }

        public static explicit operator int (MyClass x)
        {
            return x.Field;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = (dynamic)(MyClass)(int)1;
            if ((int)d.Field != 1)
                return 1;
            d = (object)(MyClass)1;
            if ((int)d.Field != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.explicit004.explicit004
{
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            object d = (dynamic)(int)1;
            if ((int)d != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.implicit001.implicit001
{
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            object o = "Foo";
            dynamic d = o;
            string s = (string)d;
            if (s != "Foo")
                return 1;
            d = "Foo";
            o = d;
            s = (string)o;
            if (s != "Foo")
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.implicitarayinit001.implicitarayinit001
{
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic dd = 23;
            var d = new[]
            {
            dd, dd, new object ()}

            ;
            if (d.Length == 3 && d[0] == 23 && d[1] == 23)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.implicitarayinit002.implicitarayinit002
{
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic dd = 23;
            var d = new[]
            {
            dd, dd
            }

            ;
            if (d.GetType() != typeof(dynamic[]))
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.implicitarayinit003.implicitarayinit003
{
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            var d = new[]
            {
            (dynamic)2, (dynamic)"Foo"
            }

            ;
            if (d.GetType() != typeof(dynamic[]))
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.implicitarayinit004.implicitarayinit004
{
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            var d = new[]
            {
            (dynamic)new
            {
            Name = "Foo"
            }

            , (dynamic)new
            {
            Address = "Bar"
            }
            }

            ;
            if (d.GetType() != typeof(dynamic[]))
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.initialize002.initialize002
{
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            int x = int.MinValue;
            object o = (dynamic)x;
            dynamic d = 3;
            if ((int)o != x)
                return 1;
            o = d;
            if ((int)o != 3)
                return 1;
            d = new MyClass()
            {
                Field = 3
            }

            ;
            o = d.Field;
            if ((int)o != 3)
                return 1;
            d = new MyClass()
            {
                Field = 3
            }

            ;
            o = d.GetNumber();
            if ((int)o != 3)
                return 1;
            d = new MyClass()
            {
                Field = 3
            }

            ;
            o = ((MyClass)d)[3];
            if ((int)o != 3)
                return 1;
            return 0;
        }

        public class MyClass
        {
            public int Field;
            public int GetNumber()
            {
                return Field;
            }

            public int this[int index]
            {
                get
                {
                    return Field;
                }
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.initialize003.initialize003
{
    public class Test
    {
        private static object s_foo = ((dynamic)new MyClass()
        {
            Field = 3
        }

        ).GetNumber();

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            if ((int)s_foo != 3)
                return 1;
            return 0;
        }

        public class MyClass
        {
            public int Field;
            public int GetNumber()
            {
                return Field;
            }

            public int this[int index]
            {
                get
                {
                    return Field;
                }
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.initialize004.initialize004
{
    public class Test
    {
        private static object s_foo = ((dynamic)new MyClass()
        {
            Field = 3
        }

        ).Field;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            if ((int)s_foo != 3)
                return 1;
            return 0;
        }

        public class MyClass
        {
            public int Field;
            public int GetNumber()
            {
                return Field;
            }

            public int this[int index]
            {
                get
                {
                    return Field;
                }
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.initialize005.initialize005
{
    public class Test
    {
        private static object s_foo = ((MyClass)((dynamic)new MyClass()
        {
            Field = 3
        }

        ))[3];
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            if ((int)s_foo != 3)
                return 1;
            return 0;
        }

        public class MyClass
        {
            public int Field;
            public int GetNumber()
            {
                return Field;
            }

            public int this[int index]
            {
                get
                {
                    return Field;
                }
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.initialize006.initialize006
{
    public class Test
    {
        private static dynamic s_foo = (new MyClass()
        {
            Field = 3
        }

        );
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            if ((int)s_foo.Field != 3)
                return 1;
            return 0;
        }

        public class MyClass
        {
            public int Field;
            public int GetNumber()
            {
                return Field;
            }

            public int this[int index]
            {
                get
                {
                    return Field;
                }
            }
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.initialize007.initialize007
{
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic o = new object();
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.lambda002.lambda002
{
    // <Title>Lambda expressions</Title>
    // <Description>match build-in delegates
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
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            int state = 0;
            Action<dynamic, dynamic> lambda1 = (dynamic x, dynamic y) =>
            {
                state = x % y;
            }

            ;
            lambda1(7, 2);
            bool ret = 1 == state;
            dynamic d = "A";
            Func<object, dynamic, string> lambda2 = (x, y) => d + x + y;
            ret &= "ABC" == lambda2("B", "C");
            return ret ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.lambda003.lambda003
{
    // <Title>Lambda expressions</Title>
    // <Description>lambda as method parameter
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
            dynamic dVal = "L";
            bool ret = "L::L" == Lambda0(dVal)();
            dVal = (byte)9;
            ret &= 81 == Lambda1((D1)(x => (short)(dVal * x)), dVal);
            dVal = 'q';
            ret &= 'q' == Lambda2(dVal)(true, 'c');
            return ret ? 0 : 1;
        }

        public static D0 Lambda0(dynamic dVal)
        {
            return () => dVal + "::" + dVal;
        }

        public static short Lambda1(D1 dd, dynamic dVal)
        {
            return dd(dVal);
        }

        public static D2 Lambda2(dynamic dVal)
        {
            return (x, y) =>
            {
                if (x)
                    return (char)dVal;
                else
                    return y;
            }

            ;
        }

        public delegate string D0();
        public delegate short D1(byte p);
        public delegate char D2(bool p1, char p2);
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.lambda004b.lambda004b
{
    // <Title>Lambda expressions</Title>
    // <Description>Overload
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System;
    using System.Linq.Expressions;

    public delegate dynamic D(long x, object y);
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            long d1 = -1;
            var d2 = new Test();
            bool ret = d2.RunTest(d1, d2);
            return ret ? 0 : 1;
        }

        private int M(D e)
        {
            return 11;
        }

        private int M(Action<long, object, dynamic> f)
        {
            return 22;
        }

        private int M(Func<string, object, dynamic> f)
        {
            return 33;
        }

        private int M1(Expression<D> e)
        {
            return 44;
        }

        private int M1(Expression<Func<string, object, dynamic>> e)
        {
            return 66;
        }

        private bool RunTest(dynamic d1, dynamic d2)
        {
            bool ret = 11 == M((p1, p2) => p1 * 100);
            ret &= 22 == M((p1, p2, p3) =>
            {
            }

            );
            ret &= 33 == M((string p1, dynamic p2) => p1 + p2.ToString());
            ret &= 44 == M1((long p1, dynamic p2) => p1 * 100);
            ret &= 66 == M1((string p1, dynamic p2) => p1);
            return ret;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.lambda005.lambda005
{
    // <Title>Lambda expressions</Title>
    // <Description>unsafe
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    //unsafe delegate byte D(byte* p1, dynamic p2);
    //[TestClass]public unsafe class Test
    //{
    //static int M(D e) { return 1; }
    //static int M(Func<dynamic, object, dynamic> f) { return 0; }
    //static int M1(Expression<D> e) { return 1; }
    //static int M1(Func<byte, dynamic, object> f) { return 0; }
    //[Test][Priority(Priority.Priority2)]public void DynamicCSharpRunTest(){Assert.AreEqual(0, MainMethod(null));} public static int MainMethod(string[] args)
    //{
    //bool ret = 0 == M((i, j) => i);
    //ret &= 1 == M((i, j) => *i);
    //ret &= 0 == M1((x, y) => 2 * x);
    //System.Console.WriteLine(ret);
    //return ret ? 0 : 1;
    //}
    //}
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.lambda006.lambda006
{
    // <Title>Lambda expressions</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System;
    using System.Linq.Expressions;

    public class Test
    {
        
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            bool ret = true;
            dynamic d = 3;
            Expression<Func<int, object>> del = x => d;
            dynamic xx = del.Compile()(d);
            try
            {
                xx.Foo();
                ret = false;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (!ErrorVerifier.Verify(ErrorMessageId.NoSuchMember, e.Message, "int", "Foo"))
                    ret = false;
            }

            Expression<Func<dynamic, object>> del1 = x => d;
            dynamic y = del.Compile()(d);
            try
            {
                y.Foo();
                ret = false;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (!ErrorVerifier.Verify(ErrorMessageId.NoSuchMember, e.Message, "int", "Foo"))
                    ret = false;
            }

            Expression<Func<object, dynamic>> del2 = x => x;
            dynamic z = del.Compile()(3);
            try
            {
                z.Foo();
                ret = false;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (!ErrorVerifier.Verify(ErrorMessageId.NoSuchMember, e.Message, "int", "Foo"))
                    ret = false;
            }

            return ret ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.lambda007.lambda007
{
    // <Title>Lambda expressions</Title>
    // <Description>with anonymous method
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public delegate dynamic D(object p1, char p2);
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            D lambda = (x, y) =>
            {
                D d = delegate (dynamic i, char j)
                {
                    return j;
                }

                ;
                return d;
            }

            ;
            bool ret = 'q' == lambda(null, 'p')(null, 'q');
            //
            lambda = (x, y) =>
            {
                D d = delegate
                {
                    return y;
                }

                ;
                return d;
            }

            ;
            ret &= 'p' == lambda(null, 'p')(null, 'q');
            return ret ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.lambda008.lambda008
{
    // <Title>Lambda expressions</Title>
    // <Description>context
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(32,78\).*CS0162</Expects>
    using System;
    using System.Linq.Expressions;

    public class LMD : IDisposable
    {
        public LMD(Func<object, dynamic> p)
        {
        }

        public void Dispose()
        {
        }
    }

    public class Test
    {
        private static int s_status = 0;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            Expression<Func<object, dynamic>> del = x => x;
            dynamic d = del.Compile()(12345678);
            lock (d)
            {
                using (var obj = new LMD(y => y))
                {
                    var v = checked(d);
                    var arr = new[]
                    {
                    new LMD(x => x), new LMD(y => y), new LMD(z => z)}

                    ;
                    foreach (dynamic i in arr)
                    {
                        s_status++;
                    }

                    for (dynamic i = d; ((Func<object, dynamic>)(y => true))(d); new LMD(z => z))
                    {
                        ++s_status;
                        break;
                    }
                }
            }

            return s_status == 4 ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.lambda009.lambda009
{
    // <Title>Lambda expressions</Title>
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
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            Func<dynamic, object> del = x => x;
            var Xx = del(3);
            if ((int)Xx != 3)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.lambda010.lambda010
{
    // <Title>Lambda expressions</Title>
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
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            Func<object, dynamic> del = x => x;
            var Xx = del(3);
            if ((int)Xx != 3)
                return 1;
            try
            {
                Xx.Foo();
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.NoSuchMember, e.Message, "int", "Foo"))
                    return 0;
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.lambda011.lambda011
{
    // <Title>Lambda expressions</Title>
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
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            Func<dynamic, dynamic> del = x => x;
            var Xx = del(3);
            if ((int)Xx != 3)
                return 1;
            try
            {
                Xx.Foo();
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.NoSuchMember, e.Message, "int", "Foo"))
                    return 0;
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.lambda012.lambda012
{
    // <Title>Lambda expressions</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System;

    public class myClass
    {
        public int Foo
        {
            get
            {
                return 0;
            }
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new myClass();
            Func<int, int> del = x => d.Foo;
            return del(3);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.lambda013.lambda013
{
    // <Title>Lambda expressions</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System;

    public class myClass
    {
        public int Foo()
        {
            return 0;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new myClass();
            Func<int, int> del = x => d.Foo();
            return del(32);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.lambda014.lambda014
{
    // <Title>Lambda expressions</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System;

    public class myClass
    {
        public int Foo()
        {
            return 0;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = 3;
            Func<int, int> del = x => d;
            if (del(10) == 3)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.lambda015.lambda015
{
    // <Title>Lambda expressions</Title>
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
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = 3;
            Func<int, int> del = x => d;
            long result = del(10);
            if (result == 3)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.lambda016.lambda016
{
    // <Title>Lambda expressions</Title>
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
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            Func<int, dynamic> del = x => x + 1;
            int result = del(3);
            if (result == 4)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.lambda017.lambda017
{
    // <Title>Lambda expressions</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System;

    public class Test
    {
        public class Base
        {
            public int Field;
        }

        public class Derived : Base
        {
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            Func<int, dynamic> del = x => new Derived()
            {
                Field = x
            }

            ;
            dynamic d = 3;
            Base result = del(d);
            if (result.Field == 3)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.lambda018.lambda018
{
    // <Title>Lambda expressions</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System;

    public class Test
    {
        public class Base
        {
            public int Field;
        }

        public class Derived : Base
        {
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            Func<int, dynamic> del = x => new Base()
            {
                Field = x
            }

            ;
            dynamic d = 3;
            try
            {
                Derived result = del(d);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.NoImplicitConvCast, e.Message, "Test.Base", "Test.Derived"))
                    return 0;
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.lambda019.lambda019
{
    // <Title>Lambda expressions</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System;

    public class Test
    {
        public class Class1
        {
            public int Field;
        }

        public class Class2
        {
            public int Field;
            public static implicit operator Class2(Class1 p1)
            {
                return new Class2()
                {
                    Field = p1.Field + 1
                }

                ;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            Func<int, dynamic> del = x => new Class1()
            {
                Field = x
            }

            ;
            dynamic d = 3;
            Class2 result = del(d);
            if (result.Field == 4)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.lambda020.lambda020
{
    // <Title>Lambda expressions</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System;

    public class Test
    {
        public class Class1
        {
            public int Field;
        }

        public class Class2
        {
            public int Field;
            public static explicit operator Class2(Class1 p1)
            {
                return new Class2()
                {
                    Field = p1.Field + 1
                }

                ;
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            Func<int, dynamic> del = x => new Class1()
            {
                Field = x
            }

            ;
            dynamic d = 3;
            Class2 result = (Class2)del(d); // call explicit conversion.
            if (result.Field == 4)
                return 0;
            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.memberinitofanontype001.memberinitofanontype001
{
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic myInt = 3;
            dynamic myString = "foo";
            dynamic myNullArr = new int?[]
            {
            1, null
            }

            ;
            dynamic myArr = new decimal[]
            {
            decimal.MaxValue, decimal.One
            }

            ;
            dynamic myDec = decimal.MaxValue;
            var mc = new
            {
                Field = myInt,
                Prop = myString,
                NullArray = myNullArr,
                Array = myArr,
                Obj = myInt
            }

            ;
            if ((int)mc.Field != 3)
                return 1;
            if ((string)mc.Prop != "foo")
                return 1;
            if (((int?[])mc.NullArray).Length != 2 || ((int?[])mc.NullArray)[1] != null)
                return 1;
            if (((decimal[])mc.Array).Length != 2 || ((decimal[])mc.Array)[0] != decimal.MaxValue)
                return 1;
            if ((int)mc.Obj != 3)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.memberinitofanontype002.memberinitofanontype002
{
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic myInt = 3;
            dynamic myString = "foo";
            dynamic myNullArr = new int?[]
            {
            1, null
            }

            ;
            dynamic myArr = new decimal[]
            {
            decimal.MaxValue, decimal.One
            }

            ;
            dynamic myDec = decimal.MaxValue;
            var mc = new
            {
                Field = (int)myInt,
                Prop = (string)myString,
                NullArray = (int?[])myNullArr,
                Array = (decimal[])myArr,
                Obj = myInt
            }

            ;
            if (mc.Field != 3)
                return 1;
            if (mc.Prop != "foo")
                return 1;
            if (mc.NullArray.Length != 2 || mc.NullArray[1] != null)
                return 1;
            if (mc.Array.Length != 2 || mc.Array[0] != decimal.MaxValue)
                return 1;
            if ((int)mc.Obj != 3)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.memberinitofobjinit001.memberinitofobjinit001
{
    // <Title>Member initializer of object initializer</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class MyClass
    {
        private string _p = string.Empty;
        private decimal[] _arr;
        public int Field;
        public string Prop
        {
            get
            {
                return _p;
            }

            set
            {
                _p = value;
            }
        }

        public int?[] NullArray;
        public decimal[] Array
        {
            get
            {
                return _arr;
            }

            set
            {
                _arr = value;
            }
        }

        public object Obj;
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic myInt = 3;
            dynamic myString = "foo";
            dynamic myNullArr = new int?[]
            {
            1, null
            }

            ;
            dynamic myArr = new decimal[]
            {
            decimal.MaxValue, decimal.One
            }

            ;
            dynamic myDec = decimal.MaxValue;
            MyClass mc = new MyClass()
            {
                Field = myInt,
                Prop = myString,
                NullArray = myNullArr,
                Array = myArr,
                Obj = myInt
            }

            ;
            if (mc.Field != 3)
                return 1;
            if (mc.Prop != "foo")
                return 1;
            if (mc.NullArray.Length != 2 || mc.NullArray[1] != null)
                return 1;
            if (mc.Array.Length != 2 || mc.Array[0] != decimal.MaxValue)
                return 1;
            if ((int)mc.Obj != 3)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.memberinitofobjinit002.memberinitofobjinit002
{
    // <Title>Member initializer of object initializer</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class MyClass
    {
        private object _p = string.Empty;
        private object _arr;
        public object Field;
        public object Prop
        {
            get
            {
                return _p;
            }

            set
            {
                _p = value;
            }
        }

        public object NullArray;
        public object Array
        {
            get
            {
                return _arr;
            }

            set
            {
                _arr = value;
            }
        }

        public object Obj;
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic myInt = 3;
            dynamic myString = "foo";
            dynamic myNullArr = new int?[]
            {
            1, null
            }

            ;
            dynamic myArr = new decimal[]
            {
            decimal.MaxValue, decimal.One
            }

            ;
            dynamic myDec = decimal.MaxValue;
            MyClass mc = new MyClass()
            {
                Field = myInt,
                Prop = myString,
                NullArray = myNullArr,
                Array = myArr,
                Obj = myInt
            }

            ;
            if ((int)mc.Field != 3)
                return 1;
            if ((string)mc.Prop != "foo")
                return 1;
            if (((int?[])mc.NullArray).Length != 2 || ((int?[])mc.NullArray)[1] != null)
                return 1;
            if (((decimal[])mc.Array).Length != 2 || ((decimal[])mc.Array)[0] != decimal.MaxValue)
                return 1;
            if ((int)mc.Obj != 3)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.memberinitofobjinit003.memberinitofobjinit003
{
    // <Title>Member initializer of object initializer</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class MyClass
    {
        private object _p = string.Empty;
        private object _arr;
        public dynamic Field;
        public dynamic Prop
        {
            get
            {
                return _p;
            }

            set
            {
                _p = value;
            }
        }

        public dynamic NullArray;
        public dynamic Array
        {
            get
            {
                return _arr;
            }

            set
            {
                _arr = value;
            }
        }

        public dynamic Obj;
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            object myInt = 3;
            object myString = "foo";
            object myNullArr = new int?[]
            {
            1, null
            }

            ;
            object myArr = new decimal[]
            {
            decimal.MaxValue, decimal.One
            }

            ;
            object myDec = decimal.MaxValue;
            MyClass mc = new MyClass()
            {
                Field = myInt,
                Prop = myString,
                NullArray = myNullArr,
                Array = myArr,
                Obj = myInt
            }

            ;
            if ((int)mc.Field != 3)
                return 1;
            if ((string)mc.Prop != "foo")
                return 1;
            if (((int?[])mc.NullArray).Length != 2 || ((int?[])mc.NullArray)[1] != null)
                return 1;
            if (((decimal[])mc.Array).Length != 2 || ((decimal[])mc.Array)[0] != decimal.MaxValue)
                return 1;
            if ((int)mc.Obj != 3)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.memberinitofobjinit004.memberinitofobjinit004
{
    // <Title>Member initializer of object initializer</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class MyClass
    {
        private object _p = string.Empty;
        private object _arr;
        public dynamic Field;
        public dynamic Prop
        {
            get
            {
                return _p;
            }

            set
            {
                _p = value;
            }
        }

        public dynamic NullArray;
        public dynamic Array
        {
            get
            {
                return _arr;
            }

            set
            {
                _arr = value;
            }
        }

        public dynamic Obj;
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            int myInt = 3;
            string myString = "foo";
            int?[] myNullArr = new int?[]
            {
            1, null
            }

            ;
            decimal[] myArr = new decimal[]
            {
            decimal.MaxValue, decimal.One
            }

            ;
            MyClass mc = new MyClass()
            {
                Field = myInt,
                Prop = myString,
                NullArray = myNullArr,
                Array = myArr,
                Obj = myInt
            }

            ;
            if ((int)mc.Field != 3)
                return 1;
            if ((string)mc.Prop != "foo")
                return 1;
            if (((int?[])mc.NullArray).Length != 2 || ((int?[])mc.NullArray)[1] != null)
                return 1;
            if (((decimal[])mc.Array).Length != 2 || ((decimal[])mc.Array)[0] != decimal.MaxValue)
                return 1;
            if ((int)mc.Obj != 3)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.negboxing002.negboxing002
{
    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            //int
            object o = int.MinValue;
            dynamic d = (int)o;
            if (d != int.MinValue)
                return 1;
            //short
            o = short.MaxValue;
            d = (short)o;
            if (d != short.MaxValue)
                return 1;
            //sbyte
            o = sbyte.MaxValue;
            d = (sbyte)o;
            if (d != sbyte.MaxValue)
                return 1;
            //long
            o = long.MaxValue;
            d = (long)o;
            if (d != long.MaxValue)
                return 1;
            //byte
            o = byte.MaxValue;
            d = (byte)o;
            if (d != byte.MaxValue)
                return 1;
            //ushort
            o = ushort.MaxValue;
            d = (ushort)o;
            if (d != ushort.MaxValue)
                return 1;
            //ulong
            o = ulong.MaxValue;
            d = (ulong)o;
            if (d != ulong.MaxValue)
                return 1;
            //uint
            o = uint.MinValue;
            d = (uint)o;
            if (d != uint.MinValue)
                return 1;
            //char
            o = 'a';
            d = (char)o;
            if (d != 'a')
                return 1;
            //float
            o = float.Epsilon;
            d = (float)o;
            if (d != float.Epsilon)
                return 1;
            o = float.NegativeInfinity;
            d = (float)o;
            if (!float.IsNegativeInfinity(d))
                return 1;
            //double
            o = double.MaxValue;
            d = (double)o;
            if (d != double.MaxValue)
                return 1;
            //decimal
            o = decimal.MaxValue;
            d = (decimal)o;
            if (d != decimal.MaxValue)
                return 1;
            //bool
            o = true;
            d = (bool)o;
            if (d != true)
                return 1;
            //UD enum
            o = myEnum.One;
            d = (myEnum)o;
            if (d != myEnum.One)
                return 1;
            //UD struct
            o = new myStruct()
            {
                field = 1
            }

            ;
            d = (myStruct)o;
            if (d.field != 1)
                return 1;
            return 0;
        }

        public enum myEnum
        {
            One
        }

        public struct myStruct
        {
            public int field;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.nullcoalesce001.nullcoalesce001
{
    // <Title>Null coalescing</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class MyClass
    {
        public dynamic Get()
        {
            return null;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new MyClass();
            var x = d.Get() ?? new object();
            return x == null ? 1 : 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.unsfe001.unsfe001
{
    // <Title>stackalloc and dynamic</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    //class B
    //{
    //[Test][Priority(Priority.Priority2)]public void DynamicCSharpRunTest(){Assert.AreEqual(0, MainMethod());} public static unsafe int MainMethod()
    //{
    ////basic alloc
    //dynamic y = 1;
    //int* x = stackalloc int[y];
    //*x = 3;
    //if (*x != 3)
    //return 1;
    ////alloc with conversion from char
    //y = 'a';
    //try{
    //float* z = stackalloc float[y];
    //}
    //catch(System.Exception ex){
    //System.Console.WriteLine(ex);
    //return 1;
    //}
    ////alloc with operator
    //y = 'b';
    //try{
    //float* z = stackalloc float[ y - 'a'];
    //*z = 34f;
    //if (*z != 34)
    //return 1;
    //}
    //catch(System.Exception ex){
    //System.Console.WriteLine(ex);
    //return 1;
    //}
    ////alloc with return from a method call
    //dynamic b = new B();
    //try{
    //decimal* z = stackalloc decimal[b.GetFloatValue()];
    //}
    //catch(Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e){
    //if (!ErrorVerifier.Verify(ErrorMessageId.NoImplicitConvCast, e.Message, "float", "int"))
    //return 1;
    //}
    ////alloc with return from indexer
    //sbyte* t = stackalloc sbyte[b[3]];
    ////alloc with return from property
    //sbyte* u = stackalloc sbyte[b.prop];
    //return 0;
    //}
    //public dynamic GetFloatValue(){ return 12.4f; }
    //public int this[byte index]{ get{ return index;} }
    //public int prop {get {return 4;} }
    //}
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.query003.query003
{
    // <Title>Query expression</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;
    using System.Linq;

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = 3;
            var list = new List<int>
            {
            1, 2, 3
            }

            ;
            var x =
                from c in list
                where c == (int)d
                select c;
            if (x.Count() != 1)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.query004.query004
{
    // <Title>Query expression</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;
    using System.Linq;

    public class myClass
    {
        public int Transform(int x)
        {
            return x + 1;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new myClass();
            var list = new List<int>
            {
            1, 2, 3
            }

            ;
            var x = (
                from c in list
                where c == 2
                select d.Transform(c)).SingleOrDefault();
            if ((int)x != 3)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.query005.query005
{
    // <Title>Query expression</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;
    using System.Linq;

    public class myClass
    {
        public int Transform(int x)
        {
            return x + 1;
        }
    }

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d = new myClass();
            var list = new List<int>
            {
            1, 2, 3
            }

            ;
            var x = (
                from c in list
                where c == 2
                orderby d.Transform(c)
                select d.Transform(c)).SingleOrDefault();
            if ((int)x != 3)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.throw003.throw003
{
    // <Title>Throw</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class D
    {
        public void Foo<T>(T t) where T : System.Exception
        {
            throw t;
        }
    }

    public class C
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new D();
            dynamic d2 = 3;
            try
            {
                d.Foo(d2);
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.GenericConstraintNotSatisfiedValType, e.Message, "D.Foo<T>(T)", "System.Exception", "T", "int"))
                    return 0;
            }

            return 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.common.common
{
    // <Title> Dynamic and static interaction utility class </Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public class Verify
    {
        internal static int Eval(Func<bool> testmethod, string comment = null)
        {
            int result = 0;
            try
            {
                if (!testmethod())
                {
                    result++;
                }
            }
            catch (Exception e)
            {
                result++;
            }

            return result;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.operate001.operate001
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.common.common;
    // <Title> Operator -.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private enum MyEnum
        {
            First,
            Second,
            Third
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(Test1);
            result += Verify.Eval(Test2);
            result += Verify.Eval(Test3);
            result += Verify.Eval(Test4);
            result += Verify.Eval(Test5);
            result += Verify.Eval(Test6);
            result += Verify.Eval(Test7);
            return result;
        }

        private static bool Test1()
        {
            dynamic a = 10;
            int b = -a;
            if (b == -10)
                return true;
            System.Console.WriteLine("Failed -- -int");
            return false;
        }

        private static bool Test2()
        {
            dynamic a = 10L;
            long b = -(-a);
            if (b == 10)
                return true;
            System.Console.WriteLine("Failed -- -long");
            return false;
        }

        private static bool Test3()
        {
            dynamic a = 10.10f;
            float b = -a;
            if (b == -10.10f)
                return true;
            System.Console.WriteLine("Failed -- -float");
            return false;
        }

        private static bool Test4()
        {
            dynamic a = 10.10d;
            double b = -a;
            if (b == -10.10)
                return true;
            System.Console.WriteLine("Failed -- -double");
            return false;
        }

        private static bool Test5()
        {
            dynamic a = 10.001M;
            decimal b = -a;
            if (b == -10.001M)
                return true;
            System.Console.WriteLine("Failed -- -decimal");
            return false;
        }

        private static bool Test6()
        {
            uint i = 10;
            dynamic a = i;
            try
            {
                int b = -a; //no - operator on uint.
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.NoImplicitConvCast, e.Message, "long", "int"))
                    return true;
            }

            return false;
        }

        private static bool Test7()
        {
            dynamic a = MyEnum.First;
            try
            {
                MyEnum b = -a; //no - operator on enum.
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadUnaryOp, e.Message, "-", "Test.MyEnum"))
                    return true;
            }

            return false;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.operate002.operate002
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.common.common;
    // <Title> Operator +.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        private enum MyEnum
        {
            First,
            Second,
            Third
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(Test1);
            result += Verify.Eval(Test2);
            result += Verify.Eval(Test3);
            result += Verify.Eval(Test4);
            result += Verify.Eval(Test5);
            result += Verify.Eval(Test6);
            result += Verify.Eval(Test7);
            return result;
        }

        private static bool Test1()
        {
            dynamic a = -10;
            int b = +a;
            if (b == -10)
                return true;
            System.Console.WriteLine("Failed -- +int");
            return false;
        }

        private static bool Test2()
        {
            dynamic a = -10L;
            long b = +(+a);
            if (b == -10)
                return true;
            System.Console.WriteLine("Failed -- +long");
            return false;
        }

        private static bool Test3()
        {
            dynamic a = 10.10f;
            float b = +a;
            if (b == 10.10f)
                return true;
            System.Console.WriteLine("Failed -- +float");
            return false;
        }

        private static bool Test4()
        {
            dynamic a = 10.10d;
            double b = +(-a);
            if (b == -10.10)
                return true;
            System.Console.WriteLine("Failed -- +double");
            return false;
        }

        private static bool Test5()
        {
            dynamic a = -10.001M;
            decimal b = +a;
            if (b == -10.001M)
                return true;
            System.Console.WriteLine("Failed -- +decimal");
            return false;
        }

        private static bool Test6()
        {
            string i = "10";
            dynamic a = i;
            try
            {
                int b = +a; //no + operator on string.
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadUnaryOp, e.Message, "+", "string"))
                    return true;
            }

            return false;
        }

        private static bool Test7()
        {
            dynamic a = MyEnum.Second;
            try
            {
                MyEnum b = +a; //no + operator on enum.
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadUnaryOp, e.Message, "+", "Test.MyEnum"))
                    return true;
            }

            return false;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.operate003.operate003
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.common.common;
    // <Title> Operator ~.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public enum MyEnum
        {
            First,
            Second,
            Third
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(Test1);
            result += Verify.Eval(Test2);
            result += Verify.Eval(Test3);
            result += Verify.Eval(Test4);
            result += Verify.Eval(Test5);
            result += Verify.Eval(Test6);
            return result;
        }

        private static bool Test1()
        {
            dynamic a = 10;
            int b = ~a;
            if (b == -11)
                return true;
            System.Console.WriteLine("Failed -- ~int");
            return false;
        }

        private static bool Test2()
        {
            dynamic a = -10L;
            long b = ~a;
            if (b == 9)
                return true;
            System.Console.WriteLine("Failed -- ~long");
            return false;
        }

        private static bool Test3()
        {
            dynamic a = uint.MinValue;
            uint b = ~a;
            if (b == uint.MaxValue)
                return true;
            System.Console.WriteLine("Failed -- ~uint");
            return false;
        }

        private static bool Test4()
        {
            dynamic a = ulong.MaxValue;
            ulong b = ~~a;
            if (b == ulong.MaxValue)
                return true;
            System.Console.WriteLine("Failed -- ~ulong");
            return false;
        }

        private static bool Test5()
        {
            dynamic a = MyEnum.Second;
            MyEnum b = ~a;
            if (b == ~MyEnum.Second)
                return true;
            System.Console.WriteLine("Failed -- ~enum");
            return false;
        }

        private static bool Test6()
        {
            dynamic a = "10";
            try
            {
                int b = ~a; //no ~ operator on string.
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadUnaryOp, e.Message, "~", "string"))
                    return true;
            }

            System.Console.WriteLine("Failed -- ~string");
            return false;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.operate004.operate004
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.common.common;
    // <Title> Operator !.</Title>
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
            int result = 0;
            result += Verify.Eval(Test1);
            result += Verify.Eval(Test2);
            result += Verify.Eval(Test3);
            return result;
        }

        private static bool Test1()
        {
            bool[] boolValues = new bool[]
            {
            true, false
            }

            ;
            foreach (bool a in boolValues)
            {
                dynamic d = a;
                if (!d != !a)
                {
                    System.Console.WriteLine("Failed -- !bool");
                    return false;
                }
            }

            return true;
        }

        private static bool Test2()
        {
            bool?[] boolValues = new bool?[]
            {
            true, false
            };

            foreach (bool? a in boolValues)
            {
                dynamic d = a;
                if (!d != !a)
                {
                    System.Console.WriteLine("Failed -- !Nullable<bool>");
                    return false;
                }
            }

            return true;
        }

        private static bool Test3()
        {
            dynamic a = "10";
            try
            {
                int b = !a; //no ! operator on string.
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadUnaryOp, e.Message, "!", "string"))
                    return true;
            }

            return false;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.operate005.operate005
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.common.common;
    // <Title> Operator ++.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public enum MyEnum
        {
            First,
            Second,
            Third
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(Test1);
            result += Verify.Eval(Test2);
            result += Verify.Eval(Test3);
            result += Verify.Eval(Test4);
            result += Verify.Eval(Test5);
            result += Verify.Eval(Test6);
            result += Verify.Eval(Test7);
            result += Verify.Eval(Test8);
            result += Verify.Eval(Test9);
            result += Verify.Eval(Test10);
            result += Verify.Eval(Test11);
            result += Verify.Eval(Test12);
            result += Verify.Eval(Test13);
            result += Verify.Eval(Test14);
            return result;
        }

        private static bool Test1()
        {
            sbyte a = 10;
            dynamic b = a;
            ++b;
            if (b == 11)
                return true;
            System.Console.WriteLine("Failed -- ++sbyte");
            return false;
        }

        private static bool Test2()
        {
            byte a = 10;
            dynamic b = a;
            ++b;
            if (b == 11)
                return true;
            System.Console.WriteLine("Failed -- ++byte");
            return false;
        }

        private static bool Test3()
        {
            short a = 1;
            dynamic b = a;
            ++b;
            if (b == 2)
                return true;
            System.Console.WriteLine("Failed -- ++short");
            return false;
        }

        private static bool Test4()
        {
            ushort a = 1;
            dynamic b = a;
            ++b;
            if (b == 2)
                return true;
            System.Console.WriteLine("Failed -- ++ushort");
            return false;
        }

        private static bool Test5()
        {
            int a = -1;
            dynamic b = a;
            ++b;
            if (b == 0)
                return true;
            System.Console.WriteLine("Failed -- ++int");
            return false;
        }

        private static bool Test6()
        {
            uint a = 1;
            dynamic b = a;
            ++b;
            if (b == 2)
                return true;
            System.Console.WriteLine("Failed -- ++uint");
            return false;
        }

        private static bool Test7()
        {
            long a = 1;
            dynamic b = a;
            ++b;
            if (b == 2)
                return true;
            System.Console.WriteLine("Failed -- ++long");
            return false;
        }

        private static bool Test8()
        {
            ulong a = 1;
            dynamic b = a;
            ++b;
            if (b == 2)
                return true;
            System.Console.WriteLine("Failed -- ++ulong");
            return false;
        }

        private static bool Test9()
        {
            char a = 'a';
            dynamic b = a;
            ++b;
            if (b == 'b')
                return true;
            System.Console.WriteLine("Failed -- ++char");
            return false;
        }

        private static bool Test10()
        {
            float a = 1.10f;
            dynamic b = a;
            ++b;
            if (b == 2.10f)
                return true;
            System.Console.WriteLine("Failed -- ++float");
            return false;
        }

        private static bool Test11()
        {
            double a = 1.10d;
            dynamic b = a;
            ++b;
            if (b == 2.10d)
                return true;
            System.Console.WriteLine("Failed -- ++double");
            return false;
        }

        private static bool Test12()
        {
            decimal a = 1.10M;
            dynamic b = a;
            ++b;
            if (b == 2.10M)
                return true;
            System.Console.WriteLine("Failed -- ++decimal");
            return false;
        }

        private static bool Test13()
        {
            MyEnum a = MyEnum.Second;
            dynamic b = a;
            ++b;
            if (b == MyEnum.Third)
                return true;
            System.Console.WriteLine("Failed -- ++enum");
            return false;
        }

        private static bool Test14()
        {
            dynamic a = "10";
            try
            {
                ++a; //no ++ operator on string.
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadUnaryOp, e.Message, "++", "string"))
                    return true;
            }

            return false;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.operate005a.operate005a
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.common.common;
    // <Title> Operator ++.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public enum MyEnum
        {
            First,
            Second,
            Third
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(Test1);
            result += Verify.Eval(Test2);
            result += Verify.Eval(Test3);
            result += Verify.Eval(Test4);
            result += Verify.Eval(Test5);
            result += Verify.Eval(Test6);
            result += Verify.Eval(Test7);
            result += Verify.Eval(Test8);
            result += Verify.Eval(Test9);
            result += Verify.Eval(Test10);
            result += Verify.Eval(Test11);
            result += Verify.Eval(Test12);
            result += Verify.Eval(Test13);
            result += Verify.Eval(Test14);
            return result;
        }

        private static bool Test1()
        {
            sbyte a = 10;
            dynamic b = a;
            b++;
            if (b == 11)
                return true;
            System.Console.WriteLine("Failed -- ++sbyte");
            return false;
        }

        private static bool Test2()
        {
            byte a = 10;
            dynamic b = a;
            b++;
            if (b == 11)
                return true;
            System.Console.WriteLine("Failed -- b++yte");
            return false;
        }

        private static bool Test3()
        {
            short a = 1;
            dynamic b = a;
            b++;
            if (b == 2)
                return true;
            System.Console.WriteLine("Failed -- ++short");
            return false;
        }

        private static bool Test4()
        {
            ushort a = 1;
            dynamic b = a;
            b++;
            if (b == 2)
                return true;
            System.Console.WriteLine("Failed -- ++ushort");
            return false;
        }

        private static bool Test5()
        {
            int a = -1;
            dynamic b = a;
            b++;
            if (b == 0)
                return true;
            System.Console.WriteLine("Failed -- ++int");
            return false;
        }

        private static bool Test6()
        {
            uint a = 1;
            dynamic b = a;
            b++;
            if (b == 2)
                return true;
            System.Console.WriteLine("Failed -- ++uint");
            return false;
        }

        private static bool Test7()
        {
            long a = 1;
            dynamic b = a;
            b++;
            if (b == 2)
                return true;
            System.Console.WriteLine("Failed -- ++long");
            return false;
        }

        private static bool Test8()
        {
            ulong a = 1;
            dynamic b = a;
            b++;
            if (b == 2)
                return true;
            System.Console.WriteLine("Failed -- ++ulong");
            return false;
        }

        private static bool Test9()
        {
            char a = 'a';
            dynamic b = a;
            b++;
            if (b == 'b')
                return true;
            System.Console.WriteLine("Failed -- ++char");
            return false;
        }

        private static bool Test10()
        {
            float a = 1.10f;
            dynamic b = a;
            b++;
            if (b == 2.10f)
                return true;
            System.Console.WriteLine("Failed -- ++float");
            return false;
        }

        private static bool Test11()
        {
            double a = 1.10d;
            dynamic b = a;
            b++;
            if (b == 2.10d)
                return true;
            System.Console.WriteLine("Failed -- ++double");
            return false;
        }

        private static bool Test12()
        {
            decimal a = 1.10M;
            dynamic b = a;
            b++;
            if (b == 2.10M)
                return true;
            System.Console.WriteLine("Failed -- ++decimal");
            return false;
        }

        private static bool Test13()
        {
            MyEnum a = MyEnum.Second;
            dynamic b = a;
            b++;
            if (b == MyEnum.Third)
                return true;
            System.Console.WriteLine("Failed -- ++enum");
            return false;
        }

        private static bool Test14()
        {
            dynamic a = "10";
            try
            {
                a++; //no ++ operator on string.
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadUnaryOp, e.Message, "++", "string"))
                    return true;
            }

            return false;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.operate006.operate006
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.common.common;
    // <Title> Operator --.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public enum MyEnum
        {
            First,
            Second,
            Third
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(Test1);
            result += Verify.Eval(Test2);
            result += Verify.Eval(Test3);
            result += Verify.Eval(Test4);
            result += Verify.Eval(Test5);
            result += Verify.Eval(Test6);
            result += Verify.Eval(Test7);
            result += Verify.Eval(Test8);
            result += Verify.Eval(Test9);
            result += Verify.Eval(Test10);
            result += Verify.Eval(Test11);
            result += Verify.Eval(Test12);
            result += Verify.Eval(Test13);
            result += Verify.Eval(Test14);
            return result;
        }

        private static bool Test1()
        {
            sbyte a = 10;
            dynamic b = a;
            --b;
            if (b == 9)
                return true;
            System.Console.WriteLine("Failed -- --sbyte");
            return false;
        }

        private static bool Test2()
        {
            byte a = 10;
            dynamic b = a;
            --b;
            if (b == 9)
                return true;
            System.Console.WriteLine("Failed -- --byte");
            return false;
        }

        private static bool Test3()
        {
            short a = 2;
            dynamic b = a;
            --b;
            if (b == 1)
                return true;
            System.Console.WriteLine("Failed -- --short");
            return false;
        }

        private static bool Test4()
        {
            ushort a = 2;
            dynamic b = a;
            --b;
            if (b == 1)
                return true;
            System.Console.WriteLine("Failed -- --ushort");
            return false;
        }

        private static bool Test5()
        {
            int a = -1;
            dynamic b = a;
            --b;
            if (b == -2)
                return true;
            System.Console.WriteLine("Failed -- --int");
            return false;
        }

        private static bool Test6()
        {
            uint a = 1;
            dynamic b = a;
            --b;
            if (b == 0)
                return true;
            System.Console.WriteLine("Failed -- --uint");
            return false;
        }

        private static bool Test7()
        {
            long a = 1;
            dynamic b = a;
            --b;
            if (b == 0)
                return true;
            System.Console.WriteLine("Failed -- --long");
            return false;
        }

        private static bool Test8()
        {
            ulong a = 2;
            dynamic b = a;
            --b;
            if (b == 1)
                return true;
            System.Console.WriteLine("Failed -- --ulong");
            return false;
        }

        private static bool Test9()
        {
            char a = 'b';
            dynamic b = a;
            --b;
            if (b == 'a')
                return true;
            System.Console.WriteLine("Failed -- --char");
            return false;
        }

        private static bool Test10()
        {
            float a = 1.11f;
            dynamic b = a;
            --b;
            a--;
            if (b == a)
                return true;
            System.Console.WriteLine("Failed -- --float");
            return false;
        }

        private static bool Test11()
        {
            double a = 1.12d;
            dynamic b = a;
            --b;
            a--;
            if (b == a)
                return true;
            System.Console.WriteLine("Failed -- --double");
            return false;
        }

        private static bool Test12()
        {
            decimal a = 2.10M;
            dynamic b = a;
            --b;
            if (b == 1.10M)
                return true;
            System.Console.WriteLine("Failed -- --decimal");
            return false;
        }

        private static bool Test13()
        {
            MyEnum a = MyEnum.Third;
            dynamic b = a;
            --b;
            if (b == MyEnum.Second)
                return true;
            System.Console.WriteLine("Failed -- --enum");
            return false;
        }

        private static bool Test14()
        {
            dynamic a = "10";
            try
            {
                --a; //no -- operator on string.
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadUnaryOp, e.Message, "--", "string"))
                    return true;
            }

            return false;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.operate006a.operate006a
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.common.common;
    // <Title> Operator --.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public enum MyEnum
        {
            First,
            Second,
            Third
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(Test1);
            result += Verify.Eval(Test2);
            result += Verify.Eval(Test3);
            result += Verify.Eval(Test4);
            result += Verify.Eval(Test5);
            result += Verify.Eval(Test6);
            result += Verify.Eval(Test7);
            result += Verify.Eval(Test8);
            result += Verify.Eval(Test9);
            result += Verify.Eval(Test10);
            result += Verify.Eval(Test11);
            result += Verify.Eval(Test12);
            result += Verify.Eval(Test13);
            result += Verify.Eval(Test14);
            return result;
        }

        private static bool Test1()
        {
            sbyte a = 10;
            dynamic b = a;
            b--;
            if (b == 9)
                return true;
            System.Console.WriteLine("Failed -- --sbyte");
            return false;
        }

        private static bool Test2()
        {
            byte a = 10;
            dynamic b = a;
            b--;
            if (b == 9)
                return true;
            System.Console.WriteLine("Failed -- b--yte");
            return false;
        }

        private static bool Test3()
        {
            short a = 2;
            dynamic b = a;
            b--;
            if (b == 1)
                return true;
            System.Console.WriteLine("Failed -- --short");
            return false;
        }

        private static bool Test4()
        {
            ushort a = 2;
            dynamic b = a;
            b--;
            if (b == 1)
                return true;
            System.Console.WriteLine("Failed -- --ushort");
            return false;
        }

        private static bool Test5()
        {
            int a = -1;
            dynamic b = a;
            b--;
            if (b == -2)
                return true;
            System.Console.WriteLine("Failed -- --int");
            return false;
        }

        private static bool Test6()
        {
            uint a = 2;
            dynamic b = a;
            b--;
            if (b == 1)
                return true;
            System.Console.WriteLine("Failed -- --uint");
            return false;
        }

        private static bool Test7()
        {
            long a = 1;
            dynamic b = a;
            b--;
            if (b == 0)
                return true;
            System.Console.WriteLine("Failed -- --long");
            return false;
        }

        private static bool Test8()
        {
            ulong a = 2;
            dynamic b = a;
            b--;
            if (b == 1)
                return true;
            System.Console.WriteLine("Failed -- --ulong");
            return false;
        }

        private static bool Test9()
        {
            char a = 'b';
            dynamic b = a;
            b--;
            if (b == 'a')
                return true;
            System.Console.WriteLine("Failed -- --char");
            return false;
        }

        private static bool Test10()
        {
            float a = 2.10f;
            dynamic b = a;
            b--;
            --a;
            if (b == a)
                return true;
            System.Console.WriteLine("Failed -- --float");
            return false;
        }

        private static bool Test11()
        {
            double a = 2.10d;
            dynamic b = a;
            b--;
            if (b == 1.10d)
                return true;
            System.Console.WriteLine("Failed -- --double");
            return false;
        }

        private static bool Test12()
        {
            decimal a = 2.10M;
            dynamic b = a;
            b--;
            if (b == 1.10M)
                return true;
            System.Console.WriteLine("Failed -- --decimal");
            return false;
        }

        private static bool Test13()
        {
            MyEnum a = MyEnum.Third;
            dynamic b = a;
            b--;
            if (b == MyEnum.Second)
                return true;
            System.Console.WriteLine("Failed -- --enum");
            return false;
        }

        private static bool Test14()
        {
            dynamic a = "10";
            try
            {
                a--; //no -- operator on string.
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadUnaryOp, e.Message, "--", "string"))
                    return true;
            }

            return false;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.operate007.operate007
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.common.common;
    // <Title>Bool logical Operator &, | , ^</Title>
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
            int result = 0;
            result += Verify.Eval(Test1);
            result += Verify.Eval(Test2);
            result += Verify.Eval(Test3);
            result += Verify.Eval(Test4);
            result += Verify.Eval(Test5);
            result += Verify.Eval(Test6);
            return result;
        }

        private static bool Test1()
        {
            bool[] boolValues = new bool[]
            {
            true, false
            }

            ;
            foreach (bool a1 in boolValues)
            {
                foreach (bool a2 in boolValues)
                {
                    dynamic d1 = a1;
                    dynamic d2 = a2;
                    if ((d1 & d2) != (a1 & a2))
                    {
                        System.Console.WriteLine("Failed -- bool & bool");
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool Test2()
        {
            bool?[] boolValues = new bool?[]
            {
            true, false, null
            }

            ;
            foreach (bool? a1 in boolValues)
            {
                foreach (bool? a2 in boolValues)
                {
                    if (a1 == null && a2 == null) 
                        continue;
                    dynamic d1 = a1;
                    dynamic d2 = a2;
                    if ((d1 & d2) != (a1 & a2))
                    {
                        System.Console.WriteLine("Failed -- bool? & bool?");
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool Test3()
        {
            bool[] boolValues = new bool[]
            {
            true, false
            }

            ;
            foreach (bool a1 in boolValues)
            {
                foreach (bool a2 in boolValues)
                {
                    dynamic d1 = a1;
                    dynamic d2 = a2;
                    if ((d1 | d2) != (a1 | a2))
                    {
                        System.Console.WriteLine("Failed -- bool | bool");
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool Test4()
        {
            bool?[] boolValues = new bool?[]
            {
            true, false, null
            }

            ;
            foreach (bool? a1 in boolValues)
            {
                foreach (bool? a2 in boolValues)
                {
                    if (a1 == null && a2 == null) 
                        continue;
                    dynamic d1 = a1;
                    dynamic d2 = a2;
                    if ((d1 | d2) != (a1 | a2))
                    {
                        System.Console.WriteLine("Failed -- bool? | bool?");
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool Test5()
        {
            bool[] boolValues = new bool[]
            {
            true, false
            }

            ;
            foreach (bool a1 in boolValues)
            {
                foreach (bool a2 in boolValues)
                {
                    dynamic d1 = a1;
                    dynamic d2 = a2;
                    if ((d1 ^ d2) != (a1 ^ a2))
                    {
                        System.Console.WriteLine("Failed -- bool ^ bool");
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool Test6()
        {
            bool?[] boolValues = new bool?[]
            {
            true, false, null
            }

            ;
            foreach (bool? a1 in boolValues)
            {
                foreach (bool? a2 in boolValues)
                {
                    if (a1 == null && a2 == null) 
                        continue;
                    dynamic d1 = a1;
                    dynamic d2 = a2;
                    if ((d1 ^ d2) != (a1 ^ a2))
                    {
                        System.Console.WriteLine("Failed -- bool? ^ bool?");
                        return false;
                    }
                }
            }

            return true;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.operate007a.operate007a
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.common.common;
    // <Title>Bool logical Operator &=, |= , ^=, Compound assignment</Title>
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
            int result = 0;
            result += Verify.Eval(Test1);
            result += Verify.Eval(Test2);
            result += Verify.Eval(Test3);
            result += Verify.Eval(Test4);
            result += Verify.Eval(Test5);
            result += Verify.Eval(Test6);
            return result;
        }

        private static bool Test1()
        {
            bool[] boolValues = new bool[]
            {
            true, false
            }

            ;
            foreach (bool a1 in boolValues)
            {
                foreach (bool a2 in boolValues)
                {
                    var b11 = a1;
                    var b12 = a2;
                    var b21 = a1;
                    var b22 = a2;
                    dynamic d1 = a1;
                    dynamic d2 = a2;
                    if ((b11 &= d2) != (b21 &= b22))
                    {
                        System.Console.WriteLine("Failed -- bool &= bool");
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool Test2()
        {
            int x = 0;
            int y = 0;
            bool?[] boolValues = new bool?[]
            {
            true, false, null
            }

            ;
            foreach (bool? a1 in boolValues)
            {
                x++;
                y = 0;
                foreach (bool? a2 in boolValues)
                {
                    y++;
                    if (a1 == null || a2 == null) 
                        continue;
                    var b11 = a1;
                    var b12 = a2;
                    var b21 = a1;
                    var b22 = a2;
                    dynamic d1 = a1;
                    dynamic d2 = a2;
                    // dynamic not keep bool? type info
                    if (b12.HasValue)
                    {
                        d1 &= b12.Value;
                    }
                    else if (d1)
                    {
                        d1 = null;
                    }

                    // if (((d1 &= b12) != (b21 &= b22)))
                    if (d1 != (b21 &= b22))
                    {
                        System.Console.WriteLine("Failed -- bool? &= bool?");
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool Test3()
        {
            bool[] boolValues = new bool[]
            {
            true, false
            }

            ;
            foreach (bool a1 in boolValues)
            {
                foreach (bool a2 in boolValues)
                {
                    var b11 = a1;
                    var b12 = a2;
                    var b21 = a1;
                    var b22 = a2;
                    dynamic d1 = a1;
                    dynamic d2 = a2;
                    if ((d1 |= d2) != (b21 |= b22))
                    {
                        System.Console.WriteLine("Failed -- bool |= bool");
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool Test4()
        {
            bool?[] boolValues = new bool?[]
            {
            true, false, null
            }

            ;
            foreach (bool? a1 in boolValues)
            {
                foreach (bool? a2 in boolValues)
                {
                    if (a1 == null && a2 == null) 
                        continue;
                    var b11 = a1;
                    var b12 = a2;
                    var b21 = a1;
                    var b22 = a2;
                    dynamic d1 = a1;
                    dynamic d2 = a2;
                    // dynamic not keep bool? type info - bool OR null obj
                    if (null == d1 && null == d2)
                    {
                        d1 = null;
                    }
                    else if (null != d1 && null != d2)
                    {
                        d1 |= d2;
                    }
                    else
                    {
                        if (null == d1)
                        {
                            if (!d2)
                                d1 = null;
                            else
                                d1 = d2;
                        }

                        if (null == d2)
                        {
                            if (!d1)
                                d1 = null;
                        }
                    }

                    //if ((d1 |= d2) != (b21 |= b22))
                    if (d1 != (b21 |= b22))
                    {
                        System.Console.WriteLine("Failed -- bool? |= bool?");
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool Test5()
        {
            bool[] boolValues = new bool[]
            {
            true, false
            }

            ;
            foreach (bool a1 in boolValues)
            {
                foreach (bool a2 in boolValues)
                {
                    var b11 = a1;
                    var b12 = a2;
                    var b21 = a1;
                    var b22 = a2;
                    dynamic d1 = a1;
                    dynamic d2 = a2;
                    if ((d1 ^= b12) != (b21 ^= b22))
                    {
                        System.Console.WriteLine("Failed -- bool ^= bool");
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool Test6()
        {
            bool?[] boolValues = new bool?[]
            {
            true, false, null
            }

            ;
            foreach (bool? a1 in boolValues)
            {
                foreach (bool? a2 in boolValues)
                {
                    if (a1 == null && a2 == null) 
                        continue;
                    var b11 = a1;
                    var b12 = a2;
                    var b21 = a1;
                    var b22 = a2;
                    dynamic d1 = a1;
                    dynamic d2 = a2;
                    if ((b11 ^= d2) != (b21 ^= b22))
                    {
                        System.Console.WriteLine("Failed -- bool? ^= bool?");
                        return false;
                    }
                }
            }

            return true;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.operate007b.operate007b
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.common.common;
    // <Title>Bool logical Operator &=, |= , ^=, Compound assignment</Title>
    // <Description>
    // dynamic op literals -
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
            int result = 0;
            result += Verify.Eval(Test1);
            result += Verify.Eval(Test2);
            // dynamic does NOT keep nullable info - either bool or null (obj)
            // test3 and test 5 no much value
            // result += Verify.Eval(Test3);
            result += Verify.Eval(Test4);
            // result += Verify.Eval(Test5);
            result += Verify.Eval(Test6);
            result += Verify.Eval(Test7);
            return result;
        }

        private static bool Test1()
        {
            bool[] boolValues = new bool[]
            {
            true, false
            }

            ;
            foreach (bool a1 in boolValues)
            {
                var b1 = a1;
                dynamic d1 = a1;
                if ((b1 &= true) != (d1 &= true))
                {
                    System.Console.WriteLine("Failed -- bool &= bool");
                    return false;
                }
            }

            return true;
        }

        private static bool Test2()
        {
            bool?[] boolValues = new bool?[]
            {
            true, false, null
            }

            ;
            foreach (bool? a1 in boolValues)
            {
                var b1 = a1;
                dynamic d1 = a1;
                if ((b1 &= false) != (d1 &= false))
                {
                    System.Console.WriteLine("Failed -- bool? &= bool?");
                    return false;
                }
            }

            return true;
        }

        private static bool Test3()
        {
            bool?[] boolValues = new bool?[]
            {
            true, false
            }

            ;
            foreach (bool? a1 in boolValues)
            {
                var b1 = a1;
                dynamic d1 = a1;
                if ((b1 &= null) != (d1 &= null))
                {
                    System.Console.WriteLine("Failed -- bool? &= null");
                    return false;
                }
            }

            return true;
        }

        private static bool Test4()
        {
            bool[] boolValues = new bool[]
            {
            true, false
            }

            ;
            foreach (bool a1 in boolValues)
            {
                var b1 = a1;
                dynamic d1 = a1;
                if ((b1 |= false) != (d1 |= false))
                {
                    System.Console.WriteLine("Failed -- bool |= bool");
                    return false;
                }
            }

            return true;
        }

        private static bool Test5()
        {
            bool?[] boolValues = new bool?[]
            {
            true, false
            }

            ;
            foreach (bool? a1 in boolValues)
            {
                var b1 = a1;
                dynamic d1 = a1;
                if ((b1 |= null) != (d1 |= null))
                {
                    System.Console.WriteLine("Failed -- bool? |= null");
                    return false;
                }
            }

            return true;
        }

        private static bool Test6()
        {
            bool[] boolValues = new bool[]
            {
            true, false
            }

            ;
            foreach (bool a1 in boolValues)
            {
                var b1 = a1;
                dynamic d1 = a1;
                if ((b1 ^= true) != (d1 ^= true))
                {
                    System.Console.WriteLine("Failed -- bool ^= bool");
                    return false;
                }
            }

            return true;
        }

        private static bool Test7()
        {
            bool?[] boolValues = new bool?[]
            {
            true, false, null
            }

            ;
            foreach (bool? a1 in boolValues)
            {
                var b1 = a1;
                dynamic d1 = a1;
                if ((b1 ^= true) != (d1 ^= true))
                {
                    System.Console.WriteLine("Failed -- bool? ^= bool?");
                    return false;
                }
            }

            return true;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.operate008.operate008
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.common.common;
    // <Title>Conditional logical operators ||, &&</Title>
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
            int result = 0;
            result += Verify.Eval(Test1);
            result += Verify.Eval(Test2);
            result += Verify.Eval(Test3);
            result += Verify.Eval(Test4);
            return result;
        }

        private static bool Test1()
        {
            bool[] boolValues = new bool[]
            {
            true, false
            }

            ;
            foreach (bool a1 in boolValues)
            {
                foreach (bool a2 in boolValues)
                {
                    dynamic d1 = a1;
                    dynamic d2 = a2;
                    if ((d1 || d2) != (a1 || a2))
                    {
                        System.Console.WriteLine("Failed -- bool & bool");
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool Test2()
        {
            bool[] boolValues = new bool[]
            {
            true, false
            }

            ;
            foreach (bool a1 in boolValues)
            {
                foreach (bool a2 in boolValues)
                {
                    dynamic d1 = a1;
                    bool d2 = a2;
                    if ((d1 || d2) != (a1 || a2))
                    {
                        System.Console.WriteLine("Failed -- bool & bool");
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool Test3()
        {
            bool[] boolValues = new bool[]
            {
            true, false
            }

            ;
            foreach (bool a1 in boolValues)
            {
                foreach (bool a2 in boolValues)
                {
                    dynamic d1 = a1;
                    dynamic d2 = a2;
                    if ((d1 && d2) != (a1 && a2))
                    {
                        System.Console.WriteLine("Failed -- bool | bool");
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool Test4()
        {
            bool[] boolValues = new bool[]
            {
            true, false
            }

            ;
            foreach (bool a1 in boolValues)
            {
                foreach (bool a2 in boolValues)
                {
                    bool d1 = a1;
                    dynamic d2 = a2;
                    if ((d1 && d2) != (a1 && a2))
                    {
                        System.Console.WriteLine("Failed -- bool | bool");
                        return false;
                    }
                }
            }

            return true;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.operate008a.operate008a
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.common.common;
    // <Title>Conditional logical operators ||, &&</Title>
    // <Description>
    // dynamic op literals
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
            int result = 0;
            result += Verify.Eval(Test1);
            result += Verify.Eval(Test2);
            result += Verify.Eval(Test3);
            result += Verify.Eval(Test3a);
            result += Verify.Eval(Test4);
            result += Verify.Eval(Test4a);
            result += Verify.Eval(Test5);
            result += Verify.Eval(Test6);
            result += Verify.Eval(Test7);
            result += Verify.Eval(Test7a);
            result += Verify.Eval(Test8);
            return result;
        }

        private static bool Test1()
        {
            bool[] boolValues = new bool[]
            {
            true, false
            }

            ;
            foreach (bool a1 in boolValues)
            {
                dynamic d1 = a1;
                if ((d1 || true) != (a1 || true))
                {
                    System.Console.WriteLine("Failed -- bool || bool");
                    return false;
                }
            }

            return true;
        }

        private static bool Test2()
        {
            bool[] boolValues = new bool[]
            {
            true, false
            }

            ;
            foreach (bool a1 in boolValues)
            {
                dynamic d1 = a1;
                if ((false || d1) != (false || a1))
                {
                    System.Console.WriteLine("Failed -- bool || bool");
                    return false;
                }
            }

            return true;
        }

        private static bool Test3()
        {
            bool?[] boolValues = new bool?[]
            {
            true, false
            }

            ;
            foreach (bool? a1 in boolValues)
            {
                dynamic d1 = a1; // should work as bool, dynamic as boxed bool
                if ((false || d1) != (false || a1.Value))
                {
                    System.Console.WriteLine("Failed -- bool? || bool?");
                    return false;
                }
            }

            return true;
        }

        private static bool Test3a()
        {
            bool?[] boolValues = new bool?[]
            {
            null
            }

            ;
            foreach (bool? a1 in boolValues)
            {
                dynamic d1 = a1;
                dynamic d = false || d1; // B#567888
                                         // System.Console.WriteLine("Failed -- bool? || bool? Neg");
                                         // return false;
            }

            return true;
        }

        private static bool Test4()
        {
            bool?[] boolValues = new bool?[]
            {
            false
            }

            ;
            foreach (bool? a1 in boolValues)
            {
                dynamic d1 = a1;
                dynamic d = d1 || null; // B#567888
                                        // System.Console.WriteLine("Failed -- bool? || null Neg");
            }

            return true;
        }

        private static bool Test4a()
        {
            bool?[] boolValues = new bool?[]
            {
            true
            }

            ;
            foreach (bool? a1 in boolValues)
            {
                dynamic d1 = a1;
                if (!(d1 || null))
                {
                    System.Console.WriteLine("Failed -- bool? || null");
                    return false;
                }
            }

            return true;
        }

        private static bool Test5()
        {
            bool[] boolValues = new bool[]
            {
            true, false
            }

            ;
            foreach (bool a1 in boolValues)
            {
                dynamic d1 = a1;
                if ((d1 && false) != (a1 && false))
                {
                    System.Console.WriteLine("Failed -- bool && bool");
                    return false;
                }
            }

            return true;
        }

        private static bool Test6()
        {
            bool[] boolValues = new bool[]
            {
            true, false
            }

            ;
            foreach (bool a1 in boolValues)
            {
                dynamic d1 = a1;
                if ((true && a1) != (true && d1))
                {
                    System.Console.WriteLine("Failed -- bool && bool");
                    return false;
                }
            }

            return true;
        }

        private static bool Test7()
        {
            bool?[] boolValues = new bool?[]
            {
            true, false
            }

            ;
            foreach (bool? a1 in boolValues)
            {
                dynamic d1 = a1; // should work as bool, dynamic as boxed bool
                if ((d1 && true) != (a1.Value && true))
                {
                    System.Console.WriteLine("Failed -- bool? && bool?");
                    return false;
                }
            }

            return true;
        }

        private static bool Test7a()
        {
            bool?[] boolValues = new bool?[]
            {
            null
            }

            ;
            foreach (bool? a1 in boolValues)
            {
                dynamic d1 = a1;
                try
                {
                    dynamic d = d1 && true;
                    System.Console.WriteLine("Failed -- bool? && bool? Neg");
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
                {
                    if (ErrorVerifier.Verify(ErrorMessageId.ValueCantBeNull, e.Message, "bool"))
                        return true;
                }
            }

            return false;
        }

        private static bool Test8()
        {
            bool?[] boolValues = new bool?[]
            {
            true
            }

            ;
            foreach (bool? a1 in boolValues)
            {
                dynamic d1 = a1;
                dynamic d = d1 && null; // B#567888
                                        // System.Console.WriteLine("Failed -- bool? && null Neg");
            }

            return true;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.operate009.operate009
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.common.common;
    // <Title>Null coalescing ??</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public enum MyEnum
        {
            First,
            Second,
            Third
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(Test1);
            result += Verify.Eval(Test2);
            result += Verify.Eval(Test3);
            return result;
        }

        private static bool Test1()
        {
            string[] stringValues = new string[]
            {
            string.Empty, "ABC", null
            }

            ;
            foreach (string a1 in stringValues)
            {
                foreach (string a2 in stringValues)
                {
                    dynamic d1 = a1;
                    dynamic d2 = a2;
                    if ((d1 ?? d2) != (a1 ?? a2))
                    {
                        System.Console.WriteLine("Failed -- string ?? string");
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool Test2()
        {
            int?[] intNValues = new int?[]
            {
            int.MaxValue, int.MinValue, 0, null
            }

            ;
            int[] intValues = new int[]
            {
            int.MaxValue, int.MinValue, 0
            }

            ;
            foreach (int? a1 in intNValues)
            {
                foreach (int a2 in intValues)
                {
                    dynamic d1 = a1;
                    int d2 = a2;
                    if ((d1 ?? d2) != (a1 ?? a2))
                    {
                        System.Console.WriteLine("Failed -- int? ?? int ");
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool Test3()
        {
            MyEnum?[] enumNValues = new MyEnum?[]
            {
            MyEnum.First, MyEnum.Second, MyEnum.Third, null
            }

            ;
            MyEnum[] enumValues = new MyEnum[]
            {
            MyEnum.First, MyEnum.Second, MyEnum.Third
            }

            ;
            foreach (MyEnum? a1 in enumNValues)
            {
                foreach (MyEnum a2 in enumValues)
                {
                    MyEnum? d1 = a1;
                    dynamic d2 = a2;
                    if ((d1 ?? d2) != (a1 ?? a2))
                    {
                        System.Console.WriteLine("Failed -- enum? ?? enum ");
                        return false;
                    }
                }
            }

            return true;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.operate009a.operate009a
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.common.common;
    // <Title>Null coalescing ??</Title>
    // <Description>
    // dynamic op literals
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public enum MyEnum
        {
            First,
            Second,
            Third
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(Test1);
            result += Verify.Eval(Test1a);
            result += Verify.Eval(Test2);
            result += Verify.Eval(Test3);
            result += Verify.Eval(Test4);
            result += Verify.Eval(Test5);
            return result;
        }

        private static bool Test1()
        {
            string[] stringValues = new string[]
            {
            string.Empty, "ABC", null
            }

            ;
            foreach (string a1 in stringValues)
            {
                dynamic d1 = a1;
                if ((d1 ?? null) != (a1 ?? null))
                {
                    System.Console.WriteLine("Failed -- string ?? null");
                    return false;
                }
            }

            return true;
        }

        private static bool Test1a()
        {
            string[] stringValues = new string[]
            {
            string.Empty, "ABC", null
            }

            ;
            foreach (string a1 in stringValues)
            {
                dynamic d1 = a1;
                if ((d1 ?? string.Empty) != (a1 ?? string.Empty))
                {
                    System.Console.WriteLine("Failed -- string ?? string");
                    return false;
                }
            }

            return true;
        }

        private static bool Test2()
        {
            string[] stringValues = new string[]
            {
            string.Empty, "ABC", null
            }

            ;
            foreach (string a1 in stringValues)
            {
                dynamic d1 = a1;
                if ((string.Empty ?? d1) != (string.Empty ?? a1))
                {
                    System.Console.WriteLine("Failed -- string ?? string");
                    return false;
                }
            }

            return true;
        }

        private static bool Test3()
        {
            string[] stringValues = new string[]
            {
            string.Empty, "ABC", null
            }

            ;
            foreach (string a1 in stringValues)
            {
                dynamic d1 = 10;
                if ((string.Empty ?? d1) != (string.Empty ?? a1))
                {
                    System.Console.WriteLine("Failed -- string ?? string");
                    return false;
                }
            }

            return true;
        }

        private static bool Test4()
        {
            int?[] intNValues = new int?[]
            {
            int.MaxValue, int.MinValue, 0, null
            }

            ;
            foreach (int? a1 in intNValues)
            {
                dynamic d1 = a1;
                if ((d1 ?? 0) != (a1 ?? 0))
                {
                    System.Console.WriteLine("Failed -- int? ?? int ");
                    return false;
                }
            }

            return true;
        }

        private static bool Test5()
        {
            MyEnum?[] enumNValues = new MyEnum?[]
            {
            MyEnum.First, MyEnum.Second, MyEnum.Third, null
            }

            ;
            MyEnum[] enumValues = new MyEnum[]
            {
            MyEnum.First, MyEnum.Second, MyEnum.Third
            }

            ;
            foreach (MyEnum? a1 in enumNValues)
            {
                dynamic d1 = a1;
                if ((d1 ?? MyEnum.Third) != (a1 ?? MyEnum.Third))
                {
                    System.Console.WriteLine("Failed -- enum? ?? enum ");
                    return false;
                }
            }

            return true;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.operate010.operate010
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.common.common;
    // <Title>Conditional operator ? :</Title>
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
            int result = 0;
            result += Verify.Eval(Test1);
            result += Verify.Eval(Test2);
            result += Verify.Eval(Test3);
            result += Verify.Eval(Test4);
            result += Verify.Eval(Test5);
            result += Verify.Eval(Test6);
            result += Verify.Eval(Test7);
            result += Verify.Eval(Test8);
            return result;
        }

        private static bool Test1()
        {
            bool[] boolValues = new bool[]
            {
            true, false
            }

            ;
            string[] stringValues = new string[]
            {
            string.Empty, "ABC", null
            }

            ;
            foreach (bool a1 in boolValues)
            {
                foreach (string a2 in stringValues)
                {
                    foreach (string a3 in stringValues)
                    {
                        dynamic d1 = a1;
                        string d2 = a2;
                        string d3 = a3;
                        if ((d1 ? d2 : d3) != (a1 ? a2 : a3))
                        {
                            System.Console.WriteLine("Failed -- bool ? string : string");
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private static bool Test2()
        {
            bool[] boolValues = new bool[]
            {
            true, false
            }

            ;
            string[] stringValues = new string[]
            {
            string.Empty, "ABC", null
            }

            ;
            foreach (bool a1 in boolValues)
            {
                foreach (string a2 in stringValues)
                {
                    foreach (string a3 in stringValues)
                    {
                        bool d1 = a1;
                        dynamic d2 = a2;
                        string d3 = a3;
                        if ((d1 ? d2 : d3) != (a1 ? a2 : a3))
                        {
                            System.Console.WriteLine("Failed -- bool ? string : string");
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private static bool Test3()
        {
            bool[] boolValues = new bool[]
            {
            true, false
            }

            ;
            string[] stringValues = new string[]
            {
            string.Empty, "ABC", null
            }

            ;
            foreach (bool a1 in boolValues)
            {
                foreach (string a2 in stringValues)
                {
                    foreach (string a3 in stringValues)
                    {
                        bool d1 = a1;
                        string d2 = a2;
                        dynamic d3 = a3;
                        if ((d1 ? d2 : d3) != (a1 ? a2 : a3))
                        {
                            System.Console.WriteLine("Failed -- bool ? string : string");
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private static bool Test4()
        {
            bool[] boolValues = new bool[]
            {
            true, false
            }

            ;
            string[] stringValues = new string[]
            {
            string.Empty, "ABC", null
            }

            ;
            foreach (bool a1 in boolValues)
            {
                foreach (string a2 in stringValues)
                {
                    foreach (string a3 in stringValues)
                    {
                        dynamic d1 = a1;
                        dynamic d2 = a2;
                        dynamic d3 = a3;
                        if ((d1 ? d2 : d3) != (a1 ? a2 : a3))
                        {
                            System.Console.WriteLine("Failed -- bool ? string : string");
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private static bool Test5()
        {
            bool[] boolValues = new bool[]
            {
            true, false
            }

            ;
            decimal[] longValues = new decimal[]
            {
            decimal.MaxValue, decimal.MinValue, 0M
            }

            ;
            foreach (bool a1 in boolValues)
            {
                foreach (decimal a2 in longValues)
                {
                    foreach (decimal a3 in longValues)
                    {
                        dynamic d1 = a1;
                        decimal d2 = a2;
                        decimal d3 = a3;
                        if ((d1 ? d2 : d3) != (a1 ? a2 : a3))
                        {
                            System.Console.WriteLine("Failed -- bool ? decimal : decimal");
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private static bool Test6()
        {
            bool[] boolValues = new bool[]
            {
            true, false
            }

            ;
            long?[] longNValues = new long?[]
            {
            long.MaxValue, long.MinValue, null
            }

            ;
            foreach (bool a1 in boolValues)
            {
                foreach (long? a2 in longNValues)
                {
                    foreach (long? a3 in longNValues)
                    {
                        bool d1 = a1;
                        dynamic d2 = a2;
                        long? d3 = a3;
                        if ((d1 ? d2 : d3) != (a1 ? a2 : a3))
                        {
                            System.Console.WriteLine("Failed -- bool ? Nullable<long> : Nullable<long>");
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private static bool Test7()
        {
            bool[] boolValues = new bool[]
            {
            true, false
            }

            ;
            Guid[] guidValues = new Guid[]
            {
            Guid.NewGuid(), Guid.NewGuid(), default (Guid)}

            ;
            foreach (bool a1 in boolValues)
            {
                foreach (Guid a2 in guidValues)
                {
                    foreach (Guid a3 in guidValues)
                    {
                        bool d1 = a1;
                        Guid d2 = a2;
                        dynamic d3 = a3;
                        if ((d1 ? d2 : d3) != (a1 ? a2 : a3))
                        {
                            System.Console.WriteLine("Failed -- bool ? Guid : Guid");
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private static bool Test8()
        {
            bool[] boolValues = new bool[]
            {
            true, false
            }

            ;
            Guid?[] guidNValues = new Guid?[]
            {
            Guid.NewGuid(), default (Guid), null
            }

            ;
            foreach (bool a1 in boolValues)
            {
                foreach (Guid? a2 in guidNValues)
                {
                    foreach (Guid? a3 in guidNValues)
                    {
                        dynamic d1 = a1;
                        dynamic d2 = a2;
                        dynamic d3 = a3;
                        if ((d1 ? d2 : d3) != (a1 ? a2 : a3))
                        {
                            System.Console.WriteLine("Failed -- bool ? Nullable<Guid> : Nullable<Guid>");
                            return false;
                        }
                    }
                }
            }

            return true;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.operate010a.operate010a
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.common.common;
    // <Title>Conditional operator ? :</Title>
    // <Description>
    // dynamic op literals
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
            int result = 0;
            result += Verify.Eval(Test1);
            result += Verify.Eval(Test2);
            result += Verify.Eval(Test3);
            result += Verify.Eval(Test3a);
            result += Verify.Eval(Test4);
            result += Verify.Eval(Test5);
            result += Verify.Eval(Test6);
            result += Verify.Eval(Test7);
            result += Verify.Eval(Test8);
            return result;
        }

        private static bool Test1()
        {
            bool[] boolValues = new bool[]
            {
            true, false
            }

            ;
            string[] stringValues = new string[]
            {
            string.Empty, "ABC", null
            }

            ;
            foreach (bool a1 in boolValues)
            {
                foreach (string a3 in stringValues)
                {
                    dynamic d1 = a1;
                    string d3 = a3;
                    if ((d1 ? null : d3) != (a1 ? null : a3))
                    {
                        System.Console.WriteLine("Failed -- bool ? string : string");
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool Test2()
        {
            bool[] boolValues = new bool[]
            {
            true, false
            }

            ;
            string[] stringValues = new string[]
            {
            string.Empty, "ABC", null
            }

            ;
            foreach (bool a1 in boolValues)
            {
                foreach (string a2 in stringValues)
                {
                    bool d1 = a1;
                    dynamic d2 = a2;
                    if ((d1 ? d2 : string.Empty) != (a1 ? a2 : string.Empty))
                    {
                        System.Console.WriteLine("Failed -- bool ? string : string");
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool Test3()
        {
            bool[] boolValues = new bool[]
            {
            true, false
            }

            ;
            string[] stringValues = new string[]
            {
            string.Empty, "ABC", null
            }

            ;
            foreach (string a2 in stringValues)
            {
                foreach (string a3 in stringValues)
                {
                    string d2 = a2;
                    dynamic d3 = a3;
                    if ((false ? d2 : d3) != (false ? a2 : a3))
                    {
                        System.Console.WriteLine("Failed -- bool ? string : string");
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool Test3a()
        {
            bool[] boolValues = new bool[]
            {
            true, false
            }

            ;
            string[] stringValues = new string[]
            {
            string.Empty, "ABC", null
            }

            ;
            foreach (string a2 in stringValues)
            {
                foreach (string a3 in stringValues)
                {
                    bool d1 = true;
                    string d2 = a2;
                    dynamic d3 = 100;
                    if ((d1 ? "ABC" : d3) != (d1 ? "ABC" : a3))
                    {
                        System.Console.WriteLine("Failed -- bool ? string : string");
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool Test4()
        {
            bool[] boolValues = new bool[]
            {
            true, false
            }

            ;
            string[] stringValues = new string[]
            {
            string.Empty, "ABC", null
            }

            ;
            foreach (bool a1 in boolValues)
            {
                foreach (string a3 in stringValues)
                {
                    dynamic d1 = a1;
                    dynamic d3 = a3;
                    if ((d1 ? "ABCD" : d3) != (a1 ? "ABCD" : a3))
                    {
                        System.Console.WriteLine("Failed -- bool ? string : string");
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool Test5()
        {
            bool[] boolValues = new bool[]
            {
            true, false
            }

            ;
            decimal[] longValues = new decimal[]
            {
            decimal.MaxValue, decimal.MinValue, 0M
            }

            ;
            foreach (bool a1 in boolValues)
            {
                foreach (decimal a2 in longValues)
                {
                    dynamic d1 = a1;
                    decimal d2 = a2;
                    if ((d1 ? d2 : 10.01M) != (a1 ? a2 : 10.01M))
                    {
                        System.Console.WriteLine("Failed -- bool ? decimal : decimal");
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool Test6()
        {
            bool[] boolValues = new bool[]
            {
            true, false
            }

            ;
            long?[] longNValues = new long?[]
            {
            long.MaxValue, long.MinValue, null
            }

            ;
            foreach (bool a1 in boolValues)
            {
                foreach (long? a2 in longNValues)
                {
                    bool d1 = a1;
                    dynamic d2 = a2;
                    if ((d1 ? d2 : 10L) != (a1 ? a2 : 10L))
                    {
                        System.Console.WriteLine("Failed -- bool ? Nullable<long> : Nullable<long>");
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool Test7()
        {
            bool[] boolValues = new bool[]
            {
            true, false
            }

            ;
            Guid[] guidValues = new Guid[]
            {
            Guid.NewGuid(), Guid.NewGuid(), default (Guid)}

            ;
            foreach (bool a1 in boolValues)
            {
                foreach (Guid a3 in guidValues)
                {
                    bool d1 = a1;
                    dynamic d3 = a3;
                    if ((d1 ? default(Guid) : d3) != (a1 ? default(Guid) : a3))
                    {
                        System.Console.WriteLine("Failed -- bool ? Guid : Guid");
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool Test8()
        {
            bool[] boolValues = new bool[]
            {
            true, false
            }

            ;
            Guid?[] guidNValues = new Guid?[]
            {
            Guid.NewGuid(), default (Guid), null
            }

            ;
            foreach (bool a1 in boolValues)
            {
                foreach (Guid? a2 in guidNValues)
                {
                    dynamic d1 = a1;
                    dynamic d2 = a2;
                    if ((d1 ? d2 : null) != (a1 ? a2 : null))
                    {
                        System.Console.WriteLine("Failed -- bool ? Nullable<Guid> : Nullable<Guid>");
                        return false;
                    }
                }
            }

            return true;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.operate011.operate011
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.common.common;
    // <Title>Equality operator</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System;

    public class Test
    {
        public enum MyEnum
        {
            First,
            Second,
            Third
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(Test1);
            result += Verify.Eval(Test2);
            result += Verify.Eval(Test3);
            result += Verify.Eval(Test4);
            result += Verify.Eval(Test5);
            result += Verify.Eval(Test6);
            result += Verify.Eval(Test7);
            result += Verify.Eval(Test8);
            return result;
        }

        private static bool Test1()
        {
            string[] stringValues = new string[]
            {
            string.Empty, "ABC", null
            }

            ;
            foreach (string a1 in stringValues)
            {
                dynamic d1 = a1;
                if (d1 != a1)
                {
                    System.Console.WriteLine("Failed -- string Equality1");
                    return false;
                }
            }

            return true;
        }

        private static bool Test2()
        {
            int?[] intNValues = new int?[]
            {
            int.MaxValue, int.MinValue, 0, null
            }

            ;
            foreach (int? a1 in intNValues)
            {
                dynamic d1 = a1;
                dynamic d2 = a1;
                if (d1 != d2)
                {
                    System.Console.WriteLine("Failed -- Nullable<int> Equality2");
                    return false;
                }
            }

            return true;
        }

        private static bool Test3()
        {
            Guid[] guidValues = new Guid[]
            {
            Guid.NewGuid(), Guid.NewGuid(), default (Guid)}

            ;
            for (int i = 0; i < guidValues.Length; i++)
            {
                for (int j = 0; j < guidValues.Length; j++)
                {
                    if (i != j)
                    {
                        dynamic d1 = guidValues[i];
                        if (d1 == guidValues[j])
                        {
                            System.Console.WriteLine("Failed -- Guid Equality3");
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private static bool Test4()
        {
            decimal[] decimalValues = new decimal[]
            {
            decimal.MaxValue, decimal.MinusOne, 0M
            }

            ;
            for (int i = 0; i < decimalValues.Length; i++)
            {
                for (int j = 0; j < decimalValues.Length; j++)
                {
                    if (i != j)
                    {
                        dynamic d1 = decimalValues[i];
                        if (decimalValues[j] == d1)
                        {
                            System.Console.WriteLine("Failed -- decimal Equality4");
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private static bool Test5()
        {
            DateTime[] datetimeValues = new DateTime[]
            {
            DateTime.Now.AddHours(10.00), DateTime.Now.AddHours(5.00), DateTime.Now
            }

            ;
            foreach (DateTime a1 in datetimeValues)
            {
                dynamic d1 = a1;
                if (d1 != a1)
                {
                    System.Console.WriteLine("Failed -- DateTime Equality5");
                    return false;
                }
            }

            return true;
        }

        private static bool Test6()
        {
            long[] longValues = new long[]
            {
            long.MaxValue, long.MinValue, 0
            }

            ;
            foreach (long a1 in longValues)
            {
                dynamic d1 = a1;
                dynamic d2 = a1;
                if (d1 != d2)
                {
                    System.Console.WriteLine("Failed -- long Equality6");
                    return false;
                }
            }

            return true;
        }

        private static bool Test7()
        {
            MyEnum[] enumValues = new MyEnum[]
            {
            MyEnum.First, MyEnum.Second, MyEnum.Third
            }

            ;
            for (int i = 0; i < enumValues.Length; i++)
            {
                for (int j = 0; j < enumValues.Length; j++)
                {
                    if (i != j)
                    {
                        dynamic d1 = enumValues[i];
                        if (d1 == enumValues[j])
                        {
                            System.Console.WriteLine("Failed -- enum Equality7");
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private static bool Test8()
        {
            MyEnum?[] enumValues = new MyEnum?[]
            {
            MyEnum.First, MyEnum.Second, MyEnum.Third, null
            }

            ;
            for (int i = 0; i < enumValues.Length; i++)
            {
                for (int j = 0; j < enumValues.Length; j++)
                {
                    if (i != j)
                    {
                        dynamic d1 = enumValues[i];
                        if (enumValues[j] == d1)
                        {
                            System.Console.WriteLine("Failed -- Nullable<enum> Equality8");
                            return false;
                        }
                    }
                }
            }

            return true;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.operate011b.operate011b
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.common.common;
    // <Title>Equality operator when one operand is literal null</Title>
    // <Description> 
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using Microsoft.CSharp.RuntimeBinder;

    #region "Type definition"
    public enum MyEnum
    {
        First,
    }

    public struct MyStruct
    {
    }

    public struct MyStruct2
    {
        private int _f1;
        public MyStruct2(int p1)
        {
            _f1 = p1;
        }

        public static bool operator ==(MyStruct2 lf, MyStruct2 rh)
        {
            return lf._f1 == rh._f1;
        }

        public static bool operator !=(MyStruct2 lf, MyStruct2 rh)
        {
            return lf._f1 != rh._f1;
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
    #endregion
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
            // int type
            result += Verify.Eval(Test1);
            result += Verify.Eval(Test2);
            result += Verify.Eval(Test3);
            result += Verify.Eval(Test4);
            // argument
            result += Verify.Eval(Test5);
            // enum type
            result += Verify.Eval(Test11);
            result += Verify.Eval(Test12);
            result += Verify.Eval(Test13);
            result += Verify.Eval(Test14);
            // struct type without user defined equality operators
            result += Verify.Eval(Test21);
            result += Verify.Eval(Test22);
            result += Verify.Eval(Test23);
            result += Verify.Eval(Test24);
            // type parameter
            result += Verify.Eval(Test31<long>);
            result += Verify.Eval(Test32<long>);
            result += Verify.Eval(Test33<long>);
            result += Verify.Eval(Test34<long>);
            // struct with user defined equality operators
            result += Verify.Eval(Test41);
            result += Verify.Eval(Test42);
            result += Verify.Eval(Test43);
            result += Verify.Eval(Test44);
            // nullable type without user defined equality operators
            result += Verify.Eval(Test51);
            result += Verify.Eval(Test52);
            result += Verify.Eval(Test53);
            result += Verify.Eval(Test54);
            result += Verify.Eval(Test55);
            // nullable type with user defined equality operators
            result += Verify.Eval(Test61);
            result += Verify.Eval(Test62);
            result += Verify.Eval(Test63);
            result += Verify.Eval(Test64);
            return result;
        }

        #region "int type"
        private static bool Test1()
        {
            dynamic d = 10;
            if (d == null)
                return false;
            return true;
        }

        private static bool Test2()
        {
            dynamic d = 10;
            if (d != null)
                return true;
            return false;
        }

        private static bool Test3()
        {
            dynamic d = 10;
            if (null == d)
                return false;
            return true;
        }

        private static bool Test4()
        {
            dynamic d = 10;
            if (null != d)
                return true;
            return false;
        }

        #endregion
        public bool M5(dynamic p1)
        {
            if (p1 == null)
                return false;
            else
                return true;
        }

        private static bool Test5()
        {
            dynamic obj = new Test();
            return obj.M5(10);
        }

        #region "Enum type"
        private static bool Test11()
        {
            dynamic d = MyEnum.First;
            if (d == null)
                return false;
            return true;
        }

        private static bool Test12()
        {
            dynamic d = MyEnum.First;
            if (d != null)
                return true;
            return false;
        }

        private static bool Test13()
        {
            dynamic d = MyEnum.First;
            if (null == d)
                return false;
            return true;
        }

        private static bool Test14()
        {
            dynamic d = MyEnum.First;
            if (null != d)
                return true;
            return false;
        }

        #endregion
        #region "struct type without user defined equality operators. "

        private static bool Test21()
        {
            dynamic d = new MyStruct();
            try
            {
                if (d == null)
                    return false;
            }
            catch (RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "==", "MyStruct", ErrorVerifier.GetErrorElement(ErrorElementId.NULL)))
                    return true;
            }

            return false;
        }

        private static bool Test22()
        {
            dynamic d = new MyStruct();
            try
            {
                if (d != null)
                    return false;
            }
            catch (RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "!=", "MyStruct", ErrorVerifier.GetErrorElement(ErrorElementId.NULL)))
                    return true;
            }

            return false;
        }

        private static bool Test23()
        {
            dynamic d = new MyStruct();
            try
            {
                if (null == d)
                    return false;
            }
            catch (RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "==", ErrorVerifier.GetErrorElement(ErrorElementId.NULL), "MyStruct"))
                    return true;
            }

            return false;
        }

        private static bool Test24()
        {
            dynamic d = new MyStruct();
            try
            {
                if (null != d)
                    return false;
            }
            catch (RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "!=", ErrorVerifier.GetErrorElement(ErrorElementId.NULL), "MyStruct"))
                    return true;
            }

            return false;
        }

        #endregion
        #region "Type parameter"
        private static bool Test31<T>() where T : struct
        {
            dynamic d = default(T);
            if (d == null)
                return false;
            return true;
        }

        private static bool Test32<T>() where T : struct
        {
            dynamic d = default(T);
            if (d != null)
                return true;
            return false;
        }

        private static bool Test33<T>() where T : struct
        {
            dynamic d = default(T);
            if (null == d)
                return false;
            return true;
        }

        private static bool Test34<T>() where T : struct
        {
            dynamic d = default(T);
            if (null != d)
                return true;
            return false;
        }

        #endregion
        #region "Struct type with user defined equality operators"

        private static bool Test41()
        {
            dynamic d = new MyStruct2(1);
            if (d == null)
                return false;
            return true;
        }

        private static bool Test42()
        {
            dynamic d = new MyStruct2(1);
            if (d != null)
                return true;
            return false;
        }

        private static bool Test43()
        {
            dynamic d = new MyStruct2(1);
            if (null == d)
                return false;
            return true;
        }

        private static bool Test44()
        {
            dynamic d = new MyStruct2(1);
            if (null != d)
                return true;
            return false;
        }

        #endregion
        #region "Nullable struct without user-defined equality operators"
        private static bool Test51()
        {
            MyStruct? ns = new MyStruct();
            dynamic d = ns;
            try
            {
                if (d == null)
                    return false;
            }
            catch (RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "==", "MyStruct", ErrorVerifier.GetErrorElement(ErrorElementId.NULL)))
                    return true;
            }

            return false;
        }

        private static bool Test52()
        {
            MyStruct? ns = new MyStruct();
            dynamic d = ns;
            try
            {
                if (d != null)
                    return false;
            }
            catch (RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "!=", "MyStruct", ErrorVerifier.GetErrorElement(ErrorElementId.NULL)))
                    return true;
            }

            return false;
        }

        private static bool Test53()
        {
            MyStruct? ns = new MyStruct();
            dynamic d = ns;
            try
            {
                if (null == d)
                    return false;
            }
            catch (RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "==", ErrorVerifier.GetErrorElement(ErrorElementId.NULL), "MyStruct"))
                    return true;
            }

            return false;
        }

        private static bool Test54()
        {
            MyStruct? ns = new MyStruct();
            dynamic d = ns;
            try
            {
                if (null != d)
                    return false;
            }
            catch (RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBinaryOps, e.Message, "!=", ErrorVerifier.GetErrorElement(ErrorElementId.NULL), "MyStruct"))
                    return true;
            }

            return false;
        }

        private static bool Test55()
        {
            MyStruct? ns = null;
            dynamic d = ns;
            if (d == null)
                return true;
            return false;
        }

        #endregion
        #region "Nullable type with user defined equality operators"

        private static bool Test61()
        {
            MyStruct2? ns = new MyStruct2(1);
            dynamic d = ns;
            if (d == null)
                return false;
            return true;
        }

        private static bool Test62()
        {
            MyStruct2? ns = new MyStruct2(1);
            dynamic d = ns;
            if (d != null)
                return true;
            return false;
        }

        private static bool Test63()
        {
            MyStruct2? ns = new MyStruct2(1);
            dynamic d = ns;
            if (null == d)
                return false;
            return true;
        }

        private static bool Test64()
        {
            MyStruct2? ns = new MyStruct2(1);
            dynamic d = ns;
            if (null != d)
                return true;
            return false;
        }
        #endregion
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.operate012.operate012
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.common.common;
    // <Title>Relational operator</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public enum MyEnum
        {
            First,
            Second,
            Third
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(Test1);
            result += Verify.Eval(Test2);
            result += Verify.Eval(Test3);
            result += Verify.Eval(Test4);
            return result;
        }

        private static bool Test1()
        {
            long[] longValues = new long[]
            {
            long.MinValue, long.MinValue, 0
            }

            ;
            foreach (long a1 in longValues)
            {
                foreach (long a2 in longValues)
                {
                    dynamic d1 = a1;
                    if ((d1 < a2) != (a1 < a2))
                    {
                        System.Console.WriteLine("Failed -- long < long");
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool Test2()
        {
            decimal?[] decimalNValues = new decimal?[]
            {
            decimal.MaxValue, int.MinValue, default (decimal), null
            }

            ;
            foreach (decimal? a1 in decimalNValues)
            {
                foreach (decimal? a2 in decimalNValues)
                {
                    dynamic d2 = a2;
                    if ((a1 > d2) != (a1 > a2))
                    {
                        System.Console.WriteLine("Failed -- decimal? > decimal?");
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool Test3()
        {
            char?[] charNValues = new char?[]
            {
            'a', 'b', default (char), null
            }

            ;
            foreach (char? a1 in charNValues)
            {
                foreach (char? a2 in charNValues)
                {
                    dynamic d1 = a1;
                    dynamic d2 = a2;
                    if ((d1 <= d2) != (a1 <= a2))
                    {
                        System.Console.WriteLine("Failed -- char? <= char?");
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool Test4()
        {
            MyEnum[] enumValues = new MyEnum[]
            {
            MyEnum.First, MyEnum.Second, MyEnum.Third
            }

            ;
            foreach (MyEnum a1 in enumValues)
            {
                foreach (MyEnum a2 in enumValues)
                {
                    dynamic d2 = a2;
                    if ((a1 >= d2) != (a1 >= a2))
                    {
                        System.Console.WriteLine("Failed -- enum >= enum");
                        return false;
                    }
                }
            }

            return true;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.operate012a.operate012a
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.common.common;
    // <Title>Relational operator</Title>
    // <Description>
    // dynamic op literals
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(69,33\).*CS0464</Expects>

    public class Test
    {
        public enum MyEnum
        {
            First,
            Second,
            Third
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(Test1);
            result += Verify.Eval(Test2);
            result += Verify.Eval(Test3);
            result += Verify.Eval(Test4);
            return result;
        }

        private static bool Test1()
        {
            long[] longValues = new long[]
            {
            long.MinValue, long.MinValue, 0
            }

            ;
            foreach (long a1 in longValues)
            {
                dynamic d1 = a1;
                if ((d1 < 10) != (a1 < 10))
                {
                    System.Console.WriteLine("Failed -- long < long");
                    return false;
                }
            }

            return true;
        }

        private static bool Test2()
        {
            decimal?[] decimalNValues = new decimal?[]
            {
            decimal.MaxValue, int.MinValue, default (decimal), null
            }

            ;
            foreach (decimal? a2 in decimalNValues)
            {
                dynamic d2 = a2;
                if ((10M > d2) != (10M > a2))
                {
                    System.Console.WriteLine("Failed -- decimal? > decimal?");
                    return false;
                }
            }

            return true;
        }

        private static bool Test2a()
        {
            decimal?[] decimalNValues = new decimal?[]
            {
            decimal.MaxValue, int.MinValue, default (decimal), null
            }

            ;
            foreach (decimal? a2 in decimalNValues)
            {
                dynamic d2 = a2;
                if ((d2 > null) != (a2 > null))
                {
                    System.Console.WriteLine("Failed -- decimal? > decimal?");
                    return false;
                }
            }

            return true;
        }

        private static bool Test3()
        {
            char?[] charNValues = new char?[]
            {
            'a', 'b', default (char), null
            }

            ;
            foreach (char? a2 in charNValues)
            {
                dynamic d2 = a2;
                if ((default(char) <= d2) != (default(char) <= a2))
                {
                    System.Console.WriteLine("Failed -- char? <= char?");
                    return false;
                }
            }

            return true;
        }

        private static bool Test4()
        {
            MyEnum[] enumValues = new MyEnum[]
            {
            MyEnum.First, MyEnum.Second, MyEnum.Third
            }

            ;
            foreach (MyEnum a1 in enumValues)
            {
                dynamic d1 = a1;
                if ((a1 >= MyEnum.Second) != (d1 >= MyEnum.Second))
                {
                    System.Console.WriteLine("Failed -- enum >= enum");
                    return false;
                }
            }

            return true;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.operate012b.operate012b
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.common.common;
    // <Title>Relational operator</Title>
    // <Description>
    // dynamic op literals
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(94,43\).*CS0464</Expects>

    public class Test
    {
        public enum MyEnum
        {
            First,
            Second,
            Third
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(Test1);
            result += Verify.Eval(Test2);
            result += Verify.Eval(Test3);
            result += Verify.Eval(Test4);
            return result;
        }

        public static long Method(long l)
        {
            return l;
        }

        public dynamic dField = default(dynamic);
        public dynamic DPro
        {
            get
            {
                return dField;
            }
        }

        public char this[char? c]
        {
            get
            {
                return c ?? 'a';
            }
        }

        public dynamic Method(object d)
        {
            return d;
        }

        private static bool Test1()
        {
            long[] longValues = new long[]
            {
            long.MinValue, long.MinValue, 0
            }

            ;
            foreach (long a1 in longValues)
            {
                dynamic d1 = a1;
                if ((Method(d1) < 10) != (a1 < 10))
                {
                    System.Console.WriteLine("Failed -- long < long");
                    return false;
                }
            }

            return true;
        }

        private static bool Test2()
        {
            decimal?[] decimalNValues = new decimal?[]
            {
            decimal.MaxValue, int.MinValue, default (decimal), null
            }

            ;
            foreach (decimal? a2 in decimalNValues)
            {
                dynamic d2 = a2;
                dynamic t = new Test();
                t.dField = d2;
                if ((10M > t.DPro) != (10M > a2))
                {
                    System.Console.WriteLine("Failed -- decimal? > decimal?");
                    return false;
                }
            }

            return true;
        }

        private static bool Test2a()
        {
            decimal?[] decimalNValues = new decimal?[]
            {
            decimal.MaxValue, int.MinValue, default (decimal), null
            }

            ;
            foreach (decimal? a2 in decimalNValues)
            {
                dynamic d2 = a2;
                dynamic t = new Test();
                if ((t.Method(d2) > null) != (a2 > null))
                {
                    System.Console.WriteLine("Failed -- decimal? > decimal?");
                    return false;
                }
            }

            return true;
        }

        private static bool Test3()
        {
            char?[] charNValues = new char?[]
            {
            'a', 'b', default (char), null
            }

            ;
            foreach (char? a2 in charNValues)
            {
                dynamic d2 = a2;
                dynamic d = new Test();
                if ((default(char) <= d[d2]) != (default(char) <= d[a2]))
                {
                    System.Console.WriteLine("Failed -- char? <= char?");
                    return false;
                }
            }

            return true;
        }

        private static bool Test4()
        {
            MyEnum[] enumValues = new MyEnum[]
            {
            MyEnum.First, MyEnum.Second, MyEnum.Third
            }

            ;
            foreach (MyEnum a1 in enumValues)
            {
                dynamic d1 = a1;
                dynamic t = new Test();
                t.dField = d1;
                if ((a1 >= MyEnum.Second) != (t.dField >= MyEnum.Second))
                {
                    System.Console.WriteLine("Failed -- enum >= enum");
                    return false;
                }
            }

            return true;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.operate013.operate013
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.common.common;
    // <Title>Arithmetic operator</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public enum MyEnum
        {
            First,
            Second,
            Third
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(Test1);
            result += Verify.Eval(Test2);
            result += Verify.Eval(Test3);
            result += Verify.Eval(Test4);
            result += Verify.Eval(Test5);
            result += Verify.Eval(Test6);
            result += Verify.Eval(Test7);
            ;
            return result;
        }

        private static bool Test1()
        {
            string[] stringValues = new string[]
            {
            string.Empty, "ABC", null
            }

            ;
            object[] objectValues = new object[]
            {
            null, 10, 10L, "10", MyEnum.First
            }

            ;
            foreach (string a1 in stringValues)
            {
                foreach (object a2 in objectValues)
                {
                    dynamic d2 = a2;
                    if ((a1 + d2) != (a1 + a2))
                    {
                        System.Console.WriteLine("Failed -- string + object");
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool Test2()
        {
            long?[] longNValues = new long?[]
            {
            10L, 30L, 0, null
            }

            ;
            foreach (long? a1 in longNValues)
            {
                foreach (long? a2 in longNValues)
                {
                    dynamic d1 = a1;
                    if ((d1 - a2) != (a1 - a2))
                    {
                        System.Console.WriteLine("Failed -- long? - long?");
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool Test3()
        {
            decimal[] decimalValues = new decimal[]
            {
            1M, 10.10M, 100.01M, 0M
            }

            ;
            foreach (decimal a1 in decimalValues)
            {
                foreach (decimal a2 in decimalValues)
                {
                    dynamic d1 = a1;
                    dynamic d2 = a2;
                    if ((d1 * d2) != (a1 * a2))
                    {
                        System.Console.WriteLine("Failed -- decimal * decimal");
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool Test4()
        {
            int[] intValues = new int[]
            {
            1, 2, 4, 99
            }

            ;
            foreach (int a1 in intValues)
            {
                foreach (int a2 in intValues)
                {
                    dynamic d1 = a1;
                    dynamic d2 = a2;
                    if ((d1 / d2) != (a1 / a2))
                    {
                        System.Console.WriteLine("Failed -- int / int");
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool Test5()
        {
            decimal?[] decimalNValues = new decimal?[]
            {
            1M, 10.10M, 100.01M, null
            }

            ;
            foreach (decimal? a1 in decimalNValues)
            {
                foreach (decimal? a2 in decimalNValues)
                {
                    dynamic d1 = a1;
                    if ((d1 % a2) != (a1 % a2))
                    {
                        System.Console.WriteLine("Failed -- decimal? % decimal?");
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool Test6()
        {
            byte[] byteValues = new byte[]
            {
            2, 30, 16, 9
            }

            ;
            foreach (byte a1 in byteValues)
            {
                foreach (byte a2 in byteValues)
                {
                    dynamic d2 = a2;
                    if ((a1 << d2) != (a1 << a2))
                    {
                        System.Console.WriteLine("Failed -- byte << byte");
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool Test7()
        {
            int?[] intNValues = new int?[]
            {
            null, 2, 4, 3
            }

            ;
            foreach (int? a1 in intNValues)
            {
                foreach (int? a2 in intNValues)
                {
                    dynamic d1 = a1;
                    if ((d1 >> a2) != (a1 >> a2))
                    {
                        System.Console.WriteLine("Failed -- Nullable<int> >> Nullable<int>");
                        return false;
                    }
                }
            }

            return true;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.operate013a.operate013a
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.common.common;
    // <Title>Arithmetic operator, Compound assignment</Title>
    // <Description>dynamic does NOT keep nullable info either non-nullable Type or null object
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public enum MyEnum
        {
            First,
            Second,
            Third
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(Test1);
            result += Verify.Eval(Test2);
            result += Verify.Eval(Test3);
            result += Verify.Eval(Test4);
            result += Verify.Eval(Test5);
            result += Verify.Eval(Test6);
            result += Verify.Eval(Test7);
            return result;
        }

        private static bool Test1()
        {
            string[] stringValues = new string[]
            {
            string.Empty, "ABC", null
            }

            ;
            object[] objectValues = new object[]
            {
            null, 10, 10L, "10", MyEnum.First
            }

            ;
            foreach (string a1 in stringValues)
            {
                foreach (object a2 in objectValues)
                {
                    var b11 = a1;
                    var b12 = a2;
                    var b21 = a1;
                    var b22 = a2;
                    dynamic d2 = a2;
                    if ((b11 += d2) != (b21 += b22))
                    {
                        System.Console.WriteLine("Failed -- string += object");
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool Test2()
        {
            long?[] longNValues = new long?[]
            {
            10L, 30L, 0, null
            }

            ;
            foreach (long? a1 in longNValues)
            {
                foreach (long? a2 in longNValues)
                {
                    var b11 = a1;
                    var b12 = a2;
                    var b21 = a1;
                    var b22 = a2;
                    dynamic d1 = a1;
                    // dynamic not keep ?
                    if (null == d1 || null == b12)
                        continue;
                    if ((d1 -= b12.Value) != (b21 -= b22))
                    {
                        System.Console.WriteLine("Failed -- long? -= long?");
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool Test3()
        {
            decimal[] decimalValues = new decimal[]
            {
            1M, 10.10M, 100.01M, 0M
            }

            ;
            foreach (decimal a1 in decimalValues)
            {
                foreach (decimal a2 in decimalValues)
                {
                    var b11 = a1;
                    var b12 = a2;
                    var b21 = a1;
                    var b22 = a2;
                    dynamic d1 = a1;
                    dynamic d2 = a2;
                    if ((d1 *= d2) != (b21 *= b22))
                    {
                        System.Console.WriteLine("Failed -- decimal *= decimal");
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool Test4()
        {
            int[] intValues = new int[]
            {
            1, 2, 4, 99
            }

            ;
            foreach (int a1 in intValues)
            {
                foreach (int a2 in intValues)
                {
                    var b11 = a1;
                    var b12 = a2;
                    var b21 = a1;
                    var b22 = a2;
                    dynamic d1 = a1;
                    dynamic d2 = a2;
                    if ((d1 /= d2) != (b21 /= b22))
                    {
                        System.Console.WriteLine("Failed -- int /= int");
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool Test5()
        {
            decimal?[] decimalNValues = new decimal?[]
            {
            1M, 10.10M, 100.01M
            }

            ;
            foreach (decimal? a1 in decimalNValues)
            {
                foreach (decimal? a2 in decimalNValues)
                {
                    var b11 = a1;
                    var b12 = a2;
                    var b21 = a1;
                    var b22 = a2;
                    dynamic d1 = a1;
                    if ((d1 %= b12.Value) != (b21 %= b22))
                    {
                        System.Console.WriteLine("Failed -- decimal? %= decimal?");
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool Test6()
        {
            byte[] byteValues = new byte[]
            {
            2, 30, 16, 9
            }

            ;
            foreach (byte a1 in byteValues)
            {
                foreach (byte a2 in byteValues)
                {
                    var b11 = a1;
                    var b12 = a2;
                    var b21 = a1;
                    var b22 = a2;
                    dynamic d2 = a2;
                    if (unchecked(b11 <<= d2) != unchecked(b21 <<= b22))
                    {
                        System.Console.WriteLine("Failed -- byte <<= byte");
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool Test7()
        {
            int?[] intNValues = new int?[]
            { /*null,*/
            2, 4, 3
            }

            ;
            foreach (int? a1 in intNValues)
            {
                foreach (int? a2 in intNValues)
                {
                    var b11 = a1;
                    var b12 = a2;
                    var b21 = a1;
                    var b22 = a2;
                    dynamic d1 = a1;
                    if ((d1 >>= b12) != (b21 >>= b22))
                    {
                        System.Console.WriteLine("Failed -- Nullable<int> >>= Nullable<int>");
                        return false;
                    }
                }
            }

            return true;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.operate013b.operate013b
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.common.common;
    // <Title>Arithmetic operator</Title>
    // <Description>
    // dynamic op literals
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(113,33\).*CS0458</Expects>

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
            result += Verify.Eval(Test1);
            result += Verify.Eval(Test1a);
            result += Verify.Eval(Test2);
            result += Verify.Eval(Test3);
            result += Verify.Eval(Test4);
            result += Verify.Eval(Test5);
            result += Verify.Eval(Test6);
            result += Verify.Eval(Test7);
            ;
            return result;
        }

        private static bool Test1()
        {
            object[] objectValues = new object[]
            {
            10, 10L, "10"
            }

            ;
            foreach (object a2 in objectValues)
            {
                dynamic d2 = a2;
                if (("ABC" + d2) != ("ABC" + 10))
                {
                    System.Console.WriteLine("Failed -- string + object");
                    return false;
                }
            }

            return true;
        }

        private static bool Test1a()
        {
            object[] objectValues = new object[]
            {
            string.Empty, new Test(), null
            }

            ;
            foreach (object a2 in objectValues)
            {
                dynamic d2 = a2;
                if (("ABC" + d2) != ("ABC" + a2))
                {
                    System.Console.WriteLine("Failed -- string + object");
                    return false;
                }
            }

            return true;
        }

        private static bool Test2()
        {
            long?[] longNValues = new long?[]
            {
            10L, 30L, 0, null
            }

            ;
            foreach (long? a1 in longNValues)
            {
                dynamic d1 = a1;
                if ((d1 - 10) != (a1 - 10))
                {
                    System.Console.WriteLine("Failed -- long? - long?");
                    return false;
                }
            }

            return true;
        }

        private static bool Test3()
        {
            decimal[] decimalValues = new decimal[]
            {
            1M, 10.10M, 100.01M, 0M
            }

            ;
            foreach (decimal a2 in decimalValues)
            {
                dynamic d2 = a2;
                if ((2M * d2) != (2M * a2))
                {
                    System.Console.WriteLine("Failed -- decimal * decimal");
                    return false;
                }
            }

            return true;
        }

        private static bool Test4()
        {
            int[] intValues = new int[]
            {
            1, 2, 4, 99
            }

            ;
            foreach (int a1 in intValues)
            {
                dynamic d1 = a1;
                if ((d1 / 3) != (a1 / 3))
                {
                    System.Console.WriteLine("Failed -- int / int");
                    return false;
                }
            }

            return true;
        }

        private static bool Test5()
        {
            decimal?[] decimalNValues = new decimal?[]
            {
            1M, 10.10M, 100.01M, null
            }

            ;
            foreach (decimal? a1 in decimalNValues)
            {
                dynamic d1 = a1;
                if ((d1 % null) != (a1 % null))
                {
                    System.Console.WriteLine("Failed -- decimal? % decimal?");
                    return false;
                }
            }

            return true;
        }

        private static bool Test6()
        {
            byte[] byteValues = new byte[]
            {
            2, 30, 16, 9
            }

            ;
            foreach (byte a2 in byteValues)
            {
                dynamic d2 = a2;
                if ((30 << d2) != (30 << a2))
                {
                    System.Console.WriteLine("Failed -- byte << byte");
                    return false;
                }
            }

            return true;
        }

        private static bool Test7()
        {
            int?[] intNValues = new int?[]
            {
            null, 2, 4, 3
            }

            ;
            foreach (int? a1 in intNValues)
            {
                dynamic d1 = a1;
                if ((d1 >> 2) != (a1 >> 2))
                {
                    System.Console.WriteLine("Failed -- Nullable<int> >> Nullable<int>");
                    return false;
                }
            }

            return true;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.operate014.operate014
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.common.common;
    // <Title>Short circuiting operators</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static bool isHit = false;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(Test1);
            result += Verify.Eval(Test2);
            result += Verify.Eval(Test3);
            result += Verify.Eval(Test4);
            return result;
        }

        private static bool Test1()
        {
            dynamic d1 = true;
            isHit = false;
            if (d1 || BoolValue)
            {
                //d1 is always true, BoolValue is never hit
                if (isHit)
                {
                    System.Console.WriteLine("Failed -- bool || bool as circuiting operators");
                    return false;
                }
            }
            else
            {
                // never hit here.
                System.Console.WriteLine("Failed -- bool || bool as circuiting operators");
                return false;
            }

            return true;
        }

        private static bool Test2()
        {
            dynamic d1 = false;
            isHit = false;
            if (d1 && BoolValue)
            {
                // never hit here.
                System.Console.WriteLine("Failed -- bool && bool as circuiting operators");
                return false;
            }
            else
            {
                //d1 is always false, BoolValue is never hit
                if (isHit)
                {
                    System.Console.WriteLine("Failed -- bool && bool as circuiting operators");
                    return false;
                }
            }

            return true;
        }

        private static bool Test3()
        {
            dynamic d1 = false;
            isHit = false;
            dynamic m = d1 ? StringValue : "ABC";
            if (isHit || m != "ABC")
            {
                System.Console.WriteLine("Failed -- ? : operator");
                return false;
            }

            return true;
        }

        private static bool Test4()
        {
            dynamic d1 = true;
            isHit = false;
            dynamic value = 20;
            dynamic m = d1 ? value : IntValue;
            if (isHit || m != 20)
            {
                System.Console.WriteLine("Failed -- ? : operator");
                return false;
            }

            return true;
        }

        public static bool BoolValue
        {
            get
            {
                isHit = true;
                return true;
            }

            set
            {
            }
        }

        public static string StringValue
        {
            get
            {
                isHit = true;
                return string.Empty;
            }

            set
            {
            }
        }

        public static int IntValue
        {
            get
            {
                isHit = true;
                return 10;
            }

            set
            {
            }
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.operate014b.operate014b
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.common.common;
    // <Title>User defined short circuiting operators</Title>
    // <Description>
    // Unlike in the static world, we can't know the type we care about (the runtime type) for the right operand,
    // as evaluating the right operand expression to get the type may result in side effects.
    // Therefore, we will only ever evaluate the right operand if we know we need to return it.
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using Microsoft.CSharp.RuntimeBinder;

    public class MyOpClass
    {
        public static explicit operator bool (MyOpClass p)
        {
            Test.isCallConvert = true;
            return true;
        }

        public static bool operator true(MyOpClass p)
        {
            Test.isCallTrue = true;
            return false;
        }

        public static bool operator false(MyOpClass p)
        {
            Test.isCallFalse = true;
            return false;
        }

        public static MyOpClass operator &(MyOpClass p1, MyOpClass p2)
        {
            Test.isCallOpAnd = true;
            return p1;
        }

        public static MyOpClass operator |(MyOpClass p1, MyOpClass p2)
        {
            Test.isCallOpOr = true;
            return p2;
        }
    }

    public class MyOpClassWithDiffType
    {
        public static explicit operator bool (MyOpClassWithDiffType p)
        {
            Test.isCallConvert = true;
            return true;
        }

        public static bool operator true(MyOpClassWithDiffType p)
        {
            Test.isCallTrue = true;
            return true;
        }

        public static bool operator false(MyOpClassWithDiffType p)
        {
            Test.isCallFalse = true;
            return false;
        }

        public static MyOpClassWithDiffType operator &(MyOpClassWithDiffType p1, int p2)
        {
            Test.isCallOpAnd = true;
            return p1;
        }

        public static MyOpClassWithDiffType operator |(int p1, MyOpClassWithDiffType p2)
        {
            Test.isCallOpOr = true;
            return p2;
        }
    }

    public class MyOpClassWithDiffType2
    {
        public static explicit operator bool (MyOpClassWithDiffType2 p)
        {
            Test.isCallConvert = true;
            return true;
        }

        public static bool operator true(MyOpClassWithDiffType2 p)
        {
            Test.isCallTrue = true;
            return false;
        }

        public static bool operator false(MyOpClassWithDiffType2 p)
        {
            Test.isCallFalse = true;
            return true;
        }

        public static MyOpClassWithDiffType2 operator &(MyOpClassWithDiffType2 p1, int p2)
        {
            Test.isCallOpAnd = true;
            return p1;
        }

        public static MyOpClassWithDiffType2 operator |(int p1, MyOpClassWithDiffType2 p2)
        {
            Test.isCallOpOr = true;
            return p2;
        }
    }

    public class MyOpClassWithErrorReturnType
    {
        public static explicit operator bool (MyOpClassWithErrorReturnType p)
        {
            Test.isCallConvert = true;
            return true;
        }

        public static bool operator true(MyOpClassWithErrorReturnType p)
        {
            Test.isCallTrue = true;
            return true;
        }

        public static bool operator false(MyOpClassWithErrorReturnType p)
        {
            Test.isCallFalse = true;
            return false;
        }

        public static int operator &(MyOpClassWithErrorReturnType p1, MyOpClassWithErrorReturnType p2)
        {
            Test.isCallOpAnd = true;
            return 1;
        }

        public static int operator |(MyOpClassWithErrorReturnType p1, MyOpClassWithErrorReturnType p2)
        {
            Test.isCallOpOr = true;
            return 2;
        }
    }

    public class MyOpClassWithErrorReturnType2
    {
        public static explicit operator bool (MyOpClassWithErrorReturnType2 p)
        {
            Test.isCallConvert = true;
            return true;
        }

        public static bool operator true(MyOpClassWithErrorReturnType2 p)
        {
            Test.isCallTrue = true;
            return false;
        }

        public static bool operator false(MyOpClassWithErrorReturnType2 p)
        {
            Test.isCallFalse = true;
            return true;
        }

        public static int operator &(MyOpClassWithErrorReturnType2 p1, MyOpClassWithErrorReturnType2 p2)
        {
            Test.isCallOpAnd = true;
            return 1;
        }

        public static int operator |(MyOpClassWithErrorReturnType2 p1, MyOpClassWithErrorReturnType2 p2)
        {
            Test.isCallOpOr = true;
            return 2;
        }
    }

    public class Test
    {
        public static bool isCallConvert = false;
        public static bool isCallTrue = false;
        public static bool isCallFalse = false;
        public static bool isCallOpAnd = false;
        public static bool isCallOpOr = false;
        public static void ClearFlags()
        {
            isCallConvert = false;
            isCallTrue = false;
            isCallFalse = false;
            isCallOpAnd = false;
            isCallOpOr = false;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int result = 0;
            result += Verify.Eval(TestAndOpWithSameTypeAndReturnType);
            result += Verify.Eval(TestAndOpWithDiffType);
            result += Verify.Eval(TestAndOpWithDiffType2);
            result += Verify.Eval(TestAndOpWithSameTypeButWrongReturnType);
            result += Verify.Eval(TestAndOpWithSameTypeButWrongReturnType2);
            result += Verify.Eval(TestOrOpWithSameTypeAndReturnType);
            result += Verify.Eval(TestOrOpWithDiffType);
            result += Verify.Eval(TestOrOpWithDiffType2);
            result += Verify.Eval(TestOrOpWithSameTypeButWrongReturnType);
            result += Verify.Eval(TestOrOpWithSameTypeButWrongReturnType2);
            return result;
        }

        #region conditional logical and operator
        private static bool TestAndOpWithSameTypeAndReturnType()
        {
            ClearFlags();
            dynamic d1 = new MyOpClass();
            dynamic d2 = new MyOpClass();
            dynamic dr = d1 && d2;
            if (dr.GetType() != typeof(MyOpClass))
            {
                System.Console.WriteLine("Failed -- got wrong return type");
                return false;
            }

            if (dr != d1)
            {
                System.Console.WriteLine("Failed -- got wrong result");
                return false;
            }

            if (!(isCallFalse && isCallOpAnd && !isCallTrue && !isCallConvert && !isCallOpOr))
            {
                System.Console.WriteLine("Failed -- executed error ops. isCallConvert[{0}, isCallTrue[{1}], isCallFalse[{2}], isCallOpAnd[{3}], isCallOpOr[{4}], ", isCallConvert, isCallTrue, isCallFalse, isCallOpAnd, isCallOpOr);
                return false;
            }

            return true;
        }

        private static bool TestAndOpWithDiffType()
        {
            ClearFlags();
            dynamic d1 = new MyOpClassWithDiffType();
            dynamic d2 = 10;
            try
            {
                dynamic dr = d1 && d2;
                System.Console.WriteLine("Failed -- didn't get RuntimeBinderException");
            }
            catch (RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBoolOp, e.Message, "MyOpClassWithDiffType.operator &(MyOpClassWithDiffType, int)"))
                    return true;
            }

            return false;
        }

        private static bool TestAndOpWithDiffType2()
        {
            ClearFlags();
            dynamic d1 = new MyOpClassWithDiffType2();
            dynamic d2 = 10;
            dynamic dr = d1 && d2;
            if (dr.GetType() != typeof(MyOpClassWithDiffType2))
            {
                System.Console.WriteLine("Failed -- got wrong return type");
                return false;
            }

            if (dr != d1)
            {
                System.Console.WriteLine("Failed -- got wrong result");
                return false;
            }

            if (!(isCallFalse && !isCallOpAnd && !isCallTrue && !isCallConvert && !isCallOpOr))
            {
                System.Console.WriteLine("Failed -- executed error ops. isCallConvert[{0}, isCallTrue[{1}], isCallFalse[{2}], isCallOpAnd[{3}], isCallOpOr[{4}], ", isCallConvert, isCallTrue, isCallFalse, isCallOpAnd, isCallOpOr);
                return false;
            }

            return true;
        }

        private static bool TestAndOpWithSameTypeButWrongReturnType()
        {
            ClearFlags();
            dynamic d1 = new MyOpClassWithErrorReturnType();
            dynamic d2 = new MyOpClassWithErrorReturnType();
            try
            {
                dynamic dr = d1 && d2;
                System.Console.WriteLine("Failed -- didn't get RuntimeBinderException");
            }
            catch (RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBoolOp, e.Message, "MyOpClassWithErrorReturnType.operator &(MyOpClassWithErrorReturnType, MyOpClassWithErrorReturnType)"))
                    return true;
            }

            return false;
        }

        private static bool TestAndOpWithSameTypeButWrongReturnType2()
        {
            ClearFlags();
            dynamic d1 = new MyOpClassWithErrorReturnType2();
            dynamic d2 = new MyOpClassWithErrorReturnType2();
            dynamic dr = d1 && d2;
            if (dr.GetType() != typeof(MyOpClassWithErrorReturnType2))
            {
                System.Console.WriteLine("Failed -- got wrong return type");
                return false;
            }

            if (dr != d1)
            {
                System.Console.WriteLine("Failed -- got wrong result");
                return false;
            }

            if (!(isCallFalse && !isCallOpAnd && !isCallTrue && !isCallConvert && !isCallOpOr))
            {
                System.Console.WriteLine("Failed -- executed error ops. isCallConvert[{0}, isCallTrue[{1}], isCallFalse[{2}], isCallOpAnd[{3}], isCallOpOr[{4}], ", isCallConvert, isCallTrue, isCallFalse, isCallOpAnd, isCallOpOr);
                return false;
            }

            return true;
        }

        #endregion
        #region conditional logical or operator
        private static bool TestOrOpWithSameTypeAndReturnType()
        {
            ClearFlags();
            dynamic d1 = new MyOpClass();
            dynamic d2 = new MyOpClass();
            dynamic dr = d1 || d2;
            if (dr.GetType() != typeof(MyOpClass))
            {
                System.Console.WriteLine("Failed -- got wrong return type");
                return false;
            }

            if (dr != d2)
            {
                System.Console.WriteLine("Failed -- got wrong result");
                return false;
            }

            if (!(isCallTrue && isCallOpOr && !isCallFalse && !isCallConvert && !isCallOpAnd))
            {
                System.Console.WriteLine("Failed -- executed error ops. isCallConvert[{0}, isCallTrue[{1}], isCallFalse[{2}], isCallOpAnd[{3}], isCallOpOr[{4}], ", isCallConvert, isCallTrue, isCallFalse, isCallOpAnd, isCallOpOr);
                return false;
            }

            return true;
        }

        private static bool TestOrOpWithDiffType()
        {
            ClearFlags();
            dynamic d1 = 10;
            dynamic d2 = new MyOpClassWithDiffType();
            try
            {
                dynamic dr = d1 || d2;
                System.Console.WriteLine("Failed -- didn't get RuntimeBinderException");
            }
            catch (RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.NoImplicitConv, e.Message, "int", "bool"))
                    return true;
            }

            return false;
        }

        private static bool TestOrOpWithDiffType2()
        {
            ClearFlags();
            dynamic d1 = 10;
            dynamic d2 = new MyOpClassWithDiffType2();
            try
            {
                dynamic dr = d1 || d2;
                System.Console.WriteLine("Failed -- didn't get RuntimeBinderException");
            }
            catch (RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.NoImplicitConv, e.Message, "int", "bool"))
                    return true;
            }

            return false;
        }

        private static bool TestOrOpWithSameTypeButWrongReturnType()
        {
            ClearFlags();
            dynamic d1 = new MyOpClassWithErrorReturnType();
            dynamic d2 = new MyOpClassWithErrorReturnType();
            dynamic dr = d1 || d2;
            if (dr.GetType() != typeof(MyOpClassWithErrorReturnType))
            {
                System.Console.WriteLine("Failed -- got wrong return type");
                return false;
            }

            if (dr != d1)
            {
                System.Console.WriteLine("Failed -- got wrong result");
                return false;
            }

            if (!(isCallTrue && !isCallOpOr && !isCallFalse && !isCallConvert && !isCallOpAnd))
            {
                System.Console.WriteLine("Failed -- executed error ops. isCallConvert[{0}, isCallTrue[{1}], isCallFalse[{2}], isCallOpAnd[{3}], isCallOpOr[{4}], ", isCallConvert, isCallTrue, isCallFalse, isCallOpAnd, isCallOpOr);
                return false;
            }

            return true;
        }

        private static bool TestOrOpWithSameTypeButWrongReturnType2()
        {
            ClearFlags();
            dynamic d1 = new MyOpClassWithErrorReturnType2();
            dynamic d2 = new MyOpClassWithErrorReturnType2();
            try
            {
                dynamic dr = d1 || d2;
                System.Console.WriteLine("Failed -- didn't get RuntimeBinderException");
            }
            catch (RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadBoolOp, e.Message, "MyOpClassWithErrorReturnType2.operator |(MyOpClassWithErrorReturnType2, MyOpClassWithErrorReturnType2)"))
                    return true;
            }

            return false;
        }
        #endregion
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.operate015.operate015
{
    // <Title> Operator -.is, as</Title>
    // <Description></Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System.Collections.Generic;

    public class A
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            object[] x = new[]
            {
            ""
            }

            ;
            dynamic[] y = x as dynamic[];
            bool ret = (x == y);
            ret &= (y is IList<string>); // used to be false
            ret &= (x is IList<string>);
            return ret ? 0 : 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.operate016.operate016
{
    // <Title> Operator -.is, as</Title>
    // <Description></Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class A
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            bool? b = true;
            dynamic d = b;
            if (!d == !b)
                return 0;
            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.operate017.operate017
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.common.common;
    // <Title>Unary operators with operand null</Title>
    // <Description>
    // The related 
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;
    using Microsoft.CSharp.RuntimeBinder;

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
            result += Verify.Eval(() => TestUnaryOp(d => +d, "+"), "Unary +");
            result += Verify.Eval(() => TestUnaryOp(d => -d, "-"), "Unary -");
            result += Verify.Eval(() => TestUnaryOp(d => !d, "!"), "Unary *");
            result += Verify.Eval(() => TestUnaryOp(d => ~d, "~"), "Unary /");
            result += Verify.Eval(Test5, "Cast");
            // result += Verify.Eval(() => TestUnaryOp(d => ++d, "++"), "pre ++");
            result += Verify.Eval(() => TestIncDec(d => ++d), "pre ++");
            result += Verify.Eval(() => TestIncDec(d => d++), "post ++");
            result += Verify.Eval(() => TestIncDec(d => --d), "pre --");
            result += Verify.Eval(() => TestIncDec(d => d--), "post --");
            result += Verify.Eval(Test10, "checked");
            result += Verify.Eval(Test11, "unchecked");
            return result;
        }

        private static bool TestUnaryOp(Func<dynamic, dynamic> exp, string op)
        {
            dynamic d = null;
            try
            {
                if (exp(d) != null)
                    return false;
            }
            catch (RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.BadUnaryOp, e.Message, op, ErrorVerifier.GetErrorElement(ErrorElementId.NULL)))
                    return true;
            }

            return false;
        }

        private static bool Test5()
        {
            dynamic d = null;
            if (((string)d) != null)
                return false;
            return true;
        }

        private static bool TestIncDec(Func<dynamic, dynamic> exp)
        {
            dynamic d = null;
            try
            {
                if (exp(d) != null)
                    return false;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                bool ret = ErrorVerifier.Verify(ErrorMessageId.IncrementLvalueExpected, e.Message);
                return ret;
            }

            return false;
        }

        private static bool Test10()
        {
            dynamic d = null;
            if ((checked(d)) != null)
                return false;
            return true;
        }

        private static bool Test11()
        {
            dynamic d = null;
            if ((unchecked(d)) != null)
                return false;
            return true;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.operate018.operate018
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.common.common;
    // <Title>Binary operators with both operands are null</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
#pragma warning disable 0458 // expression always null warning

#pragma warning disable 0464 // Comparing with null of type 'int?' always produces 'false'

    using System;
    using Microsoft.CSharp.RuntimeBinder;

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
            //dynamic d2 = null + null; // error CS0034: Operator '+' is ambiguous on operands of type '<null>' and '<null>'
            //int? ni = null;
            //dynamic d3 = ni + null;  // will get null
            result += Verify.Eval(() => TestBinaryOp(d => null + d, "+"), "binary +");
            result += Verify.Eval(() => TestResultIsNull(d =>
            {
                int? ni1 = null;
                int? ni = ni1 + null;
                return d + ni;
            }

            ), "binary /");
            result += Verify.Eval(() => TestResultIsNull(d => d - (null - null)), "binary -");
            result += Verify.Eval(() => TestResultIsNull(d =>
            {
                dynamic d2 = null * null;
                return d * d2;
            }

            ), "binary *");
            result += Verify.Eval(() => TestResultIsNull(d =>
            {
                int? ni = null / null;
                return ni / d;
            }

            ), "binary /");
            result += Verify.Eval(() => TestResultIsNull(d => null % d), "binary %");
            result += Verify.Eval(() => TestResultIsNull(d => d << (null << null)), "binary <<");
            result += Verify.Eval(() => TestResultIsNull(d =>
            {
                int? ni = null >> null;
                return d >> ni;
            }

            ), "binary >>");
            result += Verify.Eval(() => TestBinaryOpRelation(d => d == null), "binary ==");
            result += Verify.Eval(() => TestBinaryOpRelation(d => !(null != d)), "binary !=");
            result += Verify.Eval(() => TestBinaryOpRelation(d => !(null > null)), "binary >");
            result += Verify.Eval(() => TestBinaryOpRelation(d => !(d > null)), "binary >");
            result += Verify.Eval(() => TestBinaryOpRelation(d =>
            {
                dynamic d2 = null;
                return !(d >= d2);
            }

            ), "binary >=");
            result += Verify.Eval(() => TestBinaryOpRelation(d =>
            {
                int? ni = null;
                return !(ni < d);
            }

            ), "binary <");
            result += Verify.Eval(() => TestBinaryOpRelation(d => !(null <= d)), "binary <=");
            result += Verify.Eval(() => TestBinaryOp(d => d & null, "&"), "binary &");
            result += Verify.Eval(() => TestBinaryOp(d => null | d, "|"), "binary |");
            // "null & null" will get error CS0034: Operator '&' is ambiguous on operands of type '<null>' and '<null>'
            // but "ni & null" will get null
            result += Verify.Eval(() => TestResultIsNull(d =>
            {
                int? ni1 = null;
                int? ni = ni1 ^ null;
                return ni ^ d;
            }

            ), "binary ^");
            return result;
        }

        private static bool TestBinaryOp(Func<dynamic, dynamic> exp, string op)
        {
            dynamic d = null;
            try
            {
                if (exp(d) != null)
                    return false;
            }
            catch (RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.AmbigBinaryOps, e.Message, op, ErrorVerifier.GetErrorElement(ErrorElementId.NULL), ErrorVerifier.GetErrorElement(ErrorElementId.NULL)))
                    return true;
            }

            return false;
        }

        private static bool TestResultIsNull(Func<dynamic, dynamic> exp)
        {
            dynamic d = null;
            if (exp(d) != null)
                return false;
            return true;
        }

        private static bool TestBinaryOpRelation(Func<dynamic, dynamic> exp)
        {
            dynamic d = null;
            return exp(d);
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.operate019.operate019
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.common.common;
    // <Title>Other operators with both operand null</Title>
    // <Description>Conditional logical, conditional, compound</Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
#pragma warning disable 0458 // expression always null warning

#pragma warning disable 0464 // Comparing with null of type 'int?' always produces 'false'

    using System;
    using Microsoft.CSharp.RuntimeBinder;

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
            result += Verify.Eval(() => TestBinaryOp(d => d += null, "+="), "compound +=");
            result += Verify.Eval(() => TestResultIsNull(d => d -= (null - null)), "compound -=");
            result += Verify.Eval(() => TestResultIsNull(d =>
            {
                dynamic d2 = null * null;
                return d *= d2;
            }

            ), "compound *=");
            result += Verify.Eval(() => TestResultIsNull(d =>
            {
                int? ni = null / null;
                return d /= ni;
            }

            ), "compound /=");
            result += Verify.Eval(() => TestResultIsNull(d => d %= null), "compound %=");
            result += Verify.Eval(() => TestResultIsNull(d => d <<= (null << null)), "compound <<=");
            result += Verify.Eval(() => TestResultIsNull(d =>
            {
                int? ni = null >> null;
                return d >>= ni;
            }

            ), "compound >>=");
            result += Verify.Eval(() => TestBinaryOp(d => d &= null, "&="), "compound &=");
            result += Verify.Eval(() => TestBinaryOp(d =>
            {
                dynamic d2 = null;
                return d |= d2;
            }

            , "|="), "compound |=");
            result += Verify.Eval(() => TestResultIsNull(d =>
            {
                int? ni1 = null;
                int? ni = ni1 ^ null;
                return d ^= ni;
            }

            ), "compound ^=");
            result += Verify.Eval(() => TestConditionalLogicalOpWithFirstIsNull(d => d && null), "binary && with first is null");
            result += Verify.Eval(() => TestConditionalLogicalOpWithFirstIsNull(d => d || null), "binary || with first is null");
            result += Verify.Eval(() => TestConditionalLogicalOpWithFirstIsNull(d => d ? d : null), "conditional ?: with first is null");
            result += Verify.Eval(() => TestResultIsNull(d => true && d), "binary && with second is null");
            result += Verify.Eval(() => TestResultIsNull(d => false || d), "binary || with second is null");
            result += Verify.Eval(() => TestResultIsNull(d => true ? d : null), "conditional ?: with second and third are null");
            result += Verify.Eval(() => TestResultIsNull(d =>
            {
                int? ni = null;
                return true ? ni : d;
            }

            ), "conditional ?: with second and third are null");
            result += Verify.Eval(() => TestResultIsNull(d =>
            {
                dynamic d2 = null;
                return false ? d2 : d;
            }

            ), "conditional ?: with second and third are null");
            return result;
        }

        private static bool TestBinaryOp(Func<dynamic, dynamic> exp, string op)
        {
            dynamic d = null;
            try
            {
                if (exp(d) != null)
                    return false;
            }
            catch (RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.AmbigBinaryOps, e.Message, op, ErrorVerifier.GetErrorElement(ErrorElementId.NULL), ErrorVerifier.GetErrorElement(ErrorElementId.NULL)))
                    return true;
            }

            return false;
        }

        private static bool TestResultIsNull(Func<dynamic, dynamic> exp)
        {
            dynamic d = null;
            if (exp(d) != null)
                return false;
            return true;
        }

        private static bool TestConditionalLogicalOpWithFirstIsNull(Func<dynamic, dynamic> exp)
        {
            dynamic d = null;
            try
            {
                if (exp(d) != null)
                    return false;
            }
            catch (RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.ValueCantBeNull, e.Message, "bool"))
                    return true;
            }

            return false;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.operate020.operate020
{
    // <Title>Regression test</Title>
    // <Description></Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] ars)
        {
            C c = new C();
            c[M()] -= 3;
            return (C.Hit - 4);
        }

        public static int M()
        {
            C.Hit = C.Hit + 5;
            return 0;
        }
    }

    public class C
    {
        public static int Hit = 1;
        public dynamic this[object o]
        {
            get
            {
                C.Hit = C.Hit * 2;
                return 1;
            }

            set
            {
                C.Hit = C.Hit / 3;
            }
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.operate021.operate021
{
    // <Title>Regression test</Title>
    // <Description></Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    public class A
    {
        public static implicit operator int (A x)
        {
            return 1;
        }

        public static implicit operator A(int x)
        {
            return new A();
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            var a = new A();
            a++;
            dynamic b = new A();
            try
            {
                b++;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                if (ErrorVerifier.Verify(ErrorMessageId.NoExplicitConv, e.Message, "int", "A"))
                    return 0;
            }

            return 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.operate022.operate022
{
    // <Title>Regression test</Title>
    // <Description></Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(19,41\).*CS0168</Expects>

    public class A
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic a = new B<int>();
            try
            {
                var c = a || a;
            }
            catch (System.ArgumentException e)
            {
                return 0;
            }

            return 1;
        }
    }

    public class B<T>
    {
        public static B<string> operator |(B<T> x, B<T> y)
        {
            return null;
        }

        public static bool operator true(B<T> x)
        {
            return false;
        }

        public static bool operator false(B<T> x)
        {
            return false;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.literal001.literal001
{
    // <Title>Dynamic method call with literal parameter</Title>
    // <Description> </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public byte Method1(byte b)
        {
            return byte.MaxValue;
        }

        public short Method2(short b)
        {
            return 0;
        }

        public ushort Method3(ushort b)
        {
            return ushort.MinValue;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d = new Test();
            bool ret = (byte.MaxValue == d.Method1(0));
            ret &= (0 == d.Method2(0));
            ret &= (ushort.MinValue == d.Method3(0));
            return ret ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.nullable001.nullable001
{
    // <Title>Nullable and dynamic</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class D
    {
        public ulong? ulnull;
        public decimal? denull;
    }

    public class Program
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            dynamic d2 = new D();
            d2.ulnull = 3;
            if (d2.ulnull != 3)
                return 1;
            d2.denull = 3m;
            if (d2.denull != 3m)
                return 1;
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.nullable002.nullable002
{
    // <Title>Nullable and dynamic</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class D
    {
        public int? inull;
    }

    public class Program
    {
        public static int Status = 0;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d2 = new D();
            d2.inull = 3;
            if (d2.inull != 3)
                return 1;
            d2.inull = null;
            if (d2.inull != null)
                return 1;
            return Program.Status;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.nullable003.nullable003
{
    // <Title>Nullable and dynamic</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    public class D
    {
        public int? inull;
    }

    public class Program
    {
        public static int Status = 0;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic d2 = new D();
            d2.inull = 3;
            if (d2.inull != 3)
                return 1;
            d2.inull = null;
            if (d2.inull != null)
                return 1;
            return Program.Status;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.field001.field001
{
    // <Title>Dynamic static fields</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public static dynamic c1 = 4;
        public static dynamic c2 = 4;
        public dynamic c3 = 4;
        public dynamic c4 = 4;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            int rez = 0;
            Test.c1 += (Test.c1 + Test.c2); //this does not work
            if (Test.c1 == 12)
                rez++;
            dynamic d = new Test();
            d.c3 += (d.c3 + d.c4);
            if (d.c3 == 12)
                rez++;
            return rez == 2 ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.field002.field002
{
    // <Title>Dynamic static fields</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class P
    {
        public dynamic i = 0;
        public void Foo()
        {
            ++i;
        }

        public void Bar()
        {
            i++;
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            int rez = 0;
            dynamic d = new P();
            d.Foo();
            if (d.i == 1)
                rez++;
            d.Bar();
            if (d.i == 2)
                rez++;
            return rez == 2 ? 0 : 1;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.date001.date001
{
    // <Title>dynamic expression on DateTime.Add...</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>
    using System;

    public class VerTest
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            // Just to verify there is no assertion here
            dynamic d = 1;
            DateTime c = (new DateTime(2000, 1, 1)).AddDays(d);
            c = (new DateTime(2000, 1, 1)).AddHours(d);
            c = (new DateTime(2000, 1, 1)).AddSeconds(d);
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.gettype01.gettype01
{
    // <Title>GetType should not hide object.GetType</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System;

    public class Program
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            Type d = typeof(ValueType);
            return new Program().Method(d);
        }

        public int Method(dynamic d)
        {
            return 0;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.dynamicType.basic.gettype02.gettype02
{
    // <Title>GetType should not hide object.GetType</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    using System;

    public class Program
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] args)
        {
            Type d = typeof(ValueType);
            dynamic dy = new Program();
            return dy.Method(d);
        }

        public int Method(dynamic d)
        {
            return 0;
        }
    }
    // </Code>
}
