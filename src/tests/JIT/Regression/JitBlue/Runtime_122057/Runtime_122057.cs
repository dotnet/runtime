// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Reduced from a Fuzzlyn-generated repro. In .NET 8 FullOpts (DOTNET_TieredCompilation=0),
// the loop hoister moves a load of static field FuzzProgram.s_2 out of an outer loop,
// even though an in-loop store and a virtual call inside a nested inner loop modify
// the static. The hoisted load reads the stale pre-loop value and propagates
// incorrect state into subsequent Checksum calls.
//
// Root cause: optRecordLoopMemoryDependence drops memory dependencies whose update
// loop does not contain the consuming tree's loop, instead of walking up the
// update loop's parent chain to find one that does. As a result the consuming IND
// has no entry in NodeToLoopMemoryBlockMap and IsTreeLoopMemoryInvariant returns
// true vacuously.

using System;
using System.Runtime.CompilerServices;
using System.Reflection;
using Xunit;

namespace Fuzzlyn.ExecutionServer
{
    public interface IRuntime
    {
        void Checksum<T>(string id, T val) where T : unmanaged;
    }
}

public class FuzzProgram
{
    public static Fuzzlyn.ExecutionServer.IRuntime s_rt;
    public static long s_1 = 6060527230209052498L;
    public static ushort s_2 = (ushort)49710;
    public static short[,][] s_3 = new short[,][]
    {
        { new short[] { (short)-18834 }, new short[] { (short)-23590 }, new short[] { (short)3656 } },
        { new short[] { (short)-32768 }, new short[] { (short)-32768 }, new short[] { (short)17958 } },
        { new short[] { (short)20675 }, new short[] { (short)0 }, new short[] { (short)1 } },
        { new short[] { (short)-32767 }, new short[] { (short)18686 }, new short[] { (short)-10 } },
        { new short[] { (short)28549 }, new short[] { (short)1 }, new short[] { (short)-111 } }
    };
    public static int s_4 = -1184588522;
    public static uint s_5 = 3056253521U;
    public static uint s_6 = 836438251U;
    public static bool s_7 = true;
    public static uint s_8 = 3421304707U;
    public static ushort s_9 = (ushort)23128;
    public static sbyte s_10 = (sbyte)118;
    public static long s_11 = 0L;
    public static byte s_12 = (byte)60;
    public static uint[] s_13 = new uint[] { 1331423462U, 0U, 4107918217U, 375355549U, 0U, 0U, 199681325U, 595010536U, 0U, 0U };
    public static long s_14 = 1L;
    public static sbyte s_15 = (sbyte)126;
    public static long s_16 = 2L;
    public static short s_17 = (short)-4346;
    public static uint s_18 = 2174883429U;
    public static short s_19 = (short)26472;
    public static short s_20 = (short)25584;
    public static ulong s_21 = 1UL;

    public static void Problem(Fuzzlyn.ExecutionServer.IRuntime rt)
    {
        s_rt = rt;
        M0();
        s_rt.Checksum("c_120", s_1);
        s_rt.Checksum("c_121", s_2);
        s_rt.Checksum("c_122", s_3[0, 0][0]);
        s_rt.Checksum("c_123", s_4);
        s_rt.Checksum("c_124", s_5);
        s_rt.Checksum("c_125", s_6);
        s_rt.Checksum("c_126", s_7);
        s_rt.Checksum("c_127", s_8);
        s_rt.Checksum("c_128", s_9);
        s_rt.Checksum("c_129", s_10);
        s_rt.Checksum("c_130", s_11);
        s_rt.Checksum("c_131", s_12);
        s_rt.Checksum("c_132", s_13[0]);
        s_rt.Checksum("c_133", s_14);
        s_rt.Checksum("c_134", s_15);
        s_rt.Checksum("c_135", s_16);
        s_rt.Checksum("c_136", s_17);
        s_rt.Checksum("c_137", s_18);
        s_rt.Checksum("c_138", s_19);
        s_rt.Checksum("c_139", s_20);
        s_rt.Checksum("c_140", s_21);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void M0()
    {
        uint var0 = 2195370671U;
        long var1 = -1141517596473517804L;
        for (sbyte lvar2 = (sbyte)-126; lvar2 > (sbyte)-128; lvar2--)
        {
            {
                byte lvar3 = (byte)2;
                do
                {
                    s_1 = var1;
                    ref ushort var4 = ref s_2;
                    s_rt.Checksum("c_0", var4);
                }
                while (--lvar3 > (byte)0);
            }
            s_2 = (ushort)1;
            s_2 = s_2--;
            var1 = 1L;
            if (!M1())
            {
                ushort[] var5 = new ushort[] { 11370, 65535, 2275, 38065, 65534, 0, 58815, 56059, 0 };
                s_rt.Checksum("c_3", var5[0]);
            }
            var1 = 6037939224865508555L;
            if (!(true && ((short)10 != (short)(-(short)((short)32767 & s_1)))))
            {
                var1 = var1;
                var0 = (uint)(-1456655081 * lvar2);
            }
            s_rt.Checksum("c_4", lvar2);
        }
        s_3 = new short[,][]
        {
            { new short[] { 32226, 21711, 32767, 32766 }, new short[] { 0, -8198, -5475, -32767 }, new short[] { 17256 }, new short[] { 2, -32767, 415, -1 }, new short[] { 32767 }, new short[] { -13973, -19190, -2 }, new short[] { 26391, -10, 18752, 10548, -10 }, new short[] { 24460, 0, 32766, -2291 } }
        };
        {
            int lvar6 = 1;
            do
            {
                long var7 = var1;
                s_rt.Checksum("c_5", var7);
            }
            while (--lvar6 > -1);
        }
        s_1 = -var1;
        var0 = var0;
        {
            byte lvar8 = (byte)3;
            do
            {
                if (!M1())
                {
                    --var0;
                    var1 = var1;
                    {
                        sbyte var9 = (sbyte)-128;
                        var0 = var0;
                        ref int var10 = ref s_4;
                        var10 = var10++;
                        s_5 = s_5++;
                        var0 = +var0;
                        var10 = var10;
                        s_rt.Checksum("c_6", var9);
                        s_rt.Checksum("c_7", var10);
                    }
                }
                var0 = s_5;
            }
            while (--lvar8 > (byte)1);
        }
        {
            long var11 = var1++;
            s_rt.Checksum("c_8", var11);
        }
        M2(new bool[] { true, false, true, false, true, true, true, false, false });
        short var12 = (short)(-(short)M4(M2(new bool[] { false }), (uint)M4((uint)M4(false, var0, 2560909998872509564L, new long[] { 2318673134826903210L, 7391437350759032976L, 2L, 0L }, (sbyte)2) != 2147483646, s_5, var1, new long[] { -2L }, (sbyte)126), 9223372036854775806L, new long[] { 9223372036854775806L, 816024498526253435L, -9223372036854775807L, 5218626926773408866L, -8339074398150994798L, 7418162443457212333L, -9223372036854775807L }, (sbyte)(-M6())));
        for (uint lvar13 = 2U; lvar13 < 4U; lvar13++)
        {
            var1 = var1++;
            s_rt.Checksum("c_24", lvar13);
        }
        sbyte var14 = (sbyte)(s_1 ^ var0);
        var1 = var1;
        var12 = var12;
        s_3[0, 0][0] = var12;
        ushort var15 = s_2--;
        byte var16 = (byte)1;
        if (s_7)
        {
            var15 = var15;
            M1();
            ulong var17 = 8281430269624226394UL;
            s_rt.Checksum("c_25", var17);
        }
        var0 = var0;
        var1 = var1--;
        M2(new bool[] { false }) = false;
        long var18 = var1++;
        ulong var19 = (ulong)M6();
        var12 = (short)M6();
        {
            ushort lvar20 = (ushort)3;
            do
            {
                return;
            }
            while (--lvar20 > (ushort)1);
        }
        var14 = (sbyte)0;
        var19 = var19;
        M2(new bool[] { false, true, true, false, true, false, true, true }) = false;
        M2(new bool[] { true, false, true }) = true;
        var12 = s_3[0, 0][0]++;
        byte var21 = var16;
        var15 = var15;
        ulong var22 = ~var19;
        short[,] var23 = new short[,]
        {
            { (short)0, (short)16101, (short)1, (short)-32768 },
            { (short)-1417, (short)1, (short)-26255, (short)0 },
            { (short)5594, (short)-32768, (short)14494, (short)-29697 }
        };
        for (uint lvar24 = 3U; lvar24 > 1U; lvar24--)
        {
            M2(new bool[] { false, true, true, true, false });
            var0 = lvar24;
            s_1 = M7(new long[] { -9223372036854775808L, -9223372036854775807L, 0L, -2L, -9223372036854775808L, 588138993331135433L, -9118575677176615176L, -9223372036854775807L }, ref var18, ref s_4, new bool[] { false, true, false, true, true, true, false, false, false, true });
            s_rt.Checksum("c_108", lvar24);
        }
        s_rt.Checksum("c_109", var0);
        s_rt.Checksum("c_110", var1);
        s_rt.Checksum("c_111", var12);
        s_rt.Checksum("c_112", var14);
        s_rt.Checksum("c_113", var15);
        s_rt.Checksum("c_114", var16);
        s_rt.Checksum("c_115", var18);
        s_rt.Checksum("c_116", var19);
        s_rt.Checksum("c_117", var21);
        s_rt.Checksum("c_118", var22);
        s_rt.Checksum("c_119", var23[0, 0]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool M1()
    {
        long[] var0 = new long[] { -9223372036854775808L, 3469938504716738437L, -6270270318732794470L, -9223372036854775808L, 4385904196239237818L, 2922539563606063291L, 0L, -9223372036854775807L, -9223372036854775808L, 7953216457234502451L };
        var0 = new long[] { 6628726885251607807L, -1L, 1L };
        var0 = var0;
        for (uint lvar1 = 0U; lvar1 < 2U; lvar1++)
        {
            s_2 |= s_2--;
            var0[0] = var0[0];
            s_rt.Checksum("c_1", lvar1);
        }
        s_rt.Checksum("c_2", var0[0]);
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref bool M2(bool[] arg0)
    {
        {
            ulong lvar0 = 3UL;
            do { arg0[0] = false; } while (--lvar0 > 1UL);
        }
        arg0 = arg0;
        M3(s_1--, M3((long)(2147483646 >> (sbyte)((ushort)41640 >> M4(arg0[0], s_5, 1L, new long[] { 4454878612178823527L, -8832934714181572528L, 9223372036854775806L, -8119477730200691901L, 1L }, (sbyte)((byte)115 * (byte)(3761338739U >> (sbyte)M4(arg0[0], (uint)(-s_6), 0L, new long[] { -8619694286625511954L, 0L, 0L, 4015693031674954220L }, (sbyte)M4(arg0[0], 1U, s_1, new long[] { 1L, -3066604731599981898L, 3190787509676734497L, 4434028797499849018L, 8102971306148309269L, 0L, 9223372036854775807L, -2047607379922883529L }, (sbyte)0))))))), new sbyte[] { (sbyte)126, (sbyte)-128 }));
        s_rt.Checksum("c_22", arg0[0]);
        return ref arg0[0];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static sbyte[] M3(long arg0, sbyte[] arg1)
    {
        s_6 = s_5--;
        s_rt.Checksum("c_9", arg0);
        s_rt.Checksum("c_10", arg1[0]);
        return arg1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte M4(bool arg0, uint arg1, long arg2, long[] arg3, sbyte arg4)
    {
        s_4 = s_4;
        for (short lvar0 = (short)10; lvar0 < (short)12; lvar0++)
        {
            arg0 = arg0;
            s_6 = 4147567982U;
            {
                M5(s_3[0, 0][0]++, (byte)68, new ushort[] { 1, 65535, 12315, 2, 1, 26928, 0, 10158, 65534 }, arg0, ref s_2);
                s_3[0, 0] = new short[] { 32767, -4137 };
            }
            s_8 = arg1;
            s_rt.Checksum("c_16", lvar0);
        }
        M5((short)-32768, (byte)0, new ushort[] { 65535, 65534, 5920, 1, 54744, 38885, 65534 }, arg0, ref s_2);
        s_rt.Checksum("c_17", arg0);
        s_rt.Checksum("c_18", arg1);
        s_rt.Checksum("c_19", arg2);
        s_rt.Checksum("c_20", arg3[0]);
        s_rt.Checksum("c_21", arg4);
        return (byte)((ushort)(-s_2++) + (short)-32767);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] M5(short arg0, byte arg1, ushort[] arg2, bool arg3, ref ushort arg4)
    {
        s_7 = s_7;
        s_rt.Checksum("c_11", arg0);
        s_rt.Checksum("c_12", arg1);
        s_rt.Checksum("c_13", arg2[0]);
        s_rt.Checksum("c_14", arg3);
        s_rt.Checksum("c_15", arg4);
        return new byte[] { 238, 101, 1, 254 };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static sbyte M6()
    {
        ulong[] var0 = new ulong[] { 9075795339016271961UL, 6479605195186909271UL, 13774599958060018693UL, 8476875736229445483UL, 3224408522871403950UL };
        s_rt.Checksum("c_23", var0[0]);
        return (sbyte)10;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long M7(long[] arg0, ref long arg1, ref int arg2, bool[] arg3)
    {
        for (ulong lvar0 = 1UL; lvar0 < 3UL; lvar0++)
        {
            arg0 = new long[] { -823176126130731037L, -8295235001710223202L, -5162834207979005497L, 3089335335388388062L, 594988239696464375L };
            arg3[0] = arg3[0];
            s_rt.Checksum("c_26", lvar0);
        }
        for (sbyte lvar1 = (sbyte)-125; lvar1 > (sbyte)-127; lvar1--)
        {
            if (arg3[0])
            {
                M8(new sbyte[,] { { -23, -63, 58, 126, -19, 1, 71, 87, -76 }, { 126, 91, 81, -127, 26, -127, -8, 32, -14 }, { -100, -2, -126, 0, -41, -62, -14, 0, 67 }, { -59, 1, -34, 0, 10, 70, 77, 0, -23 }, { -2, -12, -2, 1, 1, 50, -128, 2, -128 }, { 1, 35, -13, 1, -95, 25, 40, 11, 18 } }, new uint[] { 0U, 739524019U, 4294967294U, 4294967295U, 1U, 3826817596U, 1958436705U }, 4294967294U);
                int[] var2 = new int[] { 2147483646 };
                s_rt.Checksum("c_32", var2[0]);
            }
            short var3 = (short)M8(M9(ref s_21), new uint[] { 0U }, 4294967295U);
            s_rt.Checksum("c_102", lvar1);
            s_rt.Checksum("c_103", var3);
        }
        s_rt.Checksum("c_104", arg0[0]);
        s_rt.Checksum("c_105", arg1);
        s_rt.Checksum("c_106", arg2);
        s_rt.Checksum("c_107", arg3[0]);
        return (long)M15();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte M8(sbyte[,] arg0, uint[] arg1, uint arg2)
    {
        arg1[0]++;
        arg1[0] = (uint)(arg0[0, 0] >> (byte)147);
        short[] var0 = s_3[0, 0];
        {
            short lvar1 = (short)1;
            do
            {
                arg2 = (uint)(arg0[0, 0] & arg1[0]);
                ushort var2 = (ushort)41560;
                s_rt.Checksum("c_27", var2);
            }
            while (++lvar1 < (short)3);
        }
        s_rt.Checksum("c_28", arg0[0, 0]);
        s_rt.Checksum("c_29", arg1[0]);
        s_rt.Checksum("c_30", arg2);
        s_rt.Checksum("c_31", var0[0]);
        return (byte)(-(byte)(arg2 | s_1--));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static sbyte[,] M9(ref ulong arg0)
    {
        arg0 = arg0;
        M10((short)(-9223372036854775807L | s_12--));
        arg0 = ~arg0;
        s_rt.Checksum("c_101", arg0);
        return new sbyte[,] {
            { 49, 127, 67, 2, 96, -30 },
            { -7, 0, 49, 126, -127, -1 },
            { 0, 126, 10, -127, 1, 127 },
            { 49, 74, 0, 26, -21, 49 },
            { -128, 10, -80, -62, -99, 76 },
            { 127, -1, 127, -64, -73, 119 },
            { -80, -49, -54, 127, 60, -30 },
            { 1, -55, 31, -10, -22, 1 },
            { 1, -128, -120, 1, 1, -10 },
            { -2, -127, -127, -1, -107, -127 }
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte M10(short arg0)
    {
        for (int lvar0 = 1; lvar0 < 3; lvar0++)
        {
            arg0 = arg0;
            s_9 = s_2--;
            byte[] var1 = new byte[] { 254, 254, 13 };
            var1[0] = (byte)(var1[0] >> (ushort)0);
            ref int var2 = ref s_4;
            s_rt.Checksum("c_33", lvar0);
            s_rt.Checksum("c_34", var1[0]);
            s_rt.Checksum("c_35", var2);
        }
        s_3 = new short[,][]
        {
            { new short[] { -32767 }, new short[] { -7067, 1, -12959, -32768, 32766 } },
            { new short[] { 3671, -26246, 31422 }, new short[] { -26940, 32766, 148, -5951, -30144 } },
            { new short[] { -6519, -9579, 32766, 22051, -1, -9788 }, new short[] { 20521, 17609, 32767, 32767 } }
        };
        arg0 = (short)((short)(-(short)(-arg0)) - arg0);
        for (uint lvar3 = 2U; lvar3 > 0U; lvar3--)
        {
            arg0 = arg0;
            int var4 = -s_4--;
            arg0 = (short)-23432;
            ulong var5 = 6834069365286559364UL;
            long var6 = 9223372036854775807L;
            s_rt.Checksum("c_36", lvar3);
            s_rt.Checksum("c_37", var4);
            s_rt.Checksum("c_38", var5);
            s_rt.Checksum("c_39", var6);
        }
        ushort var7 = s_2--;
        if (arg0 < -9223372036854775807L)
        {
            int var8 = -s_4;
            s_rt.Checksum("c_40", var8);
        }
        else
        {
            for (int lvar9 = 2147483644; lvar9 > 2147483642; lvar9--)
            {
                arg0 = (short)-32768;
                uint var10 = s_6;
                if (!!s_7)
                {
                    ulong var11 = M11();
                    s_rt.Checksum("c_77", var11);
                }
                else
                {
                    ulong var12 = 10343924371845177017UL;
                    uint[] var13 = s_13;
                    s_rt.Checksum("c_78", var12);
                    s_rt.Checksum("c_79", var13[0]);
                }
                s_16 = s_11--;
                s_10 = (sbyte)7;
                var7 = s_2;
                var7 = (ushort)1;
                s_6 = var10;
                M15();
                sbyte var14 = (sbyte)M11();
                s_rt.Checksum("c_80", lvar9);
                s_rt.Checksum("c_81", var10);
                s_rt.Checksum("c_82", var14);
            }
            var7 = (ushort)15780;
            s_17 = arg0;
            sbyte[] var15 = new sbyte[] { -127, -128, 1, -128, 119, 0, 0, 0, 0, 1 };
            {
                short lvar16 = (short)-2;
                do
                {
                    if (s_7)
                    {
                        s_15 = var15[0];
                        s_18 = s_5++;
                    }
                    for (long lvar17 = 2L; lvar17 > 0L; lvar17--)
                    {
                        s_5 = (uint)M15();
                        s_rt.Checksum("c_83", lvar17);
                    }
                    ref sbyte var18 = ref var15[0];
                    var7 = var7;
                    ushort[] var19 = new ushort[] { 0, 0, 2, 1 };
                    s_rt.Checksum("c_84", var18);
                    s_rt.Checksum("c_85", var19[0]);
                }
                while (++lvar16 < (short)0);
            }
            for (ulong lvar20 = 12UL; lvar20 > 10UL; lvar20--)
            {
                bool var21 = s_7;
                {
                    ulong lvar22 = 1UL;
                    do
                    {
                        var21 &= true;
                        var15[0] = var15[0];
                        s_1 = s_14;
                    }
                    while (++lvar22 < 3UL);
                }
                s_18 = (uint)(s_4 % (uint)(4294967294U | 1));
                s_19 = arg0++;
                s_rt.Checksum("c_86", lvar20);
                s_rt.Checksum("c_87", var21);
            }
            var7 = (ushort)M14();
            for (int lvar23 = 2147483644; lvar23 > 2147483642; lvar23--)
            {
                ref byte var24 = ref s_12;
                var15 = M16();
                s_rt.Checksum("c_89", lvar23);
                s_rt.Checksum("c_90", var24);
            }
            int[] var25 = new int[] { -2147483648, -1034170406, -2, -2098588323 };
            M17();
            var25 = new int[] { -83115896, 394999409, -1 };
            var15 = M16();
            ref short var26 = ref s_19;
            s_rt.Checksum("c_92", var15[0]);
            s_rt.Checksum("c_93", var25[0]);
            s_rt.Checksum("c_94", var26);
        }
        s_7 = false;
        short[,,][] var27 = new short[,,][]
        {
            {
                { new short[] { -15514, 1, -20635, -20050 } }, { new short[] { 0 } }, { new short[] { -29554 } }, { new short[] { -14424, 22384, 1 } }, { new short[] { -10, 32766, -10605, 1, 25245 } }, { new short[] { 19408, -27980, -32768 } }, { new short[] { 17637, 32766, 5671, -1437 } }, { new short[] { 1, 0 } }, { new short[] { -1, -32767, -1, 0 } }, { new short[] { -14015, -30033, 0, -30681, -29255 } }
            },
            {
                { new short[] { -26705, 32767, -3809, 0, -29073, -32768 } }, { new short[] { 1, -2, -14511, 32093 } }, { new short[] { -32768, 32767 } }, { new short[] { -32768, 32767, -32767, -5065 } }, { new short[] { 7318 } }, { new short[] { 15611, 32767, 1, 19983 } }, { new short[] { 16771, 1, -2914, -32768, 1, -10 } }, { new short[] { -10, -32768, 3793, -12690, -2 } }, { new short[] { 28741, 28267, 32767, 2, 10, -23047 } }, { new short[] { 10 } }
            },
            {
                { new short[] { -31350, -8186 } }, { new short[] { 32767, 1498, 7166 } }, { new short[] { -20496, 12926, -32767, -5105, 9635, 32766 } }, { new short[] { 0 } }, { new short[] { -27636, -29621 } }, { new short[] { 10, 0, -32768 } }, { new short[] { 30662, 32766, 18935, 1, 10 } }, { new short[] { 1, 0, -18281, 27965 } }, { new short[] { -2, 9044, 32766, 32767, -9241, -31719 } }, { new short[] { -12795, -7848, -10, -32767, -32768 } }
            },
            {
                { new short[] { 23667, -32767, -10167, -19920, -2870 } }, { new short[] { 10, 0, 1, 32767 } }, { new short[] { -24920, -1, -32414, 1 } }, { new short[] { -18121, -32768, 2, 32766, 0 } }, { new short[] { -2, 27200, 1, 1 } }, { new short[] { 28876, -32767, 32767, 0 } }, { new short[] { -27892 } }, { new short[] { 1, -19993, -32767, -16, 25191 } }, { new short[] { 16992, 1 } }, { new short[] { 0, 13941 } }
            },
            {
                { new short[] { 29898, -30552, 1, -26259, 1, -1793 } }, { new short[] { -24831, 17290, 17017, -31412, 0, 1 } }, { new short[] { 10, -13451, -7020 } }, { new short[] { -26827, -24003, 0, -31949, -17681, 25861 } }, { new short[] { -32767, -32768, 10, -32767, -10 } }, { new short[] { 32767, -18397 } }, { new short[] { 10292, -32767, -30023, 32767 } }, { new short[] { -9044, 0, 0, 1, -32767, 32767 } }, { new short[] { -10, 29286, 32766 } }, { new short[] { -20806, 7157 } }
            }
        };
        var27[0, 0, 0] = new short[] { 0, -32767, 1, -32768, -15412, 28056, 15578 };
        var7 = (ushort)M15();
        for (ushort lvar28 = (ushort)3; lvar28 > (ushort)1; lvar28--)
        {
            arg0 = (short)32767;
            sbyte var29 = s_10++;
            var27[0, 0, 0][0] = arg0;
            s_rt.Checksum("c_95", lvar28);
            s_rt.Checksum("c_96", var29);
        }
        var27 = var27;
        ulong var30 = 18446744073709551615UL;
        var27 = var27;
        M11();
        var30 = ~var30;
        s_rt.Checksum("c_97", arg0);
        s_rt.Checksum("c_98", var7);
        s_rt.Checksum("c_99", var27[0, 0, 0][0]);
        s_rt.Checksum("c_100", var30);
        return (byte)0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong M11()
    {
        bool var0 = true;
        var0 = var0;
        {
            var0 = false;
            var0 &= var0;
            ref int var1 = ref s_4;
            var1 = var1;
            sbyte var2 = (sbyte)((byte)254 * s_1);
            var0 = var0;
            ushort var3 = (ushort)1;
            for (byte lvar4 = (byte)0; lvar4 < (byte)2; lvar4++)
            {
                bool var5 = false;
                {
                    int lvar6 = 2;
                    do { s_10 = var2; s_11 = s_11++; var1 = var1; } while (++lvar6 < 4);
                }
                var0 = s_7;
                uint[] var7 = new uint[] { 1532710886U };
                s_rt.Checksum("c_41", lvar4);
                s_rt.Checksum("c_42", var5);
                s_rt.Checksum("c_43", var7[0]);
            }
            var2 = s_10;
            s_1 = 0L;
            byte var8 = (byte)0;
            var3 = (ushort)60731;
            s_rt.Checksum("c_44", var1);
            s_rt.Checksum("c_45", var2);
            s_rt.Checksum("c_46", var3);
            s_rt.Checksum("c_47", var8);
        }
        if (var0)
        {
            ushort var9 = (ushort)2169;
            s_rt.Checksum("c_48", var9);
        }
        else
        {
            s_5 = (uint)(-s_6);
            long var10 = s_1--;
            byte var11 = (byte)255;
            var11 &= (byte)69;
            var10 = var10;
            s_12 = var11--;
            var11 = (byte)254;
            for (int lvar12 = 2147483642; lvar12 < 2147483644; lvar12++)
            {
                for (int lvar13 = 0; lvar13 < 2; lvar13++)
                {
                    s_6 = 3053388054U;
                    s_10 = s_10;
                    M12(ref s_1, var10);
                    s_rt.Checksum("c_52", lvar13);
                }
                bool[][][][] var14 = new bool[][][][]
                {
                    new bool[][][] { new bool[][] { new bool[] { true }, new bool[] { true, true }, new bool[] { false, false }, new bool[] { true }, new bool[] { false } } },
                    new bool[][][] { new bool[][] { new bool[] { false, false }, new bool[] { true, false }, new bool[] { false, false }, new bool[] { false, true }, new bool[] { true, true }, new bool[] { true, true } }, new bool[][] { new bool[] { true, true }, new bool[] { false }, new bool[] { true }, new bool[] { false }, new bool[] { true } } },
                    new bool[][][] { new bool[][] { new bool[] { true, true }, new bool[] { true }, new bool[] { false, false }, new bool[] { false, true }, new bool[] { true, false } } },
                    new bool[][][] { new bool[][] { new bool[] { true, true }, new bool[] { false, true }, new bool[] { false, false }, new bool[] { false, true }, new bool[] { true, true }, new bool[] { true, false } } },
                    new bool[][][] { new bool[][] { new bool[] { false }, new bool[] { true }, new bool[] { false, true }, new bool[] { true, true } }, new bool[][] { new bool[] { false, false }, new bool[] { false, false }, new bool[] { true }, new bool[] { false, true } } },
                    new bool[][][] { new bool[][] { new bool[] { false, true }, new bool[] { true }, new bool[] { true } } },
                    new bool[][][] { new bool[][] { new bool[] { true, false }, new bool[] { false }, new bool[] { true }, new bool[] { false }, new bool[] { false }, new bool[] { false } }, new bool[][] { new bool[] { true, true }, new bool[] { false } } },
                    new bool[][][] { new bool[][] { new bool[] { true, false }, new bool[] { true, false }, new bool[] { false }, new bool[] { false, false }, new bool[] { true }, new bool[] { true, true } } },
                    new bool[][][] { new bool[][] { new bool[] { true }, new bool[] { false, true } } }
                };
                s_rt.Checksum("c_53", lvar12);
                s_rt.Checksum("c_54", var14[0][0][0][0]);
            }
            s_3[0, 0] = new short[] { 26467, 32766, -22566, 29068, 32766, -23929, 0, -26929 };
            s_14 = var10;
            var0 = s_7;
            int var15 = -10267422;
            for (int lvar16 = 2147483642; lvar16 < 2147483644; lvar16++)
            {
                uint var17 = 4294967294U;
                short[][] var18 = new short[][] { new short[] { -32768, 1 }, new short[] { -21505, -32768 }, new short[] { -10 }, new short[] { 4830 }, new short[] { 28129 }, new short[] { 27172, 1 }, new short[] { 0, -32767 }, new short[] { -10, 1 } };
                var18 = var18;
                for (uint lvar19 = 2U; lvar19 > 0U; lvar19--)
                {
                    sbyte[,] var20 = new sbyte[,] { { 0, -127, 75 }, { 1, 1, 40 }, { -128, -48, 111 }, { -8, 0, -110 }, { 0, -2, -63 } };
                    s_15 = (sbyte)((byte)254 / (int)(s_4 | 1));
                    s_rt.Checksum("c_55", lvar19);
                    s_rt.Checksum("c_56", var20[0, 0]);
                }
                ulong var21 = 1UL + s_2;
                var18 = var18;
                M13(ref s_9, M15() > (byte)1, (sbyte)((ushort)(-s_9++) << var18[0][0]), ref s_12);
                var18[0][0] = var18[0][0];
                s_rt.Checksum("c_68", lvar16);
                s_rt.Checksum("c_69", var17);
                s_rt.Checksum("c_70", var18[0][0]);
                s_rt.Checksum("c_71", var21);
            }
            sbyte var22 = (sbyte)(-(sbyte)M14());
            s_rt.Checksum("c_72", var10);
            s_rt.Checksum("c_73", var11);
            s_rt.Checksum("c_74", var15);
            s_rt.Checksum("c_75", var22);
        }
        s_4 = s_4++;
        s_rt.Checksum("c_76", var0);
        return (ulong)(-274716830 ^ (uint)(-(uint)M14()));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static short[][][] M12(ref long arg0, long arg1)
    {
        arg1 = arg0;
        s_13 = new uint[] { 458039818U, 3302160416U, 0U, 0U, 1138981359U, 3838865804U, 1420777593U, 4294967295U, 656122219U };
        arg1 = arg1;
        arg1 &= -arg0;
        uint[] var0 = new uint[] { 1U, 4294967295U, 0U, 3397223501U };
        s_rt.Checksum("c_49", arg0);
        s_rt.Checksum("c_50", arg1);
        s_rt.Checksum("c_51", var0[0]);
        return new short[][][]
        {
            new short[][] { new short[] { 32767, 8982 }, new short[] { -22964, 24040, -25168 }, new short[] { 1, 32766 }, new short[] { -32467, -2, 2990 } },
            new short[][] { new short[] { -14377 }, new short[] { 0, -27818 }, new short[] { 1 }, new short[] { 32766 }, new short[] { -16288, 0, 13636, -24442 } },
            new short[][] { new short[] { 0 }, new short[] { 2104, 29149, -32768 }, new short[] { 6098, -16471, 0 }, new short[] { 10 }, new short[] { -32768, 25626, 5292 }, new short[] { 32766, -3494, 0 }, new short[] { 32767, 1, 0, 1 } },
            new short[][] { new short[] { 1 }, new short[] { -24409, 22251 }, new short[] { 5744, 12622, -32768, -32767 }, new short[] { 0, 2, 2, 1 }, new short[] { 25499, 3114 }, new short[] { -11783, -15902 }, new short[] { -2, 32767 } },
            new short[][] { new short[] { 1462, -5726, 25527, 1618 }, new short[] { 30102, 1, 5029 }, new short[] { -11443 }, new short[] { 9205, 15860 }, new short[] { 7839, 0, 1, -32768 } },
            new short[][] { new short[] { -25816, -7250 }, new short[] { -16408, 10 } },
            new short[][] { new short[] { 1, 10828 } }
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static short M13(ref ushort arg0, bool arg1, sbyte arg2, ref byte arg3)
    {
        arg3 = arg3;
        ushort[] var0 = new ushort[] { 1, 2, 17505, 0, 2, 0, 1, 65534, 0, 0 };
        for (uint lvar1 = 0U; lvar1 < 2U; lvar1++)
        {
            arg1 = false;
            for (ulong lvar2 = 0UL; lvar2 < 2UL; lvar2++)
            {
                long[] var3 = new long[] { 2663589252698800025L, -5662605789936286836L, -7446348205089506664L, -1L, 1L };
                ushort var4 = s_9;
                M14();
                var4 /= (ushort)((ushort)M14() | 1);
                arg3 = ref s_12;
                s_rt.Checksum("c_57", lvar2);
                s_rt.Checksum("c_58", var3[0]);
                s_rt.Checksum("c_59", var4);
            }
            arg1 = arg1;
            byte var5 = (byte)((ulong)M14() + s_12);
            if (!arg1)
            {
                ref long var6 = ref s_1;
                s_rt.Checksum("c_60", var6);
            }
            s_rt.Checksum("c_61", lvar1);
            s_rt.Checksum("c_62", var5);
        }
        s_rt.Checksum("c_63", arg0);
        s_rt.Checksum("c_64", arg1);
        s_rt.Checksum("c_65", arg2);
        s_rt.Checksum("c_66", arg3);
        s_rt.Checksum("c_67", var0[0]);
        return (short)M14();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int M14()
    {
        s_3[0, 0] = s_3[0, 0];
        return -10;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong M15()
    {
        s_3[0, 0] = new short[] { -19860, 18732, 0, 19685, 22080, 32766, -26891, 24022 };
        return ~~((ulong)(s_14 | -1261992381) & (byte)0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static sbyte[] M16()
    {
        uint var0 = s_13[0];
        s_rt.Checksum("c_88", var0);
        return new sbyte[] { -20, 91, 10, 1, -128, -68, -127, -115, 126, -2 };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte M17()
    {
        s_13 = s_13;
        {
            int lvar0 = 2;
            do { s_2 *= (ushort)65535; s_1 = -9223372036854775808L; M18(); M18(); } while (--lvar0 > 0);
        }
        return (byte)((byte)255 & (short)M18());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort M18()
    {
        s_17 = (short)(-s_19++);
        {
            s_3[0, 0] = s_3[0, 0];
            s_20--;
            s_4 = (int)((sbyte)-41 + (+s_5));
            {
                M19();
                s_14 = s_1;
                s_8 = 1U;
            }
        }
        return s_2--;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort[] M19()
    {
        short var0 = s_17++;
        s_rt.Checksum("c_91", var0);
        return new ushort[] { 65535, 53191, 1, 32811, 39233, 27002, 10, 1 };
    }
}

// FNV-1a checksum runtime implementation.
public class MyRuntime : Fuzzlyn.ExecutionServer.IRuntime
{
    private ulong _state = 14695981039346656037;

    public void Reset()
    {
        _state = 14695981039346656037;
    }

    public string FinishHashCode() => _state.ToString("X16");

    private void ChecksumBytes(ReadOnlySpan<byte> bytes)
    {
        for (int i = 0; i < bytes.Length; i++)
        {
            _state ^= bytes[i];
            _state *= 1099511628211;
        }
    }

    public void Checksum<T>(string id, T val) where T : unmanaged
    {
        unsafe
        {
            ChecksumBytes(new ReadOnlySpan<byte>(Unsafe.AsPointer(ref val), sizeof(T)));
        }
    }
}

public class Runtime_122057
{
    [Fact]
    public static int TestEntryPoint()
    {
        var runtime = new MyRuntime();
        ResetStatics();
        try { FuzzProgram.Problem(runtime); } catch { }
        const string expected = "AB1CED180818F52B";
        string actual = runtime.FinishHashCode();
        if (actual != expected)
        {
            Console.WriteLine($"FAIL: expected {expected} got {actual}");
            return 101;
        }
        return 100;
    }

    private static void ResetStatics()
    {
        var fields = typeof(FuzzProgram).GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        foreach (var field in fields)
        {
            if (field.FieldType == typeof(long)) field.SetValue(null, 0L);
            else if (field.FieldType == typeof(int)) field.SetValue(null, 0);
            else if (field.FieldType == typeof(uint)) field.SetValue(null, 0U);
            else if (field.FieldType == typeof(short)) field.SetValue(null, (short)0);
            else if (field.FieldType == typeof(ushort)) field.SetValue(null, (ushort)0);
            else if (field.FieldType == typeof(byte)) field.SetValue(null, (byte)0);
            else if (field.FieldType == typeof(sbyte)) field.SetValue(null, (sbyte)0);
            else if (field.FieldType == typeof(bool)) field.SetValue(null, false);
            else if (field.FieldType == typeof(ulong)) field.SetValue(null, 0UL);
        }
    }
}
