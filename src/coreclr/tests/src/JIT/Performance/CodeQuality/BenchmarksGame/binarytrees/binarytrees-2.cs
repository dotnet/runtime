// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Adapted from binary-trees C# .NET Core #2 program
// http://benchmarksgame.alioth.debian.org/u64q/program.php?test=binarytrees&lang=csharpcore&id=2
// aka (as of 2017-09-01) rev 1.3 of https://alioth.debian.org/scm/viewvc.php/benchmarksgame/bench/binarytrees/binarytrees.csharp-2.csharp?root=benchmarksgame&view=log
// Best-scoring single-threaded C# .NET Core version as of 2017-09-01

/* The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/ 

   contributed by Marek Safar 
   *reset* 
*/

using System;
using Microsoft.Xunit.Performance;

[assembly: OptimizeForBenchmarks]
[assembly: MeasureGCCounts]

namespace BenchmarksGame
{
    public class BinaryTrees_2
    {
        const int minDepth = 4;

        public static int Main(String[] args)
        {
            int n = 0;
            if (args.Length > 0) n = Int32.Parse(args[0]);

            int check = Bench(n, true);
            int expected = 4398;

            // Return 100 on success, anything else on failure.
            return check - expected + 100;
        }

        [Benchmark(InnerIterationCount = 7)]
        public static void RunBench()
        {
            Benchmark.Iterate(() => Bench(16, false));
        }

        static int Bench(int n, bool verbose)
        {
            int maxDepth = Math.Max(minDepth + 2, n);
            int stretchDepth = maxDepth + 1;

            int check = (TreeNode.bottomUpTree(stretchDepth)).itemCheck();
            int checkSum = check;
            if (verbose) Console.WriteLine("stretch tree of depth {0}\t check: {1}", stretchDepth, check);

            TreeNode longLivedTree = TreeNode.bottomUpTree(maxDepth);

            for (int depth = minDepth; depth <= maxDepth; depth += 2)
            {
                int iterations = 1 << (maxDepth - depth + minDepth);

                check = 0;
                for (int i = 1; i <= iterations; i++)
                {
                    check += (TreeNode.bottomUpTree(depth)).itemCheck();
                }
                checkSum += check;

                if (verbose)
                    Console.WriteLine("{0}\t trees of depth {1}\t check: {2}", iterations, depth, check);
            }

            check = longLivedTree.itemCheck();
            checkSum += check;

            if (verbose)
                Console.WriteLine("long lived tree of depth {0}\t check: {1}", maxDepth, check);

            return checkSum;
        }


        struct TreeNode
        {
            class Next
            {
                public TreeNode left, right;
            }

            private Next next;

            internal static TreeNode bottomUpTree(int depth)
            {
                if (depth > 0)
                {
                    return new TreeNode(
                         bottomUpTree(depth - 1)
                       , bottomUpTree(depth - 1)
                       );
                }
                else
                {
                    return new TreeNode();
                }
            }

            TreeNode(TreeNode left, TreeNode right)
            {
                this.next = new Next();
                this.next.left = left;
                this.next.right = right;
            }

            internal int itemCheck()
            {
                // if necessary deallocate here
                if (next == null) return 1;
                else return 1 + next.left.itemCheck() + next.right.itemCheck();
            }
        }
    }
}
