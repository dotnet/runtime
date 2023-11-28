// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Based on Eratosthenes Sieve Prime Number Program in C, Byte Magazine, January 1983.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Benchstone.BenchI
{
public static class CSieve
{

#if DEBUG
    public const int Iterations = 1;
#else
    public const int Iterations = 200;
#endif

    const int Size = 8190;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Bench() {
        bool[] flags = new bool[Size + 1];
        int count = 0;
        for (int iter = 1; iter <= Iterations; iter++)
        {
            count = 0;

            // Initially, assume all are prime
            for (int i = 0; i <= Size; i++)
            {
                flags[i] = true;
            }

            // Refine
            for (int i = 2; i <= Size; i++)
            {
                if (flags[i])
                {
                    // Found a prime
                    for (int k = i + i; k <= Size; k += i)
                    {
                        // Cancel its multiples
                        flags[k] = false;
                    }
                    count++;
                }
            }
        }

        return (count == 1027);
    }

    static bool TestBase() {
        bool result = true;
        for (int i = 0; i < Iterations; i++) {
            result &= Bench();
        }
        return result;
    }

    [Fact]
    public static int TestEntryPoint() {
        bool result = TestBase();
        return (result ? 100 : -1);
    }
}
}
