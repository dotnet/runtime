using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

struct MyStruct
{
    int x;

    [MethodImpl(MethodImplOptions.NoInlining)]
    int IncrementAndReturn()
    {
        int t = x;
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test()
    {
        for (int i = 0; i < 10_000_000; i++)
        {
            if (new MyStruct().IncrementAndReturn() != 10)
                throw new InvalidOperationException("oops");
        }
    }
}

class Program
{
    static int Main()
    {
        Console.WriteLine("Running...");
        Parallel.For(0, 100, _ => MyStruct.Test());
        Console.WriteLine("Done");
        return 100;
    }
}
