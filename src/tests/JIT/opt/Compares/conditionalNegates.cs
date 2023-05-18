// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// unit test for the full range comparison optimization

using System;
using System.Runtime.CompilerServices;

public class ConditionalNegateTest
{

    [MethodImpl(MethodImplOptions.NoInlining)]
    static byte cneg_byte(byte op1, byte op2)
    {
        //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #42
        //ARM64-FULL-LINE-NEXT: csneg {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{gt|le}}
        return (byte) (op1 > 42 ? op2: -op1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static short cneg_short(short op1, short op2)
    {
        //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #43
        //ARM64-FULL-LINE-NEXT: csneg {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{gt|le}}
        return (short) (op1 <= 43 ? -op2 : op1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static short cneg_short_min_max(short op1)
    {
        //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #44
        //ARM64-FULL-LINE-NEXT: csel {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{gt|le}}
        return (short) (op1 > 44 ? -short.MaxValue : short.MaxValue);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int cneg_int(int op1, int op2)
    {
        //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #45
        //ARM64-FULL-LINE-NEXT: csneg {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{gt|le}}
        return op1 > 45 ? op2 : -op1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int cneg_int_min_max(int op1)
    {
        //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #46
        //ARM64-FULL-LINE-NEXT: csel {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{ge|lt}}
        return op1 >= 46 ? int.MaxValue : -int.MaxValue;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long cneg_long(long op1, long op2)
    {
        //ARM64-FULL-LINE: cmp {{x[0-9]+}}, #47
        //ARM64-FULL-LINE-NEXT: csneg {{x[0-9]+}}, {{x[0-9]+}}, {{x[0-9]+}}, {{ge|lt}}
        return op1 < 47 ? -op2 : op1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int cneg_float(float op1, int op2)
    {
        //ARM64-FULL-LINE: fcmp {{s[0-9]+}}, {{s[0-9]+}}
        //ARM64-FULL-LINE-NEXT: csneg {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{gt|le}}
        return op1 > 48.0f ? op2 : -op2;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int cneg_double(double op1, int op2)
    {
        //ARM64-FULL-LINE: fcmp {{d[0-9]+}}, {{d[0-9]+}}
        //ARM64-FULL-LINE-NEXT: csneg {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{gt|le}}
        return op1 > 49.0 ? -op2 : op2;
    }

    public static int Main()
    {
        if (cneg_byte(72, 13) != (byte)13)
        {
            Console.WriteLine("ConditionalNegateTest:cneg_byte() failed");
            return 101;
        }
        if (cneg_byte(32, 13) != (byte)224)
        {
            Console.WriteLine("ConditionalNegateTest:cneg_byte() failed");
            return 101;
        }

        if (cneg_short(34, 13) != -13)
        {
            Console.WriteLine("ConditionalNegateTest:cneg_short() failed");
            return 101;
        }

        if (cneg_short(74, 13) != 74)
        {
            Console.WriteLine("ConditionalNegateTest:cneg_short() failed");
            return 101;
        }

        if (cneg_short_min_max(75) != -short.MaxValue)
        {
            Console.WriteLine("ConditionalNegateTest:cneg_short_min_max() failed");
            return 101;
        }

        if (cneg_short_min_max(-35) != short.MaxValue)
        {
            Console.WriteLine("ConditionalNegateTest:cneg_short_min_max() failed");
            return 101;
        }

        if (cneg_int(76, 17) != 17)
        {
            Console.WriteLine("ConditionalNegateTest:cneg_int() failed");
            return 101;
        }

        if (cneg_int(36, 17) != -36)
        {
            Console.WriteLine("ConditionalNegateTest:cneg_int() failed");
            return 101;
        }

        if (cneg_int_min_max(77) != int.MaxValue)
        {
            Console.WriteLine("ConditionalNegateTest:cneg_int_min_max() failed");
            return 101;
        }

        if (cneg_int_min_max(37) != -int.MaxValue)
        {
            Console.WriteLine("ConditionalNegateTest:cneg_int_min_max() failed");
            return 101;
        }

        if (cneg_long(78, 23) != 78)
        {
            Console.WriteLine("ConditionalNegateTest:cneg_long() failed");
            return 101;
        }

        if (cneg_long(38, 23) != -23)
        {
            Console.WriteLine("ConditionalNegateTest:cneg_long() failed");
            return 101;
        }

        if (cneg_float(80.0f, 29) != 29)
        {
            Console.WriteLine("ConditionalNegateTest:cneg_float() failed");
            return 101;
        }

        if (cneg_float(30.0f, 29) != -29)
        {
            Console.WriteLine("ConditionalNegateTest:cneg_float() failed");
            return 101;
        }

        if (cneg_double(60.0, 31) != -31)
        {
            Console.WriteLine("ConditionalNegateTest:cneg_double() failed");
            return 101;
        }

        if (cneg_double(30.0, 31) != 31)
        {
            Console.WriteLine("ConditionalNegateTest:cneg_double() failed");
            return 101;
        }

        Console.WriteLine("PASSED");
        return 100;
    }
}
