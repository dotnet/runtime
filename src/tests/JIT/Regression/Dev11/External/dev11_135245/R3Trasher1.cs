// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace R3Trasher1
{


    internal struct CHESS_POSITION
    {
        internal ulong occupied_rl45;
    };


    public class Board
    {
        private ulong[,] _bishop_attacks_rl45 = new ulong[64, 256];
        private int[] _bishop_shift_rl45 = new int[64];
        private CHESS_POSITION _search;

        public const int SqValueToUse = 48;
        public const ulong ExpectedResult = 0x2030;

        public Board()
        {
            int columnIndex;
            int index;
            int rowIndex;




            _search.occupied_rl45 = 0x0030000000000000UL;


            for (index = 0; index < 64; index++)
            {
                _bishop_shift_rl45[index] = index;
            }


            for (rowIndex = 0; rowIndex < 64; rowIndex++)
            {
                for (columnIndex = 0; columnIndex < 256; columnIndex++)
                {
                    _bishop_attacks_rl45[rowIndex, columnIndex] = (ulong)(0x2000 + columnIndex);
                }
            }

            return;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public ulong AttacksDiaga1(int sq)
        {
            return _bishop_attacks_rl45[sq, (int)((_search.occupied_rl45 >> _bishop_shift_rl45[sq]) & 255)];
        }
    }


    public static class App
    {
        [Fact]
        public static int TestEntryPoint()
        {
            var board = new Board();
            int ret = 100;
            ulong result = board.AttacksDiaga1(Board.SqValueToUse);

            if (result == Board.ExpectedResult)
            {
                Console.WriteLine("Test passed.");
            }
            else
            {
                Console.WriteLine(
                    "Test failed.\r\n" +
                    "    Expected: {0:x16}\r\n" +
                    "    Observed: {1:x16}\r\n",

                    Board.ExpectedResult,
                    result
                );

                ret = 101;
            }

            return ret;
        }
    }
}
