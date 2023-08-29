// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Adapted from regex-redux C# .NET Core program
// http://benchmarksgame.alioth.debian.org/u64q/program.php?test=regexredux&lang=csharpcore&id=1
// aka (as of 2017-09-01) rev 1.3 of https://alioth.debian.org/scm/viewvc.php/benchmarksgame/bench/regexredux/regexredux.csharp?root=benchmarksgame&view=log
// Best-scoring single-threaded C# .NET Core version as of 2017-09-01

/* The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/
 * 
 * regex-dna program contributed by Isaac Gouy 
 * converted from regex-dna program
 *
*/

using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace BenchmarksGame
{
    public class RegexRedux_1
    {
        [Fact]
        public static int TestEntryPoint()
        {
            var helpers = new TestHarnessHelpers(bigInput: false);

            using (var inputStream = helpers.GetInputStream())
            using (var input = new StreamReader(inputStream))
            {
                if (Bench(input, true) != helpers.ExpectedLength)
                {
                    return -1;
                }
            }

            return 100;
        }

        static int Bench(TextReader inputReader, bool verbose)
        {
            // read FASTA sequence
            String sequence = inputReader.ReadToEnd();
            int initialLength = sequence.Length;

            // remove FASTA sequence descriptions and new-lines
            Regex r = new Regex(">.*\n|\n", RegexOptions.Compiled);
            sequence = r.Replace(sequence, "");
            int codeLength = sequence.Length;

            // regex match
            string[] variants = {
                "agggtaaa|tttaccct",
                "[cgt]gggtaaa|tttaccc[acg]",
                "a[act]ggtaaa|tttacc[agt]t",
                "ag[act]gtaaa|tttac[agt]ct",
                "agg[act]taaa|ttta[agt]cct",
                "aggg[acg]aaa|ttt[cgt]ccct",
                "agggt[cgt]aa|tt[acg]accct",
                "agggta[cgt]a|t[acg]taccct",
                "agggtaa[cgt]|[acg]ttaccct"
            };

            int count;
            foreach (string v in variants)
            {
                count = 0;
                r = new Regex(v, RegexOptions.Compiled);

                for (Match m = r.Match(sequence); m.Success; m = m.NextMatch()) count++;
                if (verbose)
                    Console.WriteLine("{0} {1}", v, count);
            }

            // regex substitution
            IUB[] codes = {
                new IUB("tHa[Nt]", "<4>"),
                new IUB("aND|caN|Ha[DS]|WaS", "<3>"),
                new IUB("a[NSt]|BY", "<2>"),
                new IUB("<[^>]*>", "|"),
                new IUB("\\|[^|][^|]*\\|" , "-")
            };

            foreach (IUB iub in codes)
            {
                r = new Regex(iub.code, RegexOptions.Compiled);
                sequence = r.Replace(sequence, iub.alternatives);
            }
            if (verbose)
                Console.WriteLine("\n{0}\n{1}\n{2}", initialLength, codeLength, sequence.Length);

            return sequence.Length;
        }

        struct IUB
        {
            public string code;
            public string alternatives;

            public IUB(string code, string alternatives)
            {
                this.code = code;
                this.alternatives = alternatives;
            }
        }
    }
}
