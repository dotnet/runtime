// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Adapted from pidigits C# .NET Core #3 program
// http://benchmarksgame.alioth.debian.org/u64q/program.php?test=pidigits&lang=csharpcore&id=3
// aka (as of 2017-09-01) rev 1.2 of https://alioth.debian.org/scm/viewvc.php/benchmarksgame/bench/pidigits/pidigits.csharp-3.csharp?root=benchmarksgame&view=log
// Best-scoring C# .NET Core version as of 2017-09-01
// (also best-scoring single-threaded C# .NET Core version as of 2017-09-01)
// **** Version #3 on website pinvokes to native GMP library; this has been modified to
//      use .NET's System.Numerics.BigInteger type instead ****

/* The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/
 *
 * Port of the Java port that uses native GMP to use native GMP with C#
 * contributed by Miguel de Icaza, based on the Java version, that was:
 * 	contributed by Mike Pall
 * 	java port by Stefan Krause
*/
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using Xunit;

namespace BenchmarksGame
{
    public class pidigits
    {
        BigInteger q = new BigInteger(), r = new BigInteger(), s = new BigInteger(), t = new BigInteger();
        BigInteger u = new BigInteger(), v = new BigInteger(), w = new BigInteger();

        int i;
        StringBuilder strBuf = new StringBuilder(40), lastBuf = null;
        int n;

        pidigits(int n)
        {
            this.n = n;
        }

        private void compose_r(int bq, int br, int bs, int bt)
        {
            u = r * bs;
            r *= bq;
            v = t * br;
            r += v;
            t *= bt;
            t += u;
            s *= bt;
            u = q * bs;
            s += u;
            q *= bq;
        }

        /* Compose matrix with numbers on the left. */
        private void compose_l(int bq, int br, int bs, int bt)
        {
            r *= bt;
            u = q * br;
            r += u;
            u = t * bs;
            t *= bt;
            v = s * br;
            t += v;
            s *= bq;
            s += u;
            q *= bq;
        }

        /* Extract one digit. */
        private int extract(int j)
        {
            u = q * j;
            u += r;
            v = s * j;
            v += t;
            w = u / v;
            return (int)w;
        }

        /* Print one digit. Returns 1 for the last digit. */
        private bool prdigit(int y, bool verbose)
        {
            strBuf.Append(y);
            if (++i % 10 == 0 || i == n)
            {
                if (i % 10 != 0)
                    for (int j = 10 - (i % 10); j > 0; j--)
                    { strBuf.Append(" "); }
                strBuf.Append("\t:");
                strBuf.Append(i);
                if (verbose) Console.WriteLine(strBuf);
                lastBuf = strBuf;
                strBuf = new StringBuilder(40);
            }
            return i == n;
        }

        /* Generate successive digits of PI. */
        void Run(bool verbose)
        {
            int k = 1;
            i = 0;
            q = 1;
            r = 0;
            s = 0;
            t = 1;
            for (; ; )
            {
                int y = extract(3);
                if (y == extract(4))
                {
                    if (prdigit(y, verbose))
                        return;
                    compose_r(10, -10 * y, 0, 1);
                }
                else
                {
                    compose_l(k, 4 * k + 2, 0, 2 * k + 1);
                    k++;
                }
            }
        }

        [Fact]
        public static int TestEntryPoint()
        {
            return Test(null);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int Test(int? arg)
        {
            int n = arg ?? 10;
            string result = Bench(n, true).ToString();
            if (result != "3141592653\t:10")
            {
                return -1;
            }
            return 100;
        }

        public static StringBuilder Bench(int n, bool verbose)
        {
            pidigits m = new pidigits(n);
            m.Run(verbose);
            return m.lastBuf;
        }
    }
}
