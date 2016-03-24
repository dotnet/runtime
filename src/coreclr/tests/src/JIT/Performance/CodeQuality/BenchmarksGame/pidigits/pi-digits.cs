// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
/* The Computer Language Benchmarks Game
 * http://benchmarksgame.alioth.debian.org/
 *
 * Port of the C code that uses GMP
 * Just switched it to use C#'s BigInteger instead
 * 
 * To compile use csc /o+ /r:System.Numerics.dll
 *
 * modified for use with xunit-performance
*/

using Microsoft.Xunit.Performance;
using System;
using System.Numerics;
using System.Text;

[assembly: OptimizeForBenchmarks]
[assembly: MeasureInstructionsRetired]

public class pidigits
{
#if DEBUG
    public const int Iterations = 1;
#else
    public const int Iterations = 50;
#endif

    private BigInteger _acc,_den,_num;

    public pidigits()
    {
        _acc = BigInteger.Zero;
        _den = BigInteger.One;
        _num = BigInteger.One;
    }

    public uint extract_digit(uint nth)
    {
        return (uint)((_num * nth + _acc) / _den);
    }

    public void eliminate_digit(uint d)
    {
        _acc -= _den * d;
        _acc *= 10;
        _num *= 10;
    }

    public void next_term(uint k)
    {
        uint k2 = k * 2 + 1;
        _acc += _num * 2;
        _acc *= k2;
        _den *= k2;
        _num *= k;
    }

    public void Calculate(int n, bool verbose = false)
    {
        StringBuilder sb = new StringBuilder(20);
        uint d, k, i;
        for (i = k = 0; i < n;)
        {
            next_term(++k);
            if (_num > _acc)
                continue;
            d = extract_digit(3);
            if (d != extract_digit(4))
                continue;
            sb.Append((char)('0' + d));
            if (++i % 10 == 0)
            {
                if (verbose)
                {
                    Console.WriteLine("{0}\t:{1}", sb, i);
                }
                sb.Clear();
            }
            eliminate_digit(d);
        }
    }

    public static int Main(String[] args)
    {
        int length = args.Length == 0 ? 10 : Int32.Parse(args[0]);
        for (int i = 0; i < Iterations; i++)
        {
            pidigits p = new pidigits();
            p.Calculate(length, true);
        }
        return 100;
    }

    [Benchmark]
    public static void Bench()
    {
        int length = 600;
        foreach (var iteration in Benchmark.Iterations)
        {
            using (iteration.StartMeasurement())
            {
                for (int i = 0; i < Iterations; i++)
                {
                    pidigits p = new pidigits();
                    p.Calculate(length);
                }
            }
        }
    }
}

