// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

public class InterpreterTest
{
    static int Main(string[] args)
    {
        RunInterpreterTests();
        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void RunInterpreterTests()
    {
//      Console.WriteLine("Run interp tests");
        if (SumN(50) != 1275)
            Environment.FailFast(null);
        if (Mul4(53, 24, 13, 131) != 2166216)
            Environment.FailFast(null);

        TestSwitch();

        if (!PowLoop(20, 10, 1661992960))
            Environment.FailFast(null);
    }

    public static int Mul4(int a, int b, int c, int d)
    {
        return a * b * c * d;
    }

    public static long SumN(int n)
    {
        if (n == 1)
            return 1;
        return (long)SumN(n - 1) + n;
    }

    public static int SwitchOp(int a, int b, int op)
    {
        switch (op)
        {
            case 0:
                return a + b;
            case 1:
                return a - b;
            case 2:
                return a * b;
            default:
               return 42;
        }
    }

    public static void TestSwitch()
    {
        int n0 = SwitchOp (20, 6, 0); // 26
        int n1 = SwitchOp (20, 6, 1); // 14
        int n2 = SwitchOp (20, 6, 2); // 120
        int n3 = SwitchOp (20, 6, 3); // 42

        if ((n0 + n1 + n2 + n3) != 202)
            Environment.FailFast(null);
    }

    public static bool PowLoop(int n, long nr, int expected)
    {
        long ret = 1;
        for (int i = 0; i < n; i++)
            ret *= nr;
        return (int)ret == expected;
    }
}
