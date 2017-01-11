// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
/* The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/

   Based on code originally contributed by Marek Safar
   and optimized by kasthack

   modified for use with xunit-performance
*/

using Microsoft.Xunit.Performance;
using System;
using System.Runtime.CompilerServices;
using Xunit;

[assembly: OptimizeForBenchmarks]
[assembly: MeasureInstructionsRetired]
[assembly: MeasureGCCounts]

namespace BenchmarksGame
{
public class BinaryTrees3
{
    private const int minDepth = 4;
    private const int Iterations = 1;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Bench(bool verbose = false)
    {
        int n = 16;
        int maxDepth = Math.Max(minDepth + 2, n);
        int stretchDepth = maxDepth + 1;
        int t = 0;

        int check = (TreeNode.bottomUpTree(0, stretchDepth)).itemCheck();
        if (verbose)
        {
            Console.WriteLine("stretch tree of depth {0}\t check: {1}", stretchDepth, check);
        }
        t += check;

        TreeNode longLivedTree = TreeNode.bottomUpTree(0, maxDepth);

        for (int depth = minDepth; depth <= maxDepth; depth += 2)
        {
            int iterations = 1 << (maxDepth - depth + minDepth);

            check = 0;
            for (int i = 1; i <= iterations; i++)
            {
                check += (TreeNode.bottomUpTree(i, depth)).itemCheck();
                check += (TreeNode.bottomUpTree(-i, depth)).itemCheck();
            }

            if (verbose)
            {
                Console.WriteLine("{0}\t trees of depth {1}\t check: {2}",
                    iterations * 2, depth, check);
            }

            t += check;
        }

        if (verbose)
        {
            Console.WriteLine("long lived tree of depth {0}\t check: {1}",
                maxDepth, longLivedTree.itemCheck());
        }

        t += check;

        return (t == -174785);
    }

    private class TreeNode
    {
        private TreeNode left, right;
        private int item;

        private TreeNode(int item)
        {
            this.item = item;
        }

        internal static TreeNode bottomUpTree(int item, int depth)
        {
            TreeNode t;
            ChildTreeNodes(out t, item, depth - 1);
            return t;
        }

        static void ChildTreeNodes(out TreeNode node, int item, int depth)
        {
            node = new TreeNode(item);
            if ( depth > 0 )
            {
                ChildTreeNodes(out node.left, 2 * item - 1, depth - 1);
                ChildTreeNodes(out node.right, 2 * item, depth - 1);
            }
        }

        internal int itemCheck()
        {
            if (right == null) return item;
            else return item + left.itemCheck() - right.itemCheck();
        }
    }

    [Benchmark]
    public static void Test()
    {
        foreach (var iteration in Benchmark.Iterations)
        {
            using (iteration.StartMeasurement())
            {
                for (int i = 0; i < Iterations; i++)
                {
                    Bench();
                }
            }
        }
    }

    private static bool TestBase()
    {
        bool result = true;
        for (int i = 0; i < Iterations; i++)
        {
            result &= Bench(true);
        }
        return result;
    }

    public static int Main()
    {
        bool result = TestBase();
        return (result ? 100 : -1);
    }
}
}
