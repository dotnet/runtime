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

public static class ConstantArgsSByte
{

#if DEBUG
    public const int Iterations = 1;
#else
    public const int Iterations = 100000;
#endif

    // Sbytes feeding math operations.
    //
    // Inlining in Bench0xp should enable constant folding
    // Inlining in Bench0xn will not enable constant folding

    static sbyte Five = 5;
    static sbyte Ten = 10;

    static sbyte Id(sbyte x)
    {
        return x;
    }

    static sbyte F00(sbyte x)
    {
        return (sbyte) (x * x);
    }

    static bool Bench00p()
    {
        sbyte t = 10;
        sbyte f = F00(t);
        return (f == 100);
    }

    static bool Bench00n()
    {
        sbyte t = Ten;
        sbyte f = F00(t);
        return (f == 100);
    }

    static bool Bench00p1()
    {
        sbyte t = Id(10);
        sbyte f = F00(t);
        return (f == 100);
    }

    static bool Bench00n1()
    {
        sbyte t = Id(Ten);
        sbyte f = F00(t);
        return (f == 100);
    }

    static bool Bench00p2()
    {
        sbyte t = Id(10);
        sbyte f = F00(Id(t));
        return (f == 100);
    }

    static bool Bench00n2()
    {
        sbyte t = Id(Ten);
        sbyte f = F00(Id(t));
        return (f == 100);
    }

    static bool Bench00p3()
    {
        sbyte t = 10;
        sbyte f = F00(Id(Id(t)));
        return (f == 100);
    }

    static bool Bench00n3()
    {
        sbyte t = Ten;
        sbyte f = F00(Id(Id(t)));
        return (f == 100);
    }

    static bool Bench00p4()
    {
        sbyte t = 5;
        sbyte f = F00((sbyte)(2 * t));
        return (f == 100);
    }

    static bool Bench00n4()
    {
        sbyte t = Five;
        sbyte f = F00((sbyte)(2 * t));
        return (f == 100);
    }

    static sbyte F01(sbyte x)
    {
        return (sbyte)(1000 / x);
    }

    static bool Bench01p()
    {
        sbyte t = 10;
        sbyte f = F01(t);
        return (f == 100);
    }

    static bool Bench01n()
    {
        sbyte t = Ten;
        sbyte f = F01(t);
        return (f == 100);
    }

    static sbyte F02(sbyte x)
    {
        return (sbyte) (20 * (x / 2));
    }

    static bool Bench02p()
    {
        sbyte t = 10;
        sbyte f = F02(t);
        return (f == 100);
    }

    static bool Bench02n()
    {
        sbyte t = Ten;
        sbyte f = F02(t);
        return (f == 100);
    }

    static sbyte F03(sbyte x)
    {
        return (sbyte)(91 + 1009 % x);
    }

    static bool Bench03p()
    {
        sbyte t = 10;
        sbyte f = F03(t);
        return (f == 100);
    }

    static bool Bench03n()
    {
        sbyte t = Ten;
        sbyte f = F03(t);
        return (f == 100);
    }

    static sbyte F04(sbyte x)
    {
        return (sbyte)(50 * (x % 4));
    }

    static bool Bench04p()
    {
        sbyte t = 10;
        sbyte f = F04(t);
        return (f == 100);
    }

    static bool Bench04n()
    {
        sbyte t = Ten;
        sbyte f = F04(t);
        return (f == 100);
    }

    static sbyte F05(sbyte x)
    {
        return (sbyte)((1 << x) - 924);
    }

    static bool Bench05p()
    {
        sbyte t = 10;
        sbyte f = F05(t);
        return (f == 100);
    }

    static bool Bench05n()
    {
        sbyte t = Ten;
        sbyte f = F05(t);
        return (f == 100);
    }

    static sbyte F051(sbyte x)
    {
        return (sbyte)(102400 >> x);
    }

    static bool Bench05p1()
    {
        sbyte t = 10;
        sbyte f = F051(t);
        return (f == 100);
    }

    static bool Bench05n1()
    {
        sbyte t = Ten;
        sbyte f = F051(t);
        return (f == 100);
    }

    static sbyte F06(sbyte x)
    {
        return (sbyte)(-x + 110);
    }

    static bool Bench06p()
    {
        sbyte t = 10;
        sbyte f = F06(t);
        return (f == 100);
    }

    static bool Bench06n()
    {
        sbyte t = Ten;
        sbyte f = F06(t);
        return (f == 100);
    }

    static sbyte F07(sbyte x)
    {
        return (sbyte)(~x + 111);
    }

    static bool Bench07p()
    {
        sbyte t = 10;
        sbyte f = F07(t);
        return (f == 100);
    }

    static bool Bench07n()
    {
        sbyte t = Ten;
        sbyte f = F07(t);
        return (f == 100);
    }

    static sbyte F071(sbyte x)
    {
        return (sbyte)((x ^ -1) + 111);
    }

    static bool Bench07p1()
    {
        sbyte t = 10;
        sbyte f = F071(t);
        return (f == 100);
    }

    static bool Bench07n1()
    {
        sbyte t = Ten;
        sbyte f = F071(t);
        return (f == 100);
    }

    static sbyte F08(sbyte x)
    {
        return (sbyte)((x & 0x7) + 98);
    }

    static bool Bench08p()
    {
        sbyte t = 10;
        sbyte f = F08(t);
        return (f == 100);
    }

    static bool Bench08n()
    {
        sbyte t = Ten;
        sbyte f = F08(t);
        return (f == 100);
    }

    static sbyte F09(sbyte x)
    {
        return (sbyte)((x | 0x7) + 85);
    }

    static bool Bench09p()
    {
        sbyte t = 10;
        sbyte f = F09(t);
        return (f == 100);
    }

    static bool Bench09n()
    {
        sbyte t = Ten;
        sbyte f = F09(t);
        return (f == 100);
    }

    // Sbytes feeding comparisons.
    //
    // Inlining in Bench1xp should enable branch optimization
    // Inlining in Bench1xn will not enable branch optimization

    static sbyte F10(sbyte x)
    {
        return x == 10 ? (sbyte) 100 : (sbyte) 0;
    }

    static bool Bench10p()
    {
        sbyte t = 10;
        sbyte f = F10(t);
        return (f == 100);
    }

    static bool Bench10n()
    {
        sbyte t = Ten;
        sbyte f = F10(t);
        return (f == 100);
    }

    static sbyte F101(sbyte x)
    {
        return x != 10 ? (sbyte) 0 : (sbyte) 100;
    }

    static bool Bench10p1()
    {
        sbyte t = 10;
        sbyte f = F101(t);
        return (f == 100);
    }

    static bool Bench10n1()
    {
        sbyte t = Ten;
        sbyte f = F101(t);
        return (f == 100);
    }

    static sbyte F102(sbyte x)
    {
        return x >= 10 ? (sbyte) 100 : (sbyte) 0;
    }

    static bool Bench10p2()
    {
        sbyte t = 10;
        sbyte f = F102(t);
        return (f == 100);
    }

    static bool Bench10n2()
    {
        sbyte t = Ten;
        sbyte f = F102(t);
        return (f == 100);
    }

    static sbyte F103(sbyte x)
    {
        return x <= 10 ? (sbyte) 100 : (sbyte) 0;
    }

    static bool Bench10p3()
    {
        sbyte t = 10;
        sbyte f = F103(t);
        return (f == 100);
    }

    static bool Bench10n3()
    {
        sbyte t = Ten;
        sbyte f = F102(t);
        return (f == 100);
    }

    static sbyte F11(sbyte x)
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
        sbyte t = 10;
        sbyte f = F11(t);
        return (f == 100);
    }

    static bool Bench11n()
    {
        sbyte t = Ten;
        sbyte f = F11(t);
        return (f == 100);
    }

    static sbyte F111(sbyte x)
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
        sbyte t = 10;
        sbyte f = F111(t);
        return (f == 100);
    }

    static bool Bench11n1()
    {
        sbyte t = Ten;
        sbyte f = F111(t);
        return (f == 100);
    }

    static sbyte F112(sbyte x)
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
        sbyte t = 10;
        sbyte f = F112(t);
        return (f == 100);
    }

    static bool Bench11n2()
    {
        sbyte t = Ten;
        sbyte f = F112(t);
        return (f == 100);
    }
    static sbyte F113(sbyte x)
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
        sbyte t = 10;
        sbyte f = F113(t);
        return (f == 100);
    }

    static bool Bench11n3()
    {
        sbyte t = Ten;
        sbyte f = F113(t);
        return (f == 100);
    }

    // Ununsed (or effectively unused) parameters
    //
    // Simple callee analysis may overstate inline benefit

    static sbyte F20(sbyte x)
    {
        return 100;
    }

    static bool Bench20p()
    {
        sbyte t = 10;
        sbyte f = F20(t);
        return (f == 100);
    }

    static bool Bench20p1()
    {
        sbyte t = Ten;
        sbyte f = F20(t);
        return (f == 100);
    }

    static sbyte F21(sbyte x)
    {
        return (sbyte)(-x + 100 + x);
    }

    static bool Bench21p()
    {
        sbyte t = 10;
        sbyte f = F21(t);
        return (f == 100);
    }

    static bool Bench21n()
    {
        sbyte t = Ten;
        sbyte f = F21(t);
        return (f == 100);
    }

    static sbyte F211(sbyte x)
    {
        return (sbyte)(x - x + 100);
    }

    static bool Bench21p1()
    {
        sbyte t = 10;
        sbyte f = F211(t);
        return (f == 100);
    }

    static bool Bench21n1()
    {
        sbyte t = Ten;
        sbyte f = F211(t);
        return (f == 100);
    }

    static sbyte F22(sbyte x)
    {
        if (x > 0)
        {
            return 100;
        }

        return 100;
    }

    static bool Bench22p()
    {
        sbyte t = 10;
        sbyte f = F22(t);
        return (f == 100);
    }

    static bool Bench22p1()
    {
        sbyte t = Ten;
        sbyte f = F22(t);
        return (f == 100);
    }

    static sbyte F23(sbyte x)
    {
        if (x > 0)
        {
            return (sbyte)(90 + x);
        }

        return 100;
    }

    static bool Bench23p()
    {
        sbyte t = 10;
        sbyte f = F23(t);
        return (f == 100);
    }

    static bool Bench23n()
    {
        sbyte t = Ten;
        sbyte f = F23(t);
        return (f == 100);
    }

    // Multiple parameters

    static sbyte F30(sbyte x, sbyte y)
    {
        return (sbyte)(y * y);
    }

    static bool Bench30p()
    {
        sbyte t = 10;
        sbyte f = F30(t, t);
        return (f == 100);
    }

    static bool Bench30n()
    {
        sbyte t = Ten;
        sbyte f = F30(t, t);
        return (f == 100);
    }

    static bool Bench30p1()
    {
        sbyte s = Ten;
        sbyte t = 10;
        sbyte f = F30(s, t);
        return (f == 100);
    }

    static bool Bench30n1()
    {
        sbyte s = 10;
        sbyte t = Ten;
        sbyte f = F30(s, t);
        return (f == 100);
    }

    static bool Bench30p2()
    {
        sbyte s = 10;
        sbyte t = 10;
        sbyte f = F30(s, t);
        return (f == 100);
    }

    static bool Bench30n2()
    {
        sbyte s = Ten;
        sbyte t = Ten;
        sbyte f = F30(s, t);
        return (f == 100);
    }

    static bool Bench30p3()
    {
        sbyte s = 10;
        sbyte t = s;
        sbyte f = F30(s, t);
        return (f == 100);
    }

    static bool Bench30n3()
    {
        sbyte s = Ten;
        sbyte t = s;
        sbyte f = F30(s, t);
        return (f == 100);
    }

    static sbyte F31(sbyte x, sbyte y, sbyte z)
    {
        return (sbyte)(z * z);
    }

    static bool Bench31p()
    {
        sbyte t = 10;
        sbyte f = F31(t, t, t);
        return (f == 100);
    }

    static bool Bench31n()
    {
        sbyte t = Ten;
        sbyte f = F31(t, t, t);
        return (f == 100);
    }

    static bool Bench31p1()
    {
        sbyte r = Ten;
        sbyte s = Ten;
        sbyte t = 10;
        sbyte f = F31(r, s, t);
        return (f == 100);
    }

    static bool Bench31n1()
    {
        sbyte r = 10;
        sbyte s = 10;
        sbyte t = Ten;
        sbyte f = F31(r, s, t);
        return (f == 100);
    }

    static bool Bench31p2()
    {
        sbyte r = 10;
        sbyte s = 10;
        sbyte t = 10;
        sbyte f = F31(r, s, t);
        return (f == 100);
    }

    static bool Bench31n2()
    {
        sbyte r = Ten;
        sbyte s = Ten;
        sbyte t = Ten;
        sbyte f = F31(r, s, t);
        return (f == 100);
    }

    static bool Bench31p3()
    {
        sbyte r = 10;
        sbyte s = r;
        sbyte t = s;
        sbyte f = F31(r, s, t);
        return (f == 100);
    }

    static bool Bench31n3()
    {
        sbyte r = Ten;
        sbyte s = r;
        sbyte t = s;
        sbyte f = F31(r, s, t);
        return (f == 100);
    }

    // Two args, both used

    static sbyte F40(sbyte x, sbyte y)
    {
        return (sbyte)(x * x + y * y - 100);
    }

    static bool Bench40p()
    {
        sbyte t = 10;
        sbyte f = F40(t, t);
        return (f == 100);
    }

    static bool Bench40n()
    {
        sbyte t = Ten;
        sbyte f = F40(t, t);
        return (f == 100);
    }

    static bool Bench40p1()
    {
        sbyte s = Ten;
        sbyte t = 10;
        sbyte f = F40(s, t);
        return (f == 100);
    }

    static bool Bench40p2()
    {
        sbyte s = 10;
        sbyte t = Ten;
        sbyte f = F40(s, t);
        return (f == 100);
    }

    static sbyte F41(sbyte x, sbyte y)
    {
        return (sbyte)(x * y);
    }

    static bool Bench41p()
    {
        sbyte t = 10;
        sbyte f = F41(t, t);
        return (f == 100);
    }

    static bool Bench41n()
    {
        sbyte t = Ten;
        sbyte f = F41(t, t);
        return (f == 100);
    }

    static bool Bench41p1()
    {
        sbyte s = 10;
        sbyte t = Ten;
        sbyte f = F41(s, t);
        return (f == 100);
    }

    static bool Bench41p2()
    {
        sbyte s = Ten;
        sbyte t = 10;
        sbyte f = F41(s, t);
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
        TypeInfo t = typeof(ConstantArgsSByte).GetTypeInfo();
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
