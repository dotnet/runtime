// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace JitTest_lcs_long_arrays_cs
{
    public class LCS
    {
        private const int RANK = 4;

        private static String buildLCS(long[,,,] b, char[] X, long[] ind)
        {
            for (long i = 0; i < RANK; i++)
                if (ind[i] == 0) return "";

            long L = b[ind[0], ind[1], ind[2], ind[3]];
            if (L == RANK)
            {
                for (long i = 0; i < RANK; i++)
                    ind[i]--;
                long idx = ind[0];
                return buildLCS(b, X, ind) + X[idx];
            }
            if (L >= 0 && L < RANK)
            {
                ind[L]--;
                return buildLCS(b, X, ind);
            }
            throw new Exception();
        }

        private static void findLCS(long[,,,] c, long[,,,] b, char[][] seq, long[] len)
        {
            long[] ind = new long[RANK];
            for (ind[0] = 1; ind[0] < len[0]; ind[0]++)
            {
                for (ind[1] = 1; ind[1] < len[1]; ind[1]++)
                {
                    for (ind[2] = 1; ind[2] < len[2]; ind[2]++)
                    {
                        for (ind[3] = 1; ind[3] < len[3]; ind[3]++)
                        {
                            bool eqFlag = true;
                            for (long i = 1; i < RANK; i++)
                            {
                                if (seq[i][ind[i] - 1] != seq[i - 1][ind[i - 1] - 1])
                                {
                                    eqFlag = false;
                                    break;
                                }
                            }

                            if (eqFlag)
                            {
                                c[ind[0], ind[1], ind[2], ind[3]] =
                                    c[ind[0] - 1, ind[1] - 1, ind[2] - 1, ind[3] - 1] + 1;
                                b[ind[0], ind[1], ind[2], ind[3]] = RANK;
                                continue;
                            }

                            long R = -1;
                            long M = -1;
                            for (long i = 0; i < RANK; i++)
                            {
                                ind[i]--;
                                if (c[ind[0], ind[1], ind[2], ind[3]] > M)
                                {
                                    R = i;
                                    M = c[ind[0], ind[1], ind[2], ind[3]];
                                }
                                ind[i]++;
                            }
                            if (R < 0 || M < 0)
                                throw new Exception();

                            c[ind[0], ind[1], ind[2], ind[3]] = M;
                            b[ind[0], ind[1], ind[2], ind[3]] = R;
                        }
                    }
                }
            }
        }

        [Fact]
        public static int TestEntryPoint()
        {
            Console.WriteLine("Test searches for longest common subsequence of 4 strings\n\n");
            String[] str = new String[RANK] {
                "The Sun has left his blackness",
                "and has found a fresher morning",
                "and the fair Moon rejoices",
                "in the clear and cloudless night"
            };

            long[] len = new long[RANK];
            char[][] seq = new char[RANK][];
            for (long i = 0; i < RANK; i++)
            {
                len[i] = str[i].Length + 1;
                seq[i] = str[i].ToCharArray();
            }

            long[,,,] c = new long[(int)len[0], (int)len[1], (int)len[2], (int)len[3]];
            long[,,,] b = new long[(int)len[0], (int)len[1], (int)len[2], (int)len[3]];

            findLCS(c, b, seq, len);

            for (long i = 0; i < RANK; i++)
                len[i]--;

            if ("n ha  es" == buildLCS(b, seq[0], len))
            {
                Console.WriteLine("Test passed");
                return 100;
            }
            else
            {
                Console.WriteLine("Test failed.");
                return 0;
            }
        }
    }
}
