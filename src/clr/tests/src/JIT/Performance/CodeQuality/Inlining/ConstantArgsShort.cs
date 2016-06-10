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

public static class ConstantArgsShort
{

#if DEBUG
    public const int Iterations = 1;
#else
    public const int Iterations = 100000;
#endif

    // Shorts feeding math operations.
    //
    // Inlining in Bench0xp should enable constant folding
    // Inlining in Bench0xn will not enable constant folding

    static short Five = 5;
    static short Ten = 10;

    static short Id(short x)
    {
        return x;
    }

    static short F00(short x)
    {
        return (short) (x * x);
    }

    static bool Bench00p()
    {
        short t = 10;
        short f = F00(t);
        return (f == 100);
    }

    static bool Bench00n()
    {
        short t = Ten;
        short f = F00(t);
        return (f == 100);
    }

    static bool Bench00p1()
    {
        short t = Id(10);
        short f = F00(t);
        return (f == 100);
    }

    static bool Bench00n1()
    {
        short t = Id(Ten);
        short f = F00(t);
        return (f == 100);
    }

    static bool Bench00p2()
    {
        short t = Id(10);
        short f = F00(Id(t));
        return (f == 100);
    }

    static bool Bench00n2()
    {
        short t = Id(Ten);
        short f = F00(Id(t));
        return (f == 100);
    }

    static bool Bench00p3()
    {
        short t = 10;
        short f = F00(Id(Id(t)));
        return (f == 100);
    }

    static bool Bench00n3()
    {
        short t = Ten;
        short f = F00(Id(Id(t)));
        return (f == 100);
    }

    static bool Bench00p4()
    {
        short t = 5;
        short f = F00((short)(2 * t));
        return (f == 100);
    }

    static bool Bench00n4()
    {
        short t = Five;
        short f = F00((short)(2 * t));
        return (f == 100);
    }

    static short F01(short x)
    {
        return (short)(1000 / x);
    }

    static bool Bench01p()
    {
        short t = 10;
        short f = F01(t);
        return (f == 100);
    }

    static bool Bench01n()
    {
        short t = Ten;
        short f = F01(t);
        return (f == 100);
    }

    static short F02(short x)
    {
        return (short) (20 * (x / 2));
    }

    static bool Bench02p()
    {
        short t = 10;
        short f = F02(t);
        return (f == 100);
    }

    static bool Bench02n()
    {
        short t = Ten;
        short f = F02(t);
        return (f == 100);
    }

    static short F03(short x)
    {
        return (short)(91 + 1009 % x);
    }

    static bool Bench03p()
    {
        short t = 10;
        short f = F03(t);
        return (f == 100);
    }

    static bool Bench03n()
    {
        short t = Ten;
        short f = F03(t);
        return (f == 100);
    }

    static short F04(short x)
    {
        return (short)(50 * (x % 4));
    }

    static bool Bench04p()
    {
        short t = 10;
        short f = F04(t);
        return (f == 100);
    }

    static bool Bench04n()
    {
        short t = Ten;
        short f = F04(t);
        return (f == 100);
    }

    static short F05(short x)
    {
        return (short)((1 << x) - 924);
    }

    static bool Bench05p()
    {
        short t = 10;
        short f = F05(t);
        return (f == 100);
    }

    static bool Bench05n()
    {
        short t = Ten;
        short f = F05(t);
        return (f == 100);
    }

    static short F051(short x)
    {
        return (short)(102400 >> x);
    }

    static bool Bench05p1()
    {
        short t = 10;
        short f = F051(t);
        return (f == 100);
    }

    static bool Bench05n1()
    {
        short t = Ten;
        short f = F051(t);
        return (f == 100);
    }

    static short F06(short x)
    {
        return (short)(-x + 110);
    }

    static bool Bench06p()
    {
        short t = 10;
        short f = F06(t);
        return (f == 100);
    }

    static bool Bench06n()
    {
        short t = Ten;
        short f = F06(t);
        return (f == 100);
    }

    static short F07(short x)
    {
        return (short)(~x + 111);
    }

    static bool Bench07p()
    {
        short t = 10;
        short f = F07(t);
        return (f == 100);
    }

    static bool Bench07n()
    {
        short t = Ten;
        short f = F07(t);
        return (f == 100);
    }

    static short F071(short x)
    {
        return (short)((x ^ -1) + 111);
    }

    static bool Bench07p1()
    {
        short t = 10;
        short f = F071(t);
        return (f == 100);
    }

    static bool Bench07n1()
    {
        short t = Ten;
        short f = F071(t);
        return (f == 100);
    }

    static short F08(short x)
    {
        return (short)((x & 0x7) + 98);
    }

    static bool Bench08p()
    {
        short t = 10;
        short f = F08(t);
        return (f == 100);
    }

    static bool Bench08n()
    {
        short t = Ten;
        short f = F08(t);
        return (f == 100);
    }

    static short F09(short x)
    {
        return (short)((x | 0x7) + 85);
    }

    static bool Bench09p()
    {
        short t = 10;
        short f = F09(t);
        return (f == 100);
    }

    static bool Bench09n()
    {
        short t = Ten;
        short f = F09(t);
        return (f == 100);
    }

    // Shorts feeding comparisons.
    //
    // Inlining in Bench1xp should enable branch optimization
    // Inlining in Bench1xn will not enable branch optimization

    static short F10(short x)
    {
        return x == 10 ? (short) 100 : (short) 0;
    }

    static bool Bench10p()
    {
        short t = 10;
        short f = F10(t);
        return (f == 100);
    }

    static bool Bench10n()
    {
        short t = Ten;
        short f = F10(t);
        return (f == 100);
    }

    static short F101(short x)
    {
        return x != 10 ? (short) 0 : (short) 100;
    }

    static bool Bench10p1()
    {
        short t = 10;
        short f = F101(t);
        return (f == 100);
    }

    static bool Bench10n1()
    {
        short t = Ten;
        short f = F101(t);
        return (f == 100);
    }

    static short F102(short x)
    {
        return x >= 10 ? (short) 100 : (short) 0;
    }

    static bool Bench10p2()
    {
        short t = 10;
        short f = F102(t);
        return (f == 100);
    }

    static bool Bench10n2()
    {
        short t = Ten;
        short f = F102(t);
        return (f == 100);
    }

    static short F103(short x)
    {
        return x <= 10 ? (short) 100 : (short) 0;
    }

    static bool Bench10p3()
    {
        short t = 10;
        short f = F103(t);
        return (f == 100);
    }

    static bool Bench10n3()
    {
        short t = Ten;
        short f = F102(t);
        return (f == 100);
    }

    static short F11(short x)
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
        short t = 10;
        short f = F11(t);
        return (f == 100);
    }

    static bool Bench11n()
    {
        short t = Ten;
        short f = F11(t);
        return (f == 100);
    }

    static short F111(short x)
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
        short t = 10;
        short f = F111(t);
        return (f == 100);
    }

    static bool Bench11n1()
    {
        short t = Ten;
        short f = F111(t);
        return (f == 100);
    }

    static short F112(short x)
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
        short t = 10;
        short f = F112(t);
        return (f == 100);
    }

    static bool Bench11n2()
    {
        short t = Ten;
        short f = F112(t);
        return (f == 100);
    }
    static short F113(short x)
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
        short t = 10;
        short f = F113(t);
        return (f == 100);
    }

    static bool Bench11n3()
    {
        short t = Ten;
        short f = F113(t);
        return (f == 100);
    }

    // Ununsed (or effectively unused) parameters
    //
    // Simple callee analysis may overstate inline benefit

    static short F20(short x)
    {
        return 100;
    }

    static bool Bench20p()
    {
        short t = 10;
        short f = F20(t);
        return (f == 100);
    }

    static bool Bench20p1()
    {
        short t = Ten;
        short f = F20(t);
        return (f == 100);
    }

    static short F21(short x)
    {
        return (short)(-x + 100 + x);
    }

    static bool Bench21p()
    {
        short t = 10;
        short f = F21(t);
        return (f == 100);
    }

    static bool Bench21n()
    {
        short t = Ten;
        short f = F21(t);
        return (f == 100);
    }

    static short F211(short x)
    {
        return (short)(x - x + 100);
    }

    static bool Bench21p1()
    {
        short t = 10;
        short f = F211(t);
        return (f == 100);
    }

    static bool Bench21n1()
    {
        short t = Ten;
        short f = F211(t);
        return (f == 100);
    }

    static short F22(short x)
    {
        if (x > 0)
        {
            return 100;
        }

        return 100;
    }

    static bool Bench22p()
    {
        short t = 10;
        short f = F22(t);
        return (f == 100);
    }

    static bool Bench22p1()
    {
        short t = Ten;
        short f = F22(t);
        return (f == 100);
    }

    static short F23(short x)
    {
        if (x > 0)
        {
            return (short)(90 + x);
        }

        return 100;
    }

    static bool Bench23p()
    {
        short t = 10;
        short f = F23(t);
        return (f == 100);
    }

    static bool Bench23n()
    {
        short t = Ten;
        short f = F23(t);
        return (f == 100);
    }

    // Multiple parameters

    static short F30(short x, short y)
    {
        return (short)(y * y);
    }

    static bool Bench30p()
    {
        short t = 10;
        short f = F30(t, t);
        return (f == 100);
    }

    static bool Bench30n()
    {
        short t = Ten;
        short f = F30(t, t);
        return (f == 100);
    }

    static bool Bench30p1()
    {
        short s = Ten;
        short t = 10;
        short f = F30(s, t);
        return (f == 100);
    }

    static bool Bench30n1()
    {
        short s = 10;
        short t = Ten;
        short f = F30(s, t);
        return (f == 100);
    }

    static bool Bench30p2()
    {
        short s = 10;
        short t = 10;
        short f = F30(s, t);
        return (f == 100);
    }

    static bool Bench30n2()
    {
        short s = Ten;
        short t = Ten;
        short f = F30(s, t);
        return (f == 100);
    }

    static bool Bench30p3()
    {
        short s = 10;
        short t = s;
        short f = F30(s, t);
        return (f == 100);
    }

    static bool Bench30n3()
    {
        short s = Ten;
        short t = s;
        short f = F30(s, t);
        return (f == 100);
    }

    static short F31(short x, short y, short z)
    {
        return (short)(z * z);
    }

    static bool Bench31p()
    {
        short t = 10;
        short f = F31(t, t, t);
        return (f == 100);
    }

    static bool Bench31n()
    {
        short t = Ten;
        short f = F31(t, t, t);
        return (f == 100);
    }

    static bool Bench31p1()
    {
        short r = Ten;
        short s = Ten;
        short t = 10;
        short f = F31(r, s, t);
        return (f == 100);
    }

    static bool Bench31n1()
    {
        short r = 10;
        short s = 10;
        short t = Ten;
        short f = F31(r, s, t);
        return (f == 100);
    }

    static bool Bench31p2()
    {
        short r = 10;
        short s = 10;
        short t = 10;
        short f = F31(r, s, t);
        return (f == 100);
    }

    static bool Bench31n2()
    {
        short r = Ten;
        short s = Ten;
        short t = Ten;
        short f = F31(r, s, t);
        return (f == 100);
    }

    static bool Bench31p3()
    {
        short r = 10;
        short s = r;
        short t = s;
        short f = F31(r, s, t);
        return (f == 100);
    }

    static bool Bench31n3()
    {
        short r = Ten;
        short s = r;
        short t = s;
        short f = F31(r, s, t);
        return (f == 100);
    }

    // Two args, both used

    static short F40(short x, short y)
    {
        return (short)(x * x + y * y - 100);
    }

    static bool Bench40p()
    {
        short t = 10;
        short f = F40(t, t);
        return (f == 100);
    }

    static bool Bench40n()
    {
        short t = Ten;
        short f = F40(t, t);
        return (f == 100);
    }

    static bool Bench40p1()
    {
        short s = Ten;
        short t = 10;
        short f = F40(s, t);
        return (f == 100);
    }

    static bool Bench40p2()
    {
        short s = 10;
        short t = Ten;
        short f = F40(s, t);
        return (f == 100);
    }

    static short F41(short x, short y)
    {
        return (short)(x * y);
    }

    static bool Bench41p()
    {
        short t = 10;
        short f = F41(t, t);
        return (f == 100);
    }

    static bool Bench41n()
    {
        short t = Ten;
        short f = F41(t, t);
        return (f == 100);
    }

    static bool Bench41p1()
    {
        short s = 10;
        short t = Ten;
        short f = F41(s, t);
        return (f == 100);
    }

    static bool Bench41p2()
    {
        short s = Ten;
        short t = 10;
        short f = F41(s, t);
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
        TypeInfo t = typeof(ConstantArgsShort).GetTypeInfo();
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
