// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xunit.Performance;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Collections.Generic;
using Xunit;

[assembly: OptimizeForBenchmarks]
[assembly: MeasureInstructionsRetired]

public static class ConstantArgsLong
{

#if DEBUG
    public const int Iterations = 1;
#else
    public const int Iterations = 100000;
#endif

    // Longs feeding math operations.
    //
    // Inlining in Bench0xp should enable constant folding
    // Inlining in Bench0xn will not enable constant folding

    static long Five = 5;
    static long Ten = 10;

    static long Id(long x)
    {
        return x;
    }

    static long F00(long x)
    {
        return x * x;
    }

    static bool Bench00p()
    {
        long t = 10;
        long f = F00(t);
        return (f == 100);
    }

    static bool Bench00n()
    {
        long t = Ten;
        long f = F00(t);
        return (f == 100);
    }

    static bool Bench00p1()
    {
        long t = Id(10);
        long f = F00(t);
        return (f == 100);
    }

    static bool Bench00n1()
    {
        long t = Id(Ten);
        long f = F00(t);
        return (f == 100);
    }

    static bool Bench00p2()
    {
        long t = Id(10);
        long f = F00(Id(t));
        return (f == 100);
    }

    static bool Bench00n2()
    {
        long t = Id(Ten);
        long f = F00(Id(t));
        return (f == 100);
    }

    static bool Bench00p3()
    {
        long t = 10;
        long f = F00(Id(Id(t)));
        return (f == 100);
    }

    static bool Bench00n3()
    {
        long t = Ten;
        long f = F00(Id(Id(t)));
        return (f == 100);
    }

    static bool Bench00p4()
    {
        long t = 5;
        long f = F00(2 * t);
        return (f == 100);
    }

    static bool Bench00n4()
    {
        long t = Five;
        long f = F00(2 * t);
        return (f == 100);
    }

    static long F01(long x)
    {
        return 1000 / x;
    }

    static bool Bench01p()
    {
        long t = 10;
        long f = F01(t);
        return (f == 100);
    }

    static bool Bench01n()
    {
        long t = Ten;
        long f = F01(t);
        return (f == 100);
    }

    static long F02(long x)
    {
        return 20 * (x / 2);
    }

    static bool Bench02p()
    {
        long t = 10;
        long f = F02(t);
        return (f == 100);
    }

    static bool Bench02n()
    {
        long t = Ten;
        long f = F02(t);
        return (f == 100);
    }

    static long F03(long x)
    {
        return 91 + 1009 % x;
    }

    static bool Bench03p()
    {
        long t = 10;
        long f = F03(t);
        return (f == 100);
    }

    static bool Bench03n()
    {
        long t = Ten;
        long f = F03(t);
        return (f == 100);
    }

    static long F04(long x)
    {
        return 50 * (x % 4);
    }

    static bool Bench04p()
    {
        long t = 10;
        long f = F04(t);
        return (f == 100);
    }

    static bool Bench04n()
    {
        long t = Ten;
        long f = F04(t);
        return (f == 100);
    }

    static long F05(long x)
    {
        return (1 << (int) x) - 924;
    }

    static bool Bench05p()
    {
        long t = 10;
        long f = F05(t);
        return (f == 100);
    }

    static bool Bench05n()
    {
        long t = Ten;
        long f = F05(t);
        return (f == 100);
    }

    static long F051(long x)
    {
        return (102400 >> (int) x);
    }

    static bool Bench05p1()
    {
        long t = 10;
        long f = F051(t);
        return (f == 100);
    }

    static bool Bench05n1()
    {
        long t = Ten;
        long f = F051(t);
        return (f == 100);
    }

    static long F06(long x)
    {
        return -x + 110;
    }

    static bool Bench06p()
    {
        long t = 10;
        long f = F06(t);
        return (f == 100);
    }

    static bool Bench06n()
    {
        long t = Ten;
        long f = F06(t);
        return (f == 100);
    }

    static long F07(long x)
    {
        return ~x + 111;
    }

    static bool Bench07p()
    {
        long t = 10;
        long f = F07(t);
        return (f == 100);
    }

    static bool Bench07n()
    {
        long t = Ten;
        long f = F07(t);
        return (f == 100);
    }

    static long F071(long x)
    {
        return (x ^ -1) + 111;
    }

    static bool Bench07p1()
    {
        long t = 10;
        long f = F071(t);
        return (f == 100);
    }

    static bool Bench07n1()
    {
        long t = Ten;
        long f = F071(t);
        return (f == 100);
    }

    static long F08(long x)
    {
        return (x & 0x7) + 98;
    }

    static bool Bench08p()
    {
        long t = 10;
        long f = F08(t);
        return (f == 100);
    }

    static bool Bench08n()
    {
        long t = Ten;
        long f = F08(t);
        return (f == 100);
    }

    static long F09(long x)
    {
        return (x | 0x7) + 85;
    }

    static bool Bench09p()
    {
        long t = 10;
        long f = F09(t);
        return (f == 100);
    }

    static bool Bench09n()
    {
        long t = Ten;
        long f = F09(t);
        return (f == 100);
    }

    // Longs feeding comparisons.
    //
    // Inlining in Bench1xp should enable branch optimization
    // Inlining in Bench1xn will not enable branch optimization

    static long F10(long x)
    {
        return x == 10 ? 100 : 0;
    }

    static bool Bench10p()
    {
        long t = 10;
        long f = F10(t);
        return (f == 100);
    }

    static bool Bench10n()
    {
        long t = Ten;
        long f = F10(t);
        return (f == 100);
    }

    static long F101(long x)
    {
        return x != 10 ? 0 : 100;
    }

    static bool Bench10p1()
    {
        long t = 10;
        long f = F101(t);
        return (f == 100);
    }

    static bool Bench10n1()
    {
        long t = Ten;
        long f = F101(t);
        return (f == 100);
    }

    static long F102(long x)
    {
        return x >= 10 ? 100 : 0;
    }

    static bool Bench10p2()
    {
        long t = 10;
        long f = F102(t);
        return (f == 100);
    }

    static bool Bench10n2()
    {
        long t = Ten;
        long f = F102(t);
        return (f == 100);
    }

    static long F103(long x)
    {
        return x <= 10 ? 100 : 0;
    }

    static bool Bench10p3()
    {
        long t = 10;
        long f = F103(t);
        return (f == 100);
    }

    static bool Bench10n3()
    {
        long t = Ten;
        long f = F102(t);
        return (f == 100);
    }

    static long F11(long x)
    {
        if (x == 10)
        {
            return 100;
        }
        else
        {
            return 0;
        }
    }

    static bool Bench11p()
    {
        long t = 10;
        long f = F11(t);
        return (f == 100);
    }

    static bool Bench11n()
    {
        long t = Ten;
        long f = F11(t);
        return (f == 100);
    }

    static long F111(long x)
    {
        if (x != 10)
        {
            return 0;
        }
        else
        {
            return 100;
        }
    }

    static bool Bench11p1()
    {
        long t = 10;
        long f = F111(t);
        return (f == 100);
    }

    static bool Bench11n1()
    {
        long t = Ten;
        long f = F111(t);
        return (f == 100);
    }

    static long F112(long x)
    {
        if (x > 10)
        {
            return 0;
        }
        else
        {
            return 100;
        }
    }

    static bool Bench11p2()
    {
        long t = 10;
        long f = F112(t);
        return (f == 100);
    }

    static bool Bench11n2()
    {
        long t = Ten;
        long f = F112(t);
        return (f == 100);
    }
    static long F113(long x)
    {
        if (x < 10)
        {
            return 0;
        }
        else
        {
            return 100;
        }
    }

    static bool Bench11p3()
    {
        long t = 10;
        long f = F113(t);
        return (f == 100);
    }

    static bool Bench11n3()
    {
        long t = Ten;
        long f = F113(t);
        return (f == 100);
    }

    // Ununsed (or effectively unused) parameters
    //
    // Simple callee analysis may overstate inline benefit

    static long F20(long x)
    {
        return 100;
    }

    static bool Bench20p()
    {
        long t = 10;
        long f = F20(t);
        return (f == 100);
    }

    static bool Bench20p1()
    {
        long t = Ten;
        long f = F20(t);
        return (f == 100);
    }

    static long F21(long x)
    {
        return -x + 100 + x;
    }

    static bool Bench21p()
    {
        long t = 10;
        long f = F21(t);
        return (f == 100);
    }

    static bool Bench21n()
    {
        long t = Ten;
        long f = F21(t);
        return (f == 100);
    }

    static long F211(long x)
    {
        return x - x + 100;
    }

    static bool Bench21p1()
    {
        long t = 10;
        long f = F211(t);
        return (f == 100);
    }

    static bool Bench21n1()
    {
        long t = Ten;
        long f = F211(t);
        return (f == 100);
    }

    static long F22(long x)
    {
        if (x > 0)
        {
            return 100;
        }

        return 100;
    }

    static bool Bench22p()
    {
        long t = 10;
        long f = F22(t);
        return (f == 100);
    }

    static bool Bench22p1()
    {
        long t = Ten;
        long f = F22(t);
        return (f == 100);
    }

    static long F23(long x)
    {
        if (x > 0)
        {
            return 90 + x;
        }

        return 100;
    }

    static bool Bench23p()
    {
        long t = 10;
        long f = F23(t);
        return (f == 100);
    }

    static bool Bench23n()
    {
        long t = Ten;
        long f = F23(t);
        return (f == 100);
    }

    // Multiple parameters

    static long F30(long x, long y)
    {
        return y * y;
    }

    static bool Bench30p()
    {
        long t = 10;
        long f = F30(t, t);
        return (f == 100);
    }

    static bool Bench30n()
    {
        long t = Ten;
        long f = F30(t, t);
        return (f == 100);
    }

    static bool Bench30p1()
    {
        long s = Ten;
        long t = 10;
        long f = F30(s, t);
        return (f == 100);
    }

    static bool Bench30n1()
    {
        long s = 10;
        long t = Ten;
        long f = F30(s, t);
        return (f == 100);
    }

    static bool Bench30p2()
    {
        long s = 10;
        long t = 10;
        long f = F30(s, t);
        return (f == 100);
    }

    static bool Bench30n2()
    {
        long s = Ten;
        long t = Ten;
        long f = F30(s, t);
        return (f == 100);
    }

    static bool Bench30p3()
    {
        long s = 10;
        long t = s;
        long f = F30(s, t);
        return (f == 100);
    }

    static bool Bench30n3()
    {
        long s = Ten;
        long t = s;
        long f = F30(s, t);
        return (f == 100);
    }

    static long F31(long x, long y, long z)
    {
        return z * z;
    }

    static bool Bench31p()
    {
        long t = 10;
        long f = F31(t, t, t);
        return (f == 100);
    }

    static bool Bench31n()
    {
        long t = Ten;
        long f = F31(t, t, t);
        return (f == 100);
    }

    static bool Bench31p1()
    {
        long r = Ten;
        long s = Ten;
        long t = 10;
        long f = F31(r, s, t);
        return (f == 100);
    }

    static bool Bench31n1()
    {
        long r = 10;
        long s = 10;
        long t = Ten;
        long f = F31(r, s, t);
        return (f == 100);
    }

    static bool Bench31p2()
    {
        long r = 10;
        long s = 10;
        long t = 10;
        long f = F31(r, s, t);
        return (f == 100);
    }

    static bool Bench31n2()
    {
        long r = Ten;
        long s = Ten;
        long t = Ten;
        long f = F31(r, s, t);
        return (f == 100);
    }

    static bool Bench31p3()
    {
        long r = 10;
        long s = r;
        long t = s;
        long f = F31(r, s, t);
        return (f == 100);
    }

    static bool Bench31n3()
    {
        long r = Ten;
        long s = r;
        long t = s;
        long f = F31(r, s, t);
        return (f == 100);
    }

    // Two args, both used

    static long F40(long x, long y)
    {
        return x * x + y * y - 100;
    }

    static bool Bench40p()
    {
        long t = 10;
        long f = F40(t, t);
        return (f == 100);
    }

    static bool Bench40n()
    {
        long t = Ten;
        long f = F40(t, t);
        return (f == 100);
    }

    static bool Bench40p1()
    {
        long s = Ten;
        long t = 10;
        long f = F40(s, t);
        return (f == 100);
    }

    static bool Bench40p2()
    {
        long s = 10;
        long t = Ten;
        long f = F40(s, t);
        return (f == 100);
    }

    static long F41(long x, long y)
    {
        return x * y;
    }

    static bool Bench41p()
    {
        long t = 10;
        long f = F41(t, t);
        return (f == 100);
    }

    static bool Bench41n()
    {
        long t = Ten;
        long f = F41(t, t);
        return (f == 100);
    }

    static bool Bench41p1()
    {
        long s = 10;
        long t = Ten;
        long f = F41(s, t);
        return (f == 100);
    }

    static bool Bench41p2()
    {
        long s = Ten;
        long t = 10;
        long f = F41(s, t);
        return (f == 100);
    }

    private static IEnumerable<object[]> MakeArgs(params string[] args)
    {
        return args.Select(arg => new object[] { arg });
    }

    public static IEnumerable<object[]> TestFuncs = MakeArgs(
        "Bench00p",  "Bench00n",
        "Bench00p1", "Bench00n1",
        "Bench00p2", "Bench00n2",
        "Bench00p3", "Bench00n3",
        "Bench00p4", "Bench00n4",
        "Bench01p",  "Bench01n",
        "Bench02p",  "Bench02n",
        "Bench03p",  "Bench03n",
        "Bench04p",  "Bench04n",
        "Bench05p",  "Bench05n",
        "Bench05p1", "Bench05n1",
        "Bench06p",  "Bench06n",
        "Bench07p",  "Bench07n",
        "Bench07p1", "Bench07n1",
        "Bench08p",  "Bench08n",
        "Bench09p",  "Bench09n",
        "Bench10p",  "Bench10n",
        "Bench10p1", "Bench10n1",
        "Bench10p2", "Bench10n2",
        "Bench10p3", "Bench10n3",
        "Bench11p",  "Bench11n",
        "Bench11p1", "Bench11n1",
        "Bench11p2", "Bench11n2",
        "Bench11p3", "Bench11n3",
        "Bench20p",  "Bench20p1",
        "Bench21p",  "Bench21n",
        "Bench21p1", "Bench21n1",
        "Bench22p",  "Bench22p1",
        "Bench23p",  "Bench23n",
        "Bench30p",  "Bench30n",
        "Bench30p1", "Bench30n1",
        "Bench30p2", "Bench30n2",
        "Bench30p3", "Bench30n3",
        "Bench31p",  "Bench31n",
        "Bench31p1", "Bench31n1",
        "Bench31p2", "Bench31n2",
        "Bench31p3", "Bench31n3",
        "Bench40p",  "Bench40n",
        "Bench40p1", "Bench40p2",
        "Bench41p",  "Bench41n",
        "Bench41p1", "Bench41p2"
    );

    static Func<bool> LookupFunc(object o)
    {
        TypeInfo t = typeof(ConstantArgsLong).GetTypeInfo();
        MethodInfo m = t.GetDeclaredMethod((string) o);
        return m.CreateDelegate(typeof(Func<bool>)) as Func<bool>;
    }

    [Benchmark]
    [MemberData(nameof(TestFuncs))]
    public static void Test(object funcName)
    {
        Func<bool> f = LookupFunc(funcName);
        foreach (var iteration in Benchmark.Iterations)
        {
            using (iteration.StartMeasurement())
            {
                for (int i = 0; i < Iterations; i++)
                {
                    f();
                }
            }
        }
    }

    static bool TestBase(Func<bool> f)
    {
        bool result = true;
        for (int i = 0; i < Iterations; i++)
        {
            result &= f();
        }
        return result;
    }

    public static int Main()
    {
        bool result = true;

        foreach(object[] o in TestFuncs)
        {
            string funcName = (string) o[0];
            Func<bool> func = LookupFunc(funcName);
            bool thisResult = TestBase(func);
            if (!thisResult)
            {
                Console.WriteLine("{0} failed", funcName);
            }
            result &= thisResult;
        }

        return (result ? 100 : -1);
    }
}
