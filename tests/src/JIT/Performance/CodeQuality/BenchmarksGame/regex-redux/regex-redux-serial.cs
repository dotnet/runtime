// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Adapted from regex-redux C# .NET Core program
// http://benchmarksgame.alioth.debian.org/u64q/program.php?test=regexredux&lang=csharpcore&id=1
// Best-scoring single-threaded C# .NET Core version as of 2017-09-01

/* The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/
 * 
 * regex-dna program contributed by Isaac Gouy 
 * converted from regex-dna program
 *
*/

using System;
using System.Text.RegularExpressions;

namespace BenchmarksGame
{
    class regexredux
    {
        static void Main(string[] args)
        {

            // read FASTA sequence
            String sequence = Console.In.ReadToEnd();
            int initialLength = sequence.Length;

            // remove FASTA sequence descriptions and new-lines
            Regex r = new Regex(">.*\n|\n", RegexOptions.Compiled);
            sequence = r.Replace(sequence, "");
            int codeLength = sequence.Length;


            // regex match
            string[] variants = {
         "agggtaaa|tttaccct"
         ,"[cgt]gggtaaa|tttaccc[acg]"
         ,"a[act]ggtaaa|tttacc[agt]t"
         ,"ag[act]gtaaa|tttac[agt]ct"
         ,"agg[act]taaa|ttta[agt]cct"
         ,"aggg[acg]aaa|ttt[cgt]ccct"
         ,"agggt[cgt]aa|tt[acg]accct"
         ,"agggta[cgt]a|t[acg]taccct"
         ,"agggtaa[cgt]|[acg]ttaccct"
      };

            int count;
            foreach (string v in variants)
            {
                count = 0;
                r = new Regex(v, RegexOptions.Compiled);

                for (Match m = r.Match(sequence); m.Success; m = m.NextMatch()) count++;
                Console.WriteLine("{0} {1}", v, count);
            }


            // regex substitution
            IUB[] codes = {
          new IUB("tHa[Nt]", "<4>")
         ,new IUB("aND|caN|Ha[DS]|WaS", "<3>")
         ,new IUB("a[NSt]|BY", "<2>")
         ,new IUB("<[^>]*>", "|")
         ,new IUB("\\|[^|][^|]*\\|" , "-")
      };

            foreach (IUB iub in codes)
            {
                r = new Regex(iub.code, RegexOptions.Compiled);
                sequence = r.Replace(sequence, iub.alternatives);
            }
            Console.WriteLine("\n{0}\n{1}\n{2}",
               initialLength, codeLength, sequence.Length);
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
