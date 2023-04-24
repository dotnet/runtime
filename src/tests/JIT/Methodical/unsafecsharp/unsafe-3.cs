// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using UnsafeCSharp;
using Xunit;

namespace Test_unsafe_3
{
public class TestApp
{
    private static unsafe long test_3(B* pb)
    {
        return (pb--)->m_bval;
    }
    private static unsafe long test_10(AA[,] ab, long i)
    {
        long j = 0;
        fixed (B* pb = &ab[--i, ++j].m_b)
        {
            return pb->m_bval;
        }
    }
    private static unsafe long test_17(B* pb1, long i)
    {
        B* pb;
        return (pb = pb1 + i)->m_bval;
    }
    private static unsafe long test_24(B* pb1, B* pb2)
    {
        return (pb1 > pb2 ? pb2 : null)->m_bval;
    }
    private static unsafe long test_31(long pb)
    {
        return ((B*)pb)->m_bval;
    }
    private static unsafe long test_38(double* pb, long i)
    {
        return ((B*)(pb + i))->m_bval;
    }
    private static unsafe long test_45(ref B b)
    {
        fixed (B* pb = &b)
        {
            return pb[0].m_bval;
        }
    }
    private static unsafe long test_52(B* pb)
    {
        return (--pb)[0].m_bval;
    }
    private static unsafe long test_59(B* pb, long i)
    {
        return (&pb[-(i << (int)i)])[0].m_bval;
    }
    private static unsafe long test_66(AA* px)
    {
        return AA.get_pb(px)[0].m_bval;
    }
    private static unsafe long test_73(long pb)
    {
        return ((B*)checked((long)pb))[0].m_bval;
    }
    private static unsafe long test_80(B* pb)
    {
        return AA.get_bv1((pb++));
    }
    private static unsafe long test_87(B[,] ab, long i, long j)
    {
        fixed (B* pb = &ab[i, j])
        {
            return AA.get_bv1(pb);
        }
    }
    private static unsafe long test_94(B* pb1)
    {
        B* pb;
        return AA.get_bv1((pb = pb1 - 8));
    }
    private static unsafe long test_101(B* pb, B* pb1, B* pb2)
    {
        return AA.get_bv1((pb = pb + (pb2 - pb1)));
    }
    private static unsafe long test_108(B* pb1, bool trig)
    {
        fixed (B* pb = &AA.s_x.m_b)
        {
            return AA.get_bv1((trig ? pb : pb1));
        }
    }
    private static unsafe long test_115(byte* pb)
    {
        return AA.get_bv1(((B*)(pb + 7)));
    }
    private static unsafe long test_122(B b)
    {
        return AA.get_bv2(*(&b));
    }
    private static unsafe long test_129()
    {
        fixed (B* pb = &AA.s_x.m_b)
        {
            return AA.get_bv2(*pb);
        }
    }
    private static unsafe long test_136(B* pb, long i)
    {
        return AA.get_bv2(*(&pb[i * 2]));
    }
    private static unsafe long test_143(B* pb1, B* pb2)
    {
        return AA.get_bv2(*(pb1 >= pb2 ? pb1 : null));
    }
    private static unsafe long test_150(long pb)
    {
        return AA.get_bv2(*((B*)pb));
    }
    private static unsafe long test_157(B* pb)
    {
        return AA.get_bv3(ref *pb);
    }
    private static unsafe long test_164(B[] ab, long i)
    {
        fixed (B* pb = &ab[i])
        {
            return AA.get_bv3(ref *pb);
        }
    }
    private static unsafe long test_171(B* pb)
    {
        return AA.get_bv3(ref *(pb += 6));
    }
    private static unsafe long test_178(B* pb, long[,,] i, long ii)
    {
        return AA.get_bv3(ref *(&pb[++i[--ii, 0, 0]]));
    }
    private static unsafe long test_185(AA* px)
    {
        return AA.get_bv3(ref *((B*)AA.get_pb_i(px)));
    }
    private static unsafe long test_192(byte diff, A* pa)
    {
        return AA.get_bv3(ref *((B*)(((byte*)pa) + diff)));
    }
    private static unsafe long test_199()
    {
        AA loc_x = new AA(0, 100);
        return (&loc_x.m_b)->m_bval == 100 ? 100 : 101;
    }
    private static unsafe long test_206(B[][] ab, long i, long j)
    {
        fixed (B* pb = &ab[i][j])
        {
            return pb->m_bval == 100 ? 100 : 101;
        }
    }
    private static unsafe long test_213(B* pb1, long i)
    {
        B* pb;
        return (pb = (B*)(((byte*)pb1) + i * sizeof(B)))->m_bval == 100 ? 100 : 101;
    }
    private static unsafe long test_220(B* pb, long[,,] i, long ii, byte jj)
    {
        return (&pb[i[ii - jj, 0, ii - jj] = ii - 1])->m_bval == 100 ? 100 : 101;
    }
    private static unsafe long test_227(ulong ub, byte lb)
    {
        return ((B*)(ub | lb))->m_bval == 100 ? 100 : 101;
    }
    private static unsafe long test_234(long p, long s)
    {
        return ((B*)((p >> 4) | s))->m_bval == 100 ? 100 : 101;
    }
    private static unsafe long test_241(B[] ab)
    {
        fixed (B* pb = &ab[0])
        {
            return AA.get_i1(&pb->m_bval);
        }
    }
    private static unsafe long test_248(B* pb)
    {
        return AA.get_i1(&(++pb)->m_bval);
    }
    private static unsafe long test_255(B* pb, long[] i, long ii)
    {
        return AA.get_i1(&(&pb[i[ii]])->m_bval);
    }
    private static unsafe long test_262(AA* px)
    {
        return AA.get_i1(&(AA.get_pb_1(px) + 1)->m_bval);
    }
    private static unsafe long test_269(long pb)
    {
        return AA.get_i1(&((B*)checked(((long)pb) + 1))->m_bval);
    }
    private static unsafe long test_276(B* pb)
    {
        return AA.get_i2((pb--)->m_bval);
    }
    private static unsafe long test_283(AA[,] ab, long i)
    {
        long j = 0;
        fixed (B* pb = &ab[--i, ++j].m_b)
        {
            return AA.get_i2(pb->m_bval);
        }
    }
    private static unsafe long test_290(B* pb1, long i)
    {
        B* pb;
        return AA.get_i2((pb = pb1 + i)->m_bval);
    }
    private static unsafe long test_297(B* pb1, B* pb2)
    {
        return AA.get_i2((pb1 > pb2 ? pb2 : null)->m_bval);
    }
    private static unsafe long test_304(long pb)
    {
        return AA.get_i2(((B*)pb)->m_bval);
    }
    private static unsafe long test_311(double* pb, long i)
    {
        return AA.get_i2(((B*)(pb + i))->m_bval);
    }
    private static unsafe long test_318(ref B b)
    {
        fixed (B* pb = &b)
        {
            return AA.get_i3(ref pb->m_bval);
        }
    }
    private static unsafe long test_325(B* pb)
    {
        return AA.get_i3(ref (--pb)->m_bval);
    }
    private static unsafe long test_332(B* pb, long i)
    {
        return AA.get_i3(ref (&pb[-(i << (int)i)])->m_bval);
    }
    private static unsafe long test_339(AA* px)
    {
        return AA.get_i3(ref AA.get_pb(px)->m_bval);
    }
    private static unsafe long test_346(long pb)
    {
        return AA.get_i3(ref ((B*)checked((long)pb))->m_bval);
    }
    private static unsafe long test_353(B* pb)
    {
        return AA.get_bv1((pb++)) != 100 ? 99 : 100;
    }
    private static unsafe long test_360(B[,] ab, long i, long j)
    {
        fixed (B* pb = &ab[i, j])
        {
            return AA.get_bv1(pb) != 100 ? 99 : 100;
        }
    }
    private static unsafe long test_367(B* pb1)
    {
        B* pb;
        return AA.get_bv1((pb = pb1 - 8)) != 100 ? 99 : 100;
    }
    private static unsafe long test_374(B* pb, B* pb1, B* pb2)
    {
        return AA.get_bv1((pb = pb + (pb2 - pb1))) != 100 ? 99 : 100;
    }
    private static unsafe long test_381(B* pb1, bool trig)
    {
        fixed (B* pb = &AA.s_x.m_b)
        {
            return AA.get_bv1((trig ? pb : pb1)) != 100 ? 99 : 100;
        }
    }
    private static unsafe long test_388(byte* pb)
    {
        return AA.get_bv1(((B*)(pb + 7))) != 100 ? 99 : 100;
    }
    private static unsafe long test_395(B* pb)
    {
        if (pb + 1 > pb) return 100;
        throw new Exception();
    }
    [Fact]
    public static unsafe int TestEntryPoint()
    {
        AA loc_x = new AA(0, 100);
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_3(&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_3() failed.");
            return 103;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_10(new AA[,] { { new AA(), new AA() }, { new AA(), loc_x } }, 2) != 100)
        {
            Console.WriteLine("test_10() failed.");
            return 110;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_17(&loc_x.m_b - 8, 8) != 100)
        {
            Console.WriteLine("test_17() failed.");
            return 117;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_24(&loc_x.m_b + 1, &loc_x.m_b) != 100)
        {
            Console.WriteLine("test_24() failed.");
            return 124;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_31((long)&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_31() failed.");
            return 131;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_38(((double*)(&loc_x.m_b)) - 4, 4) != 100)
        {
            Console.WriteLine("test_38() failed.");
            return 138;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_45(ref loc_x.m_b) != 100)
        {
            Console.WriteLine("test_45() failed.");
            return 145;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_52(&loc_x.m_b + 1) != 100)
        {
            Console.WriteLine("test_52() failed.");
            return 152;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_59(&loc_x.m_b + 2, 1) != 100)
        {
            Console.WriteLine("test_59() failed.");
            return 159;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_66(&loc_x) != 100)
        {
            Console.WriteLine("test_66() failed.");
            return 166;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_73((long)(long)&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_73() failed.");
            return 173;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_80(&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_80() failed.");
            return 180;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_87(new B[,] { { new B(), new B() }, { new B(), loc_x.m_b } }, 1, 1) != 100)
        {
            Console.WriteLine("test_87() failed.");
            return 187;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_94(&loc_x.m_b + 8) != 100)
        {
            Console.WriteLine("test_94() failed.");
            return 194;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_101(&loc_x.m_b - 2, &loc_x.m_b - 1, &loc_x.m_b + 1) != 100)
        {
            Console.WriteLine("test_101() failed.");
            return 201;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_108(&loc_x.m_b, true) != 100)
        {
            Console.WriteLine("test_108() failed.");
            return 208;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_115(((byte*)(&loc_x.m_b)) - 7) != 100)
        {
            Console.WriteLine("test_115() failed.");
            return 215;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_122(loc_x.m_b) != 100)
        {
            Console.WriteLine("test_122() failed.");
            return 222;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_129() != 100)
        {
            Console.WriteLine("test_129() failed.");
            return 229;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_136(&loc_x.m_b - 2, 1) != 100)
        {
            Console.WriteLine("test_136() failed.");
            return 236;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_143(&loc_x.m_b, &loc_x.m_b) != 100)
        {
            Console.WriteLine("test_143() failed.");
            return 243;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_150((long)&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_150() failed.");
            return 250;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_157(&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_157() failed.");
            return 257;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_164(new B[] { new B(), new B(), loc_x.m_b }, 2) != 100)
        {
            Console.WriteLine("test_164() failed.");
            return 264;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_171(&loc_x.m_b - 6) != 100)
        {
            Console.WriteLine("test_171() failed.");
            return 271;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_178(&loc_x.m_b - 1, new long[,,] { { { 0 } }, { { 0 } } }, 2) != 100)
        {
            Console.WriteLine("test_178() failed.");
            return 278;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_185(&loc_x) != 100)
        {
            Console.WriteLine("test_185() failed.");
            return 285;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_192((byte)(((long)&loc_x.m_b) - ((long)&loc_x.m_a)), &loc_x.m_a) != 100)
        {
            Console.WriteLine("test_192() failed.");
            return 292;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_199() != 100)
        {
            Console.WriteLine("test_199() failed.");
            return 299;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_206(new B[][] { new B[] { new B(), new B() }, new B[] { new B(), loc_x.m_b } }, 1, 1) != 100)
        {
            Console.WriteLine("test_206() failed.");
            return 306;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_213(&loc_x.m_b - 8, 8) != 100)
        {
            Console.WriteLine("test_213() failed.");
            return 313;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_220(&loc_x.m_b - 1, new long[,,] { { { 0 } }, { { 0 } } }, 2, 2) != 100)
        {
            Console.WriteLine("test_220() failed.");
            return 320;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_227(((ulong)&loc_x.m_b) & (~(ulong)0xff), unchecked((byte)&loc_x.m_b)) != 100)
        {
            Console.WriteLine("test_227() failed.");
            return 327;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_234(((long)(&loc_x.m_b)) << 4, ((long)(&loc_x.m_b)) & 0xff000000) != 100)
        {
            Console.WriteLine("test_234() failed.");
            return 334;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_241(new B[] { loc_x.m_b }) != 100)
        {
            Console.WriteLine("test_241() failed.");
            return 341;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_248(&loc_x.m_b - 1) != 100)
        {
            Console.WriteLine("test_248() failed.");
            return 348;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_255(&loc_x.m_b - 1, new long[] { 0, 1 }, 1) != 100)
        {
            Console.WriteLine("test_255() failed.");
            return 355;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_262(&loc_x) != 100)
        {
            Console.WriteLine("test_262() failed.");
            return 362;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_269((long)(((long)&loc_x.m_b) - 1)) != 100)
        {
            Console.WriteLine("test_269() failed.");
            return 369;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_276(&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_276() failed.");
            return 376;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_283(new AA[,] { { new AA(), new AA() }, { new AA(), loc_x } }, 2) != 100)
        {
            Console.WriteLine("test_283() failed.");
            return 383;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_290(&loc_x.m_b - 8, 8) != 100)
        {
            Console.WriteLine("test_290() failed.");
            return 390;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_297(&loc_x.m_b + 1, &loc_x.m_b) != 100)
        {
            Console.WriteLine("test_297() failed.");
            return 397;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_304((long)&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_304() failed.");
            return 404;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_311(((double*)(&loc_x.m_b)) - 4, 4) != 100)
        {
            Console.WriteLine("test_311() failed.");
            return 411;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_318(ref loc_x.m_b) != 100)
        {
            Console.WriteLine("test_318() failed.");
            return 418;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_325(&loc_x.m_b + 1) != 100)
        {
            Console.WriteLine("test_325() failed.");
            return 425;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_332(&loc_x.m_b + 2, 1) != 100)
        {
            Console.WriteLine("test_332() failed.");
            return 432;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_339(&loc_x) != 100)
        {
            Console.WriteLine("test_339() failed.");
            return 439;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_346((long)(long)&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_346() failed.");
            return 446;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_353(&loc_x.m_b) != 100)
        {
            Console.WriteLine("test_353() failed.");
            return 453;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_360(new B[,] { { new B(), new B() }, { new B(), loc_x.m_b } }, 1, 1) != 100)
        {
            Console.WriteLine("test_360() failed.");
            return 460;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_367(&loc_x.m_b + 8) != 100)
        {
            Console.WriteLine("test_367() failed.");
            return 467;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_374(&loc_x.m_b - 2, &loc_x.m_b - 1, &loc_x.m_b + 1) != 100)
        {
            Console.WriteLine("test_374() failed.");
            return 474;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_381(&loc_x.m_b, true) != 100)
        {
            Console.WriteLine("test_381() failed.");
            return 481;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_388(((byte*)(&loc_x.m_b)) - 7) != 100)
        {
            Console.WriteLine("test_388() failed.");
            return 488;
        }
        AA.init_all(0);
        loc_x = new AA(0, 100);
        if (test_395((B*)1) != 100)
        {
            Console.WriteLine("test_395() failed.");
            return 495;
        }
        Console.WriteLine("All tests passed.");
        return 100;
    }
}
}
