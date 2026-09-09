// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

// Call arguments were sorted purely by cost, which could evaluate a later argument's
// volatile read before an earlier one's. With a monotonically increasing value the
// first read can never observe a larger value than the second, so any such observation
// means the two acquire loads were reordered.
public class Runtime_133524
{
    private static int s_value;
    private static int s_stop;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static bool ReadsReversed(ref int value) =>
        Sink(Volatile.Read(ref value), Volatile.Read(ref value) + 1);

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static bool Sink(int first, int secondPlusOne) => first > secondPlusOne - 1;

    [Fact]
    public static void TestEntryPoint()
    {
        Thread publisher = new Thread(static () =>
        {
            for (int i = 1; (i <= 100_000_000) && (Volatile.Read(ref s_stop) == 0); i++)
            {
                Volatile.Write(ref s_value, i);
            }
        });

        publisher.IsBackground = true;
        publisher.Start();

        int reversed = 0;
        for (int i = 0; i < 2_000_000; i++)
        {
            if (ReadsReversed(ref s_value))
            {
                reversed++;
            }
        }

        Volatile.Write(ref s_stop, 1);
        publisher.Join();

        Assert.Equal(0, reversed);
    }
}
