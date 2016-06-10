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

public static class ConstantArgsChar
{

#if DEBUG
    public const int Iterations = 1;
#else
    public const int Iterations = 100000;
#endif

    // Chars feeding math operations.
    //
    // Inlining in Bench0xp should enable constant folding
    // Inlining in Bench0xn will not enable constant folding

    static char Five = (char) 5;
    static char Ten = (char) 10;

    static char Id(char x)
    {
        return x;
    }

    static char F00(char x)
    {
        return (char) (x * x);
    }

    static bool Bench00p()
    {
        char t = (char) 10;
        char f = F00(t);
        return (f == 100);
    }

    static bool Bench00n()
    {
        char t = Ten;
        char f = F00(t);
        return (f == 100);
    }

    static bool Bench00p1()
    {
        char t = Id((char)10);
        char f = F00(t);
        return (f == 100);
    }

    static bool Bench00n1()
    {
        char t = Id(Ten);
        char f = F00(t);
        return (f == 100);
    }

    static bool Bench00p2()
    {
        char t = Id((char)10);
        char f = F00(Id(t));
        return (f == 100);
    }

    static bool Bench00n2()
    {
        char t = Id(Ten);
        char f = F00(Id(t));
        return (f == 100);
    }

    static bool Bench00p3()
    {
        char t = (char) 10;
        char f = F00(Id(Id(t)));
        return (f == 100);
    }

    static bool Bench00n3()
    {
        char t = Ten;
        char f = F00(Id(Id(t)));
        return (f == 100);
    }

    static bool Bench00p4()
    {
        char t = (char) 5;
        char f = F00((char)(2 * t));
        return (f == 100);
    }

    static bool Bench00n4()
    {
        char t = Five;
        char f = F00((char)(2 * t));
        return (f == 100);
    }

    static char F01(char x)
    {
        return (char)(1000 / x);
    }

    static bool Bench01p()
    {
        char t = (char) 10;
        char f = F01(t);
        return (f == 100);
    }

    static bool Bench01n()
    {
        char t = Ten;
        char f = F01(t);
        return (f == 100);
    }

    static char F02(char x)
    {
        return (char) (20 * (x / 2));
    }

    static bool Bench02p()
    {
        char t = (char) 10;
        char f = F02(t);
        return (f == 100);
    }

    static bool Bench02n()
    {
        char t = Ten;
        char f = F02(t);
        return (f == 100);
    }

    static char F03(char x)
    {
        return (char)(91 + 1009 % x);
    }

    static bool Bench03p()
    {
        char t = (char) 10;
        char f = F03(t);
        return (f == 100);
    }

    static bool Bench03n()
    {
        char t = Ten;
        char f = F03(t);
        return (f == 100);
    }

    static char F04(char x)
    {
        return (char)(50 * (x % 4));
    }

    static bool Bench04p()
    {
        char t = (char) 10;
        char f = F04(t);
        return (f == 100);
    }

    static bool Bench04n()
    {
        char t = Ten;
        char f = F04(t);
        return (f == 100);
    }

    static char F05(char x)
    {
        return (char)((1 << x) - 924);
    }

    static bool Bench05p()
    {
        char t = (char) 10;
        char f = F05(t);
        return (f == 100);
    }

    static bool Bench05n()
    {
        char t = Ten;
        char f = F05(t);
        return (f == 100);
    }

    static char F051(char x)
    {
        return (char)(102400 >> x);
    }

    static bool Bench05p1()
    {
        char t = (char) 10;
        char f = F051(t);
        return (f == 100);
    }

    static bool Bench05n1()
    {
        char t = Ten;
        char f = F051(t);
        return (f == 100);
    }

    static char F06(char x)
    {
        return (char)(-x + 110);
    }

    static bool Bench06p()
    {
        char t = (char) 10;
        char f = F06(t);
        return (f == 100);
    }

    static bool Bench06n()
    {
        char t = Ten;
        char f = F06(t);
        return (f == 100);
    }

    static char F07(char x)
    {
        return (char)(~x + 111);
    }

    static bool Bench07p()
    {
        char t = (char) 10;
        char f = F07(t);
        return (f == 100);
    }

    static bool Bench07n()
    {
        char t = Ten;
        char f = F07(t);
        return (f == 100);
    }

    static char F071(char x)
    {
        return (char)((x ^ -1) + 111);
    }

    static bool Bench07p1()
    {
        char t = (char) 10;
        char f = F071(t);
        return (f == 100);
    }

    static bool Bench07n1()
    {
        char t = Ten;
        char f = F071(t);
        return (f == 100);
    }

    static char F08(char x)
    {
        return (char)((x & 0x7) + 98);
    }

    static bool Bench08p()
    {
        char t = (char) 10;
        char f = F08(t);
        return (f == 100);
    }

    static bool Bench08n()
    {
        char t = Ten;
        char f = F08(t);
        return (f == 100);
    }

    static char F09(char x)
    {
        return (char)((x | 0x7) + 85);
    }

    static bool Bench09p()
    {
        char t = (char) 10;
        char f = F09(t);
        return (f == 100);
    }

    static bool Bench09n()
    {
        char t = Ten;
        char f = F09(t);
        return (f == 100);
    }

    // Chars feeding comparisons.
    //
    // Inlining in Bench1xp should enable branch optimization
    // Inlining in Bench1xn will not enable branch optimization

    static char F10(char x)
    {
        return x == 10 ? (char) 100 : (char) 0;
    }

    static bool Bench10p()
    {
        char t = (char) 10;
        char f = F10(t);
        return (f == 100);
    }

    static bool Bench10n()
    {
        char t = Ten;
        char f = F10(t);
        return (f == 100);
    }

    static char F101(char x)
    {
        return x != 10 ? (char) 0 : (char) 100;
    }

    static bool Bench10p1()
    {
        char t = (char) 10;
        char f = F101(t);
        return (f == 100);
    }

    static bool Bench10n1()
    {
        char t = Ten;
        char f = F101(t);
        return (f == 100);
    }

    static char F102(char x)
    {
        return x >= 10 ? (char) 100 : (char) 0;
    }

    static bool Bench10p2()
    {
        char t = (char) 10;
        char f = F102(t);
        return (f == 100);
    }

    static bool Bench10n2()
    {
        char t = Ten;
        char f = F102(t);
        return (f == 100);
    }

    static char F103(char x)
    {
        return x <= 10 ? (char) 100 : (char) 0;
    }

    static bool Bench10p3()
    {
        char t = (char) 10;
        char f = F103(t);
        return (f == 100);
    }

    static bool Bench10n3()
    {
        char t = Ten;
        char f = F102(t);
        return (f == 100);
    }

    static char F11(char x)
    {
        if (x == 10)
        {
            return (char) 100;
        }
        else
        {
            return (char) 0;
        }
    }

    static bool Bench11p()
    {
        char t = (char) 10;
        char f = F11(t);
        return (f == 100);
    }

    static bool Bench11n()
    {
        char t = Ten;
        char f = F11(t);
        return (f == 100);
    }

    static char F111(char x)
    {
        if (x != 10)
        {
            return (char) 0;
        }
        else
        {
            return (char) 100;
        }
    }

    static bool Bench11p1()
    {
        char t = (char) 10;
        char f = F111(t);
        return (f == 100);
    }

    static bool Bench11n1()
    {
        char t = Ten;
        char f = F111(t);
        return (f == 100);
    }

    static char F112(char x)
    {
        if (x > 10)
        {
            return (char) 0;
        }
        else
        {
            return (char) 100;
        }
    }

    static bool Bench11p2()
    {
        char t = (char) 10;
        char f = F112(t);
        return (f == 100);
    }

    static bool Bench11n2()
    {
        char t = Ten;
        char f = F112(t);
        return (f == 100);
    }
    static char F113(char x)
    {
        if (x < 10)
        {
            return (char) 0;
        }
        else
        {
            return (char) 100;
        }
    }

    static bool Bench11p3()
    {
        char t = (char) 10;
        char f = F113(t);
        return (f == 100);
    }

    static bool Bench11n3()
    {
        char t = Ten;
        char f = F113(t);
        return (f == 100);
    }

    // Ununsed (or effectively unused) parameters
    //
    // Simple callee analysis may overstate inline benefit

    static char F20(char x)
    {
        return (char) 100;
    }

    static bool Bench20p()
    {
        char t = (char) 10;
        char f = F20(t);
        return (f == 100);
    }

    static bool Bench20p1()
    {
        char t = Ten;
        char f = F20(t);
        return (f == 100);
    }

    static char F21(char x)
    {
        return (char)(-x + 100 + x);
    }

    static bool Bench21p()
    {
        char t = (char) 10;
        char f = F21(t);
        return (f == 100);
    }

    static bool Bench21n()
    {
        char t = Ten;
        char f = F21(t);
        return (f == 100);
    }

    static char F211(char x)
    {
        return (char)(x - x + 100);
    }

    static bool Bench21p1()
    {
        char t = (char) 10;
        char f = F211(t);
        return (f == 100);
    }

    static bool Bench21n1()
    {
        char t = Ten;
        char f = F211(t);
        return (f == 100);
    }

    static char F22(char x)
    {
        if (x > 0)
        {
            return (char) 100;
        }

        return (char) 100;
    }

    static bool Bench22p()
    {
        char t = (char) 10;
        char f = F22(t);
        return (f == 100);
    }

    static bool Bench22p1()
    {
        char t = Ten;
        char f = F22(t);
        return (f == 100);
    }

    static char F23(char x)
    {
        if (x > 0)
        {
            return (char)(90 + x);
        }

        return (char) 100;
    }

    static bool Bench23p()
    {
        char t = (char) 10;
        char f = F23(t);
        return (f == 100);
    }

    static bool Bench23n()
    {
        char t = Ten;
        char f = F23(t);
        return (f == 100);
    }

    // Multiple parameters

    static char F30(char x, char y)
    {
        return (char)(y * y);
    }

    static bool Bench30p()
    {
        char t = (char) 10;
        char f = F30(t, t);
        return (f == 100);
    }

    static bool Bench30n()
    {
        char t = Ten;
        char f = F30(t, t);
        return (f == 100);
    }

    static bool Bench30p1()
    {
        char s = Ten;
        char t = (char) 10;
        char f = F30(s, t);
        return (f == 100);
    }

    static bool Bench30n1()
    {
        char s = (char) 10;
        char t = Ten;
        char f = F30(s, t);
        return (f == 100);
    }

    static bool Bench30p2()
    {
        char s = (char) 10;
        char t = (char) 10;
        char f = F30(s, t);
        return (f == 100);
    }

    static bool Bench30n2()
    {
        char s = Ten;
        char t = Ten;
        char f = F30(s, t);
        return (f == 100);
    }

    static bool Bench30p3()
    {
        char s = (char) 10;
        char t = s;
        char f = F30(s, t);
        return (f == 100);
    }

    static bool Bench30n3()
    {
        char s = Ten;
        char t = s;
        char f = F30(s, t);
        return (f == 100);
    }

    static char F31(char x, char y, char z)
    {
        return (char)(z * z);
    }

    static bool Bench31p()
    {
        char t = (char) 10;
        char f = F31(t, t, t);
        return (f == 100);
    }

    static bool Bench31n()
    {
        char t = Ten;
        char f = F31(t, t, t);
        return (f == 100);
    }

    static bool Bench31p1()
    {
        char r = Ten;
        char s = Ten;
        char t = (char) 10;
        char f = F31(r, s, t);
        return (f == 100);
    }

    static bool Bench31n1()
    {
        char r = (char) 10;
        char s = (char) 10;
        char t = Ten;
        char f = F31(r, s, t);
        return (f == 100);
    }

    static bool Bench31p2()
    {
        char r = (char) 10;
        char s = (char) 10;
        char t = (char) 10;
        char f = F31(r, s, t);
        return (f == 100);
    }

    static bool Bench31n2()
    {
        char r = Ten;
        char s = Ten;
        char t = Ten;
        char f = F31(r, s, t);
        return (f == 100);
    }

    static bool Bench31p3()
    {
        char r = (char) 10;
        char s = r;
        char t = s;
        char f = F31(r, s, t);
        return (f == 100);
    }

    static bool Bench31n3()
    {
        char r = Ten;
        char s = r;
        char t = s;
        char f = F31(r, s, t);
        return (f == 100);
    }

    // Two args, both used

    static char F40(char x, char y)
    {
        return (char)(x * x + y * y - 100);
    }

    static bool Bench40p()
    {
        char t = (char) 10;
        char f = F40(t, t);
        return (f == 100);
    }

    static bool Bench40n()
    {
        char t = Ten;
        char f = F40(t, t);
        return (f == 100);
    }

    static bool Bench40p1()
    {
        char s = Ten;
        char t = (char) 10;
        char f = F40(s, t);
        return (f == 100);
    }

    static bool Bench40p2()
    {
        char s = (char) 10;
        char t = Ten;
        char f = F40(s, t);
        return (f == 100);
    }

    static char F41(char x, char y)
    {
        return (char)(x * y);
    }

    static bool Bench41p()
    {
        char t = (char) 10;
        char f = F41(t, t);
        return (f == 100);
    }

    static bool Bench41n()
    {
        char t = Ten;
        char f = F41(t, t);
        return (f == 100);
    }

    static bool Bench41p1()
    {
        char s = (char) 10;
        char t = Ten;
        char f = F41(s, t);
        return (f == 100);
    }

    static bool Bench41p2()
    {
        char s = Ten;
        char t = (char) 10;
        char f = F41(s, t);
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
        TypeInfo t = typeof(ConstantArgsChar).GetTypeInfo();
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
