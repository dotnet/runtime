// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
//
//'varDsc->IsAlwaysAliveInMemory() || ((regSet.GetMaskVars() & regMask) == 0)' in '_107841.Program:M137(_107841.C1,byref)'

using System;
using Xunit;

namespace _107841
{
    public interface I0
    {
    }

    public interface I1
    {
    }

    public class C0 : I1
    {
        public ushort F0;
        public double F1;
        public uint F3;
        public ulong F4;
        public bool F5;
        public bool F6;
        public uint F7;
        public C0(double f1, double f2, float f8)
        {
        }
    }

    public struct S0
    {
        public bool F0;
        public ulong F1;
        public C0 F2;
        public bool F3;
        public int F4;
        public long F5;
        public C0 F6;
        public uint F7;
        public sbyte F8;
        public int F9;
        public S0(C0 f2, bool f3, C0 f6, int f9) : this()
        {
        }
    }

    public class C1 : I1
    {
        public short F0;
        public int F2;
        public sbyte F3;
        public int F4;
        public ushort F5;
        public sbyte F6;
        public short F7;
        public C1(float f1)
        {
        }
    }

    public struct S1 : I1
    {
        public long F1;
        public S0 F2;
        public float F3;
        public S1(float f0, long f1, S0 f2, float f3) : this()
        {
        }
    }

    public class Program
    {
        public static IRuntime s_rt;
        public static C0[] s_2;
        public static S1 s_4;
        public static double[][][][] s_7;
        public static I0 s_8;
        public static short[][] s_9;
        public static S0[,] s_17;
        public static float[] s_26;
        public static C1[] s_29;
        public static bool s_33;
        public static S0 s_42;
        public static I0 s_43;
        public static S1 s_48;
        public static S1[] s_59;
        public static S0[,,] s_66;
        public static S1 s_68;
        public static S1 s_71;
        public static S0 s_78;
        public static I0 s_85;
        public static I0 s_95;
        public static S0 s_101;
        public static I0 s_107;
        public static I0 s_111;
        public static I0[] s_112;
        public static S1[,,][,][][] s_113;
        public static S1 s_120;
        public static S0 s_127;
        public static C1 s_128;
        public static S0 s_135;
        public static C0 s_141;
        public static ulong s_143;
        public static S1 s_149;

        [Fact]
        public static void TestEntryPoint()
        {
            try
            {
                CollectibleALC alc = new CollectibleALC();
                System.Reflection.Assembly asm = alc.LoadFromAssemblyPath(System.Reflection.Assembly.GetExecutingAssembly().Location);
                System.Reflection.MethodInfo mi = asm.GetType(typeof(Program).FullName).GetMethod(nameof(MainInner));
                System.Type runtimeTy = asm.GetType(typeof(Runtime).FullName);
                mi.Invoke(null, new object[] { System.Activator.CreateInstance(runtimeTy) });
            } catch {}
        }

        private static void MainInner(IRuntime rt)
        {
            var vr54 = new C1(0);
            M137(vr54, ref s_26);
        }

        private static bool M140(C0 argThis, S1[,] arg0, double arg1, ref double[][] arg2)
        {
            return default(bool);
        }

        private static C0 M143()
        {
            return default(C0);
        }

        private static S1[,] M144()
        {
            return default(S1[,]);
        }

        private static S0 M142(S0 argThis, ref S0 arg0, S1 arg1, S1 arg2, ref I0 arg3, ref ulong arg4)
        {
            return default(S0);
        }

        private static void M137(C1 argThis, ref float[] arg1)
        {
            double[] var0 = default(double[]);
            S0 var14 = default(S0);
            C1 var20 = default(C1);
            bool[][] var28 = default(bool[][]);
            byte var31 = default(byte);
            byte[] var35 = default(byte[]);
            S1 var36 = default(S1);
            long var37 = default(long);
            if (s_17[0, 0].F3)
            {
                if (s_113[0, 0, 0][0, 0][0][0].F2.F2.F5)
                {
                    if (s_78.F2.F6)
                    {
                        s_48.F3 = arg1[0]--;
                        return;
                    }
                    else
                    {
                        s_68.F2 = new S0(new C0(0, 0, 3.4028235E+38f), true, new C0(0, 0, 0), 0);
                    }

                    var vr3 = s_4.F2.F2;
                    var vr26 = new S1[,]
                    {
                    {
                        new S1(0, 0, new S0(new C0(1.7976931348623157E+308d, 0, 0), false, new C0(0, 0, 0), 0), 0)
                    },
                    {
                        new S1(0, 0, new S0(new C0(0, 0, 0), false, new C0(0, 0, 3.4028235E+38f), 0), 0)
                    },
                    {
                        new S1(0, 0, new S0(new C0(0, 0, -3.4028235E+38f), false, new C0(0, 0, 0), 0), 0)
                    },
                    {
                        new S1(0, 0, new S0(new C0(0, 1, 0), false, new C0(0, 0, -3.4028235E+38f), 0), 3.4028235E+38f)
                    },
                    {
                        new S1(0, 0, new S0(new C0(1, 1, 0), true, new C0(0, 1, 0), 0), 0)
                    }
                    };
                    var vr33 = var0[0]++;
                    bool vr44 = M140(vr3, vr26, vr33, ref s_7[0][0]);
                }

                C0 var9 = new C0(0, 0, 0);
                s_rt.WriteLine("c_5143", 0);
                s_rt.WriteLine("c_5144", var9.F0);
                s_rt.WriteLine("c_5147", var9.F3);
                s_rt.WriteLine("c_5148", var9.F4);
                s_rt.WriteLine("c_5149", var9.F5);
                s_rt.WriteLine("c_5150", var9.F6);
                s_rt.WriteLine("c_5151", var9.F7);
                for (int var11 = 0; var11 < 2; var11++)
                {
                    var vr13 = new S0(new C0(0, -1.7976931348623157E+308d, 0), false, new C0(0, 1, 0), 0);
                    var vr15 = new S1(0, 0, new S0(new C0(0, 0, 0), true, new C0(0, 0, 0), 0), 0);
                    var vr24 = s_113[0, 0, 0][0, 0][0][0];
                    M142(vr13, ref s_48.F2, vr15, vr24, ref s_111, ref s_2[0].F4);
                }

                int var12 = 0;
                s_rt.WriteLine("c_5162", var12);
                var vr6 = s_17[0, 0];
                M142(vr6, ref s_66[0, 0, 0], new S1(0, 0, new S0(new C0(0, 0, 0), false, new C0(0, 0, 0), 0), 0), s_71, ref s_8, ref s_42.F6.F4);
            }
            else
            {
                var vr4 = new C0(1.7976931348623157E+308d, 1, 0);
                C0 vr56 = default(C0);
                var vr7 = new S1[,]
                {
                {
                    new S1(0, 0, new S0(new C0(0, 0, 0), false, new C0(0, 0, 0), 0), 0),
                    new S1(0, 0, new S0(new C0(-1.7976931348623157E+308d, 0, 0), true, new C0(0, 0, -3.4028235E+38f), 0), 0),
                    new S1(0, 0, new S0(new C0(0, 0, 0), true, new C0(0, 0, 0), 0), 0)
                },
                {
                    new S1(0, 0, new S0(new C0(0, 0, 0), false, new C0(0, 0, 0), 0), 0),
                    new S1(0, 0, new S0(new C0(0, 0, 3.4028235E+38f), false, new C0(0, 0, 0), 0), 0),
                    new S1(3.4028235E+38f, 0, new S0(new C0(0, 0, 0), false, new C0(0, 0, 0), 0), 0)
                }
                };
                var vr16 = s_68.F2.F6.F1;
                M140(vr56, vr7, vr16, ref s_7[0][0]);
                s_rt.WriteLine("c_5174", var14.F0);
                s_rt.WriteLine("c_5175", var14.F1);
                s_rt.WriteLine("c_5176", var14.F2.F0);
                s_rt.WriteLine("c_5179", var14.F2.F3);
                s_rt.WriteLine("c_5180", var14.F2.F4);
                s_rt.WriteLine("c_5181", var14.F2.F5);
                s_rt.WriteLine("c_5183", var14.F2.F7);
                s_rt.WriteLine("c_5195", var14.F6.F7);
                s_rt.WriteLine("c_5199", var14.F9);
            }

            argThis.F6 = (sbyte)s_78.F5--;
            var vr21 = new C0(0, 0, 0);
            var vr28 = new S1[,]
            {
            {
                new S1(0, 0, new S0(new C0(0, 0, 0), true, new C0(0, 0, 0), 0), 0),
                new S1(0, 0, new S0(new C0(1.7976931348623157E+308d, 0, 0), true, new C0(0, 0, 0), 0), 0),
                new S1(0, 0, new S0(new C0(0, 0, 0), true, new C0(0, 0, 0), 0), 0),
                new S1(0, 0, new S0(new C0(0, 0, 0), false, new C0(0, 0, 0), 0), 0)
            }
            };
            C0 vr64 = default(C0);
            C0 vr65 = vr21;
            bool vr69 = default(bool);
            var vr5 = new S0(new C0(0, 0, 3.4028235E+38f), vr69, vr64, argThis.F2);
            var vr32 = new S0(new C0(0, 0, 0), true, new C0(0, 0, 0), 0);
            if (M141(argThis))
            {
                var vr17 = new S1[,]
                {
                {
                    new S1(0, 0, new S0(new C0(0, 0, 0), false, new C0(0, 0, 3.4028235E+38f), 0), 0),
                    new S1(0, 0, new S0(new C0(0, 0, 0), false, new C0(1.7976931348623157E+308d, 0, 0), 0), 0),
                    new S1(-3.4028235E+38f, 0, new S0(new C0(0, 0, 0), true, new C0(0, 0, 0), 0), 0),
                    new S1(0, 0, new S0(new C0(0, 1.7976931348623157E+308d, 0), true, new C0(0, 0, 0), 0), 0),
                    new S1(0, 0, new S0(new C0(0, 0, 0), false, new C0(0, 0, 0), 0), -3.4028235E+38f),
                    new S1(-3.4028235E+38f, 0, new S0(new C0(0, 0, 0), true, new C0(0, 0, 0), 0), 0)
                }
                };
                if (s_33)
                {
                    I1[] var18 = new I1[]
                    {
                    new C0(0, 0, 0),
                    new C1(0),
                    new C1(3.4028235E+38f),
                    new C0(0, 0, 0),
                    new C1(3.4028235E+38f),
                    new S1(0, -8736734971933627417L, new S0(new C0(1.7976931348623157E+308d, 0, 0), false, new C0(0, 0, 0), 523515085), -3.4028235E+38f),
                    new C0(0, 0, 0),
                    new C1(-3.4028235E+38f),
                    new S1(0, 0, new S0(new C0(0, 0, 0), false, new C0(0, 0, 0), 1), 0)
                    };
                }
                else
                {
                    var vr12 = new S0(new C0(0, 0, 0), true, new C0(0, 0, 0), 0);
                    var vr34 = new S1(0, 0, new S0(new C0(1.7976931348623157E+308d, 0, 0), true, new C0(0, 0, 0), 0), 0);
                    var vr8 = M142(vr12, ref s_113[0, 0, 0][0, 0][0][0].F2, vr34, new S1(0, 0, new S0(new C0(0, 0, 0), true, new C0(0, 0, 0), 0), 0), ref s_43, ref s_68.F2.F6.F4);
                    var vr20 = new S0(new C0(0, 0, 3.4028235E+38f), true, new C0(0, 0, 0), 0);
                    var vr40 = new C1(0);
                    var vr42 = new S0(new C0(0, 0, -3.4028235E+38f), true, new C0(0, 0, 0), 0);
                    try
                    {
                        s_9 = s_9;
                    }
                    finally
                    {
                        s_rt.WriteLine("c_5201", var20.F0);
                        s_rt.WriteLine("c_5203", var20.F2);
                        s_rt.WriteLine("c_5204", var20.F3);
                        s_rt.WriteLine("c_5205", var20.F4);
                        s_rt.WriteLine("c_5206", var20.F5);
                        s_rt.WriteLine("c_5208", var20.F7);
                    }

                    S0 var21 = s_59[0].F2;
                    var21.F2.F0 = s_48.F2.F6.F0++;
                    s_rt.WriteLine("c_5210", var21.F0);
                    s_rt.WriteLine("c_5211", var21.F1);
                    s_rt.WriteLine("c_5216", var21.F2.F4);
                    s_rt.WriteLine("c_5217", var21.F2.F5);
                    s_rt.WriteLine("c_5218", var21.F2.F6);
                    s_rt.WriteLine("c_5219", var21.F2.F7);
                    s_rt.WriteLine("c_5221", var21.F3);
                    s_rt.WriteLine("c_5223", var21.F5);
                    s_rt.WriteLine("c_5229", var21.F6.F5);
                    s_rt.WriteLine("c_5230", var21.F6.F6);
                    s_rt.WriteLine("c_5233", var21.F7);
                    s_rt.WriteLine("c_5234", var21.F8);
                }

                s_7[0][0][0][0] = var0[0]--;
                var vr38 = new S0(new C0(0, 1.7976931348623157E+308d, 0), true, new C0(0, 0, 0), 0);
                var vr39 = s_59[0];
                var vr35 = M142(vr38, ref s_101, new S1(0, -7153361952214254310L, new S0(new C0(0, 0, 0), true, new C0(0, 0, 0), 0), 0), vr39, ref s_85, ref s_17[0, 0].F1);
                M142(vr35, ref s_66[0, 0, 0], new S1(0, -7210483454585770996L, new S0(new C0(0, 0, 0), false, new C0(0, 0, 0), 1), 0), new S1(0, 2810699383726706373L, new S0(new C0(0, 0, 0), true, new C0(0, 1.7976931348623157E+308d, 0), 2147483646), 0), ref s_111, ref s_68.F2.F1);
                I1[,] var23 = new I1[,]
                {
                {
                    new C1(0),
                    new C0(0, 0, 0),
                    new S1(-3.4028235E+38f, 0, new S0(new C0(0, 0, 0), true, new C0(0, 0, 3.4028235E+38f), 0), 3.4028235E+38f),
                    new C0(0, 0, 0),
                    new C0(0, 0, 0),
                    new S1(0, 0, new S0(new C0(0, 0, 0), false, new C0(0, 0, 0), 0), 0),
                    new C0(0, 0, 0),
                    new S1(0, 0, new S0(new C0(0, 0, 0), true, new C0(0, 0, 3.4028235E+38f), 0), 0),
                    new C1(0),
                    new C1(0)
                },
                {
                    new C1(3.4028235E+38f),
                    new C0(0, 0, 0),
                    new C1(0),
                    new C0(0, 0, 0),
                    new C0(0, 0, 0),
                    new C1(0),
                    new S1(0, 0, new S0(new C0(0, 0, 0), true, new C0(0, 0, 0), 0), 0),
                    new C1(3.4028235E+38f),
                    new C0(0, 0, 0),
                    new C0(0, 0, 0)
                }
                };
            }

            C1 var25 = s_128;
            C1 var26 = new C1(0);
            M142(s_42, ref s_135, new S1(0, 725286118131275146L, new S0(new C0(0, 0, 0), false, new C0(0, 0, 0), 0), 0), new S1(0, 0, new S0(new C0(0, 0, 0), false, new C0(0, 0, 0), 0), 0), ref s_112[0], ref s_17[0, 0].F1);
            var vr41 = new S0(new C0(0, 0, 0), true, new C0(0, 0, 3.4028235E+38f), 0);
            bool vr0 = s_120.F2.F6.F4-- < var0[0];
            var vr10 = new S0(new C0(0, 0, 0), true, new C0(0, 0, 0), 0);
            var vr30 = new S1(0, 0, new S0(new C0(0, 0, 0), true, new C0(0, 0, 0), 0), 0);
            M142(vr10, ref s_101, vr30, new S1(0, 0, new S0(new C0(0, 0, 0), true, new C0(0, 1, 3.4028235E+38f), 0), 0), ref s_95, ref s_141.F4);
            s_rt.WriteLine("c_5240", var28[0][0]);
            var vr45 = s_29[0];
            var vr46 = new S0(new C0(0, 0, 0), true, new C0(0, 0, 0), 0);
            if (M141(vr45))
            {
                var vr19 = new S0(new C0(0, 0, 3.4028235E+38f), true, new C0(0, 0, 0), 0);
                s_rt.WriteLine("c_5243", var31);
                var vr23 = new S0(new C0(0, 0, 0), true, new C0(0, 0, 0), 0);
            }

            C1 var32 = new C1(0);
            byte[] var34 = new byte[]
            {
            1
            };
            s_rt.WriteLine("c_5406", var35[0]);
            var vr47 = s_149.F2.F2.F3++;
            s_rt.WriteLine("c_5409", var36.F2.F0);
            s_rt.WriteLine("c_5411", var36.F2.F2.F0);
            s_rt.WriteLine("c_5414", var36.F2.F2.F3);
            s_rt.WriteLine("c_5415", var36.F2.F2.F4);
            s_rt.WriteLine("c_5416", var36.F2.F2.F5);
            s_rt.WriteLine("c_5417", var36.F2.F2.F6);
            s_rt.WriteLine("c_5420", var36.F2.F3);
            s_rt.WriteLine("c_5422", var36.F2.F5);
            s_rt.WriteLine("c_5426", var36.F2.F6.F3);
            s_rt.WriteLine("c_5427", var36.F2.F6.F4);
            s_rt.WriteLine("c_5430", var36.F2.F6.F7);
            s_rt.WriteLine("c_5432", var36.F2.F7);
            s_rt.WriteLine("c_5433", var36.F2.F8);
            s_rt.WriteLine("c_5434", var36.F2.F9);
            s_rt.WriteLine("c_5435", System.BitConverter.SingleToUInt32Bits(var36.F3));
            var vr48 = s_149.F2;
            M142(vr48, ref s_149.F2, s_71, new S1(0, 0, new S0(new C0(0, 0, 0), true, new C0(0, 0, -3.4028235E+38f), 0), 0), ref s_85, ref s_149.F2.F6.F4);
            s_rt.WriteLine("c_5437", s_149.F1);
            s_rt.WriteLine("c_5438", s_149.F2.F0);
            s_rt.WriteLine("c_5439", s_149.F2.F1);
            s_rt.WriteLine("c_5444", s_149.F2.F2.F4);
            s_rt.WriteLine("c_5449", s_149.F2.F3);
            s_rt.WriteLine("c_5450", s_149.F2.F4);
            s_rt.WriteLine("c_5452", s_149.F2.F6.F0);
            s_rt.WriteLine("c_5455", s_149.F2.F6.F3);
            s_rt.WriteLine("c_5456", s_149.F2.F6.F4);
            s_rt.WriteLine("c_5457", s_149.F2.F6.F5);
            s_rt.WriteLine("c_5459", s_149.F2.F6.F7);
            s_rt.WriteLine("c_5461", s_149.F2.F7);
            s_rt.WriteLine("c_5462", s_149.F2.F8);
            s_rt.WriteLine("c_5465", var34[0]);
            s_rt.WriteLine("c_5466", var37);
            s_rt.WriteLine("c_5468", var25.F0);
            s_rt.WriteLine("c_5470", var25.F2);
            s_rt.WriteLine("c_5471", var25.F3);
            s_rt.WriteLine("c_5472", var25.F4);
            s_rt.WriteLine("c_5473", var25.F5);
            s_rt.WriteLine("c_5478", var26.F2);
            s_rt.WriteLine("c_5479", var26.F3);
            s_rt.WriteLine("c_5480", var26.F4);
            s_rt.WriteLine("c_5481", var26.F5);
            s_rt.WriteLine("c_5482", var26.F6);
            s_rt.WriteLine("c_5483", var26.F7);
            s_rt.WriteLine("c_5486", var32.F2);
            s_rt.WriteLine("c_5488", var32.F4);
            var vr49 = new S1(0, 0, new S0(new C0(0, 0, 0), true, new C0(0, 0, -3.4028235E+38f), 0), -3.4028235E+38f);
            for (int var39 = 0; var39 < -1; var39++)
            {
                var vr9 = M143();
                var vr29 = M144();
                if (M140(vr9, vr29, 0, ref s_7[0][0]))
                {
                    var vr27 = s_120.F2;
                    var vr36 = s_59[0];
                    S0 vr57 = vr27;
                    S0 vr58 = s_127;
                    S1 vr59 = vr36;
                    S0 vr63 = default(S0);
                    var vr37 = s_59[0];
                    var vr14 = M142(vr63, ref s_101, new S1(-3.4028235E+38f, 0, new S0(new C0(0, 0, 0), false, new C0(0, 0, 0), 0), 0), vr37, ref s_85, ref s_68.F2.F1);
                    var vr43 = new S1(0, 0, new S0(new C0(0, 0, 0), false, new C0(0, 0, 0), 0), -3.4028235E+38f);
                    M142(vr14, ref s_127, vr43, new S1(0, 0, new S0(new C0(0, 0, 0), false, new C0(0, 0, 0), 1), 3.4028235E+38f), ref s_107, ref s_143);
                }
            }
        }

        private static bool M141(C1 argThis)
        {
            return default(bool);
        }
    }

    public interface IRuntime
    {
        void WriteLine<T>(string site, T value);
    }

    public class Runtime : IRuntime
    {
        public void WriteLine<T>(string site, T value) => System.Console.WriteLine(value);
    }

    public class CollectibleALC : System.Runtime.Loader.AssemblyLoadContext
    {
        public CollectibleALC() : base(true)
        {
        }
    }
}