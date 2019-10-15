// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;  
using System.Runtime.InteropServices;  

namespace structinreg
{
 
    class Program3
    {
        class TestClass
        {
            object w;
        };

        public struct S1
        {
            public int x;
            public int y;
            public int z;
            public int w;
        }

        public struct S2
        {
            public int x;
            public int y;
            public float z;
        }

        public struct S3
        {
            public int x;
            public int y;
            public double z;
        }

        public struct S4
        {
            public int x;
            public float y;
        }

        public struct S5
        {
            public int x;
            public double y;
        }

        public struct S6
        {
            public short x;
            public short y;
            public int z;
            public int w;
        }

        public struct S7
        {
            public double x;
            public int y;
            public int z;
        }

        public struct S8
        {
            public double x;
            public int y;
        }

        public struct S9
        {
            public int x;
            public int y;
            public float z;
            public float w;
        }

        public struct S10
        {
            public byte a;
            public byte b;
            public byte c;
            public byte d;
            public byte e;
            public byte f;
            public byte g;
            public byte h;
        }
        
        public struct S11
        {
            public byte a;
            public byte b;
            public byte c;
            public byte d;
            public double e;
        }

        public struct S12
        {
            public byte a;
            public byte b;
            public byte c;
            public byte d;
            public byte e;
            public byte f;
            public byte g;
            public byte h;
            public long i;
        }

        public struct S13
        {
            public byte hasValue;
            public int x;
        }

        public struct S14
        {
            public byte x;
            public long y;
        }

        public struct S15
        {
            public byte a;
            public byte b;
            public byte c;
            public byte d;
            public byte e;
            public byte f;
            public byte g;
            public byte h;
            public byte i;
        }

        public struct S16
        {
            public byte x;
            public short y;
        }

        public struct S17
        {
            public float x;
            public float y;
        }

        public struct S18
        {
            public float x;
            public int y;
            public float z;
        }

        public struct S19
        {
            public int x;
            public float y;
            public int z;
            public float w;
        }

        // struct that doesn't fit into registers
        public struct S20
        {
            public long x;
            public long y;
            public long z;
            public long w;
        }
/* These tests are not working on non Windows CoreCLR. Enable this when GH Issue #2076 is resolved.
        [StructLayout(LayoutKind.Sequential)]
        public struct S28
        {
            public object x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct S29
        {
            public int x;
            public object y;
        }
 Enable this when GH Issue #2076 is resolved. */
        public struct S30
        {
            public long x;
            public long y;
        }

        public delegate void MyCallback1(S1 s);
        public delegate void MyCallback2(S2 s);
        public delegate void MyCallback3(S3 s);
        public delegate void MyCallback4(S4 s);
        public delegate void MyCallback5(S5 s);
        public delegate void MyCallback6(S6 s);
        public delegate void MyCallback7(S7 s);
        public delegate void MyCallback8(S8 s);
        public delegate void MyCallback9(S9 s);
        public delegate void MyCallback10(S10 s);
        public delegate void MyCallback11(S11 s);
        public delegate void MyCallback12(S12 s);
        public delegate void MyCallback13(S13 s);
        public delegate void MyCallback14(S14 s);
        public delegate void MyCallback15(S15 s);
        public delegate void MyCallback16(S16 s);
        public delegate void MyCallback17(S17 s);
        public delegate void MyCallback18(S18 s);
        public delegate void MyCallback19(S19 s);
        public delegate void MyCallback20(S20 s);
        
/* These tests are not working on non Windows CoreCLR.  Enable this when GH Issue #2076 is resolved.
        public delegate void MyCallback28(S28 s);
        public delegate void MyCallback29(S29 s);
 Enable this when GH Issue #2076 is resolved. */
        public delegate void MyCallback30(S30 s1, S30 s2, S30 s3);
        
        [DllImport("jitstructtests_lib")]
        public static extern void InvokeCallback1(MyCallback1 callback, S1 s);

        [DllImport("jitstructtests_lib")]
        public static extern void InvokeCallback2(MyCallback2 callback, S2 s);

        [DllImport("jitstructtests_lib")]
        public static extern void InvokeCallback3(MyCallback3 callback, S3 s);

        [DllImport("jitstructtests_lib")]
        public static extern void InvokeCallback4(MyCallback4 callback, S4 s);

        [DllImport("jitstructtests_lib")]
        public static extern void InvokeCallback5(MyCallback5 callback, S5 s);

        [DllImport("jitstructtests_lib")]
        public static extern void InvokeCallback6(MyCallback6 callback, S6 s);

        [DllImport("jitstructtests_lib")]
        public static extern void InvokeCallback7(MyCallback7 callback, S7 s);

        [DllImport("jitstructtests_lib")]
        public static extern void InvokeCallback8(MyCallback8 callback, S8 s);

        [DllImport("jitstructtests_lib")]
        public static extern void InvokeCallback9(MyCallback9 callback, S9 s);

        [DllImport("jitstructtests_lib")]
        public static extern void InvokeCallback10(MyCallback10 callback, S10 s);

        [DllImport("jitstructtests_lib")]
        public static extern void InvokeCallback11(MyCallback11 callback, S11 s);

        [DllImport("jitstructtests_lib")]
        public static extern void InvokeCallback12(MyCallback12 callback, S12 s);

        [DllImport("jitstructtests_lib")]
        public static extern void InvokeCallback13(MyCallback13 callback, S13 s);

        [DllImport("jitstructtests_lib")]
        public static extern void InvokeCallback14(MyCallback14 callback, S14 s);

        [DllImport("jitstructtests_lib")]
        public static extern void InvokeCallback15(MyCallback15 callback, S15 s);

        [DllImport("jitstructtests_lib")]
        public static extern void InvokeCallback16(MyCallback16 callback, S16 s);

        [DllImport("jitstructtests_lib")]
        public static extern void InvokeCallback17(MyCallback17 callback, S17 s);

        [DllImport("jitstructtests_lib")]
        public static extern void InvokeCallback18(MyCallback18 callback, S18 s);

        [DllImport("jitstructtests_lib")]
        public static extern void InvokeCallback19(MyCallback19 callback, S19 s);

        [DllImport("jitstructtests_lib")]
        public static extern void InvokeCallback20(MyCallback20 callback, S20 s);
/* These tests are not working on non Windows CoreCLR.  Enable this when GH Issue #2076 is resolved.
        
        [DllImport("jitstructtests_lib")]
        public static extern void InvokeCallback28(MyCallback28 callback, S28 s);

        [DllImport("jitstructtests_lib")]
        public static extern void InvokeCallback29(MyCallback29 callback, S29 s);
 Enable this when GH Issue #2076 is resolved. */
        [DllImport("jitstructtests_lib")]
        public static extern void InvokeCallback30(MyCallback30 callback, S30 s1, S30 s2, S30 s3);

        [DllImport("jitstructtests_lib")]
        public static extern S1 InvokeCallback1R(MyCallback1 callback, S1 s);

        [DllImport("jitstructtests_lib")]
        public static extern S2 InvokeCallback2R(MyCallback2 callback, S2 s);

        [DllImport("jitstructtests_lib")]
        public static extern S3 InvokeCallback3R(MyCallback3 callback, S3 s);

        [DllImport("jitstructtests_lib")]
        public static extern S4 InvokeCallback4R(MyCallback4 callback, S4 s);

        [DllImport("jitstructtests_lib")]
        public static extern S5 InvokeCallback5R(MyCallback5 callback, S5 s);

        [DllImport("jitstructtests_lib")]
        public static extern S6 InvokeCallback6R(MyCallback6 callback, S6 s);

        [DllImport("jitstructtests_lib")]
        public static extern S7 InvokeCallback7R(MyCallback7 callback, S7 s);

        [DllImport("jitstructtests_lib")]
        public static extern S8 InvokeCallback8R(MyCallback8 callback, S8 s);

        [DllImport("jitstructtests_lib")]
        public static extern S9 InvokeCallback9R(MyCallback9 callback, S9 s);

        [DllImport("jitstructtests_lib")]
        public static extern S10 InvokeCallback10R(MyCallback10 callback, S10 s);

        [DllImport("jitstructtests_lib")]
        public static extern S11 InvokeCallback11R(MyCallback11 callback, S11 s);

        [DllImport("jitstructtests_lib")]
        public static extern S12 InvokeCallback12R(MyCallback12 callback, S12 s);

        [DllImport("jitstructtests_lib")]
        public static extern S13 InvokeCallback13R(MyCallback13 callback, S13 s);

        [DllImport("jitstructtests_lib")]
        public static extern S14 InvokeCallback14R(MyCallback14 callback, S14 s);

        [DllImport("jitstructtests_lib")]
        public static extern S15 InvokeCallback15R(MyCallback15 callback, S15 s);

        [DllImport("jitstructtests_lib")]
        public static extern S16 InvokeCallback16R(MyCallback16 callback, S16 s);

        [DllImport("jitstructtests_lib")]
        public static extern S17 InvokeCallback17R(MyCallback17 callback, S17 s);

        [DllImport("jitstructtests_lib")]
        public static extern S18 InvokeCallback18R(MyCallback18 callback, S18 s);

        [DllImport("jitstructtests_lib")]
        public static extern S19 InvokeCallback19R(MyCallback19 callback, S19 s);

        [DllImport("jitstructtests_lib")]
        public static extern S20 InvokeCallback20R(MyCallback20 callback, S20 s);
/* These tests are not working on non Windows CoreCLR.  Enable this when GH Issue #2076 is resolved.

        [DllImport("jitstructtests_lib")]
        public static extern S28 InvokeCallback28R(MyCallback28 callback, S28 s);

        [DllImport("jitstructtests_lib")]
        public static extern S29 InvokeCallback29R(MyCallback29 callback, S29 s);
 Enable this when GH Issue #2076 is resolved. */        
        static public int Main1()
        {
            Program3 p = new Program3();
            S1 s1 = new S1();
            s1.x = 1;
            s1.y = 2;
            s1.z = 3;
            s1.w = 4;

            try
            {
                InvokeCallback1((par) =>
                {
                    Console.WriteLine("S1: {0}, {1}, {2}, {3}", par.x, par.y, par.z, par.w);
                    if (par.x != 1 || par.y != 2 || par.z != 3 || par.w != 4)
                    {
                        throw new System.Exception();
                    }
                }, s1);

                S2 s2;
                s2.x = 1;
                s2.y = 2;
                s2.z = 3;
                InvokeCallback2((par) =>
                {
                    Console.WriteLine("S2: {0}, {1}, {2}", par.x, par.y, par.z);
                    if (par.x != 1 || par.y != 2 || par.z != 3)
                    {
                        throw new System.Exception();
                    }
                }, s2);

                S3 s3;
                s3.x = 1;
                s3.y = 2;
                s3.z = 3;
                InvokeCallback3((par) =>
                {
                    Console.WriteLine("S3: {0}, {1}, {2}", par.x, par.y, par.z);
                    if (par.x != 1 || par.y != 2 || par.z != 3)
                    {
                        throw new System.Exception();
                    }
                }, s3);

                S4 s4;
                s4.x = 1;
                s4.y = 2;
                InvokeCallback4((par) =>
                {
                    Console.WriteLine("S4: {0}, {1}", par.x, par.y);
                    if (par.x != 1 || par.y != 2)
                    {
                        throw new System.Exception();
                    }
                }, s4);

                S5 s5;
                s5.x = 1;
                s5.y = 2;
                InvokeCallback5((par) =>
                {
                    Console.WriteLine("S5: {0}, {1}", par.x, par.y);
                    if (par.x != 1 || par.y != 2)
                    {
                        throw new System.Exception();
                    }
                }, s5);

                S6 s6;
                s6.x = 1;
                s6.y = 2;
                s6.z = 3;
                s6.w = 4;
                InvokeCallback6((par) =>
                {
                    Console.WriteLine("S6: {0}, {1}, {2}, {3}", par.x, par.y, par.z, par.w);
                    if (par.x != 1 || par.y != 2 || par.z != 3 || par.w != 4)
                    {
                        throw new System.Exception();
                    }
                }, s6);

                S7 s7;
                s7.x = 1;
                s7.y = 2;
                s7.z = 3;
                InvokeCallback7((par) =>
                {
                    Console.WriteLine("S7: {0}, {1}, {2}", par.x, par.y, par.z);
                    if (par.x != 1 || par.y != 2 || par.z != 3)
                    {
                        throw new System.Exception();
                    }
                }, s7);

                S8 s8;
                s8.x = 1;
                s8.y = 2;
                InvokeCallback8((par) =>
                {
                    Console.WriteLine("S8: {0}, {1}", par.x, par.y);
                    if (par.x != 1 || par.y != 2)
                    {
                        throw new System.Exception();
                    }
                }, s8);

                S9 s9;
                s9.x = 1;
                s9.y = 2;
                s9.z = 3;
                s9.w = 4;
                InvokeCallback9((par) =>
                {
                    Console.WriteLine("S9: {0}, {1}, {2}, {3}", par.x, par.y, par.z, par.w);
                    if (par.x != 1 || par.y != 2 || par.z != 3 || par.w != 4)
                    {
                        throw new System.Exception();
                    }
                }, s9);

                S10 s10;
                s10.a = 1;
                s10.b = 2;
                s10.c = 3;
                s10.d = 4;
                s10.e = 5;
                s10.f = 6;
                s10.g = 7;
                s10.h = 8;
                InvokeCallback10((par) =>
                {
                    Console.WriteLine("S10: {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}", par.a, par.b, par.c, par.d, par.e, par.f, par.g, par.h);
                    if (par.a != 1 || par.b != 2 || par.c != 3 || par.d != 4 ||
                        par.e != 5 || par.f != 6 || par.g != 7 || par.h != 8)
                    {
                        throw new System.Exception();
                    }
                }, s10);

                S11 s11;
                s11.a = 1;
                s11.b = 2;
                s11.c = 3;
                s11.d = 4;
                s11.e = 5;
                InvokeCallback11((par) =>
                {
                    Console.WriteLine("S11: {0}, {1}, {2}, {3}, {4}", par.a, par.b, par.c, par.d, par.e);
                    if (par.a != 1 || par.b != 2 || par.c != 3 || par.d != 4 || par.e != 5)
                    {
                        throw new System.Exception();
                    }
                }, s11);

                S12 s12;
                s12.a = 1;
                s12.b = 2;
                s12.c = 3;
                s12.d = 4;
                s12.e = 5;
                s12.f = 6;
                s12.g = 7;
                s12.h = 8;
                s12.i = 9;
                InvokeCallback12((par) =>
                {
                    Console.WriteLine("S12: {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}", par.a, par.b, par.c, par.d, par.e, par.f, par.g, par.h, par.i);
                    if (par.a != 1 || par.b != 2 || par.c != 3 || par.d != 4 ||
                        par.e != 5 || par.f != 6 || par.g != 7 || par.h != 8 || par.i != 9)
                    {
                        throw new System.Exception();
                    }
                }, s12);

                S13 s13;
                s13.hasValue = 1;
                s13.x = 2;
                InvokeCallback13((par) =>
                {
                    Console.WriteLine("S13: {0}, {1}", par.hasValue, par.x);
                    if (par.hasValue != 1 || par.x != 2)
                    {
                        throw new System.Exception();
                    }
                }, s13);

                S14 s14;
                s14.x = 1;
                s14.y = 2;
                InvokeCallback14((par) =>
                {
                    Console.WriteLine("S14: {0}, {1}", par.x, par.y);
                    if (par.x != 1 || par.y != 2)
                    {
                        throw new System.Exception();
                    }
                }, s14);

                S15 s15;
                s15.a = 1;
                s15.b = 2;
                s15.c = 3;
                s15.d = 4;
                s15.e = 5;
                s15.f = 6;
                s15.g = 7;
                s15.h = 8;
                s15.i = 9;
                InvokeCallback15((par) =>
                {
                    Console.WriteLine("S15: {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}", par.a, par.b, par.c, par.d, par.e, par.f, par.g, par.h, par.i);
                    if (par.a != 1 || par.b != 2 || par.c != 3 || par.d != 4 ||
                        par.e != 5 || par.f != 6 || par.g != 7 || par.h != 8 || par.i != 9)
                    {
                        throw new System.Exception();
                    }
                }, s15);

                S16 s16;
                s16.x = 1;
                s16.y = 2;
                InvokeCallback16((par) =>
                {
                    Console.WriteLine("S16: {0}, {1}", par.x, par.y);
                    if (par.x != 1 || par.y != 2)
                    {
                        throw new System.Exception();
                    }
                }, s16);

                S17 s17;
                s17.x = 1;
                s17.y = 2;
                InvokeCallback17((par) =>
                {
                    Console.WriteLine("S17: {0}, {1}", par.x, par.y);
                    if (par.x != 1 || par.y != 2)
                    {
                        throw new System.Exception();
                    }
                }, s17);

                S18 s18;
                s18.x = 1;
                s18.y = 2;
                s18.z = 3;
                InvokeCallback18((par) =>
                {
                    Console.WriteLine("S18: {0}, {1}, {2}", par.x, par.y, par.z);
                    if (par.x != 1 || par.y != 2 || par.z != 3)
                    {
                        throw new System.Exception();
                    }
                }, s18);

                S19 s19;
                s19.x = 1;
                s19.y = 2;
                s19.z = 3;
                s19.w = 4;
                InvokeCallback19((par) =>
                {
                    Console.WriteLine("S19: {0}, {1}, {2}, {3}", par.x, par.y, par.z, par.w);
                    if (par.x != 1 || par.y != 2 || par.z != 3 || par.w != 4)
                    {
                        throw new System.Exception();
                    }
                }, s19);

                S20 s20;
                s20.x = 1;
                s20.y = 2;
                s20.z = 3;
                s20.w = 4;
                InvokeCallback20((par) =>
                {
                    Console.WriteLine("S20: {0}, {1}, {2}, {3}", par.x, par.y, par.z, par.w);
                    if (par.x != 1 || par.y != 2 || par.z != 3 || par.w != 4)
                    {
                        throw new System.Exception();
                    }
                }, s20);

                /* These tests are not working on non Windows CoreCLR.  Enable this when GH Issue #2076 is resolved.
                TestClass testClass = new TestClass();
                S28 s28;
                s28.x = null;
                s28.y = 1;

                InvokeCallback28((par) => {
                    Console.WriteLine("S28: {0}, {1}", par.x == null ? "Null" : "Not null", par.y);
                    if (par.x != null || par.y != 1)
                    {
                        throw new System.Exception();
                    }
                }, s28);

                s28.x = testClass;
                s28.y = 5;

                InvokeCallback28((par) => {
                    Console.WriteLine("S28: {0}, {1}", par.x == null ? "Null" : "Not null", par.y);
                    if (par.x != testClass || par.y != 5)
                    {
                        throw new System.Exception();
                    }
                }, s28);

                S29 s29;
                s29.x = 1;
                s29.y = null;

                InvokeCallback29((par) => {
                    Console.WriteLine("S29: {0}, {1}", par.x, par.y == null ? "Null" : "Not null");
                    if (par.x != 1 || par.y != null)
                    {
                        throw new System.Exception();
                    }
                }, s29);

                s29.x = 5;
                s29.y = testClass;

                InvokeCallback29((par) => {
                    Console.WriteLine("S29: {0}, {1}", par.x, par.y == null ? "Null" : "Not null");
                    if (par.x != 5 || par.y != testClass)
                    {
                        throw new System.Exception();
                    }
                }, s29);
                 Enable this when GH Issue #2076 is resolved. */
                S30 s30;
                s30.x = 1;
                s30.y = 2;

                S30 s30_2;
                s30_2.x = 3;
                s30_2.y = 4;

                S30 s30_3;
                s30_3.x = 5;
                s30_3.y = 6;

                // Program p = new Program();
                InvokeCallback30(p.Test30, s30, s30_2, s30_3);
                S1 s1r = InvokeCallback1R((par) =>
                {
                    Console.WriteLine("S1: {0}, {1}, {2}, {3}", par.x, par.y, par.z, par.w);
                    if (par.x != 1 || par.y != 2 || par.z != 3 || par.w != 4)
                    {
                        throw new System.Exception();
                    }

                }, s1);
                Console.WriteLine("S1R: {0}, {1}, {2}, {3}", s1r.x, s1r.y, s1r.z, s1r.w);
                if (s1r.x != 1 || s1r.y != 2 || s1r.z != 3 || s1r.w != 4)
                {
                    throw new System.Exception();
                }

                S2 s2r = InvokeCallback2R((par) =>
                {
                    Console.WriteLine("S2: {0}, {1}, {2}", par.x, par.y, par.z);
                    if (par.x != 1 || par.y != 2 || par.z != 3)
                    {
                        throw new System.Exception();
                    }
                }, s2);
                Console.WriteLine("S2R: {0}, {1}, {2}", s2r.x, s2r.y, s2r.z);
                if (s2r.x != 1 || s2r.y != 2 || s2r.z != 3)
                {
                    throw new System.Exception();
                }

                S3 s3r = InvokeCallback3R((par) =>
                {
                    Console.WriteLine("S3: {0}, {1}, {2}", par.x, par.y, par.z);
                    if (par.x != 1 || par.y != 2 || par.z != 3)
                    {
                        throw new System.Exception();
                    }
                }, s3);
                Console.WriteLine("S3R: {0}, {1}, {2}", s3r.x, s3r.y, s3r.z);
                if (s3r.x != 1 || s3r.y != 2 || s3r.z != 3)
                {
                    throw new System.Exception();
                }

                S4 s4r = InvokeCallback4R((par) =>
                {
                    Console.WriteLine("S4: {0}, {1}", par.x, par.y);
                    if (par.x != 1 || par.y != 2)
                    {
                        throw new System.Exception();
                    }
                }, s4);
                Console.WriteLine("S4R: {0}, {1}", s4r.x, s4r.y);
                if (s4r.x != 1 || s4r.y != 2)
                {
                    throw new System.Exception();
                }

                S5 s5r = InvokeCallback5R((par) =>
                {
                    Console.WriteLine("S5: {0}, {1}", par.x, par.y);
                    if (par.x != 1 || par.y != 2)
                    {
                        throw new System.Exception();
                    }
                }, s5);
                Console.WriteLine("S5R: {0}, {1}", s5r.x, s5r.y);
                if (s5r.x != 1 || s5r.y != 2)
                {
                    throw new System.Exception();
                }

                S6 s6r = InvokeCallback6R((par) =>
                {
                    Console.WriteLine("S6: {0}, {1}, {2}, {3}", par.x, par.y, par.z, par.w);
                    if (par.x != 1 || par.y != 2 || par.z != 3 || par.w != 4)
                    {
                        throw new System.Exception();
                    }
                }, s6);
                Console.WriteLine("S6R: {0}, {1}, {2}, {3}", s6r.x, s6r.y, s6r.z, s6r.w);
                if (s6r.x != 1 || s6r.y != 2 || s6r.z != 3 || s6r.w != 4)
                {
                    throw new System.Exception();
                }

                S7 s7r = InvokeCallback7R((par) =>
                {
                    Console.WriteLine("S7: {0}, {1}, {2}", par.x, par.y, par.z);
                    if (par.x != 1 || par.y != 2 || par.z != 3)
                    {
                        throw new System.Exception();
                    }
                }, s7);
                Console.WriteLine("S7R: {0}, {1}, {2}", s7r.x, s7r.y, s7r.z);
                if (s7r.x != 1 || s7r.y != 2 || s7r.z != 3)
                {
                    throw new System.Exception();
                }

                S8 s8r = InvokeCallback8R((par) =>
                {
                    Console.WriteLine("S8: {0}, {1}", par.x, par.y);
                    if (par.x != 1 || par.y != 2)
                    {
                        throw new System.Exception();
                    }
                }, s8);
                Console.WriteLine("S8R: {0}, {1}", s8r.x, s8r.y);
                if (s8r.x != 1 || s8r.y != 2)
                {
                    throw new System.Exception();
                }

                S9 s9r = InvokeCallback9R((par) =>
                {
                    Console.WriteLine("S9: {0}, {1}, {2}, {3}", par.x, par.y, par.z, par.w);
                    if (par.x != 1 || par.y != 2 || par.z != 3 || par.w != 4)
                    {
                        throw new System.Exception();
                    }
                }, s9);
                Console.WriteLine("S9R: {0}, {1}, {2}, {3}", s9r.x, s9r.y, s9r.z, s9r.w);
                if (s9r.x != 1 || s9r.y != 2 || s9r.z != 3 || s9r.w != 4)
                {
                    throw new System.Exception();
                }

                S10 s10r = InvokeCallback10R((par) =>
                {
                    Console.WriteLine("S10: {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}", par.a, par.b, par.c, par.d, par.e, par.f, par.g, par.h);
                    if (par.a != 1 || par.b != 2 || par.c != 3 || par.d != 4 ||
                        par.e != 5 || par.f != 6 || par.g != 7 || par.h != 8)
                    {
                        throw new System.Exception();
                    }
                }, s10);
                Console.WriteLine("S10R: {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}", s10r.a, s10r.b, s10r.c, s10r.d, s10r.e, s10r.f, s10r.g, s10r.h);
                if (s10r.a != 1 || s10r.b != 2 || s10r.c != 3 || s10r.d != 4 ||
                    s10r.e != 5 || s10r.f != 6 || s10r.g != 7 || s10r.h != 8)
                {
                    throw new System.Exception();
                }

                S11 s11r = InvokeCallback11R((par) =>
                {
                    Console.WriteLine("S11: {0}, {1}, {2}, {3}, {4}", par.a, par.b, par.c, par.d, par.e);
                    if (par.a != 1 || par.b != 2 || par.c != 3 || par.d != 4 || par.e != 5)
                    {
                        throw new System.Exception();
                    }
                }, s11);
                Console.WriteLine("S11R: {0}, {1}, {2}, {3}, {4}", s11r.a, s11r.b, s11r.c, s11r.d, s11r.e);
                if (s11r.a != 1 || s11r.b != 2 || s11r.c != 3 || s11r.d != 4 || s11r.e != 5)
                {
                    throw new System.Exception();
                }

                S12 s12r = InvokeCallback12R((par) =>
                {
                    Console.WriteLine("S12: {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}", par.a, par.b, par.c, par.d, par.e, par.f, par.g, par.h, par.i);
                    if (par.a != 1 || par.b != 2 || par.c != 3 || par.d != 4 ||
                        par.e != 5 || par.f != 6 || par.g != 7 || par.h != 8 || par.i != 9)
                    {
                        throw new System.Exception();
                    }
                }, s12);
                Console.WriteLine("S12R: {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}", s12r.a, s12r.b, s12r.c, s12r.d, s12r.e, s12r.f, s12r.g, s12r.h, s12r.i);
                if (s12r.a != 1 || s12r.b != 2 || s12r.c != 3 || s12r.d != 4 ||
                    s12r.e != 5 || s12r.f != 6 || s12r.g != 7 || s12r.h != 8 || s12r.i != 9)
                {
                    throw new System.Exception();
                }

                S13 s13r = InvokeCallback13R((par) =>
                {
                    Console.WriteLine("S13: {0}, {1}", par.hasValue, par.x);
                    if (par.hasValue != 1 || par.x != 2)
                    {
                        throw new System.Exception();
                    }
                }, s13);
                Console.WriteLine("S13R: {0}, {1}", s13r.hasValue, s13r.x);
                if (s13r.hasValue != 1 || s13r.x != 2)
                {
                    throw new System.Exception();
                }

                S14 s14r = InvokeCallback14R((par) =>
                {
                    Console.WriteLine("S14: {0}, {1}", par.x, par.y);
                    if (par.x != 1 || par.y != 2)
                    {
                        throw new System.Exception();
                    }
                }, s14);
                Console.WriteLine("S14R: {0}, {1}", s14r.x, s14r.y);
                if (s14r.x != 1 || s14r.y != 2)
                {
                    throw new System.Exception();
                }

                S15 s15r = InvokeCallback15R((par) =>
                {
                    Console.WriteLine("S15: {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}", par.a, par.b, par.c, par.d, par.e, par.f, par.g, par.h, par.i);
                    if (par.a != 1 || par.b != 2 || par.c != 3 || par.d != 4 ||
                        par.e != 5 || par.f != 6 || par.g != 7 || par.h != 8 || par.i != 9)
                    {
                        throw new System.Exception();
                    }
                }, s15);
                Console.WriteLine("S15R: {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}", s15r.a, s15r.b, s15r.c, s15r.d, s15r.e, s15r.f, s15r.g, s15r.h, s15r.i);
                if (s15r.a != 1 || s15r.b != 2 || s15r.c != 3 || s15r.d != 4 ||
                    s15r.e != 5 || s15r.f != 6 || s15r.g != 7 || s15r.h != 8 || s15r.i != 9)
                {
                    throw new System.Exception();
                }

                S16 s16r = InvokeCallback16R((par) =>
                {
                    Console.WriteLine("S16: {0}, {1}", par.x, par.y);
                    if (par.x != 1 || par.y != 2)
                    {
                        throw new System.Exception();
                    }
                }, s16);
                Console.WriteLine("S16R: {0}, {1}", s16r.x, s16r.y);
                if (s16r.x != 1 || s16r.y != 2)
                {
                    throw new System.Exception();
                }

                S17 s17r = InvokeCallback17R((par) =>
                {
                    Console.WriteLine("S17: {0}, {1}", par.x, par.y);
                    if (par.x != 1 || par.y != 2)
                    {
                        throw new System.Exception();
                    }
                }, s17);
                Console.WriteLine("S17R: {0}, {1}", s17r.x, s17r.y);
                if (s17r.x != 1 || s17r.y != 2)
                {
                    throw new System.Exception();
                }

                S18 s18r = InvokeCallback18R((par) =>
                {
                    Console.WriteLine("S18: {0}, {1}, {2}", par.x, par.y, par.z);
                    if (par.x != 1 || par.y != 2 || par.z != 3)
                    {
                        throw new System.Exception();
                    }
                }, s18);
                Console.WriteLine("S18R: {0}, {1}, {2}", s18r.x, s18r.y, s18r.z);
                if (s18r.x != 1 || s18r.y != 2 || s18r.z != 3)
                {
                    throw new System.Exception();
                }

                S19 s19r = InvokeCallback19R((par) =>
                {
                    Console.WriteLine("S19: {0}, {1}, {2}, {3}", par.x, par.y, par.z, par.w);
                    if (par.x != 1 || par.y != 2 || par.z != 3 || par.w != 4)
                    {
                        throw new System.Exception();
                    }
                }, s19);
                Console.WriteLine("S19R: {0}, {1}, {2}, {3}", s19r.x, s19r.y, s19r.z, s19r.w);
                if (s19r.x != 1 || s19r.y != 2 || s19r.z != 3 || s19r.w != 4)
                {
                    throw new System.Exception();
                }

                S20 s20r = InvokeCallback20R((par) =>
                {
                    Console.WriteLine("S20: {0}, {1}, {2}, {3}", par.x, par.y, par.z, par.w);
                    if (par.x != 1 || par.y != 2 || par.z != 3 || par.w != 4)
                    {
                        throw new System.Exception();
                    }
                }, s20);
                Console.WriteLine("S20R: {0}, {1}, {2}, {3}", s20r.x, s20r.y, s20r.z, s20r.w);
                if (s20r.x != 1 || s20r.y != 2 || s20r.z != 3 || s20r.w != 4)
                {
                    throw new System.Exception();
                }
                /* These tests are not working on non Windows CoreCLR.  Enable this when GH Issue #2076 is resolved.
                s28.x = null;
                S28 s28r = InvokeCallback28R((par) => {
                    Console.WriteLine("S28: {0}, {1}", par.x == null ? "Null" : "Not null", par.y);
                    if (par.x == null || par.y != 5)
                    {
                        throw new System.Exception();
                    }
                }, s28);
                Console.WriteLine("S28R: {0}, {1}", s28r.x == null ? "Null" : "Not null", s28r.y);
                if (s28r.x == null || s28r.y != 5)
                {
                    throw new System.Exception();
                }

                s28.x = testClass;
                s28.y = 5;

                s28r = InvokeCallback28R((par) => {
                    Console.WriteLine("S28: {0}, {1}", par.x == null ? "Null" : "Not null", par.y);
                    if (par.x != testClass || par.y != 5)
                    {
                        throw new System.Exception();
                    }
                }, s28);
                Console.WriteLine("S28R: {0}, {1}", s28r.x == null ? "Null" : "Not null", s28r.y);
                if (s28r.x != testClass || s28r.y != 5)
                {
                    throw new System.Exception();
                }

                s29.y = null;
                S29 s29r = InvokeCallback29R((par) => {
                    Console.WriteLine("S29: {0}, {1}", par.x, par.y == null ? "Null" : "Not null");
                    if (par.x != 5 || par.y == null)
                    {
                        throw new System.Exception();
                    }
                }, s29);
                Console.WriteLine("S29R: {0}, {1}", s29r.x, s29r.y == null ? "Null" : "Not null");
                if (s29r.x != 5 || s29r.y == null)
                {
                    throw new System.Exception();
                }

                s29.x = 5;
                s29.y = testClass;
                s29r = InvokeCallback29R((par) => {
                    Console.WriteLine("S29: {0}, {1}", par.x, par.y == null ? "Null" : "Not null");
                    if (par.x != 5 || par.y != testClass)
                    {
                        throw new System.Exception();
                    }
                }, s29);            
                Console.WriteLine("S29R: {0}, {1}", s29r.x, s29r.y == null ? "Null" : "Not null");
                if (s29r.x != 5 || s29r.y != testClass)
                {
                    throw new System.Exception();
                }
                 Enable this when GH Issue #2076 is resolved. */
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return -1;
            }
            return 100;
        }
        
        void Test30(S30 par1, S30 par2, S30 par3)
        {
            Console.WriteLine("S30: {0}, {1}, {2}, {3}, {4}, {5}", par1.x, par1.y, par2.x, par2.y, par3.x, par3.y);
            if (par1.x != 1 || par1.y != 2 || par2.x != 3 || par2.y != 4 ||
                par3.x != 5 || par3.y != 6)
            {
                throw new System.Exception();
            }
        }
    }
}
