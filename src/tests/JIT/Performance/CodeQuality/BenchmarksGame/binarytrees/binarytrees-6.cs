// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Adapted from binary-trees C# .NET Core #6 program
// Best-scoring C# .NET Core version as of 2020-08-12

// The Computer Language Benchmarks Game
// https://salsa.debian.org/benchmarksgame-team/benchmarksgame/
//
// contributed by Marek Safar
// concurrency added by Peperud
// fixed long-lived tree by Anthony Lloyd
// ported from F# version by Anthony Lloyd

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;
//using BenchmarkDotNet.Attributes;
//using MicroBenchmarks;

namespace BenchmarksGame
{
    //[MaxIterationCount(40)] // the default 20 is not enough, the benchmark has multimodal distribution and needs more runs
    //[BenchmarkCategory(Categories.Runtime, Categories.BenchmarksGame, Categories.JIT, Categories.NoWASM)]
    public class BinaryTrees_6
    {
        const int MinDepth = 4;
        const int NoTasks = 4;

        // 16 is about 80ms
        // 18 is about 500ms
        // 20 is about 3.9s
        // 21 is used in official numbers; about 7.8s
        const int N = 18;

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

            const int expected = 4398;

            // Return 100 on success, anything else on failure.
            return check - expected + 100;
        }

        //[Benchmark(Description = nameof(BinaryTrees_6))]
        //public int RunBench() => Bench(N, verbose: false);

        static int Bench(int param, bool verbose)
        {
            int maxDepth = Math.Max(MinDepth + 2, param);

            var stretchTreeCheck = Task.Run(() =>
            {
                int stretchDepth = maxDepth + 1;
                return "stretch tree of depth " + stretchDepth + "\t check: " +
                            TreeNode.Create(stretchDepth).Check();
            });

            var longLivedTree = TreeNode.Create(maxDepth);
            var longLivedText = Task.Run(() =>
            {
                return "long lived tree of depth " + maxDepth +
                            "\t check: " + longLivedTree.Check();
            });

            var results = new string[(maxDepth - MinDepth) / 2 + 1];

            for (int i = 0; i < results.Length; i++)
            {
                int depth = i * 2 + MinDepth;
                int n = (1 << maxDepth - depth + MinDepth) / NoTasks;
                var tasks = new Task<int>[NoTasks];
                for (int t = 0; t < tasks.Length; t++)
                {
                    tasks[t] = Task.Run(() =>
                    {
                        var check2 = 0;
                        for (int i2 = n; i2 > 0; i2--)
                            check2 += TreeNode.Create(depth).Check();
                        return check2;
                    });
                }
                var check = tasks[0].Result;
                for (int t = 1; t < tasks.Length; t++)
                    check += tasks[t].Result;
                results[i] = (n * NoTasks) + "\t trees of depth " + depth +
                                "\t check: " + check;
            }

            if (verbose)
            {
                int count = 0;
                Action<string> printAndSum = (string s) =>
                {
                    Console.WriteLine(s);
                    count += int.Parse(s.Substring(s.LastIndexOf(':') + 1).TrimStart());
                };

                printAndSum(stretchTreeCheck.Result);
                for (int i = 0; i < results.Length; i++)
                    printAndSum(results[i]);
                printAndSum(longLivedText.Result);

                return count;
            }

            return 0;
        }

        struct TreeNode
        {
            class Next { public TreeNode left, right; }
            readonly Next next;

            TreeNode(TreeNode left, TreeNode right) =>
                next = new Next { left = left, right = right };

            internal static TreeNode Create(int d)
            {
                return d == 1 ? new TreeNode(new TreeNode(), new TreeNode())
                              : new TreeNode(Create(d - 1), Create(d - 1));
            }

            internal int Check()
            {
                int c = 1;
                var current = next;
                while (current != null)
                {
                    c += current.right.Check() + 1;
                    current = current.left.next;
                }
                return c;
            }
        }
    }
}
