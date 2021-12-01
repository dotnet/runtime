// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// The program tests different cases that could cause issues with aggresive 
// struct optimizations with existing retyping or missing field sequences.

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Numerics;


namespace TestStructFields
{
    class Program
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void blockPromotion<T>(ref T s)
        {

        }

        #region S4 tests

        struct S4
        {
            public int i;
        }

        struct S4W
        {
            public S4 s4;
        }

        struct S4WW
        {
            public S4W s4W;
        }

        struct S4Copy
        {
            public int i;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct S4Corrupted1
        {
            [FieldOffset(0)] public int i;
            [FieldOffset(0)] public bool b0;
            [FieldOffset(1)] public bool b1;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct S4Corrupted2
        {
            [FieldOffset(0)] public int i;
            [FieldOffset(0)] public bool b0;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct S4Corrupted3
        {
            [FieldOffset(0)] public byte b0;
            [FieldOffset(3)] public byte b1;
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        static int TestS4_Simple()
        {
            S4 s1 = new S4();
            S4 s2 = new S4();
            s2.i = 1;
            if (s1.i != 0)
            {
                return 101;
            }
            blockPromotion(ref s2);
            s1 = s2;
            s2.i = 2;
            if (s1.i != 1)
            {
                return 101;
            }
            if (s2.i != 2)
            {
                return 101;
            }
            return 100;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int TestS4_W1()
        {
            S4 s1 = new S4();
            S4W s2 = new S4W();
            s2.s4.i = 1;
            if (s1.i != 0)
            {
                return 101;
            }
            s1 = s2.s4;
            s2.s4.i = 2;
            if (s1.i != 1)
            {
                return 101;
            }
            if (s2.s4.i != 2)
            {
                return 101;
            }
            return 100;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int TestS4_W2()
        {
            S4W s1 = new S4W();
            S4 s2 = new S4();
            s2.i = 1;
            if (s1.s4.i != 0)
            {
                return 101;
            }
            s1.s4 = s2;
            s2.i = 2;
            if (s1.s4.i != 1)
            {
                return 101;
            }
            if (s2.i != 2)
            {
                return 101;
            }
            return 100;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int TestS4_WW1()
        {
            S4 s1 = new S4();
            S4WW s2 = new S4WW();
            s2.s4W.s4.i = 1;
            if (s1.i != 0)
            {
                return 101;
            }
            s1 = s2.s4W.s4;
            s2.s4W.s4.i = 2;
            if (s1.i != 1)
            {
                return 101;
            }
            if (s2.s4W.s4.i != 2)
            {
                return 101;
            }
            return 100;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int TestS4_WW2()
        {
            S4WW s1 = new S4WW();
            S4 s2 = new S4();
            s2.i = 1;
            if (s1.s4W.s4.i != 0)
            {
                return 101;
            }
            s1.s4W.s4 = s2;
            s2.i = 2;
            if (s1.s4W.s4.i != 1)
            {
                return 101;
            }
            if (s2.i != 2)
            {
                return 101;
            }
            return 100;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int TestS4_Copy1()
        {
            S4 s1 = new S4();
            S4Copy s2 = new S4Copy();
            s2.i = 1;
            if (s1.i != 0)
            {
                return 101;
            }
            s1 = Unsafe.As<S4Copy, S4>(ref s2);
            s2.i = 2;
            if (s1.i != 1)
            {
                return 101;
            }
            if (s2.i != 2)
            {
                return 101;
            }
            return 100;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int TestS4_Copy2()
        {
            S4Copy s1 = new S4Copy();
            S4 s2 = new S4();
            s2.i = 1;
            if (s1.i != 0)
            {
                return 101;
            }
            s1 = Unsafe.As<S4, S4Copy>(ref s2);
            s2.i = 2;
            if (s1.i == 0)
            {
                return 101;
            }
            if (s2.i != 2)
            {
                return 101;
            }
            return 100;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int TestS4_Corrupted1()
        {
            S4 s1 = new S4();
            S4Corrupted1 s2 = new S4Corrupted1();
            s2.i = 1;
            if (s1.i != 0)
            {
                return 101;
            }
            s1 = Unsafe.As<S4Corrupted1, S4>(ref s2);
            s2.i = 2;
            if (s1.i != 1)
            {
                return 101;
            }
            if (s2.i != 2)
            {
                return 101;
            }

            s2.b0 = false;
            s1 = Unsafe.As<S4Corrupted1, S4>(ref s2);
            if (s1.i != 0)
            {
                return 101;
            }
            if (s2.i != 0)
            {
                return 101;
            }

            return 100;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int TestS4_Corrupted2()
        {
            S4 s1 = new S4();
            S4Corrupted2 s2 = new S4Corrupted2();
            s2.i = 1;
            if (s1.i != 0)
            {
                return 101;
            }
            s1 = Unsafe.As<S4Corrupted2, S4>(ref s2);
            s2.i = 2;
            if (s1.i != 1)
            {
                return 101;
            }
            if (s2.i != 2)
            {
                return 101;
            }

            s2.b0 = false;
            s1 = Unsafe.As<S4Corrupted2, S4>(ref s2);
            if (s1.i != 0)
            {
                return 101;
            }
            if (s2.i != 0)
            {
                return 101;
            }

            return 100;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int TestS4_Corrupted3()
        {
            S4 s1 = new S4();
            S4Corrupted3 s2 = new S4Corrupted3();
            s2.b0 = 1;
            if (s1.i != 0)
            {
                return 101;
            }
            s1 = Unsafe.As<S4Corrupted3, S4>(ref s2);
            s2.b0 = 2;
            if (s1.i != 1)
            {
                return 101;
            }
            if (s2.b0 != 2)
            {
                return 101;
            }

            s2.b1 = 1;
            s1 = Unsafe.As<S4Corrupted3, S4>(ref s2);
            if (s1.i != 16777218)
            {
                return 101;
            }
            if (s2.b0 != 2)
            {
                return 101;
            }

            return 100;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int TestS4_Corrupted4()
        {
            S4 s1 = new S4();
            s1.i = 0x1010101;
            S4Corrupted3 s2 = new S4Corrupted3();
            s2 = Unsafe.As<S4, S4Corrupted3>(ref s1);
            S4Corrupted3 s3 = s2;
            s2.b0 = 0;
            s3.b1 = 1;

            s1 = Unsafe.As<S4Corrupted3, S4>(ref s3);
            if (s1.i != 0x01010101)
            {
                return 101;
            }
            if (s2.b0 != 0)
            {
                return 101;
            }

            return 100;
        }

        static int TestS4()
        {
            int res = 100;
            bool failed = false;
            res = TestS4_Simple();
            if (res != 100)
            {
                Console.WriteLine("TestS4_Simple failed");
                failed = true;
            }

            res = TestS4_W1();
            if (res != 100)
            {
                Console.WriteLine("TestS4_W1 failed");
                failed = true;
            }

            res = TestS4_W2();
            if (res != 100)
            {
                Console.WriteLine("TestS4_W2 failed");
                failed = true;
            }

            res = TestS4_WW1();
            if (res != 100)
            {
                Console.WriteLine("TestS4_WW1 failed");
                failed = true;
            }

            res = TestS4_WW2();
            if (res != 100)
            {
                Console.WriteLine("TestS4_WW2 failed");
                failed = true;
            }

            res = TestS4_Copy1();
            if (res != 100)
            {
                Console.WriteLine("TestS4_Copy1 failed");
                failed = true;
            }

            res = TestS4_Copy2();
            if (res != 100)
            {
                Console.WriteLine("TestS4_Copy2 failed");
                failed = true;
            }

            res = TestS4_Corrupted1();
            if (res != 100)
            {
                Console.WriteLine("TestS4_Corrupted1 failed");
                failed = true;
            }

            res = TestS4_Corrupted2();
            if (res != 100)
            {
                Console.WriteLine("TestS4_Corrupted2 failed");
                failed = true;
            }

            res = TestS4_Corrupted3();
            if (res != 100)
            {
                Console.WriteLine("TestS4_Corrupted3 failed");
                failed = true;
            }

            res = TestS4_Corrupted4();
            if (res != 100)
            {
                Console.WriteLine("TestS4_Corrupted4 failed");
                failed = true;
            }

            if (failed)
            {
                return 101;
            }
            return 100;
        }

        #endregion  // S4 tests

        #region S8 tests
        struct S8
        {
            public int i1;
            public int i2;
        }

        struct S8W
        {
            public S8 s8;
        }

        struct S8WW
        {
            public S8W s8W;
        }

        struct S8Copy
        {
            public int i1;
            public int i2;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct S8Corrupted1
        {
            [FieldOffset(0)] public int i1;
            [FieldOffset(4)] public int i2;
            [FieldOffset(7)] public bool b0;
            [FieldOffset(5)] public bool b1;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct S8Corrupted2
        {
            [FieldOffset(0)] public int i1;
            [FieldOffset(7)] public byte b1;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct S8Corrupted3
        {
            [FieldOffset(0)] public object o1;
            [FieldOffset(0)] public long i1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int TestS8_Simple()
        {
            S8 s1 = new S8();
            S8 s2 = new S8();
            s2.i1 = 1;
            s2.i2 = 2;
            if (s1.i1 != 0)
            {
                return 101;
            }
            blockPromotion(ref s2);
            s1 = s2;
            s2.i1 = 3;
            s2.i2 = 4;

            if (s1.i1 != 1)
            {
                return 101;
            }
            if (s1.i2 != 2)
            {
                return 101;
            }
            if (s2.i1 != 3)
            {
                return 101;
            }
            if (s2.i2 != 4)
            {
                return 101;
            }
            return 100;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int TestS8_W1()
        {
            S8 s1 = new S8();
            S8W s2 = new S8W();
            s2.s8.i1 = 1;
            s2.s8.i2 = 2;
            if (s1.i1 != 0)
            {
                return 101;
            }
            s1 = s2.s8;
            s2.s8.i1 = 3;
            s2.s8.i2 = 4;

            if (s1.i1 != 1)
            {
                return 101;
            }
            if (s1.i2 != 2)
            {
                return 101;
            }
            if (s2.s8.i1 != 3)
            {
                return 101;
            }
            if (s2.s8.i2 != 4)
            {
                return 101;
            }
            return 100;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int TestS8_W2()
        {
            S8W s1 = new S8W();
            S8 s2 = new S8();
            s2.i1 = 1;
            s2.i2 = 2;
            if (s1.s8.i1 != 0)
            {
                return 101;
            }
            if (s1.s8.i2 != 0)
            {
                return 101;
            }
            s1.s8 = s2;
            s2.i1 = 3;
            s2.i2 = 4;
            if (s1.s8.i1 != 1)
            {
                return 101;
            }
            if (s1.s8.i2 != 2)
            {
                return 101;
            }
            if (s2.i1 != 3)
            {
                return 101;
            }
            if (s2.i2 != 4)
            {
                return 101;
            }
            return 100;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int TestS8_WW1()
        {
            S8 s1 = new S8();
            S8WW s2 = new S8WW();
            s2.s8W.s8.i1 = 1;
            s2.s8W.s8.i2 = 2;
            if (s1.i1 != 0)
            {
                return 101;
            }
            if (s1.i2 != 0)
            {
                return 101;
            }
            s1 = s2.s8W.s8;
            s2.s8W.s8.i1 = 3;
            s2.s8W.s8.i2 = 4;
            if (s1.i1 != 1)
            {
                return 101;
            }
            if (s1.i2 != 2)
            {
                return 101;
            }
            if (s2.s8W.s8.i1 != 3)
            {
                return 101;
            }
            if (s2.s8W.s8.i2 != 4)
            {
                return 101;
            }
            return 100;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int TestS8_WW2()
        {
            S8WW s1 = new S8WW();
            S8 s2 = new S8();
            s2.i1 = 1;
            s2.i2 = 2;
            if (s1.s8W.s8.i1 != 0)
            {
                return 101;
            }
            if (s1.s8W.s8.i2 != 0)
            {
                return 101;
            }
            s1.s8W.s8 = s2;
            s2.i1 = 3;
            s2.i2 = 4;
            if (s1.s8W.s8.i1 != 1)
            {
                return 101;
            }
            if (s1.s8W.s8.i2 != 2)
            {
                return 101;
            }
            if (s2.i1 != 3)
            {
                return 101;
            }
            if (s2.i2 != 4)
            {
                return 101;
            }
            return 100;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int TestS8_Copy1()
        {
            S8 s1 = new S8();
            S8Copy s2 = new S8Copy();
            s2.i1 = 1;
            s2.i2 = 2;
            if (s1.i1 != 0)
            {
                return 101;
            }
            if (s1.i2 != 0)
            {
                return 101;
            }
            s1 = Unsafe.As<S8Copy, S8>(ref s2);
            s2.i1 = 3;
            s2.i2 = 4;
            if (s1.i1 != 1)
            {
                return 101;
            }
            if (s1.i2 != 2)
            {
                return 101;
            }

            if (s2.i1 != 3)
            {
                return 101;
            }
            if (s2.i2 != 4)
            {
                return 101;
            }
            return 100;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int TestS8_Copy2()
        {
            S8Copy s1 = new S8Copy();
            S8 s2 = new S8();
            s2.i1 = 132;
            s2.i2 = 567;
            if (s1.i1 != 0)
            {
                return 101;
            }
            if (s1.i2 != 0)
            {
                return 101;
            }
            s1 = Unsafe.As<S8, S8Copy>(ref s2);
            s2.i1 = 32;
            s2.i2 = 33;
            if (s1.i1 != 132)
            {
                return 101;
            }
            if (s1.i2 != 567)
            {
                return 101;
            }
            if (s2.i1 == 132)
            {
                return 101;
            }
            if (s2.i2 != 33)
            {
                return 101;
            }
            return 100;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int TestS8_Corrupted1()
        {
            S8 s1 = new S8();
            S8Corrupted1 s2 = new S8Corrupted1();
            s2.i1 = 1;
            s2.i2 = 2;
            if (s1.i1 != 0)
            {
                return 101;
            }
            if (s1.i2 != 0)
            {
                return 101;
            }
            s1 = Unsafe.As<S8Corrupted1, S8>(ref s2);
            s2.i1 = 3;
            s2.i2 = 4;
            if (s1.i1 != 1)
            {
                return 101;
            }
            if (s1.i2 != 2)
            {
                return 101;
            }
            if (s2.i1 != 3)
            {
                return 101;
            }
            if (s2.i2 != 4)
            {
                return 101;
            }

            s2.b0 = true;
            s1 = Unsafe.As<S8Corrupted1, S8>(ref s2);
            if (s1.i1 != 3)
            {
                return 101;
            }
            if (s1.i2 != 0x01000004)
            {
                return 101;
            }
            s2.b1 = true;
            if (s2.i1 != 3)
            {
                return 101;
            }
            if (s2.i2 != 0x01000104)
            {
                return 101;
            }

            return 100;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int TestS8_Corrupted2()
        {
            S8 s1 = new S8();
            S8Corrupted2 s2 = new S8Corrupted2();
            s2.i1 = 1;
            s2.b1 = 2;
            if (s1.i1 != 0)
            {
                return 101;
            }
            if (s1.i2 != 0)
            {
                return 101;
            }
            s1 = Unsafe.As<S8Corrupted2, S8>(ref s2);
            s2.i1 = 3;
            s2.b1 = 4;
            if (s1.i1 != 1)
            {
                return 101;
            }
            if (s1.i2 != 0x02000000)
            {
                return 101;
            }
            if (s2.i1 != 3)
            {
                return 101;
            }
            if (s2.b1 != 4)
            {
                return 101;
            }

            s2.b1 = 5;
            s1 = Unsafe.As<S8Corrupted2, S8>(ref s2);
            if (s1.i1 != 3)
            {
                return 101;
            }
            if (s1.i2 != 0x05000000)
            {
                return 101;
            }
            if (s2.i1 != 3)
            {
                return 101;
            }
            if (s2.b1 != 5)
            {
                return 101;
            }

            return 100;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int TestS8_Corrupted3()
        {
            S8 s1 = new S8();
            S8Corrupted3 s2 = new S8Corrupted3();
            s2.o1 = new string("Hello world!");
            s1 = Unsafe.As<S8Corrupted3, S8>(ref s2);
            S8Corrupted3 s3 = Unsafe.As<S8, S8Corrupted3>(ref s1);
            s2.i1 = 0;
            GC.Collect();
            s3.i1 = 0;
            GC.Collect();

            return 100;
        }

        static int TestS8()
        {
            int res = 100;
            bool failed = false;
            res = TestS8_Simple();
            if (res != 100)
            {
                Console.WriteLine("TestS8_Simple failed");
                failed = true;
            }

            res = TestS8_W1();
            if (res != 100)
            {
                Console.WriteLine("TestS8_W1 failed");
                failed = true;
            }

            res = TestS8_W2();
            if (res != 100)
            {
                Console.WriteLine("TestS8_W2 failed");
                failed = true;
            }

            res = TestS8_WW1();
            if (res != 100)
            {
                Console.WriteLine("TestS8_WW1 failed");
                failed = true;
            }

            res = TestS8_WW2();
            if (res != 100)
            {
                Console.WriteLine("TestS8_WW2 failed");
                failed = true;
            }

            res = TestS8_Copy1();
            if (res != 100)
            {
                Console.WriteLine("TestS8_Copy1 failed");
                failed = true;
            }

            res = TestS8_Copy2();
            if (res != 100)
            {
                Console.WriteLine("TestS8_Copy2 failed");
                failed = true;
            }

            res = TestS8_Corrupted1();
            if (res != 100)
            {
                Console.WriteLine("TestS8_Corrupted1 failed");
                failed = true;
            }

            res = TestS8_Corrupted2();
            if (res != 100)
            {
                Console.WriteLine("TestS8_Corrupted2 failed");
                failed = true;
            }

            try
            {
                res = TestS8_Corrupted3();
                if (res != 100)
                {
                    Console.WriteLine("TestS8_Corrupted3 failed");
                    failed = true;
                }
                failed = true;
            }
            catch
            {

            }

            if (failed)
            {
                return 101;
            }
            return 100;
        }

        #endregion // S8 tests



        #region S16 tests
        struct S16
        {
            public int i1;
            public int i2;
            public int i3;
            public int i4;
        }

        struct S16W
        {
            public S16 s16;
        }

        struct S16WW
        {
            public S16W s16W;
        }

        struct S16Copy
        {
            public int i1;
            public int i2;
            public int i3;
            public int i4;
        }


        struct S16WithS4
        {
            public S4 s1;
            public S4 s2;
            public S8 s3;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int TestS16_Simple()
        {
            S16 s1 = new S16();
            S16 s2 = new S16();
            s2.i1 = 1;
            s2.i2 = 2;
            s2.i3 = 3;
            s2.i4 = 4;
            if (s1.i1 != 0)
            {
                return 101;
            }
            if (s1.i2 != 0)
            {
                return 101;
            }
            if (s1.i3 != 0)
            {
                return 101;
            }
            if (s1.i4 != 0)
            {
                return 101;
            }
            blockPromotion(ref s2);
            s1 = s2;
            s2.i1 = 5;
            s2.i2 = 6;
            s2.i3 = 7;
            s2.i4 = 8;

            if (s1.i1 != 1)
            {
                return 101;
            }
            if (s1.i2 != 2)
            {
                return 101;
            }
            if (s1.i3 != 3)
            {
                return 101;
            }
            if (s1.i4 != 4)
            {
                return 101;
            }

            if (s2.i1 != 5)
            {
                return 101;
            }
            if (s2.i2 != 6)
            {
                return 101;
            }
            if (s2.i3 != 7)
            {
                return 101;
            }
            if (s2.i4 != 8)
            {
                return 101;
            }
            return 100;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int TestS16_W1()
        {
            S16 s1 = new S16();
            S16W s2 = new S16W();
            s2.s16.i1 = 1;
            s2.s16.i2 = 2;
            s2.s16.i3 = 3;
            s2.s16.i4 = 4;
            if (s1.i1 != 0)
            {
                return 101;
            }
            if (s1.i2 != 0)
            {
                return 101;
            }
            if (s1.i3 != 0)
            {
                return 101;
            }
            if (s1.i4 != 0)
            {
                return 101;
            }
            s1 = s2.s16;
            s2.s16.i1 = 5;
            s2.s16.i2 = 6;
            s2.s16.i3 = 7;
            s2.s16.i4 = 8;

            if (s1.i1 != 1)
            {
                return 101;
            }
            if (s1.i2 != 2)
            {
                return 101;
            }
            if (s1.i3 != 3)
            {
                return 101;
            }
            if (s1.i4 != 4)
            {
                return 101;
            }

            if (s2.s16.i1 != 5)
            {
                return 101;
            }
            if (s2.s16.i2 != 6)
            {
                return 101;
            }
            if (s2.s16.i3 != 7)
            {
                return 101;
            }
            if (s2.s16.i4 != 8)
            {
                return 101;
            }
            return 100;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int TestS16_W2()
        {
            S16W s1 = new S16W();
            S16 s2 = new S16();
            s2.i1 = 1;
            s2.i2 = 2;
            s2.i3 = 3;
            s2.i4 = 4;
            if (s1.s16.i1 != 0)
            {
                return 101;
            }
            if (s1.s16.i2 != 0)
            {
                return 101;
            }
            if (s1.s16.i3 != 0)
            {
                return 101;
            }
            if (s1.s16.i4 != 0)
            {
                return 101;
            }
            s1.s16 = s2;
            s2.i1 = 5;
            s2.i2 = 6;
            s2.i3 = 7;
            s2.i4 = 8;

            if (s1.s16.i1 != 1)
            {
                return 101;
            }
            if (s1.s16.i2 != 2)
            {
                return 101;
            }
            if (s1.s16.i3 != 3)
            {
                return 101;
            }
            if (s1.s16.i4 != 4)
            {
                return 101;
            }

            if (s2.i1 != 5)
            {
                return 101;
            }
            if (s2.i2 != 6)
            {
                return 101;
            }
            if (s2.i3 != 7)
            {
                return 101;
            }
            if (s2.i4 != 8)
            {
                return 101;
            }
            return 100;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int TestS16_WW1()
        {
            S16 s1 = new S16();
            S16WW s2 = new S16WW();
            s2.s16W.s16.i1 = 1;
            s2.s16W.s16.i2 = 2;
            s2.s16W.s16.i3 = 3;
            s2.s16W.s16.i4 = 4;
            if (s1.i1 != 0)
            {
                return 101;
            }
            if (s1.i2 != 0)
            {
                return 101;
            }
            if (s1.i3 != 0)
            {
                return 101;
            }
            if (s1.i4 != 0)
            {
                return 101;
            }
            s1 = s2.s16W.s16;
            s2.s16W.s16.i1 = 5;
            s2.s16W.s16.i2 = 6;
            s2.s16W.s16.i3 = 7;
            s2.s16W.s16.i4 = 8;

            if (s1.i1 != 1)
            {
                return 101;
            }
            if (s1.i2 != 2)
            {
                return 101;
            }
            if (s1.i3 != 3)
            {
                return 101;
            }
            if (s1.i4 != 4)
            {
                return 101;
            }

            if (s2.s16W.s16.i1 != 5)
            {
                return 101;
            }
            if (s2.s16W.s16.i2 != 6)
            {
                return 101;
            }
            if (s2.s16W.s16.i3 != 7)
            {
                return 101;
            }
            if (s2.s16W.s16.i4 != 8)
            {
                return 101;
            }
            return 100;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int TestS16_WW2()
        {
            S16WW s1 = new S16WW();
            S16 s2 = new S16();
            s2.i1 = 1;
            s2.i2 = 2;
            s2.i3 = 3;
            s2.i4 = 4;
            if (s1.s16W.s16.i1 != 0)
            {
                return 101;
            }
            if (s1.s16W.s16.i2 != 0)
            {
                return 101;
            }
            if (s1.s16W.s16.i3 != 0)
            {
                return 101;
            }
            if (s1.s16W.s16.i4 != 0)
            {
                return 101;
            }
            s1.s16W.s16 = s2;
            s2.i1 = 5;
            s2.i2 = 6;
            s2.i3 = 7;
            s2.i4 = 8;

            if (s1.s16W.s16.i1 != 1)
            {
                return 101;
            }
            if (s1.s16W.s16.i2 != 2)
            {
                return 101;
            }
            if (s1.s16W.s16.i3 != 3)
            {
                return 101;
            }
            if (s1.s16W.s16.i4 != 4)
            {
                return 101;
            }

            if (s2.i1 != 5)
            {
                return 101;
            }
            if (s2.i2 != 6)
            {
                return 101;
            }
            if (s2.i3 != 7)
            {
                return 101;
            }
            if (s2.i4 != 8)
            {
                return 101;
            }
            return 100;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int TestS16_Copy1()
        {
            S16 s1 = new S16();
            S16Copy s2 = new S16Copy();
            s2.i1 = 1;
            s2.i2 = 2;
            s2.i3 = 3;
            s2.i4 = 4;
            if (s1.i1 != 0)
            {
                return 101;
            }
            if (s1.i2 != 0)
            {
                return 101;
            }
            if (s1.i3 != 0)
            {
                return 101;
            }
            if (s1.i4 != 0)
            {
                return 101;
            }
            s1 = Unsafe.As<S16Copy, S16>(ref s2);
            s2.i1 = 5;
            s2.i2 = 6;
            s2.i3 = 7;
            s2.i4 = 8;

            if (s1.i1 != 1)
            {
                return 101;
            }
            if (s1.i2 != 2)
            {
                return 101;
            }
            if (s1.i3 != 3)
            {
                return 101;
            }
            if (s1.i4 != 4)
            {
                return 101;
            }

            if (s2.i1 != 5)
            {
                return 101;
            }
            if (s2.i2 != 6)
            {
                return 101;
            }
            if (s2.i3 != 7)
            {
                return 101;
            }
            if (s2.i4 != 8)
            {
                return 101;
            }
            return 100;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int TestS16_Copy2()
        {
            S16Copy s1 = new S16Copy();
            S16 s2 = new S16();
            s2.i1 = 1;
            s2.i2 = 2;
            s2.i3 = 3;
            s2.i4 = 4;
            if (s1.i1 != 0)
            {
                return 101;
            }
            if (s1.i2 != 0)
            {
                return 101;
            }
            if (s1.i3 != 0)
            {
                return 101;
            }
            if (s1.i4 != 0)
            {
                return 101;
            }
            s1 = Unsafe.As<S16, S16Copy>(ref s2);
            s2.i1 = 5;
            s2.i2 = 6;
            s2.i3 = 7;
            s2.i4 = 8;

            if (s1.i1 != 1)
            {
                return 101;
            }
            if (s1.i2 != 2)
            {
                return 101;
            }
            if (s1.i3 != 3)
            {
                return 101;
            }
            if (s1.i4 != 4)
            {
                return 101;
            }

            if (s2.i1 != 5)
            {
                return 101;
            }
            if (s2.i2 != 6)
            {
                return 101;
            }
            if (s2.i3 != 7)
            {
                return 101;
            }
            if (s2.i4 != 8)
            {
                return 101;
            }
            return 100;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int TestS16_RetypedFields1()
        {
            S16WithS4 s16 = new S16WithS4();
            S4 s4 = new S4();
            s4.i = 1;
            if (s4.i != 1)
            {
                return 101;
            }
            if (s16.s1.i != 0)
            {
                return 101;
            }

            s16.s1 = s4;
            s4.i = 2;
            s16.s2 = s4;
            s4.i = 3;
            if (s16.s1.i != 1)
            {
                return 101;
            }
            if (s16.s2.i != 2)
            {
                return 101;
            }
            if (s4.i != 3)
            {
                return 101;
            }
            if (s4.i + s16.s1.i + s16.s2.i != 6)
            {
                return 101;
            }

            return 100;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int TestS16_RetypedFields2()
        {
            S16WithS4 s16 = new S16WithS4();
            S4 s4 = new S4();
            s4.i = 1;
            if (s4.i != 1)
            {
                return 101;
            }
            if (s16.s1.i != 0)
            {
                return 101;
            }

            s16.s1 = Unsafe.As<int, S4>(ref s4.i);
            s4.i = 2;
            s16.s2 = Unsafe.As<int, S4>(ref s4.i);
            s4.i = 3;
            if (s16.s1.i != 1)
            {
                return 101;
            }
            if (s16.s2.i != 2)
            {
                return 101;
            }
            if (s4.i != 3)
            {
                return 101;
            }
            if (s4.i + s16.s1.i + s16.s2.i != 6)
            {
                return 101;
            }

            return 100;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int TestS16_RetypedFields3()
        {
            S16WithS4 s16 = new S16WithS4();
            S4 s4 = new S4();
            s4.i = 1;
            if (s4.i != 1)
            {
                return 101;
            }
            if (s16.s1.i != 0)
            {
                return 101;
            }

            s16.s1.i = Unsafe.As<S4, int>(ref s4);
            s4.i = 2;
            s16.s2.i = Unsafe.As<S4, int>(ref s4);
            s4.i = 3;
            if (s16.s1.i != 1)
            {
                return 101;
            }
            if (s16.s2.i != 2)
            {
                return 101;
            }
            if (s4.i != 3)
            {
                return 101;
            }
            if (s4.i + s16.s1.i + s16.s2.i != 6)
            {
                return 101;
            }

            return 100;
        }

        static int TestS16()
        {
            int res = 100;
            bool failed = false;

            res = TestS16_Simple();
            if (res != 100)
            {
                Console.WriteLine("TestS16_Simple failed");
                failed = true;
            }

            res = TestS16_W1();
            if (res != 100)
            {
                Console.WriteLine("TestS16_W1 failed");
                failed = true;
            }

            res = TestS16_W2();
            if (res != 100)
            {
                Console.WriteLine("TestS16_W2 failed");
                failed = true;
            }

            res = TestS16_WW1();
            if (res != 100)
            {
                Console.WriteLine("TestS16_WW1 failed");
                failed = true;
            }

            res = TestS16_WW2();
            if (res != 100)
            {
                Console.WriteLine("TestS16_WW2 failed");
                failed = true;
            }

            res = TestS16_Copy1();
            if (res != 100)
            {
                Console.WriteLine("TestS16_Copy1 failed");
                failed = true;
            }

            res = TestS16_Copy2();
            if (res != 100)
            {
                Console.WriteLine("TestS16_Copy2 failed");
                failed = true;
            }

            res = TestS16_RetypedFields1();
            if (res != 100)
            {
                Console.WriteLine("TestS16_RetypedFields1 failed");
                failed = true;
            }

            res = TestS16_RetypedFields2();
            if (res != 100)
            {
                Console.WriteLine("TestS16_RetypedFields2 failed");
                failed = true;
            }

            res = TestS16_RetypedFields3();
            if (res != 100)
            {
                Console.WriteLine("TestS16_RetypedFields3 failed");
                failed = true;
            }

            if (failed)
            {
                return 101;
            }
            return 100;
        }

        #endregion // S16 tests

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int Test()
        {
            int res = 100;
            bool failed = false;

            res = TestS4();
            if (res != 100)
            {
                Console.WriteLine("TestS4 failed");
                failed = true;
            }

            res = TestS8();
            if (res != 100)
            {
                Console.WriteLine("TestS8 failed");
                failed = true;
            }

            res = TestS16();
            if (res != 100)
            {
                Console.WriteLine("TestS16 failed");
                failed = true;
            }

            if (failed)
            {
                return 101;
            }
            return 100;
        }

        static int Main(string[] args)
        {
            return Test();
        }
    }
}
