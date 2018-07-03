// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.basic.field01.field01
{
    // <Title> Compound operator in field.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public delegate int MyDel(int i);

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] ars)
        {
            Test t = new Test();
            dynamic d = t;
            char c = (char)2;
            d.field *= c;
            int i = 5;
            d.field /= Method(i);
            sbyte s = 3;
            d.field %= t[s];
            if (d.field != 1)
                return 1;
            MyDel md = Method;
            dynamic dmd = new MyDel(Method);
            d.field += md(2);
            if (d.field != 3)
                return 1;
            d.field -= dmd(2);
            if (d.field != 1)
                return 1;
            return (int)(d.field -= LongPro);
        }

        public long field = 10;
        public static int Method(int i)
        {
            return i;
        }

        public dynamic this[object o]
        {
            get
            {
                return o;
            }
        }

        public static long LongPro
        {
            protected get
            {
                return 1L;
            }

            set
            {
            }
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.basic.field02.field02
{
    // <Title> Compound operator in field.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public delegate int MyDel(int i);

        
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] ars)
        {
            Test t = new Test();
            dynamic d = t;
            dynamic c = (char)2;
            dynamic c0 = c;
            d.field *= c0;
            c = 5;
            d.field /= Method(c);
            sbyte s = 3;
            c = s;
            d.field %= t[c];
            if (d.field != 1)
                return 1;
            MyDel md = Method;
            dynamic dmd = new MyDel(Method);
            c = 2;
            d.field += md(c);
            if (d.field != 3)
                return 1;
            d.field -= dmd(2);
            if (d.field != 1)
                return 1;
            return (int)(d.field -= d.LongPro);
        }

        public long field = 10;
        public static int Method(int i)
        {
            return i;
        }

        public dynamic this[object o]
        {
            get
            {
                return o;
            }
        }

        public long LongPro
        {
            protected get
            {
                return 1L;
            }

            set
            {
            }
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.basic.field03.field03
{
    public class Test
    {
        public static dynamic count1 = 1;
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            count1 = count1 + 1;
            count1 += 1;
            return 0;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.basic.property01.property01
{
    // <Title> Compound operator in property.</Title>
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

        public static int MainMethod(string[] ars)
        {
            Test t = new Test();
            dynamic d = t;
            d.StringProp = "a";
            d.StringProp += "b";
            d.StringProp += t.StringProp;
            d.StringProp += t.Method(1);
            var temp = 10.1M;
            d.StringProp += t[temp];
            if (d.StringProp != "abab1" + temp.ToString())
                return 1;
            return 0;
        }

        public string StringProp
        {
            get;
            set;
        }

        public int Method(int i)
        {
            return i;
        }

        public dynamic this[object o]
        {
            get
            {
                return o;
            }
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.basic.property02.property02
{
    // <Title> Compound operator in property.</Title>
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

        public static int MainMethod(string[] ars)
        {
            Test t = new Test();
            dynamic d = t;
            dynamic c = "a";
            d.StringProp = c;
            c = "b";
            d.StringProp += c;
            d.StringProp += d.StringProp;
            c = 1;
            d.StringProp += d.Method(c);
            c = 10.1M;
            d.StringProp += d[c];
            if (d.StringProp != "abab1" + c.ToString())
                return 1;
            return 0;
        }

        public string StringProp
        {
            get;
            set;
        }

        public int Method(int i)
        {
            return i;
        }

        public dynamic this[object o]
        {
            get
            {
                return o;
            }
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.basic.property03.property03
{
    // <Title> Compound operator with property.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class C
    {
        public dynamic P
        {
            get;
            set;
        }

        public static dynamic operator +(C lhs, int rhs)
        {
            lhs.P = lhs.P + rhs;
            return lhs;
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
            C c = new C()
            {
                P = 0
            }

            ;
            c += 2;
            return c.P == 2 ? 0 : 1;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.basic.indexer01.indexer01
{
    // <Title> Compound operator in indexer.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public delegate int MyDel(int i);

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] ars)
        {
            Test t = new Test();
            dynamic d = t;
            char c = (char)2;
            d[null] *= c;
            int i = 5;
            d[1] /= Method(i);
            sbyte s = 3;
            d[default(long)] %= t[s, ""];
            if (d[default(string)] != 1)
                return 1;
            MyDel md = Method;
            dynamic dmd = new MyDel(Method);
            d[default(dynamic)] += md(2);
            if (d[12.34f] != 3)
                return 1;
            d[typeof(Test)] -= dmd(2);
            if (d[""] != 1)
                return 1;
            return (int)(d[new Test()] -= LongPro);
        }

        private long _field = 10;
        public long this[dynamic d]
        {
            get
            {
                return _field;
            }

            set
            {
                _field = value;
            }
        }

        public static int Method(int i)
        {
            return i;
        }

        public dynamic this[object o, string m]
        {
            get
            {
                return o;
            }
        }

        public static long LongPro
        {
            protected get
            {
                return 1L;
            }

            set
            {
            }
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.basic.indexer02.indexer02
{
    // <Title> Compound operator in indexer.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public delegate int MyDel(int i);

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] ars)
        {
            Test t = new Test();
            dynamic d = t;
            char c = (char)2;
            dynamic c0 = c;
            d[null] *= c0;
            dynamic i = 5;
            d[1] /= Method(i);
            sbyte s = 3;
            dynamic s0 = s;
            d[default(long)] %= t[s0, ""];
            if (d[default(string)] != 1)
                return 1;
            MyDel md = Method;
            dynamic dmd = new MyDel(Method);
            dynamic md0 = 2;
            d[default(dynamic)] += md(md0);
            if (d[12.34f] != 3)
                return 1;
            d[typeof(Test)] -= dmd(2);
            if (d[""] != 1)
                return 1;
            return (int)(d[new Test()] -= LongPro);
        }

        private long _field = 10;
        public long this[dynamic d]
        {
            get
            {
                return _field;
            }

            set
            {
                _field = value;
            }
        }

        public static int Method(int i)
        {
            return i;
        }

        public dynamic this[object o, string m]
        {
            get
            {
                return o;
            }
        }

        public static long LongPro
        {
            protected get
            {
                return 1L;
            }

            set
            {
            }
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.basic.order01.order01
{
    // <Title> Compound operator execute orders.</Title>
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

        public static int MainMethod(string[] ars)
        {
            dynamic t = new Test();
            t.field *= t.field += t.field;
            if (t.field != 200)
                return 1;
            return 0;
        }

        public long field = 10;
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.basic.order02.order02
{
    // <Title> Compound operator execute orders.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] ars)
        {
            dynamic t = new Test();
            dynamic b = (byte)2;
            t.MyPro = 7;
            t.field = 8;
            t.MyPro %= t.field >>= b;
            if (t.MyPro != 1 || t.field != 2)
                return 1;
            return 0;
        }

        public byte field = 10;
        public short myProvalue;
        public short MyPro
        {
            internal get
            {
                return myProvalue;
            }

            set
            {
                myProvalue = value;
            }
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.basic.order03.order03
{
    // <Title> Compound operator execute orders.</Title>
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

        public static int MainMethod(string[] ars)
        {
            dynamic t = new Test();
            dynamic b = (byte)2;
            t[null] += t[b] *= t[""];
            if (t[null] != 110)
                return 1;
            return 0;
        }

        public dynamic myvalue = 10;
        public dynamic this[object o]
        {
            get
            {
                return myvalue;
            }

            set
            {
                this.myvalue = value;
            }
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.basic.order04.order04
{
    // <Title> Compound operator execute orders.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public delegate int MyDel(int i);
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] ars)
        {
            Test t = new Test();
            MyDel md = new MyDel(Method);
            t.field *= t.LongPro %= t[(sbyte)10] -= md(3);
            if (t.field != 40 || t.LongPro != 4 || t[null] != 5)
                return 1;
            return 0;
        }

        public long field = 10;
        public static int Method(int i)
        {
            return i;
        }

        private long _this0 = 8;
        public long this[object o]
        {
            get
            {
                return _this0;
            }

            set
            {
                _this0 = value;
            }
        }

        private long _longvalue = 9;
        public long LongPro
        {
            protected get
            {
                return _longvalue;
            }

            set
            {
                _longvalue = value;
            }
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.basic.order05.order05
{
    // <Title> Compound operator execute orders.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public delegate int MyDel(int i);
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] ars)
        {
            Test t = new Test();
            MyDel md = new MyDel(Method);
            t.field *= t.LongPro += t[(int)10] -= md(3);
            if (t.field != 50 || t.LongPro != 5 || t[null] != 5)
                return 1;
            return 0;
        }

        public long field = 10;
        public static int Method(int i)
        {
            return i;
        }

        private dynamic _this0 = 8;
        public dynamic this[object o]
        {
            get
            {
                return _this0;
            }

            set
            {
                _this0 = value;
            }
        }

        private long _longvalue = 0;
        public long LongPro
        {
            protected get
            {
                return _longvalue;
            }

            set
            {
                _longvalue = value;
            }
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.basic.order06.order06
{
    // <Title> Compound operator execute orders.</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    // <Code>

    public class Test
    {
        public delegate int MyDel(int i);

        
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod(null));
        }

        public static int MainMethod(string[] ars)
        {
            dynamic t = new Test();
            MyDel md = new MyDel(Method);
            t.field *= t.LongPro += t[(int)10] -= md(3);
            if (t.field != 50 || t.LongPro != 5 || t[null] != 5)
                return 1;
            return 0;
        }

        public long field = 10;
        public static int Method(int i)
        {
            return i;
        }

        private dynamic _this0 = 8;
        public dynamic this[object o]
        {
            get
            {
                return _this0;
            }

            set
            {
                _this0 = value;
            }
        }

        private long _longvalue = 0;
        public long LongPro
        {
            protected get
            {
                return _longvalue;
            }

            set
            {
                _longvalue = value;
            }
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.basic.context01.context01
{
    // <Title> Compound operator</Title>
    // <Description>context
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    using System;

    public class Test
    {
        
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            var obj = new Test();
            //
            dynamic v1 = obj.Prop; //100
            dynamic v2 = obj.Method((short)(v1 -= 80));
            v1 += 80;
            dynamic v3 = obj[v2 *= (s_field -= 6)];
            v2 /= s_field;
            dynamic val = 0;
            for (int i = v2; i < checked(v1 += v3 -= v2); i++)
            {
                val += 1;
            }

            // System.Console.WriteLine("{0}, {1}, {2} {3}", v1, v2, v3, field);
            dynamic ret = 7 == val;
            //
            v1 *= obj.Prop -= 15; // 120
            v2 *= (s_field += 1.1);
            v3 = obj[v2 %= (s_field *= 3.4)] + 100;
            dynamic arr = new[]
            {
            v1 <<= (int)(v1 >>= 10), v2 *= v2 *= 2, v3 *= v1 * (v1 -= s_field)}

            ;
            // System.Console.WriteLine("{0}, {1}, {2}", arr[0], arr[1], arr[2]);
            ret &= 3400 == arr[0];
            ret &= (468.18 - arr[1]) < double.Epsilon;
            ret &= (1326070373.2 - arr[2]) < double.Epsilon;
            v1 = 1.1f;
            v2 = -2.2f;
            v3 = 5.5f;
            arr = new
            {
                a1 = v3 -= v1 += v2,
                a2 = v1 *= v2 + 1,
                a3 = v3 /= 1.1f
            }

            ;
            System.Console.WriteLine("{0}, {1}, {2}", (object)arr.a1, (object)arr.a2, (object)arr.a3);
            ret &= (6.6 - arr.a1) < 0.0000001f; // delta ~ 0.00000009f
            ret &= (1.23 - arr.a2) < double.Epsilon;
            ret &= (6 - arr.a3) < double.Epsilon;
            System.Console.WriteLine((object)ret);
            return ret ? 0 : 1;
        }

        private static dynamic s_field = 10;
        public dynamic Prop
        {
            get
            {
                return 100L;
            }

            set
            {
            }
        }

        public dynamic Method(short i)
        {
            return i;
        }

        public dynamic this[object o]
        {
            get
            {
                return o;
            }
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.basic.context02b.context02b
{
    // <Title> Compound operator</Title>
    // <Description>context
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
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
            var obj = new Test();
            bool ret = obj.RunTest();
            return ret ? 0 : 1;
        }

        public struct MySt
        {
            public MySt(ulong? f)
            {
                field = f;
            }

            public ulong? field;
        }

        public dynamic RunTest()
        {
            dynamic[] darr = new[]
            {
            "X", "0", "Y"
            }

            ;
            List<dynamic> list = new List<dynamic>();
            list.AddRange(darr);
            bool ret = "X0X0Y" == Method()(list[0] += list[1], list[2]);
            ret &= "YX" == this[list[2]](null, darr[0]);
            var va = new List<dynamic>
            {
            new MySt(null), new MySt(3), new MySt(5), new MySt(7), new MySt(11), new MySt(13)}

            ;
            var q =
                from i in new[]
                {
                va[1].field *= 11, va[2].field *= va[2].field %= 19, va[3].field += va[4].field *= va[5].field
                }

                where (0 < (va[5].field += va[3].field -= 2))
                select i;
            dynamic idx = 0;
            foreach (var v in q)
            {
                if (0 == idx)
                {
                    ret &= (33 == v);
                    // System.Console.WriteLine("v1 {0}", v);
                }
                else if (idx == 1)
                {
                    // System.Console.WriteLine("v2 {0}", v);
                    ret &= (25 == v);
                }
                else if (idx == 2)
                {
                    // System.Console.WriteLine("v3 {0}", v);
                    ret &= (150 == v);
                }

                idx++;
            }

            return ret;
        }

        public delegate dynamic MyDel(dynamic p1, dynamic p2 = default(object), long p3 = 100);
        public dynamic Method()
        {
            MyDel md = delegate (dynamic d, object o, long n)
            {
                d += d += o;
                return d;
            }

            ;
            return md;
        }

        public dynamic this[dynamic o]
        {
            get
            {
                return new MyDel((x, y, z) =>
                {
                    return o + y;
                }

                );
            }
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.basic.context02c.context02c
{
    // <Title> Compound operator</Title>
    // <Description>context
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
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
            var obj = new Test();
            bool ret = obj.RunTest();
            return ret ? 0 : 1;
        }

        public struct MySt
        {
            public MySt(ulong? f)
            {
                field = f;
            }

            public ulong? field;
        }

        public dynamic RunTest()
        {
            dynamic[] darr = new[]
            {
            "X", "0", "Y"
            }

            ;
            List<dynamic> list = new List<dynamic>();
            list.AddRange(darr);
            bool ret = "X0YY" == Method()(list[0] += list[1], list[2]);
            ret &= "YX" == this[list[2]](null, darr[0]);
            var va = new List<dynamic>
            {
            new MySt(null), new MySt(3), new MySt(5), new MySt(7), new MySt(11), new MySt(13)}

            ;
            var q =
                from i in new[]
                {
                va[1].field *= 11, va[2].field *= va[2].field %= 19, va[3].field += va[4].field *= va[5].field
                }

                where (0 < (va[5].field += va[3].field -= 2))
                select i;
            dynamic idx = 0;
            foreach (var v in q)
            {
                if (0 == idx)
                {
                    ret &= (33 == v);
                    // System.Console.WriteLine("v1 {0}", v);
                }
                else if (idx == 1)
                {
                    // System.Console.WriteLine("v2 {0}", v);
                    ret &= (25 == v);
                }
                else if (idx == 2)
                {
                    // System.Console.WriteLine("v3 {0}", v);
                    ret &= (150 == v);
                }

                idx++;
            }

            return ret;
        }

        public delegate dynamic MyDel(dynamic p1, dynamic p2 = default(object), long p3 = 100);
        public dynamic Method()
        {
            MyDel md = delegate (dynamic d, dynamic o, long n)
            {
                d += o += o;
                return d;
            }

            ;
            return md;
        }

        public dynamic this[dynamic o]
        {
            get
            {
                return new MyDel((x, y, z) =>
                {
                    return o + y;
                }

                );
            }
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.basic.context03.context03
{
    // <Title> Compound operator (Regression) </Title>
    // <Description>context
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>

    public class Test
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic[] da = new[]
            {
            "X", "Y"
            }

            ;
            var v = M()(da[0] += "Z");
            return (v == "XZ") ? 0 : 1;
        }

        public delegate string MyDel(string s);
        public static MyDel M()
        {
            return new MyDel(x =>
            {
                return x;
            }

            );
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.basic.bug741491array.bug741491array
{
    // <Title> Compound operator (Regression) </Title>
    // <Description>LHS of compound op with dynamic array/index access
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>

    public class Test
    {
        public dynamic this[int i]
        {
            get
            {
                return 0;
            }

            set
            {
            }
        }

        public int this[int i, int j]
        {
            get
            {
                return i + j;
            }

            set
            {
            }
        }

        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            bool ret = true;
            var x = new Test();
            x[i: 0] += null; // [IL] stack overflow
            x[i: 1] -= 1;
            x[i: -0]++; // ICE
            x[i: -1]--; // ICE
            dynamic d = 3;
            x[0, j: 1] += d;
            x[i: 1, j: -1] -= d;
            ret &= x.Test01();
            ret &= x.Test02();
            ret &= x.Test03();
            return ret ? 0 : 1;
        }

        private bool Test01()
        {
            bool ret = true;
            dynamic[] dary = new dynamic[]
            {
            100, 200, 300
            }

            ;
            dary[0]++;
            ret &= 101 == dary[0];
            dary[1]--;
            ret &= 199 == dary[1];
            if (!ret)
                System.Console.WriteLine("Test01 fail - ++/--");
            return ret;
        }

        private bool Test02()
        {
            bool ret = true;
            dynamic[] dary = new dynamic[]
            {
            0, -1, 1
            }

            ;
            dary[0] += null;
            dary[1] += 2;
            ret &= 1 == dary[1];
            dary[2] -= 1;
            ret &= 0 == dary[2];
            if (!ret)
                System.Console.WriteLine("Test02 fail - +=/-=");
            return ret;
        }

        private bool Test03()
        {
            bool ret = true;
            int[] iary = new[]
            {
            -1, -2, -3
            }

            ;
            dynamic d = 3;
            iary[0] += d;
            ret &= 2 == iary[0];
            iary[1] -= d;
            ret &= -5 == iary[1];
            if (!ret)
                System.Console.WriteLine("Test03 fail - +=/-=");
            return ret;
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.operate.compound.basic.using01.using01
{
    // <Title> Dynamic modification of a using variable </Title>
    // <Description>
    // Different from static behavior, no exception.
    // </Description>
    // <RelatedBugs></RelatedBugs>
    // <Expects Status=success></Expects>
    //<Expects Status=warning>\(13,16\).*CS0649</Expects>
    using System;

    public class TestClass
    {
        [Fact]
        public void RunTest()
        {
            A.DynamicCSharpRunTest();
        }
    }

    public struct A : IDisposable
    {
        public int X;

        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            using (dynamic a = new A())
            {
                a.X++; //no Exception here in dynamic call.
            }

            return 0;
        }

        public void Dispose()
        {
        }
    }
}
