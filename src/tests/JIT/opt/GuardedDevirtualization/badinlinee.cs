// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

public class BadInlinee
{
    private class Base
    {
        public virtual int M() => 42;
    }

    private sealed class Derived : Base
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        public override int M() => 43;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Poison(Base value) => value.M();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Test()
    {
        Base value = new Base();
        return value.M();
    }

    [Fact]
    public static int TestEntryPoint()
    {
        Base value = new Derived();
        int result = 0;

        for (int i = 0; i < 100; i++)
        {
            for (int j = 0; j < 100; j++)
            {
                result += Poison(value);
            }

            Thread.Sleep(15);
        }

        for (int i = 0; i < 100; i++)
        {
            for (int j = 0; j < 100; j++)
            {
                result += Test();
            }

            Thread.Sleep(15);
        }

        long allocatedBytesBefore = GC.GetAllocatedBytesForCurrentThread();

        for (int i = 0; i < 1_000; i++)
        {
            result += Test();
        }

        long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBytesBefore;

        if (allocatedBytes != 0)
        {
            Console.WriteLine($"Test allocated {allocatedBytes} bytes");
            return -1;
        }

        return result == 892_000 ? 100 : -1;
    }
}
