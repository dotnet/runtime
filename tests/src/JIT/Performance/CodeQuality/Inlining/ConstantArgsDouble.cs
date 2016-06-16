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

public static class ConstantArgsDouble
{

#if DEBUG
    public const int Iterations = 1;
#else
    public const int Iterations = 100000;
#endif

    // Doubles feeding math operations.
    //
    // Inlining in Bench0xp should enable constant folding
    // Inlining in Bench0xn will not enable constant folding

    static double Five = 5;
    static double Ten = 10;

    static double Id(double x)
    {
        return x;
    }

    static double F00(double x)
    {
        return x * x;
    }

    static bool Bench00p()
    {
        double t = 10;
        double f = F00(t);
        return (f == 100);
    }

    static bool Bench00n()
    {
        double t = Ten;
        double f = F00(t);
        return (f == 100);
    }

    static bool Bench00p1()
    {
        double t = Id(10);
        double f = F00(t);
        return (f == 100);
    }

    static bool Bench00n1()
    {
        double t = Id(Ten);
        double f = F00(t);
        return (f == 100);
    }

    static bool Bench00p2()
    {
        double t = Id(10);
        double f = F00(Id(t));
        return (f == 100);
    }

    static bool Bench00n2()
    {
        double t = Id(Ten);
        double f = F00(Id(t));
        return (f == 100);
    }

    static bool Bench00p3()
    {
        double t = 10;
        double f = F00(Id(Id(t)));
        return (f == 100);
    }

    static bool Bench00n3()
    {
        double t = Ten;
        double f = F00(Id(Id(t)));
        return (f == 100);
    }

    static bool Bench00p4()
    {
        double t = 5;
        double f = F00(2 * t);
        return (f == 100);
    }

    static bool Bench00n4()
    {
        double t = Five;
        double f = F00(2 * t);
        return (f == 100);
    }

    static double F01(double x)
    {
        return 1000 / x;
    }

    static bool Bench01p()
    {
        double t = 10;
        double f = F01(t);
        return (f == 100);
    }

    static bool Bench01n()
    {
        double t = Ten;
        double f = F01(t);
        return (f == 100);
    }

    static double F02(double x)
    {
        return 20 * (x / 2);
    }

    static bool Bench02p()
    {
        double t = 10;
        double f = F02(t);
        return (f == 100);
    }

    static bool Bench02n()
    {
        double t = Ten;
        double f = F02(t);
        return (f == 100);
    }

    static double F03(double x)
    {
        return 91 + 1009 % x;
    }

    static bool Bench03p()
    {
        double t = 10;
        double f = F03(t);
        return (f == 100);
    }

    static bool Bench03n()
    {
        double t = Ten;
        double f = F03(t);
        return (f == 100);
    }

    static double F04(double x)
    {
        return 50 * (x % 4);
    }

    static bool Bench04p()
    {
        double t = 10;
        double f = F04(t);
        return (f == 100);
    }

    static bool Bench04n()
    {
        double t = Ten;
        double f = F04(t);
        return (f == 100);
    }

    static double F06(double x)
    {
        return -x + 110;
    }

    static bool Bench06p()
    {
        double t = 10;
        double f = F06(t);
        return (f == 100);
    }

    static bool Bench06n()
    {
        double t = Ten;
        double f = F06(t);
        return (f == 100);
    }

    // Doubles feeding comparisons.
    //
    // Inlining in Bench1xp should enable branch optimization
    // Inlining in Bench1xn will not enable branch optimization

    static double F10(double x)
    {
        return x == 10 ? 100 : 0;
    }

    static bool Bench10p()
    {
        double t = 10;
        double f = F10(t);
        return (f == 100);
    }

    static bool Bench10n()
    {
        double t = Ten;
        double f = F10(t);
        return (f == 100);
    }

    static double F101(double x)
    {
        return x != 10 ? 0 : 100;
    }

    static bool Bench10p1()
    {
        double t = 10;
        double f = F101(t);
        return (f == 100);
    }

    static bool Bench10n1()
    {
        double t = Ten;
        double f = F101(t);
        return (f == 100);
    }

    static double F102(double x)
    {
        return x >= 10 ? 100 : 0;
    }

    static bool Bench10p2()
    {
        double t = 10;
        double f = F102(t);
        return (f == 100);
    }

    static bool Bench10n2()
    {
        double t = Ten;
        double f = F102(t);
        return (f == 100);
    }

    static double F103(double x)
    {
        return x <= 10 ? 100 : 0;
    }

    static bool Bench10p3()
    {
        double t = 10;
        double f = F103(t);
        return (f == 100);
    }

    static bool Bench10n3()
    {
        double t = Ten;
        double f = F102(t);
        return (f == 100);
    }

    static double F11(double x)
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
        double t = 10;
        double f = F11(t);
        return (f == 100);
    }

    static bool Bench11n()
    {
        double t = Ten;
        double f = F11(t);
        return (f == 100);
    }

    static double F111(double x)
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
        double t = 10;
        double f = F111(t);
        return (f == 100);
    }

    static bool Bench11n1()
    {
        double t = Ten;
        double f = F111(t);
        return (f == 100);
    }

    static double F112(double x)
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
        double t = 10;
        double f = F112(t);
        return (f == 100);
    }

    static bool Bench11n2()
    {
        double t = Ten;
        double f = F112(t);
        return (f == 100);
    }
    static double F113(double x)
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
        double t = 10;
        double f = F113(t);
        return (f == 100);
    }

    static bool Bench11n3()
    {
        double t = Ten;
        double f = F113(t);
        return (f == 100);
    }

    // Ununsed (or effectively unused) parameters
    //
    // Simple callee analysis may overstate inline benefit

    static double F20(double x)
    {
        return 100;
    }

    static bool Bench20p()
    {
        double t = 10;
        double f = F20(t);
        return (f == 100);
    }

    static bool Bench20p1()
    {
        double t = Ten;
        double f = F20(t);
        return (f == 100);
    }

    static double F21(double x)
    {
        return -x + 100 + x;
    }

    static bool Bench21p()
    {
        double t = 10;
        double f = F21(t);
        return (f == 100);
    }

    static bool Bench21n()
    {
        double t = Ten;
        double f = F21(t);
        return (f == 100);
    }

    static double F211(double x)
    {
        return x - x + 100;
    }

    static bool Bench21p1()
    {
        double t = 10;
        double f = F211(t);
        return (f == 100);
    }

    static bool Bench21n1()
    {
        double t = Ten;
        double f = F211(t);
        return (f == 100);
    }

    static double F22(double x)
    {
        if (x > 0)
        {
            return 100;
        }

        return 100;
    }

    static bool Bench22p()
    {
        double t = 10;
        double f = F22(t);
        return (f == 100);
    }

    static bool Bench22p1()
    {
        double t = Ten;
        double f = F22(t);
        return (f == 100);
    }

    static double F23(double x)
    {
        if (x > 0)
        {
            return 90 + x;
        }

        return 100;
    }

    static bool Bench23p()
    {
        double t = 10;
        double f = F23(t);
        return (f == 100);
    }

    static bool Bench23n()
    {
        double t = Ten;
        double f = F23(t);
        return (f == 100);
    }

    // Multiple parameters

    static double F30(double x, double y)
    {
        return y * y;
    }

    static bool Bench30p()
    {
        double t = 10;
        double f = F30(t, t);
        return (f == 100);
    }

    static bool Bench30n()
    {
        double t = Ten;
        double f = F30(t, t);
        return (f == 100);
    }

    static bool Bench30p1()
    {
        double s = Ten;
        double t = 10;
        double f = F30(s, t);
        return (f == 100);
    }

    static bool Bench30n1()
    {
        double s = 10;
        double t = Ten;
        double f = F30(s, t);
        return (f == 100);
    }

    static bool Bench30p2()
    {
        double s = 10;
        double t = 10;
        double f = F30(s, t);
        return (f == 100);
    }

    static bool Bench30n2()
    {
        double s = Ten;
        double t = Ten;
        double f = F30(s, t);
        return (f == 100);
    }

    static bool Bench30p3()
    {
        double s = 10;
        double t = s;
        double f = F30(s, t);
        return (f == 100);
    }

    static bool Bench30n3()
    {
        double s = Ten;
        double t = s;
        double f = F30(s, t);
        return (f == 100);
    }

    static double F31(double x, double y, double z)
    {
        return z * z;
    }

    static bool Bench31p()
    {
        double t = 10;
        double f = F31(t, t, t);
        return (f == 100);
    }

    static bool Bench31n()
    {
        double t = Ten;
        double f = F31(t, t, t);
        return (f == 100);
    }

    static bool Bench31p1()
    {
        double r = Ten;
        double s = Ten;
        double t = 10;
        double f = F31(r, s, t);
        return (f == 100);
    }

    static bool Bench31n1()
    {
        double r = 10;
        double s = 10;
        double t = Ten;
        double f = F31(r, s, t);
        return (f == 100);
    }

    static bool Bench31p2()
    {
        double r = 10;
        double s = 10;
        double t = 10;
        double f = F31(r, s, t);
        return (f == 100);
    }

    static bool Bench31n2()
    {
        double r = Ten;
        double s = Ten;
        double t = Ten;
        double f = F31(r, s, t);
        return (f == 100);
    }

    static bool Bench31p3()
    {
        double r = 10;
        double s = r;
        double t = s;
        double f = F31(r, s, t);
        return (f == 100);
    }

    static bool Bench31n3()
    {
        double r = Ten;
        double s = r;
        double t = s;
        double f = F31(r, s, t);
        return (f == 100);
    }

    // Two args, both used

    static double F40(double x, double y)
    {
        return x * x + y * y - 100;
    }

    static bool Bench40p()
    {
        double t = 10;
        double f = F40(t, t);
        return (f == 100);
    }

    static bool Bench40n()
    {
        double t = Ten;
        double f = F40(t, t);
        return (f == 100);
    }

    static bool Bench40p1()
    {
        double s = Ten;
        double t = 10;
        double f = F40(s, t);
        return (f == 100);
    }

    static bool Bench40p2()
    {
        double s = 10;
        double t = Ten;
        double f = F40(s, t);
        return (f == 100);
    }

    static double F41(double x, double y)
    {
        return x * y;
    }

    static bool Bench41p()
    {
        double t = 10;
        double f = F41(t, t);
        return (f == 100);
    }

    static bool Bench41n()
    {
        double t = Ten;
        double f = F41(t, t);
        return (f == 100);
    }

    static bool Bench41p1()
    {
        double s = 10;
        double t = Ten;
        double f = F41(s, t);
        return (f == 100);
    }

    static bool Bench41p2()
    {
        double s = Ten;
        double t = 10;
        double f = F41(s, t);
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
        "Bench06p",  "Bench06n",
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
        TypeInfo t = typeof(ConstantArgsDouble).GetTypeInfo();
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
