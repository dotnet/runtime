// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace JitTest_lcs_refany_cs
{
    public class LCS
    {
        private const int RANK = 4;

        private static String buildLCS( /*int[,,,]*/TypedReference _b,
                                /*char[]*/ TypedReference _X,
                                /*int[]*/ TypedReference _ind)
        {
            int _i = 0;
            for (TypedReference i = __makeref(_i);
                __refvalue(i, int) < RANK; _i++)
                if (__refvalue(_ind, int[])[__refvalue(i, int)] == 0) return "";

            int L = __refvalue(_b, int[,,,])[
                        __refvalue(_ind, int[])[0],
                        __refvalue(_ind, int[])[1],
                        __refvalue(_ind, int[])[2],
                        __refvalue(_ind, int[])[3]];

            if (L == RANK)
            {
                _i = 0;
                for (TypedReference i = __makeref(_i);
                    __refvalue(i, int) < RANK; _i++)
                    __refvalue(_ind, int[])[__refvalue(i, int)]--;
                int idx = __refvalue(_ind, int[])[0];
                return buildLCS(_b, _X, _ind) + __refvalue(_X, char[])[idx];
            }
            if (L >= 0 && L < RANK)
            {
                __refvalue(_ind, int[])[L]--;
                return buildLCS(_b, _X, _ind);
            }
            throw new Exception();
        }

        private static void findLCS(    /*int[,,,]*/ TypedReference _c,
                                /*int[,,,]*/ TypedReference _b,
                                /*char[][]*/ TypedReference _seq,
                                /*int[]*/ TypedReference _len)
        {
            int[] ind = new int[RANK];
            for (ind[0] = 1; ind[0] < __refvalue(_len, int[])[0]; ind[0]++)
            {
                for (ind[1] = 1; ind[1] < __refvalue(_len, int[])[1]; ind[1]++)
                {
                    for (ind[2] = 1; ind[2] < __refvalue(_len, int[])[2]; ind[2]++)
                    {
                        for (ind[3] = 1; ind[3] < __refvalue(_len, int[])[3]; ind[3]++)
                        {
                            bool eqFlag = true;
                            for (int i = 1; i < RANK; i++)
                            {
                                if (__refvalue(_seq, char[][])[i][ind[i] - 1] !=
                                    __refvalue(_seq, char[][])[i - 1][ind[i - 1] - 1])
                                {
                                    eqFlag = false;
                                    break;
                                }
                            }

                            if (eqFlag)
                            {
                                __refvalue(_c, int[,,,])[ind[0], ind[1], ind[2], ind[3]] =
                                    __refvalue(_c, int[,,,])[ind[0] - 1, ind[1] - 1, ind[2] - 1, ind[3] - 1] + 1;
                                __refvalue(_b, int[,,,])[ind[0], ind[1], ind[2], ind[3]] = RANK;
                                continue;
                            }

                            int R = -1;
                            int M = -1;
                            for (int i = 0; i < RANK; i++)
                            {
                                ind[i]--;
                                if (__refvalue(_c, int[,,,])[ind[0], ind[1], ind[2], ind[3]] > M)
                                {
                                    R = i;
                                    M = __refvalue(_c, int[,,,])[ind[0], ind[1], ind[2], ind[3]];
                                }
                                ind[i]++;
                            }
                            if (R < 0 || M < 0)
                                throw new Exception();

                            __refvalue(_c, int[,,,])[ind[0], ind[1], ind[2], ind[3]] = M;
                            __refvalue(_b, int[,,,])[ind[0], ind[1], ind[2], ind[3]] = R;
                        }
                    }
                }
            }
        }

        [Fact]
        public static int TestEntryPoint()
        {
            String[] str = new String[RANK] {
                "The Sun has left",
                "his blackness and",
                "has found a fresher",
                "morning and the fair Moon"
            };

            int[] len = new int[RANK];
            char[][] seq = new char[RANK][];
            for (int i = 0; i < RANK; i++)
            {
                len[i] = str[i].Length + 1;
                seq[i] = str[i].ToCharArray();
            }

            int[,,,] c = new int[len[0], len[1], len[2], len[3]];
            int[,,,] b = new int[len[0], len[1], len[2], len[3]];

            findLCS(__makeref(c), __makeref(b), __makeref(seq), __makeref(len));

            for (int i = 0; i < RANK; i++)
                len[i]--;

            String s = buildLCS(__makeref(b), __makeref(seq[0]), __makeref(len));
            if (" n a" == s)
            {
                return 100;
            }
            else
            {
                return 1;
            }
        }
    }
}
