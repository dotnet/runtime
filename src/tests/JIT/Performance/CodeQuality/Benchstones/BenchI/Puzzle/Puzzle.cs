// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Benchstone.BenchI
{
public class Puzzle
{
#if DEBUG
    public const int Iterations = 1;
#else
    public const int Iterations = 400;
#endif

    private const int PuzzleSize = 511;
    private const int ClassMax = 3;
    private const int TypeMax = 12;
    private const int D = 8;

    private int[] _pieceCount = new int[ClassMax + 1];
    private int[] _class = new int[TypeMax + 1];
    private int[] _pieceMax = new int[TypeMax + 1];
    private bool[] _puzzle = new bool[PuzzleSize + 1];
    private bool[][] _p;
    private int _count;

    private static T[][] AllocArray<T>(int n1, int n2)
    {
        T[][] a = new T[n1][];
        for (int i = 0; i < n1; ++i)
        {
            a[i] = new T[n2];
        }

        return a;
    }

    private bool Fit(int i, int j)
    {
        for (int k = 0; k <= _pieceMax[i]; k++)
        {
            if (_p[i][k])
            {
                if (_puzzle[j + k])
                {
                    return false;
                }
            }
        }

        return true;
    }

    private int Place(int i, int j)
    {
        int k;
        for (k = 0; k <= _pieceMax[i]; k++)
        {
            if (_p[i][k])
            {
                _puzzle[j + k] = true;
            }
        }

        _pieceCount[_class[i]] = _pieceCount[_class[i]] - 1;

        for (k = j; k <= PuzzleSize; k++)
        {
            if (!_puzzle[k])
            {
                return k;
            }
        }

        return 0;
    }

    private void RemoveLocal(int i, int j)
    {
        for (int k = 0; k <= _pieceMax[i]; k++)
        {
            if (_p[i][k])
            {
                _puzzle[j + k] = false;
            }
        }

        _pieceCount[_class[i]] = _pieceCount[_class[i]] + 1;
    }

    private bool Trial(int j)
    {
        for (int i = 0; i <= TypeMax; i++)
        {
            if (_pieceCount[_class[i]] != 0)
            {
                if (Fit(i, j))
                {
                    int k = Place(i, j);
                    if (Trial(k) || (k == 0))
                    {
                        _count = _count + 1;
                        return true;
                    }
                    else
                    {
                        RemoveLocal(i, j);
                    }
                }
            }
        }

        _count = _count + 1;
        return false;
    }

    private bool DoIt()
    {
        int i, j, k, m, n;

        for (m = 0; m <= PuzzleSize; m++)
        {
            _puzzle[m] = true;
        }

        for (i = 1; i <= 5; i++)
        {
            for (j = 1; j <= 5; j++)
            {
                for (k = 1; k <= 5; k++)
                {
                    _puzzle[i + D * (j + D * k)] = false;
                }
            }
        }

        for (i = 0; i <= TypeMax; i++)
        {
            for (m = 0; m <= PuzzleSize; m++)
            {
                _p[i][m] = false;
            }
        }

        for (i = 0; i <= 3; i++)
        {
            for (j = 0; j <= 1; j++)
            {
                for (k = 0; k <= 0; k++)
                {
                    _p[0][i + D * (j + D * k)] = true;
                }
            }
        }

        _class[0] = 0;
        _pieceMax[0] = 3 + D * 1 + D * D * 0;

        for (i = 0; i <= 1; i++)
        {
            for (j = 0; j <= 0; j++)
            {
                for (k = 0; k <= 3; k++)
                {
                    _p[1][i + D * (j + D * k)] = true;
                }
            }
        }

        _class[1] = 0;
        _pieceMax[1] = 1 + D * 0 + D * D * 3;

        for (i = 0; i <= 0; i++)
        {
            for (j = 0; j <= 3; j++)
            {
                for (k = 0; k <= 1; k++)
                {
                    _p[2][i + D * (j + D * k)] = true;
                }
            }
        }
        _class[2] = 0;
        _pieceMax[2] = 0 + D * 3 + D * D * 1;

        for (i = 0; i <= 1; i++)
        {
            for (j = 0; j <= 3; j++)
            {
                for (k = 0; k <= 0; k++)
                {
                    _p[3][i + D * (j + D * k)] = true;
                }
            }
        }

        _class[3] = 0;
        _pieceMax[3] = 1 + D * 3 + D * D * 0;

        for (i = 0; i <= 3; i++)
        {
            for (j = 0; j <= 0; j++)
            {
                for (k = 0; k <= 1; k++)
                {
                    _p[4][i + D * (j + D * k)] = true;
                }
            }
        }

        _class[4] = 0;
        _pieceMax[4] = 3 + D * 0 + D * D * 1;

        for (i = 0; i <= 0; i++)
        {
            for (j = 0; j <= 1; j++)
            {
                for (k = 0; k <= 3; k++)
                {
                    _p[5][i + D * (j + D * k)] = true;
                }
            }
        }

        _class[5] = 0;
        _pieceMax[5] = 0 + D * 1 + D * D * 3;

        for (i = 0; i <= 2; i++)
        {
            for (j = 0; j <= 0; j++)
            {
                for (k = 0; k <= 0; k++)
                {
                    _p[6][i + D * (j + D * k)] = true;
                }
            }
        }

        _class[6] = 1;
        _pieceMax[6] = 2 + D * 0 + D * D * 0;

        for (i = 0; i <= 0; i++)
        {
            for (j = 0; j <= 2; j++)
            {
                for (k = 0; k <= 0; k++)
                {
                    _p[7][i + D * (j + D * k)] = true;
                }
            }
        }

        _class[7] = 1;
        _pieceMax[7] = 0 + D * 2 + D * D * 0;

        for (i = 0; i <= 0; i++)
        {
            for (j = 0; j <= 0; j++)
            {
                for (k = 0; k <= 2; k++)
                {
                    _p[8][i + D * (j + D * k)] = true;
                }
            }
        }

        _class[8] = 1;
        _pieceMax[8] = 0 + D * 0 + D * D * 2;

        for (i = 0; i <= 1; i++)
        {
            for (j = 0; j <= 1; j++)
            {
                for (k = 0; k <= 0; k++)
                {
                    _p[9][i + D * (j + D * k)] = true;
                }
            }
        }
        _class[9] = 2;
        _pieceMax[9] = 1 + D * 1 + D * D * 0;

        for (i = 0; i <= 1; i++)
        {
            for (j = 0; j <= 0; j++)
            {
                for (k = 0; k <= 1; k++)
                {
                    _p[10][i + D * (j + D * k)] = true;
                }
            }
        }

        _class[10] = 2;
        _pieceMax[10] = 1 + D * 0 + D * D * 1;

        for (i = 0; i <= 0; i++)
        {
            for (j = 0; j <= 1; j++)
            {
                for (k = 0; k <= 1; k++)
                {
                    _p[11][i + D * (j + D * k)] = true;
                }
            }
        }

        _class[11] = 2;
        _pieceMax[11] = 0 + D * 1 + D * D * 1;

        for (i = 0; i <= 1; i++)
        {
            for (j = 0; j <= 1; j++)
            {
                for (k = 0; k <= 1; k++)
                {
                    _p[12][i + D * (j + D * k)] = true;
                }
            }
        }

        _class[12] = 3;
        _pieceMax[12] = 1 + D * 1 + D * D * 1;
        _pieceCount[0] = 13;
        _pieceCount[1] = 3;
        _pieceCount[2] = 1;
        _pieceCount[3] = 1;
        m = 1 + D * (1 + D * 1);
        _count = 0;

        bool result = true;

        if (Fit(0, m))
        {
            n = Place(0, m);
            result = Trial(n);
        }
        else
        {
            result = false;
        }

        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool Bench()
    {
        _p = AllocArray<bool>(TypeMax + 1, PuzzleSize + 1);

        bool result = true;

        for (int i = 0; i < Iterations; ++i)
        {
            result &= DoIt();
        }

        return result;
    }

    private static bool TestBase()
    {
        Puzzle P = new Puzzle();
        bool result = P.Bench();
        return result;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        bool result = TestBase();
        return (result ? 100 : -1);
    }
}
}
