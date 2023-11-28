// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Adapted from k-nucleotide C# .NET Core program
// http://benchmarksgame.alioth.debian.org/u64q/program.php?test=knucleotide&lang=csharpcore&id=1
// aka (as of 2017-09-01) rev 1.2 of https://alioth.debian.org/scm/viewvc.php/benchmarksgame/bench/knucleotide/knucleotide.csharp?root=benchmarksgame&view=log
// Best-scoring single-threaded C# .NET Core version as of 2017-09-01

/* The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/
 *
 * byte processing version using C# *3.0 idioms by Robert F. Tobler
 */

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace BenchmarksGame
{
    public struct ByteString : IEquatable<ByteString>
    {
        public byte[] Array;
        public int Start;
        public int Length;

        public ByteString(byte[] array, int start, int length)
        {
            Array = array; Start = start; Length = length;
        }

        public ByteString(string text)
        {
            Start = 0; Length = text.Length;
            Array = Encoding.ASCII.GetBytes(text);
        }

        public override int GetHashCode()
        {
            if (Length < 1) return 0;
            int hc = Length ^ (Array[Start] << 24); if (Length < 2) return hc;
            hc ^= Array[Start + Length - 1] << 20; if (Length < 3) return hc;
            for (int c = Length - 2; c > 0; c--)
                hc ^= Array[Start + c] << (c & 0xf);
            return hc;
        }

        public bool Equals(ByteString other)
        {
            if (Length != other.Length) return false;
            for (int i = 0; i < Length; i++)
                if (Array[Start + i] != other.Array[other.Start + i]) return false;
            return true;
        }

        public override string ToString()
        {
            return Encoding.ASCII.GetString(Array, Start, Length);
        }
    }

    public static class Extensions
    {
        public static byte[] GetBytes(this List<string> input)
        {
            int count = 0;
            for (int i = 0; i < input.Count; i++) count += input[i].Length;
            var byteArray = new byte[count];
            count = 0;
            for (int i = 0; i < input.Count; i++)
            {
                string line = input[i];
                Encoding.ASCII.GetBytes(line, 0, line.Length, byteArray, count);
                count += line.Length;
            }
            return byteArray;
        }
    }

    public class KNucleotide_1
    {

        [Fact]
        public static int TestEntryPoint()
        {
            var helpers = new TestHarnessHelpers(bigInput: false);

            using (var inputStream = helpers.GetInputStream())
            {
                if (!Bench(inputStream, helpers, true))
                {
                    return -1;
                }
            }

            return 100;
        }

        static bool Bench(Stream inputStream, TestHarnessHelpers helpers, bool verbose)
        {
            string line;
            StreamReader source = new StreamReader(inputStream);
            var input = new List<string>();

            while ((line = source.ReadLine()) != null)
                if (line[0] == '>' && line.Substring(1, 5) == "THREE")
                    break;

            while ((line = source.ReadLine()) != null)
            {
                char c = line[0];
                if (c == '>') break;
                if (c != ';') input.Add(line.ToUpper());
            }

            KNucleotide kn = new KNucleotide(input.GetBytes());
            input = null;
            bool ok = true;
            for (int f = 1; f < 3; f++)
                ok &= kn.WriteFrequencies(f, helpers.expectedFrequencies[f - 1], verbose);
            int i = 0;
            foreach (var seq in
                     new[] { "GGT", "GGTA", "GGTATT", "GGTATTTTAATT",
                         "GGTATTTTAATTTATAGT"})
                ok &= kn.WriteCount(seq, helpers.expectedCountFragments[i++], verbose);

            return ok;
        }
    }

    public class KNucleotide
    {

        private class Count
        {
            public int V;
            public Count(int v) { V = v; }
        }

        private Dictionary<ByteString, Count> frequencies
            = new Dictionary<ByteString, Count>();
        private byte[] sequence;

        public KNucleotide(byte[] s) { sequence = s; }

        public bool WriteFrequencies(int length, int[] expectedCounts, bool verbose)
        {
            GenerateFrequencies(length);
            var items = new List<KeyValuePair<ByteString, Count>>(frequencies);
            items.Sort(SortByFrequencyAndCode);
            double percent = 100.0 / (sequence.Length - length + 1);
            bool ok = true;
            int i = 0;
            foreach (var item in items)
            {
                ok &= (item.Value.V == expectedCounts[i++]);
                if (verbose)
                {
                    Console.WriteLine("{0} {1:f3}",
                                item.Key.ToString(), item.Value.V * percent);
                }
            }
            if (verbose) Console.WriteLine();
            return ok;
        }

        public bool WriteCount(string fragment, int expectedCount, bool verbose)
        {
            GenerateFrequencies(fragment.Length);
            Count count;
            if (!frequencies.TryGetValue(new ByteString(fragment), out count))
                count = new Count(0);
            if (verbose) Console.WriteLine("{0}\t{1}", count.V, fragment);
            return (count.V == expectedCount);
        }

        private void GenerateFrequencies(int length)
        {
            frequencies.Clear();
            for (int frame = 0; frame < length; frame++)
                KFrequency(frame, length);
        }

        private void KFrequency(int frame, int k)
        {
            int n = sequence.Length - k + 1;
            for (int i = frame; i < n; i += k)
            {
                var key = new ByteString(sequence, i, k);
                Count count;
                if (frequencies.TryGetValue(key, out count))
                    count.V++;
                else
                    frequencies[key] = new Count(1);
            }
        }

        int SortByFrequencyAndCode(
                KeyValuePair<ByteString, Count> i0,
                KeyValuePair<ByteString, Count> i1)
        {
            int order = i1.Value.V.CompareTo(i0.Value.V);
            if (order != 0) return order;
            return i0.Key.ToString().CompareTo(i1.Key.ToString());
        }
    }
}
