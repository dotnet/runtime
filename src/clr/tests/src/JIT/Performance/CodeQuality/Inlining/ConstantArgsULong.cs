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

public static class ConstantArgsULong
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

    static ulong Five = 5;
    static ulong Ten = 10;

    static ulong Id(ulong x)
    {
        return x;
    }

    static ulong F00(ulong x)
    {
        return x * x;
    }

    static bool Bench00p()
    {
        ulong t = 10;
        ulong f = F00(t);
        return (f == 100);
    }

    static bool Bench00n()
    {
        ulong t = Ten;
        ulong f = F00(t);
        return (f == 100);
    }

    static bool Bench00p1()
    {
        ulong t = Id(10);
        ulong f = F00(t);
        return (f == 100);
    }

    static bool Bench00n1()
    {
        ulong t = Id(Ten);
        ulong f = F00(t);
        return (f == 100);
    }

    static bool Bench00p2()
    {
        ulong t = Id(10);
        ulong f = F00(Id(t));
        return (f == 100);
    }

    static bool Bench00n2()
    {
        ulong t = Id(Ten);
        ulong f = F00(Id(t));
        return (f == 100);
    }

    static bool Bench00p3()
    {
        ulong t = 10;
        ulong f = F00(Id(Id(t)));
        return (f == 100);
    }

    static bool Bench00n3()
    {
        ulong t = Ten;
        ulong f = F00(Id(Id(t)));
        return (f == 100);
    }

    static bool Bench00p4()
    {
        ulong t = 5;
        ulong f = F00(2 * t);
        return (f == 100);
    }

    static bool Bench00n4()
    {
        ulong t = Five;
        ulong f = F00(2 * t);
        return (f == 100);
    }

    static ulong F01(ulong x)
    {
        return 1000 / x;
    }

    static bool Bench01p()
    {
        ulong t = 10;
        ulong f = F01(t);
        return (f == 100);
    }

    static bool Bench01n()
    {
        ulong t = Ten;
        ulong f = F01(t);
        return (f == 100);
    }

    static ulong F02(ulong x)
    {
        return 20 * (x / 2);
    }

    static bool Bench02p()
    {
        ulong t = 10;
        ulong f = F02(t);
        return (f == 100);
    }

    static bool Bench02n()
    {
        ulong t = Ten;
        ulong f = F02(t);
        return (f == 100);
    }

    static ulong F03(ulong x)
    {
        return 91 + 1009 % x;
    }

    static bool Bench03p()
    {
        ulong t = 10;
        ulong f = F03(t);
        return (f == 100);
    }

    static bool Bench03n()
    {
        ulong t = Ten;
        ulong f = F03(t);
        return (f == 100);
    }

    static ulong F04(ulong x)
    {
        return 50 * (x % 4);
    }

    static bool Bench04p()
    {
        ulong t = 10;
        ulong f = F04(t);
        return (f == 100);
    }

    static bool Bench04n()
    {
        ulong t = Ten;
        ulong f = F04(t);
        return (f == 100);
    }

    static ulong F06(ulong x)
    {
        return 110 - x;
    }

    static bool Bench06p()
    {
        ulong t = 10;
        ulong f = F06(t);
        return (f == 100);
    }

    static bool Bench06n()
    {
        ulong t = Ten;
        ulong f = F06(t);
        return (f == 100);
    }

    static ulong F07(ulong x)
    {
        return ~x + 111;
    }

    static bool Bench07p()
    {
        ulong t = 10;
        ulong f = F07(t);
        return (f == 100);
    }

    static bool Bench07n()
    {
        ulong t = Ten;
        ulong f = F07(t);
        return (f == 100);
    }

    static ulong F071(ulong x)
    {
        return (x ^ 0xFFFFFFFFFFFFFFFF) + 111;
    }

    static bool Bench07p1()
    {
        ulong t = 10;
        ulong f = F071(t);
        return (f == 100);
    }

    static bool Bench07n1()
    {
        ulong t = Ten;
        ulong f = F071(t);
        return (f == 100);
    }

    static ulong F08(ulong x)
    {
        return (x & 0x7) + 98;
    }

    static bool Bench08p()
    {
        ulong t = 10;
        ulong f = F08(t);
        return (f == 100);
    }

    static bool Bench08n()
    {
        ulong t = Ten;
        ulong f = F08(t);
        return (f == 100);
    }

    static ulong F09(ulong x)
    {
        return (x | 0x7) + 85;
    }

    static bool Bench09p()
    {
        ulong t = 10;
        ulong f = F09(t);
        return (f == 100);
    }

    static bool Bench09n()
    {
        ulong t = Ten;
        ulong f = F09(t);
        return (f == 100);
    }

    // Ulongs feeding comparisons.
    //
    // Inlining in Bench1xp should enable branch optimization
    // Inlining in Bench1xn will not enable branch optimization

    static ulong F10(ulong x)
    {
        return x == 10 ? 100u : 0u;
    }

    static bool Bench10p()
    {
        ulong t = 10;
        ulong f = F10(t);
        return (f == 100);
    }

    static bool Bench10n()
    {
        ulong t = Ten;
        ulong f = F10(t);
        return (f == 100);
    }

    static ulong F101(ulong x)
    {
        return x != 10 ? 0u : 100u;
    }

    static bool Bench10p1()
    {
        ulong t = 10;
        ulong f = F101(t);
        return (f == 100);
    }

    static bool Bench10n1()
    {
        ulong t = Ten;
        ulong f = F101(t);
        return (f == 100);
    }

    static ulong F102(ulong x)
    {
        return x >= 10 ? 100u : 0u;
    }

    static bool Bench10p2()
    {
        ulong t = 10;
        ulong f = F102(t);
        return (f == 100);
    }

    static bool Bench10n2()
    {
        ulong t = Ten;
        ulong f = F102(t);
        return (f == 100);
    }

    static ulong F103(ulong x)
    {
        return x <= 10 ? 100u : 0u;
    }

    static bool Bench10p3()
    {
        ulong t = 10;
        ulong f = F103(t);
        return (f == 100);
    }

    static bool Bench10n3()
    {
        ulong t = Ten;
        ulong f = F102(t);
        return (f == 100);
    }

    static ulong F11(ulong x)
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
        ulong t = 10;
        ulong f = F11(t);
        return (f == 100);
    }

    static bool Bench11n()
    {
        ulong t = Ten;
        ulong f = F11(t);
        return (f == 100);
    }

    static ulong F111(ulong x)
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
        ulong t = 10;
        ulong f = F111(t);
        return (f == 100);
    }

    static bool Bench11n1()
    {
        ulong t = Ten;
        ulong f = F111(t);
        return (f == 100);
    }

    static ulong F112(ulong x)
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
        ulong t = 10;
        ulong f = F112(t);
        return (f == 100);
    }

    static bool Bench11n2()
    {
        ulong t = Ten;
        ulong f = F112(t);
        return (f == 100);
    }
    static ulong F113(ulong x)
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
        ulong t = 10;
        ulong f = F113(t);
        return (f == 100);
    }

    static bool Bench11n3()
    {
        ulong t = Ten;
        ulong f = F113(t);
        return (f == 100);
    }

    // Ununsed (or effectively unused) parameters
    //
    // Simple callee analysis may overstate inline benefit

    static ulong F20(ulong x)
    {
        return 100;
    }

    static bool Bench20p()
    {
        ulong t = 10;
        ulong f = F20(t);
        return (f == 100);
    }

    static bool Bench20p1()
    {
        ulong t = Ten;
        ulong f = F20(t);
        return (f == 100);
    }

    static ulong F21(ulong x)
    {
        return x + 100 - x;
    }

    static bool Bench21p()
    {
        ulong t = 10;
        ulong f = F21(t);
        return (f == 100);
    }

    static bool Bench21n()
    {
        ulong t = Ten;
        ulong f = F21(t);
        return (f == 100);
    }

    static ulong F211(ulong x)
    {
        return x - x + 100;
    }

    static bool Bench21p1()
    {
        ulong t = 10;
        ulong f = F211(t);
        return (f == 100);
    }

    static bool Bench21n1()
    {
        ulong t = Ten;
        ulong f = F211(t);
        return (f == 100);
    }

    static ulong F22(ulong x)
    {
        if (x > 0)
        {
            return 100;
        }

        return 100;
    }

    static bool Bench22p()
    {
        ulong t = 10;
        ulong f = F22(t);
        return (f == 100);
    }

    static bool Bench22p1()
    {
        ulong t = Ten;
        ulong f = F22(t);
        return (f == 100);
    }

    static ulong F23(ulong x)
    {
        if (x > 0)
        {
            return 90 + x;
        }

        return 100;
    }

    static bool Bench23p()
    {
        ulong t = 10;
        ulong f = F23(t);
        return (f == 100);
    }

    static bool Bench23n()
    {
        ulong t = Ten;
        ulong f = F23(t);
        return (f == 100);
    }

    // Multiple parameters

    static ulong F30(ulong x, ulong y)
    {
        return y * y;
    }

    static bool Bench30p()
    {
        ulong t = 10;
        ulong f = F30(t, t);
        return (f == 100);
    }

    static bool Bench30n()
    {
        ulong t = Ten;
        ulong f = F30(t, t);
        return (f == 100);
    }

    static bool Bench30p1()
    {
        ulong s = Ten;
        ulong t = 10;
        ulong f = F30(s, t);
        return (f == 100);
    }

    static bool Bench30n1()
    {
        ulong s = 10;
        ulong t = Ten;
        ulong f = F30(s, t);
        return (f == 100);
    }

    static bool Bench30p2()
    {
        ulong s = 10;
        ulong t = 10;
        ulong f = F30(s, t);
        return (f == 100);
    }

    static bool Bench30n2()
    {
        ulong s = Ten;
        ulong t = Ten;
        ulong f = F30(s, t);
        return (f == 100);
    }

    static bool Bench30p3()
    {
        ulong s = 10;
        ulong t = s;
        ulong f = F30(s, t);
        return (f == 100);
    }

    static bool Bench30n3()
    {
        ulong s = Ten;
        ulong t = s;
        ulong f = F30(s, t);
        return (f == 100);
    }

    static ulong F31(ulong x, ulong y, ulong z)
    {
        return z * z;
    }

    static bool Bench31p()
    {
        ulong t = 10;
        ulong f = F31(t, t, t);
        return (f == 100);
    }

    static bool Bench31n()
    {
        ulong t = Ten;
        ulong f = F31(t, t, t);
        return (f == 100);
    }

    static bool Bench31p1()
    {
        ulong r = Ten;
        ulong s = Ten;
        ulong t = 10;
        ulong f = F31(r, s, t);
        return (f == 100);
    }

    static bool Bench31n1()
    {
        ulong r = 10;
        ulong s = 10;
        ulong t = Ten;
        ulong f = F31(r, s, t);
        return (f == 100);
    }

    static bool Bench31p2()
    {
        ulong r = 10;
        ulong s = 10;
        ulong t = 10;
        ulong f = F31(r, s, t);
        return (f == 100);
    }

    static bool Bench31n2()
    {
        ulong r = Ten;
        ulong s = Ten;
        ulong t = Ten;
        ulong f = F31(r, s, t);
        return (f == 100);
    }

    static bool Bench31p3()
    {
        ulong r = 10;
        ulong s = r;
        ulong t = s;
        ulong f = F31(r, s, t);
        return (f == 100);
    }

    static bool Bench31n3()
    {
        ulong r = Ten;
        ulong s = r;
        ulong t = s;
        ulong f = F31(r, s, t);
        return (f == 100);
    }

    // Two args, both used

    static ulong F40(ulong x, ulong y)
    {
        return x * x + y * y - 100;
    }

    static bool Bench40p()
    {
        ulong t = 10;
        ulong f = F40(t, t);
        return (f == 100);
    }

    static bool Bench40n()
    {
        ulong t = Ten;
        ulong f = F40(t, t);
        return (f == 100);
    }

    static bool Bench40p1()
    {
        ulong s = Ten;
        ulong t = 10;
        ulong f = F40(s, t);
        return (f == 100);
    }

    static bool Bench40p2()
    {
        ulong s = 10;
        ulong t = Ten;
        ulong f = F40(s, t);
        return (f == 100);
    }

    static ulong F41(ulong x, ulong y)
    {
        return x * y;
    }

    static bool Bench41p()
    {
        ulong t = 10;
        ulong f = F41(t, t);
        return (f == 100);
    }

    static bool Bench41n()
    {
        ulong t = Ten;
        ulong f = F41(t, t);
        return (f == 100);
    }

    static bool Bench41p1()
    {
        ulong s = 10;
        ulong t = Ten;
        ulong f = F41(s, t);
        return (f == 100);
    }

    static bool Bench41p2()
    {
        ulong s = Ten;
        ulong t = 10;
        ulong f = F41(s, t);
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
        TypeInfo t = typeof(ConstantArgsULong).GetTypeInfo();
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
