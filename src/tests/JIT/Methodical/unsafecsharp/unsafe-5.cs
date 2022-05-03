// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using UnsafeCSharp;
using Xunit;

namespace Test_unsafe_5
{
public class TestApp
{
    private static unsafe long test_5(B b)
    {
        return (&b)->m_bval;
    }
    private static unsafe long test_12()
    {
        fixed (B* pb = &AA.s_x.m_b)
        {
            return pb->m_bval;
        }
    }
    private static unsafe long test_19(B* pb, long i)
    {
        return (&pb[i * 2])->m_bval;
    }
    private static unsafe long test_26(B* pb1, B* pb2)
    {
        return (pb1 >= pb2 ? pb1 : null)->m_bval;
    }
    private static unsafe long test_33(long pb)
    {
        return ((B*)pb)->m_bval;
    }
    private static unsafe long test_40(B* pb)
    {
        return pb[0].m_bval;
    }
    private static unsafe long test_47(B[] ab, long i)
    {
        fixed (B* pb = &ab[i])
        {
            return pb[0].m_bval;
        }
    }
    private static unsafe long test_54(B* pb)
    {
        return (pb += 6)[0].m_bval;
    }
    private static unsafe long test_61(B* pb, long[,,] i, long ii)
    {
        return (&pb[++i[--ii, 0, 0]])[0].m_bval;
    }
    private static unsafe long test_68(AA* px)
    {
        return ((B*)AA.get_pb_i(px))[0].m_bval;
    }
    private static unsafe long test_75(byte diff, A* pa)
    {
        return ((B*)(((byte*)pa) + diff))[0].m_bval;
    }
    private static unsafe long test_82()
    {
        AA loc_x = new AA(0, 100);
        return AA.get_bv1((&loc_x.m_b));
    }
    private static unsafe long test_89(B[][] ab, long i, long j)
    {
        fixed (B* pb = &ab[i][j])
        {
            return AA.get_bv1(pb);
        }
    }
    private static unsafe long test_96(B* pb1, long i)
    {
        B* pb;
        return AA.get_bv1((pb = (B*)(((byte*)pb1) + i * sizeof(B))));
    }
    private static unsafe long test_103(B* pb, long[,,] i, long ii, byte jj)
    {
        return AA.get_bv1((&pb[i[ii - jj, 0, ii - jj] = ii - 1]));
    }
    private static unsafe long test_110(ulong ub, byte lb)
    {
        return AA.get_bv1(((B*)(ub | lb)));
    }
    private static unsafe long test_117(long p, long s)
    {
        return AA.get_bv1(((B*)((p >> 4) | s)));
    }
    private static unsafe long test_124(B[] ab)
    {
        fixed (B* pb = &ab[0])
        {
            return AA.get_bv2(*pb);
        }
    }
    private static unsafe long test_131(B* pb)
    {
        return AA.get_bv2(*(++pb));
    }
    private static unsafe long test_138(B* pb, long[] i, long ii)
    {
        return AA.get_bv2(*(&pb[i[ii]]));
    }
    private static unsafe long test_145(AA* px)
    {
        return AA.get_bv2(*(AA.get_pb_1(px) + 1));
    }
    private static unsafe long test_152(long pb)
    {
        return AA.get_bv2(*((B*)checked(((long)pb) + 1)));
    }
    private static unsafe long test_159(B* pb)
    {
        return AA.get_bv3(ref *(pb--));
    }
    private static unsafe long test_166(AA[,] ab, long i)
    {
        long j = 0;
        fixed (B* pb = &ab[--i, ++j].m_b)
        {
            return AA.get_bv3(ref *pb);
        }
    }
    private static unsafe long test_173(B* pb1, long i)
    {
        B* pb;
        return AA.get_bv3(ref *(pb = pb1 + i));
    }
    private static unsafe long test_180(B* pb1, B* pb2)
    {
        return AA.get_bv3(ref *(pb1 > pb2 ? pb2 : null));
    }
    private static unsafe long test_187(long pb)
    {
        return AA.get_bv3(ref *((B*)pb));
    }
    private static unsafe long test_194(double* pb, long i)
    {
        return AA.get_bv3(ref *((B*)(pb + i)));
    }
    private static unsafe long test_201(ref B b)
    {
        fixed (B* pb = &b)
        {
            return pb->m_bval == 100 ? 100 : 101;
        }
    }
    private static unsafe long test_208(B* pb)
    {
        return (--pb)->m_bval == 100 ? 100 : 101;
    }
    private static unsafe long test_215(B* pb, long i)
    {
        return (&pb[-(i << (int)i)])->m_bval == 100 ? 100 : 101;
    }
    private static unsafe long test_222(AA* px)
    {
        return AA.get_pb(px)->m_bval == 100 ? 100 : 101;
    }
    private static unsafe long test_229(long pb)
    {
        return ((B*)checked((long)pb))->m_bval == 100 ? 100 : 101;
    }
    private static unsafe long test_236(B* pb)
    {
        return AA.get_i1(&(pb++)->m_bval);
    }
    private static unsafe long test_243(B[,] ab, long i, long j)
    {
        fixed (B* pb = &ab[i, j])
        {
            return AA.get_i1(&pb->m_bval);
        }
    }
    private static unsafe long test_250(B* pb1)
    {
        B* pb;
        return AA.get_i1(&(pb = pb1 - 8)->m_bval);
    }
    private static unsafe long test_257(B* pb, B* pb1, B* pb2)
    {
        return AA.get_i1(&(pb = pb + (pb2 - pb1))->m_bval);
    }
    private static unsafe long test_264(B* pb1, bool trig)
    {
        fixed (B* pb = &AA.s_x.m_b)
        {
            return AA.get_i1(&(trig ? pb : pb1)->m_bval);
        }
    }
    private static unsafe long test_271(byte* pb)
    {
        return AA.get_i1(&((B*)(pb + 7))->m_bval);
    }
    private static unsafe long test_278(B b)
    {
        return AA.get_i2((&b)->m_bval);
    }
    private static unsafe long test_285()
    {
        fixed (B* pb = &AA.s_x.m_b)
        {
            return AA.get_i2(pb->m_bval);
        }
    }
    private static unsafe long test_292(B* pb, long i)
    {
        return AA.get_i2((&pb[i * 2])->m_bval);
    }
    private static unsafe long test_299(B* pb1, B* pb2)
    {
        return AA.get_i2((pb1 >= pb2 ? pb1 : null)->m_bval);
    }
    private static unsafe long test_306(long pb)
    {
        return AA.get_i2(((B*)pb)->m_bval);
    }
    private static unsafe long test_313(B* pb)
    {
        return AA.get_i3(ref pb->m_bval);
    }
    private static unsafe long test_320(B[] ab, long i)
    {
        fixed (B* pb = &ab[i])
        {
            return AA.get_i3(ref pb->m_bval);
        }
    }
    private static unsafe long test_327(B* pb)
    {
        return AA.get_i3(ref (pb += 6)->m_bval);
    }
    private static unsafe long test_334(B* pb, long[,,] i, long ii)
    {
        return AA.get_i3(ref (&pb[++i[--ii, 0, 0]])->m_bval);
    }
    private static unsafe long test_341(AA* px)
    {
        return AA.get_i3(ref ((B*)AA.get_pb_i(px))->m_bval);
    }
    private static unsafe long test_348(byte diff, A* pa)
    {
        return AA.get_i3(ref ((B*)(((byte*)pa) + diff))->m_bval);
    }
    private static unsafe long test_355()
    {
        AA loc_x = new AA(0, 100);
        return AA.get_bv1((&loc_x.m_b)) != 100 ? 99 : 100;
    }
    private static unsafe long test_362(B[][] ab, long i, long j)
    {
        fixed (B* pb = &ab[i][j])
        {
            return AA.get_bv1(pb) != 100 ? 99 : 100;
        }
    }
    private static unsafe long test_369(B* pb1, long i)
    {
        B* pb;
        return AA.get_bv1((pb = (B*)(((byte*)pb1) + i * sizeof(B)))) != 100 ? 99 : 100;
    }
    private static unsafe long test_376(B* pb, long[,,] i, long ii, byte jj)
    {
        return AA.get_bv1((&pb[i[ii - jj, 0, ii - jj] = ii - 1])) != 100 ? 99 : 100;
    }
    private static unsafe long test_383(ulong ub, byte lb)
    {
        return AA.get_bv1(((B*)(ub | lb))) != 100 ? 99 : 100;
    }
    private static unsafe long test_390(long p, long s)
    {
        return AA.get_bv1(((B*)((p >> 4) | s))) != 100 ? 99 : 100;
    }
    [Fact]
    public static unsafe int TestEntryPoint()
    {
        AA loc_x = new AA(0, 100);
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_5(loc_x.m_b) != 100)
        {
            Console.WriteLine("test_5() failed.");
            return 105;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_12() != 100)
        {
            Console.WriteLine("test_12() failed.");
            return 112;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_19(&loc_x.m_b - 2, 1) != 100)
        {
            Console.WriteLine("test_19() failed.");
            return 119;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_26(&loc_x.m_b, &loc_x.m_b) != 100)
        {
            Console.WriteLine("test_26() failed.");
            return 126;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_33((long)&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_33() failed.");
            return 133;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_40(&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_40() failed.");
            return 140;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_47(new B[] { new B(), new B(), loc_x.m_b }, 2) != 100)
        {
            Console.WriteLine("test_47() failed.");
            return 147;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_54(&loc_x.m_b - 6) != 100)
        {
            Console.WriteLine("test_54() failed.");
            return 154;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_61(&loc_x.m_b - 1, new long[,,] { { { 0 } }, { { 0 } } }, 2) != 100)
        {
            Console.WriteLine("test_61() failed.");
            return 161;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_68(&loc_x) != 100)
        {
            Console.WriteLine("test_68() failed.");
            return 168;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_75((byte)(((long)&loc_x.m_b) - ((long)&loc_x.m_a)), &loc_x.m_a) != 100)
        {
            Console.WriteLine("test_75() failed.");
            return 175;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_82() != 100)
        {
            Console.WriteLine("test_82() failed.");
            return 182;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_89(new B[][] { new B[] { new B(), new B() }, new B[] { new B(), loc_x.m_b } }, 1, 1) != 100)
        {
            Console.WriteLine("test_89() failed.");
            return 189;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_96(&loc_x.m_b - 8, 8) != 100)
        {
            Console.WriteLine("test_96() failed.");
            return 196;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_103(&loc_x.m_b - 1, new long[,,] { { { 0 } }, { { 0 } } }, 2, 2) != 100)
        {
            Console.WriteLine("test_103() failed.");
            return 203;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_110(((ulong)&loc_x.m_b) & (~(ulong)0xff), unchecked((byte)&loc_x.m_b)) != 100)
        {
            Console.WriteLine("test_110() failed.");
            return 210;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_117(((long)(&loc_x.m_b)) << 4, ((long)(&loc_x.m_b)) & 0xff000000) != 100)
        {
            Console.WriteLine("test_117() failed.");
            return 217;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_124(new B[] { loc_x.m_b }) != 100)
        {
            Console.WriteLine("test_124() failed.");
            return 224;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_131(&loc_x.m_b - 1) != 100)
        {
            Console.WriteLine("test_131() failed.");
            return 231;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_138(&loc_x.m_b - 1, new long[] { 0, 1 }, 1) != 100)
        {
            Console.WriteLine("test_138() failed.");
            return 238;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_145(&loc_x) != 100)
        {
            Console.WriteLine("test_145() failed.");
            return 245;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_152((long)(((long)&loc_x.m_b) - 1)) != 100)
        {
            Console.WriteLine("test_152() failed.");
            return 252;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_159(&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_159() failed.");
            return 259;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_166(new AA[,] { { new AA(), new AA() }, { new AA(), loc_x } }, 2) != 100)
        {
            Console.WriteLine("test_166() failed.");
            return 266;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_173(&loc_x.m_b - 8, 8) != 100)
        {
            Console.WriteLine("test_173() failed.");
            return 273;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_180(&loc_x.m_b + 1, &loc_x.m_b) != 100)
        {
            Console.WriteLine("test_180() failed.");
            return 280;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_187((long)&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_187() failed.");
            return 287;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_194(((double*)(&loc_x.m_b)) - 4, 4) != 100)
        {
            Console.WriteLine("test_194() failed.");
            return 294;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_201(ref loc_x.m_b) != 100)
        {
            Console.WriteLine("test_201() failed.");
            return 301;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_208(&loc_x.m_b + 1) != 100)
        {
            Console.WriteLine("test_208() failed.");
            return 308;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_215(&loc_x.m_b + 2, 1) != 100)
        {
            Console.WriteLine("test_215() failed.");
            return 315;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_222(&loc_x) != 100)
        {
            Console.WriteLine("test_222() failed.");
            return 322;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_229((long)(long)&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_229() failed.");
            return 329;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_236(&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_236() failed.");
            return 336;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_243(new B[,] { { new B(), new B() }, { new B(), loc_x.m_b } }, 1, 1) != 100)
        {
            Console.WriteLine("test_243() failed.");
            return 343;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_250(&loc_x.m_b + 8) != 100)
        {
            Console.WriteLine("test_250() failed.");
            return 350;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_257(&loc_x.m_b - 2, &loc_x.m_b - 1, &loc_x.m_b + 1) != 100)
        {
            Console.WriteLine("test_257() failed.");
            return 357;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_264(&loc_x.m_b, true) != 100)
        {
            Console.WriteLine("test_264() failed.");
            return 364;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_271(((byte*)(&loc_x.m_b)) - 7) != 100)
        {
            Console.WriteLine("test_271() failed.");
            return 371;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_278(loc_x.m_b) != 100)
        {
            Console.WriteLine("test_278() failed.");
            return 378;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_285() != 100)
        {
            Console.WriteLine("test_285() failed.");
            return 385;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_292(&loc_x.m_b - 2, 1) != 100)
        {
            Console.WriteLine("test_292() failed.");
            return 392;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_299(&loc_x.m_b, &loc_x.m_b) != 100)
        {
            Console.WriteLine("test_299() failed.");
            return 399;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_306((long)&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_306() failed.");
            return 406;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_313(&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_313() failed.");
            return 413;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_320(new B[] { new B(), new B(), loc_x.m_b }, 2) != 100)
        {
            Console.WriteLine("test_320() failed.");
            return 420;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_327(&loc_x.m_b - 6) != 100)
        {
            Console.WriteLine("test_327() failed.");
            return 427;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_334(&loc_x.m_b - 1, new long[,,] { { { 0 } }, { { 0 } } }, 2) != 100)
        {
            Console.WriteLine("test_334() failed.");
            return 434;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_341(&loc_x) != 100)
        {
            Console.WriteLine("test_341() failed.");
            return 441;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_348((byte)(((long)&loc_x.m_b) - ((long)&loc_x.m_a)), &loc_x.m_a) != 100)
        {
            Console.WriteLine("test_348() failed.");
            return 448;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_355() != 100)
        {
            Console.WriteLine("test_355() failed.");
            return 455;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_362(new B[][] { new B[] { new B(), new B() }, new B[] { new B(), loc_x.m_b } }, 1, 1) != 100)
        {
            Console.WriteLine("test_362() failed.");
            return 462;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_369(&loc_x.m_b - 8, 8) != 100)
        {
            Console.WriteLine("test_369() failed.");
            return 469;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_376(&loc_x.m_b - 1, new long[,,] { { { 0 } }, { { 0 } } }, 2, 2) != 100)
        {
            Console.WriteLine("test_376() failed.");
            return 476;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_383(((ulong)&loc_x.m_b) & (~(ulong)0xff), unchecked((byte)&loc_x.m_b)) != 100)
        {
            Console.WriteLine("test_383() failed.");
            return 483;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_390(((long)(&loc_x.m_b)) << 4, ((long)(&loc_x.m_b)) & 0xff000000) != 100)
        {
            Console.WriteLine("test_390() failed.");
            return 490;
        }
        Console.WriteLine("All tests passed.");
        return 100;
    }
}
}
