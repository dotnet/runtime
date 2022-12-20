// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace KnightMove_knight_cs
{
    internal struct MV
    {
        public int x, y;
        public int v;
    }
    public struct SQ
    {
        public int visited;

        [Fact]
        public static int TestEntryPoint()
        {
            const int SIZE = 5;
            const int VARNUM = 8;
            const int STARTX = 0;
            const int STARTY = 0;
            SQ[,] sq = new SQ[SIZE, SIZE];
            int[] xt = new int[] { 1, 1, -1, -1, 2, 2, -2, -2 };
            int[] yt = new int[] { 2, -2, 2, -2, 1, -1, 1, -1 };

            MV[] mv = new MV[SIZE * SIZE];
            sq[STARTX, STARTY].visited = 1;
            mv[0].x = STARTX;
            mv[0].y = STARTY;
            int i = 1;
            while (true)
            {
                if (mv[i].v >= VARNUM)
                {
                    i--;
                    if (i == 0)
                    {
                        return 1;
                    }
                    if (sq[mv[i].x, mv[i].y].visited != i + 1)
                        throw new Exception();
                    sq[mv[i].x, mv[i].y].visited = 0;
                    mv[i].v++;
                    continue;
                }
                int nx = mv[i - 1].x + xt[mv[i].v];
                int ny = mv[i - 1].y + yt[mv[i].v];
                if (nx < 0 || nx >= SIZE || ny < 0 || ny >= SIZE || sq[nx, ny].visited != 0)
                {
                    mv[i].v++;
                    continue;
                }
                mv[i].x = nx;
                mv[i++].y = ny;
                sq[nx, ny].visited = i;
                if (i == SIZE * SIZE)
                {
                    for (int x = 0; x < SIZE; x++)
                    {
                        for (int y = 0; y < SIZE; y++)
                        {
                            String n = sq[x, y].visited.ToString();
                            n += n.Length == 1 ? "  " : " ";
                        }
                    }
                    return 100;
                }
                mv[i].v = 0;
            }
        }
    }
}
