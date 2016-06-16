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

public static class ConstantArgsUShort
{

#if DEBUG
    public const int Iterations = 1;
#else
    public const int Iterations = 100000;
#endif

    // Ushorts feeding math operations.
    //
    // Inlining in Bench0xp should enable constant folding
    // Inlining in Bench0xn will not enable constant folding

    static ushort Five = 5;
    static ushort Ten = 10;

    static ushort Id(ushort x)
    {
        return x;
    }

    static ushort F00(ushort x)
    {
        return (ushort) (x * x);
    }

    static bool Bench00p()
    {
        ushort t = 10;
        ushort f = F00(t);
        return (f == 100);
    }

    static bool Bench00n()
    {
        ushort t = Ten;
        ushort f = F00(t);
        return (f == 100);
    }

    static bool Bench00p1()
    {
        ushort t = Id(10);
        ushort f = F00(t);
        return (f == 100);
    }

    static bool Bench00n1()
    {
        ushort t = Id(Ten);
        ushort f = F00(t);
        return (f == 100);
    }

    static bool Bench00p2()
    {
        ushort t = Id(10);
        ushort f = F00(Id(t));
        return (f == 100);
    }

    static bool Bench00n2()
    {
        ushort t = Id(Ten);
        ushort f = F00(Id(t));
        return (f == 100);
    }

    static bool Bench00p3()
    {
        ushort t = 10;
        ushort f = F00(Id(Id(t)));
        return (f == 100);
    }

    static bool Bench00n3()
    {
        ushort t = Ten;
        ushort f = F00(Id(Id(t)));
        return (f == 100);
    }

    static bool Bench00p4()
    {
        ushort t = 5;
        ushort f = F00((ushort)(2 * t));
        return (f == 100);
    }

    static bool Bench00n4()
    {
        ushort t = Five;
        ushort f = F00((ushort)(2 * t));
        return (f == 100);
    }

    static ushort F01(ushort x)
    {
        return (ushort)(1000 / x);
    }

    static bool Bench01p()
    {
        ushort t = 10;
        ushort f = F01(t);
        return (f == 100);
    }

    static bool Bench01n()
    {
        ushort t = Ten;
        ushort f = F01(t);
        return (f == 100);
    }

    static ushort F02(ushort x)
    {
        return (ushort) (20 * (x / 2));
    }

    static bool Bench02p()
    {
        ushort t = 10;
        ushort f = F02(t);
        return (f == 100);
    }

    static bool Bench02n()
    {
        ushort t = Ten;
        ushort f = F02(t);
        return (f == 100);
    }

    static ushort F03(ushort x)
    {
        return (ushort)(91 + 1009 % x);
    }

    static bool Bench03p()
    {
        ushort t = 10;
        ushort f = F03(t);
        return (f == 100);
    }

    static bool Bench03n()
    {
        ushort t = Ten;
        ushort f = F03(t);
        return (f == 100);
    }

    static ushort F04(ushort x)
    {
        return (ushort)(50 * (x % 4));
    }

    static bool Bench04p()
    {
        ushort t = 10;
        ushort f = F04(t);
        return (f == 100);
    }

    static bool Bench04n()
    {
        ushort t = Ten;
        ushort f = F04(t);
        return (f == 100);
    }

    static ushort F05(ushort x)
    {
        return (ushort)((1 << x) - 924);
    }

    static bool Bench05p()
    {
        ushort t = 10;
        ushort f = F05(t);
        return (f == 100);
    }

    static bool Bench05n()
    {
        ushort t = Ten;
        ushort f = F05(t);
        return (f == 100);
    }

    static ushort F051(ushort x)
    {
        return (ushort)(102400 >> x);
    }

    static bool Bench05p1()
    {
        ushort t = 10;
        ushort f = F051(t);
        return (f == 100);
    }

    static bool Bench05n1()
    {
        ushort t = Ten;
        ushort f = F051(t);
        return (f == 100);
    }

    static ushort F06(ushort x)
    {
        return (ushort)(-x + 110);
    }

    static bool Bench06p()
    {
        ushort t = 10;
        ushort f = F06(t);
        return (f == 100);
    }

    static bool Bench06n()
    {
        ushort t = Ten;
        ushort f = F06(t);
        return (f == 100);
    }

    static ushort F07(ushort x)
    {
        return (ushort)(~x + 111);
    }

    static bool Bench07p()
    {
        ushort t = 10;
        ushort f = F07(t);
        return (f == 100);
    }

    static bool Bench07n()
    {
        ushort t = Ten;
        ushort f = F07(t);
        return (f == 100);
    }

    static ushort F071(ushort x)
    {
        return (ushort)((x ^ -1) + 111);
    }

    static bool Bench07p1()
    {
        ushort t = 10;
        ushort f = F071(t);
        return (f == 100);
    }

    static bool Bench07n1()
    {
        ushort t = Ten;
        ushort f = F071(t);
        return (f == 100);
    }

    static ushort F08(ushort x)
    {
        return (ushort)((x & 0x7) + 98);
    }

    static bool Bench08p()
    {
        ushort t = 10;
        ushort f = F08(t);
        return (f == 100);
    }

    static bool Bench08n()
    {
        ushort t = Ten;
        ushort f = F08(t);
        return (f == 100);
    }

    static ushort F09(ushort x)
    {
        return (ushort)((x | 0x7) + 85);
    }

    static bool Bench09p()
    {
        ushort t = 10;
        ushort f = F09(t);
        return (f == 100);
    }

    static bool Bench09n()
    {
        ushort t = Ten;
        ushort f = F09(t);
        return (f == 100);
    }

    // Ushorts feeding comparisons.
    //
    // Inlining in Bench1xp should enable branch optimization
    // Inlining in Bench1xn will not enable branch optimization

    static ushort F10(ushort x)
    {
        return x == 10 ? (ushort) 100 : (ushort) 0;
    }

    static bool Bench10p()
    {
        ushort t = 10;
        ushort f = F10(t);
        return (f == 100);
    }

    static bool Bench10n()
    {
        ushort t = Ten;
        ushort f = F10(t);
        return (f == 100);
    }

    static ushort F101(ushort x)
    {
        return x != 10 ? (ushort) 0 : (ushort) 100;
    }

    static bool Bench10p1()
    {
        ushort t = 10;
        ushort f = F101(t);
        return (f == 100);
    }

    static bool Bench10n1()
    {
        ushort t = Ten;
        ushort f = F101(t);
        return (f == 100);
    }

    static ushort F102(ushort x)
    {
        return x >= 10 ? (ushort) 100 : (ushort) 0;
    }

    static bool Bench10p2()
    {
        ushort t = 10;
        ushort f = F102(t);
        return (f == 100);
    }

    static bool Bench10n2()
    {
        ushort t = Ten;
        ushort f = F102(t);
        return (f == 100);
    }

    static ushort F103(ushort x)
    {
        return x <= 10 ? (ushort) 100 : (ushort) 0;
    }

    static bool Bench10p3()
    {
        ushort t = 10;
        ushort f = F103(t);
        return (f == 100);
    }

    static bool Bench10n3()
    {
        ushort t = Ten;
        ushort f = F102(t);
        return (f == 100);
    }

    static ushort F11(ushort x)
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
        ushort t = 10;
        ushort f = F11(t);
        return (f == 100);
    }

    static bool Bench11n()
    {
        ushort t = Ten;
        ushort f = F11(t);
        return (f == 100);
    }

    static ushort F111(ushort x)
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
        ushort t = 10;
        ushort f = F111(t);
        return (f == 100);
    }

    static bool Bench11n1()
    {
        ushort t = Ten;
        ushort f = F111(t);
        return (f == 100);
    }

    static ushort F112(ushort x)
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
        ushort t = 10;
        ushort f = F112(t);
        return (f == 100);
    }

    static bool Bench11n2()
    {
        ushort t = Ten;
        ushort f = F112(t);
        return (f == 100);
    }
    static ushort F113(ushort x)
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
        ushort t = 10;
        ushort f = F113(t);
        return (f == 100);
    }

    static bool Bench11n3()
    {
        ushort t = Ten;
        ushort f = F113(t);
        return (f == 100);
    }

    // Ununsed (or effectively unused) parameters
    //
    // Simple callee analysis may overstate inline benefit

    static ushort F20(ushort x)
    {
        return 100;
    }

    static bool Bench20p()
    {
        ushort t = 10;
        ushort f = F20(t);
        return (f == 100);
    }

    static bool Bench20p1()
    {
        ushort t = Ten;
        ushort f = F20(t);
        return (f == 100);
    }

    static ushort F21(ushort x)
    {
        return (ushort)(-x + 100 + x);
    }

    static bool Bench21p()
    {
        ushort t = 10;
        ushort f = F21(t);
        return (f == 100);
    }

    static bool Bench21n()
    {
        ushort t = Ten;
        ushort f = F21(t);
        return (f == 100);
    }

    static ushort F211(ushort x)
    {
        return (ushort)(x - x + 100);
    }

    static bool Bench21p1()
    {
        ushort t = 10;
        ushort f = F211(t);
        return (f == 100);
    }

    static bool Bench21n1()
    {
        ushort t = Ten;
        ushort f = F211(t);
        return (f == 100);
    }

    static ushort F22(ushort x)
    {
        if (x > 0)
        {
            return 100;
        }

        return 100;
    }

    static bool Bench22p()
    {
        ushort t = 10;
        ushort f = F22(t);
        return (f == 100);
    }

    static bool Bench22p1()
    {
        ushort t = Ten;
        ushort f = F22(t);
        return (f == 100);
    }

    static ushort F23(ushort x)
    {
        if (x > 0)
        {
            return (ushort)(90 + x);
        }

        return 100;
    }

    static bool Bench23p()
    {
        ushort t = 10;
        ushort f = F23(t);
        return (f == 100);
    }

    static bool Bench23n()
    {
        ushort t = Ten;
        ushort f = F23(t);
        return (f == 100);
    }

    // Multiple parameters

    static ushort F30(ushort x, ushort y)
    {
        return (ushort)(y * y);
    }

    static bool Bench30p()
    {
        ushort t = 10;
        ushort f = F30(t, t);
        return (f == 100);
    }

    static bool Bench30n()
    {
        ushort t = Ten;
        ushort f = F30(t, t);
        return (f == 100);
    }

    static bool Bench30p1()
    {
        ushort s = Ten;
        ushort t = 10;
        ushort f = F30(s, t);
        return (f == 100);
    }

    static bool Bench30n1()
    {
        ushort s = 10;
        ushort t = Ten;
        ushort f = F30(s, t);
        return (f == 100);
    }

    static bool Bench30p2()
    {
        ushort s = 10;
        ushort t = 10;
        ushort f = F30(s, t);
        return (f == 100);
    }

    static bool Bench30n2()
    {
        ushort s = Ten;
        ushort t = Ten;
        ushort f = F30(s, t);
        return (f == 100);
    }

    static bool Bench30p3()
    {
        ushort s = 10;
        ushort t = s;
        ushort f = F30(s, t);
        return (f == 100);
    }

    static bool Bench30n3()
    {
        ushort s = Ten;
        ushort t = s;
        ushort f = F30(s, t);
        return (f == 100);
    }

    static ushort F31(ushort x, ushort y, ushort z)
    {
        return (ushort)(z * z);
    }

    static bool Bench31p()
    {
        ushort t = 10;
        ushort f = F31(t, t, t);
        return (f == 100);
    }

    static bool Bench31n()
    {
        ushort t = Ten;
        ushort f = F31(t, t, t);
        return (f == 100);
    }

    static bool Bench31p1()
    {
        ushort r = Ten;
        ushort s = Ten;
        ushort t = 10;
        ushort f = F31(r, s, t);
        return (f == 100);
    }

    static bool Bench31n1()
    {
        ushort r = 10;
        ushort s = 10;
        ushort t = Ten;
        ushort f = F31(r, s, t);
        return (f == 100);
    }

    static bool Bench31p2()
    {
        ushort r = 10;
        ushort s = 10;
        ushort t = 10;
        ushort f = F31(r, s, t);
        return (f == 100);
    }

    static bool Bench31n2()
    {
        ushort r = Ten;
        ushort s = Ten;
        ushort t = Ten;
        ushort f = F31(r, s, t);
        return (f == 100);
    }

    static bool Bench31p3()
    {
        ushort r = 10;
        ushort s = r;
        ushort t = s;
        ushort f = F31(r, s, t);
        return (f == 100);
    }

    static bool Bench31n3()
    {
        ushort r = Ten;
        ushort s = r;
        ushort t = s;
        ushort f = F31(r, s, t);
        return (f == 100);
    }

    // Two args, both used

    static ushort F40(ushort x, ushort y)
    {
        return (ushort)(x * x + y * y - 100);
    }

    static bool Bench40p()
    {
        ushort t = 10;
        ushort f = F40(t, t);
        return (f == 100);
    }

    static bool Bench40n()
    {
        ushort t = Ten;
        ushort f = F40(t, t);
        return (f == 100);
    }

    static bool Bench40p1()
    {
        ushort s = Ten;
        ushort t = 10;
        ushort f = F40(s, t);
        return (f == 100);
    }

    static bool Bench40p2()
    {
        ushort s = 10;
        ushort t = Ten;
        ushort f = F40(s, t);
        return (f == 100);
    }

    static ushort F41(ushort x, ushort y)
    {
        return (ushort)(x * y);
    }

    static bool Bench41p()
    {
        ushort t = 10;
        ushort f = F41(t, t);
        return (f == 100);
    }

    static bool Bench41n()
    {
        ushort t = Ten;
        ushort f = F41(t, t);
        return (f == 100);
    }

    static bool Bench41p1()
    {
        ushort s = 10;
        ushort t = Ten;
        ushort f = F41(s, t);
        return (f == 100);
    }

    static bool Bench41p2()
    {
        ushort s = Ten;
        ushort t = 10;
        ushort f = F41(s, t);
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
        TypeInfo t = typeof(ConstantArgsUShort).GetTypeInfo();
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
