// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// unit test for the full range comparison optimization

using System;
using System.Runtime.CompilerServices;

public class ConditionalInvertTest
{

    [MethodImpl(MethodImplOptions.NoInlining)]
    static byte cinv_byte(byte op1, byte op2)
    {
        //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #42
        //ARM64-FULL-LINE-NEXT: cinv {{w[0-9]+}}, {{w[0-9]+}}, {{gt|le}}
        return (byte) (op1 > 42 ? op2: ~op2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static short cinv_short(short op1, short op2)
    {
        //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #44
        //ARM64-FULL-LINE-NEXT: cinv {{w[0-9]+}}, {{w[0-9]+}}, {{gt|le}}
        return (short) (op1 <= 44 ? ~op2 : op2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static short cinv_short_min_max(short op1)
    {
        //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #45
        //ARM64-FULL-LINE-NEXT: csel {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{gt|le}}
        return (short) (op1 > 45 ? ~short.MaxValue : short.MaxValue);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int cinv_int(int op1, int op2)
    {
        //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #46
        //ARM64-FULL-LINE-NEXT: cinv {{w[0-9]+}}, {{w[0-9]+}}, {{gt|le}}
        return op1 > 46 ? op2 : ~op2;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int cinv_int_min_max(int op1)
    {
        //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #47
        //ARM64-FULL-LINE-NEXT: csel {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{ge|lt}}
        return op1 >= 47 ? int.MaxValue : ~int.MaxValue;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long cinv_long(long op1, long op2)
    {
        //ARM64-FULL-LINE: cmp {{x[0-9]+}}, #48
        //ARM64-FULL-LINE-NEXT: cinv {{x[0-9]+}}, {{x[0-9]+}}, {{ge|lt}}
        return op1 < 48 ? ~op2 : op2;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int cinv_float(float op1, int op2)
    {
        //ARM64-FULL-LINE: fcmp {{s[0-9]+}}, {{s[0-9]+}}
        //ARM64-FULL-LINE-NEXT: cinv {{w[0-9]+}}, {{w[0-9]+}}, {{gt|le}}
        return op1 > 50.0f ? op2 : ~op2;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int cinv_double(double op1, int op2)
    {
        //ARM64-FULL-LINE: fcmp {{d[0-9]+}}, {{d[0-9]+}}
        //ARM64-FULL-LINE-NEXT: cinv {{w[0-9]+}}, {{w[0-9]+}}, {{gt|le}}
        return op1 > 51.0 ? ~op2 : op2;
    }

    public static int Main()
    {
        if (cinv_byte(72, 13) != (byte)13)
        {
            Console.WriteLine("ConditionalIncrementTest:cinv_byte() failed");
            return 101;
        }
        if (cinv_byte(32, 13) != (byte)242)
        {
            Console.WriteLine("ConditionalIncrementTest:cinv_byte() failed");
            return 101;
        }

        if (cinv_short(34, 13) != ~13)
        {
            Console.WriteLine("ConditionalIncrementTest:cinv_short() failed");
            return 101;
        }

        if (cinv_short(74, 13) != 13)
        {
            Console.WriteLine("ConditionalIncrementTest:cinv_short() failed");
            return 101;
        }

        if (cinv_short_min_max(75) != ~short.MaxValue)
        {
            Console.WriteLine("ConditionalIncrementTest:cinv_short_min_max() failed");
            return 101;
        }

        if (cinv_short_min_max(-35) != short.MaxValue)
        {
            Console.WriteLine("ConditionalIncrementTest:cinv_short_min_max() failed");
            return 101;
        }

        if (cinv_int(76, 17) != 17)
        {
            Console.WriteLine("ConditionalIncrementTest:cinv_int() failed");
            return 101;
        }

        if (cinv_int(36, 17) != ~17)
        {
            Console.WriteLine("ConditionalIncrementTest:cinv_int() failed");
            return 101;
        }

        if (cinv_int_min_max(77) != int.MaxValue)
        {
            Console.WriteLine("ConditionalIncrementTest:cinv_int_min_max() failed");
            return 101;
        }

        if (cinv_int_min_max(37) != ~int.MaxValue)
        {
            Console.WriteLine("ConditionalIncrementTest:cinv_int_min_max() failed");
            return 101;
        }

        if (cinv_long(78, 23) != 23)
        {
            Console.WriteLine("ConditionalIncrementTest:cinv_long() failed");
            return 101;
        }

        if (cinv_long(38, 23) != ~23)
        {
            Console.WriteLine("ConditionalIncrementTest:cinv_long() failed");
            return 101;
        }

        if (cinv_float(80.0f, 29) != 29)
        {
            Console.WriteLine("ConditionalIncrementTest:cinv_float() failed");
            return 101;
        }

        if (cinv_float(30.0f, 29) != ~29)
        {
            Console.WriteLine("ConditionalIncrementTest:cinv_float() failed");
            return 101;
        }

        if (cinv_double(60.0, 31) != ~31)
        {
            Console.WriteLine("ConditionalIncrementTest:cinv_double() failed");
            return 101;
        }

        if (cinv_double(30.0, 31) != 31)
        {
            Console.WriteLine("ConditionalIncrementTest:cinv_double() failed");
            return 101;
        }

        Console.WriteLine("PASSED");
        return 100;
    }
}
