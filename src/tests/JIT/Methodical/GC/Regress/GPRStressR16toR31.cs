using System;
using System.Runtime.CompilerServices;

class GPRStressR16toR31
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void StressRegisters()
    {
        // 32 reference variables to force JIT to use all GPRs
        object o0 = new object(), o1 = new object(), o2 = new object(), o3 = new object();
        object o4 = new object(), o5 = new object(), o6 = new object(), o7 = new object();
        object o8 = new object(), o9 = new object(), o10 = new object(), o11 = new object();
        object o12 = new object(), o13 = new object(), o14 = new object(), o15 = new object();
        object o16 = new object(), o17 = new object(), o18 = new object(), o19 = new object();
        object o20 = new object(), o21 = new object(), o22 = new object(), o23 = new object();
        object o24 = new object(), o25 = new object(), o26 = new object(), o27 = new object();
        object o28 = new object(), o29 = new object(), o30 = new object(), o31 = new object();

        // Use all variables in a way that prevents optimization
        for (int i = 0; i < 10000; i++)
        {
            GC.Collect();
            GC.KeepAlive(o0); GC.KeepAlive(o1); GC.KeepAlive(o2); GC.KeepAlive(o3);
            GC.KeepAlive(o4); GC.KeepAlive(o5); GC.KeepAlive(o6); GC.KeepAlive(o7);
            GC.KeepAlive(o8); GC.KeepAlive(o9); GC.KeepAlive(o10); GC.KeepAlive(o11);
            GC.KeepAlive(o12); GC.KeepAlive(o13); GC.KeepAlive(o14); GC.KeepAlive(o15);
            GC.KeepAlive(o16); GC.KeepAlive(o17); GC.KeepAlive(o18); GC.KeepAlive(o19);
            GC.KeepAlive(o20); GC.KeepAlive(o21); GC.KeepAlive(o22); GC.KeepAlive(o23);
            GC.KeepAlive(o24); GC.KeepAlive(o25); GC.KeepAlive(o26); GC.KeepAlive(o27);
            GC.KeepAlive(o28); GC.KeepAlive(o29); GC.KeepAlive(o30); GC.KeepAlive(o31);
        }
    }

    static int Main()
    {
        StressRegisters();
        Console.WriteLine("Test Passed");
        return 100;
    }
} 