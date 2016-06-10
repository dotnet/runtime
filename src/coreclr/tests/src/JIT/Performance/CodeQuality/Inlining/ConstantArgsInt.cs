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

public static class ConstantArgsInt
{

#if DEBUG
    public const int Iterations = 1;
#else
    public const int Iterations = 100000;
#endif

    // Ints feeding math operations.
    //
    // Inlining in Bench0xp should enable constant folding
    // Inlining in Bench0xn will not enable constant folding

    static int Five = 5;
    static int Ten = 10;

    static int Id(int x)
    {
        return x;
    }

    static int F00(int x)
    {
        return x * x;
    }

    static bool Bench00p()
    {
        int t = 10;
        int f = F00(t);
        return (f == 100);
    }

    static bool Bench00n()
    {
        int t = Ten;
        int f = F00(t);
        return (f == 100);
    }

    static bool Bench00p1()
    {
        int t = Id(10);
        int f = F00(t);
        return (f == 100);
    }

    static bool Bench00n1()
    {
        int t = Id(Ten);
        int f = F00(t);
        return (f == 100);
    }

    static bool Bench00p2()
    {
        int t = Id(10);
        int f = F00(Id(t));
        return (f == 100);
    }

    static bool Bench00n2()
    {
        int t = Id(Ten);
        int f = F00(Id(t));
        return (f == 100);
    }

    static bool Bench00p3()
    {
        int t = 10;
        int f = F00(Id(Id(t)));
        return (f == 100);
    }

    static bool Bench00n3()
    {
        int t = Ten;
        int f = F00(Id(Id(t)));
        return (f == 100);
    }

    static bool Bench00p4()
    {
        int t = 5;
        int f = F00(2 * t);
        return (f == 100);
    }

    static bool Bench00n4()
    {
        int t = Five;
        int f = F00(2 * t);
        return (f == 100);
    }

    static int F01(int x)
    {
        return 1000 / x;
    }

    static bool Bench01p()
    {
        int t = 10;
        int f = F01(t);
        return (f == 100);
    }

    static bool Bench01n()
    {
        int t = Ten;
        int f = F01(t);
        return (f == 100);
    }

    static int F02(int x)
    {
        return 20 * (x / 2);
    }

    static bool Bench02p()
    {
        int t = 10;
        int f = F02(t);
        return (f == 100);
    }

    static bool Bench02n()
    {
        int t = Ten;
        int f = F02(t);
        return (f == 100);
    }

    static int F03(int x)
    {
        return 91 + 1009 % x;
    }

    static bool Bench03p()
    {
        int t = 10;
        int f = F03(t);
        return (f == 100);
    }

    static bool Bench03n()
    {
        int t = Ten;
        int f = F03(t);
        return (f == 100);
    }

    static int F04(int x)
    {
        return 50 * (x % 4);
    }

    static bool Bench04p()
    {
        int t = 10;
        int f = F04(t);
        return (f == 100);
    }

    static bool Bench04n()
    {
        int t = Ten;
        int f = F04(t);
        return (f == 100);
    }

    static int F05(int x)
    {
        return (1 << x) - 924;
    }

    static bool Bench05p()
    {
        int t = 10;
        int f = F05(t);
        return (f == 100);
    }

    static bool Bench05n()
    {
        int t = Ten;
        int f = F05(t);
        return (f == 100);
    }

    static int F051(int x)
    {
        return (102400 >> x);
    }

    static bool Bench05p1()
    {
        int t = 10;
        int f = F051(t);
        return (f == 100);
    }

    static bool Bench05n1()
    {
        int t = Ten;
        int f = F051(t);
        return (f == 100);
    }

    static int F06(int x)
    {
        return -x + 110;
    }

    static bool Bench06p()
    {
        int t = 10;
        int f = F06(t);
        return (f == 100);
    }

    static bool Bench06n()
    {
        int t = Ten;
        int f = F06(t);
        return (f == 100);
    }

    static int F07(int x)
    {
        return ~x + 111;
    }

    static bool Bench07p()
    {
        int t = 10;
        int f = F07(t);
        return (f == 100);
    }

    static bool Bench07n()
    {
        int t = Ten;
        int f = F07(t);
        return (f == 100);
    }

    static int F071(int x)
    {
        return (x ^ -1) + 111;
    }

    static bool Bench07p1()
    {
        int t = 10;
        int f = F071(t);
        return (f == 100);
    }

    static bool Bench07n1()
    {
        int t = Ten;
        int f = F071(t);
        return (f == 100);
    }

    static int F08(int x)
    {
        return (x & 0x7) + 98;
    }

    static bool Bench08p()
    {
        int t = 10;
        int f = F08(t);
        return (f == 100);
    }

    static bool Bench08n()
    {
        int t = Ten;
        int f = F08(t);
        return (f == 100);
    }

    static int F09(int x)
    {
        return (x | 0x7) + 85;
    }

    static bool Bench09p()
    {
        int t = 10;
        int f = F09(t);
        return (f == 100);
    }

    static bool Bench09n()
    {
        int t = Ten;
        int f = F09(t);
        return (f == 100);
    }

    // Ints feeding comparisons.
    //
    // Inlining in Bench1xp should enable branch optimization
    // Inlining in Bench1xn will not enable branch optimization

    static int F10(int x)
    {
        return x == 10 ? 100 : 0;
    }

    static bool Bench10p()
    {
        int t = 10;
        int f = F10(t);
        return (f == 100);
    }

    static bool Bench10n()
    {
        int t = Ten;
        int f = F10(t);
        return (f == 100);
    }

    static int F101(int x)
    {
        return x != 10 ? 0 : 100;
    }

    static bool Bench10p1()
    {
        int t = 10;
        int f = F101(t);
        return (f == 100);
    }

    static bool Bench10n1()
    {
        int t = Ten;
        int f = F101(t);
        return (f == 100);
    }

    static int F102(int x)
    {
        return x >= 10 ? 100 : 0;
    }

    static bool Bench10p2()
    {
        int t = 10;
        int f = F102(t);
        return (f == 100);
    }

    static bool Bench10n2()
    {
        int t = Ten;
        int f = F102(t);
        return (f == 100);
    }

    static int F103(int x)
    {
        return x <= 10 ? 100 : 0;
    }

    static bool Bench10p3()
    {
        int t = 10;
        int f = F103(t);
        return (f == 100);
    }

    static bool Bench10n3()
    {
        int t = Ten;
        int f = F102(t);
        return (f == 100);
    }

    static int F11(int x)
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
        int t = 10;
        int f = F11(t);
        return (f == 100);
    }

    static bool Bench11n()
    {
        int t = Ten;
        int f = F11(t);
        return (f == 100);
    }

    static int F111(int x)
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
        int t = 10;
        int f = F111(t);
        return (f == 100);
    }

    static bool Bench11n1()
    {
        int t = Ten;
        int f = F111(t);
        return (f == 100);
    }

    static int F112(int x)
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
        int t = 10;
        int f = F112(t);
        return (f == 100);
    }

    static bool Bench11n2()
    {
        int t = Ten;
        int f = F112(t);
        return (f == 100);
    }
    static int F113(int x)
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
        int t = 10;
        int f = F113(t);
        return (f == 100);
    }

    static bool Bench11n3()
    {
        int t = Ten;
        int f = F113(t);
        return (f == 100);
    }

    // Ununsed (or effectively unused) parameters
    //
    // Simple callee analysis may overstate inline benefit

    static int F20(int x)
    {
        return 100;
    }

    static bool Bench20p()
    {
        int t = 10;
        int f = F20(t);
        return (f == 100);
    }

    static bool Bench20p1()
    {
        int t = Ten;
        int f = F20(t);
        return (f == 100);
    }

    static int F21(int x)
    {
        return -x + 100 + x;
    }

    static bool Bench21p()
    {
        int t = 10;
        int f = F21(t);
        return (f == 100);
    }

    static bool Bench21n()
    {
        int t = Ten;
        int f = F21(t);
        return (f == 100);
    }

    static int F211(int x)
    {
        return x - x + 100;
    }

    static bool Bench21p1()
    {
        int t = 10;
        int f = F211(t);
        return (f == 100);
    }

    static bool Bench21n1()
    {
        int t = Ten;
        int f = F211(t);
        return (f == 100);
    }

    static int F22(int x)
    {
        if (x > 0)
        {
            return 100;
        }

        return 100;
    }

    static bool Bench22p()
    {
        int t = 10;
        int f = F22(t);
        return (f == 100);
    }

    static bool Bench22p1()
    {
        int t = Ten;
        int f = F22(t);
        return (f == 100);
    }

    static int F23(int x)
    {
        if (x > 0)
        {
            return 90 + x;
        }

        return 100;
    }

    static bool Bench23p()
    {
        int t = 10;
        int f = F23(t);
        return (f == 100);
    }

    static bool Bench23n()
    {
        int t = Ten;
        int f = F23(t);
        return (f == 100);
    }

    // Multiple parameters

    static int F30(int x, int y)
    {
        return y * y;
    }

    static bool Bench30p()
    {
        int t = 10;
        int f = F30(t, t);
        return (f == 100);
    }

    static bool Bench30n()
    {
        int t = Ten;
        int f = F30(t, t);
        return (f == 100);
    }

    static bool Bench30p1()
    {
        int s = Ten;
        int t = 10;
        int f = F30(s, t);
        return (f == 100);
    }

    static bool Bench30n1()
    {
        int s = 10;
        int t = Ten;
        int f = F30(s, t);
        return (f == 100);
    }

    static bool Bench30p2()
    {
        int s = 10;
        int t = 10;
        int f = F30(s, t);
        return (f == 100);
    }

    static bool Bench30n2()
    {
        int s = Ten;
        int t = Ten;
        int f = F30(s, t);
        return (f == 100);
    }

    static bool Bench30p3()
    {
        int s = 10;
        int t = s;
        int f = F30(s, t);
        return (f == 100);
    }

    static bool Bench30n3()
    {
        int s = Ten;
        int t = s;
        int f = F30(s, t);
        return (f == 100);
    }

    static int F31(int x, int y, int z)
    {
        return z * z;
    }

    static bool Bench31p()
    {
        int t = 10;
        int f = F31(t, t, t);
        return (f == 100);
    }

    static bool Bench31n()
    {
        int t = Ten;
        int f = F31(t, t, t);
        return (f == 100);
    }

    static bool Bench31p1()
    {
        int r = Ten;
        int s = Ten;
        int t = 10;
        int f = F31(r, s, t);
        return (f == 100);
    }

    static bool Bench31n1()
    {
        int r = 10;
        int s = 10;
        int t = Ten;
        int f = F31(r, s, t);
        return (f == 100);
    }

    static bool Bench31p2()
    {
        int r = 10;
        int s = 10;
        int t = 10;
        int f = F31(r, s, t);
        return (f == 100);
    }

    static bool Bench31n2()
    {
        int r = Ten;
        int s = Ten;
        int t = Ten;
        int f = F31(r, s, t);
        return (f == 100);
    }

    static bool Bench31p3()
    {
        int r = 10;
        int s = r;
        int t = s;
        int f = F31(r, s, t);
        return (f == 100);
    }

    static bool Bench31n3()
    {
        int r = Ten;
        int s = r;
        int t = s;
        int f = F31(r, s, t);
        return (f == 100);
    }

    // Two args, both used

    static int F40(int x, int y)
    {
        return x * x + y * y - 100;
    }

    static bool Bench40p()
    {
        int t = 10;
        int f = F40(t, t);
        return (f == 100);
    }

    static bool Bench40n()
    {
        int t = Ten;
        int f = F40(t, t);
        return (f == 100);
    }

    static bool Bench40p1()
    {
        int s = Ten;
        int t = 10;
        int f = F40(s, t);
        return (f == 100);
    }

    static bool Bench40p2()
    {
        int s = 10;
        int t = Ten;
        int f = F40(s, t);
        return (f == 100);
    }

    static int F41(int x, int y)
    {
        return x * y;
    }

    static bool Bench41p()
    {
        int t = 10;
        int f = F41(t, t);
        return (f == 100);
    }

    static bool Bench41n()
    {
        int t = Ten;
        int f = F41(t, t);
        return (f == 100);
    }

    static bool Bench41p1()
    {
        int s = 10;
        int t = Ten;
        int f = F41(s, t);
        return (f == 100);
    }

    static bool Bench41p2()
    {
        int s = Ten;
        int t = 10;
        int f = F41(s, t);
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
        TypeInfo t = typeof(ConstantArgsInt).GetTypeInfo();
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
