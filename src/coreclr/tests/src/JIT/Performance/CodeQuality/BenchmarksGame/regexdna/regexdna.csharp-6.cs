// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
/* The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/
 *
 * contributed by Jimmy Tang
 *
 * modified for use with xunit-performance
 */

using Microsoft.Xunit.Performance;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

[assembly: OptimizeForBenchmarks]
[assembly: MeasureInstructionsRetired]

namespace BenchmarksGame
{

public static class Regexdna
{

#if DEBUG
    const bool Verbose = true;
    const int Iterations = 1;
    const string InputFile = "regexdna-input25.txt";
#else
    const bool Verbose = false;
    const int Iterations = 6;
    const string InputFile = "regexdna-input25000.txt";
#endif

   static string FindInput(string s)
   {
       string CoreRoot = System.Environment.GetEnvironmentVariable("CORE_ROOT");

       if (CoreRoot == null)
       {
           Console.WriteLine("This benchmark requries CORE_ROOT to be set");
           return null;
       }

       string inputFile = s ?? InputFile;

       // Normal testing -- input file will end up next to the assembly
       // and CoreRoot points at the test overlay dir
       string[] pathPartsNormal = new string[] {
           CoreRoot, "..", "..", "JIT", "Performance",
           "CodeQuality", "BenchmarksGame", "regexdna", "regexdna", inputFile
       };

       string inputPathNormal = Path.Combine(pathPartsNormal);

       // Perf testing -- input file will end up next to the assembly
       // and CoreRoot points at this directory
       string[] pathPartsPerf = new string[] { CoreRoot, inputFile };

       string inputPathPerf = Path.Combine(pathPartsPerf);

       string inputPath = null;

       if (File.Exists(inputPathNormal))
       {
           inputPath = inputPathNormal;
       }
       else if (File.Exists(inputPathPerf))
       {
           inputPath = inputPathPerf;
       }

       if (inputPath != null)
       {
           Console.WriteLine("Using input file {0}", inputPath);
       }
       else
       {
           Console.WriteLine("Unable to find input file {0}", inputFile);
       }

       return inputPath;
   }

   public static int Main(string[] args)
   {
       string inputFile = InputFile;
       int iterations = Iterations;
       bool verbose = Verbose;

       for (int i = 0; i < args.Length; i++)
       {
           if (args[i] == "-v")
           {
               verbose = true;
           }
           else if (args[i] == "-q")
           {
               verbose = false;
           }
           else if (args[i] == "-i")
           {
               i++;

               if (i < args.Length)
               {
                   Int32.TryParse(args[i], out iterations);
               }
           }
           else
           {
               inputFile = args[i];
           }
       }

       string fullInputFile = FindInput(inputFile);

       if (fullInputFile == null)
       {
           Console.WriteLine("unable to find input");
           return -1;
       }

       if (iterations != Iterations)
       {
           Console.WriteLine("Running {0} iterations", iterations);
       }

       using (var r = File.OpenText(fullInputFile))
       {
           string sequence = r.ReadToEnd();

           // Warmup

           BenchInner(verbose, sequence);

           Stopwatch sw = Stopwatch.StartNew();
           for (int j = 0; j < iterations; j++)
           {
               BenchInner(verbose, sequence);
           }
           sw.Stop();

           Console.WriteLine("regexdna [{0} iters]: {1}ms", iterations, sw.ElapsedMilliseconds);
       }

       return 100;
   }

   static void BenchInner(bool verbose, string sequence)
   {
       int initialLength = sequence.Length;

       sequence = Regex.Replace(sequence, ">.*\n|\n", "");
       int codeLength = sequence.Length;

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

       var flags = variants.Select((v, i) => {
           var flag = new ManualResetEvent(false);
           ThreadPool.QueueUserWorkItem(x => {
               variants[i] += " " + Regex.Matches(sequence, v).Count;
               flag.Set();
           });
           return flag;
       });
       WaitHandle.WaitAll(flags.ToArray());
       if (verbose)
       {
           Console.WriteLine(string.Join("\n", variants));
       }

       var dict = new Dictionary<string, string> {
           {"B", "(c|g|t)"}, {"D", "(a|g|t)"},   {"H", "(a|c|t)"}, {"K", "(g|t)"},
           {"M", "(a|c)"},   {"N", "(a|c|g|t)"}, {"R", "(a|g)"},   {"S", "(c|g)"},
           {"V", "(a|c|g)"}, {"W", "(a|t)"},     {"Y", "(c|t)"}
       };

       sequence = new Regex("[WYKMSRBDVHN]").Replace(sequence, m => dict[m.Value]);

       if (verbose)
       {
           Console.WriteLine("\n{0}\n{1}\n{2}", initialLength, codeLength, sequence.Length);
       }
   }

   [Benchmark]
   public static void Bench()
   {
       string fullInputFile = FindInput(InputFile);

       if (fullInputFile == null)
       {
           throw new Exception("unable to find input");
       }

       using (var r = File.OpenText(fullInputFile))
       {
           string sequence = r.ReadToEnd();

           foreach (var iteration in Benchmark.Iterations)
           {
               using (iteration.StartMeasurement())
               {
                   for (int i = 0; i < Iterations; i++)
                   {
                       BenchInner(false, sequence);
                   }
               }
           }
       }
   }
}

}
