// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace JitTest_lcsmixed_lcs_cs
{
    public class LCS
    {
        private const int RANK = 8;

        private static String buildLCS(int[,][,][,][,] b, char[] X, int[] ind)
        {
            for (int i = 0; i < RANK; i++)
                if (ind[i] == 0) return "";

            int L = b[ind[0], ind[1]][ind[2], ind[3]][ind[4], ind[5]][ind[6], ind[7]];
            if (L == RANK)
            {
                for (int i = 0; i < RANK; i++)
                    ind[i]--;
                int idx = ind[0];
                return buildLCS(b, X, ind) + X[idx];
            }
            if (L >= 0 && L < RANK)
            {
                ind[L]--;
                return buildLCS(b, X, ind);
            }
            throw new Exception();
        }

        private static void findLCS(int[,,,][,,,] c, int[,][,][,][,] b, char[][] seq, int[] len)
        {
            int[] ind = new int[RANK];
            for (int i = 0; i < RANK; i++)
                ind[i] = 1;

            int R = 0;
            while (R < RANK)
            {
                bool eqFlag = true;
                for (int i = 1; i < RANK; i++)
                {
                    if (seq[i][ind[i] - 1] != seq[i - 1][ind[i - 1] - 1])
                    {
                        eqFlag = false;
                        break;
                    }
                }

                if (eqFlag)
                {
                    c[ind[0], ind[1], ind[2], ind[3]][ind[4], ind[5], ind[6], ind[7]] =
                        c[ind[0] - 1, ind[1] - 1, ind[2] - 1, ind[3] - 1]
                            [ind[4] - 1, ind[5] - 1, ind[6] - 1, ind[7] - 1] + 1;
                    b[ind[0], ind[1]][ind[2], ind[3]][ind[4], ind[5]][ind[6], ind[7]] = RANK;
                }
                else
                {
                    R = -1;
                    int M = -1;
                    for (int i = 0; i < RANK; i++)
                    {
                        ind[i]--;
                        if (c[ind[0], ind[1], ind[2], ind[3]][ind[4], ind[5], ind[6], ind[7]] > M)
                        {
                            R = i;
                            M = c[ind[0], ind[1], ind[2], ind[3]][ind[4], ind[5], ind[6], ind[7]];
                        }
                        ind[i]++;
                    }
                    if (R < 0 || M < 0)
                        throw new Exception();

                    c[ind[0], ind[1], ind[2], ind[3]][ind[4], ind[5], ind[6], ind[7]] = M;
                    b[ind[0], ind[1]][ind[2], ind[3]][ind[4], ind[5]][ind[6], ind[7]] = R;
                }

                R = 0;
                while (R < RANK)
                {
                    ind[R]++;
                    if (ind[R] < len[R]) break;
                    ind[R++] = 1;
                }
            }
        }

        [Fact]
        public static int TestEntryPoint()
        {
            Console.WriteLine("Test searches for longest common subsequence of 8 strings\n\n");
            String[] str = {
                "abdc",
                "badc",
                "bdacw",
                "bdca",
                "bcfdc",
                "bddsc",
                "bdccca",
                "bbdc"
            };

            int[] len = new int[RANK];
            char[][] seq = new char[RANK][];
            for (int i = 0; i < RANK; i++)
            {
                len[i] = str[i].Length + 1;
                seq[i] = str[i].ToCharArray();
            }

            //	allocate weird matrices
            int[,,,][,,,] c = new int[len[0], len[1], len[2], len[3]][,,,];
            int[,][,][,][,] b = new int[len[0], len[1]][,][,][,];
            int[] ind = new int[RANK];
            for (ind[0] = 0; ind[0] < len[0]; ind[0]++)
            {
                for (ind[1] = 0; ind[1] < len[1]; ind[1]++)
                {
                    b[ind[0], ind[1]] = new int[len[2], len[3]][,][,];
                    for (ind[2] = 0; ind[2] < len[2]; ind[2]++)
                    {
                        for (ind[3] = 0; ind[3] < len[3]; ind[3]++)
                        {
                            b[ind[0], ind[1]][ind[2], ind[3]] = new int[len[4], len[5]][,];
                            c[ind[0], ind[1], ind[2], ind[3]] = new int[len[4], len[5], len[6], len[7]];
                            for (ind[4] = 0; ind[4] < len[4]; ind[4]++)
                            {
                                for (ind[5] = 0; ind[5] < len[5]; ind[5]++)
                                    b[ind[0], ind[1]][ind[2], ind[3]][ind[4], ind[5]] = new int[len[6], len[7]];
                            }
                        }
                    }
                }
            }

            findLCS(c, b, seq, len);

            for (int i = 0; i < RANK; i++)
                len[i]--;

            if ("bdc" == buildLCS(b, seq[0], len))
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
