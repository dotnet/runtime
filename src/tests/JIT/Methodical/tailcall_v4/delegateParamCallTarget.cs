// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*
 * When generating the code for a tail call, the JIT was not careful to preserve any stack-based parameters used in the computation of the call target address if it is a simple indirection (like in the case of delgate.Invoke).
 * Thus, in the case where an outgoing argument overwrites the same slot as the incoming delegate, the new value is used in the indirection to compute the target address.
 * The fix is to not hoist any parameter uses when we hoist the indirection onto the call, and instead use tmpvars. This leaves the use of the parameter above the assignment to the slot for the outgoing tail call. In this one example it actually made the code better.
 *
 * Expected output:
 * Accomplice
 * Passed
 *
 * Actual output:
 * Accomplice
 * Failed 
 */

using System;

public delegate int DoIt(int a, int b, int c, DoIt d);

internal class Repro
{
    private int DoItWrong(int a, int b, int c, DoIt d)
    {
        Console.WriteLine("Failed");
        return -1;
    }

    private int DoItRight(int a, int b, int c, DoIt d)
    {
        Console.WriteLine("Pass");
        return 100;
    }

    private int Accomplice(int a, int b, int c, DoIt d)
    {
        Console.WriteLine("Accomplice");
        DoIt d2 = this.DoItWrong;
        return d(a, b, c, d2);
    }

    public static int Main()
    {
        Repro r = new Repro();
        DoIt d = r.DoItRight;
        return r.Accomplice(1, 2, 3, d);
    }
}
