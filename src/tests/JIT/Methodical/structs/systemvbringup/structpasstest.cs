// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Runtime.CompilerServices;
using Xunit;
namespace structinreg
{
    public class Program
    {
        [Fact]
        public static int TestEntryPoint()
        {
            try
            {
                int ret = Program0.Main1();
                if (ret != 100)
                {
                    return ret;
                }

                ret = Program1.Main1();
                if (ret != 100)
                {
                    return ret;
                }

                ret = Program2.Main1();
                if (ret != 100)
                {
                    return ret;
                }

                ret = Program3.Main1();
                if (ret != 100)
                {
                    return ret;
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            return 100;
        }
    }

    struct Test20
    {
        public int i1;
        public int i2;
        public int i3;
        public int i4;
        public int i5;
        public int i6;
        public int i7;
        public int i8;
    }

    struct Test21
    {
        public int i1;
        public double d1;
    }

    struct Test22
    {
        public object o1;
        public object o2;
        public object o3;
        public object o4;
    }
    struct Test23
    {
        public int i1;
        public int i2;
        public int i3;
        public int i4;
        public int i5;
        public int i6;
        public int i7;
        public int i8;
        public int i9;
        public int i10;
        public int i11;
        public int i12;
        public int i13;
        public int i14;
        public int i15;
        public int i16;
        public int i17;
        public int i18;
        public int i19;
        public int i20;
        public int i21;
        public int i22;
        public int i23;
        public int i24;
    }

    public class Foo1
    {
        public int iFoo;
    }

    public class Program2
    {
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        static int test20(Test20 t1)
        {
            Console.WriteLine("test1Res: {0}", t1.i1 + t1.i2 + t1.i3 + t1.i4 + t1.i5 + t1.i6 + t1.i7 + t1.i8);
            return t1.i1 + t1.i2 + t1.i3 + t1.i4 + t1.i5 + t1.i6 + t1.i7 + t1.i8;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        static int test21(Test21 t2)
        {
            Console.WriteLine("test2Res: {0}", t2.i1 + t2.d1);
            return (int)(t2.i1 + t2.d1);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        static int test22(Test22 t3)
        {
            Console.WriteLine("test3Res: {0} {1} {2} {3}", t3.o1, t3.o2, t3.o3, t3.o4);
            return (int)(t3.o1.GetHashCode() + t3.o2.GetHashCode() + t3.o3.GetHashCode() + t3.o4.GetHashCode());
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        static int test23(Test23 t4)
        {
            Console.WriteLine("test4Res: {0}", t4.i1 + t4.i2, +t4.i3 + t4.i4 + t4.i5 + t4.i6 + t4.i7 + t4.i8 +
                t4.i9 + t4.i10 + t4.i11 + t4.i12 + t4.i13 + t4.i14 +
                t4.i15 + t4.i16 + t4.i17 + t4.i18 + t4.i19 + t4.i20 + t4.i21 + t4.i22 + t4.i23 + t4.i24);
            return t4.i1 + t4.i2 + t4.i3 + t4.i4 + t4.i5 + t4.i6 + t4.i7 +
                t4.i8 + t4.i9 + t4.i10 + t4.i11 + t4.i12 + t4.i13 + t4.i14 +
                t4.i15 + t4.i16 + t4.i17 + t4.i18 + t4.i19 + t4.i20 + t4.i21 + t4.i22 + t4.i23 + t4.i24;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        static int test24(int i1, int i2, int i3, int i4, int i5, int i6, int i7, Foo1 foo)
        {
            Console.WriteLine("test5Res: {0}", i1 + i2 + i3 + i4 + i5 + i6 + i7 + foo.iFoo);
            return (i1 + i2 + i3 + i4 + i5 + i6 + i7 + foo.iFoo);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static int Main1()
        {
            Test20 t1 = default(Test20);
            t1.i1 = 1;
            t1.i2 = 2;
            t1.i3 = 3;
            t1.i4 = 4;
            t1.i5 = 5;
            t1.i6 = 6;
            t1.i7 = 7;
            t1.i8 = 8;

            Test21 t2 = default(Test21);
            t2.i1 = 9;
            t2.d1 = 10;

            Test22 t3 = default(Test22);
            t3.o1 = new object();
            t3.o2 = new object();
            t3.o3 = new object();
            t3.o4 = new object();

            Test23 t4 = default(Test23);
            t4.i1 = 1;
            t4.i2 = 2;
            t4.i3 = 3;
            t4.i4 = 4;
            t4.i5 = 5;
            t4.i6 = 6;
            t4.i7 = 7;
            t4.i8 = 8;
            t4.i9 = 9;
            t4.i10 = 10;
            t4.i11 = 11;
            t4.i12 = 12;
            t4.i13 = 13;
            t4.i14 = 14;
            t4.i15 = 15;
            t4.i16 = 16;
            t4.i17 = 17;
            t4.i18 = 18;
            t4.i19 = 19;
            t4.i20 = 20;
            t4.i21 = 21;
            t4.i22 = 22;
            t4.i23 = 23;
            t4.i24 = 24;

            Foo1 foo = new Foo1();
            foo.iFoo = 8;

            int t1Res = test20(t1);
            Console.WriteLine("test1 Result: {0}", t1Res);
            if (t1Res != 36)
            {
                throw new Exception("Failed test1 test!");
            }

            int t2Res = test21(t2);
            Console.WriteLine("test2 Result: {0}", t2Res);
            if (t2Res != 19)
            {
                throw new Exception("Failed test2 test!");
            }

            int t3Res = test22(t3);
            Console.WriteLine("test3 Result: {0}", t3Res);
            if (t3Res == 0) // Adding HashCodes should be != 0.
            {
                throw new Exception("Failed test3 test!");
            }

            int t4Res = test23(t4);
            Console.WriteLine("test4 Result: {0}", t4Res);
            if (t4Res != 300)
            {
                throw new Exception("Failed test4 test!");
            }

            int t5Res = test24(1, 2, 3, 4, 5, 6, 7, foo);
            if (t5Res != 36)
            {
                throw new Exception("Failed test5 test!");
            }
            return 100;
        }
    }
}
