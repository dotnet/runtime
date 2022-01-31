// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib01.dlgatedeclarelib01
{
    // <Area> Dynamic type in delegates </Area>
    // <Title> Delegate declaration at top level or under namespaces</Title>
    // <Description></Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects status=success></Expects>
    // <Code>

    public delegate dynamic D001(dynamic d);
    public delegate object D002(dynamic d, object o);
    public delegate dynamic D003(ref dynamic d1, object o, out dynamic d3);
    public delegate void D004(ref int n, dynamic[] d1, params dynamic[] d2);
    namespace DynNamespace01
    {
        public interface DynInterface01
        {
        }

        public class DynClass01
        {
            public int n = 0;
        }

        public struct DynStruct01
        {
        }

        public delegate dynamic D101(dynamic d, DynInterface01 i);
        public delegate void D102(ref DynClass01 c, dynamic d1, ref object d2);
        namespace DynNamespace02
        {
            public delegate void D201(ref dynamic d1, DynClass01 c, out dynamic[] d2, DynStruct01 st);
            public delegate dynamic D202(DynStruct01 st, params object[] d2);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgate001.dlgate001
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib01.dlgatedeclarelib01;
    // <Area> Dynamic type in delegates </Area>
    // <Title> Delegate instantiation</Title>
    // <Description> interchangeable dynamic and object parameters </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects status=success></Expects>

    namespace nms
    {
        public class Foo
        {
            // public delegate dynamic D001(dynamic v);
            public static dynamic M01(dynamic v)
            {
                return 0x01;
            }

            public object M02(dynamic v)
            {
                return 0x02;
            }

            public dynamic M03(object v)
            {
                return 0x03;
            }

            public static dynamic M04(object v)
            {
                return 0x04;
            }

            // public delegate object D002(dynamic d, object o);
            public static object M05(dynamic v1, object v2)
            {
                return 0x05;
            }

            public object M06(object v1, dynamic v2)
            {
                return 0x06;
            }

            public static dynamic M07(dynamic v1, object v2)
            {
                return 0x07;
            }

            public dynamic M08(object v1, dynamic v2)
            {
                return 0x08;
            }

            // dynamic D003(ref dynamic d1, object o, out dynamic d3);
            public static dynamic M09(ref dynamic v1, object v2, out dynamic v3)
            {
                v3 = null;
                return 0x09;
            }

            public object M0A(ref object v1, object v2, out object v3)
            {
                v3 = null;
                return 0x0A;
            }

            public object M0B(ref dynamic v1, dynamic v2, out dynamic v3)
            {
                v3 = null;
                return 0x0B;
            }

            // public delegate void D004(dynamic[] d1, params dynamic[] d2);
            public static void M0C(ref int n, dynamic[] v1, params dynamic[] v2)
            {
                n += 0x0C;
            }

            public void M0D(ref int n, object[] v1, params object[] v2)
            {
                n += 0x0D;
            }
        }

        public class Start
        {
            private static int s_retval = (int)((1 + 0x0D) * 0x0D) / 2;
            // field
            private static D001 s_del001 = null;
            private static D003 s_del003 = null;
            private static dynamic s_sd = null;
            private static object s_so = new object();
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                Foo foo = new Foo();
                dynamic d = new object();
                object o = null;
                s_del001 = new D001(Foo.M01);
                s_retval -= (int)s_del001(s_sd);
                D001 d001 = new D001(foo.M02);
                s_retval -= (int)d001(o);
                d001 = new D001(foo.M03);
                s_retval -= (int)d001(s_sd);
                s_del001 = new D001(Foo.M04);
                s_retval -= (int)s_del001(d);
                D002 del002 = null;
                del002 = new D002(Foo.M05);
                s_retval -= (int)del002(d, o);
                D002 d002 = new D002(foo.M06);
                s_retval -= (int)d002(o, s_sd);
                del002 = new D002(Foo.M07);
                s_retval -= (int)del002(s_so, o);
                d002 = new D002(foo.M08);
                s_retval -= (int)d002(s_sd, s_so);
                s_del003 = new D003(Foo.M09);
                s_retval -= (int)s_del003(ref d, s_so, out s_sd);
                D003 d003 = new D003(foo.M0A);
                // Ex: Null ref
                // retval -= (int)d003(ref o, sd, out so);
                s_retval -= (int)d003(ref o, o, out o);
                d003 = new D003(foo.M0B);
                // Ex: Null ref
                // retval -= (int)d003(ref so, d, out d);
                s_retval -= (int)d003(ref s_so, s_so, out s_so);
                dynamic[] dary = new object[]
                {
                }

                ;
                int ret = 0;
                D004 del004 = new D004(Foo.M0C);
                del004(ref ret, new object[]
                {
                }

                , dary);
                s_retval -= ret;
                del004 = new D004(foo.M0D);
                ret = 0;
                del004(ref ret, new dynamic[]
                {
                }

                , dary);
                s_retval -= ret;
                return s_retval;
            }
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgate002.dlgate002
{
    // <Area> Dynamic type in delegates </Area>
    // <Title> Delegate instantiation</Title>
    // <Description> delegate can be assigned by ternary operator| +=, compared for equality
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects status=success></Expects>

    namespace nms
    {
        public class Foo
        {
            // DynNamespace01: public delegate dynamic D101(dynamic d, DynInterface01 i);
            public static dynamic M01(dynamic v1, ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib01.dlgatedeclarelib01.DynNamespace01.DynInterface01 v2)
            {
                return 0x01;
            }

            public dynamic M02(object v1, ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib01.dlgatedeclarelib01.DynNamespace01.DynInterface01 v2)
            {
                return 0x01;
            }

            // DynNamespace01: public delegate void D102(DynClass01 c, ref dynamic d1, ref object d2)
            public static void M03(ref ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib01.dlgatedeclarelib01.DynNamespace01.DynClass01 v1, dynamic v2, ref object v3)
            {
                v1.n = 3;
            }

            public void M04(ref ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib01.dlgatedeclarelib01.DynNamespace01.DynClass01 v1, object v2, ref dynamic v3)
            {
                v1.n = 4;
            }

            // DynNamespace01:
            //  public delegate void D201(ref dynamic d1, DynClass01[] c, out dynamic d2, DynStruct01 st)
            public void M05(ref dynamic v1, ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib01.dlgatedeclarelib01.DynNamespace01.DynClass01 v2, out dynamic[] v3, ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib01.dlgatedeclarelib01.DynNamespace01.DynStruct01 v4)
            {
                v1 = 5;
                v3 = null;
            }

            public static void M06(ref object v1, ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib01.dlgatedeclarelib01.DynNamespace01.DynClass01 v2, out dynamic[] v3, ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib01.dlgatedeclarelib01.DynNamespace01.DynStruct01 v4)
            {
                v1 = 6;
                v3 = null;
            }

            public void M07(ref object v1, ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib01.dlgatedeclarelib01.DynNamespace01.DynClass01 v2, out object[] v3, ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib01.dlgatedeclarelib01.DynNamespace01.DynStruct01 v4)
            {
                v1 = 7;
                v3 = null;
            }

            // DynNamespace01:
            //   public delegate dynamic D202(DynStruct01 st, params object[] d2)
            public static dynamic M08(ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib01.dlgatedeclarelib01.DynNamespace01.DynStruct01 v1, params object[] v2)
            {
                return 0x08;
            }

            public object M09(ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib01.dlgatedeclarelib01.DynNamespace01.DynStruct01 v1, params dynamic[] v2)
            {
                return 0x09;
            }
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib02.dlgatedeclarelib02
{
    // <Area> Dynamic type in delegates </Area>
    // <Title> Delegate declaration under other types</Title>
    // <Description></Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects status=success></Expects>
    // <Code>

    namespace DynNamespace01
    {
        public class DynClass
        {
            public delegate string D001(object v1, dynamic v2, DynEnum v3);
            public struct DynStruct
            {
                public delegate string D101(DynEnum v1, ref object v2, params dynamic[] v3);
                public delegate string D102(DynEnum v1, ref dynamic v2, params object[] v3);
            }
        }

        public enum DynEnum
        {
            item0,
            item1,
            item2,
            item3,
            item4,
            item5,
            item6
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgate003.dlgate003
{
    // <Area> Dynamic type in delegates </Area>
    // <Title> Delegate instantiation</Title>
    // <Description> delegates can be aggregated in arrays and compared for equality.
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects status=success></Expects>

    namespace nms
    {
        public class Foo
        {
            // DynNamespace01.DynClass:
            //  public delegate string D001(object v1, dynamic v2, ref DynEnum v3)
            public static string M01(object v1, object v2, ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib02.dlgatedeclarelib02.DynNamespace01.DynEnum v3)
            {
                return v3.ToString();
            }

            public string M11(object v1, object v2, ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib02.dlgatedeclarelib02.DynNamespace01.DynEnum v3)
            {
                return v3.ToString();
            }

            public string M21(object v1, object v2, ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib02.dlgatedeclarelib02.DynNamespace01.DynEnum v3)
            {
                return v3.ToString();
            }

            public static string M02(dynamic v1, dynamic v2, ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib02.dlgatedeclarelib02.DynNamespace01.DynEnum v3)
            {
                return v3.ToString();
            }

            // DynNamespace01.DynClass.DynStruct:
            //  public delegate string D101(ref DynEnum v1, ref object v2, params dynamic[] v3);
            public string M03(ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib02.dlgatedeclarelib02.DynNamespace01.DynEnum v1, ref object v2, params dynamic[] v3)
            {
                return v1.ToString();
            }

            // DynNamespace01.DynClass.DynStruct:
            //  blic delegate string D101(ref DynEnum v1, ref dynamic v2, params object[] v3)
            public string M04(ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib02.dlgatedeclarelib02.DynNamespace01.DynEnum v1, ref dynamic v2, params object[] v3)
            {
                return v1.ToString();
            }
        }

        public class Start
        {
            // field
            private static ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib02.dlgatedeclarelib02.DynNamespace01.DynClass.D001[] s_d001 = new ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib02.dlgatedeclarelib02.DynNamespace01.DynClass.D001[3];
            private static dynamic s_sd = null;
            private static object s_so = new object();
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                bool ret = true;
                Foo foo = new Foo();
                dynamic d = new object();
                object o = null;
                s_d001[0] = new ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib02.dlgatedeclarelib02.DynNamespace01.DynClass.D001(foo.M11);
                s_d001[1] = new ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib02.dlgatedeclarelib02.DynNamespace01.DynClass.D001(foo.M21);
                s_d001[2] = new ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib02.dlgatedeclarelib02.DynNamespace01.DynClass.D001(Foo.M01);
                ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib02.dlgatedeclarelib02.DynNamespace01.DynClass.D001[] ary101 =
                {
                new ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib02.dlgatedeclarelib02.DynNamespace01.DynClass.D001(Foo.M01), new ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib02.dlgatedeclarelib02.DynNamespace01.DynClass.D001(foo.M21), new ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib02.dlgatedeclarelib02.DynNamespace01.DynClass.D001(foo.M11)}

                ;
                if (s_d001[0] != ary101[2])
                {
                    ret = false;
                }

                if (s_d001[1] != ary101[1])
                {
                    ret = false;
                }

                if (s_d001[2] != ary101[0])
                {
                    ret = false;
                }

                ary101[0] = new ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib02.dlgatedeclarelib02.DynNamespace01.DynClass.D001(Foo.M02);
                string st1 = ary101[0](s_so, s_sd, ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib02.dlgatedeclarelib02.DynNamespace01.DynEnum.item1);
                if (ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib02.dlgatedeclarelib02.DynNamespace01.DynEnum.item1.ToString() != s_d001[0](s_so, d, ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib02.dlgatedeclarelib02.DynNamespace01.DynEnum.item1))
                {
                    ret = false;
                }

                if (ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib02.dlgatedeclarelib02.DynNamespace01.DynEnum.item1.ToString() != st1)
                {
                    ret = false;
                }

                dynamic[] dary = new object[]
                {
                }

                ;
                object[] oary = new object[]
                {
                }

                ;
                ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib02.dlgatedeclarelib02.DynNamespace01.DynClass.DynStruct.D101 d101 = null;
                d101 = new ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib02.dlgatedeclarelib02.DynNamespace01.DynClass.DynStruct.D101(foo.M03);
                ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib02.dlgatedeclarelib02.DynNamespace01.DynClass.DynStruct.D101 d111 = new ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib02.dlgatedeclarelib02.DynNamespace01.DynClass.DynStruct.D101(foo.M04);
                st1 = d101(ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib02.dlgatedeclarelib02.DynNamespace01.DynEnum.item2, ref d, dary);
                if (ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib02.dlgatedeclarelib02.DynNamespace01.DynEnum.item2.ToString() != st1)
                {
                    ret = false;
                }

                st1 = d101(ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib02.dlgatedeclarelib02.DynNamespace01.DynEnum.item3, ref o, oary);
                if (ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib02.dlgatedeclarelib02.DynNamespace01.DynEnum.item3.ToString() != st1)
                {
                    ret = false;
                }

                st1 = d111(ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib02.dlgatedeclarelib02.DynNamespace01.DynEnum.item3, ref o, oary);
                if (ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib02.dlgatedeclarelib02.DynNamespace01.DynEnum.item3.ToString() != st1)
                {
                    ret = false;
                }

                st1 = d111(ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib02.dlgatedeclarelib02.DynNamespace01.DynEnum.item4, ref s_so, dary);
                if (ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib02.dlgatedeclarelib02.DynNamespace01.DynEnum.item4.ToString() != st1)
                {
                    ret = false;
                }

                ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib02.dlgatedeclarelib02.DynNamespace01.DynClass.DynStruct.D102 d102 = new ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib02.dlgatedeclarelib02.DynNamespace01.DynClass.DynStruct.D102(foo.M04);
                st1 = d102(ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib02.dlgatedeclarelib02.DynNamespace01.DynEnum.item5, ref d, dary);
                if (ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib02.dlgatedeclarelib02.DynNamespace01.DynEnum.item5.ToString() != st1)
                {
                    ret = false;
                }

                ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib02.dlgatedeclarelib02.DynNamespace01.DynClass.DynStruct.D102 d122 = new ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib02.dlgatedeclarelib02.DynNamespace01.DynClass.DynStruct.D102(foo.M03);
                st1 = d122(ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib02.dlgatedeclarelib02.DynNamespace01.DynEnum.item6, ref s_so, oary);
                if (ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib02.dlgatedeclarelib02.DynNamespace01.DynEnum.item6.ToString() != st1)
                {
                    ret = false;
                }

                return ret ? 0 : 1;
            }
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib03.dlgatedeclarelib03
{
    // <Area> Dynamic type in delegates </Area>
    // <Title> Delegate declaration with different modifiers</Title>
    // <Description></Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects status=success></Expects>
    // <Code>

    namespace DynNamespace31
    {
        public class DynClassBase
        {
            public delegate void PublicDel(dynamic v, ref int n);
            private delegate void PrivateDel(dynamic d);
        }

        public class DynClassDrived : DynClassBase
        {
            private new delegate void PublicDel(dynamic v, ref int n);
            // protected: can not access if in dll
            public delegate long InternalDel(sbyte v1, dynamic v2, short v3, dynamic v4, int v5, dynamic v6, long v7, dynamic v8, dynamic v9);
            protected delegate void ProtectedDel(dynamic d);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgate004.dlgate004
{
    // <Area> Dynamic type in delegates </Area>
    // <Title> Delegate instantiation</Title>
    // <Description> delegates can be combined by using +, - +=, -=
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects status=success></Expects>
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib03.dlgatedeclarelib03.DynNamespace31;

    namespace nms
    {
        public class Foo
        {
            // DynNamespace31.DynClassBase: public delegate void PublicDel(dynamic v, ref int n)
            // DynNamespace31.DynClassDrived: new delegate void NewPublicDel(dynamic v, ref int n);
            public void M01(dynamic v, ref int n)
            {
                n = 1;
            }

            internal void M02(dynamic v, ref int n)
            {
                n = 2;
            }

            public void M03(object v, ref int n)
            {
                n = 4;
            }

            internal void M04(object v, ref int n)
            {
                n = 8;
            }

            // DynNamespace31.DynClassDrived:
            // internal delegate long InternalDel(sbyte v1, dynamic v2, short v3, dynamic v4, int v5, dynamic v6, long v7, dynamic v8, dynamic v9)
            public static long M11(sbyte v1, dynamic v2, short v3, dynamic v4, int v5, dynamic v6, long v7, dynamic v8, dynamic v9)
            {
                return v1 + (int)v2 + v3 + (int)v4 + v5 + (int)v6 + v7 + (int)v8 + (int)v9;
            }

            internal static long M12(sbyte v1, dynamic v2, short v3, dynamic v4, int v5, dynamic v6, long v7, dynamic v8, object v9)
            {
                return v1 + v3 + v5 + v7 + (int)v9;
            }

            public static long M13(sbyte v1, dynamic v2, short v3, object v4, int v5, dynamic v6, long v7, dynamic v8, object v9)
            {
                return (int)v2 + (int)v4 + (int)v6 + (int)v8;
            }

            // DynNamespace31.DynClassDrived:
            //  public static delegate int StPublicDel(dynamic v1, decimal v2);
            internal int M21(dynamic v1, decimal v2)
            {
                return 33;
            }

            internal int M22(object v1, decimal v2)
            {
                return 66;
            }

            internal int M23(dynamic v1, decimal v2)
            {
                return 99;
            }

            internal int M24(object v1, decimal v2)
            {
                return -1;
            }
        }

        public struct Start
        {
            // field
            private static DynClassDrived.InternalDel s_interDel;


            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                bool ret = true;
                Foo foo = new Foo();
                dynamic d = new object();
                object o = new object();
                DynClassBase.PublicDel pd01 = new DynClassBase.PublicDel(foo.M01);
                DynClassDrived.PublicDel pd02 = new DynClassBase.PublicDel(foo.M02);
                DynClassBase.PublicDel pd03 = new DynClassBase.PublicDel(foo.M03);
                DynClassDrived.PublicDel pd04 = new DynClassBase.PublicDel(foo.M04);
                DynClassBase.PublicDel pd05 = pd01 + pd02;
                pd05 += new DynClassBase.PublicDel(pd01);
                DynClassBase.PublicDel pd06 = pd03 + new DynClassDrived.PublicDel(foo.M04);
                pd06 = pd04 + pd05 + pd06; // M04+M01+M02+M01+M03+M04 => 8 (last one)
                int n = 0;
                pd06(d, ref n);
                if (8 != n)
                {
                    ret = false;
                }

                pd06 -= pd04;
                pd06(d, ref n);
                if (4 != n)
                {
                    ret = false;
                }

                pd06 -= pd01;
                pd06(d, ref n);
                if (4 != n)
                {
                    ret = false;
                }

                //
                s_interDel = new DynClassDrived.InternalDel(Foo.M11);
                DynClassDrived.InternalDel dd01 = new DynClassDrived.InternalDel(s_interDel); //45
                DynClassDrived.InternalDel dd02 = new DynClassDrived.InternalDel(Foo.M12); // 25
                DynClassDrived.InternalDel dd03 = new DynClassDrived.InternalDel(Foo.M13); // 20
                DynClassDrived.InternalDel dd04 = dd01; // 45
                dd04 += dd02; // 25
                DynClassDrived.InternalDel dd05 = dd02 + dd03;
                dd04 += dd05 + new DynClassDrived.InternalDel(dd05); // new(1)+2+ 2+3 + new(2+3)
                long lg = dd04(1, 2, 3, 4, 5, 6, 7, 8, 9);
                if (20 != lg) // dd03
                {
                    ret = false;
                }

                dd04 = dd04 - dd02 - dd02 - dd03; // new(1)+ new(2+3)
                lg = dd04(1, 2, 3, 4, 5, 6, 7, 8, 9);
                if (20 != lg) // dd03
                {
                    ret = false;
                }

                dd04 -= new DynClassDrived.InternalDel(dd05); // new(1)
                lg = dd04(1, 2, 3, 4, 5, 6, 7, 8, 9);
                if (45 != lg)
                {
                    ret = false;
                }

                return ret ? 0 : 1;
            }
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib04.dlgatedeclarelib04
{
    // <Area> Dynamic type in delegates </Area>
    // <Title> Delegate declaration with generic types</Title>
    // <Description></Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects status=success></Expects>
    // <Code>

    public delegate R D001<R>(dynamic d);
    namespace DynNamespace41
    {
        public delegate void D002<T1, T2>(T1 t1, T2 t2);
        public class DynClass
        {
            public delegate R D011<T, R>(T[] v1, dynamic v2);
            // internal: can not access if in dll
            public delegate dynamic D012<T>(ref T v1, ref dynamic[] v2);
        }

        public struct DynStruct
        {
            public delegate R D021<T, R>(out dynamic d, out T t);
            // internal: can not access if in dll
            public delegate dynamic D022<T1, T2, T3>(T1 t1, ref T2 t2, out T3 t3, params dynamic[] d);
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib05.dlgatedeclarelib05
{
    // <Area> Dynamic type in delegates </Area>
    // <Title> Delegate declaration with optional parameters</Title>
    // <Description></Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects status=success></Expects>
    // <Code>

    public delegate void D001(dynamic d = null); // not allow init with values other than null:(
    public delegate void D002(dynamic v1, object v2 = null, dynamic v3 = null /*DynNamespace51.DynClass.strDyn*/);
    namespace DynNamespace51
    {
        public delegate void D011(dynamic d1 = null, params dynamic[] d2);
        public class DynClass
        {
            public const string strDyn = "dynamic";
            // internal: can not access if in dll
            public delegate int D021(params dynamic[] d);
            public delegate void D022(DynStruct01 v1, dynamic v2 = null, int v3 = -1);
            public struct DynStruct
            {
                public delegate dynamic D031(DynClass01 v1, DynStruct01 v2 = new DynStruct01(), dynamic[] v3 = null);
            }
        }

        public class DynClass01
        {
        }

        public struct DynStruct01
        {
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgate006.dlgate006
{
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib05.dlgatedeclarelib05;
    // <Area> Dynamic type in delegates </Area>
    // <Title> Delegate instantiation</Title>
    // <Description> delegates can has optional parameters
    //      default value of dynamic can only be set to null :(
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects status=success></Expects>
    using System;
    using ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgatedeclarelib05.dlgatedeclarelib05.DynNamespace51;

    namespace nms
    {
        public class Foo
        {
            public static int val = -1;
            public static int? nval = -1;
            public static string str = string.Empty;
            // public delegate void D001(dynamic d = null);
            public void M01(dynamic d = null)
            {
                val = d;
            }

            internal void M02(dynamic d)
            {
                nval = d;
            }

            // internal delegate void D002(dynamic v1, object v2 = null, dynamic v3 = null)
            internal static void M11(dynamic v1, object v2, dynamic v3 = null)
            {
                str = v3;
            }

            // public delegate void D011(dynamic d1 = null, params dynamic[] d2);
            public void M21(dynamic d1, params dynamic[] d2)
            {
                nval = d1;
            }

            // internal delegate int D021(params dynamic[] d);
            internal static int M31(params dynamic[] d)
            {
                return 31;
            }

            // public delegate void D022(DynStruct01 v1, dynamic v2 = 0.123f, int v3 = -1);
            internal static void M41(DynStruct01 v1, dynamic v2 = null, int v3 = 41)
            {
                val = v3;
            }

            // public delegate dynamic D031(DynClass01 v1, DynStruct01 v2 = new DynStruct01(), dynamic[] v3 = null);
            public static dynamic M51(DynClass01 v1, DynStruct01 v2 = new DynStruct01(), dynamic[] v3 = null)
            {
                return 51;
            }
        }

        public class TestClass
        {
            [Fact]
            public void RunTest()
            {
                Start.DynamicCSharpRunTest();
            }
        }

        public struct Start
        {
            // field
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                bool ret = true;
                Foo foo = new Foo();
                dynamic d = new object();
                D001 dd01 = new D001(foo.M01);
                dd01(123); // use delegate's default value

                if (123 != Foo.val)
                {
                    ret = false;
                }

                dd01(100);
                if (100 != Foo.val)
                {
                    ret = false;
                }

                Foo.val = -1;
                D001 dd11 = new D001(foo.M02);
                dd11();
                if (null != Foo.nval)
                {
                    ret = false;
                }

                dd11(101);
                if (101 != Foo.nval)
                {
                    ret = false;
                }

                D002 dd021;
                dd021 = new D002(Foo.M11);
                dd021(88);
                if (null != Foo.str)
                {
                    ret = false;
                }

                dd021(66, "boo", "Dah");
                if (0 != string.CompareOrdinal("Dah", Foo.str))
                {
                    ret = false;
                }

                dynamic dstr = "Hello";
                dd021(88, null, dstr);
                if ((string)dstr != Foo.str)
                {
                    ret = false;
                }

                D011 dd011 = new D011(foo.M21);
                dd011();
                if (Foo.nval.HasValue)
                {
                    ret = false;
                }

                dd011(102);
                if (!Foo.nval.HasValue || 102 != Foo.nval)
                {
                    ret = false;
                }

                DynClass.D021 dd21 = new DynClass.D021(Foo.M31);
                int n = dd21();
                n = dd21(d);
                DynClass.D022 dd22 = null;
                dd22 = new DynClass.D022(Foo.M41);
                dd22(new DynStruct01());
                if (-1 != Foo.val) // 41
                {
                    ret = false;
                }

                dd22(new DynStruct01(), 0.780f, 103);
                if (103 != Foo.val)
                {
                    ret = false;
                }

                DynClass.DynStruct.D031 dd31 = new DynClass.DynStruct.D031(Foo.M51);
                n = dd31(new DynClass01());
                if (51 != n)
                {
                    ret = false;
                }

                n = dd31(null, new DynStruct01());
                if (51 != n)
                {
                    ret = false;
                }

                n = dd31(new DynClass01(), new DynStruct01(), null);
                if (51 != n)
                {
                    ret = false;
                }

                return ret ? 0 : 1;
            }
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgate007bug.dlgate007bug
{
    // <Area> Dynamic type in delegates </Area>
    // <Title> Delegate instantiation</Title>
    // <Description> Assert and NullRef Exception when call with delegate 2nd as out param </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects status=success></Expects>

    namespace nms
    {
        public delegate void DelOut(object v1, out object v2);
        public class Foo
        {
            public static void M01(object v1, out object v2)
            {
                v2 = null;
            }

            public static void M02(object v1, out dynamic v2)
            {
                v2 = null;
            }

            public static void M03(dynamic v1, out object v2)
            {
                v2 = null;
            }

            internal static void M04(dynamic v1, out dynamic v2)
            {
                v2 = null;
            }
        }

        public class Start
        {

            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                dynamic nd = null;
                dynamic d = new object();
                object no = null;
                object o = new object();
                DelOut d_oo = new DelOut(Foo.M01);
                DelOut d_od = new DelOut(Foo.M02);
                DelOut d_do = new DelOut(Foo.M03);
                DelOut d_dd = new DelOut(Foo.M03);
                // object, object
                d_oo(no, out o);
                d_od(o, out no);
                d_do(no, out o);
                d_od(o, out no);
                // object, dynamic
                d_oo(o, out nd);
                d_od(no, out d);
                d_do(o, out nd);
                d_od(no, out d);
                // Assert and Null Ref Exception
                // dynamic, object
                d_oo(nd, out o);
                d_od(nd, out no);
                d_do(d, out o);
                d_od(d, out no);
                // dynamic, dynamic
                d_oo(d, out d);
                d_od(d, out nd);
                d_do(nd, out d);
                d_od(nd, out nd);
                return 0;
            }
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgate008bug.dlgate008bug
{
    // <Area> Dynamic type in delegates </Area>
    // <Title> Delegate instantiation (Behavior is 'by design' as they are 2 different boxed instances)</Title>
    // <Description> Delegate: compare same delegates return false if the method is struct's not static method </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects status=success></Expects>

    namespace nms
    {
        public delegate void Del(dynamic v1);
        public struct Foo
        {
            public void MinStruct(dynamic v1)
            {
            }

            public static void SMinStruct(dynamic v1)
            {
            }
        }

        public class Bar
        {
            public void MinClass(dynamic v1)
            {
            }

            public static void SMinClass(dynamic v1)
            {
            }
        }

        public class Start
        {
            [Fact]
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                bool ret = true;
                Bar bar = new Bar();
                Del[] ary01 =
                {
                new Del(Foo.SMinStruct), new Del(bar.MinClass), new Del(Bar.SMinClass)}

                ;
                Del[] ary02 =
                {
                new Del(Foo.SMinStruct), new Del(bar.MinClass), new Del(Bar.SMinClass)}

                ;
                int idx = 0;
                foreach (Del d in ary01)
                {
                    if (d != ary02[idx])
                    {
                        ret = false;
                    }

                    idx++;
                }

                return ret ? 0 : 1;
            }
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.dlgate009bug.dlgate009bug
{
    // <Area> Dynamic type in delegates </Area>
    // <Title> Delegate instantiation</Title>
    // <Description>
    //    Delegate with params: ASSERT FAILED,(result) ? (GetOutputContext().m_bHadNamedAndOptionalArguments) : true, File: ... transformpass.cpp, Line: 234
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects status=success></Expects>

    namespace nms
    {
        internal delegate void DOptObj(params object[] ary);
        internal delegate void DOptDyn(params dynamic[] ary);
        public class Foo
        {
            public void M01(params dynamic[] d)
            {
            }

            public void M02(params object[] d)
            {
            }
        }

        public class TestClass
        {
            [Fact]
            public void RunTest()
            {
                Start.DynamicCSharpRunTest();
            }
        }

        public struct Start
        {
            public static void DynamicCSharpRunTest()
            {
                Assert.Equal(0, MainMethod());
            }

            public static int MainMethod()
            {
                Foo foo = new Foo();
                DOptObj dobj01 = new DOptObj(foo.M01);
                DOptObj dobj02 = new DOptObj(foo.M02);
                DOptDyn ddyn01 = new DOptDyn(foo.M01);
                DOptDyn ddyn02 = new DOptDyn(foo.M02);
                // Assert
                dobj01(1, 2, 3);
                dobj02(1, 2, 3);
                ddyn01(1, 2, 3);
                ddyn02(1, 2, 3);
                return 0;
            }
        }
    }
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.generic001.generic001
{
    // <Title>+= on a generic event does work</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
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
            dynamic d = new SubGenericClass<int>();
            d.myDel += (GenDlg<int>)null;
            d.myDel -= (GenDlg<int>)null;
            return 0;
        }

        public GenDlg<int> GetMe()
        {
            return null;
        }
    }

    public delegate T GenDlg<T>(int t);
    public class SubGenericClass<T>
    {
        public GenDlg<T> myDel;
        public void Foo<U>(U x)
        {
            GenDlg<U> d = null;
            d += (GenDlg<U>)null;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.generic002.generic002
{
    // <Title>+= on a generic event does work</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(39,28\).*CS0067</Expects>

    public class Program
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new SubGenericClass<int>();
            t.Foo(4);
            var p = new Program();
            dynamic d = new SubGenericClass<int>();
            d.myDel += (GenDlg<int>)p.GetMe();
            d.myDel -= (GenDlg<int>)p.GetMe();
            d = new SubGenericClass<string>();
            d.vEv += (GenDlg<string>)null;
            d.vEv -= (GenDlg<string>)null;
            return 0;
        }

        public GenDlg<int> GetMe()
        {
            return null;
        }
    }

    public delegate T GenDlg<out T>(int t);
    public class SubGenericClass<T>
    {
        public GenDlg<T> myDel;
        public event GenDlg<T> vEv;
        public void Foo<U>(U x)
        {
            GenDlg<U> d = null;
            d += (GenDlg<U>)null;
        }
    }
    //</Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.generic003.generic003
{
    // <Title>+= on a generic event does work</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(42,31\).*CS0067</Expects>

    public class Program
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new SubGenericClass<int>();
            t.Foo(4);
            dynamic d = new SubGenericClass<int>();
            d.myDel += (GenDlg<C<int>>)null;
            d.myDel -= (GenDlg<C<int>>)null;
            d = new SubGenericClass<string>();
            d.vEv += (GenDlg<C<string>>)null;
            d.vEv -= (GenDlg<C<string>>)null;
            return 0;
        }

        public GenDlg<int> GetMe()
        {
            return null;
        }
    }

    public delegate T GenDlg<T>(int t);
    public class C<T>
    {
    }

    public class SubGenericClass<T>
    {
        public GenDlg<C<T>> myDel;
        public event GenDlg<C<T>> vEv;
        public void Foo<U>(U x)
        {
            GenDlg<U> d = null;
            d += (GenDlg<U>)null;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.generic004.generic004
{
    // <Title>+= on a generic event does work</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(41,28\).*CS0067</Expects>

    public class Program
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new SubGenericClass<int>();
            t.Foo(4);
            dynamic d = new SubGenericClass<int>();
            d.myDel += (GenDlg<int>)(x => 4);
            d.myDel -= (GenDlg<int>)(x => 4);
            d = new SubGenericClass<string>();
            d.vEv += (GenDlg<string>)(x => "");
            d.vEv -= (GenDlg<string>)(x => "");
            return 0;
        }

        public GenDlg<int> GetMe()
        {
            return null;
        }
    }

    public delegate T GenDlg<T>(int t);
    public class SubGenericClass<T>
    {
        public GenDlg<T> myDel;
        public event GenDlg<T> vEv;
        public void Foo<U>(U x)
        {
            GenDlg<U> d = null;
            d += (GenDlg<U>)null;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.generic005.generic005
{
    // <Title>+= on a generic event does work</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(48,28\).*CS0067</Expects>

    public class C
    {
        public static implicit operator GenDlg<int>(C c)
        {
            return null;
        }
    }

    public class Program
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new SubGenericClass<int>();
            t.Foo(4);
            dynamic d = new SubGenericClass<int>();
            d.myDel += (GenDlg<int>)new C(); //RuntimeBinderException
            d.myDel -= (GenDlg<int>)(x => 4); //RuntimeBinderException
            d = new SubGenericClass<string>();
            d.vEv += (GenDlg<string>)(x => ""); //RuntimeBinderException
            d.vEv -= (GenDlg<string>)(x => ""); //RuntimeBinderException
            return 0;
        }

        public GenDlg<int> GetMe()
        {
            return null;
        }
    }

    public delegate T GenDlg<T>(int t);
    public class SubGenericClass<T>
    {
        public GenDlg<T> myDel;
        public event GenDlg<T> vEv;
        public void Foo<U>(U x)
        {
            GenDlg<U> d = null;
            d += (GenDlg<U>)null;
        }
    }
    // </Code>
}



namespace ManagedTests.DynamicCSharp.Conformance.dynamic.context.dlgateEvent.dlgate.generic006.generic006
{
    // <Title>+= on a generic event does work</Title>
    // <Description>
    // </Description>
    // <RelatedBugs></RelatedBugs>
    //<Expects Status=success></Expects>
    // <Code>
    //<Expects Status=warning>\(43,35\).*CS0067</Expects>

    public class C
    {
        public static implicit operator GenDlg<int>(C c)
        {
            return null;
        }
    }

    public class Program
    {
        [Fact]
        public static void DynamicCSharpRunTest()
        {
            Assert.Equal(0, MainMethod());
        }

        public static int MainMethod()
        {
            dynamic t = new SubGenericClass<int>();
            t.Foo(4);
            dynamic d = new SubGenericClass<int>();
            SubGenericClass<int>.myDel += (GenDlg<int>)(dynamic)new C();
            SubGenericClass<int>.vEv += (GenDlg<int>)(dynamic)new C();
            return 0;
        }

        public GenDlg<int> GetMe()
        {
            return null;
        }
    }

    public delegate T GenDlg<T>(int t);
    public class SubGenericClass<T>
    {
        public static GenDlg<T> myDel;
        public static event GenDlg<T> vEv;
        public void Foo<U>(U x)
        {
            GenDlg<U> d = null;
            d += (GenDlg<U>)null;
        }
    }
    // </Code>
}
