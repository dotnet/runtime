// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using UnsafeCSharp;
using Xunit;

namespace Test_unsafe_4
{
public class TestApp
{
    private static unsafe long test_4()
    {
        AA loc_x = new AA(0, 100);
        return (&loc_x.m_b)->m_bval;
    }
    private static unsafe long test_11(B[][] ab, long i, long j)
    {
        fixed (B* pb = &ab[i][j])
        {
            return pb->m_bval;
        }
    }
    private static unsafe long test_18(B* pb1, long i)
    {
        B* pb;
        return (pb = (B*)(((byte*)pb1) + i * sizeof(B)))->m_bval;
    }
    private static unsafe long test_25(B* pb, long[,,] i, long ii, byte jj)
    {
        return (&pb[i[ii - jj, 0, ii - jj] = ii - 1])->m_bval;
    }
    private static unsafe long test_32(ulong ub, byte lb)
    {
        return ((B*)(ub | lb))->m_bval;
    }
    private static unsafe long test_39(long p, long s)
    {
        return ((B*)((p >> 4) | s))->m_bval;
    }
    private static unsafe long test_46(B[] ab)
    {
        fixed (B* pb = &ab[0])
        {
            return pb[0].m_bval;
        }
    }
    private static unsafe long test_53(B* pb)
    {
        return (++pb)[0].m_bval;
    }
    private static unsafe long test_60(B* pb, long[] i, long ii)
    {
        return (&pb[i[ii]])[0].m_bval;
    }
    private static unsafe long test_67(AA* px)
    {
        return (AA.get_pb_1(px) + 1)[0].m_bval;
    }
    private static unsafe long test_74(long pb)
    {
        return ((B*)checked(((long)pb) + 1))[0].m_bval;
    }
    private static unsafe long test_81(B* pb)
    {
        return AA.get_bv1((pb--));
    }
    private static unsafe long test_88(AA[,] ab, long i)
    {
        long j = 0;
        fixed (B* pb = &ab[--i, ++j].m_b)
        {
            return AA.get_bv1(pb);
        }
    }
    private static unsafe long test_95(B* pb1, long i)
    {
        B* pb;
        return AA.get_bv1((pb = pb1 + i));
    }
    private static unsafe long test_102(B* pb1, B* pb2)
    {
        return AA.get_bv1((pb1 > pb2 ? pb2 : null));
    }
    private static unsafe long test_109(long pb)
    {
        return AA.get_bv1(((B*)pb));
    }
    private static unsafe long test_116(double* pb, long i)
    {
        return AA.get_bv1(((B*)(pb + i)));
    }
    private static unsafe long test_123(ref B b)
    {
        fixed (B* pb = &b)
        {
            return AA.get_bv2(*pb);
        }
    }
    private static unsafe long test_130(B* pb)
    {
        return AA.get_bv2(*(--pb));
    }
    private static unsafe long test_137(B* pb, long i)
    {
        return AA.get_bv2(*(&pb[-(i << (int)i)]));
    }
    private static unsafe long test_144(AA* px)
    {
        return AA.get_bv2(*AA.get_pb(px));
    }
    private static unsafe long test_151(long pb)
    {
        return AA.get_bv2(*((B*)checked((long)pb)));
    }
    private static unsafe long test_158(B* pb)
    {
        return AA.get_bv3(ref *(pb++));
    }
    private static unsafe long test_165(B[,] ab, long i, long j)
    {
        fixed (B* pb = &ab[i, j])
        {
            return AA.get_bv3(ref *pb);
        }
    }
    private static unsafe long test_172(B* pb1)
    {
        B* pb;
        return AA.get_bv3(ref *(pb = pb1 - 8));
    }
    private static unsafe long test_179(B* pb, B* pb1, B* pb2)
    {
        return AA.get_bv3(ref *(pb = pb + (pb2 - pb1)));
    }
    private static unsafe long test_186(B* pb1, bool trig)
    {
        fixed (B* pb = &AA.s_x.m_b)
        {
            return AA.get_bv3(ref *(trig ? pb : pb1));
        }
    }
    private static unsafe long test_193(byte* pb)
    {
        return AA.get_bv3(ref *((B*)(pb + 7)));
    }
    private static unsafe long test_200(B b)
    {
        return (&b)->m_bval == 100 ? 100 : 101;
    }
    private static unsafe long test_207()
    {
        fixed (B* pb = &AA.s_x.m_b)
        {
            return pb->m_bval == 100 ? 100 : 101;
        }
    }
    private static unsafe long test_214(B* pb, long i)
    {
        return (&pb[i * 2])->m_bval == 100 ? 100 : 101;
    }
    private static unsafe long test_221(B* pb1, B* pb2)
    {
        return (pb1 >= pb2 ? pb1 : null)->m_bval == 100 ? 100 : 101;
    }
    private static unsafe long test_228(long pb)
    {
        return ((B*)pb)->m_bval == 100 ? 100 : 101;
    }
    private static unsafe long test_235(B* pb)
    {
        return AA.get_i1(&pb->m_bval);
    }
    private static unsafe long test_242(B[] ab, long i)
    {
        fixed (B* pb = &ab[i])
        {
            return AA.get_i1(&pb->m_bval);
        }
    }
    private static unsafe long test_249(B* pb)
    {
        return AA.get_i1(&(pb += 6)->m_bval);
    }
    private static unsafe long test_256(B* pb, long[,,] i, long ii)
    {
        return AA.get_i1(&(&pb[++i[--ii, 0, 0]])->m_bval);
    }
    private static unsafe long test_263(AA* px)
    {
        return AA.get_i1(&((B*)AA.get_pb_i(px))->m_bval);
    }
    private static unsafe long test_270(byte diff, A* pa)
    {
        return AA.get_i1(&((B*)(((byte*)pa) + diff))->m_bval);
    }
    private static unsafe long test_277()
    {
        AA loc_x = new AA(0, 100);
        return AA.get_i2((&loc_x.m_b)->m_bval);
    }
    private static unsafe long test_284(B[][] ab, long i, long j)
    {
        fixed (B* pb = &ab[i][j])
        {
            return AA.get_i2(pb->m_bval);
        }
    }
    private static unsafe long test_291(B* pb1, long i)
    {
        B* pb;
        return AA.get_i2((pb = (B*)(((byte*)pb1) + i * sizeof(B)))->m_bval);
    }
    private static unsafe long test_298(B* pb, long[,,] i, long ii, byte jj)
    {
        return AA.get_i2((&pb[i[ii - jj, 0, ii - jj] = ii - 1])->m_bval);
    }
    private static unsafe long test_305(ulong ub, byte lb)
    {
        return AA.get_i2(((B*)(ub | lb))->m_bval);
    }
    private static unsafe long test_312(long p, long s)
    {
        return AA.get_i2(((B*)((p >> 4) | s))->m_bval);
    }
    private static unsafe long test_319(B[] ab)
    {
        fixed (B* pb = &ab[0])
        {
            return AA.get_i3(ref pb->m_bval);
        }
    }
    private static unsafe long test_326(B* pb)
    {
        return AA.get_i3(ref (++pb)->m_bval);
    }
    private static unsafe long test_333(B* pb, long[] i, long ii)
    {
        return AA.get_i3(ref (&pb[i[ii]])->m_bval);
    }
    private static unsafe long test_340(AA* px)
    {
        return AA.get_i3(ref (AA.get_pb_1(px) + 1)->m_bval);
    }
    private static unsafe long test_347(long pb)
    {
        return AA.get_i3(ref ((B*)checked(((long)pb) + 1))->m_bval);
    }
    private static unsafe long test_354(B* pb)
    {
        return AA.get_bv1((pb--)) != 100 ? 99 : 100;
    }
    private static unsafe long test_361(AA[,] ab, long i)
    {
        long j = 0;
        fixed (B* pb = &ab[--i, ++j].m_b)
        {
            return AA.get_bv1(pb) != 100 ? 99 : 100;
        }
    }
    private static unsafe long test_368(B* pb1, long i)
    {
        B* pb;
        return AA.get_bv1((pb = pb1 + i)) != 100 ? 99 : 100;
    }
    private static unsafe long test_375(B* pb1, B* pb2)
    {
        return AA.get_bv1((pb1 > pb2 ? pb2 : null)) != 100 ? 99 : 100;
    }
    private static unsafe long test_382(long pb)
    {
        return AA.get_bv1(((B*)pb)) != 100 ? 99 : 100;
    }
    private static unsafe long test_389(double* pb, long i)
    {
        return AA.get_bv1(((B*)(pb + i))) != 100 ? 99 : 100;
    }
    private static unsafe long test_396(B* pb1, B* pb2)
    {
        if (pb1 >= pb2) return 100;
        throw new Exception();
    }
    [Fact]
    public static unsafe int TestEntryPoint()
    {
        AA loc_x = new AA(0, 100);
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_4() != 100)
        {
            Console.WriteLine("test_4() failed.");
            return 104;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_11(new B[][] { new B[] { new B(), new B() }, new B[] { new B(), loc_x.m_b } }, 1, 1) != 100)
        {
            Console.WriteLine("test_11() failed.");
            return 111;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_18(&loc_x.m_b - 8, 8) != 100)
        {
            Console.WriteLine("test_18() failed.");
            return 118;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_25(&loc_x.m_b - 1, new long[,,] { { { 0 } }, { { 0 } } }, 2, 2) != 100)
        {
            Console.WriteLine("test_25() failed.");
            return 125;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_32(((ulong)&loc_x.m_b) & (~(ulong)0xff), unchecked((byte)&loc_x.m_b)) != 100)
        {
            Console.WriteLine("test_32() failed.");
            return 132;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_39(((long)(&loc_x.m_b)) << 4, ((long)(&loc_x.m_b)) & 0xff000000) != 100)
        {
            Console.WriteLine("test_39() failed.");
            return 139;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_46(new B[] { loc_x.m_b }) != 100)
        {
            Console.WriteLine("test_46() failed.");
            return 146;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_53(&loc_x.m_b - 1) != 100)
        {
            Console.WriteLine("test_53() failed.");
            return 153;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_60(&loc_x.m_b - 1, new long[] { 0, 1 }, 1) != 100)
        {
            Console.WriteLine("test_60() failed.");
            return 160;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_67(&loc_x) != 100)
        {
            Console.WriteLine("test_67() failed.");
            return 167;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_74((long)(((long)&loc_x.m_b) - 1)) != 100)
        {
            Console.WriteLine("test_74() failed.");
            return 174;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_81(&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_81() failed.");
            return 181;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_88(new AA[,] { { new AA(), new AA() }, { new AA(), loc_x } }, 2) != 100)
        {
            Console.WriteLine("test_88() failed.");
            return 188;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_95(&loc_x.m_b - 8, 8) != 100)
        {
            Console.WriteLine("test_95() failed.");
            return 195;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_102(&loc_x.m_b + 1, &loc_x.m_b) != 100)
        {
            Console.WriteLine("test_102() failed.");
            return 202;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_109((long)&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_109() failed.");
            return 209;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_116(((double*)(&loc_x.m_b)) - 4, 4) != 100)
        {
            Console.WriteLine("test_116() failed.");
            return 216;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_123(ref loc_x.m_b) != 100)
        {
            Console.WriteLine("test_123() failed.");
            return 223;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_130(&loc_x.m_b + 1) != 100)
        {
            Console.WriteLine("test_130() failed.");
            return 230;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_137(&loc_x.m_b + 2, 1) != 100)
        {
            Console.WriteLine("test_137() failed.");
            return 237;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_144(&loc_x) != 100)
        {
            Console.WriteLine("test_144() failed.");
            return 244;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_151((long)(long)&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_151() failed.");
            return 251;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_158(&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_158() failed.");
            return 258;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_165(new B[,] { { new B(), new B() }, { new B(), loc_x.m_b } }, 1, 1) != 100)
        {
            Console.WriteLine("test_165() failed.");
            return 265;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_172(&loc_x.m_b + 8) != 100)
        {
            Console.WriteLine("test_172() failed.");
            return 272;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_179(&loc_x.m_b - 2, &loc_x.m_b - 1, &loc_x.m_b + 1) != 100)
        {
            Console.WriteLine("test_179() failed.");
            return 279;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_186(&loc_x.m_b, true) != 100)
        {
            Console.WriteLine("test_186() failed.");
            return 286;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_193(((byte*)(&loc_x.m_b)) - 7) != 100)
        {
            Console.WriteLine("test_193() failed.");
            return 293;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_200(loc_x.m_b) != 100)
        {
            Console.WriteLine("test_200() failed.");
            return 300;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_207() != 100)
        {
            Console.WriteLine("test_207() failed.");
            return 307;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_214(&loc_x.m_b - 2, 1) != 100)
        {
            Console.WriteLine("test_214() failed.");
            return 314;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_221(&loc_x.m_b, &loc_x.m_b) != 100)
        {
            Console.WriteLine("test_221() failed.");
            return 321;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_228((long)&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_228() failed.");
            return 328;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_235(&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_235() failed.");
            return 335;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_242(new B[] { new B(), new B(), loc_x.m_b }, 2) != 100)
        {
            Console.WriteLine("test_242() failed.");
            return 342;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_249(&loc_x.m_b - 6) != 100)
        {
            Console.WriteLine("test_249() failed.");
            return 349;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_256(&loc_x.m_b - 1, new long[,,] { { { 0 } }, { { 0 } } }, 2) != 100)
        {
            Console.WriteLine("test_256() failed.");
            return 356;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_263(&loc_x) != 100)
        {
            Console.WriteLine("test_263() failed.");
            return 363;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_270((byte)(((long)&loc_x.m_b) - ((long)&loc_x.m_a)), &loc_x.m_a) != 100)
        {
            Console.WriteLine("test_270() failed.");
            return 370;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_277() != 100)
        {
            Console.WriteLine("test_277() failed.");
            return 377;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_284(new B[][] { new B[] { new B(), new B() }, new B[] { new B(), loc_x.m_b } }, 1, 1) != 100)
        {
            Console.WriteLine("test_284() failed.");
            return 384;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_291(&loc_x.m_b - 8, 8) != 100)
        {
            Console.WriteLine("test_291() failed.");
            return 391;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_298(&loc_x.m_b - 1, new long[,,] { { { 0 } }, { { 0 } } }, 2, 2) != 100)
        {
            Console.WriteLine("test_298() failed.");
            return 398;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_305(((ulong)&loc_x.m_b) & (~(ulong)0xff), unchecked((byte)&loc_x.m_b)) != 100)
        {
            Console.WriteLine("test_305() failed.");
            return 405;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_312(((long)(&loc_x.m_b)) << 4, ((long)(&loc_x.m_b)) & 0xff000000) != 100)
        {
            Console.WriteLine("test_312() failed.");
            return 412;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_319(new B[] { loc_x.m_b }) != 100)
        {
            Console.WriteLine("test_319() failed.");
            return 419;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_326(&loc_x.m_b - 1) != 100)
        {
            Console.WriteLine("test_326() failed.");
            return 426;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_333(&loc_x.m_b - 1, new long[] { 0, 1 }, 1) != 100)
        {
            Console.WriteLine("test_333() failed.");
            return 433;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_340(&loc_x) != 100)
        {
            Console.WriteLine("test_340() failed.");
            return 440;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_347((long)(((long)&loc_x.m_b) - 1)) != 100)
        {
            Console.WriteLine("test_347() failed.");
            return 447;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_354(&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_354() failed.");
            return 454;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_361(new AA[,] { { new AA(), new AA() }, { new AA(), loc_x } }, 2) != 100)
        {
            Console.WriteLine("test_361() failed.");
            return 461;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_368(&loc_x.m_b - 8, 8) != 100)
        {
            Console.WriteLine("test_368() failed.");
            return 468;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_375(&loc_x.m_b + 1, &loc_x.m_b) != 100)
        {
            Console.WriteLine("test_375() failed.");
            return 475;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_382((long)&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_382() failed.");
            return 482;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_389(((double*)(&loc_x.m_b)) - 4, 4) != 100)
        {
            Console.WriteLine("test_389() failed.");
            return 489;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_396(&loc_x.m_b, &loc_x.m_b) != 100)
        {
            Console.WriteLine("test_396() failed.");
            return 496;
        }
        Console.WriteLine("All tests passed.");
        return 100;
    }
}
}
