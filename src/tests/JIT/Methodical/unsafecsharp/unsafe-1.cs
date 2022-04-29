// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using UnsafeCSharp;
using Xunit;

namespace Test_unsafe_1
{
public class TestApp
{
    private static unsafe long test_1(B* pb)
    {
        return pb->m_bval;
    }
    private static unsafe long test_8(B[] ab, long i)
    {
        fixed (B* pb = &ab[i])
        {
            return pb->m_bval;
        }
    }
    private static unsafe long test_15(B* pb)
    {
        return (pb += 6)->m_bval;
    }
    private static unsafe long test_22(B* pb, long[,,] i, long ii)
    {
        return (&pb[++i[--ii, 0, 0]])->m_bval;
    }
    private static unsafe long test_29(AA* px)
    {
        return ((B*)AA.get_pb_i(px))->m_bval;
    }
    private static unsafe long test_36(byte diff, A* pa)
    {
        return ((B*)(((byte*)pa) + diff))->m_bval;
    }
    private static unsafe long test_43()
    {
        AA loc_x = new AA(0, 100);
        return (&loc_x.m_b)[0].m_bval;
    }
    private static unsafe long test_50(B[][] ab, long i, long j)
    {
        fixed (B* pb = &ab[i][j])
        {
            return pb[0].m_bval;
        }
    }
    private static unsafe long test_57(B* pb1, long i)
    {
        B* pb;
        return (pb = (B*)(((byte*)pb1) + i * sizeof(B)))[0].m_bval;
    }
    private static unsafe long test_64(B* pb, long[,,] i, long ii, byte jj)
    {
        return (&pb[i[ii - jj, 0, ii - jj] = ii - 1])[0].m_bval;
    }
    private static unsafe long test_71(ulong ub, byte lb)
    {
        return ((B*)(ub | lb))[0].m_bval;
    }
    private static unsafe long test_78(long p, long s)
    {
        return ((B*)((p >> 4) | s))[0].m_bval;
    }
    private static unsafe long test_85(B[] ab)
    {
        fixed (B* pb = &ab[0])
        {
            return AA.get_bv1(pb);
        }
    }
    private static unsafe long test_92(B* pb)
    {
        return AA.get_bv1((++pb));
    }
    private static unsafe long test_99(B* pb, long[] i, long ii)
    {
        return AA.get_bv1((&pb[i[ii]]));
    }
    private static unsafe long test_106(AA* px)
    {
        return AA.get_bv1((AA.get_pb_1(px) + 1));
    }
    private static unsafe long test_113(long pb)
    {
        return AA.get_bv1(((B*)checked(((long)pb) + 1)));
    }
    private static unsafe long test_120(B* pb)
    {
        return AA.get_bv2(*(pb--));
    }
    private static unsafe long test_127(AA[,] ab, long i)
    {
        long j = 0;
        fixed (B* pb = &ab[--i, ++j].m_b)
        {
            return AA.get_bv2(*pb);
        }
    }
    private static unsafe long test_134(B* pb1, long i)
    {
        B* pb;
        return AA.get_bv2(*(pb = pb1 + i));
    }
    private static unsafe long test_141(B* pb1, B* pb2)
    {
        return AA.get_bv2(*(pb1 > pb2 ? pb2 : null));
    }
    private static unsafe long test_148(long pb)
    {
        return AA.get_bv2(*((B*)pb));
    }
    private static unsafe long test_155(double* pb, long i)
    {
        return AA.get_bv2(*((B*)(pb + i)));
    }
    private static unsafe long test_162(ref B b)
    {
        fixed (B* pb = &b)
        {
            return AA.get_bv3(ref *pb);
        }
    }
    private static unsafe long test_169(B* pb)
    {
        return AA.get_bv3(ref *(--pb));
    }
    private static unsafe long test_176(B* pb, long i)
    {
        return AA.get_bv3(ref *(&pb[-(i << (int)i)]));
    }
    private static unsafe long test_183(AA* px)
    {
        return AA.get_bv3(ref *AA.get_pb(px));
    }
    private static unsafe long test_190(long pb)
    {
        return AA.get_bv3(ref *((B*)checked((long)pb)));
    }
    private static unsafe long test_197(B* pb)
    {
        return (pb++)->m_bval == 100 ? 100 : 101;
    }
    private static unsafe long test_204(B[,] ab, long i, long j)
    {
        fixed (B* pb = &ab[i, j])
        {
            return pb->m_bval == 100 ? 100 : 101;
        }
    }
    private static unsafe long test_211(B* pb1)
    {
        B* pb;
        return (pb = pb1 - 8)->m_bval == 100 ? 100 : 101;
    }
    private static unsafe long test_218(B* pb, B* pb1, B* pb2)
    {
        return (pb = pb + (pb2 - pb1))->m_bval == 100 ? 100 : 101;
    }
    private static unsafe long test_225(B* pb1, bool trig)
    {
        fixed (B* pb = &AA.s_x.m_b)
        {
            return (trig ? pb : pb1)->m_bval == 100 ? 100 : 101;
        }
    }
    private static unsafe long test_232(byte* pb)
    {
        return ((B*)(pb + 7))->m_bval == 100 ? 100 : 101;
    }
    private static unsafe long test_239(B b)
    {
        return AA.get_i1(&(&b)->m_bval);
    }
    private static unsafe long test_246()
    {
        fixed (B* pb = &AA.s_x.m_b)
        {
            return AA.get_i1(&pb->m_bval);
        }
    }
    private static unsafe long test_253(B* pb, long i)
    {
        return AA.get_i1(&(&pb[i * 2])->m_bval);
    }
    private static unsafe long test_260(B* pb1, B* pb2)
    {
        return AA.get_i1(&(pb1 >= pb2 ? pb1 : null)->m_bval);
    }
    private static unsafe long test_267(long pb)
    {
        return AA.get_i1(&((B*)pb)->m_bval);
    }
    private static unsafe long test_274(B* pb)
    {
        return AA.get_i2(pb->m_bval);
    }
    private static unsafe long test_281(B[] ab, long i)
    {
        fixed (B* pb = &ab[i])
        {
            return AA.get_i2(pb->m_bval);
        }
    }
    private static unsafe long test_288(B* pb)
    {
        return AA.get_i2((pb += 6)->m_bval);
    }
    private static unsafe long test_295(B* pb, long[,,] i, long ii)
    {
        return AA.get_i2((&pb[++i[--ii, 0, 0]])->m_bval);
    }
    private static unsafe long test_302(AA* px)
    {
        return AA.get_i2(((B*)AA.get_pb_i(px))->m_bval);
    }
    private static unsafe long test_309(byte diff, A* pa)
    {
        return AA.get_i2(((B*)(((byte*)pa) + diff))->m_bval);
    }
    private static unsafe long test_316()
    {
        AA loc_x = new AA(0, 100);
        return AA.get_i3(ref (&loc_x.m_b)->m_bval);
    }
    private static unsafe long test_323(B[][] ab, long i, long j)
    {
        fixed (B* pb = &ab[i][j])
        {
            return AA.get_i3(ref pb->m_bval);
        }
    }
    private static unsafe long test_330(B* pb1, long i)
    {
        B* pb;
        return AA.get_i3(ref (pb = (B*)(((byte*)pb1) + i * sizeof(B)))->m_bval);
    }
    private static unsafe long test_337(B* pb, long[,,] i, long ii, byte jj)
    {
        return AA.get_i3(ref (&pb[i[ii - jj, 0, ii - jj] = ii - 1])->m_bval);
    }
    private static unsafe long test_344(ulong ub, byte lb)
    {
        return AA.get_i3(ref ((B*)(ub | lb))->m_bval);
    }
    private static unsafe long test_351(long p, long s)
    {
        return AA.get_i3(ref ((B*)((p >> 4) | s))->m_bval);
    }
    private static unsafe long test_358(B[] ab)
    {
        fixed (B* pb = &ab[0])
        {
            return AA.get_bv1(pb) != 100 ? 99 : 100;
        }
    }
    private static unsafe long test_365(B* pb)
    {
        return AA.get_bv1((++pb)) != 100 ? 99 : 100;
    }
    private static unsafe long test_372(B* pb, long[] i, long ii)
    {
        return AA.get_bv1((&pb[i[ii]])) != 100 ? 99 : 100;
    }
    private static unsafe long test_379(AA* px)
    {
        return AA.get_bv1((AA.get_pb_1(px) + 1)) != 100 ? 99 : 100;
    }
    private static unsafe long test_386(long pb)
    {
        return AA.get_bv1(((B*)checked(((long)pb) + 1))) != 100 ? 99 : 100;
    }
    private static unsafe long test_393(B* pb)
    {
        return pb + 1 > pb ? 100 : 101;
    }
    [Fact]
    public static unsafe int TestEntryPoint()
    {
        AA loc_x = new AA(0, 100);
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_1(&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_1() failed.");
            return 101;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_8(new B[] { new B(), new B(), loc_x.m_b }, 2) != 100)
        {
            Console.WriteLine("test_8() failed.");
            return 108;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_15(&loc_x.m_b - 6) != 100)
        {
            Console.WriteLine("test_15() failed.");
            return 115;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_22(&loc_x.m_b - 1, new long[,,] { { { 0 } }, { { 0 } } }, 2) != 100)
        {
            Console.WriteLine("test_22() failed.");
            return 122;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_29(&loc_x) != 100)
        {
            Console.WriteLine("test_29() failed.");
            return 129;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_36((byte)(((long)&loc_x.m_b) - ((long)&loc_x.m_a)), &loc_x.m_a) != 100)
        {
            Console.WriteLine("test_36() failed.");
            return 136;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_43() != 100)
        {
            Console.WriteLine("test_43() failed.");
            return 143;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_50(new B[][] { new B[] { new B(), new B() }, new B[] { new B(), loc_x.m_b } }, 1, 1) != 100)
        {
            Console.WriteLine("test_50() failed.");
            return 150;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_57(&loc_x.m_b - 8, 8) != 100)
        {
            Console.WriteLine("test_57() failed.");
            return 157;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_64(&loc_x.m_b - 1, new long[,,] { { { 0 } }, { { 0 } } }, 2, 2) != 100)
        {
            Console.WriteLine("test_64() failed.");
            return 164;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_71(((ulong)&loc_x.m_b) & (~(ulong)0xff), unchecked((byte)&loc_x.m_b)) != 100)
        {
            Console.WriteLine("test_71() failed.");
            return 171;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_78(((long)(&loc_x.m_b)) << 4, ((long)(&loc_x.m_b)) & 0xff000000) != 100)
        {
            Console.WriteLine("test_78() failed.");
            return 178;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_85(new B[] { loc_x.m_b }) != 100)
        {
            Console.WriteLine("test_85() failed.");
            return 185;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_92(&loc_x.m_b - 1) != 100)
        {
            Console.WriteLine("test_92() failed.");
            return 192;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_99(&loc_x.m_b - 1, new long[] { 0, 1 }, 1) != 100)
        {
            Console.WriteLine("test_99() failed.");
            return 199;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_106(&loc_x) != 100)
        {
            Console.WriteLine("test_106() failed.");
            return 206;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_113((long)(((long)&loc_x.m_b) - 1)) != 100)
        {
            Console.WriteLine("test_113() failed.");
            return 213;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_120(&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_120() failed.");
            return 220;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_127(new AA[,] { { new AA(), new AA() }, { new AA(), loc_x } }, 2) != 100)
        {
            Console.WriteLine("test_127() failed.");
            return 227;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_134(&loc_x.m_b - 8, 8) != 100)
        {
            Console.WriteLine("test_134() failed.");
            return 234;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_141(&loc_x.m_b + 1, &loc_x.m_b) != 100)
        {
            Console.WriteLine("test_141() failed.");
            return 241;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_148((long)&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_148() failed.");
            return 248;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_155(((double*)(&loc_x.m_b)) - 4, 4) != 100)
        {
            Console.WriteLine("test_155() failed.");
            return 255;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_162(ref loc_x.m_b) != 100)
        {
            Console.WriteLine("test_162() failed.");
            return 262;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_169(&loc_x.m_b + 1) != 100)
        {
            Console.WriteLine("test_169() failed.");
            return 269;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_176(&loc_x.m_b + 2, 1) != 100)
        {
            Console.WriteLine("test_176() failed.");
            return 276;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_183(&loc_x) != 100)
        {
            Console.WriteLine("test_183() failed.");
            return 283;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_190((long)(long)&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_190() failed.");
            return 290;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_197(&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_197() failed.");
            return 297;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_204(new B[,] { { new B(), new B() }, { new B(), loc_x.m_b } }, 1, 1) != 100)
        {
            Console.WriteLine("test_204() failed.");
            return 304;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_211(&loc_x.m_b + 8) != 100)
        {
            Console.WriteLine("test_211() failed.");
            return 311;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_218(&loc_x.m_b - 2, &loc_x.m_b - 1, &loc_x.m_b + 1) != 100)
        {
            Console.WriteLine("test_218() failed.");
            return 318;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_225(&loc_x.m_b, true) != 100)
        {
            Console.WriteLine("test_225() failed.");
            return 325;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_232(((byte*)(&loc_x.m_b)) - 7) != 100)
        {
            Console.WriteLine("test_232() failed.");
            return 332;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_239(loc_x.m_b) != 100)
        {
            Console.WriteLine("test_239() failed.");
            return 339;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_246() != 100)
        {
            Console.WriteLine("test_246() failed.");
            return 346;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_253(&loc_x.m_b - 2, 1) != 100)
        {
            Console.WriteLine("test_253() failed.");
            return 353;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_260(&loc_x.m_b, &loc_x.m_b) != 100)
        {
            Console.WriteLine("test_260() failed.");
            return 360;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_267((long)&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_267() failed.");
            return 367;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_274(&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_274() failed.");
            return 374;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_281(new B[] { new B(), new B(), loc_x.m_b }, 2) != 100)
        {
            Console.WriteLine("test_281() failed.");
            return 381;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_288(&loc_x.m_b - 6) != 100)
        {
            Console.WriteLine("test_288() failed.");
            return 388;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_295(&loc_x.m_b - 1, new long[,,] { { { 0 } }, { { 0 } } }, 2) != 100)
        {
            Console.WriteLine("test_295() failed.");
            return 395;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_302(&loc_x) != 100)
        {
            Console.WriteLine("test_302() failed.");
            return 402;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_309((byte)(((long)&loc_x.m_b) - ((long)&loc_x.m_a)), &loc_x.m_a) != 100)
        {
            Console.WriteLine("test_309() failed.");
            return 409;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_316() != 100)
        {
            Console.WriteLine("test_316() failed.");
            return 416;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_323(new B[][] { new B[] { new B(), new B() }, new B[] { new B(), loc_x.m_b } }, 1, 1) != 100)
        {
            Console.WriteLine("test_323() failed.");
            return 423;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_330(&loc_x.m_b - 8, 8) != 100)
        {
            Console.WriteLine("test_330() failed.");
            return 430;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_337(&loc_x.m_b - 1, new long[,,] { { { 0 } }, { { 0 } } }, 2, 2) != 100)
        {
            Console.WriteLine("test_337() failed.");
            return 437;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_344(((ulong)&loc_x.m_b) & (~(ulong)0xff), unchecked((byte)&loc_x.m_b)) != 100)
        {
            Console.WriteLine("test_344() failed.");
            return 444;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_351(((long)(&loc_x.m_b)) << 4, ((long)(&loc_x.m_b)) & 0xff000000) != 100)
        {
            Console.WriteLine("test_351() failed.");
            return 451;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_358(new B[] { loc_x.m_b }) != 100)
        {
            Console.WriteLine("test_358() failed.");
            return 458;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_365(&loc_x.m_b - 1) != 100)
        {
            Console.WriteLine("test_365() failed.");
            return 465;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_372(&loc_x.m_b - 1, new long[] { 0, 1 }, 1) != 100)
        {
            Console.WriteLine("test_372() failed.");
            return 472;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_379(&loc_x) != 100)
        {
            Console.WriteLine("test_379() failed.");
            return 479;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_386((long)(((long)&loc_x.m_b) - 1)) != 100)
        {
            Console.WriteLine("test_386() failed.");
            return 486;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_393((B*)1) != 100)
        {
            Console.WriteLine("test_393() failed.");
            return 493;
        }
        Console.WriteLine("All tests passed.");
        return 100;
    }
}
}
