// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace PrimitiveVT
{
    public unsafe class CallConv1
    {
        public const int DefaultSeed = 20010415;
        public static int Seed = Environment.GetEnvironmentVariable("CORECLR_SEED") switch
        {
            string seedStr when seedStr.Equals("random", StringComparison.OrdinalIgnoreCase) => new Random().Next(),
            string seedStr when int.TryParse(seedStr, out int envSeed) => envSeed,
            _ => DefaultSeed
        };

        private static Random s_rand = new Random(Seed);
        private VT1A _vt1a;
        private static VT1A s_x;

        private static int f1(VT1B x, VT1B y) { return x.m + y.m; }
        private VT1B f2a(VT1A x, VT1B y) { VT1B z; z.m = x.m + y.m; return z; }
        private VT1B f2b(VT1A x, VT1B y) { return f2a(x, y); }
        private VT1B f2(VT1A x, VT1B y) { return f2b(x, y); }
        private static int f3(ref VT1B x, VT1B y) { return x.m - y.m; }
        private VT1B f4(VT1A x, VT1B y) { VT1B z; z.m = x.m - y.m; return z; }
        private static int f5(VT1B x, VT1A y) { return x.m * y.m; }
        private int f6(VT1B[] x, VT1B y) { return x[0].m * y.m; }
        private VT1B f7(VT1A x, VT1B y) { return f4(x, y); }
        private float f8(VT1A x, VT1B y) { return x.m / y.m; }

        private static VT1B[,] f9a() { return new VT1B[1, 2]; }
        private static VT1B[,] f9() { return f9a(); }
        private int f10(params VT1B[] args) { int sum = 0; for (int i = 0; i < args.Length; sum += args[i], i++) { }; return sum; }


        [Fact]
        public static int TestEntryPoint()
        {
            int a = s_rand.Next();

            CallConv1 t = new CallConv1();
            t._vt1a.m = a;

            VT1B vt1b = (VT1B)t._vt1a;

            int b = vt1b;
            if (b != a)
            {
                Console.WriteLine("FAILED, b!=a");
                return 1;
            }

            int c = (int)(VT1B)(VT1A)(VT1B)(int)(VT1B)t._vt1a;
            if (c != b)
            {
                Console.WriteLine("FAILED, c!=b");
                return 1;
            }

            int d = s_rand.Next();
            int e = Int32.MinValue;
            int f = Int32.MaxValue / 2;
            s_x = new VT1A();
            VT1B[] yarr = new VT1B[2];
            yarr[0] = new VT1B(e);
            VT1B y = yarr[0];
            s_x.m = d;
            VT1B u = s_x * y - (new VT1B(f)) + yarr[0] + (VT1B)s_x + (VT1B)f + y * s_x + (int)(s_x / (d % 2 == 0 ? (VT1B)(d / 2) : (VT1B)(d + 1 / 2)));
            int w = f5((VT1B)s_x, (VT1A)y) + t.f6(yarr, (VT1B)s_x) + f1(y, d) + (int)t.f8((VT1A)(VT1B)d, (d % 2 == 0 ? (VT1B)(d / 2) : (VT1B)(d + 1 / 2)));
            if (u != w)
            {
                Console.WriteLine("FAILED, u!=w");
                Console.WriteLine(u);
                Console.WriteLine(w);
                return 1;
            }

            for (VT1B z = 3; z <= 10; z++, t.f2((VT1A)y, 1)) { }

            if (f3(ref y, Int32.MinValue) != 0)
            {
                Console.WriteLine("FAILED, f3(y,Int32.MinValue)!=0");
                Console.WriteLine(f3(ref y, Int32.MinValue));
                return 1;
            }

            VT1B* o = stackalloc VT1B[3];
            o[0] = 1;
            o[1] = 2;
            o[2] = 3;

            if ((t.f7((VT1A)o[2], o[0])) != 2)
            {
                Console.WriteLine("FAILED (t.f7((VT1A)o[2], o[0]))!=2");
                Console.WriteLine(t.f7((VT1A)o[2], o[0]));
                return 1;
            }

            VT1B[][,] arr = new VT1B[2][,];
            arr[1] = f9();
            arr[1][0, 0] = (VT1B)(*o);

            if ((t.f10(arr[1][0, 0])) != 1)
            {
                Console.WriteLine("FAILED (t.f10(arr[1][0,0]))!=1");
                Console.WriteLine(t.f10(arr[1][0, 0]));
                return 1;
            }

            if ((t.f10(arr[1][0, 0], t.f7((VT1A)(new VT1B(2)), (VT1B)o[0]), 4)) != 6)
            {
                Console.WriteLine("FAILED (t.f10(arr[1][0,0], t.f7((VT1A)(new VT1B(2)), (VT1B)o[0]), 4))!=6");
                Console.WriteLine(t.f10(arr[1][0, 0], t.f7((VT1A)(new VT1B(2)), (VT1B)o[0]), 4));
                return 1;
            }

            Console.WriteLine("PASSED");
            return 100;
        }
    }
}

