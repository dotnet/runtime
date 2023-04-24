// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace JitTest_lcs_gcref_port
{
    internal class LCSO
    {
        private LCSO _m_child;
        public LCSO(LCSO child) { _m_child = child; }
    }

    public struct LCSV
    {
        private int _v;
        private LCSO[] _gcref;
        private const int RANK = 4;

        private static String buildLCS(LCSV[,,,] b, char[] X, LCSV[] ind)
        {
            for (int i = 0; i < RANK; i++)
                if (ind[i]._v == 0) return "";

            LCSV L = b[ind[0]._v, ind[1]._v, ind[2]._v, ind[3]._v];
            LCSV Z;
            Z._v = 0;
            Z._gcref = new LCSO[] { new LCSO(new LCSO(null)) };
            if (L._v == RANK)
            {
                for (LCSV i = Z; i._v < RANK; i._v++)
                    ind[i._v]._v--;
                LCSV idx = ind[0];
                return buildLCS(b, X, ind) + X[idx._v];
            }
            if (L._v >= 0 && L._v < RANK)
            {
                ind[L._v]._v--;
                ind[L._v]._gcref = new LCSO[] { new LCSO(null) };
                return buildLCS(b, X, ind);
            }
            throw new Exception();
        }

        private static void findLCS(LCSV[,,,] c, LCSV[,,,] b, char[][] seq, LCSV[] len)
        {
            LCSV[] ind = new LCSV[RANK];
            for (ind[0]._v = 1; ind[0]._v < len[0]._v; ind[0]._v++)
            {
                for (ind[1]._v = 1; ind[1]._v < len[1]._v; ind[1]._v++)
                {
                    for (ind[2]._v = 1; ind[2]._v < len[2]._v; ind[2]._v++)
                    {
                        ind[2]._gcref = new LCSO[] { new LCSO(null) };
                        for (ind[3]._v = 1; ind[3]._v < len[3]._v; ind[3]._v++)
                        {
                            bool eqFlag = true;
                            for (int i = 1; i < RANK; i++)
                            {
                                if (seq[i][ind[i]._v - 1] != seq[i - 1][ind[i - 1]._v - 1])
                                {
                                    eqFlag = false;
                                    break;
                                }
                            }

                            if (eqFlag)
                            {
                                c[ind[0]._v, ind[1]._v, ind[2]._v, ind[3]._v]._v =
                                    c[ind[0]._v - 1, ind[1]._v - 1, ind[2]._v - 1, ind[3]._v - 1]._v + 1;
                                b[ind[0]._v, ind[1]._v, ind[2]._v, ind[3]._v]._v = RANK;
                                continue;
                            }

                            LCSV R, M, Z;
                            Z._v = 0;
                            R._v = M._v = -1;
                            Z._gcref = R._gcref = M._gcref = null;

                            for (LCSV i = Z; i._v < RANK; i._v++)
                            {
                                ind[i._v]._v--;
                                if (c[ind[0]._v, ind[1]._v, ind[2]._v, ind[3]._v]._v > M._v)
                                {
                                    R = i;
                                    M = c[ind[0]._v, ind[1]._v, ind[2]._v, ind[3]._v];
                                }
                                ind[i._v]._v++;
                            }
                            if (R._v < 0 || M._v < 0)
                                throw new Exception();

                            c[ind[0]._v, ind[1]._v, ind[2]._v, ind[3]._v] = M;
                            b[ind[0]._v, ind[1]._v, ind[2]._v, ind[3]._v] = R;
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

            LCSV[] len = new LCSV[RANK];
            char[][] seq = new char[RANK][];
            for (int i = 0; i < RANK; i++)
            {
                len[i]._v = str[i].Length + 1;
                seq[i] = str[i].ToCharArray();
            }

            LCSV[,,,] c = new LCSV[len[0]._v, len[1]._v, len[2]._v, len[3]._v];
            LCSV[,,,] b = new LCSV[len[0]._v, len[1]._v, len[2]._v, len[3]._v];

            findLCS(c, b, seq, len);

            for (int i = 0; i < RANK; i++)
                len[i]._v--;

            if ("n ha  es" == buildLCS(b, seq[0], len))
            {
                Console.WriteLine("Test passed");
                return 100;
            }
            else
            {
                Console.WriteLine("Test failed.");
                return 1;
            }
        }
    }
}
