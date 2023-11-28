// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Adapted from k-nucleotide C# .NET Core #9 program
// http://benchmarksgame.alioth.debian.org/u64q/program.php?test=knucleotide&lang=csharpcore&id=9
// aka (as of 2017-09-01) rev 1.1 of https://alioth.debian.org/scm/viewvc.php/benchmarksgame/bench/knucleotide/knucleotide.csharp-9.csharp?root=benchmarksgame&view=log
// Best-scoring C# .NET Core version as of 2017-09-01

/* The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/
 
   submitted by Josh Goldfoot
   Modified to reduce memory and do more in parallel by Anthony Lloyd
 */

using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Xunit;

namespace BenchmarksGame
{
    class Wrapper { public int v = 1; }
    public class KNucleotide_9
    {
        const int BLOCK_SIZE = 1024 * 1024 * 8;
        static List<byte[]> threeBlocks = new List<byte[]>();
        static int threeStart, threeEnd;
        static byte[] tonum = new byte[256];
        static char[] tochar = new char[] { 'A', 'C', 'G', 'T' };

        static int read(Stream stream, byte[] buffer, int offset, int count)
        {
            var bytesRead = stream.Read(buffer, offset, count);
            return bytesRead == count ? offset + count
                 : bytesRead == 0 ? offset
                 : read(stream, buffer, offset + bytesRead, count - bytesRead);
        }

        static int find(byte[] buffer, byte[] toFind, int i, ref int matchIndex)
        {
            if (matchIndex == 0)
            {
                i = Array.IndexOf(buffer, toFind[0], i);
                if (i == -1) return -1;
                matchIndex = 1;
                return find(buffer, toFind, i + 1, ref matchIndex);
            }
            else
            {
                int bl = buffer.Length, fl = toFind.Length;
                while (i < bl && matchIndex < fl)
                {
                    if (buffer[i++] != toFind[matchIndex++])
                    {
                        matchIndex = 0;
                        return find(buffer, toFind, i, ref matchIndex);
                    }
                }
                return matchIndex == fl ? i : -1;
            }
        }

        static void loadThreeData(Stream stream)
        {
            // find three sequence
            int matchIndex = 0;
            var toFind = new[] { (byte)'>', (byte)'T', (byte)'H', (byte)'R', (byte)'E', (byte)'E' };
            var buffer = new byte[BLOCK_SIZE];
            do
            {
                threeEnd = read(stream, buffer, 0, BLOCK_SIZE);
                threeStart = find(buffer, toFind, 0, ref matchIndex);
            } while (threeStart == -1);

            // Skip to end of line
            matchIndex = 0;
            toFind = new[] { (byte)'\n' };
            threeStart = find(buffer, toFind, threeStart, ref matchIndex);
            while (threeStart == -1)
            {
                threeEnd = read(stream, buffer, 0, BLOCK_SIZE);
                threeStart = find(buffer, toFind, 0, ref matchIndex);
            }
            threeBlocks.Add(buffer);

            if (threeEnd != BLOCK_SIZE) // Needs to be at least 2 blocks
            {
                var bytes = threeBlocks[0];
                for (int i = threeEnd; i < bytes.Length; i++)
                    bytes[i] = 255;
                threeEnd = 0;
                threeBlocks.Add(Array.Empty<byte>());
                return;
            }

            // find next seq or end of input
            matchIndex = 0;
            toFind = new[] { (byte)'>' };
            threeEnd = find(buffer, toFind, threeStart, ref matchIndex);
            while (threeEnd == -1)
            {
                buffer = new byte[BLOCK_SIZE];
                var bytesRead = read(stream, buffer, 0, BLOCK_SIZE);
                threeEnd = bytesRead == BLOCK_SIZE ? find(buffer, toFind, 0, ref matchIndex)
                            : bytesRead;
                threeBlocks.Add(buffer);
            }

            if (threeStart + 18 > BLOCK_SIZE) // Key needs to be in the first block
            {
                byte[] block0 = threeBlocks[0], block1 = threeBlocks[1];
                Buffer.BlockCopy(block0, threeStart, block0, threeStart - 18, BLOCK_SIZE - threeStart);
                Buffer.BlockCopy(block1, 0, block0, BLOCK_SIZE - 18, 18);
                for (int i = 0; i < 18; i++) block1[i] = 255;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void check(Dictionary<long, Wrapper> dict, ref long rollingKey, byte nb, long mask)
        {
            if (nb == 255) return;
            rollingKey = ((rollingKey & mask) << 2) | nb;
            Wrapper w;
            if (dict.TryGetValue(rollingKey, out w))
                w.v++;
            else
                dict[rollingKey] = new Wrapper();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void checkEnding(Dictionary<long, Wrapper> dict, ref long rollingKey, byte b, byte nb, long mask)
        {
            if (nb == b)
            {
                Wrapper w;
                if (dict.TryGetValue(rollingKey, out w))
                    w.v++;
                else
                    dict[rollingKey] = new Wrapper();
                rollingKey = ((rollingKey << 2) | nb) & mask;
            }
            else if (nb != 255)
            {
                rollingKey = ((rollingKey << 2) | nb) & mask;
            }
        }

        static Task<string> count(int l, long mask, Func<Dictionary<long, Wrapper>, string> summary)
        {
            return Task.Run(() =>
            {
                long rollingKey = 0;
                var firstBlock = threeBlocks[0];
                var start = threeStart;
                while (--l > 0) rollingKey = (rollingKey << 2) | firstBlock[start++];
                var dict = new Dictionary<long, Wrapper>();
                for (int i = start; i < firstBlock.Length; i++)
                    check(dict, ref rollingKey, firstBlock[i], mask);

                int lastBlockId = threeBlocks.Count - 1;
                for (int bl = 1; bl < lastBlockId; bl++)
                {
                    var bytes = threeBlocks[bl];
                    for (int i = 0; i < bytes.Length; i++)
                        check(dict, ref rollingKey, bytes[i], mask);
                }

                var lastBlock = threeBlocks[lastBlockId];
                for (int i = 0; i < threeEnd; i++)
                    check(dict, ref rollingKey, lastBlock[i], mask);
                return summary(dict);
            });
        }

        static Dictionary<long, Wrapper> countEnding(int l, long mask, byte b)
        {
            long rollingKey = 0;
            var firstBlock = threeBlocks[0];
            var start = threeStart;
            while (--l > 0) rollingKey = (rollingKey << 2) | firstBlock[start++];
            var dict = new Dictionary<long, Wrapper>();
            for (int i = start; i < firstBlock.Length; i++)
                checkEnding(dict, ref rollingKey, b, firstBlock[i], mask);

            int lastBlockId = threeBlocks.Count - 1;
            for (int bl = 1; bl < lastBlockId; bl++)
            {
                var bytes = threeBlocks[bl];
                for (int i = 0; i < bytes.Length; i++)
                    checkEnding(dict, ref rollingKey, b, bytes[i], mask);
            }

            var lastBlock = threeBlocks[lastBlockId];
            for (int i = 0; i < threeEnd; i++)
                checkEnding(dict, ref rollingKey, b, lastBlock[i], mask);
            return dict;
        }

        static Task<string> count4(int l, long mask, Func<Dictionary<long, Wrapper>, string> summary)
        {
            return Task.Factory.ContinueWhenAll(
                new[] {
                Task.Run(() => countEnding(l, mask, 0)),
                Task.Run(() => countEnding(l, mask, 1)),
                Task.Run(() => countEnding(l, mask, 2)),
                Task.Run(() => countEnding(l, mask, 3))
                }
                , dicts =>
                {
                    var d = new Dictionary<long, Wrapper>(dicts.Sum(i => i.Result.Count));
                    for (int i = 0; i < dicts.Length; i++)
                        foreach (var kv in dicts[i].Result)
                            d[(kv.Key << 2) | (long)i] = kv.Value;
                    return summary(d);
                });
        }

        static string writeFrequencies(Dictionary<long, Wrapper> freq, int fragmentLength, int[] expected, ref bool ok)
        {
            var sb = new StringBuilder();
            double percent = 100.0 / freq.Values.Sum(i => i.v);
            int idx = 0;
            foreach (var kv in freq.OrderByDescending(i => i.Value.v))
            {
                ok &= (kv.Value.v == expected[idx++]);
                var keyChars = new char[fragmentLength];
                var key = kv.Key;
                for (int i = keyChars.Length - 1; i >= 0; --i)
                {
                    keyChars[i] = tochar[key & 0x3];
                    key >>= 2;
                }
                sb.Append(keyChars);
                sb.Append(" ");
                sb.AppendLine((kv.Value.v * percent).ToString("F3"));
            }
            return sb.ToString();
        }

        static string writeCount(Dictionary<long, Wrapper> dictionary, string fragment, int expected, ref bool ok)
        {
            long key = 0;
            for (int i = 0; i < fragment.Length; ++i)
                key = (key << 2) | tonum[fragment[i]];
            Wrapper w;
            var n = dictionary.TryGetValue(key, out w) ? w.v : 0;
            ok &= (n == expected);
            return string.Concat(n.ToString(), "\t", fragment);
        }

        [Fact]
        public static int TestEntryPoint()
        {
            var helpers = new TestHarnessHelpers(bigInput: false);
            bool ok = Bench(helpers, true);

            return (ok ? 100 : -1);
        }

        static bool Bench(TestHarnessHelpers helpers, bool verbose)
        {
            // Reset static state
            threeBlocks.Clear();
            threeStart = 0;
            threeEnd = 0;

            tonum['c'] = 1; tonum['C'] = 1;
            tonum['g'] = 2; tonum['G'] = 2;
            tonum['t'] = 3; tonum['T'] = 3;
            tonum['\n'] = 255; tonum['>'] = 255; tonum[255] = 255;

            using (var inputStream = helpers.GetInputStream())
            {
                loadThreeData(inputStream);
            }

            Parallel.ForEach(threeBlocks, bytes =>
            {
                for (int i = 0; i < bytes.Length; i++)
                    bytes[i] = tonum[bytes[i]];
            });

            bool ok = true;

            var task18 = count4(18, 0x7FFFFFFFF, d => writeCount(d, "GGTATTTTAATTTATAGT", helpers.expectedCountFragments[4], ref ok));
            var task12 = count4(12, 0x7FFFFF, d => writeCount(d, "GGTATTTTAATT", helpers.expectedCountFragments[3], ref ok));
            var task6 = count(6, 0x3FF, d => writeCount(d, "GGTATT", helpers.expectedCountFragments[2], ref ok));
            var task4 = count(4, 0x3F, d => writeCount(d, "GGTA", helpers.expectedCountFragments[1], ref ok));
            var task3 = count(3, 0xF, d => writeCount(d, "GGT", helpers.expectedCountFragments[0], ref ok));
            var task2 = count(2, 0x3, d => writeFrequencies(d, 2, helpers.expectedFrequencies[1], ref ok));
            var task1 = count(1, 0, d => writeFrequencies(d, 1, helpers.expectedFrequencies[0], ref ok));

            if (verbose)
            {
                Console.Out.WriteLineAsync(task1.Result);
                Console.Out.WriteLineAsync(task2.Result);
                Console.Out.WriteLineAsync(task3.Result);
                Console.Out.WriteLineAsync(task4.Result);
                Console.Out.WriteLineAsync(task6.Result);
                Console.Out.WriteLineAsync(task12.Result);
                Console.Out.WriteLineAsync(task18.Result);
            }
            else
            {
                Task.WaitAll(task1, task2, task3, task4, task6, task12, task18);
            }

            return ok;
        }
    }
}
