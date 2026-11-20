// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

public class Runtime_134489
{
    [Fact]
    public static void TestEntryPoint()
    {
        Holder holder = new Holder();
        int[] array = new int[10];
        int stop = 0;
        Thread writer = new Thread(() =>
        {
            while (Volatile.Read(ref stop) == 0)
            {
                Volatile.Write(ref holder.F.A, 0);
                Volatile.Write(ref holder.F.A, 100_000_000);
            }
        })
        {
            IsBackground = true
        };
        writer.Start();

        try
        {
            for (int i = 0; i < 1_000_000; i++)
            {
                try
                {
                    Test(holder, array, true);
                }
                catch (TestException)
                {
                }
                catch (IndexOutOfRangeException)
                {
                }
            }
        }
        finally
        {
            Volatile.Write(ref stop, 1);
            writer.Join();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string Use(S value) => "";

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Test(Holder holder, int[] array, bool condition)
    {
        S s = holder.F;
        if (condition)
        {
            s = holder.F;
        }

        if ((uint)s.A < (uint)array.Length)
        {
            S t = holder.F;
            if (condition)
            {
                t = holder.F;
            }

            throw new TestException(Use(s) + Use(t) + array[t.A] + s.B);
        }

        return -1;
    }

    private struct S
    {
        public int A, B, C, D, E;
    }

    private sealed class Holder
    {
        public S F;
    }

    private sealed class TestException : Exception
    {
        public TestException(string message)
            : base(message)
        {
        }
    }
}
