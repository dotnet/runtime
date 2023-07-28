// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Adapted from reverse-complement C# .NET Core program
// http://benchmarksgame.alioth.debian.org/u64q/program.php?test=revcomp&lang=csharpcore&id=1
// aka (as of 2017-09-01) rev 1.2 of https://alioth.debian.org/scm/viewvc.php/benchmarksgame/bench/revcomp/revcomp.csharp?root=benchmarksgame&view=log
// Best-scoring single-threaded C# .NET Core version as of 2017-09-01

/* The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/

   contributed by Robert F. Tobler to process large blocks of byte arrays
*/

using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
using Xunit;

namespace BenchmarksGame
{
    public class ReverseComplement_1
    {
        struct Block
        {
            public byte[] Data; public int Count;
            public int Read(BinaryReader r)
            {
                Data = r.ReadBytes(16384); Count++; return Data.Length;
            }
            public Index IndexOf(byte b, int o)
            {
                return new Index { Block = Count, Pos = Array.IndexOf(Data, b, o) };
            }
        }

        struct Index
        {
            public int Block; public int Pos;
            public static readonly Index None = new Index { Block = -1, Pos = -1 };
            public bool InBlock(Block b) { return Block == b.Count; }
        }

        const byte Gt = (byte)'>';
        const byte Lf = (byte)'\n';

        [Fact]
        public static int TestEntryPoint()
        {
            var helpers = new TestHarnessHelpers(bigInput: false);
            var outBytes = new byte[helpers.FileLength];
            using (var inputStream = helpers.GetInputStream())
            using (var outputStream = new MemoryStream(outBytes))
            {
                Bench(inputStream, outputStream);
            }
            Console.WriteLine(System.Text.Encoding.UTF8.GetString(outBytes));
            if (!MatchesChecksum(outBytes, helpers.CheckSum))
            {
                return -1;
            }
            return 100;
        }

        static bool MatchesChecksum(byte[] bytes, string checksum)
        {
            using (var md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(bytes);
                return (checksum == BitConverter.ToString(hash));
            }
        }

        static void Bench(Stream input, Stream output)
        {
            InitComplements();
            var seq = new List<byte[]>();
            var b = new Block { Count = -1 };
            Index line = Index.None, start = Index.None, end = Index.None;
            using (var r = new BinaryReader(input))
            {
                using (var w = output)
                {
                    while (b.Read(r) > 0)
                    {
                        seq.Add(b.Data);
                        if (line.Pos < 0) line = b.IndexOf(Gt, 0);
                        while (line.Pos >= 0)
                        {
                            if (start.Pos < 0)
                            {
                                var off = line.InBlock(b) ? line.Pos : 0;
                                start = b.IndexOf(Lf, off);
                                if (start.Pos < 0)
                                {
                                    w.Write(b.Data, off, b.Data.Length - off);
                                    seq.Clear(); break;
                                }
                                w.Write(b.Data, off, start.Pos + 1 - off);
                            }
                            if (end.Pos < 0)
                            {
                                end = b.IndexOf(Gt, start.InBlock(b) ? start.Pos : 0);
                                if (end.Pos < 0) break;
                            }
                            Reverse(w, start.Pos, end.Pos, seq);
                            if (seq.Count > 1) seq.RemoveRange(0, seq.Count - 1);
                            line = end; end = Index.None; start = Index.None;
                        }
                    }
                    if (start.Pos >= 0 && end.Pos < 0)
                        Reverse(w, start.Pos, seq[seq.Count - 1].Length, seq);
                }
            }
        }

        const string Seq = "ABCDGHKMRTVYabcdghkmrtvy";
        const string Rev = "TVGHCDMKYABRTVGHCDMKYABR";
        static byte[] comp = new byte[256];

        static void InitComplements()
        {
            for (byte i = 0; i < 255; i++) comp[i] = i;
            for (int i = 0; i < Seq.Length; i++)
                comp[(byte)Seq[i]] = (byte)Rev[i];
            comp[Lf] = 0; comp[(byte)' '] = 0;
        }

        const int LineLen = 61;
        const int BufSize = LineLen * 269;
        static byte[] buf = new byte[BufSize];

        static void Reverse(Stream w, int si, int ei, List<byte[]> bl)
        {
            int bi = 0, line = LineLen - 1;
            for (int ri = bl.Count - 1; ri >= 0; ri--)
            {
                var b = bl[ri]; int off = ri == 0 ? si : 0;
                for (int i = (ri == bl.Count - 1 ? ei : b.Length) - 1; i >= off; i--)
                {
                    var c = comp[b[i]]; if (c > 0) buf[bi++] = c;
                    if (bi == line)
                    {
                        buf[bi++] = Lf; line += LineLen;
                        if (bi == BufSize)
                        {
                            w.Write(buf, 0, BufSize); bi = 0; line = LineLen - 1;
                        }
                    }
                }
            }
            if (bi > 0)
            {
                if (buf[bi - 1] != Lf) buf[bi++] = Lf; w.Write(buf, 0, bi);
            }
        }
    }
}
