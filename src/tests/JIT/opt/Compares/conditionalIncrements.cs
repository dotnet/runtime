// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// unit test for the full range comparison optimization

using System;
using System.Runtime.CompilerServices;

public class ConditionalIncrementTest
{

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int cinc_byte(byte op1)
    {
        //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #42
        //ARM64-FULL-LINE-NEXT: cinc {{w[0-9]+}}, {{w[0-9]+}}, {{gt|le}}
        return op1 > 42 ? 6: 5;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static byte cinc_byte_min_max(byte op1)
    {
        //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #43
        //ARM64-FULL-LINE-NEXT: csel {{w[0-9]+}}, wzr, {{w[0-9]+}}, {{ge|lt}}
        return op1 >= 43 ? byte.MinValue : byte.MaxValue;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int cinc_short(short op1)
    {
        //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #44
        //ARM64-FULL-LINE-NEXT: cinc {{w[0-9]+}}, {{w[0-9]+}}, {{gt|le}}
        return op1 <= 44 ? 6 : 5;
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    static short cinc_short_min_max(short op1)
    {
        //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #45
        //ARM64-FULL-LINE-NEXT: csel {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{gt|le}}
        return op1 > 45 ? short.MinValue : short.MaxValue;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int cinc_int(int op1)
    {
        //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #46
        //ARM64-FULL-LINE-NEXT: cinc {{w[0-9]+}}, {{w[0-9]+}}, {{gt|le}}
        return op1 > 46 ? 6 : 5;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int cinc_int_min_max(int op1)
    {
        //ARM64-FULL-LINE: cmp {{w[0-9]+}}, #47
        //ARM64-FULL-LINE-NEXT: csel {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, {{ge|lt}}
        return op1 >= 47 ? int.MinValue : int.MaxValue;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long cinc_long(long op1)
    {
        //ARM64-FULL-LINE: cmp {{x[0-9]+}}, #48
        //ARM64-FULL-LINE-NEXT: cinc {{w[0-9]+}}, {{w[0-9]+}}, {{ge|lt}}
        //ARM64-FULL-LINE-NEXT: sxtw {{x[0-9]+}}, {{w[0-9]+}}
        return op1 < 48 ? 6 : 5;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long cinc_long_min_max(long op1)
    {
        //ARM64-FULL-LINE: cmp {{x[0-9]+}}, #49
        //ARM64-FULL-LINE-NEXT: cinc {{x[0-9]+}}, {{x[0-9]+}}, {{ge|lt}}
        return op1 < 49 ? long.MinValue : long.MaxValue;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int cinc_float(float op1)
    {
        //ARM64-FULL-LINE: fcmp {{s[0-9]+}}, {{s[0-9]+}}
        //ARM64-FULL-LINE-NEXT: cinc {{w[0-9]+}}, {{w[0-9]+}}, {{gt|le}}
        return op1 > 50.0f ? 6 : 5;
    }


    public static int Main()
    {
        if (cinc_byte(72) != 6)
        {
            Console.WriteLine("ConditionalIncrementTest:cinc_byte() failed");
            return 101;
        }

        if (cinc_byte(32) != 5)
        {
            Console.WriteLine("ConditionalIncrementTest:cinc_byte() failed");
            return 101;
        }

        if (cinc_byte_min_max(72) != byte.MinValue)
        {
            Console.WriteLine("ConditionalIncrementTest:cinc_byte_min_max() failed");
            return 101;
        }

        if (cinc_byte_min_max(32) != byte.MaxValue)
        {
            Console.WriteLine("ConditionalIncrementTest:cinc_byte_min_max() failed");
            return 101;
        }

        if (cinc_short(34) != 6)
        {
            Console.WriteLine("ConditionalIncrementTest:cinc_short() failed");
            return 101;
        }

        if (cinc_short(74) != 5)
        {
            Console.WriteLine("ConditionalIncrementTest:cinc_short() failed");
            return 101;
        }

        if (cinc_short_min_max(75) != short.MinValue)
        {
            Console.WriteLine("ConditionalIncrementTest:cinc_short_min_max() failed");
            return 101;
        }

        if (cinc_short_min_max(-35) != short.MaxValue)
        {
            Console.WriteLine("ConditionalIncrementTest:cinc_short_min_max() failed");
            return 101;
        }

        if (cinc_int(76) != 6)
        {
            Console.WriteLine("ConditionalIncrementTest:cinc_int() failed");
            return 101;
        }

        if (cinc_int(36) != 5)
        {
            Console.WriteLine("ConditionalIncrementTest:cinc_int() failed");
            return 101;
        }

        if (cinc_int_min_max(77) != int.MinValue)
        {
            Console.WriteLine("ConditionalIncrementTest:cinc_int_min_max() failed");
            return 101;
        }

        if (cinc_int_min_max(37) != int.MaxValue)
        {
            Console.WriteLine("ConditionalIncrementTest:cinc_int_min_max() failed");
            return 101;
        }

        if (cinc_long(78) != 5)
        {
            Console.WriteLine("ConditionalIncrementTest:cinc_long() failed");
            return 101;
        }

        if (cinc_long(38) != 6)
        {
            Console.WriteLine("ConditionalIncrementTest:cinc_long() failed");
            return 101;
        }

        if (cinc_long_min_max(79) != long.MaxValue)
        {
            Console.WriteLine("ConditionalIncrementTest:cinc_long_min_max() failed");
            return 101;
        }

        if (cinc_long_min_max(39) != long.MinValue)
        {
            Console.WriteLine("ConditionalIncrementTest:cinc_long_min_max() failed");
            return 101;
        }

        if (cinc_float(80.0f) != 6)
        {
            Console.WriteLine("ConditionalIncrementTest:cinc_float() failed");
            return 101;
        }

        if (cinc_float(30.0f) != 5)
        {
            Console.WriteLine("ConditionalIncrementTest:cinc_float() failed");
            return 101;
        }

        Console.WriteLine("PASSED");
        return 100;
    }
}
