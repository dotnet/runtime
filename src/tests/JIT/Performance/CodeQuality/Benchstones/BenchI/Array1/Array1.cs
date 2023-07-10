// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// The sorting benchmark calls a random number generator the number
// of times specified by Maxnum to create an array of int integers,
// then does a quicksort on the array of ints. Random numbers
// are produced using a multiplicative modulus method with known
// seed, so that the generated array is constant across compilers.
//
// This is adapted from a benchmark in BYTE Magazine, August 1984.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Benchstone.BenchI
{
public static class Array1
{
#if DEBUG
    private const int Iterations = 1;
    private const int Maxnum = 100;
#else
    private const int Iterations = 125;
    private const int Maxnum = 1000;
#endif

    private const int Modulus = ((int)0x20000);
    private const int C = 13849;
    private const int A = 25173;
    static int s_seed = 7;

    private static void Quick(int lo, int hi, int[] input)
    {
        int i, j;
        int pivot, temp;

        if (lo < hi)
        {
            // 0 <= lo < hi
            for (i = lo, j = (hi + 1), pivot = input[lo]; ;)
            {
                do
                {
                    ++i;
                } while (input[i] < pivot);

                do
                {
                    --j;
                    // Accessing upto hi
                } while (input[j] > pivot);

                if (i < j)
                {
                    temp = input[i];
                    input[i] = input[j];
                    input[j] = temp;
                }
                else
                {
                    break;
                }
            }
            temp = input[j];
            input[j] = input[lo];
            input[lo] = temp;
            Quick(lo, j - 1, input);
            Quick(j + 1, hi, input);
        }
    }

    private static int Random(int size)
    {
        unchecked
        {
            s_seed = s_seed * A + C;
        }

        return (s_seed % size);
    }

    private static bool VerifySort(int[] buffer)
    {
        for (int y = 0; y < Maxnum - 2; y++)
        {
            if (buffer[y] > buffer[y + 1])
            {
                return false;
            }
        }
        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Bench()
    {
        int[] buffer = new int[Maxnum + 1];

        for (int i = 0; i < Iterations; ++i)
        {
            for (int j = 0; j < Maxnum; ++j)
            {
                int temp = Random(Modulus);
                if (temp < 0L)
                {
                    temp = (-temp);
                }
                buffer[j] = temp;
            }
            buffer[Maxnum] = Modulus;

            Quick(0, Maxnum - 1, buffer);
        }

        bool result = VerifySort(buffer);

        return result;
    }

    private static bool TestBase()
    {
        bool result = true;
        for (int i = 0; i < Iterations; i++)
        {
            result &= Bench();
        }
        return result;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        bool result = TestBase();
        return (result ? 100 : -1);
    }
}
}
