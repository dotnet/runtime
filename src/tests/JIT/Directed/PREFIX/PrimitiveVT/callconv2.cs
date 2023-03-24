// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;
namespace PrimitiveVT
{

    public unsafe class CallConv2
    {
        public const int DefaultSeed = 20010415;
        public static int Seed = Environment.GetEnvironmentVariable("CORECLR_SEED") switch
        {
            string seedStr when seedStr.Equals("random", StringComparison.OrdinalIgnoreCase) => new Random().Next(),
            string seedStr when int.TryParse(seedStr, out int envSeed) => envSeed,
            _ => DefaultSeed
        };

        static Random rand = new Random(Seed);
        VT2A vt1a;
        static VT2A x;

        static uint f1(VT2B x, VT2B y) { return x.m + y.m; }
        VT2B f2a(VT2A x, VT2B y) { VT2B z; z.m = x.m + y.m; return z; }
        VT2B f2b(VT2A x, VT2B y) { return f2a(x, y); }
        VT2B f2(VT2A x, VT2B y) { return f2b(x, y); }
        static uint f3(ref VT2B x, VT2B y) { return x.m - y.m; }
        VT2B f4(VT2A x, VT2B y) { VT2B z; z.m = x.m - y.m; return z; }
        static uint f5(VT2B x, VT2A y) { return x.m * y.m; }
        uint f6(VT2B[] x, VT2B y) { return x[0].m * y.m; }
        VT2B f7(VT2A x, VT2B y) { return f4(x, y); }
        float f8(VT2A x, VT2B y) { return x.m / y.m; }

        static VT2B[,] f9a() { return new VT2B[1, 2]; }
        static VT2B[,] f9() { return f9a(); }
        uint f10(params VT2B[] args) { uint sum = 0; for (uint i = 0; i < args.Length; sum += args[i], i++) { }; return sum; }


        [Fact]
        public static int TestEntryPoint()
        {
            uint a = (uint)rand.Next();

            CallConv2 t = new CallConv2();
            t.vt1a.m = a;

            VT2B vt1b = (VT2B)t.vt1a;

            uint b = vt1b;
            if (b != a)
            {
                Console.WriteLine("FAILED, b!=a");
                return 1;
            }

            uint c = (uint)(VT2B)(VT2A)(VT2B)(uint)(VT2B)t.vt1a;
            if (c != b)
            {
                Console.WriteLine("FAILED, c!=b");
                return 1;
            }

            uint d = (uint)rand.Next();
            uint e = UInt32.MinValue + 2;
            uint f = UInt32.MaxValue / 2;
            x = new VT2A();
            VT2B[] yarr = new VT2B[2];
            yarr[0] = new VT2B(e);
            VT2B y = yarr[0];
            x.m = d;
            VT2B u = x * y - (new VT2B(f)) + yarr[0] + (VT2B)x + (VT2B)f + y * x + (uint)(x / (d % 2 == 0 ? (VT2B)(d / 2) : (VT2B)(d + 1 / 2)));
            uint w = f5((VT2B)x, (VT2A)y) + t.f6(yarr, (VT2B)x) + f1(y, d) + (uint)t.f8((VT2A)(VT2B)d, (d % 2 == 0 ? (VT2B)(d / 2) : (VT2B)(d + 1 / 2)));
            if (u != w)
            {
                Console.WriteLine("FAILED, u!=w");
                Console.WriteLine(u);
                Console.WriteLine(w);
                return 1;
            }

            for (VT2B z = 3; z <= 10; z++, t.f2((VT2A)y, 1)) { }

            if (f3(ref y, UInt32.MinValue) != 2)
            {
                Console.WriteLine("FAILED, f3(y,UInt32.MinValue)!=2");
                Console.WriteLine(f3(ref y, UInt32.MinValue));
                return 1;
            }

            VT2B* o = stackalloc VT2B[3];
            o[0] = 1;
            o[1] = 2;
            o[2] = 3;

            if ((t.f7((VT2A)o[2], o[0])) != 2)
            {
                Console.WriteLine("FAILED (t.f7((VT2A)o[2], o[0]))!=2");
                Console.WriteLine(t.f7((VT2A)o[2], o[0]));
                return 1;
            }

            VT2B[][,] arr = new VT2B[2][,];
            arr[1] = f9();
            arr[1][0, 0] = (VT2B)(*o);

            if ((t.f10(arr[1][0, 0])) != 1)
            {
                Console.WriteLine("FAILED (t.f10(arr[1][0,0]))!=1");
                Console.WriteLine(t.f10(arr[1][0, 0]));
                return 1;
            }

            if ((t.f10(arr[1][0, 0], t.f7((VT2A)(new VT2B(2)), (VT2B)o[0]), 4)) != 6)
            {
                Console.WriteLine("FAILED (t.f10(arr[1][0,0], t.f7((VT2A)(new VT2B(2)), (VT2B)o[0]), 4))!=6");
                Console.WriteLine(t.f10(arr[1][0, 0], t.f7((VT2A)(new VT2B(2)), (VT2B)o[0]), 4));
                return 1;
            }

            Console.WriteLine("PASSED");
            return 100;
        }

    }

}

