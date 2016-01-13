// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.Xunit.Performance;
using System;
using System.Runtime.CompilerServices;
using Xunit;

[assembly: OptimizeForBenchmarks]
[assembly: MeasureInstructionsRetired]

public class Permutate
{
#if DEBUG
    public const int Iterations = 1;
#else
    public const int Iterations = 20000;
#endif

    private int[] _permArray = new int[11];
    private static int s_pctr;

    private static
    void Swap(int[] arr, int i, int j)
    {
        int t = arr[i];
        arr[i] = arr[j];
        arr[j] = t;
    }

    private void Initialize()
    {
        for (int i = 1; i <= 7; i++)
        {
            _permArray[i] = i - 1;
        }
    }

    private void PermuteArray(int n)
    {
        int k;
        s_pctr = s_pctr + 1;
        if (n != 1)
        {
            PermuteArray(n - 1);
            for (k = n - 1; k >= 1; k--)
            {
                Swap(_permArray, n, k);
                PermuteArray(n - 1);
                Swap(_permArray, n, k);
            }
        }
    }

    private bool Validate()
    {
        int k = 0;

        for (int i = 0; i <= 6; i++)
        {
            for (int j = 1; j <= 7; j++)
            {
                if (_permArray[j] == i)
                {
                    k = k + 1;
                }
            }
        }

        return (k == 7);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool Bench()
    {
        Initialize();

        for (int i = 0; i < Iterations; ++i)
        {
            s_pctr = 0;
            PermuteArray(7);
        }

        bool result = Validate();

        return result;
    }

    [Benchmark]
    public static void Test()
    {
        Permutate P = new Permutate();
        foreach (var iteration in Benchmark.Iterations)
        {
            using (iteration.StartMeasurement())
            {
                P.Bench();
            }
        }
    }

    private static bool TestBase()
    {
        Permutate P = new Permutate();
        bool result = P.Bench();
        return result;
    }

    public static int Main()
    {
        bool result = TestBase();
        return (result ? 100 : -1);
    }
}
