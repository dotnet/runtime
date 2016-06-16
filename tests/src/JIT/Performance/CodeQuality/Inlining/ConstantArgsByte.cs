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

public static class ConstantArgsByte
{

#if DEBUG
    public const int Iterations = 1;
#else
    public const int Iterations = 100000;
#endif

    // Bytes feeding math operations.
    //
    // Inlining in Bench0xp should enable constant folding
    // Inlining in Bench0xn will not enable constant folding

    static byte Five = 5;
    static byte Ten = 10;

    static byte Id(byte x)
    {
        return x;
    }

    static byte F00(byte x)
    {
        return (byte) (x * x);
    }

    static bool Bench00p()
    {
        byte t = 10;
        byte f = F00(t);
        return (f == 100);
    }

    static bool Bench00n()
    {
        byte t = Ten;
        byte f = F00(t);
        return (f == 100);
    }

    static bool Bench00p1()
    {
        byte t = Id(10);
        byte f = F00(t);
        return (f == 100);
    }

    static bool Bench00n1()
    {
        byte t = Id(Ten);
        byte f = F00(t);
        return (f == 100);
    }

    static bool Bench00p2()
    {
        byte t = Id(10);
        byte f = F00(Id(t));
        return (f == 100);
    }

    static bool Bench00n2()
    {
        byte t = Id(Ten);
        byte f = F00(Id(t));
        return (f == 100);
    }

    static bool Bench00p3()
    {
        byte t = 10;
        byte f = F00(Id(Id(t)));
        return (f == 100);
    }

    static bool Bench00n3()
    {
        byte t = Ten;
        byte f = F00(Id(Id(t)));
        return (f == 100);
    }

    static bool Bench00p4()
    {
        byte t = 5;
        byte f = F00((byte)(2 * t));
        return (f == 100);
    }

    static bool Bench00n4()
    {
        byte t = Five;
        byte f = F00((byte)(2 * t));
        return (f == 100);
    }

    static byte F01(byte x)
    {
        return (byte)(1000 / x);
    }

    static bool Bench01p()
    {
        byte t = 10;
        byte f = F01(t);
        return (f == 100);
    }

    static bool Bench01n()
    {
        byte t = Ten;
        byte f = F01(t);
        return (f == 100);
    }

    static byte F02(byte x)
    {
        return (byte) (20 * (x / 2));
    }

    static bool Bench02p()
    {
        byte t = 10;
        byte f = F02(t);
        return (f == 100);
    }

    static bool Bench02n()
    {
        byte t = Ten;
        byte f = F02(t);
        return (f == 100);
    }

    static byte F03(byte x)
    {
        return (byte)(91 + 1009 % x);
    }

    static bool Bench03p()
    {
        byte t = 10;
        byte f = F03(t);
        return (f == 100);
    }

    static bool Bench03n()
    {
        byte t = Ten;
        byte f = F03(t);
        return (f == 100);
    }

    static byte F04(byte x)
    {
        return (byte)(50 * (x % 4));
    }

    static bool Bench04p()
    {
        byte t = 10;
        byte f = F04(t);
        return (f == 100);
    }

    static bool Bench04n()
    {
        byte t = Ten;
        byte f = F04(t);
        return (f == 100);
    }

    static byte F05(byte x)
    {
        return (byte)((1 << x) - 924);
    }

    static bool Bench05p()
    {
        byte t = 10;
        byte f = F05(t);
        return (f == 100);
    }

    static bool Bench05n()
    {
        byte t = Ten;
        byte f = F05(t);
        return (f == 100);
    }

    static byte F051(byte x)
    {
        return (byte)(102400 >> x);
    }

    static bool Bench05p1()
    {
        byte t = 10;
        byte f = F051(t);
        return (f == 100);
    }

    static bool Bench05n1()
    {
        byte t = Ten;
        byte f = F051(t);
        return (f == 100);
    }

    static byte F06(byte x)
    {
        return (byte)(-x + 110);
    }

    static bool Bench06p()
    {
        byte t = 10;
        byte f = F06(t);
        return (f == 100);
    }

    static bool Bench06n()
    {
        byte t = Ten;
        byte f = F06(t);
        return (f == 100);
    }

    static byte F07(byte x)
    {
        return (byte)(~x + 111);
    }

    static bool Bench07p()
    {
        byte t = 10;
        byte f = F07(t);
        return (f == 100);
    }

    static bool Bench07n()
    {
        byte t = Ten;
        byte f = F07(t);
        return (f == 100);
    }

    static byte F071(byte x)
    {
        return (byte)((x ^ -1) + 111);
    }

    static bool Bench07p1()
    {
        byte t = 10;
        byte f = F071(t);
        return (f == 100);
    }

    static bool Bench07n1()
    {
        byte t = Ten;
        byte f = F071(t);
        return (f == 100);
    }

    static byte F08(byte x)
    {
        return (byte)((x & 0x7) + 98);
    }

    static bool Bench08p()
    {
        byte t = 10;
        byte f = F08(t);
        return (f == 100);
    }

    static bool Bench08n()
    {
        byte t = Ten;
        byte f = F08(t);
        return (f == 100);
    }

    static byte F09(byte x)
    {
        return (byte)((x | 0x7) + 85);
    }

    static bool Bench09p()
    {
        byte t = 10;
        byte f = F09(t);
        return (f == 100);
    }

    static bool Bench09n()
    {
        byte t = Ten;
        byte f = F09(t);
        return (f == 100);
    }

    // Bytes feeding comparisons.
    //
    // Inlining in Bench1xp should enable branch optimization
    // Inlining in Bench1xn will not enable branch optimization

    static byte F10(byte x)
    {
        return x == 10 ? (byte) 100 : (byte) 0;
    }

    static bool Bench10p()
    {
        byte t = 10;
        byte f = F10(t);
        return (f == 100);
    }

    static bool Bench10n()
    {
        byte t = Ten;
        byte f = F10(t);
        return (f == 100);
    }

    static byte F101(byte x)
    {
        return x != 10 ? (byte) 0 : (byte) 100;
    }

    static bool Bench10p1()
    {
        byte t = 10;
        byte f = F101(t);
        return (f == 100);
    }

    static bool Bench10n1()
    {
        byte t = Ten;
        byte f = F101(t);
        return (f == 100);
    }

    static byte F102(byte x)
    {
        return x >= 10 ? (byte) 100 : (byte) 0;
    }

    static bool Bench10p2()
    {
        byte t = 10;
        byte f = F102(t);
        return (f == 100);
    }

    static bool Bench10n2()
    {
        byte t = Ten;
        byte f = F102(t);
        return (f == 100);
    }

    static byte F103(byte x)
    {
        return x <= 10 ? (byte) 100 : (byte) 0;
    }

    static bool Bench10p3()
    {
        byte t = 10;
        byte f = F103(t);
        return (f == 100);
    }

    static bool Bench10n3()
    {
        byte t = Ten;
        byte f = F102(t);
        return (f == 100);
    }

    static byte F11(byte x)
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
        byte t = 10;
        byte f = F11(t);
        return (f == 100);
    }

    static bool Bench11n()
    {
        byte t = Ten;
        byte f = F11(t);
        return (f == 100);
    }

    static byte F111(byte x)
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
        byte t = 10;
        byte f = F111(t);
        return (f == 100);
    }

    static bool Bench11n1()
    {
        byte t = Ten;
        byte f = F111(t);
        return (f == 100);
    }

    static byte F112(byte x)
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
        byte t = 10;
        byte f = F112(t);
        return (f == 100);
    }

    static bool Bench11n2()
    {
        byte t = Ten;
        byte f = F112(t);
        return (f == 100);
    }
    static byte F113(byte x)
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
        byte t = 10;
        byte f = F113(t);
        return (f == 100);
    }

    static bool Bench11n3()
    {
        byte t = Ten;
        byte f = F113(t);
        return (f == 100);
    }

    // Ununsed (or effectively unused) parameters
    //
    // Simple callee analysis may overstate inline benefit

    static byte F20(byte x)
    {
        return 100;
    }

    static bool Bench20p()
    {
        byte t = 10;
        byte f = F20(t);
        return (f == 100);
    }

    static bool Bench20p1()
    {
        byte t = Ten;
        byte f = F20(t);
        return (f == 100);
    }

    static byte F21(byte x)
    {
        return (byte)(-x + 100 + x);
    }

    static bool Bench21p()
    {
        byte t = 10;
        byte f = F21(t);
        return (f == 100);
    }

    static bool Bench21n()
    {
        byte t = Ten;
        byte f = F21(t);
        return (f == 100);
    }

    static byte F211(byte x)
    {
        return (byte)(x - x + 100);
    }

    static bool Bench21p1()
    {
        byte t = 10;
        byte f = F211(t);
        return (f == 100);
    }

    static bool Bench21n1()
    {
        byte t = Ten;
        byte f = F211(t);
        return (f == 100);
    }

    static byte F22(byte x)
    {
        if (x > 0)
        {
            return 100;
        }

        return 100;
    }

    static bool Bench22p()
    {
        byte t = 10;
        byte f = F22(t);
        return (f == 100);
    }

    static bool Bench22p1()
    {
        byte t = Ten;
        byte f = F22(t);
        return (f == 100);
    }

    static byte F23(byte x)
    {
        if (x > 0)
        {
            return (byte)(90 + x);
        }

        return 100;
    }

    static bool Bench23p()
    {
        byte t = 10;
        byte f = F23(t);
        return (f == 100);
    }

    static bool Bench23n()
    {
        byte t = Ten;
        byte f = F23(t);
        return (f == 100);
    }

    // Multiple parameters

    static byte F30(byte x, byte y)
    {
        return (byte)(y * y);
    }

    static bool Bench30p()
    {
        byte t = 10;
        byte f = F30(t, t);
        return (f == 100);
    }

    static bool Bench30n()
    {
        byte t = Ten;
        byte f = F30(t, t);
        return (f == 100);
    }

    static bool Bench30p1()
    {
        byte s = Ten;
        byte t = 10;
        byte f = F30(s, t);
        return (f == 100);
    }

    static bool Bench30n1()
    {
        byte s = 10;
        byte t = Ten;
        byte f = F30(s, t);
        return (f == 100);
    }

    static bool Bench30p2()
    {
        byte s = 10;
        byte t = 10;
        byte f = F30(s, t);
        return (f == 100);
    }

    static bool Bench30n2()
    {
        byte s = Ten;
        byte t = Ten;
        byte f = F30(s, t);
        return (f == 100);
    }

    static bool Bench30p3()
    {
        byte s = 10;
        byte t = s;
        byte f = F30(s, t);
        return (f == 100);
    }

    static bool Bench30n3()
    {
        byte s = Ten;
        byte t = s;
        byte f = F30(s, t);
        return (f == 100);
    }

    static byte F31(byte x, byte y, byte z)
    {
        return (byte)(z * z);
    }

    static bool Bench31p()
    {
        byte t = 10;
        byte f = F31(t, t, t);
        return (f == 100);
    }

    static bool Bench31n()
    {
        byte t = Ten;
        byte f = F31(t, t, t);
        return (f == 100);
    }

    static bool Bench31p1()
    {
        byte r = Ten;
        byte s = Ten;
        byte t = 10;
        byte f = F31(r, s, t);
        return (f == 100);
    }

    static bool Bench31n1()
    {
        byte r = 10;
        byte s = 10;
        byte t = Ten;
        byte f = F31(r, s, t);
        return (f == 100);
    }

    static bool Bench31p2()
    {
        byte r = 10;
        byte s = 10;
        byte t = 10;
        byte f = F31(r, s, t);
        return (f == 100);
    }

    static bool Bench31n2()
    {
        byte r = Ten;
        byte s = Ten;
        byte t = Ten;
        byte f = F31(r, s, t);
        return (f == 100);
    }

    static bool Bench31p3()
    {
        byte r = 10;
        byte s = r;
        byte t = s;
        byte f = F31(r, s, t);
        return (f == 100);
    }

    static bool Bench31n3()
    {
        byte r = Ten;
        byte s = r;
        byte t = s;
        byte f = F31(r, s, t);
        return (f == 100);
    }

    // Two args, both used

    static byte F40(byte x, byte y)
    {
        return (byte)(x * x + y * y - 100);
    }

    static bool Bench40p()
    {
        byte t = 10;
        byte f = F40(t, t);
        return (f == 100);
    }

    static bool Bench40n()
    {
        byte t = Ten;
        byte f = F40(t, t);
        return (f == 100);
    }

    static bool Bench40p1()
    {
        byte s = Ten;
        byte t = 10;
        byte f = F40(s, t);
        return (f == 100);
    }

    static bool Bench40p2()
    {
        byte s = 10;
        byte t = Ten;
        byte f = F40(s, t);
        return (f == 100);
    }

    static byte F41(byte x, byte y)
    {
        return (byte)(x * y);
    }

    static bool Bench41p()
    {
        byte t = 10;
        byte f = F41(t, t);
        return (f == 100);
    }

    static bool Bench41n()
    {
        byte t = Ten;
        byte f = F41(t, t);
        return (f == 100);
    }

    static bool Bench41p1()
    {
        byte s = 10;
        byte t = Ten;
        byte f = F41(s, t);
        return (f == 100);
    }

    static bool Bench41p2()
    {
        byte s = Ten;
        byte t = 10;
        byte f = F41(s, t);
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
        TypeInfo t = typeof(ConstantArgsByte).GetTypeInfo();
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
