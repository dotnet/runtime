// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Runtime.CompilerServices;

namespace structinreg
{
    struct Test1
    {
        public static int int0;
        public int i1;
        public int i2;
        public int i3;
        public int i4;
        public int i5;
        public int i6;
        public int i7;
        public int i8;
    }

    struct Test2
    {
        public int i1;
        public double d1;
    }

    struct Test5
    {
        public float f1;
        public Test2 t2;
        public long l1;
    }
    struct Test9
    {
        public float f3;
    }

    struct Test10
    {
        public bool b1;
        public Foo2 obj;
    }

    struct Test11
    {
        public string s1;
        public Int32 int32;
    }

    struct Test6
    {
        public float f2;
        public Test9 t9;
        public int i3;
    }

    struct Test7
    {
        static int staticInt;
        public Test6 t6;
        public int i2;
    }

    struct Test3
    {
        public Foo2 o1;
        public Foo2 o2;
        public Foo2 o3;
        public Foo2 o4;
    }
    struct Test4
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

    class Foo2
    {
        public int iFoo;
    }
    struct Test12
    {
        public Foo2 foo;
        public int i;
    }

    struct Test13
    {
        public Foo2 foo1;
    }

    struct Test14
    {
        public Test13 t13;
    }

    struct Test15
    {
        public byte b0;
        public byte b1;
        public byte b2;
        public byte b3;
        public byte b4;
        public byte b5;
        public byte b6;
        public byte b7;
        public byte b8;
        public byte b9;
        public byte b10;
        public byte b11;
        public byte b12;
        public byte b13;
        public byte b14;
        public byte b15;
    }

    class Program1
    {
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        static int test1(Test1 t1)
        {
            Console.WriteLine("test1: {0}", t1.i1 + t1.i2 + t1.i3 + t1.i4 + t1.i5 + t1.i6 + t1.i7 + t1.i8);
            return t1.i1 + t1.i2 + t1.i3 + t1.i4 + t1.i5 + t1.i6 + t1.i7 + t1.i8;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        static double test2(Test2 t2)
        {
            Console.WriteLine("test2: {0}", t2.i1 + t2.d1);
            return t2.i1 + t2.d1;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        static int test3(Test3 t3)
        {
            Console.WriteLine("test3: {0} {1} {2} {3}", t3.o1, t3.o2, t3.o3, t3.o4, t3.o1.iFoo + t3.o2.iFoo + t3.o3.iFoo + t3.o4.iFoo);

            return t3.o1.iFoo + t3.o2.iFoo + t3.o3.iFoo + t3.o4.iFoo;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        static int test4(Test4 t4)
        {
            Console.WriteLine("test4 Res: {0}", t4.i1 + t4.i2 + t4.i3 + t4.i4 + t4.i5 + t4.i6 + t4.i7 +
                t4.i8 + t4.i9 + t4.i10 + t4.i11 + t4.i12 + t4.i13 + t4.i14 +
                t4.i15 + t4.i16 + t4.i17 + t4.i18 + t4.i19 + t4.i20 + t4.i21 + t4.i22 + t4.i23 + t4.i24);
            return t4.i1 + t4.i2 + t4.i3 + t4.i4 + t4.i5 + t4.i6 + t4.i7 +
                t4.i8 + t4.i9 + t4.i10 + t4.i11 + t4.i12 + t4.i13 + t4.i14 +
                t4.i15 + t4.i16 + t4.i17 + t4.i18 + t4.i19 + t4.i20 + t4.i21 + t4.i22 + t4.i23 + t4.i24;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        static double test5(Test5 t5)
        {
            Console.WriteLine("test5 Res: {0}", t5.f1 + t5.t2.i1 + t5.t2.d1 + t5.l1);
            return t5.f1 + t5.t2.i1 + t5.t2.d1 + t5.l1;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        static float test7(Test7 t7)
        {
            Console.WriteLine("t7 Res: {0}", t7.i2 + t7.t6.f2 + t7.t6.i3 + t7.t6.t9.f3);
            return t7.i2 + t7.t6.f2 + t7.t6.i3 + t7.t6.t9.f3;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        static int test10(Test10 t10)
        {
            Console.WriteLine("t10 Res: {0}, {1}", t10.b1, t10.obj.iFoo);
            int res = t10.b1 ? 8 : 9;
            res += t10.obj.iFoo;

            return res;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        static int test11(Test11 t11)
        {
            Console.WriteLine("t11 Res: {0}, {1}", t11.s1, t11.int32);
            return int.Parse(t11.s1) + t11.int32;
        }

        static int test12(Test12 t12)
        {
            Console.WriteLine("t12Res: {0}", t12.foo.iFoo + t12.i);
            return t12.foo.iFoo + t12.i;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        static int test13(Test13 t13)
        {
            Console.WriteLine("t13Res: {0}", t13.foo1.iFoo);
            return t13.foo1.iFoo;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        static int test14(Test14 t14)
        {
            Console.WriteLine("t14 Res: {0}", t14.t13.foo1.iFoo);
            return t14.t13.foo1.iFoo;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        static int test15(Test15 t15)
        {
            Console.WriteLine("t15 Res: {0}", t15.b0 + t15.b1 + t15.b2 + t15.b3 +
                t15.b4 + t15.b5 + t15.b6 + t15.b7 + t15.b8 + t15.b9 + t15.b10 + 
                t15.b11 + t15.b12 + t15.b13 + t15.b14 + t15.b15);
            return (t15.b0 + t15.b1 + t15.b2 + t15.b3 +
                t15.b4 + t15.b5 + t15.b6 + t15.b7 + t15.b8 + t15.b9 + t15.b10 +
                t15.b11 + t15.b12 + t15.b13 + t15.b14 + t15.b15);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static int Main1()
        {
            Console.WriteLine("Foo2:Foo2:Foo2!!!");
            Test1 t1 = default(Test1);
            Test1.int0 = 999;
            t1.i1 = 1;
            t1.i2 = 2;
            t1.i3 = 3;
            t1.i4 = 4;
            t1.i5 = 5;
            t1.i6 = 6;
            t1.i7 = 7;
            t1.i8 = 8;

            Test2 t2 = default(Test2);
            t2.i1 = 9;
            t2.d1 = 10;

            Test3 t3 = default(Test3);
            t3.o1 = new Foo2();
            t3.o1.iFoo = 1;
            t3.o2 = new Foo2();
            t3.o2.iFoo = 2;
            t3.o3 = new Foo2();
            t3.o3.iFoo = 3;
            t3.o4 = new Foo2();
            t3.o4.iFoo = 4;

            Test4 t4 = default(Test4);
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

            Test5 t5 = default(Test5);
            t5.f1 = 1;
            t5.t2.i1 = 2;
            t5.t2.d1 = 3;
            t5.l1 = 4;

            Test7 t7 = default(Test7);
            t7.i2 = 31;
            t7.t6.f2 = 32.0F;
            t7.t6.i3 = 33;
            t7.t6.t9.f3 = 34.0F;

            Test10 t10 = default(Test10);
            t10.b1 = true;
            t10.obj = new Foo2();
            t10.obj.iFoo = 7;

            Test11 t11 = default(Test11);
            t11.s1 = "78";
            t11.int32 = 87;

            Test12 t12 = default(Test12);
            t12.foo = new Foo2();
            t12.foo.iFoo = 45;
            t12.i = 56;

            Test13 t13 = default(Test13);
            t13.foo1 = new Foo2();
            t13.foo1.iFoo = 333;

            Test14 t14 = default(Test14);
            t14.t13.foo1 = new Foo2();
            t14.t13.foo1.iFoo = 444;

            int t13Res = test13(t13);
            Console.WriteLine("test13 Result: {0}", t13Res);
            if (t13Res != 333)
            {
                throw new Exception("Failed test13 test!");
            }

            int t14Res = test14(t14);
            Console.WriteLine("test14 Result: {0}", t14Res);
            if (t14Res != 444)
            {
                throw new Exception("Failed test14 test!");
            }

            int t10Res = test10(t10);
            Console.WriteLine("test10 Result: {0}", t10Res);
            if (t10Res != 15)
            {
                throw new Exception("Failed test10 test!");
            }

            int t11Res = test11(t11);
            Console.WriteLine("test11 Result: {0}", t11Res);
            if (t11Res != 165)
            {
                throw new Exception("Failed test11 test!");
            }

            int t12Res = test12(t12);
            Console.WriteLine("test12 Result: {0}", t12Res);
            if (t12Res != 101)
            {
                throw new Exception("Failed test12 test!");
            }

            int t1Res = test1(t1);
            Console.WriteLine("test1 Result: {0}", t1Res);
            if (t1Res != 36)
            {
                throw new Exception("Failed test1 test!");
            }

            double t2Res = test2(t2);
            Console.WriteLine("test2 Result: {0}", t2Res);
            if (t2Res != 19.0D)
            {
                throw new Exception("Failed test2 test!");
            }

            int t3Res = test3(t3);
            Console.WriteLine("test3 Result: {0}", t3Res);
            if (t3Res != 10)
            {
                throw new Exception("Failed test3 test!");
            }

            int t4Res = test4(t4);
            Console.WriteLine("test4 Result: {0}", t4Res);
            if (t4Res != 300)
            {
                throw new Exception("Failed test4 test!");
            }

            double t5Res = test5(t5);
            Console.WriteLine("test5 Result: {0}", t5Res);
            if (t5Res != 10.0D)
            {
                throw new Exception("Failed test5 test!");
            }

            float t7Res = test7(t7);
            Console.WriteLine("test7 Result: {0}", t7Res);
            if (t7Res != 130.00)
            {
                throw new Exception("Failed test7 test!");
            }

            Test15 t15 = default(Test15);
            t15.b0 = 1;
            t15.b1 = 2;
            t15.b2 = 3;
            t15.b3 = 4;
            t15.b4 = 5;
            t15.b5 = 6;
            t15.b6 = 7;
            t15.b7 = 8;
            t15.b8 = 9;
            t15.b9 = 10;
            t15.b10 = 11;
            t15.b11 = 12;
            t15.b12 = 13;
            t15.b13 = 14;
            t15.b14 = 15;
            t15.b15 = 16;

            int t15Res = test15(t15);
            Console.WriteLine("test15 Result: {0}", t15Res);
            if (t15Res != 136) {
                throw new Exception("Failed test15 test!");
            }

            return 100;
        }
    }
}
