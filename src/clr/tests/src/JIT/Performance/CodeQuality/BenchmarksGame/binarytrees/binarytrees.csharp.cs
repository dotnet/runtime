// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
/* The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/ 

   contributed by Marek Safar  

   modified for use with xunit-performance
*/

using Microsoft.Xunit.Performance;
using System;
using System.Runtime.CompilerServices;
using Xunit;

[assembly: OptimizeForBenchmarks]
[assembly: MeasureInstructionsRetired]

public class BinaryTrees
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

    private struct TreeNode
    {
        private class Next
        {
            public TreeNode left, right;
        }

        private Next _next;
        private int _item;

        private TreeNode(int item)
        {
            _item = item;
            _next = null;
        }

        internal static TreeNode bottomUpTree(int item, int depth)
        {
            if (depth > 0)
            {
                return new TreeNode(
                     bottomUpTree(2 * item - 1, depth - 1)
                   , bottomUpTree(2 * item, depth - 1)
                   , item
                   );
            }
            else
            {
                return new TreeNode(item);
            }
        }

        private TreeNode(TreeNode left, TreeNode right, int item)
        {
            _next = new Next();
            _next.left = left;
            _next.right = right;
            _item = item;
        }

        internal int itemCheck()
        {
            // if necessary deallocate here
            if (_next == null) return _item;
            else return _item + _next.left.itemCheck() - _next.right.itemCheck();
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
