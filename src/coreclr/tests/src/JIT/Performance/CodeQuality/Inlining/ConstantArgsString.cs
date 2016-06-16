// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Strings are the only ref class where there is built-in support for
// constant objects.

using Microsoft.Xunit.Performance;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Collections.Generic;
using Xunit;

[assembly: OptimizeForBenchmarks]
[assembly: MeasureInstructionsRetired]

public static class ConstantArgsString
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

    static string Ten = "Ten";
    static string Five = "Five";

    static string Id(string x)
    {
        return x;
    }

    static char F00(string x)
    {
        return x[0];
    }

    static bool Bench00p()
    {
        string t = "Ten";
        char f = F00(t);
        return (f == 'T');
    }

    static bool Bench00n()
    {
        string t = Ten;
        char f = F00(t);
        return (f == 'T');
    }

    static int F01(string x)
    {
        return x.Length;
    }

    static bool Bench01p()
    {
        string t = "Ten";
        int f = F01(t);
        return (f == 3);
    }

    static bool Bench01n()
    {
        string t = Ten;
        int f = F01(t);
        return (f == 3);
    }

    static bool Bench01p1()
    {
        return "Ten".Length == 3;
    }

    static bool Bench01n1()
    {
        return Ten.Length == 3;
    }

    static bool Bench02p()
    {
        return "Ten".Equals("Ten", StringComparison.Ordinal);
    }

    static bool Bench02n()
    {
        return "Ten".Equals(Ten, StringComparison.Ordinal);
    }

    static bool Bench02n1()
    {
        return Ten.Equals("Ten", StringComparison.Ordinal);
    }

    static bool Bench02n2()
    {
        return Ten.Equals(Ten, StringComparison.Ordinal);
    }

    static bool Bench03p()
    {
        return "Ten" == "Ten";
    }

    static bool Bench03n()
    {
        return "Ten" == Ten;
    }

    static bool Bench03n1()
    {
        return Ten == "Ten";
    }

    static bool Bench03n2()
    {
        return Ten == Ten;
    }

    static bool Bench04p()
    {
        return "Ten" != "Five";
    }

    static bool Bench04n()
    {
        return "Ten" != Five;
    }

    static bool Bench04n1()
    {
        return Ten != "Five";
    }

    static bool Bench04n2()
    {
        return Ten != Five;
    }

    static bool Bench05p()
    {
        string t = "Ten";
        return (t == t);
    }

    static bool Bench05n()
    {
        string t = Ten;
        return (t == t);
    }

    static bool Bench06p()
    {
        return "Ten" != null;
    }

    static bool Bench06n()
    {
        return Ten != null;
    }

    static bool Bench07p()
    {
        return !"Ten".Equals(null);
    }

    static bool Bench07n()
    {
        return !Ten.Equals(null);
    }

    static bool Bench08p()
    {
        return !"Ten".Equals("Five", StringComparison.Ordinal);
    }

    static bool Bench08n()
    {
        return !"Ten".Equals(Five, StringComparison.Ordinal);
    }

    static bool Bench08n1()
    {
        return !Ten.Equals("Five", StringComparison.Ordinal);
    }

    static bool Bench08n2()
    {
        return !Ten.Equals(Five, StringComparison.Ordinal);
    }

    static bool Bench09p()
    {
        return string.Equals("Ten", "Ten");
    }

    static bool Bench09n()
    {
        return string.Equals("Ten", Ten);
    }

    static bool Bench09n1()
    {
        return string.Equals(Ten, "Ten");
    }

    static bool Bench09n2()
    {
        return string.Equals(Ten, Ten);
    }

    static bool Bench10p()
    {
        return !string.Equals("Five", "Ten");
    }

    static bool Bench10n()
    {
        return !string.Equals(Five, "Ten");
    }

    static bool Bench10n1()
    {
        return !string.Equals("Five", Ten);
    }

    static bool Bench10n2()
    {
        return !string.Equals(Five, Ten);
    }

    static bool Bench11p()
    {
        return !string.Equals("Five", null);
    }

    static bool Bench11n()
    {
        return !string.Equals(Five, null);
    }

    private static IEnumerable<object[]> MakeArgs(params string[] args)
    {
        return args.Select(arg => new object[] { arg });
    }

    public static IEnumerable<object[]> TestFuncs = MakeArgs(
        "Bench00p",  "Bench00n",
        "Bench01p",  "Bench01n",
        "Bench01p1", "Bench01n1",
        "Bench02p",  "Bench02n",
        "Bench02n1", "Bench02n2",
        "Bench03p",  "Bench03n",
        "Bench03n1", "Bench03n2",
        "Bench04p",  "Bench04n",
        "Bench04n1", "Bench04n2",
        "Bench05p",  "Bench05n",
        "Bench06p",  "Bench06n",
        "Bench07p",  "Bench07n",
        "Bench08p",  "Bench08n",
        "Bench08n1", "Bench08n2",
        "Bench09p",  "Bench09n",
        "Bench09n1", "Bench09n2",
        "Bench10p",  "Bench10n",
        "Bench10n1", "Bench10n2",
        "Bench11p",  "Bench11n"
    );

    static Func<bool> LookupFunc(object o)
    {
        TypeInfo t = typeof(ConstantArgsString).GetTypeInfo();
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
