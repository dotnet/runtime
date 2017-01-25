// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
/* The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/

   contributed by Robert F. Tobler to process large blocks of byte arrays

   modified for use with xunit-performance
*/

using Microsoft.Xunit.Performance;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

[assembly: OptimizeForBenchmarks]
[assembly: MeasureInstructionsRetired]

namespace BenchmarksGame
{

public static class Revcomp
{

#if DEBUG
    const int Iterations = 1;
    const string InputFile = "revcomp-input25.txt";
#else
    const int Iterations = 800;
    const string InputFile = "revcomp-input25000.txt";
#endif

   struct Block {
      public byte[] Data; public int Count;
      public int Read(BinaryReader r) {
         Data = r.ReadBytes(16384); Count++; return Data.Length;
      }
      public Index IndexOf(byte b, int o) {
         return new Index { Block = Count, Pos = Array.IndexOf(Data, b, o) };
      }
   }

   struct Index {
      public int Block; public int Pos;
      public static readonly Index None = new Index { Block = -1, Pos = -1 };
      public bool InBlock(Block b) { return Block == b.Count; }
   }

   const byte Gt = (byte)'>';
   const byte Lf = (byte)'\n';

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
           "CodeQuality", "BenchmarksGame", "revcomp", "revcomp", inputFile
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

   static int Main(string[] args)
   {
       bool verbose = false;
       string inputFile = InputFile;
       int iterations = Iterations;

       for (int i = 0; i < args.Length; i++)
       {
           if (args[i] == "-v")
           {
               verbose = true;
           }
           if (args[i] == "-i")
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
           return -1;
       }

       if (iterations != Iterations)
       {
           Console.WriteLine("Running {0} iterations", iterations);
       }

       // Warmup
       BenchInner(false, fullInputFile);

       Stopwatch sw = Stopwatch.StartNew();
       for (int j = 0; j < iterations; j++)
       {
           BenchInner(verbose, fullInputFile);
       }
       sw.Stop();
       Console.WriteLine("revcomp [{0} iters]: {1}ms", iterations, sw.ElapsedMilliseconds);

       return 100;
   }

   static void BenchInner(bool doOutput, string inputFile)
   {
      InitComplements();
      var seq = new List<byte[]>();
      var b = new Block { Count = -1 };
      Index line = Index.None, start = Index.None, end = Index.None;
      using (var r = new BinaryReader(File.OpenRead(inputFile))) {
          using (var w = doOutput ? Console.OpenStandardOutput() : Stream.Null) {
              while (b.Read(r) > 0) {
                  seq.Add(b.Data);
                  if (line.Pos < 0) line = b.IndexOf(Gt, 0);
                  while (line.Pos >= 0) {
                      if (start.Pos < 0) {
                          var off = line.InBlock(b) ? line.Pos : 0;
                          start = b.IndexOf(Lf, off);
                          if (start.Pos < 0) {
                              w.Write(b.Data, off, b.Data.Length - off);
                              seq.Clear(); break;
                          }
                          w.Write(b.Data, off, start.Pos + 1 - off);
                      }
                      if (end.Pos < 0) {
                          end = b.IndexOf(Gt, start.InBlock(b) ? start.Pos : 0);
                          if (end.Pos < 0) break;
                      }
                      w.Reverse(start.Pos, end.Pos, seq);
                      if (seq.Count > 1) seq.RemoveRange(0, seq.Count - 1);
                      line = end; end = Index.None; start = Index.None;
                  }
              }
              if (start.Pos >= 0 && end.Pos < 0)
              w.Reverse(start.Pos, seq[seq.Count -1].Length, seq);
          }
      }
   }

   const string Seq = "ABCDGHKMRTVYabcdghkmrtvy";
   const string Rev = "TVGHCDMKYABRTVGHCDMKYABR";
   static byte[] comp = new byte[256];

   static void InitComplements() {
      for (byte i = 0; i < 255; i++) comp[i] = i;
      for (int i = 0; i < Seq.Length; i++)
         comp[(byte)Seq[i]] = (byte)Rev[i];
      comp[Lf] = 0;  comp[(byte)' '] = 0;
   }

   const int LineLen = 61;
   const int BufSize = LineLen * 269;
   static byte[] buf = new byte[BufSize];

   static void Reverse(this Stream w, int si, int ei, List<byte[]> bl) {
      int bi = 0, line = LineLen - 1;
      for (int ri = bl.Count-1; ri >= 0; ri--) {
         var b = bl[ri]; int off = ri == 0 ? si : 0;
         for (int i = (ri == bl.Count-1 ? ei : b.Length)-1; i >= off; i--) {
            var c = comp[b[i]]; if (c > 0) buf[bi++] = c;
            if (bi == line) {
               buf[bi++] = Lf; line += LineLen;
               if (bi == BufSize) {
                  w.Write(buf, 0, BufSize); bi = 0; line = LineLen - 1;
               }
            }
         }
      }
      if (bi > 0) {
          if (buf[bi-1] != Lf) buf[bi++] = Lf; w.Write(buf, 0, bi);
      }
   }

   [Benchmark]
   public static void Bench()
   {
       string inputFile = FindInput(InputFile);

       if (inputFile == null)
       {
           throw new Exception("unable to find input");
       }

        foreach (var iteration in Benchmark.Iterations)
        {
            using (iteration.StartMeasurement())
            {
                for (int i = 0; i < Iterations; i++)
                {
                    BenchInner(false, inputFile);
                }
            }
        }
   }
}

}
