// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace JitTest_lcsvalbox_lcs_cs
{
    internal struct Data
    {
        public int b, c;
    };

    public class LCS
    {
        private const int RANK = 4;

        private static String buildLCS(object[,,,] mtx, char[] X, int[] ind)
        {
            for (int i = 0; i < RANK; i++)
                if (ind[i] == 0) return "";

            int L = ((Data)mtx[ind[0], ind[1], ind[2], ind[3]]).b;
            if (L == RANK)
            {
                for (int i = 0; i < RANK; i++)
                    ind[i]--;
                int idx = ind[0];
                return buildLCS(mtx, X, ind) + X[idx];
            }
            if (L >= 0 && L < RANK)
            {
                ind[L]--;
                return buildLCS(mtx, X, ind);
            }
            throw new Exception();
        }

        private static void findLCS(object[,,,] mtx, char[][] seq, int[] len)
        {
            int[] ind = new int[RANK];
            for (ind[0] = 1; ind[0] < len[0]; ind[0]++)
            {
                for (ind[1] = 1; ind[1] < len[1]; ind[1]++)
                {
                    for (ind[2] = 1; ind[2] < len[2]; ind[2]++)
                    {
                        for (ind[3] = 1; ind[3] < len[3]; ind[3]++)
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
                                Data d;
                                if (mtx[ind[0] - 1, ind[1] - 1, ind[2] - 1, ind[3] - 1] == null)
                                    d.c = 1;
                                else
                                    d.c = ((Data)mtx[ind[0] - 1, ind[1] - 1, ind[2] - 1, ind[3] - 1]).c + 1;
                                d.b = RANK;
                                mtx[ind[0], ind[1], ind[2], ind[3]] = d;
                                continue;
                            }

                            int R = -1;
                            int M = -1;
                            for (int i = 0; i < RANK; i++)
                            {
                                ind[i]--;
                                int cc;
                                try
                                {
                                    if (mtx[ind[0], ind[1], ind[2], ind[3]] == null)
                                        cc = 0;
                                    else
                                        cc = ((Data)mtx[ind[0], ind[1], ind[2], ind[3]]).c;
                                }
                                catch (NullReferenceException)
                                {
                                    cc = 0;
                                }
                                if (cc > M)
                                {
                                    R = i;
                                    M = cc;
                                }
                                ind[i]++;
                            }
                            if (R < 0 || M < 0)
                                throw new Exception();

                            Data d1;
                            d1.c = M;
                            d1.b = R;
                            mtx[ind[0], ind[1], ind[2], ind[3]] = d1;
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

            int[] len = new int[RANK];
            char[][] seq = new char[RANK][];
            for (int i = 0; i < RANK; i++)
            {
                len[i] = str[i].Length + 1;
                seq[i] = str[i].ToCharArray();
            }

            object[,,,] mtx = new object[len[0], len[1], len[2], len[3]];

            findLCS(mtx, seq, len);

            for (int i = 0; i < RANK; i++)
                len[i]--;

            if ("n ha  es" == buildLCS(mtx, seq[0], len))
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
