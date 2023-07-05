// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using UnsafeCSharp;
using Xunit;

namespace Test_unsafe_6
{
public class TestApp
{
    private static unsafe long test_6(ref B b)
    {
        fixed (B* pb = &b)
        {
            return pb->m_bval;
        }
    }
    private static unsafe long test_13(B* pb)
    {
        return (--pb)->m_bval;
    }
    private static unsafe long test_20(B* pb, long i)
    {
        return (&pb[-(i << (int)i)])->m_bval;
    }
    private static unsafe long test_27(AA* px)
    {
        return AA.get_pb(px)->m_bval;
    }
    private static unsafe long test_34(long pb)
    {
        return ((B*)checked((long)pb))->m_bval;
    }
    private static unsafe long test_41(B* pb)
    {
        return (pb++)[0].m_bval;
    }
    private static unsafe long test_48(B[,] ab, long i, long j)
    {
        fixed (B* pb = &ab[i, j])
        {
            return pb[0].m_bval;
        }
    }
    private static unsafe long test_55(B* pb1)
    {
        B* pb;
        return (pb = pb1 - 8)[0].m_bval;
    }
    private static unsafe long test_62(B* pb, B* pb1, B* pb2)
    {
        return (pb = pb + (pb2 - pb1))[0].m_bval;
    }
    private static unsafe long test_69(B* pb1, bool trig)
    {
        fixed (B* pb = &AA.s_x.m_b)
        {
            return (trig ? pb : pb1)[0].m_bval;
        }
    }
    private static unsafe long test_76(byte* pb)
    {
        return ((B*)(pb + 7))[0].m_bval;
    }
    private static unsafe long test_83(B b)
    {
        return AA.get_bv1((&b));
    }
    private static unsafe long test_90()
    {
        fixed (B* pb = &AA.s_x.m_b)
        {
            return AA.get_bv1(pb);
        }
    }
    private static unsafe long test_97(B* pb, long i)
    {
        return AA.get_bv1((&pb[i * 2]));
    }
    private static unsafe long test_104(B* pb1, B* pb2)
    {
        return AA.get_bv1((pb1 >= pb2 ? pb1 : null));
    }
    private static unsafe long test_111(long pb)
    {
        return AA.get_bv1(((B*)pb));
    }
    private static unsafe long test_118(B* pb)
    {
        return AA.get_bv2(*pb);
    }
    private static unsafe long test_125(B[] ab, long i)
    {
        fixed (B* pb = &ab[i])
        {
            return AA.get_bv2(*pb);
        }
    }
    private static unsafe long test_132(B* pb)
    {
        return AA.get_bv2(*(pb += 6));
    }
    private static unsafe long test_139(B* pb, long[,,] i, long ii)
    {
        return AA.get_bv2(*(&pb[++i[--ii, 0, 0]]));
    }
    private static unsafe long test_146(AA* px)
    {
        return AA.get_bv2(*((B*)AA.get_pb_i(px)));
    }
    private static unsafe long test_153(byte diff, A* pa)
    {
        return AA.get_bv2(*((B*)(((byte*)pa) + diff)));
    }
    private static unsafe long test_160()
    {
        AA loc_x = new AA(0, 100);
        return AA.get_bv3(ref *(&loc_x.m_b));
    }
    private static unsafe long test_167(B[][] ab, long i, long j)
    {
        fixed (B* pb = &ab[i][j])
        {
            return AA.get_bv3(ref *pb);
        }
    }
    private static unsafe long test_174(B* pb1, long i)
    {
        B* pb;
        return AA.get_bv3(ref *(pb = (B*)(((byte*)pb1) + i * sizeof(B))));
    }
    private static unsafe long test_181(B* pb, long[,,] i, long ii, byte jj)
    {
        return AA.get_bv3(ref *(&pb[i[ii - jj, 0, ii - jj] = ii - 1]));
    }
    private static unsafe long test_188(ulong ub, byte lb)
    {
        return AA.get_bv3(ref *((B*)(ub | lb)));
    }
    private static unsafe long test_195(long p, long s)
    {
        return AA.get_bv3(ref *((B*)((p >> 4) | s)));
    }
    private static unsafe long test_202(B[] ab)
    {
        fixed (B* pb = &ab[0])
        {
            return pb->m_bval == 100 ? 100 : 101;
        }
    }
    private static unsafe long test_209(B* pb)
    {
        return (++pb)->m_bval == 100 ? 100 : 101;
    }
    private static unsafe long test_216(B* pb, long[] i, long ii)
    {
        return (&pb[i[ii]])->m_bval == 100 ? 100 : 101;
    }
    private static unsafe long test_223(AA* px)
    {
        return (AA.get_pb_1(px) + 1)->m_bval == 100 ? 100 : 101;
    }
    private static unsafe long test_230(long pb)
    {
        return ((B*)checked(((long)pb) + 1))->m_bval == 100 ? 100 : 101;
    }
    private static unsafe long test_237(B* pb)
    {
        return AA.get_i1(&(pb--)->m_bval);
    }
    private static unsafe long test_244(AA[,] ab, long i)
    {
        long j = 0;
        fixed (B* pb = &ab[--i, ++j].m_b)
        {
            return AA.get_i1(&pb->m_bval);
        }
    }
    private static unsafe long test_251(B* pb1, long i)
    {
        B* pb;
        return AA.get_i1(&(pb = pb1 + i)->m_bval);
    }
    private static unsafe long test_258(B* pb1, B* pb2)
    {
        return AA.get_i1(&(pb1 > pb2 ? pb2 : null)->m_bval);
    }
    private static unsafe long test_265(long pb)
    {
        return AA.get_i1(&((B*)pb)->m_bval);
    }
    private static unsafe long test_272(double* pb, long i)
    {
        return AA.get_i1(&((B*)(pb + i))->m_bval);
    }
    private static unsafe long test_279(ref B b)
    {
        fixed (B* pb = &b)
        {
            return AA.get_i2(pb->m_bval);
        }
    }
    private static unsafe long test_286(B* pb)
    {
        return AA.get_i2((--pb)->m_bval);
    }
    private static unsafe long test_293(B* pb, long i)
    {
        return AA.get_i2((&pb[-(i << (int)i)])->m_bval);
    }
    private static unsafe long test_300(AA* px)
    {
        return AA.get_i2(AA.get_pb(px)->m_bval);
    }
    private static unsafe long test_307(long pb)
    {
        return AA.get_i2(((B*)checked((long)pb))->m_bval);
    }
    private static unsafe long test_314(B* pb)
    {
        return AA.get_i3(ref (pb++)->m_bval);
    }
    private static unsafe long test_321(B[,] ab, long i, long j)
    {
        fixed (B* pb = &ab[i, j])
        {
            return AA.get_i3(ref pb->m_bval);
        }
    }
    private static unsafe long test_328(B* pb1)
    {
        B* pb;
        return AA.get_i3(ref (pb = pb1 - 8)->m_bval);
    }
    private static unsafe long test_335(B* pb, B* pb1, B* pb2)
    {
        return AA.get_i3(ref (pb = pb + (pb2 - pb1))->m_bval);
    }
    private static unsafe long test_342(B* pb1, bool trig)
    {
        fixed (B* pb = &AA.s_x.m_b)
        {
            return AA.get_i3(ref (trig ? pb : pb1)->m_bval);
        }
    }
    private static unsafe long test_349(byte* pb)
    {
        return AA.get_i3(ref ((B*)(pb + 7))->m_bval);
    }
    private static unsafe long test_356(B b)
    {
        return AA.get_bv1((&b)) != 100 ? 99 : 100;
    }
    private static unsafe long test_363()
    {
        fixed (B* pb = &AA.s_x.m_b)
        {
            return AA.get_bv1(pb) != 100 ? 99 : 100;
        }
    }
    private static unsafe long test_370(B* pb, long i)
    {
        return AA.get_bv1((&pb[i * 2])) != 100 ? 99 : 100;
    }
    private static unsafe long test_377(B* pb1, B* pb2)
    {
        return AA.get_bv1((pb1 >= pb2 ? pb1 : null)) != 100 ? 99 : 100;
    }
    private static unsafe long test_384(long pb)
    {
        return AA.get_bv1(((B*)pb)) != 100 ? 99 : 100;
    }
    private static unsafe long test_391(B* pb)
    {
        long[] e = { 100, 101 };
        return e[pb + 1 > pb ? 0 : 1];
    }
    [Fact]
    public static unsafe int TestEntryPoint()
    {
        AA loc_x = new AA(0, 100);
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_6(ref loc_x.m_b) != 100)
        {
            Console.WriteLine("test_6() failed.");
            return 106;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_13(&loc_x.m_b + 1) != 100)
        {
            Console.WriteLine("test_13() failed.");
            return 113;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_20(&loc_x.m_b + 2, 1) != 100)
        {
            Console.WriteLine("test_20() failed.");
            return 120;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_27(&loc_x) != 100)
        {
            Console.WriteLine("test_27() failed.");
            return 127;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_34((long)(long)&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_34() failed.");
            return 134;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_41(&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_41() failed.");
            return 141;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_48(new B[,] { { new B(), new B() }, { new B(), loc_x.m_b } }, 1, 1) != 100)
        {
            Console.WriteLine("test_48() failed.");
            return 148;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_55(&loc_x.m_b + 8) != 100)
        {
            Console.WriteLine("test_55() failed.");
            return 155;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_62(&loc_x.m_b - 2, &loc_x.m_b - 1, &loc_x.m_b + 1) != 100)
        {
            Console.WriteLine("test_62() failed.");
            return 162;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_69(&loc_x.m_b, true) != 100)
        {
            Console.WriteLine("test_69() failed.");
            return 169;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_76(((byte*)(&loc_x.m_b)) - 7) != 100)
        {
            Console.WriteLine("test_76() failed.");
            return 176;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_83(loc_x.m_b) != 100)
        {
            Console.WriteLine("test_83() failed.");
            return 183;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_90() != 100)
        {
            Console.WriteLine("test_90() failed.");
            return 190;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_97(&loc_x.m_b - 2, 1) != 100)
        {
            Console.WriteLine("test_97() failed.");
            return 197;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_104(&loc_x.m_b, &loc_x.m_b) != 100)
        {
            Console.WriteLine("test_104() failed.");
            return 204;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_111((long)&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_111() failed.");
            return 211;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_118(&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_118() failed.");
            return 218;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_125(new B[] { new B(), new B(), loc_x.m_b }, 2) != 100)
        {
            Console.WriteLine("test_125() failed.");
            return 225;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_132(&loc_x.m_b - 6) != 100)
        {
            Console.WriteLine("test_132() failed.");
            return 232;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_139(&loc_x.m_b - 1, new long[,,] { { { 0 } }, { { 0 } } }, 2) != 100)
        {
            Console.WriteLine("test_139() failed.");
            return 239;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_146(&loc_x) != 100)
        {
            Console.WriteLine("test_146() failed.");
            return 246;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_153((byte)(((long)&loc_x.m_b) - ((long)&loc_x.m_a)), &loc_x.m_a) != 100)
        {
            Console.WriteLine("test_153() failed.");
            return 253;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_160() != 100)
        {
            Console.WriteLine("test_160() failed.");
            return 260;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_167(new B[][] { new B[] { new B(), new B() }, new B[] { new B(), loc_x.m_b } }, 1, 1) != 100)
        {
            Console.WriteLine("test_167() failed.");
            return 267;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_174(&loc_x.m_b - 8, 8) != 100)
        {
            Console.WriteLine("test_174() failed.");
            return 274;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_181(&loc_x.m_b - 1, new long[,,] { { { 0 } }, { { 0 } } }, 2, 2) != 100)
        {
            Console.WriteLine("test_181() failed.");
            return 281;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_188(((ulong)&loc_x.m_b) & (~(ulong)0xff), unchecked((byte)&loc_x.m_b)) != 100)
        {
            Console.WriteLine("test_188() failed.");
            return 288;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_195(((long)(&loc_x.m_b)) << 4, ((long)(&loc_x.m_b)) & 0xff000000) != 100)
        {
            Console.WriteLine("test_195() failed.");
            return 295;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_202(new B[] { loc_x.m_b }) != 100)
        {
            Console.WriteLine("test_202() failed.");
            return 302;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_209(&loc_x.m_b - 1) != 100)
        {
            Console.WriteLine("test_209() failed.");
            return 309;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_216(&loc_x.m_b - 1, new long[] { 0, 1 }, 1) != 100)
        {
            Console.WriteLine("test_216() failed.");
            return 316;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_223(&loc_x) != 100)
        {
            Console.WriteLine("test_223() failed.");
            return 323;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_230((long)(((long)&loc_x.m_b) - 1)) != 100)
        {
            Console.WriteLine("test_230() failed.");
            return 330;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_237(&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_237() failed.");
            return 337;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_244(new AA[,] { { new AA(), new AA() }, { new AA(), loc_x } }, 2) != 100)
        {
            Console.WriteLine("test_244() failed.");
            return 344;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_251(&loc_x.m_b - 8, 8) != 100)
        {
            Console.WriteLine("test_251() failed.");
            return 351;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_258(&loc_x.m_b + 1, &loc_x.m_b) != 100)
        {
            Console.WriteLine("test_258() failed.");
            return 358;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_265((long)&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_265() failed.");
            return 365;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_272(((double*)(&loc_x.m_b)) - 4, 4) != 100)
        {
            Console.WriteLine("test_272() failed.");
            return 372;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_279(ref loc_x.m_b) != 100)
        {
            Console.WriteLine("test_279() failed.");
            return 379;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_286(&loc_x.m_b + 1) != 100)
        {
            Console.WriteLine("test_286() failed.");
            return 386;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_293(&loc_x.m_b + 2, 1) != 100)
        {
            Console.WriteLine("test_293() failed.");
            return 393;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_300(&loc_x) != 100)
        {
            Console.WriteLine("test_300() failed.");
            return 400;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_307((long)(long)&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_307() failed.");
            return 407;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_314(&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_314() failed.");
            return 414;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_321(new B[,] { { new B(), new B() }, { new B(), loc_x.m_b } }, 1, 1) != 100)
        {
            Console.WriteLine("test_321() failed.");
            return 421;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_328(&loc_x.m_b + 8) != 100)
        {
            Console.WriteLine("test_328() failed.");
            return 428;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_335(&loc_x.m_b - 2, &loc_x.m_b - 1, &loc_x.m_b + 1) != 100)
        {
            Console.WriteLine("test_335() failed.");
            return 435;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_342(&loc_x.m_b, true) != 100)
        {
            Console.WriteLine("test_342() failed.");
            return 442;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_349(((byte*)(&loc_x.m_b)) - 7) != 100)
        {
            Console.WriteLine("test_349() failed.");
            return 449;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_356(loc_x.m_b) != 100)
        {
            Console.WriteLine("test_356() failed.");
            return 456;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_363() != 100)
        {
            Console.WriteLine("test_363() failed.");
            return 463;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_370(&loc_x.m_b - 2, 1) != 100)
        {
            Console.WriteLine("test_370() failed.");
            return 470;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_377(&loc_x.m_b, &loc_x.m_b) != 100)
        {
            Console.WriteLine("test_377() failed.");
            return 477;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_384((long)&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_384() failed.");
            return 484;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_391((B*)1) != 100)
        {
            Console.WriteLine("test_391() failed.");
            return 491;
        }
        Console.WriteLine("All tests passed.");
        return 100;
    }
}
}
