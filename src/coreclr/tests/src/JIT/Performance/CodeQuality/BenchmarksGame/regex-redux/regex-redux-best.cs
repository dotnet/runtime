// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Adapted from regex-redux C# .NET Core #5 program
// http://benchmarksgame.alioth.debian.org/u64q/program.php?test=regexredux&lang=csharpcore&id=5
// Best-scoring C# .NET Core version as of 2017-09-01

/* The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/
 
   Regex-Redux by Josh Goldfoot
   order variants by execution time by Anthony Lloyd
*/

using System;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace BenchmarksGame
{
    public static class regexredux
    {
        static Regex regex(string re)
        {
            // Not compiled on .Net Core, hence poor benchmark results.
            return new Regex(re, RegexOptions.Compiled);
        }

        static string regexCount(string s, string r)
        {
            int c = 0;
            var m = regex(r).Match(s);
            while (m.Success) { c++; m = m.NextMatch(); }
            return r + " " + c;
        }

        public static void Main(string[] args)
        {
            var sequences = Console.In.ReadToEnd();
            var initialLength = sequences.Length;
            sequences = Regex.Replace(sequences, ">.*\n|\n", "");

            var magicTask = Task.Run(() =>
            {
                var newseq = regex("tHa[Nt]").Replace(sequences, "<4>");
                newseq = regex("aND|caN|Ha[DS]|WaS").Replace(newseq, "<3>");
                newseq = regex("a[NSt]|BY").Replace(newseq, "<2>");
                newseq = regex("<[^>]*>").Replace(newseq, "|");
                newseq = regex("\\|[^|][^|]*\\|").Replace(newseq, "-");
                return newseq.Length;
            });

            var variant2 = Task.Run(() => regexCount(sequences, "[cgt]gggtaaa|tttaccc[acg]"));
            var variant3 = Task.Run(() => regexCount(sequences, "a[act]ggtaaa|tttacc[agt]t"));
            var variant7 = Task.Run(() => regexCount(sequences, "agggt[cgt]aa|tt[acg]accct"));
            var variant6 = Task.Run(() => regexCount(sequences, "aggg[acg]aaa|ttt[cgt]ccct"));
            var variant4 = Task.Run(() => regexCount(sequences, "ag[act]gtaaa|tttac[agt]ct"));
            var variant5 = Task.Run(() => regexCount(sequences, "agg[act]taaa|ttta[agt]cct"));
            var variant1 = Task.Run(() => regexCount(sequences, "agggtaaa|tttaccct"));
            var variant9 = Task.Run(() => regexCount(sequences, "agggtaa[cgt]|[acg]ttaccct"));
            var variant8 = Task.Run(() => regexCount(sequences, "agggta[cgt]a|t[acg]taccct"));

            Console.Out.WriteLineAsync(variant1.Result);
            Console.Out.WriteLineAsync(variant2.Result);
            Console.Out.WriteLineAsync(variant3.Result);
            Console.Out.WriteLineAsync(variant4.Result);
            Console.Out.WriteLineAsync(variant5.Result);
            Console.Out.WriteLineAsync(variant6.Result);
            Console.Out.WriteLineAsync(variant7.Result);
            Console.Out.WriteLineAsync(variant8.Result);
            Console.Out.WriteLineAsync(variant9.Result);
            Console.Out.WriteLineAsync("\n" + initialLength + "\n" + sequences.Length);
            Console.Out.WriteLineAsync(magicTask.Result.ToString());
        }
    }
}
