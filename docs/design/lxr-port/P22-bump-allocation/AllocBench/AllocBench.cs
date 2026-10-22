using System.Diagnostics;
using System.Runtime.CompilerServices;

// P22 (D2): allocation micro benchmark. Each kind allocates in a tight loop. A ring of 64 slots keeps a few objects alive.
// The output is one line per kind: ns per object (best of the repetitions), the number of objects and a checksum.
internal static class AllocBench
{
    private sealed class O24 { public long A; }
    private sealed class O48 { public long A, B, C, D; }
    private sealed class O96 { public long A, B, C, D, E, F, G, H, I, J; }

    private static readonly object[] Ring = new object[64];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Run(int kind, long count)
    {
        long sum = 0;
        for (long i = 0; i < count; i++)
        {
            object o;
            switch (kind)
            {
                case 0: o = new O24 { A = i }; break;
                case 1: o = new O48 { A = i }; break;
                case 2: o = new O96 { A = i }; break;
                case 3: o = new string('x', 16 + (int)(i & 7)); break;
                case 4: o = new int[8 + (int)(i & 7)]; break;
                default: o = new object[4]; break;
            }
            Ring[i & 63] = o;
            sum += i & 1;
        }
        return sum;
    }

    private static void Main(string[] args)
    {
        long count = args.Length > 0 ? long.Parse(args[0]) : 20_000_000;
        int reps = args.Length > 1 ? int.Parse(args[1]) : 3;
        string[] names = { "obj24", "obj48", "obj96", "string", "int[8..15]", "object[4]" };
        long checksum = 0;
        for (int kind = 0; kind < names.Length; kind++)
        {
            Run(kind, 200_000);
            double best = double.MaxValue;
            for (int r = 0; r < reps; r++)
            {
                GC.Collect();
                var sw = Stopwatch.StartNew();
                checksum += Run(kind, count);
                best = Math.Min(best, sw.Elapsed.TotalMilliseconds * 1_000_000.0 / count);
            }
            Console.WriteLine($"{names[kind],-12} {best,8:F2} ns/object  objects={count} reps={reps}");
        }
        Console.WriteLine($"checksum={checksum} gcs={GC.CollectionCount(0)}");
    }
}
