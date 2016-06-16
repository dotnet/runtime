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

public static class ConstantArgsFloat
{

#if DEBUG
    public const int Iterations = 1;
#else
    public const int Iterations = 100000;
#endif

    // Floats feeding math operations.
    //
    // Inlining in Bench0xp should enable constant folding
    // Inlining in Bench0xn will not enable constant folding

    static float Five = 5;
    static float Ten = 10;

    static float Id(float x)
    {
        return x;
    }

    static float F00(float x)
    {
        return x * x;
    }

    static bool Bench00p()
    {
        float t = 10;
        float f = F00(t);
        return (f == 100);
    }

    static bool Bench00n()
    {
        float t = Ten;
        float f = F00(t);
        return (f == 100);
    }

    static bool Bench00p1()
    {
        float t = Id(10);
        float f = F00(t);
        return (f == 100);
    }

    static bool Bench00n1()
    {
        float t = Id(Ten);
        float f = F00(t);
        return (f == 100);
    }

    static bool Bench00p2()
    {
        float t = Id(10);
        float f = F00(Id(t));
        return (f == 100);
    }

    static bool Bench00n2()
    {
        float t = Id(Ten);
        float f = F00(Id(t));
        return (f == 100);
    }

    static bool Bench00p3()
    {
        float t = 10;
        float f = F00(Id(Id(t)));
        return (f == 100);
    }

    static bool Bench00n3()
    {
        float t = Ten;
        float f = F00(Id(Id(t)));
        return (f == 100);
    }

    static bool Bench00p4()
    {
        float t = 5;
        float f = F00(2 * t);
        return (f == 100);
    }

    static bool Bench00n4()
    {
        float t = Five;
        float f = F00(2 * t);
        return (f == 100);
    }

    static float F01(float x)
    {
        return 1000 / x;
    }

    static bool Bench01p()
    {
        float t = 10;
        float f = F01(t);
        return (f == 100);
    }

    static bool Bench01n()
    {
        float t = Ten;
        float f = F01(t);
        return (f == 100);
    }

    static float F02(float x)
    {
        return 20 * (x / 2);
    }

    static bool Bench02p()
    {
        float t = 10;
        float f = F02(t);
        return (f == 100);
    }

    static bool Bench02n()
    {
        float t = Ten;
        float f = F02(t);
        return (f == 100);
    }

    static float F03(float x)
    {
        return 91 + 1009 % x;
    }

    static bool Bench03p()
    {
        float t = 10;
        float f = F03(t);
        return (f == 100);
    }

    static bool Bench03n()
    {
        float t = Ten;
        float f = F03(t);
        return (f == 100);
    }

    static float F04(float x)
    {
        return 50 * (x % 4);
    }

    static bool Bench04p()
    {
        float t = 10;
        float f = F04(t);
        return (f == 100);
    }

    static bool Bench04n()
    {
        float t = Ten;
        float f = F04(t);
        return (f == 100);
    }

    static float F06(float x)
    {
        return -x + 110;
    }

    static bool Bench06p()
    {
        float t = 10;
        float f = F06(t);
        return (f == 100);
    }

    static bool Bench06n()
    {
        float t = Ten;
        float f = F06(t);
        return (f == 100);
    }

    // Floats feeding comparisons.
    //
    // Inlining in Bench1xp should enable branch optimization
    // Inlining in Bench1xn will not enable branch optimization

    static float F10(float x)
    {
        return x == 10 ? 100 : 0;
    }

    static bool Bench10p()
    {
        float t = 10;
        float f = F10(t);
        return (f == 100);
    }

    static bool Bench10n()
    {
        float t = Ten;
        float f = F10(t);
        return (f == 100);
    }

    static float F101(float x)
    {
        return x != 10 ? 0 : 100;
    }

    static bool Bench10p1()
    {
        float t = 10;
        float f = F101(t);
        return (f == 100);
    }

    static bool Bench10n1()
    {
        float t = Ten;
        float f = F101(t);
        return (f == 100);
    }

    static float F102(float x)
    {
        return x >= 10 ? 100 : 0;
    }

    static bool Bench10p2()
    {
        float t = 10;
        float f = F102(t);
        return (f == 100);
    }

    static bool Bench10n2()
    {
        float t = Ten;
        float f = F102(t);
        return (f == 100);
    }

    static float F103(float x)
    {
        return x <= 10 ? 100 : 0;
    }

    static bool Bench10p3()
    {
        float t = 10;
        float f = F103(t);
        return (f == 100);
    }

    static bool Bench10n3()
    {
        float t = Ten;
        float f = F102(t);
        return (f == 100);
    }

    static float F11(float x)
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
        float t = 10;
        float f = F11(t);
        return (f == 100);
    }

    static bool Bench11n()
    {
        float t = Ten;
        float f = F11(t);
        return (f == 100);
    }

    static float F111(float x)
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
        float t = 10;
        float f = F111(t);
        return (f == 100);
    }

    static bool Bench11n1()
    {
        float t = Ten;
        float f = F111(t);
        return (f == 100);
    }

    static float F112(float x)
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
        float t = 10;
        float f = F112(t);
        return (f == 100);
    }

    static bool Bench11n2()
    {
        float t = Ten;
        float f = F112(t);
        return (f == 100);
    }
    static float F113(float x)
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
        float t = 10;
        float f = F113(t);
        return (f == 100);
    }

    static bool Bench11n3()
    {
        float t = Ten;
        float f = F113(t);
        return (f == 100);
    }

    // Ununsed (or effectively unused) parameters
    //
    // Simple callee analysis may overstate inline benefit

    static float F20(float x)
    {
        return 100;
    }

    static bool Bench20p()
    {
        float t = 10;
        float f = F20(t);
        return (f == 100);
    }

    static bool Bench20p1()
    {
        float t = Ten;
        float f = F20(t);
        return (f == 100);
    }

    static float F21(float x)
    {
        return -x + 100 + x;
    }

    static bool Bench21p()
    {
        float t = 10;
        float f = F21(t);
        return (f == 100);
    }

    static bool Bench21n()
    {
        float t = Ten;
        float f = F21(t);
        return (f == 100);
    }

    static float F211(float x)
    {
        return x - x + 100;
    }

    static bool Bench21p1()
    {
        float t = 10;
        float f = F211(t);
        return (f == 100);
    }

    static bool Bench21n1()
    {
        float t = Ten;
        float f = F211(t);
        return (f == 100);
    }

    static float F22(float x)
    {
        if (x > 0)
        {
            return 100;
        }

        return 100;
    }

    static bool Bench22p()
    {
        float t = 10;
        float f = F22(t);
        return (f == 100);
    }

    static bool Bench22p1()
    {
        float t = Ten;
        float f = F22(t);
        return (f == 100);
    }

    static float F23(float x)
    {
        if (x > 0)
        {
            return 90 + x;
        }

        return 100;
    }

    static bool Bench23p()
    {
        float t = 10;
        float f = F23(t);
        return (f == 100);
    }

    static bool Bench23n()
    {
        float t = Ten;
        float f = F23(t);
        return (f == 100);
    }

    // Multiple parameters

    static float F30(float x, float y)
    {
        return y * y;
    }

    static bool Bench30p()
    {
        float t = 10;
        float f = F30(t, t);
        return (f == 100);
    }

    static bool Bench30n()
    {
        float t = Ten;
        float f = F30(t, t);
        return (f == 100);
    }

    static bool Bench30p1()
    {
        float s = Ten;
        float t = 10;
        float f = F30(s, t);
        return (f == 100);
    }

    static bool Bench30n1()
    {
        float s = 10;
        float t = Ten;
        float f = F30(s, t);
        return (f == 100);
    }

    static bool Bench30p2()
    {
        float s = 10;
        float t = 10;
        float f = F30(s, t);
        return (f == 100);
    }

    static bool Bench30n2()
    {
        float s = Ten;
        float t = Ten;
        float f = F30(s, t);
        return (f == 100);
    }

    static bool Bench30p3()
    {
        float s = 10;
        float t = s;
        float f = F30(s, t);
        return (f == 100);
    }

    static bool Bench30n3()
    {
        float s = Ten;
        float t = s;
        float f = F30(s, t);
        return (f == 100);
    }

    static float F31(float x, float y, float z)
    {
        return z * z;
    }

    static bool Bench31p()
    {
        float t = 10;
        float f = F31(t, t, t);
        return (f == 100);
    }

    static bool Bench31n()
    {
        float t = Ten;
        float f = F31(t, t, t);
        return (f == 100);
    }

    static bool Bench31p1()
    {
        float r = Ten;
        float s = Ten;
        float t = 10;
        float f = F31(r, s, t);
        return (f == 100);
    }

    static bool Bench31n1()
    {
        float r = 10;
        float s = 10;
        float t = Ten;
        float f = F31(r, s, t);
        return (f == 100);
    }

    static bool Bench31p2()
    {
        float r = 10;
        float s = 10;
        float t = 10;
        float f = F31(r, s, t);
        return (f == 100);
    }

    static bool Bench31n2()
    {
        float r = Ten;
        float s = Ten;
        float t = Ten;
        float f = F31(r, s, t);
        return (f == 100);
    }

    static bool Bench31p3()
    {
        float r = 10;
        float s = r;
        float t = s;
        float f = F31(r, s, t);
        return (f == 100);
    }

    static bool Bench31n3()
    {
        float r = Ten;
        float s = r;
        float t = s;
        float f = F31(r, s, t);
        return (f == 100);
    }

    // Two args, both used

    static float F40(float x, float y)
    {
        return x * x + y * y - 100;
    }

    static bool Bench40p()
    {
        float t = 10;
        float f = F40(t, t);
        return (f == 100);
    }

    static bool Bench40n()
    {
        float t = Ten;
        float f = F40(t, t);
        return (f == 100);
    }

    static bool Bench40p1()
    {
        float s = Ten;
        float t = 10;
        float f = F40(s, t);
        return (f == 100);
    }

    static bool Bench40p2()
    {
        float s = 10;
        float t = Ten;
        float f = F40(s, t);
        return (f == 100);
    }

    static float F41(float x, float y)
    {
        return x * y;
    }

    static bool Bench41p()
    {
        float t = 10;
        float f = F41(t, t);
        return (f == 100);
    }

    static bool Bench41n()
    {
        float t = Ten;
        float f = F41(t, t);
        return (f == 100);
    }

    static bool Bench41p1()
    {
        float s = 10;
        float t = Ten;
        float f = F41(s, t);
        return (f == 100);
    }

    static bool Bench41p2()
    {
        float s = Ten;
        float t = 10;
        float f = F41(s, t);
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
        TypeInfo t = typeof(ConstantArgsFloat).GetTypeInfo();
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
