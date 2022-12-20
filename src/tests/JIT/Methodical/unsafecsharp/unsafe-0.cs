// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using UnsafeCSharp;
using Xunit;

namespace Test_unsafe_0
{
public class TestApp
{
    private static unsafe long test_7(B[] ab)
    {
        fixed (B* pb = &ab[0])
        {
            return pb->m_bval;
        }
    }
    private static unsafe long test_14(B* pb)
    {
        return (++pb)->m_bval;
    }
    private static unsafe long test_21(B* pb, long[] i, long ii)
    {
        return (&pb[i[ii]])->m_bval;
    }
    private static unsafe long test_28(AA* px)
    {
        return (AA.get_pb_1(px) + 1)->m_bval;
    }
    private static unsafe long test_35(long pb)
    {
        return ((B*)checked(((long)pb) + 1))->m_bval;
    }
    private static unsafe long test_42(B* pb)
    {
        return (pb--)[0].m_bval;
    }
    private static unsafe long test_49(AA[,] ab, long i)
    {
        long j = 0;
        fixed (B* pb = &ab[--i, ++j].m_b)
        {
            return pb[0].m_bval;
        }
    }
    private static unsafe long test_56(B* pb1, long i)
    {
        B* pb;
        return (pb = pb1 + i)[0].m_bval;
    }
    private static unsafe long test_63(B* pb1, B* pb2)
    {
        return (pb1 > pb2 ? pb2 : null)[0].m_bval;
    }
    private static unsafe long test_70(long pb)
    {
        return ((B*)pb)[0].m_bval;
    }
    private static unsafe long test_77(double* pb, long i)
    {
        return ((B*)(pb + i))[0].m_bval;
    }
    private static unsafe long test_84(ref B b)
    {
        fixed (B* pb = &b)
        {
            return AA.get_bv1(pb);
        }
    }
    private static unsafe long test_91(B* pb)
    {
        return AA.get_bv1((--pb));
    }
    private static unsafe long test_98(B* pb, long i)
    {
        return AA.get_bv1((&pb[-(i << (int)i)]));
    }
    private static unsafe long test_105(AA* px)
    {
        return AA.get_bv1(AA.get_pb(px));
    }
    private static unsafe long test_112(long pb)
    {
        return AA.get_bv1(((B*)checked((long)pb)));
    }
    private static unsafe long test_119(B* pb)
    {
        return AA.get_bv2(*(pb++));
    }
    private static unsafe long test_126(B[,] ab, long i, long j)
    {
        fixed (B* pb = &ab[i, j])
        {
            return AA.get_bv2(*pb);
        }
    }
    private static unsafe long test_133(B* pb1)
    {
        B* pb;
        return AA.get_bv2(*(pb = pb1 - 8));
    }
    private static unsafe long test_140(B* pb, B* pb1, B* pb2)
    {
        return AA.get_bv2(*(pb = pb + (pb2 - pb1)));
    }
    private static unsafe long test_147(B* pb1, bool trig)
    {
        fixed (B* pb = &AA.s_x.m_b)
        {
            return AA.get_bv2(*(trig ? pb : pb1));
        }
    }
    private static unsafe long test_154(byte* pb)
    {
        return AA.get_bv2(*((B*)(pb + 7)));
    }
    private static unsafe long test_161(B b)
    {
        return AA.get_bv3(ref *(&b));
    }
    private static unsafe long test_168()
    {
        fixed (B* pb = &AA.s_x.m_b)
        {
            return AA.get_bv3(ref *pb);
        }
    }
    private static unsafe long test_175(B* pb, long i)
    {
        return AA.get_bv3(ref *(&pb[i * 2]));
    }
    private static unsafe long test_182(B* pb1, B* pb2)
    {
        return AA.get_bv3(ref *(pb1 >= pb2 ? pb1 : null));
    }
    private static unsafe long test_189(long pb)
    {
        return AA.get_bv3(ref *((B*)pb));
    }
    private static unsafe long test_196(B* pb)
    {
        return pb->m_bval == 100 ? 100 : 101;
    }
    private static unsafe long test_203(B[] ab, long i)
    {
        fixed (B* pb = &ab[i])
        {
            return pb->m_bval == 100 ? 100 : 101;
        }
    }
    private static unsafe long test_210(B* pb)
    {
        return (pb += 6)->m_bval == 100 ? 100 : 101;
    }
    private static unsafe long test_217(B* pb, long[,,] i, long ii)
    {
        return (&pb[++i[--ii, 0, 0]])->m_bval == 100 ? 100 : 101;
    }
    private static unsafe long test_224(AA* px)
    {
        return ((B*)AA.get_pb_i(px))->m_bval == 100 ? 100 : 101;
    }
    private static unsafe long test_231(byte diff, A* pa)
    {
        return ((B*)(((byte*)pa) + diff))->m_bval == 100 ? 100 : 101;
    }
    private static unsafe long test_238()
    {
        AA loc_x = new AA(0, 100);
        return AA.get_i1(&(&loc_x.m_b)->m_bval);
    }
    private static unsafe long test_245(B[][] ab, long i, long j)
    {
        fixed (B* pb = &ab[i][j])
        {
            return AA.get_i1(&pb->m_bval);
        }
    }
    private static unsafe long test_252(B* pb1, long i)
    {
        B* pb;
        return AA.get_i1(&(pb = (B*)(((byte*)pb1) + i * sizeof(B)))->m_bval);
    }
    private static unsafe long test_259(B* pb, long[,,] i, long ii, byte jj)
    {
        return AA.get_i1(&(&pb[i[ii - jj, 0, ii - jj] = ii - 1])->m_bval);
    }
    private static unsafe long test_266(ulong ub, byte lb)
    {
        return AA.get_i1(&((B*)(ub | lb))->m_bval);
    }
    private static unsafe long test_273(long p, long s)
    {
        return AA.get_i1(&((B*)((p >> 4) | s))->m_bval);
    }
    private static unsafe long test_280(B[] ab)
    {
        fixed (B* pb = &ab[0])
        {
            return AA.get_i2(pb->m_bval);
        }
    }
    private static unsafe long test_287(B* pb)
    {
        return AA.get_i2((++pb)->m_bval);
    }
    private static unsafe long test_294(B* pb, long[] i, long ii)
    {
        return AA.get_i2((&pb[i[ii]])->m_bval);
    }
    private static unsafe long test_301(AA* px)
    {
        return AA.get_i2((AA.get_pb_1(px) + 1)->m_bval);
    }
    private static unsafe long test_308(long pb)
    {
        return AA.get_i2(((B*)checked(((long)pb) + 1))->m_bval);
    }
    private static unsafe long test_315(B* pb)
    {
        return AA.get_i3(ref (pb--)->m_bval);
    }
    private static unsafe long test_322(AA[,] ab, long i)
    {
        long j = 0;
        fixed (B* pb = &ab[--i, ++j].m_b)
        {
            return AA.get_i3(ref pb->m_bval);
        }
    }
    private static unsafe long test_329(B* pb1, long i)
    {
        B* pb;
        return AA.get_i3(ref (pb = pb1 + i)->m_bval);
    }
    private static unsafe long test_336(B* pb1, B* pb2)
    {
        return AA.get_i3(ref (pb1 > pb2 ? pb2 : null)->m_bval);
    }
    private static unsafe long test_343(long pb)
    {
        return AA.get_i3(ref ((B*)pb)->m_bval);
    }
    private static unsafe long test_350(double* pb, long i)
    {
        return AA.get_i3(ref ((B*)(pb + i))->m_bval);
    }
    private static unsafe long test_357(ref B b)
    {
        fixed (B* pb = &b)
        {
            return AA.get_bv1(pb) != 100 ? 99 : 100;
        }
    }
    private static unsafe long test_364(B* pb)
    {
        return AA.get_bv1((--pb)) != 100 ? 99 : 100;
    }
    private static unsafe long test_371(B* pb, long i)
    {
        return AA.get_bv1((&pb[-(i << (int)i)])) != 100 ? 99 : 100;
    }
    private static unsafe long test_378(AA* px)
    {
        return AA.get_bv1(AA.get_pb(px)) != 100 ? 99 : 100;
    }
    private static unsafe long test_385(long pb)
    {
        return AA.get_bv1(((B*)checked((long)pb))) != 100 ? 99 : 100;
    }
    private static unsafe long test_392(B* pb1, B* pb2)
    {
        long[] e = { 100, 101 };
        return e[pb1 >= pb2 ? 0 : 1];
    }
    [Fact]
    public static unsafe int TestEntryPoint()
    {
        AA loc_x = new AA(0, 100);
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_7(new B[] { loc_x.m_b }) != 100)
        {
            Console.WriteLine("test_7() failed.");
            return 107;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_14(&loc_x.m_b - 1) != 100)
        {
            Console.WriteLine("test_14() failed.");
            return 114;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_21(&loc_x.m_b - 1, new long[] { 0, 1 }, 1) != 100)
        {
            Console.WriteLine("test_21() failed.");
            return 121;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_28(&loc_x) != 100)
        {
            Console.WriteLine("test_28() failed.");
            return 128;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_35((long)(((long)&loc_x.m_b) - 1)) != 100)
        {
            Console.WriteLine("test_35() failed.");
            return 135;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_42(&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_42() failed.");
            return 142;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_49(new AA[,] { { new AA(), new AA() }, { new AA(), loc_x } }, 2) != 100)
        {
            Console.WriteLine("test_49() failed.");
            return 149;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_56(&loc_x.m_b - 8, 8) != 100)
        {
            Console.WriteLine("test_56() failed.");
            return 156;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_63(&loc_x.m_b + 1, &loc_x.m_b) != 100)
        {
            Console.WriteLine("test_63() failed.");
            return 163;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_70((long)&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_70() failed.");
            return 170;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_77(((double*)(&loc_x.m_b)) - 4, 4) != 100)
        {
            Console.WriteLine("test_77() failed.");
            return 177;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_84(ref loc_x.m_b) != 100)
        {
            Console.WriteLine("test_84() failed.");
            return 184;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_91(&loc_x.m_b + 1) != 100)
        {
            Console.WriteLine("test_91() failed.");
            return 191;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_98(&loc_x.m_b + 2, 1) != 100)
        {
            Console.WriteLine("test_98() failed.");
            return 198;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_105(&loc_x) != 100)
        {
            Console.WriteLine("test_105() failed.");
            return 205;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_112((long)(long)&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_112() failed.");
            return 212;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_119(&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_119() failed.");
            return 219;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_126(new B[,] { { new B(), new B() }, { new B(), loc_x.m_b } }, 1, 1) != 100)
        {
            Console.WriteLine("test_126() failed.");
            return 226;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_133(&loc_x.m_b + 8) != 100)
        {
            Console.WriteLine("test_133() failed.");
            return 233;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_140(&loc_x.m_b - 2, &loc_x.m_b - 1, &loc_x.m_b + 1) != 100)
        {
            Console.WriteLine("test_140() failed.");
            return 240;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_147(&loc_x.m_b, true) != 100)
        {
            Console.WriteLine("test_147() failed.");
            return 247;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_154(((byte*)(&loc_x.m_b)) - 7) != 100)
        {
            Console.WriteLine("test_154() failed.");
            return 254;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_161(loc_x.m_b) != 100)
        {
            Console.WriteLine("test_161() failed.");
            return 261;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_168() != 100)
        {
            Console.WriteLine("test_168() failed.");
            return 268;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_175(&loc_x.m_b - 2, 1) != 100)
        {
            Console.WriteLine("test_175() failed.");
            return 275;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_182(&loc_x.m_b, &loc_x.m_b) != 100)
        {
            Console.WriteLine("test_182() failed.");
            return 282;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_189((long)&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_189() failed.");
            return 289;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_196(&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_196() failed.");
            return 296;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_203(new B[] { new B(), new B(), loc_x.m_b }, 2) != 100)
        {
            Console.WriteLine("test_203() failed.");
            return 303;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_210(&loc_x.m_b - 6) != 100)
        {
            Console.WriteLine("test_210() failed.");
            return 310;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_217(&loc_x.m_b - 1, new long[,,] { { { 0 } }, { { 0 } } }, 2) != 100)
        {
            Console.WriteLine("test_217() failed.");
            return 317;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_224(&loc_x) != 100)
        {
            Console.WriteLine("test_224() failed.");
            return 324;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_231((byte)(((long)&loc_x.m_b) - ((long)&loc_x.m_a)), &loc_x.m_a) != 100)
        {
            Console.WriteLine("test_231() failed.");
            return 331;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_238() != 100)
        {
            Console.WriteLine("test_238() failed.");
            return 338;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_245(new B[][] { new B[] { new B(), new B() }, new B[] { new B(), loc_x.m_b } }, 1, 1) != 100)
        {
            Console.WriteLine("test_245() failed.");
            return 345;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_252(&loc_x.m_b - 8, 8) != 100)
        {
            Console.WriteLine("test_252() failed.");
            return 352;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_259(&loc_x.m_b - 1, new long[,,] { { { 0 } }, { { 0 } } }, 2, 2) != 100)
        {
            Console.WriteLine("test_259() failed.");
            return 359;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_266(((ulong)&loc_x.m_b) & (~(ulong)0xff), unchecked((byte)&loc_x.m_b)) != 100)
        {
            Console.WriteLine("test_266() failed.");
            return 366;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_273(((long)(&loc_x.m_b)) << 4, (long)(((ulong)&loc_x.m_b) & 0xff00000000000000)) != 100)
        {
            Console.WriteLine("test_273() failed.");
            return 373;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_280(new B[] { loc_x.m_b }) != 100)
        {
            Console.WriteLine("test_280() failed.");
            return 380;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_287(&loc_x.m_b - 1) != 100)
        {
            Console.WriteLine("test_287() failed.");
            return 387;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_294(&loc_x.m_b - 1, new long[] { 0, 1 }, 1) != 100)
        {
            Console.WriteLine("test_294() failed.");
            return 394;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_301(&loc_x) != 100)
        {
            Console.WriteLine("test_301() failed.");
            return 401;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_308((long)(((long)&loc_x.m_b) - 1)) != 100)
        {
            Console.WriteLine("test_308() failed.");
            return 408;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_315(&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_315() failed.");
            return 415;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_322(new AA[,] { { new AA(), new AA() }, { new AA(), loc_x } }, 2) != 100)
        {
            Console.WriteLine("test_322() failed.");
            return 422;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_329(&loc_x.m_b - 8, 8) != 100)
        {
            Console.WriteLine("test_329() failed.");
            return 429;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_336(&loc_x.m_b + 1, &loc_x.m_b) != 100)
        {
            Console.WriteLine("test_336() failed.");
            return 436;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_343((long)&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_343() failed.");
            return 443;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_350(((double*)(&loc_x.m_b)) - 4, 4) != 100)
        {
            Console.WriteLine("test_350() failed.");
            return 450;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_357(ref loc_x.m_b) != 100)
        {
            Console.WriteLine("test_357() failed.");
            return 457;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_364(&loc_x.m_b + 1) != 100)
        {
            Console.WriteLine("test_364() failed.");
            return 464;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_371(&loc_x.m_b + 2, 1) != 100)
        {
            Console.WriteLine("test_371() failed.");
            return 471;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_378(&loc_x) != 100)
        {
            Console.WriteLine("test_378() failed.");
            return 478;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_385((long)(long)&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_385() failed.");
            return 485;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_392(&loc_x.m_b, &loc_x.m_b) != 100)
        {
            Console.WriteLine("test_392() failed.");
            return 492;
        }
        Console.WriteLine("All tests passed.");
        return 100;
    }
}
}
