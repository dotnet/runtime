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

public static class ConstantArgsUInt
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

    static uint Five = 5;
    static uint Ten = 10;

    static uint Id(uint x)
    {
        return x;
    }

    static uint F00(uint x)
    {
        return x * x;
    }

    static bool Bench00p()
    {
        uint t = 10;
        uint f = F00(t);
        return (f == 100);
    }

    static bool Bench00n()
    {
        uint t = Ten;
        uint f = F00(t);
        return (f == 100);
    }

    static bool Bench00p1()
    {
        uint t = Id(10);
        uint f = F00(t);
        return (f == 100);
    }

    static bool Bench00n1()
    {
        uint t = Id(Ten);
        uint f = F00(t);
        return (f == 100);
    }

    static bool Bench00p2()
    {
        uint t = Id(10);
        uint f = F00(Id(t));
        return (f == 100);
    }

    static bool Bench00n2()
    {
        uint t = Id(Ten);
        uint f = F00(Id(t));
        return (f == 100);
    }

    static bool Bench00p3()
    {
        uint t = 10;
        uint f = F00(Id(Id(t)));
        return (f == 100);
    }

    static bool Bench00n3()
    {
        uint t = Ten;
        uint f = F00(Id(Id(t)));
        return (f == 100);
    }

    static bool Bench00p4()
    {
        uint t = 5;
        uint f = F00(2 * t);
        return (f == 100);
    }

    static bool Bench00n4()
    {
        uint t = Five;
        uint f = F00(2 * t);
        return (f == 100);
    }

    static uint F01(uint x)
    {
        return 1000 / x;
    }

    static bool Bench01p()
    {
        uint t = 10;
        uint f = F01(t);
        return (f == 100);
    }

    static bool Bench01n()
    {
        uint t = Ten;
        uint f = F01(t);
        return (f == 100);
    }

    static uint F02(uint x)
    {
        return 20 * (x / 2);
    }

    static bool Bench02p()
    {
        uint t = 10;
        uint f = F02(t);
        return (f == 100);
    }

    static bool Bench02n()
    {
        uint t = Ten;
        uint f = F02(t);
        return (f == 100);
    }

    static uint F03(uint x)
    {
        return 91 + 1009 % x;
    }

    static bool Bench03p()
    {
        uint t = 10;
        uint f = F03(t);
        return (f == 100);
    }

    static bool Bench03n()
    {
        uint t = Ten;
        uint f = F03(t);
        return (f == 100);
    }

    static uint F04(uint x)
    {
        return 50 * (x % 4);
    }

    static bool Bench04p()
    {
        uint t = 10;
        uint f = F04(t);
        return (f == 100);
    }

    static bool Bench04n()
    {
        uint t = Ten;
        uint f = F04(t);
        return (f == 100);
    }

    static uint F06(uint x)
    {
        return 110 - x;
    }

    static bool Bench06p()
    {
        uint t = 10;
        uint f = F06(t);
        return (f == 100);
    }

    static bool Bench06n()
    {
        uint t = Ten;
        uint f = F06(t);
        return (f == 100);
    }

    static uint F07(uint x)
    {
        return ~x + 111;
    }

    static bool Bench07p()
    {
        uint t = 10;
        uint f = F07(t);
        return (f == 100);
    }

    static bool Bench07n()
    {
        uint t = Ten;
        uint f = F07(t);
        return (f == 100);
    }

    static uint F071(uint x)
    {
        return (x ^ 0xFFFFFFFF) + 111;
    }

    static bool Bench07p1()
    {
        uint t = 10;
        uint f = F071(t);
        return (f == 100);
    }

    static bool Bench07n1()
    {
        uint t = Ten;
        uint f = F071(t);
        return (f == 100);
    }

    static uint F08(uint x)
    {
        return (x & 0x7) + 98;
    }

    static bool Bench08p()
    {
        uint t = 10;
        uint f = F08(t);
        return (f == 100);
    }

    static bool Bench08n()
    {
        uint t = Ten;
        uint f = F08(t);
        return (f == 100);
    }

    static uint F09(uint x)
    {
        return (x | 0x7) + 85;
    }

    static bool Bench09p()
    {
        uint t = 10;
        uint f = F09(t);
        return (f == 100);
    }

    static bool Bench09n()
    {
        uint t = Ten;
        uint f = F09(t);
        return (f == 100);
    }

    // Uints feeding comparisons.
    //
    // Inlining in Bench1xp should enable branch optimization
    // Inlining in Bench1xn will not enable branch optimization

    static uint F10(uint x)
    {
        return x == 10 ? 100u : 0u;
    }

    static bool Bench10p()
    {
        uint t = 10;
        uint f = F10(t);
        return (f == 100);
    }

    static bool Bench10n()
    {
        uint t = Ten;
        uint f = F10(t);
        return (f == 100);
    }

    static uint F101(uint x)
    {
        return x != 10 ? 0u : 100u;
    }

    static bool Bench10p1()
    {
        uint t = 10;
        uint f = F101(t);
        return (f == 100);
    }

    static bool Bench10n1()
    {
        uint t = Ten;
        uint f = F101(t);
        return (f == 100);
    }

    static uint F102(uint x)
    {
        return x >= 10 ? 100u : 0u;
    }

    static bool Bench10p2()
    {
        uint t = 10;
        uint f = F102(t);
        return (f == 100);
    }

    static bool Bench10n2()
    {
        uint t = Ten;
        uint f = F102(t);
        return (f == 100);
    }

    static uint F103(uint x)
    {
        return x <= 10 ? 100u : 0u;
    }

    static bool Bench10p3()
    {
        uint t = 10;
        uint f = F103(t);
        return (f == 100);
    }

    static bool Bench10n3()
    {
        uint t = Ten;
        uint f = F102(t);
        return (f == 100);
    }

    static uint F11(uint x)
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
        uint t = 10;
        uint f = F11(t);
        return (f == 100);
    }

    static bool Bench11n()
    {
        uint t = Ten;
        uint f = F11(t);
        return (f == 100);
    }

    static uint F111(uint x)
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
        uint t = 10;
        uint f = F111(t);
        return (f == 100);
    }

    static bool Bench11n1()
    {
        uint t = Ten;
        uint f = F111(t);
        return (f == 100);
    }

    static uint F112(uint x)
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
        uint t = 10;
        uint f = F112(t);
        return (f == 100);
    }

    static bool Bench11n2()
    {
        uint t = Ten;
        uint f = F112(t);
        return (f == 100);
    }
    static uint F113(uint x)
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
        uint t = 10;
        uint f = F113(t);
        return (f == 100);
    }

    static bool Bench11n3()
    {
        uint t = Ten;
        uint f = F113(t);
        return (f == 100);
    }

    // Ununsed (or effectively unused) parameters
    //
    // Simple callee analysis may overstate inline benefit

    static uint F20(uint x)
    {
        return 100;
    }

    static bool Bench20p()
    {
        uint t = 10;
        uint f = F20(t);
        return (f == 100);
    }

    static bool Bench20p1()
    {
        uint t = Ten;
        uint f = F20(t);
        return (f == 100);
    }

    static uint F21(uint x)
    {
        return (uint) -x + 100 + x;
    }

    static bool Bench21p()
    {
        uint t = 10;
        uint f = F21(t);
        return (f == 100);
    }

    static bool Bench21n()
    {
        uint t = Ten;
        uint f = F21(t);
        return (f == 100);
    }

    static uint F211(uint x)
    {
        return x - x + 100;
    }

    static bool Bench21p1()
    {
        uint t = 10;
        uint f = F211(t);
        return (f == 100);
    }

    static bool Bench21n1()
    {
        uint t = Ten;
        uint f = F211(t);
        return (f == 100);
    }

    static uint F22(uint x)
    {
        if (x > 0)
        {
            return 100;
        }

        return 100;
    }

    static bool Bench22p()
    {
        uint t = 10;
        uint f = F22(t);
        return (f == 100);
    }

    static bool Bench22p1()
    {
        uint t = Ten;
        uint f = F22(t);
        return (f == 100);
    }

    static uint F23(uint x)
    {
        if (x > 0)
        {
            return 90 + x;
        }

        return 100;
    }

    static bool Bench23p()
    {
        uint t = 10;
        uint f = F23(t);
        return (f == 100);
    }

    static bool Bench23n()
    {
        uint t = Ten;
        uint f = F23(t);
        return (f == 100);
    }

    // Multiple parameters

    static uint F30(uint x, uint y)
    {
        return y * y;
    }

    static bool Bench30p()
    {
        uint t = 10;
        uint f = F30(t, t);
        return (f == 100);
    }

    static bool Bench30n()
    {
        uint t = Ten;
        uint f = F30(t, t);
        return (f == 100);
    }

    static bool Bench30p1()
    {
        uint s = Ten;
        uint t = 10;
        uint f = F30(s, t);
        return (f == 100);
    }

    static bool Bench30n1()
    {
        uint s = 10;
        uint t = Ten;
        uint f = F30(s, t);
        return (f == 100);
    }

    static bool Bench30p2()
    {
        uint s = 10;
        uint t = 10;
        uint f = F30(s, t);
        return (f == 100);
    }

    static bool Bench30n2()
    {
        uint s = Ten;
        uint t = Ten;
        uint f = F30(s, t);
        return (f == 100);
    }

    static bool Bench30p3()
    {
        uint s = 10;
        uint t = s;
        uint f = F30(s, t);
        return (f == 100);
    }

    static bool Bench30n3()
    {
        uint s = Ten;
        uint t = s;
        uint f = F30(s, t);
        return (f == 100);
    }

    static uint F31(uint x, uint y, uint z)
    {
        return z * z;
    }

    static bool Bench31p()
    {
        uint t = 10;
        uint f = F31(t, t, t);
        return (f == 100);
    }

    static bool Bench31n()
    {
        uint t = Ten;
        uint f = F31(t, t, t);
        return (f == 100);
    }

    static bool Bench31p1()
    {
        uint r = Ten;
        uint s = Ten;
        uint t = 10;
        uint f = F31(r, s, t);
        return (f == 100);
    }

    static bool Bench31n1()
    {
        uint r = 10;
        uint s = 10;
        uint t = Ten;
        uint f = F31(r, s, t);
        return (f == 100);
    }

    static bool Bench31p2()
    {
        uint r = 10;
        uint s = 10;
        uint t = 10;
        uint f = F31(r, s, t);
        return (f == 100);
    }

    static bool Bench31n2()
    {
        uint r = Ten;
        uint s = Ten;
        uint t = Ten;
        uint f = F31(r, s, t);
        return (f == 100);
    }

    static bool Bench31p3()
    {
        uint r = 10;
        uint s = r;
        uint t = s;
        uint f = F31(r, s, t);
        return (f == 100);
    }

    static bool Bench31n3()
    {
        uint r = Ten;
        uint s = r;
        uint t = s;
        uint f = F31(r, s, t);
        return (f == 100);
    }

    // Two args, both used

    static uint F40(uint x, uint y)
    {
        return x * x + y * y - 100;
    }

    static bool Bench40p()
    {
        uint t = 10;
        uint f = F40(t, t);
        return (f == 100);
    }

    static bool Bench40n()
    {
        uint t = Ten;
        uint f = F40(t, t);
        return (f == 100);
    }

    static bool Bench40p1()
    {
        uint s = Ten;
        uint t = 10;
        uint f = F40(s, t);
        return (f == 100);
    }

    static bool Bench40p2()
    {
        uint s = 10;
        uint t = Ten;
        uint f = F40(s, t);
        return (f == 100);
    }

    static uint F41(uint x, uint y)
    {
        return x * y;
    }

    static bool Bench41p()
    {
        uint t = 10;
        uint f = F41(t, t);
        return (f == 100);
    }

    static bool Bench41n()
    {
        uint t = Ten;
        uint f = F41(t, t);
        return (f == 100);
    }

    static bool Bench41p1()
    {
        uint s = 10;
        uint t = Ten;
        uint f = F41(s, t);
        return (f == 100);
    }

    static bool Bench41p2()
    {
        uint s = Ten;
        uint t = 10;
        uint f = F41(s, t);
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
        TypeInfo t = typeof(ConstantArgsUInt).GetTypeInfo();
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
