using System;
using System.Runtime.CompilerServices;
using System.Threading;

class Program
{
    static volatile int x;

    [MethodImpl(MethodImplOptions.NoInlining)]
    int IncrementField()
    {
        int t = 0;
        Volatile.Write(ref x, Volatile.Read(ref x) + 1);
        t += x;
        Volatile.Write(ref x, Volatile.Read(ref x) + 1);
        t += x;
        Volatile.Write(ref x, Volatile.Read(ref x) + 1);
        t += x;
        Volatile.Write(ref x, Volatile.Read(ref x) + 1);
        t += x;
        Volatile.Write(ref x, Volatile.Read(ref x) + 1);
        return t;
    }

    static int Main()
    {
        Program p = new Program();
        for (int i = 0; i < 50000000; i++)
        {
            x = 0;
            if (p.IncrementField() != 10)
                throw new Exception();
        }
        return 100;
    }
}
