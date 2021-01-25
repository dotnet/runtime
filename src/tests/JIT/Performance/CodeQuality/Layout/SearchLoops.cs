// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Xunit.Performance;
using Microsoft.Xunit.Performance.Api;
using System;
using System.Reflection;
using Xunit;

[assembly: OptimizeForBenchmarks]

// Test code taken directly from https://github.com/dotnet/runtime/issues/7474
// Laying the loop's early return path in-line can cost 30% on this micro-benchmark.

namespace Layout
{
    public unsafe class SearchLoops
    {
        public static int Main(string[] args)
        {
            // Make sure equal strings compare as such
            if (!LoopReturn("hello", "hello") || !LoopGoto("goodbye", "goodbye"))
            {
                return -1;
            }

            // Make sure not-equal strings compare as such
            if (LoopReturn("hello", "goodbye") || LoopGoto("goodbye", "hello"))
            {
                return -1;
            }

            // Success
            return 100;
        }

        public int length = 100;

        private string test1;
        private string test2;

        public SearchLoops()
        {
            test1 = new string('A', length);
            test2 = new string('A', length);
        }

        [Benchmark(InnerIterationCount = 20000000)]
        public void LoopReturnIter()
        {
            Benchmark.Iterate(() => LoopReturn(test1, test2));
        }

        [Benchmark(InnerIterationCount = 20000000)]
        public void LoopGotoIter()
        {
            Benchmark.Iterate(() => LoopGoto(test1, test2));
        }

        // Variant with code written naturally -- need JIT to lay this out
        // with return path out of loop for best performance.
        public static bool LoopReturn(String strA, String strB)
        {
            int length = strA.Length;

            fixed (char* ap = strA) fixed (char* bp = strB)
            {
                char* a = ap;
                char* b = bp;

                while (length != 0)
                {
                    int charA = *a;
                    int charB = *b;

                    if (charA != charB)
                        return false;  // placement of prolog for this return is the issue

                    a++;
                    b++;
                    length--;
                }

                return true;
            }
        }

        // Variant with code written awkwardly but which acheives the desired
        // performance if JIT simply lays out code in source order.
        public static bool LoopGoto(String strA, String strB)
        {
            int length = strA.Length;

            fixed (char* ap = strA) fixed (char* bp = strB)
            {
                char* a = ap;
                char* b = bp;

                while (length != 0)
                {
                    int charA = *a;
                    int charB = *b;

                    if (charA != charB)
                        goto ReturnFalse;  // placement of prolog for this return is the issue

                    a++;
                    b++;
                    length--;
                }

                return true;

                ReturnFalse:
                return false;
            }
        }
    }
}
