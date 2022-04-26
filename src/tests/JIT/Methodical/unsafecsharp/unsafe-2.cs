// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using UnsafeCSharp;
using Xunit;

namespace Test_unsafe_2
{
public class TestApp
{
    private static unsafe long test_2(B* pb)
    {
        return (pb++)->m_bval;
    }
    private static unsafe long test_9(B[,] ab, long i, long j)
    {
        fixed (B* pb = &ab[i, j])
        {
            return pb->m_bval;
        }
    }
    private static unsafe long test_16(B* pb1)
    {
        B* pb;
        return (pb = pb1 - 8)->m_bval;
    }
    private static unsafe long test_23(B* pb, B* pb1, B* pb2)
    {
        return (pb = pb + (pb2 - pb1))->m_bval;
    }
    private static unsafe long test_30(B* pb1, bool trig)
    {
        fixed (B* pb = &AA.s_x.m_b)
        {
            return (trig ? pb : pb1)->m_bval;
        }
    }
    private static unsafe long test_37(byte* pb)
    {
        return ((B*)(pb + 7))->m_bval;
    }
    private static unsafe long test_44(B b)
    {
        return (&b)[0].m_bval;
    }
    private static unsafe long test_51()
    {
        fixed (B* pb = &AA.s_x.m_b)
        {
            return pb[0].m_bval;
        }
    }
    private static unsafe long test_58(B* pb, long i)
    {
        return (&pb[i * 2])[0].m_bval;
    }
    private static unsafe long test_65(B* pb1, B* pb2)
    {
        return (pb1 >= pb2 ? pb1 : null)[0].m_bval;
    }
    private static unsafe long test_72(long pb)
    {
        return ((B*)pb)[0].m_bval;
    }
    private static unsafe long test_79(B* pb)
    {
        return AA.get_bv1(pb);
    }
    private static unsafe long test_86(B[] ab, long i)
    {
        fixed (B* pb = &ab[i])
        {
            return AA.get_bv1(pb);
        }
    }
    private static unsafe long test_93(B* pb)
    {
        return AA.get_bv1((pb += 6));
    }
    private static unsafe long test_100(B* pb, long[,,] i, long ii)
    {
        return AA.get_bv1((&pb[++i[--ii, 0, 0]]));
    }
    private static unsafe long test_107(AA* px)
    {
        return AA.get_bv1(((B*)AA.get_pb_i(px)));
    }
    private static unsafe long test_114(byte diff, A* pa)
    {
        return AA.get_bv1(((B*)(((byte*)pa) + diff)));
    }
    private static unsafe long test_121()
    {
        AA loc_x = new AA(0, 100);
        return AA.get_bv2(*(&loc_x.m_b));
    }
    private static unsafe long test_128(B[][] ab, long i, long j)
    {
        fixed (B* pb = &ab[i][j])
        {
            return AA.get_bv2(*pb);
        }
    }
    private static unsafe long test_135(B* pb1, long i)
    {
        B* pb;
        return AA.get_bv2(*(pb = (B*)(((byte*)pb1) + i * sizeof(B))));
    }
    private static unsafe long test_142(B* pb, long[,,] i, long ii, byte jj)
    {
        return AA.get_bv2(*(&pb[i[ii - jj, 0, ii - jj] = ii - 1]));
    }
    private static unsafe long test_149(ulong ub, byte lb)
    {
        return AA.get_bv2(*((B*)(ub | lb)));
    }
    private static unsafe long test_156(long p, long s)
    {
        return AA.get_bv2(*((B*)((p >> 4) | s)));
    }
    private static unsafe long test_163(B[] ab)
    {
        fixed (B* pb = &ab[0])
        {
            return AA.get_bv3(ref *pb);
        }
    }
    private static unsafe long test_170(B* pb)
    {
        return AA.get_bv3(ref *(++pb));
    }
    private static unsafe long test_177(B* pb, long[] i, long ii)
    {
        return AA.get_bv3(ref *(&pb[i[ii]]));
    }
    private static unsafe long test_184(AA* px)
    {
        return AA.get_bv3(ref *(AA.get_pb_1(px) + 1));
    }
    private static unsafe long test_191(long pb)
    {
        return AA.get_bv3(ref *((B*)checked(((long)pb) + 1)));
    }
    private static unsafe long test_198(B* pb)
    {
        return (pb--)->m_bval == 100 ? 100 : 101;
    }
    private static unsafe long test_205(AA[,] ab, long i)
    {
        long j = 0;
        fixed (B* pb = &ab[--i, ++j].m_b)
        {
            return pb->m_bval == 100 ? 100 : 101;
        }
    }
    private static unsafe long test_212(B* pb1, long i)
    {
        B* pb;
        return (pb = pb1 + i)->m_bval == 100 ? 100 : 101;
    }
    private static unsafe long test_219(B* pb1, B* pb2)
    {
        return (pb1 > pb2 ? pb2 : null)->m_bval == 100 ? 100 : 101;
    }
    private static unsafe long test_226(long pb)
    {
        return ((B*)pb)->m_bval == 100 ? 100 : 101;
    }
    private static unsafe long test_233(double* pb, long i)
    {
        return ((B*)(pb + i))->m_bval == 100 ? 100 : 101;
    }
    private static unsafe long test_240(ref B b)
    {
        fixed (B* pb = &b)
        {
            return AA.get_i1(&pb->m_bval);
        }
    }
    private static unsafe long test_247(B* pb)
    {
        return AA.get_i1(&(--pb)->m_bval);
    }
    private static unsafe long test_254(B* pb, long i)
    {
        return AA.get_i1(&(&pb[-(i << (int)i)])->m_bval);
    }
    private static unsafe long test_261(AA* px)
    {
        return AA.get_i1(&AA.get_pb(px)->m_bval);
    }
    private static unsafe long test_268(long pb)
    {
        return AA.get_i1(&((B*)checked((long)pb))->m_bval);
    }
    private static unsafe long test_275(B* pb)
    {
        return AA.get_i2((pb++)->m_bval);
    }
    private static unsafe long test_282(B[,] ab, long i, long j)
    {
        fixed (B* pb = &ab[i, j])
        {
            return AA.get_i2(pb->m_bval);
        }
    }
    private static unsafe long test_289(B* pb1)
    {
        B* pb;
        return AA.get_i2((pb = pb1 - 8)->m_bval);
    }
    private static unsafe long test_296(B* pb, B* pb1, B* pb2)
    {
        return AA.get_i2((pb = pb + (pb2 - pb1))->m_bval);
    }
    private static unsafe long test_303(B* pb1, bool trig)
    {
        fixed (B* pb = &AA.s_x.m_b)
        {
            return AA.get_i2((trig ? pb : pb1)->m_bval);
        }
    }
    private static unsafe long test_310(byte* pb)
    {
        return AA.get_i2(((B*)(pb + 7))->m_bval);
    }
    private static unsafe long test_317(B b)
    {
        return AA.get_i3(ref (&b)->m_bval);
    }
    private static unsafe long test_324()
    {
        fixed (B* pb = &AA.s_x.m_b)
        {
            return AA.get_i3(ref pb->m_bval);
        }
    }
    private static unsafe long test_331(B* pb, long i)
    {
        return AA.get_i3(ref (&pb[i * 2])->m_bval);
    }
    private static unsafe long test_338(B* pb1, B* pb2)
    {
        return AA.get_i3(ref (pb1 >= pb2 ? pb1 : null)->m_bval);
    }
    private static unsafe long test_345(long pb)
    {
        return AA.get_i3(ref ((B*)pb)->m_bval);
    }
    private static unsafe long test_352(B* pb)
    {
        return AA.get_bv1(pb) != 100 ? 99 : 100;
    }
    private static unsafe long test_359(B[] ab, long i)
    {
        fixed (B* pb = &ab[i])
        {
            return AA.get_bv1(pb) != 100 ? 99 : 100;
        }
    }
    private static unsafe long test_366(B* pb)
    {
        return AA.get_bv1((pb += 6)) != 100 ? 99 : 100;
    }
    private static unsafe long test_373(B* pb, long[,,] i, long ii)
    {
        return AA.get_bv1((&pb[++i[--ii, 0, 0]])) != 100 ? 99 : 100;
    }
    private static unsafe long test_380(AA* px)
    {
        return AA.get_bv1(((B*)AA.get_pb_i(px))) != 100 ? 99 : 100;
    }
    private static unsafe long test_387(byte diff, A* pa)
    {
        return AA.get_bv1(((B*)(((byte*)pa) + diff))) != 100 ? 99 : 100;
    }
    private static unsafe long test_394(B* pb1, B* pb2)
    {
        return pb1 >= pb2 ? 100 : 101;
    }
    [Fact]
    public static unsafe int TestEntryPoint()
    {
        AA loc_x = new AA(0, 100);
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_2(&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_2() failed.");
            return 102;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_9(new B[,] { { new B(), new B() }, { new B(), loc_x.m_b } }, 1, 1) != 100)
        {
            Console.WriteLine("test_9() failed.");
            return 109;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_16(&loc_x.m_b + 8) != 100)
        {
            Console.WriteLine("test_16() failed.");
            return 116;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_23(&loc_x.m_b - 2, &loc_x.m_b - 1, &loc_x.m_b + 1) != 100)
        {
            Console.WriteLine("test_23() failed.");
            return 123;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_30(&loc_x.m_b, true) != 100)
        {
            Console.WriteLine("test_30() failed.");
            return 130;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_37(((byte*)(&loc_x.m_b)) - 7) != 100)
        {
            Console.WriteLine("test_37() failed.");
            return 137;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_44(loc_x.m_b) != 100)
        {
            Console.WriteLine("test_44() failed.");
            return 144;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_51() != 100)
        {
            Console.WriteLine("test_51() failed.");
            return 151;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_58(&loc_x.m_b - 2, 1) != 100)
        {
            Console.WriteLine("test_58() failed.");
            return 158;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_65(&loc_x.m_b, &loc_x.m_b) != 100)
        {
            Console.WriteLine("test_65() failed.");
            return 165;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_72((long)&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_72() failed.");
            return 172;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_79(&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_79() failed.");
            return 179;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_86(new B[] { new B(), new B(), loc_x.m_b }, 2) != 100)
        {
            Console.WriteLine("test_86() failed.");
            return 186;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_93(&loc_x.m_b - 6) != 100)
        {
            Console.WriteLine("test_93() failed.");
            return 193;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_100(&loc_x.m_b - 1, new long[,,] { { { 0 } }, { { 0 } } }, 2) != 100)
        {
            Console.WriteLine("test_100() failed.");
            return 200;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_107(&loc_x) != 100)
        {
            Console.WriteLine("test_107() failed.");
            return 207;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_114((byte)(((long)&loc_x.m_b) - ((long)&loc_x.m_a)), &loc_x.m_a) != 100)
        {
            Console.WriteLine("test_114() failed.");
            return 214;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_121() != 100)
        {
            Console.WriteLine("test_121() failed.");
            return 221;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_128(new B[][] { new B[] { new B(), new B() }, new B[] { new B(), loc_x.m_b } }, 1, 1) != 100)
        {
            Console.WriteLine("test_128() failed.");
            return 228;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_135(&loc_x.m_b - 8, 8) != 100)
        {
            Console.WriteLine("test_135() failed.");
            return 235;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_142(&loc_x.m_b - 1, new long[,,] { { { 0 } }, { { 0 } } }, 2, 2) != 100)
        {
            Console.WriteLine("test_142() failed.");
            return 242;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_149(((ulong)&loc_x.m_b) & (~(ulong)0xff), unchecked((byte)&loc_x.m_b)) != 100)
        {
            Console.WriteLine("test_149() failed.");
            return 249;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_156(((long)(&loc_x.m_b)) << 4, ((long)(&loc_x.m_b)) & 0xff000000) != 100)
        {
            Console.WriteLine("test_156() failed.");
            return 256;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_163(new B[] { loc_x.m_b }) != 100)
        {
            Console.WriteLine("test_163() failed.");
            return 263;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_170(&loc_x.m_b - 1) != 100)
        {
            Console.WriteLine("test_170() failed.");
            return 270;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_177(&loc_x.m_b - 1, new long[] { 0, 1 }, 1) != 100)
        {
            Console.WriteLine("test_177() failed.");
            return 277;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_184(&loc_x) != 100)
        {
            Console.WriteLine("test_184() failed.");
            return 284;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_191((long)(((long)&loc_x.m_b) - 1)) != 100)
        {
            Console.WriteLine("test_191() failed.");
            return 291;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_198(&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_198() failed.");
            return 298;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_205(new AA[,] { { new AA(), new AA() }, { new AA(), loc_x } }, 2) != 100)
        {
            Console.WriteLine("test_205() failed.");
            return 305;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_212(&loc_x.m_b - 8, 8) != 100)
        {
            Console.WriteLine("test_212() failed.");
            return 312;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_219(&loc_x.m_b + 1, &loc_x.m_b) != 100)
        {
            Console.WriteLine("test_219() failed.");
            return 319;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_226((long)&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_226() failed.");
            return 326;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_233(((double*)(&loc_x.m_b)) - 4, 4) != 100)
        {
            Console.WriteLine("test_233() failed.");
            return 333;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_240(ref loc_x.m_b) != 100)
        {
            Console.WriteLine("test_240() failed.");
            return 340;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_247(&loc_x.m_b + 1) != 100)
        {
            Console.WriteLine("test_247() failed.");
            return 347;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_254(&loc_x.m_b + 2, 1) != 100)
        {
            Console.WriteLine("test_254() failed.");
            return 354;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_261(&loc_x) != 100)
        {
            Console.WriteLine("test_261() failed.");
            return 361;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_268((long)(long)&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_268() failed.");
            return 368;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_275(&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_275() failed.");
            return 375;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_282(new B[,] { { new B(), new B() }, { new B(), loc_x.m_b } }, 1, 1) != 100)
        {
            Console.WriteLine("test_282() failed.");
            return 382;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_289(&loc_x.m_b + 8) != 100)
        {
            Console.WriteLine("test_289() failed.");
            return 389;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_296(&loc_x.m_b - 2, &loc_x.m_b - 1, &loc_x.m_b + 1) != 100)
        {
            Console.WriteLine("test_296() failed.");
            return 396;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_303(&loc_x.m_b, true) != 100)
        {
            Console.WriteLine("test_303() failed.");
            return 403;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_310(((byte*)(&loc_x.m_b)) - 7) != 100)
        {
            Console.WriteLine("test_310() failed.");
            return 410;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_317(loc_x.m_b) != 100)
        {
            Console.WriteLine("test_317() failed.");
            return 417;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_324() != 100)
        {
            Console.WriteLine("test_324() failed.");
            return 424;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_331(&loc_x.m_b - 2, 1) != 100)
        {
            Console.WriteLine("test_331() failed.");
            return 431;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_338(&loc_x.m_b, &loc_x.m_b) != 100)
        {
            Console.WriteLine("test_338() failed.");
            return 438;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_345((long)&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_345() failed.");
            return 445;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_352(&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_352() failed.");
            return 452;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_359(new B[] { new B(), new B(), loc_x.m_b }, 2) != 100)
        {
            Console.WriteLine("test_359() failed.");
            return 459;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_366(&loc_x.m_b - 6) != 100)
        {
            Console.WriteLine("test_366() failed.");
            return 466;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_373(&loc_x.m_b - 1, new long[,,] { { { 0 } }, { { 0 } } }, 2) != 100)
        {
            Console.WriteLine("test_373() failed.");
            return 473;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_380(&loc_x) != 100)
        {
            Console.WriteLine("test_380() failed.");
            return 480;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_387((byte)(((long)&loc_x.m_b) - ((long)&loc_x.m_a)), &loc_x.m_a) != 100)
        {
            Console.WriteLine("test_387() failed.");
            return 487;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_394(&loc_x.m_b, &loc_x.m_b) != 100)
        {
            Console.WriteLine("test_394() failed.");
            return 494;
        }
        Console.WriteLine("All tests passed.");
        return 100;
    }
}
}
