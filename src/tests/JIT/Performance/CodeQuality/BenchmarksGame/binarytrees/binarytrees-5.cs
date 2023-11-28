// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Adapted from binary-trees C# .NET Core #5 program
// http://benchmarksgame.alioth.debian.org/u64q/program.php?test=binarytrees&lang=csharpcore&id=5
// aka (as of 2017-09-01) rev 1.1 of https://alioth.debian.org/scm/viewvc.php/benchmarksgame/bench/binarytrees/binarytrees.csharp-5.csharp?root=benchmarksgame&view=log
// Best-scoring C# .NET Core version as of 2017-09-01

/* The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/ 

   contributed by Marek Safar  
   *reset*
   concurrency added by Peperud
   minor improvements by Alex Yakunin
*/

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

namespace BenchmarksGame
{
    public class BinaryTrees_5
    {
        public const int MinDepth = 4;

        [Fact]
        public static int TestEntryPoint()
        {
            return Test(null);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int Test(int? arg)
        {
            var n = arg ?? 0;

            int check = Bench(n, true);
            int expected = 4398;

            // Return 100 on success, anything else on failure.
            return check - expected + 100;
        }

        static int Bench(int n, bool verbose)
        {
            var maxDepth = n < (MinDepth + 2) ? MinDepth + 2 : n;
            var stretchDepth = maxDepth + 1;

            var stretchDepthTask = Task.Run(() => TreeNode.CreateTree(stretchDepth).CountNodes());
            var maxDepthTask = Task.Run(() => TreeNode.CreateTree(maxDepth).CountNodes());

            var tasks = new Task<string>[(maxDepth - MinDepth) / 2 + 1];
            for (int depth = MinDepth, ti = 0; depth <= maxDepth; depth += 2, ti++)
            {
                var iterationCount = 1 << (maxDepth - depth + MinDepth);
                var depthCopy = depth; // To make sure closure value doesn't change
                tasks[ti] = Task.Run(() =>
                {
                    var count = 0;
                    if (depthCopy >= 17)
                    {
                        // Parallelized computation for relatively large tasks
                        var miniTasks = new Task<int>[iterationCount];
                        for (var i = 0; i < iterationCount; i++)
                            miniTasks[i] = Task.Run(() => TreeNode.CreateTree(depthCopy).CountNodes());
                        Task.WaitAll(miniTasks);
                        for (var i = 0; i < iterationCount; i++)
                            count += miniTasks[i].Result;
                    }
                    else
                    {
                        // Sequential computation for smaller tasks
                        for (var i = 0; i < iterationCount; i++)
                            count += TreeNode.CreateTree(depthCopy).CountNodes();
                    }
                    return $"{iterationCount}\t trees of depth {depthCopy}\t check: {count}";
                });
            }
            Task.WaitAll(tasks);

            if (verbose)
            {
                int count = 0;
                Action<string> printAndSum = (string s) =>
                {
                    Console.WriteLine(s);
                    count += int.Parse(s.Substring(s.LastIndexOf(':') + 1).TrimStart());
                };

                printAndSum(String.Format("stretch tree of depth {0}\t check: {1}",
                    stretchDepth, stretchDepthTask.Result));
                foreach (var task in tasks)
                    printAndSum(task.Result);
                printAndSum(String.Format("long lived tree of depth {0}\t check: {1}",
                    maxDepth, maxDepthTask.Result));

                return count;
            }

            return 0;
        }
    }

    public struct TreeNode
    {
        public sealed class NodeData
        {
            public TreeNode Left, Right;

            public NodeData(TreeNode left, TreeNode right)
            {
                Left = left;
                Right = right;
            }
        }

        public NodeData Data;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TreeNode(TreeNode left, TreeNode right)
        {
            Data = new NodeData(left, right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TreeNode CreateTree(int depth)
        {
            return depth <= 0
                ? default(TreeNode)
                : new TreeNode(CreateTree(depth - 1), CreateTree(depth - 1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CountNodes()
        {
            if (ReferenceEquals(Data, null))
                return 1;
            return 1 + Data.Left.CountNodes() + Data.Right.CountNodes();
        }
    }
}
