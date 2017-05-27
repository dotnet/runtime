/* The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/
 *
 * submitted by Josh Goldfoot
 *
 */

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xunit.Performance;
using Xunit;

[assembly: OptimizeForBenchmarks]
[assembly: MeasureGCCounts]

namespace BenchmarksGame
{

public class knucleotide
{
#if DEBUG
    const int Iterations = 1;
    const string InputFile = "knucleotide-input.txt";
    static int[] expectedCountLetter = new int[] { 1480, 974, 970, 1576 };
    static int[] expectedCountPairs = new int[] { 420, 272, 292, 496, 273, 202, 201, 298, 316, 185, 167, 302, 470, 315, 310, 480 };
    static int[] expectedCountFragments = new int[] { 54, 24, 4, 0, 0 };
#else
    const int Iterations = 10;
    const string InputFile = "knucleotide-input-big.txt";
    static int[] expectedCountLetter = new int[] { 302923, 198136, 197566, 301375 };
    static int[] expectedCountPairs = new int[] { 91779, 60030, 59889, 91225, 60096, 39203, 39081, 59756, 59795, 39190, 39023, 59557, 91253, 59713, 59572, 90837 };
    static int[] expectedCountFragments = new int[] { 11765, 3572, 380, 7, 7 };
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
           "CodeQuality", "BenchmarksGame", "k-nucleotide", "k-nucleotide", inputFile
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
        int iterations = Iterations;

        string inputFile = FindInput(InputFile);
        if (inputFile == null)
        {
            throw new Exception("unable to find input");
        }

        PrepareLookups();
        var source = new FileStream(inputFile, FileMode.Open);
        var buffer = GetBytesForThirdSequence(source);
        var fragmentLengths = new[] { 1, 2, 3, 4, 6, 12, 18 };
        var dicts =
            (from fragmentLength in fragmentLengths.AsParallel()
             select CountFrequency(buffer, fragmentLength)).ToArray();
        source.Dispose();
        int res = 100;
        for (ulong i = 0; i < 4; ++i){
            if (dicts[0][i].V != expectedCountLetter[i]){
                res = -1;
            }
        }
         for (ulong i = 0; i < 16; ++i){
            if (dicts[1][i].V != expectedCountPairs[i]){
                res = -1;
            }
        }
        int buflen = dicts[0].Values.Sum(x => x.V);
        WriteFrequencies(dicts[0], buflen, 1);
        WriteFrequencies(dicts[1], buflen, 2);
        if (WriteCount(dicts[2], "GGT") != expectedCountFragments[0]) { res = -1; }
        if (WriteCount(dicts[3], "GGTA") != expectedCountFragments[1]) { res = -1; }
        if (WriteCount(dicts[4], "GGTATT") != expectedCountFragments[2]) { res = -1; }
        if (WriteCount(dicts[5], "GGTATTTTAATT") != expectedCountFragments[3]) { res = -1; }
        if (WriteCount(dicts[6], "GGTATTTTAATTTATAGT") != expectedCountFragments[4]) { res = -1; }
        //Console.ReadKey();
        return res;
    }

    private static void WriteFrequencies(Dictionary<ulong, Wrapper> freq, int buflen, int fragmentLength)
    {

        double percent = 100.0 / (buflen - fragmentLength + 1);
        foreach (var line in (from k in freq.Keys
                              orderby freq[k].V descending
                              select string.Format("{0} {1:f3}", PrintKey(k, fragmentLength),
                                (freq.ContainsKey(k) ? freq[k].V : 0) * percent)))
            Console.WriteLine(line);
        Console.WriteLine();
    }

    private static int WriteCount(Dictionary<ulong, Wrapper> dictionary, string fragment)
    {
        ulong key = 0;
        var keybytes = Encoding.ASCII.GetBytes(fragment.ToLower());
        for (int i = 0; i < keybytes.Length; i++)
        {
            key <<= 2;
            key |= tonum[keybytes[i]];
        }
        Wrapper w;
        int count = dictionary.TryGetValue(key, out w) ? w.V : 0;
        Console.WriteLine("{0}\t{1}",
            count,
            fragment);
        return count;
    }

    private static string PrintKey(ulong key, int fragmentLength)
    {
        char[] items = new char[fragmentLength];
        for (int i = 0; i < fragmentLength; ++i)
        {
            items[fragmentLength - i - 1] = tochar[key & 0x3];
            key >>= 2;
        }
        return new string(items);
    }

    private static Dictionary<ulong, Wrapper> CountFrequency(byte[] buffer, int fragmentLength)
    {
        var dictionary = new Dictionary<ulong, Wrapper>();
        ulong rollingKey = 0;
        ulong mask = 0;
        int cursor;
        for (cursor = 0; cursor < fragmentLength - 1; cursor++)
        {
            rollingKey <<= 2;
            rollingKey |= tonum[buffer[cursor]];
            mask = (mask << 2) + 3;
        }
        mask = (mask << 2) + 3;
        int stop = buffer.Length;
        Wrapper w;
        byte cursorByte;
        while (cursor < stop)
        {
            if ((cursorByte = buffer[cursor++]) < (byte)'a')
                cursorByte = buffer[cursor++];
            rollingKey = ((rollingKey << 2) & mask) | tonum[cursorByte];
            if (dictionary.TryGetValue(rollingKey, out w))
                w.V++;
            else
                dictionary.Add(rollingKey, new Wrapper(1));
        }
        return dictionary;
    }

    private static byte[] GetBytesForThirdSequence(FileStream source)
    {
        const int buffersize = 2500120;
        byte[] threebuffer = null;
        var buffer = new byte[buffersize];
        int amountRead, threebuflen, indexOfFirstByteInThreeSequence, indexOfGreaterThan, threepos, tocopy;
        amountRead = threebuflen = indexOfFirstByteInThreeSequence = indexOfGreaterThan = threepos = tocopy = 0;
        bool threeFound = false;
        //var source = new FileStream(inputFile, FileMode.Open);
        source.Seek(0, SeekOrigin.Begin);
        while (!threeFound && (amountRead = source.Read(buffer, 0, buffersize)) > 0)
        {
            indexOfGreaterThan = Array.LastIndexOf(buffer, (byte)'>');
            threeFound = (indexOfGreaterThan > -1 &&
                buffer[indexOfGreaterThan + 1] == (byte)'T' &&
                buffer[indexOfGreaterThan + 2] == (byte)'H');
            if (threeFound)
            {
                threepos += indexOfGreaterThan;
                threebuflen = threepos - 48;
                threebuffer = new byte[threebuflen];
                indexOfFirstByteInThreeSequence = Array.IndexOf<byte>(buffer, 10, indexOfGreaterThan) + 1;
                tocopy = amountRead - indexOfFirstByteInThreeSequence;
                if (amountRead < buffersize)
                    tocopy -= 1;
                Buffer.BlockCopy(buffer, indexOfFirstByteInThreeSequence, threebuffer, 0, tocopy);
                buffer = null;
            }
            else
                threepos += amountRead;
        }
        int toread = threebuflen - tocopy;
        source.Read(threebuffer, tocopy, toread);
        return threebuffer;
    }

    private static byte[] tonum = new byte[256];
    private static char[] tochar = new char[4];
    private static void PrepareLookups()
    {
        tonum['a'] = 0;
        tonum['c'] = 1;
        tonum['g'] = 2;
        tonum['t'] = 3;
        tochar[0] = 'A';
        tochar[1] = 'C';
        tochar[2] = 'G';
        tochar[3] = 'T';
    }

    [Benchmark(InnerIterationCount=Iterations)]
    public static void Bench_Parallel()
    {
        PrepareLookups();
        string inputFile = FindInput(InputFile);
        var source = new FileStream(inputFile, FileMode.Open);

        if (inputFile == null)
        {
            throw new Exception("unable to find input");
        }
        foreach (var iteration in Benchmark.Iterations)
        {
            using (iteration.StartMeasurement())
            {
                for (int i = 0; i < Benchmark.InnerIterationCount; ++i)
                {
                    var buffer = GetBytesForThirdSequence(source);
                    var fragmentLengths = new[] { 1, 2, 3, 4, 6, 12, 18 };
                    var dicts =
                        (from fragmentLength in fragmentLengths.AsParallel()
                         select CountFrequency(buffer, fragmentLength)).ToArray();
                }
            }
        }
        source.Dispose();
    }
    [Benchmark(InnerIterationCount=Iterations)]
    public static void Bench_No_Parallel()
    {
        string inputFile = FindInput(InputFile);
        var source = new FileStream(inputFile, FileMode.Open);

        if (inputFile == null)
        {
            throw new Exception("unable to find input");
        }
        foreach (var iteration in Benchmark.Iterations)
        {
            using (iteration.StartMeasurement())
            {
                for (int i = 0; i < Benchmark.InnerIterationCount; ++i)
                {
                    PrepareLookups();
                    var buffer = GetBytesForThirdSequence(source);
                    var fragmentLengths = new[] { 1, 2, 3, 4, 6, 12, 18 };
                    var dicts =
                        (from fragmentLength in fragmentLengths
                         select CountFrequency(buffer, fragmentLength)).ToArray();
                }
            }
        }
        source.Dispose();
    }
}

public class Wrapper
{
    public int V;
    public Wrapper(int v) { V = v; }
}

}