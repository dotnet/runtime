// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;
using Xunit;
using Microsoft.Xunit.Performance;

namespace System.Text.RegularExpressions.Tests
{
    public class RegexRedux
    {
        static readonly string input = File.ReadAllText(Path.Combine("regexredux", "200_000.in"));

        static Regex regex(string re, RegexOptions options)
        {
             return new Regex(re, options);
        }

        static string regexCount(string s, string r, RegexOptions options)
        {
            int c = 0;
            var m = regex(r, options).Match(s);
            while (m.Success) { c++; m = m.NextMatch(); }
            return r + " " + c;
        }

        [Benchmark]
        [InlineData(RegexOptions.None)]
        [InlineData(RegexOptions.Compiled)]
        public void RegexReduxMini(RegexOptions options)
        {
            foreach (var iteration in Benchmark.Iterations)
            using (iteration.StartMeasurement())
            {
                string sequences = input;
                var initialLength = sequences.Length;
                sequences = Regex.Replace(sequences, ">.*\n|\n", "");

                var magicTask = Task.Run(() =>
                {
                    var newseq = regex("tHa[Nt]", options).Replace(sequences, "<4>");
                    newseq = regex("aND|caN|Ha[DS]|WaS", options).Replace(newseq, "<3>");
                    newseq = regex("a[NSt]|BY", options).Replace(newseq, "<2>");
                    newseq = regex("<[^>]*>", options).Replace(newseq, "|");
                    newseq = regex("\\|[^|][^|]*\\|", options).Replace(newseq, "-");
                    return newseq.Length;
                });

                var variant2 = Task.Run(() => regexCount(sequences, "[cgt]gggtaaa|tttaccc[acg]", options));
                var variant3 = Task.Run(() => regexCount(sequences, "a[act]ggtaaa|tttacc[agt]t", options));
                var variant7 = Task.Run(() => regexCount(sequences, "agggt[cgt]aa|tt[acg]accct", options));
                var variant6 = Task.Run(() => regexCount(sequences, "aggg[acg]aaa|ttt[cgt]ccct", options));
                var variant4 = Task.Run(() => regexCount(sequences, "ag[act]gtaaa|tttac[agt]ct", options));
                var variant5 = Task.Run(() => regexCount(sequences, "agg[act]taaa|ttta[agt]cct", options));
                var variant1 = Task.Run(() => regexCount(sequences, "agggtaaa|tttaccct", options));
                var variant9 = Task.Run(() => regexCount(sequences, "agggtaa[cgt]|[acg]ttaccct", options));
                var variant8 = Task.Run(() => regexCount(sequences, "agggta[cgt]a|t[acg]taccct", options));

                Task.WaitAll(magicTask, variant1, variant2, variant3, variant4, variant5, variant6, variant7, variant8, variant9);
            }
        }
    }
}
