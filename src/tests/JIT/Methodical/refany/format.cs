// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace JitTest_format_cs
{
    public enum PlatformID
    {
        MacOSX = 6,
        Unix = 4,
        Win32NT = 2,
        Win32S = 0,
        Win32Windows = 1,
        WinCE = 3,
        Xbox = 5,
    }
    internal enum Mood
    {
        good,
        bad,
        worse
    }

    public class TestClass
    {
        private static String Format(TypedReference format, TypedReference _ref)
        {
            int length = __refvalue(format, String).Length;
            char[] chars = __refvalue(format, String).ToCharArray();

            String result = "";
            int arg = 0;
            for (int I = 0; I < length; I++)
            {
                if (chars[I] != '%')
                    result += chars[I];
                else
                {
                    I++;
                    if (I >= length)
                        throw new Exception();

                    bool FALSE = false;
                    bool TRUE = true;
                    TypedReference bLong = __makeref(FALSE);
                    if (chars[I] == 'l')
                    {
                        bLong = __makeref(TRUE);
                        I++;
                        if (I >= length)
                            throw new Exception();
                    }

                    if (arg++ == 1)
                        throw new Exception();

                    switch (chars[I])
                    {
                        case 'b':
                            if (__refvalue(bLong, bool))
                                throw new Exception();
                            if (__reftype(_ref) != typeof(bool))
                                throw new Exception();
                            if (__refvalue(_ref, bool))
                                result += "true";
                            else
                                result += "false";
                            break;

                        case 'd':
                            if (__refvalue(bLong, bool))
                            {
                                if (__reftype(_ref) != typeof(long))
                                    throw new Exception();
                                result += __refvalue(_ref, long).ToString();
                            }
                            else
                            {
                                if (__reftype(_ref) != typeof(int))
                                    throw new Exception();
                                result += __refvalue(_ref, int).ToString();
                            }
                            break;

                        case 'u':
                            if (__refvalue(bLong, bool))
                            {
                                if (__reftype(_ref) != typeof(UInt64))
                                    throw new Exception();
                                result += __refvalue(_ref, ulong).ToString();
                            }
                            else
                            {
                                if (__reftype(_ref) != typeof(uint))
                                    throw new Exception();
                                result += __refvalue(_ref, uint).ToString();
                            }
                            break;

                        case 'f':
                            if (__refvalue(bLong, bool))
                            {
                                if (__reftype(_ref) != typeof(double))
                                    throw new Exception();
                                result += __refvalue(_ref, double).ToString();
                            }
                            else
                            {
                                if (__reftype(_ref) != typeof(float))
                                    throw new Exception();
                                result += __refvalue(_ref, float).ToString();
                            }
                            break;

                        case 's':
                            if (__refvalue(bLong, bool))
                                throw new Exception();
                            if (__reftype(_ref) != typeof(String))
                                throw new Exception();
                            result += __refvalue(_ref, String) != null ? __refvalue(_ref, String) : "(null)";
                            break;

                        case 't':
                            if (__refvalue(bLong, bool))
                                throw new Exception();
                            if (__reftype(_ref) != typeof(DateTime))
                                throw new Exception();
                            result += __refvalue(_ref, DateTime).ToString();
                            break;

                        case 'p':
                            if (__refvalue(bLong, bool))
                                throw new Exception();
                            if (__reftype(_ref) != typeof(PlatformID))
                                throw new Exception();
                            result += __refvalue(_ref, PlatformID).ToString();
                            break;

                        case 'e':
                            if (__refvalue(bLong, bool))
                                throw new Exception();
                            if (__reftype(_ref) != typeof(Mood))
                                throw new Exception();
                            switch (__refvalue(_ref, Mood))
                            {
                                case Mood.good:
                                    result += "good";
                                    break;
                                case Mood.bad:
                                    result += "bad";
                                    break;
                                case Mood.worse:
                                    result += "worse";
                                    break;
                                default:
                                    throw new Exception();
                            }
                            break;

                        default:
                            throw new Exception();
                    }
                }
            }
            return result;
        }

        private static void Test(String format, TypedReference arg, String result)
        {
            String s = Format(__makeref(format), arg);
            if (s != result)
            {
                throw new Exception();
            }
        }

        private static void TestLocals()
        {
            int d = 10;
            uint u = 11u;
            long l = 12;
            ulong ul = 13u;
            float f = 14.0f;
            double dbl = 15.0d;
            bool b = true;
            DateTime t = new DateTime(100, 10, 1);
            PlatformID pid = PlatformID.Win32NT;
            Mood mood = Mood.good;
            Test("{%d}", __makeref(d), "{10}");
            Test("{%u}", __makeref(u), "{11}");
            Test("{%ld}", __makeref(l), "{12}");
            Test("{%lu}", __makeref(ul), "{13}");
            Test("{%f}", __makeref(f), "{14}");
            Test("{%lf}", __makeref(dbl), "{15}");
            Test("{%b}", __makeref(b), "{true}");
            Test("{%t}", __makeref(t), "{" + t.ToString() + "}");
            Test("{%p}", __makeref(pid), "{Win32NT}");
            Test("{%e}", __makeref(mood), "{good}");
        }

        private int _m_d = 20;
        private static uint s_m_u = 21u;
        private long _m_l = 22;
        private static ulong s_m_ul = 23u;
        private float _m_f = 24.0f;
        private double _m_dbl = 25.0d;
        private bool _m_b = false;
        private static DateTime s_m_t = new DateTime(100, 10, 1);
        private PlatformID _m_pid = PlatformID.Win32NT;
        private Mood _m_mood = Mood.good;

        private void TestFields()
        {
            Test("{%d}", __makeref(_m_d), "{20}");
            Test("{%u}", __makeref(s_m_u), "{21}");
            Test("{%ld}", __makeref(_m_l), "{22}");
            Test("{%lu}", __makeref(s_m_ul), "{23}");
            Test("{%f}", __makeref(_m_f), "{24}");
            Test("{%lf}", __makeref(_m_dbl), "{25}");
            Test("{%b}", __makeref(_m_b), "{false}");
            Test("{%t}", __makeref(s_m_t), "{" + s_m_t.ToString() + "}");
            Test("{%p}", __makeref(_m_pid), "{Win32NT}");
            Test("{%e}", __makeref(_m_mood), "{good}");
        }

        private static void DoTestArgSlots(ref int d, ref uint u, ref long l,
            ref ulong ul, ref float f, ref double dbl, ref bool b,
            ref DateTime t, ref PlatformID pid)
        {
            Test("{%d}", __makeref(d), "{20}");
            Test("{%u}", __makeref(u), "{21}");
            Test("{%ld}", __makeref(l), "{22}");
            Test("{%lu}", __makeref(ul), "{23}");
            Test("{%f}", __makeref(f), "{24}");
            Test("{%lf}", __makeref(dbl), "{25}");
            Test("{%b}", __makeref(b), "{false}");
            Test("{%t}", __makeref(t), "{" + t.ToString() + "}");
            Test("{%p}", __makeref(pid), "{2}");
        }

        private static void TestArgSlots()
        {
            int d = 20;
            uint u = 21u;
            long l = 22;
            ulong ul = 23u;
            float f = 24.0f;
            double dbl = 25.0d;
            bool b = false;
            DateTime t = new DateTime(100, 10, 1);
            PlatformID pid = PlatformID.Win32NT;
            DoTestArgSlots(ref d, ref u, ref l, ref ul, ref f, ref dbl, ref b, ref t, ref pid);
        }

        private static void TestArrayElem()
        {
            int[] d = new int[] { 10 };
            uint[] u = new uint[] { 11u };
            long[] l = new long[] { 12 };
            ulong[] ul = new ulong[] { 13u };
            float[] f = new float[] { 14.0f };
            double[] dbl = new double[] { 15.0d };
            bool[] b = new bool[] { true };
            DateTime[] t = new DateTime[200];
            t[1] = new DateTime(100, 10, 1);
            PlatformID[] pid = new PlatformID[] { PlatformID.Win32NT };
            Mood[] mood = new Mood[] { Mood.good };
            Test("{%d}", __makeref(d[0]), "{10}");
            Test("{%u}", __makeref(u[0]), "{11}");
            Test("{%ld}", __makeref(l[0]), "{12}");
            Test("{%lu}", __makeref(ul[0]), "{13}");
            Test("{%f}", __makeref(f[0]), "{14}");
            Test("{%lf}", __makeref(dbl[0]), "{15}");
            Test("{%b}", __makeref(b[0]), "{true}");
            Test("{%t}", __makeref(t[1]), "{" + t[1].ToString() + "}");
            Test("{%p}", __makeref(pid[0]), "{Win32NT}");
            Test("{%e}", __makeref(mood[0]), "{good}");
        }

        [Fact]
        public static int TestEntryPoint()
        {
            TestLocals();
            new TestClass().TestFields();
            TestArrayElem();
            return 100;
        }
    }
}
