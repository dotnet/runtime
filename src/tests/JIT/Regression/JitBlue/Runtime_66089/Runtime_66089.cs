// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Test arm64 funclet frame type 5: frames with large outgoing argument space
// where FP/LR are stored at the top of the frame due to the need for a GS
// cookie.

using System;
using System.Runtime.CompilerServices;

public class Runtime_66089
{
    public static unsafe int Main()
    {
        int* foo = stackalloc int[30];
        try
        {
            Console.WriteLine("try");
            throw new Exception();
        }
        catch (Exception)
        {
            Console.WriteLine("catch");
            foo[0] = 10;
            ManyArgs(new Guid(foo[0], 0, 0, 0, 0, 0, 0, 0, 0, 0, 0));
        }
        Console.WriteLine("after");

        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ManyArgs(
        Guid g0 = default,
        Guid g1 = default,
        Guid g2 = default,
        Guid g3 = default,
        Guid g4 = default,
        Guid g5 = default,
        Guid g6 = default,
        Guid g7 = default,
        Guid g8 = default,
        Guid g9 = default,
        Guid g10 = default,
        Guid g11 = default,
        Guid g12 = default,
        Guid g13 = default,
        Guid g14 = default,
        Guid g15 = default,
        Guid g16 = default,
        Guid g17 = default,
        Guid g18 = default,
        Guid g19 = default,
        Guid g20 = default,
        Guid g21 = default,
        Guid g22 = default,
        Guid g23 = default,
        Guid g24 = default,
        Guid g25 = default,
        Guid g26 = default,
        Guid g27 = default,
        Guid g28 = default,
        Guid g29 = default,
        Guid g30 = default,
        Guid g31 = default,
        Guid g32 = default,
        Guid g33 = default,
        Guid g34 = default,
        Guid g35 = default,
        Guid g36 = default,
        Guid g37 = default,
        Guid g38 = default,
        Guid g39 = default,
        Guid g40 = default,
        Guid g41 = default)
    {
    }
}
