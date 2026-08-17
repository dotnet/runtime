// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// 
// We should see Hfa24 passed by reference into VarArgMethod, because VarArgs functions
// do not process HFAs for this calling convention. We check the integrity of all the
// arguments to confirm this.
//
// The GC refs and GCStress mode for this test are designed to exercise
// ArgIteratorTemplate::GetNextOffset in src/coreclr/vm/callingconvention.h.
using System.Runtime.CompilerServices;

public class Program
{
    private struct Hfa24
    {
        public double A;
        public double B;
        public double C;
    }

    private sealed class Payload
    {
        public readonly int Value;

        public Payload(int value)
        {
            Value = value;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int VarArgMethod(
        Hfa24 hfa, Payload first, Payload second, long nonGc1, long nonGc2, __arglist)
    {
        if (hfa.A != 1.0 || hfa.B != 2.0 || hfa.C != 3.0)
        {
            return -1;
        }

        if (first.Value != 10 || second.Value != 20)
        {
            return -1;
        }

        if (nonGc1 != 1 || nonGc2 != 2)
        {
            return -1;
        }

        return 100;
    }

    public static int Main()
    {
        Hfa24 hfa = new Hfa24
        {
            A = 1.0,
            B = 2.0,
            C = 3.0,
        };

        return VarArgMethod(hfa, new Payload(10), new Payload(20), 1, 2, __arglist());
    }
}
