// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Adapted from reverse-complement C# .NET Core #6
// http://benchmarksgame.alioth.debian.org/u64q/program.php?test=revcomp&lang=csharpcore&id=6
// aka (as of 2017-09-01) rev 1.4 of https://alioth.debian.org/scm/viewvc.php/benchmarksgame/bench/revcomp/revcomp.csharp-6.csharp?root=benchmarksgame&view=log
// Best-scoring C# .NET Core version as of 2017-09-01

/* The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/

   Contributed by Peperud
   Modified to reduce memory use by Anthony Lloyd
*/

using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Threading;
using Xunit;

namespace BenchmarksGame
{
    class RevCompSequence
    {
        public List<byte[]> Pages;
        public int StartHeader, EndExclusive;
        public Thread ReverseThread;
    }

    public class ReverseComplement_6
    {
        const int READER_BUFFER_SIZE = 1024 * 1024;
        const byte LF = 10, GT = (byte)'>', SP = 32;
        static BlockingCollection<byte[]> readQue;
        static BlockingCollection<RevCompSequence> writeQue;
        static byte[] map;

        static int read(Stream stream, byte[] buffer, int offset, int count)
        {
            var bytesRead = stream.Read(buffer, offset, count);
            return bytesRead == count ? offset + count
                 : bytesRead == 0 ? offset
                 : read(stream, buffer, offset + bytesRead, count - bytesRead);
        }
        static Stream ReaderStream;
        static void Reader()
        {
            using (var stream = ReaderStream)
            {
                int bytesRead;
                do
                {
                    var buffer = new byte[READER_BUFFER_SIZE];
                    bytesRead = read(stream, buffer, 0, READER_BUFFER_SIZE);
                    readQue.Add(buffer);
                } while (bytesRead == READER_BUFFER_SIZE);
                readQue.CompleteAdding();
            }
        }

        static bool tryTake<T>(BlockingCollection<T> q, out T t) where T : class
        {
            t = null;
            var wait = new SpinWait();
            while (!q.IsCompleted && !q.TryTake(out t)) wait.SpinOnce();
            return t != null;
        }

        static void Grouper()
        {
            // Set up complements map
            map = new byte[256];
            for (byte b = 0; b < 255; b++) map[b] = b;
            map[(byte)'A'] = (byte)'T';
            map[(byte)'B'] = (byte)'V';
            map[(byte)'C'] = (byte)'G';
            map[(byte)'D'] = (byte)'H';
            map[(byte)'G'] = (byte)'C';
            map[(byte)'H'] = (byte)'D';
            map[(byte)'K'] = (byte)'M';
            map[(byte)'M'] = (byte)'K';
            map[(byte)'R'] = (byte)'Y';
            map[(byte)'T'] = (byte)'A';
            map[(byte)'V'] = (byte)'B';
            map[(byte)'Y'] = (byte)'R';
            map[(byte)'a'] = (byte)'T';
            map[(byte)'b'] = (byte)'V';
            map[(byte)'c'] = (byte)'G';
            map[(byte)'d'] = (byte)'H';
            map[(byte)'g'] = (byte)'C';
            map[(byte)'h'] = (byte)'D';
            map[(byte)'k'] = (byte)'M';
            map[(byte)'m'] = (byte)'K';
            map[(byte)'r'] = (byte)'Y';
            map[(byte)'t'] = (byte)'A';
            map[(byte)'v'] = (byte)'B';
            map[(byte)'y'] = (byte)'R';

            var startHeader = 0;
            var i = 0;
            bool afterFirst = false;
            var data = new List<byte[]>();
            byte[] bytes;
            while (tryTake(readQue, out bytes))
            {
                data.Add(bytes);
                while ((i = Array.IndexOf<byte>(bytes, GT, i + 1)) != -1)
                {
                    var sequence = new RevCompSequence
                    {
                        Pages = data,
                        StartHeader = startHeader,
                        EndExclusive = i
                    };
                    if (afterFirst)
                        (sequence.ReverseThread = new Thread(() => Reverse(sequence))).Start();
                    else
                        afterFirst = true;
                    writeQue.Add(sequence);
                    startHeader = i;
                    data = new List<byte[]> { bytes };
                }
            }
            i = Array.IndexOf<byte>(data[data.Count - 1], 0, 0);
            var lastSequence = new RevCompSequence
            {
                Pages = data,
                StartHeader = startHeader,
                EndExclusive = i == -1 ? data[data.Count - 1].Length : i
            };
            Reverse(lastSequence);
            writeQue.Add(lastSequence);
            writeQue.CompleteAdding();
        }

        static void Reverse(RevCompSequence sequence)
        {
            var startPageId = 0;
            var startBytes = sequence.Pages[0];
            var startIndex = sequence.StartHeader;

            // Skip header line
            while ((startIndex = Array.IndexOf<byte>(startBytes, LF, startIndex)) == -1)
            {
                startBytes = sequence.Pages[++startPageId];
                startIndex = 0;
            }

            var endPageId = sequence.Pages.Count - 1;
            var endIndex = sequence.EndExclusive - 1;
            if (endIndex == -1) endIndex = sequence.Pages[--endPageId].Length - 1;
            var endBytes = sequence.Pages[endPageId];

            // Swap in place across pages
            do
            {
                var startByte = startBytes[startIndex];
                if (startByte < SP)
                {
                    if (++startIndex == startBytes.Length)
                    {
                        startBytes = sequence.Pages[++startPageId];
                        startIndex = 0;
                    }
                    if (startIndex == endIndex && startPageId == endPageId) break;
                    startByte = startBytes[startIndex];
                }
                var endByte = endBytes[endIndex];
                if (endByte < SP)
                {
                    if (--endIndex == -1)
                    {
                        endBytes = sequence.Pages[--endPageId];
                        endIndex = endBytes.Length - 1;
                    }
                    if (startIndex == endIndex && startPageId == endPageId) break;
                    endByte = endBytes[endIndex];
                }

                startBytes[startIndex] = map[endByte];
                endBytes[endIndex] = map[startByte];

                if (++startIndex == startBytes.Length)
                {
                    startBytes = sequence.Pages[++startPageId];
                    startIndex = 0;
                }
                if (--endIndex == -1)
                {
                    endBytes = sequence.Pages[--endPageId];
                    endIndex = endBytes.Length - 1;
                }
            } while (startPageId < endPageId || (startPageId == endPageId && startIndex < endIndex));
            if (startIndex == endIndex) startBytes[startIndex] = map[startBytes[startIndex]];
        }

        static Stream WriterStream;
        static void Writer()
        {
            using (var stream = WriterStream)
            {
                bool first = true;
                RevCompSequence sequence;
                while (tryTake(writeQue, out sequence))
                {
                    var startIndex = sequence.StartHeader;
                    var pages = sequence.Pages;
                    if (first)
                    {
                        Reverse(sequence);
                        first = false;
                    }
                    else
                    {
                        sequence.ReverseThread?.Join();
                    }
                    for (int i = 0; i < pages.Count - 1; i++)
                    {
                        var bytes = pages[i];
                        stream.Write(bytes, startIndex, bytes.Length - startIndex);
                        startIndex = 0;
                    }
                    stream.Write(pages[pages.Count - 1], startIndex, sequence.EndExclusive - startIndex);
                }
            }
        }

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
            readQue = new BlockingCollection<byte[]>();
            writeQue = new BlockingCollection<RevCompSequence>();
            ReaderStream = input;
            WriterStream = output;
            new Thread(Reader).Start();
            new Thread(Grouper).Start();
            Writer();
        }
    }
}
