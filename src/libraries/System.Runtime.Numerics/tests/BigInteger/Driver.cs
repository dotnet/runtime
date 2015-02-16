// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Tools;
using Xunit;

namespace System.Numerics.Tests
{
    public static class Driver
    {
        public static BigInteger b1;
        public static BigInteger b2;
        public static BigInteger b3;
        public static BigInteger[][] results;
        private static Random s_random = new Random(100);

        [Fact]
        [OuterLoop]
        public static void RunTests()
        {
            int cycles = 1;

            //Get the BigIntegers to be testing;
            b1 = new BigInteger(GetRandomByteArray(s_random));
            b2 = new BigInteger(GetRandomByteArray(s_random));
            b3 = new BigInteger(GetRandomByteArray(s_random));
            results = new BigInteger[32][];
            // ...Sign
            results[0] = new BigInteger[3];
            results[0][0] = MyBigIntImp.DoUnaryOperatorMine(b1, "uSign");
            results[0][1] = MyBigIntImp.DoUnaryOperatorMine(b2, "uSign");
            results[0][2] = MyBigIntImp.DoUnaryOperatorMine(b3, "uSign");
            // ...Op ~
            results[1] = new BigInteger[3];
            results[1][0] = MyBigIntImp.DoUnaryOperatorMine(b1, "u~");
            results[1][1] = MyBigIntImp.DoUnaryOperatorMine(b2, "u~");
            results[1][2] = MyBigIntImp.DoUnaryOperatorMine(b3, "u~");
            // ...Log10
            results[2] = new BigInteger[3];
            results[2][0] = MyBigIntImp.DoUnaryOperatorMine(b1, "uLog10");
            results[2][1] = MyBigIntImp.DoUnaryOperatorMine(b2, "uLog10");
            results[2][2] = MyBigIntImp.DoUnaryOperatorMine(b3, "uLog10");
            // ...Log
            results[3] = new BigInteger[3];
            results[3][0] = MyBigIntImp.DoUnaryOperatorMine(b1, "uLog");
            results[3][1] = MyBigIntImp.DoUnaryOperatorMine(b2, "uLog");
            results[3][2] = MyBigIntImp.DoUnaryOperatorMine(b3, "uLog");
            // ...Abs
            results[4] = new BigInteger[3];
            results[4][0] = MyBigIntImp.DoUnaryOperatorMine(b1, "uAbs");
            results[4][1] = MyBigIntImp.DoUnaryOperatorMine(b2, "uAbs");
            results[4][2] = MyBigIntImp.DoUnaryOperatorMine(b3, "uAbs");
            // ...Op --
            results[5] = new BigInteger[3];
            results[5][0] = MyBigIntImp.DoUnaryOperatorMine(b1, "u--");
            results[5][1] = MyBigIntImp.DoUnaryOperatorMine(b2, "u--");
            results[5][2] = MyBigIntImp.DoUnaryOperatorMine(b3, "u--");
            // ...Op ++
            results[6] = new BigInteger[3];
            results[6][0] = MyBigIntImp.DoUnaryOperatorMine(b1, "u++");
            results[6][1] = MyBigIntImp.DoUnaryOperatorMine(b2, "u++");
            results[6][2] = MyBigIntImp.DoUnaryOperatorMine(b3, "u++");
            // ...Negate
            results[7] = new BigInteger[3];
            results[7][0] = MyBigIntImp.DoUnaryOperatorMine(b1, "uNegate");
            results[7][1] = MyBigIntImp.DoUnaryOperatorMine(b2, "uNegate");
            results[7][2] = MyBigIntImp.DoUnaryOperatorMine(b3, "uNegate");
            // ...Op -
            results[8] = new BigInteger[3];
            results[8][0] = MyBigIntImp.DoUnaryOperatorMine(b1, "u-");
            results[8][1] = MyBigIntImp.DoUnaryOperatorMine(b2, "u-");
            results[8][2] = MyBigIntImp.DoUnaryOperatorMine(b3, "u-");
            // ...Op +
            results[9] = new BigInteger[3];
            results[9][0] = MyBigIntImp.DoUnaryOperatorMine(b1, "u+");
            results[9][1] = MyBigIntImp.DoUnaryOperatorMine(b2, "u+");
            results[9][2] = MyBigIntImp.DoUnaryOperatorMine(b3, "u+");
            // ...Min
            results[10] = new BigInteger[9];
            results[10][0] = MyBigIntImp.DoBinaryOperatorMine(b1, b1, "bMin");
            results[10][1] = MyBigIntImp.DoBinaryOperatorMine(b1, b2, "bMin");
            results[10][2] = MyBigIntImp.DoBinaryOperatorMine(b1, b3, "bMin");
            results[10][3] = MyBigIntImp.DoBinaryOperatorMine(b2, b1, "bMin");
            results[10][4] = MyBigIntImp.DoBinaryOperatorMine(b2, b2, "bMin");
            results[10][5] = MyBigIntImp.DoBinaryOperatorMine(b2, b3, "bMin");
            results[10][6] = MyBigIntImp.DoBinaryOperatorMine(b3, b1, "bMin");
            results[10][7] = MyBigIntImp.DoBinaryOperatorMine(b3, b2, "bMin");
            results[10][8] = MyBigIntImp.DoBinaryOperatorMine(b3, b3, "bMin");
            // ...Max
            results[11] = new BigInteger[9];
            results[11][0] = MyBigIntImp.DoBinaryOperatorMine(b1, b1, "bMax");
            results[11][1] = MyBigIntImp.DoBinaryOperatorMine(b1, b2, "bMax");
            results[11][2] = MyBigIntImp.DoBinaryOperatorMine(b1, b3, "bMax");
            results[11][3] = MyBigIntImp.DoBinaryOperatorMine(b2, b1, "bMax");
            results[11][4] = MyBigIntImp.DoBinaryOperatorMine(b2, b2, "bMax");
            results[11][5] = MyBigIntImp.DoBinaryOperatorMine(b2, b3, "bMax");
            results[11][6] = MyBigIntImp.DoBinaryOperatorMine(b3, b1, "bMax");
            results[11][7] = MyBigIntImp.DoBinaryOperatorMine(b3, b2, "bMax");
            results[11][8] = MyBigIntImp.DoBinaryOperatorMine(b3, b3, "bMax");
            // ...Op >>
            results[12] = new BigInteger[9];
            results[12][0] = MyBigIntImp.DoBinaryOperatorMine(b1, Makeint(b1), "b>>");
            results[12][1] = MyBigIntImp.DoBinaryOperatorMine(b1, Makeint(b2), "b>>");
            results[12][2] = MyBigIntImp.DoBinaryOperatorMine(b1, Makeint(b3), "b>>");
            results[12][3] = MyBigIntImp.DoBinaryOperatorMine(b2, Makeint(b1), "b>>");
            results[12][4] = MyBigIntImp.DoBinaryOperatorMine(b2, Makeint(b2), "b>>");
            results[12][5] = MyBigIntImp.DoBinaryOperatorMine(b2, Makeint(b3), "b>>");
            results[12][6] = MyBigIntImp.DoBinaryOperatorMine(b3, Makeint(b1), "b>>");
            results[12][7] = MyBigIntImp.DoBinaryOperatorMine(b3, Makeint(b2), "b>>");
            results[12][8] = MyBigIntImp.DoBinaryOperatorMine(b3, Makeint(b3), "b>>");
            // ...Op <<
            results[13] = new BigInteger[9];
            results[13][0] = MyBigIntImp.DoBinaryOperatorMine(b1, Makeint(b1), "b<<");
            results[13][1] = MyBigIntImp.DoBinaryOperatorMine(b1, Makeint(b2), "b<<");
            results[13][2] = MyBigIntImp.DoBinaryOperatorMine(b1, Makeint(b3), "b<<");
            results[13][3] = MyBigIntImp.DoBinaryOperatorMine(b2, Makeint(b1), "b<<");
            results[13][4] = MyBigIntImp.DoBinaryOperatorMine(b2, Makeint(b2), "b<<");
            results[13][5] = MyBigIntImp.DoBinaryOperatorMine(b2, Makeint(b3), "b<<");
            results[13][6] = MyBigIntImp.DoBinaryOperatorMine(b3, Makeint(b1), "b<<");
            results[13][7] = MyBigIntImp.DoBinaryOperatorMine(b3, Makeint(b2), "b<<");
            results[13][8] = MyBigIntImp.DoBinaryOperatorMine(b3, Makeint(b3), "b<<");
            // ...Op ^
            results[14] = new BigInteger[9];
            results[14][0] = MyBigIntImp.DoBinaryOperatorMine(b1, b1, "b^");
            results[14][1] = MyBigIntImp.DoBinaryOperatorMine(b1, b2, "b^");
            results[14][2] = MyBigIntImp.DoBinaryOperatorMine(b1, b3, "b^");
            results[14][3] = MyBigIntImp.DoBinaryOperatorMine(b2, b1, "b^");
            results[14][4] = MyBigIntImp.DoBinaryOperatorMine(b2, b2, "b^");
            results[14][5] = MyBigIntImp.DoBinaryOperatorMine(b2, b3, "b^");
            results[14][6] = MyBigIntImp.DoBinaryOperatorMine(b3, b1, "b^");
            results[14][7] = MyBigIntImp.DoBinaryOperatorMine(b3, b2, "b^");
            results[14][8] = MyBigIntImp.DoBinaryOperatorMine(b3, b3, "b^");
            // ...Op |
            results[15] = new BigInteger[9];
            results[15][0] = MyBigIntImp.DoBinaryOperatorMine(b1, b1, "b|");
            results[15][1] = MyBigIntImp.DoBinaryOperatorMine(b1, b2, "b|");
            results[15][2] = MyBigIntImp.DoBinaryOperatorMine(b1, b3, "b|");
            results[15][3] = MyBigIntImp.DoBinaryOperatorMine(b2, b1, "b|");
            results[15][4] = MyBigIntImp.DoBinaryOperatorMine(b2, b2, "b|");
            results[15][5] = MyBigIntImp.DoBinaryOperatorMine(b2, b3, "b|");
            results[15][6] = MyBigIntImp.DoBinaryOperatorMine(b3, b1, "b|");
            results[15][7] = MyBigIntImp.DoBinaryOperatorMine(b3, b2, "b|");
            results[15][8] = MyBigIntImp.DoBinaryOperatorMine(b3, b3, "b|");
            // ...Op &
            results[16] = new BigInteger[9];
            results[16][0] = MyBigIntImp.DoBinaryOperatorMine(b1, b1, "b&");
            results[16][1] = MyBigIntImp.DoBinaryOperatorMine(b1, b2, "b&");
            results[16][2] = MyBigIntImp.DoBinaryOperatorMine(b1, b3, "b&");
            results[16][3] = MyBigIntImp.DoBinaryOperatorMine(b2, b1, "b&");
            results[16][4] = MyBigIntImp.DoBinaryOperatorMine(b2, b2, "b&");
            results[16][5] = MyBigIntImp.DoBinaryOperatorMine(b2, b3, "b&");
            results[16][6] = MyBigIntImp.DoBinaryOperatorMine(b3, b1, "b&");
            results[16][7] = MyBigIntImp.DoBinaryOperatorMine(b3, b2, "b&");
            results[16][8] = MyBigIntImp.DoBinaryOperatorMine(b3, b3, "b&");
            // ...Log
            results[17] = new BigInteger[9];
            results[17][0] = MyBigIntImp.DoBinaryOperatorMine(b1, b1, "bLog");
            results[17][1] = MyBigIntImp.DoBinaryOperatorMine(b1, b2, "bLog");
            results[17][2] = MyBigIntImp.DoBinaryOperatorMine(b1, b3, "bLog");
            results[17][3] = MyBigIntImp.DoBinaryOperatorMine(b2, b1, "bLog");
            results[17][4] = MyBigIntImp.DoBinaryOperatorMine(b2, b2, "bLog");
            results[17][5] = MyBigIntImp.DoBinaryOperatorMine(b2, b3, "bLog");
            results[17][6] = MyBigIntImp.DoBinaryOperatorMine(b3, b1, "bLog");
            results[17][7] = MyBigIntImp.DoBinaryOperatorMine(b3, b2, "bLog");
            results[17][8] = MyBigIntImp.DoBinaryOperatorMine(b3, b3, "bLog");
            // ...GCD
            results[18] = new BigInteger[9];
            results[18][0] = MyBigIntImp.DoBinaryOperatorMine(b1, b1, "bGCD");
            results[18][1] = MyBigIntImp.DoBinaryOperatorMine(b1, b2, "bGCD");
            results[18][2] = MyBigIntImp.DoBinaryOperatorMine(b1, b3, "bGCD");
            results[18][3] = MyBigIntImp.DoBinaryOperatorMine(b2, b1, "bGCD");
            results[18][4] = MyBigIntImp.DoBinaryOperatorMine(b2, b2, "bGCD");
            results[18][5] = MyBigIntImp.DoBinaryOperatorMine(b2, b3, "bGCD");
            results[18][6] = MyBigIntImp.DoBinaryOperatorMine(b3, b1, "bGCD");
            results[18][7] = MyBigIntImp.DoBinaryOperatorMine(b3, b2, "bGCD");
            results[18][8] = MyBigIntImp.DoBinaryOperatorMine(b3, b3, "bGCD");
            // ...DivRem
            results[20] = new BigInteger[9];
            results[20][0] = MyBigIntImp.DoBinaryOperatorMine(b1, b1, "bDivRem");
            results[20][1] = MyBigIntImp.DoBinaryOperatorMine(b1, b2, "bDivRem");
            results[20][2] = MyBigIntImp.DoBinaryOperatorMine(b1, b3, "bDivRem");
            results[20][3] = MyBigIntImp.DoBinaryOperatorMine(b2, b1, "bDivRem");
            results[20][4] = MyBigIntImp.DoBinaryOperatorMine(b2, b2, "bDivRem");
            results[20][5] = MyBigIntImp.DoBinaryOperatorMine(b2, b3, "bDivRem");
            results[20][6] = MyBigIntImp.DoBinaryOperatorMine(b3, b1, "bDivRem");
            results[20][7] = MyBigIntImp.DoBinaryOperatorMine(b3, b2, "bDivRem");
            results[20][8] = MyBigIntImp.DoBinaryOperatorMine(b3, b3, "bDivRem");
            // ...Remainder
            results[21] = new BigInteger[9];
            results[21][0] = MyBigIntImp.DoBinaryOperatorMine(b1, b1, "bRemainder");
            results[21][1] = MyBigIntImp.DoBinaryOperatorMine(b1, b2, "bRemainder");
            results[21][2] = MyBigIntImp.DoBinaryOperatorMine(b1, b3, "bRemainder");
            results[21][3] = MyBigIntImp.DoBinaryOperatorMine(b2, b1, "bRemainder");
            results[21][4] = MyBigIntImp.DoBinaryOperatorMine(b2, b2, "bRemainder");
            results[21][5] = MyBigIntImp.DoBinaryOperatorMine(b2, b3, "bRemainder");
            results[21][6] = MyBigIntImp.DoBinaryOperatorMine(b3, b1, "bRemainder");
            results[21][7] = MyBigIntImp.DoBinaryOperatorMine(b3, b2, "bRemainder");
            results[21][8] = MyBigIntImp.DoBinaryOperatorMine(b3, b3, "bRemainder");
            // ...Op %
            results[22] = new BigInteger[9];
            results[22][0] = MyBigIntImp.DoBinaryOperatorMine(b1, b1, "b%");
            results[22][1] = MyBigIntImp.DoBinaryOperatorMine(b1, b2, "b%");
            results[22][2] = MyBigIntImp.DoBinaryOperatorMine(b1, b3, "b%");
            results[22][3] = MyBigIntImp.DoBinaryOperatorMine(b2, b1, "b%");
            results[22][4] = MyBigIntImp.DoBinaryOperatorMine(b2, b2, "b%");
            results[22][5] = MyBigIntImp.DoBinaryOperatorMine(b2, b3, "b%");
            results[22][6] = MyBigIntImp.DoBinaryOperatorMine(b3, b1, "b%");
            results[22][7] = MyBigIntImp.DoBinaryOperatorMine(b3, b2, "b%");
            results[22][8] = MyBigIntImp.DoBinaryOperatorMine(b3, b3, "b%");
            // ...Divide
            results[23] = new BigInteger[9];
            results[23][0] = MyBigIntImp.DoBinaryOperatorMine(b1, b1, "bDivide");
            results[23][1] = MyBigIntImp.DoBinaryOperatorMine(b1, b2, "bDivide");
            results[23][2] = MyBigIntImp.DoBinaryOperatorMine(b1, b3, "bDivide");
            results[23][3] = MyBigIntImp.DoBinaryOperatorMine(b2, b1, "bDivide");
            results[23][4] = MyBigIntImp.DoBinaryOperatorMine(b2, b2, "bDivide");
            results[23][5] = MyBigIntImp.DoBinaryOperatorMine(b2, b3, "bDivide");
            results[23][6] = MyBigIntImp.DoBinaryOperatorMine(b3, b1, "bDivide");
            results[23][7] = MyBigIntImp.DoBinaryOperatorMine(b3, b2, "bDivide");
            results[23][8] = MyBigIntImp.DoBinaryOperatorMine(b3, b3, "bDivide");
            // ...Op /
            results[24] = new BigInteger[9];
            results[24][0] = MyBigIntImp.DoBinaryOperatorMine(b1, b1, "b/");
            results[24][1] = MyBigIntImp.DoBinaryOperatorMine(b1, b2, "b/");
            results[24][2] = MyBigIntImp.DoBinaryOperatorMine(b1, b3, "b/");
            results[24][3] = MyBigIntImp.DoBinaryOperatorMine(b2, b1, "b/");
            results[24][4] = MyBigIntImp.DoBinaryOperatorMine(b2, b2, "b/");
            results[24][5] = MyBigIntImp.DoBinaryOperatorMine(b2, b3, "b/");
            results[24][6] = MyBigIntImp.DoBinaryOperatorMine(b3, b1, "b/");
            results[24][7] = MyBigIntImp.DoBinaryOperatorMine(b3, b2, "b/");
            results[24][8] = MyBigIntImp.DoBinaryOperatorMine(b3, b3, "b/");
            // ...Multiply
            results[25] = new BigInteger[9];
            results[25][0] = MyBigIntImp.DoBinaryOperatorMine(b1, b1, "bMultiply");
            results[25][1] = MyBigIntImp.DoBinaryOperatorMine(b1, b2, "bMultiply");
            results[25][2] = MyBigIntImp.DoBinaryOperatorMine(b1, b3, "bMultiply");
            results[25][3] = MyBigIntImp.DoBinaryOperatorMine(b2, b1, "bMultiply");
            results[25][4] = MyBigIntImp.DoBinaryOperatorMine(b2, b2, "bMultiply");
            results[25][5] = MyBigIntImp.DoBinaryOperatorMine(b2, b3, "bMultiply");
            results[25][6] = MyBigIntImp.DoBinaryOperatorMine(b3, b1, "bMultiply");
            results[25][7] = MyBigIntImp.DoBinaryOperatorMine(b3, b2, "bMultiply");
            results[25][8] = MyBigIntImp.DoBinaryOperatorMine(b3, b3, "bMultiply");
            // ...Op *
            results[26] = new BigInteger[9];
            results[26][0] = MyBigIntImp.DoBinaryOperatorMine(b1, b1, "b*");
            results[26][1] = MyBigIntImp.DoBinaryOperatorMine(b1, b2, "b*");
            results[26][2] = MyBigIntImp.DoBinaryOperatorMine(b1, b3, "b*");
            results[26][3] = MyBigIntImp.DoBinaryOperatorMine(b2, b1, "b*");
            results[26][4] = MyBigIntImp.DoBinaryOperatorMine(b2, b2, "b*");
            results[26][5] = MyBigIntImp.DoBinaryOperatorMine(b2, b3, "b*");
            results[26][6] = MyBigIntImp.DoBinaryOperatorMine(b3, b1, "b*");
            results[26][7] = MyBigIntImp.DoBinaryOperatorMine(b3, b2, "b*");
            results[26][8] = MyBigIntImp.DoBinaryOperatorMine(b3, b3, "b*");
            // ...Subtract
            results[27] = new BigInteger[9];
            results[27][0] = MyBigIntImp.DoBinaryOperatorMine(b1, b1, "bSubtract");
            results[27][1] = MyBigIntImp.DoBinaryOperatorMine(b1, b2, "bSubtract");
            results[27][2] = MyBigIntImp.DoBinaryOperatorMine(b1, b3, "bSubtract");
            results[27][3] = MyBigIntImp.DoBinaryOperatorMine(b2, b1, "bSubtract");
            results[27][4] = MyBigIntImp.DoBinaryOperatorMine(b2, b2, "bSubtract");
            results[27][5] = MyBigIntImp.DoBinaryOperatorMine(b2, b3, "bSubtract");
            results[27][6] = MyBigIntImp.DoBinaryOperatorMine(b3, b1, "bSubtract");
            results[27][7] = MyBigIntImp.DoBinaryOperatorMine(b3, b2, "bSubtract");
            results[27][8] = MyBigIntImp.DoBinaryOperatorMine(b3, b3, "bSubtract");
            // ...Op -
            results[28] = new BigInteger[9];
            results[28][0] = MyBigIntImp.DoBinaryOperatorMine(b1, b1, "b-");
            results[28][1] = MyBigIntImp.DoBinaryOperatorMine(b1, b2, "b-");
            results[28][2] = MyBigIntImp.DoBinaryOperatorMine(b1, b3, "b-");
            results[28][3] = MyBigIntImp.DoBinaryOperatorMine(b2, b1, "b-");
            results[28][4] = MyBigIntImp.DoBinaryOperatorMine(b2, b2, "b-");
            results[28][5] = MyBigIntImp.DoBinaryOperatorMine(b2, b3, "b-");
            results[28][6] = MyBigIntImp.DoBinaryOperatorMine(b3, b1, "b-");
            results[28][7] = MyBigIntImp.DoBinaryOperatorMine(b3, b2, "b-");
            results[28][8] = MyBigIntImp.DoBinaryOperatorMine(b3, b3, "b-");
            // ...Add
            results[29] = new BigInteger[9];
            results[29][0] = MyBigIntImp.DoBinaryOperatorMine(b1, b1, "bAdd");
            results[29][1] = MyBigIntImp.DoBinaryOperatorMine(b1, b2, "bAdd");
            results[29][2] = MyBigIntImp.DoBinaryOperatorMine(b1, b3, "bAdd");
            results[29][3] = MyBigIntImp.DoBinaryOperatorMine(b2, b1, "bAdd");
            results[29][4] = MyBigIntImp.DoBinaryOperatorMine(b2, b2, "bAdd");
            results[29][5] = MyBigIntImp.DoBinaryOperatorMine(b2, b3, "bAdd");
            results[29][6] = MyBigIntImp.DoBinaryOperatorMine(b3, b1, "bAdd");
            results[29][7] = MyBigIntImp.DoBinaryOperatorMine(b3, b2, "bAdd");
            results[29][8] = MyBigIntImp.DoBinaryOperatorMine(b3, b3, "bAdd");
            // ...Op +
            results[30] = new BigInteger[9];
            results[30][0] = MyBigIntImp.DoBinaryOperatorMine(b1, b1, "b+");
            results[30][1] = MyBigIntImp.DoBinaryOperatorMine(b1, b2, "b+");
            results[30][2] = MyBigIntImp.DoBinaryOperatorMine(b1, b3, "b+");
            results[30][3] = MyBigIntImp.DoBinaryOperatorMine(b2, b1, "b+");
            results[30][4] = MyBigIntImp.DoBinaryOperatorMine(b2, b2, "b+");
            results[30][5] = MyBigIntImp.DoBinaryOperatorMine(b2, b3, "b+");
            results[30][6] = MyBigIntImp.DoBinaryOperatorMine(b3, b1, "b+");
            results[30][7] = MyBigIntImp.DoBinaryOperatorMine(b3, b2, "b+");
            results[30][8] = MyBigIntImp.DoBinaryOperatorMine(b3, b3, "b+");
            // ...ModPow
            results[31] = new BigInteger[27];
            results[31][0] = MyBigIntImp.DoTertanaryOperatorMine(b1, (b1 < 0 ? -b1 : b1), b1, "tModPow");
            results[31][1] = MyBigIntImp.DoTertanaryOperatorMine(b1, (b1 < 0 ? -b1 : b1), b2, "tModPow");
            results[31][2] = MyBigIntImp.DoTertanaryOperatorMine(b1, (b1 < 0 ? -b1 : b1), b3, "tModPow");
            results[31][3] = MyBigIntImp.DoTertanaryOperatorMine(b1, (b2 < 0 ? -b2 : b2), b1, "tModPow");
            results[31][4] = MyBigIntImp.DoTertanaryOperatorMine(b1, (b2 < 0 ? -b2 : b2), b2, "tModPow");
            results[31][5] = MyBigIntImp.DoTertanaryOperatorMine(b1, (b2 < 0 ? -b2 : b2), b3, "tModPow");
            results[31][6] = MyBigIntImp.DoTertanaryOperatorMine(b1, (b3 < 0 ? -b3 : b3), b1, "tModPow");
            results[31][7] = MyBigIntImp.DoTertanaryOperatorMine(b1, (b3 < 0 ? -b3 : b3), b2, "tModPow");
            results[31][8] = MyBigIntImp.DoTertanaryOperatorMine(b1, (b3 < 0 ? -b3 : b3), b3, "tModPow");
            results[31][9] = MyBigIntImp.DoTertanaryOperatorMine(b2, (b1 < 0 ? -b1 : b1), b1, "tModPow");
            results[31][10] = MyBigIntImp.DoTertanaryOperatorMine(b2, (b1 < 0 ? -b1 : b1), b2, "tModPow");
            results[31][11] = MyBigIntImp.DoTertanaryOperatorMine(b2, (b1 < 0 ? -b1 : b1), b3, "tModPow");
            results[31][12] = MyBigIntImp.DoTertanaryOperatorMine(b2, (b2 < 0 ? -b2 : b2), b1, "tModPow");
            results[31][13] = MyBigIntImp.DoTertanaryOperatorMine(b2, (b2 < 0 ? -b2 : b2), b2, "tModPow");
            results[31][14] = MyBigIntImp.DoTertanaryOperatorMine(b2, (b2 < 0 ? -b2 : b2), b3, "tModPow");
            results[31][15] = MyBigIntImp.DoTertanaryOperatorMine(b2, (b3 < 0 ? -b3 : b3), b1, "tModPow");
            results[31][16] = MyBigIntImp.DoTertanaryOperatorMine(b2, (b3 < 0 ? -b3 : b3), b2, "tModPow");
            results[31][17] = MyBigIntImp.DoTertanaryOperatorMine(b2, (b3 < 0 ? -b3 : b3), b3, "tModPow");
            results[31][18] = MyBigIntImp.DoTertanaryOperatorMine(b3, (b1 < 0 ? -b1 : b1), b1, "tModPow");
            results[31][19] = MyBigIntImp.DoTertanaryOperatorMine(b3, (b1 < 0 ? -b1 : b1), b2, "tModPow");
            results[31][20] = MyBigIntImp.DoTertanaryOperatorMine(b3, (b1 < 0 ? -b1 : b1), b3, "tModPow");
            results[31][21] = MyBigIntImp.DoTertanaryOperatorMine(b3, (b2 < 0 ? -b2 : b2), b1, "tModPow");
            results[31][22] = MyBigIntImp.DoTertanaryOperatorMine(b3, (b2 < 0 ? -b2 : b2), b2, "tModPow");
            results[31][23] = MyBigIntImp.DoTertanaryOperatorMine(b3, (b2 < 0 ? -b2 : b2), b3, "tModPow");
            results[31][24] = MyBigIntImp.DoTertanaryOperatorMine(b3, (b3 < 0 ? -b3 : b3), b1, "tModPow");
            results[31][25] = MyBigIntImp.DoTertanaryOperatorMine(b3, (b3 < 0 ? -b3 : b3), b2, "tModPow");
            results[31][26] = MyBigIntImp.DoTertanaryOperatorMine(b3, (b3 < 0 ? -b3 : b3), b3, "tModPow");


            for (int i = 0; i < cycles; i++)
            {
                Worker worker = new Worker();
                worker.id = i;
                worker.random = new Random(s_random.Next());
                worker.DoWork();
                Assert.True(worker.ret, " Verification Failed");
            }
        }

        private static Byte[] GetRandomByteArray(Random random)
        {
            return GetRandomByteArray(random, random.Next(1, 18));
        }
        private static Byte[] GetRandomByteArray(Random random, int size)
        {
            byte[] value = new byte[size];
            bool zero = true;

            while (zero)
            {
                for (int i = 0; i < value.Length; ++i)
                {
                    value[i] = (byte)random.Next(0, 256);
                    if (value[i] != 0) zero = false;
                }
            }

            return value;
        }
        private static int Makeint(BigInteger input)
        {
            int output;

            if (input < 0) input = -input;
            input = input + int.MaxValue;

            byte[] temp = input.ToByteArray();
            temp[1] = 0;
            temp[2] = 0;
            temp[3] = 0;

            output = BitConverter.ToInt32(temp, 0);
            if (output == 0) output = 1;

            return output;
        }
    }

    public class Worker
    {
        public Random random;
        public int id;
        public bool ret = true;
        public bool done = false;

        public void DoWork()
        {
            bool corrupt = false;
            BigInteger b1 = Driver.b1;
            BigInteger b2 = Driver.b2;
            BigInteger b3 = Driver.b3;
            BigInteger[][] results = Driver.results;

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            while (stopWatch.ElapsedMilliseconds < 10000)
            {
                int op = random.Next(0, 32);

                //remove trouble cases (Pow for perf)
                while (op == 19) op = random.Next(0, 32);

                int order = random.Next(0, 27);
                switch (op)
                {
                    case 0:
                        switch (order % 3)
                        {
                            case 0:
                                Assert.True(Sign(b1, results[0][0]), " Verification Failed");
                                break;
                            case 1:
                                Assert.True(Sign(b2, results[0][1]), " Verification Failed");
                                break;
                            case 2:
                                Assert.True(Sign(b3, results[0][2]), " Verification Failed");
                                break;
                            default:
                                Console.WriteLine("Invalid bigInt selected");
                                ret = false;
                                break;
                        }
                        break;
                    case 1:
                        switch (order % 3)
                        {
                            case 0:
                                Assert.True(Op_Not(b1, results[1][0]), " Verification Failed");
                                break;
                            case 1:
                                Assert.True(Op_Not(b2, results[1][1]), " Verification Failed");
                                break;
                            case 2:
                                Assert.True(Op_Not(b3, results[1][2]), " Verification Failed");
                                break;
                            default:
                                Console.WriteLine("Invalid bigInt selected");
                                ret = false;
                                break;
                        }
                        break;
                    case 2:
                        switch (order % 3)
                        {
                            case 0:
                                Assert.True(Log10(b1, results[2][0]), " Verification Failed");
                                break;
                            case 1:
                                Assert.True(Log10(b2, results[2][1]), " Verification Failed");
                                break;
                            case 2:
                                Assert.True(Log10(b3, results[2][2]), " Verification Failed");
                                break;
                            default:
                                Console.WriteLine("Invalid bigInt selected");
                                ret = false;
                                break;
                        }
                        break;
                    case 3:
                        switch (order % 3)
                        {
                            case 0:
                                Assert.True(Log(b1, results[3][0]), " Verification Failed");
                                break;
                            case 1:
                                Assert.True(Log(b2, results[3][1]), " Verification Failed");
                                break;
                            case 2:
                                Assert.True(Log(b3, results[3][2]), " Verification Failed");
                                break;
                            default:
                                Console.WriteLine("Invalid bigInt selected");
                                ret = false;
                                break;
                        }
                        break;
                    case 4:
                        switch (order % 3)
                        {
                            case 0:
                                Assert.True(Abs(b1, results[4][0]), " Verification Failed");
                                break;
                            case 1:
                                Assert.True(Abs(b2, results[4][1]), " Verification Failed");
                                break;
                            case 2:
                                Assert.True(Abs(b3, results[4][2]), " Verification Failed");
                                break;
                            default:
                                Console.WriteLine("Invalid bigInt selected");
                                ret = false;
                                break;
                        }
                        break;
                    case 5:
                        switch (order % 3)
                        {
                            case 0:
                                Assert.True(Op_Decrement(b1, results[5][0]), " Verification Failed");
                                break;
                            case 1:
                                Assert.True(Op_Decrement(b2, results[5][1]), " Verification Failed");
                                break;
                            case 2:
                                Assert.True(Op_Decrement(b3, results[5][2]), " Verification Failed");
                                break;
                            default:
                                Console.WriteLine("Invalid bigInt selected");
                                ret = false;
                                break;
                        }
                        break;
                    case 6:
                        switch (order % 3)
                        {
                            case 0:
                                Assert.True(Op_Increment(b1, results[6][0]), " Verification Failed");
                                break;
                            case 1:
                                Assert.True(Op_Increment(b2, results[6][1]), " Verification Failed");
                                break;
                            case 2:
                                Assert.True(Op_Increment(b3, results[6][2]), " Verification Failed");
                                break;
                            default:
                                Console.WriteLine("Invalid bigInt selected");
                                ret = false;
                                break;
                        }
                        break;
                    case 7:
                        switch (order % 3)
                        {
                            case 0:
                                Assert.True(Negate(b1, results[7][0]), " Verification Failed");
                                break;
                            case 1:
                                Assert.True(Negate(b2, results[7][1]), " Verification Failed");
                                break;
                            case 2:
                                Assert.True(Negate(b3, results[7][2]), " Verification Failed");
                                break;
                            default:
                                Console.WriteLine("Invalid bigInt selected");
                                ret = false;
                                break;
                        }
                        break;
                    case 8:
                        switch (order % 3)
                        {
                            case 0:
                                Assert.True(Op_Negate(b1, results[8][0]), " Verification Failed");
                                break;
                            case 1:
                                Assert.True(Op_Negate(b2, results[8][1]), " Verification Failed");
                                break;
                            case 2:
                                Assert.True(Op_Negate(b3, results[8][2]), " Verification Failed");
                                break;
                            default:
                                Console.WriteLine("Invalid bigInt selected");
                                ret = false;
                                break;
                        }
                        break;
                    case 9:
                        switch (order % 3)
                        {
                            case 0:
                                Assert.True(Op_Plus(b1, results[9][0]), " Verification Failed");
                                break;
                            case 1:
                                Assert.True(Op_Plus(b2, results[9][1]), " Verification Failed");
                                break;
                            case 2:
                                Assert.True(Op_Plus(b3, results[9][2]), " Verification Failed");
                                break;
                            default:
                                Console.WriteLine("Invalid bigInt selected");
                                ret = false;
                                break;
                        }
                        break;
                    case 10:
                        switch (order % 9)
                        {
                            case 0:
                                Assert.True(Min(b1, b1, results[10][0]), " Verification Failed");
                                break;
                            case 1:
                                Assert.True(Min(b1, b2, results[10][1]), " Verification Failed");
                                break;
                            case 2:
                                Assert.True(Min(b1, b3, results[10][2]), " Verification Failed");
                                break;
                            case 3:
                                Assert.True(Min(b2, b1, results[10][3]), " Verification Failed");
                                break;
                            case 4:
                                Assert.True(Min(b2, b2, results[10][4]), " Verification Failed");
                                break;
                            case 5:
                                Assert.True(Min(b2, b3, results[10][5]), " Verification Failed");
                                break;
                            case 6:
                                Assert.True(Min(b3, b1, results[10][6]), " Verification Failed");
                                break;
                            case 7:
                                Assert.True(Min(b3, b2, results[10][7]), " Verification Failed");
                                break;
                            case 8:
                                Assert.True(Min(b3, b3, results[10][8]), " Verification Failed");
                                break;
                            default:
                                Console.WriteLine("Invalid bigInt selected");
                                ret = false;
                                break;
                        }
                        break;
                    case 11:
                        switch (order % 9)
                        {
                            case 0:
                                Assert.True(Max(b1, b1, results[11][0]), " Verification Failed");
                                break;
                            case 1:
                                Assert.True(Max(b1, b2, results[11][1]), " Verification Failed");
                                break;
                            case 2:
                                Assert.True(Max(b1, b3, results[11][2]), " Verification Failed");
                                break;
                            case 3:
                                Assert.True(Max(b2, b1, results[11][3]), " Verification Failed");
                                break;
                            case 4:
                                Assert.True(Max(b2, b2, results[11][4]), " Verification Failed");
                                break;
                            case 5:
                                Assert.True(Max(b2, b3, results[11][5]), " Verification Failed");
                                break;
                            case 6:
                                Assert.True(Max(b3, b1, results[11][6]), " Verification Failed");
                                break;
                            case 7:
                                Assert.True(Max(b3, b2, results[11][7]), " Verification Failed");
                                break;
                            case 8:
                                Assert.True(Max(b3, b3, results[11][8]), " Verification Failed");
                                break;
                            default:
                                Console.WriteLine("Invalid bigInt selected");
                                ret = false;
                                break;
                        }
                        break;
                    case 12:
                        switch (order % 9)
                        {
                            case 0:
                                Assert.True(Op_RightShift(b1, b1, results[12][0]), " Verification Failed");
                                break;
                            case 1:
                                Assert.True(Op_RightShift(b1, b2, results[12][1]), " Verification Failed");
                                break;
                            case 2:
                                Assert.True(Op_RightShift(b1, b3, results[12][2]), " Verification Failed");
                                break;
                            case 3:
                                Assert.True(Op_RightShift(b2, b1, results[12][3]), " Verification Failed");
                                break;
                            case 4:
                                Assert.True(Op_RightShift(b2, b2, results[12][4]), " Verification Failed");
                                break;
                            case 5:
                                Assert.True(Op_RightShift(b2, b3, results[12][5]), " Verification Failed");
                                break;
                            case 6:
                                Assert.True(Op_RightShift(b3, b1, results[12][6]), " Verification Failed");
                                break;
                            case 7:
                                Assert.True(Op_RightShift(b3, b2, results[12][7]), " Verification Failed");
                                break;
                            case 8:
                                Assert.True(Op_RightShift(b3, b3, results[12][8]), " Verification Failed");
                                break;
                            default:
                                Console.WriteLine("Invalid bigInt selected");
                                ret = false;
                                break;
                        }
                        break;
                    case 13:
                        switch (order % 9)
                        {
                            case 0:
                                Assert.True(Op_LeftShift(b1, b1, results[13][0]), " Verification Failed");
                                break;
                            case 1:
                                Assert.True(Op_LeftShift(b1, b2, results[13][1]), " Verification Failed");
                                break;
                            case 2:
                                Assert.True(Op_LeftShift(b1, b3, results[13][2]), " Verification Failed");
                                break;
                            case 3:
                                Assert.True(Op_LeftShift(b2, b1, results[13][3]), " Verification Failed");
                                break;
                            case 4:
                                Assert.True(Op_LeftShift(b2, b2, results[13][4]), " Verification Failed");
                                break;
                            case 5:
                                Assert.True(Op_LeftShift(b2, b3, results[13][5]), " Verification Failed");
                                break;
                            case 6:
                                Assert.True(Op_LeftShift(b3, b1, results[13][6]), " Verification Failed");
                                break;
                            case 7:
                                Assert.True(Op_LeftShift(b3, b2, results[13][7]), " Verification Failed");
                                break;
                            case 8:
                                Assert.True(Op_LeftShift(b3, b3, results[13][8]), " Verification Failed");
                                break;
                            default:
                                Console.WriteLine("Invalid bigInt selected");
                                ret = false;
                                break;
                        }
                        break;
                    case 14:
                        switch (order % 9)
                        {
                            case 0:
                                Assert.True(Op_Xor(b1, b1, results[14][0]), " Verification Failed");
                                break;
                            case 1:
                                Assert.True(Op_Xor(b1, b2, results[14][1]), " Verification Failed");
                                break;
                            case 2:
                                Assert.True(Op_Xor(b1, b3, results[14][2]), " Verification Failed");
                                break;
                            case 3:
                                Assert.True(Op_Xor(b2, b1, results[14][3]), " Verification Failed");
                                break;
                            case 4:
                                Assert.True(Op_Xor(b2, b2, results[14][4]), " Verification Failed");
                                break;
                            case 5:
                                Assert.True(Op_Xor(b2, b3, results[14][5]), " Verification Failed");
                                break;
                            case 6:
                                Assert.True(Op_Xor(b3, b1, results[14][6]), " Verification Failed");
                                break;
                            case 7:
                                Assert.True(Op_Xor(b3, b2, results[14][7]), " Verification Failed");
                                break;
                            case 8:
                                Assert.True(Op_Xor(b3, b3, results[14][8]), " Verification Failed");
                                break;
                            default:
                                Console.WriteLine("Invalid bigInt selected");
                                ret = false;
                                break;
                        }
                        break;
                    case 15:
                        switch (order % 9)
                        {
                            case 0:
                                Assert.True(Op_Or(b1, b1, results[15][0]), " Verification Failed");
                                break;
                            case 1:
                                Assert.True(Op_Or(b1, b2, results[15][1]), " Verification Failed");
                                break;
                            case 2:
                                Assert.True(Op_Or(b1, b3, results[15][2]), " Verification Failed");
                                break;
                            case 3:
                                Assert.True(Op_Or(b2, b1, results[15][3]), " Verification Failed");
                                break;
                            case 4:
                                Assert.True(Op_Or(b2, b2, results[15][4]), " Verification Failed");
                                break;
                            case 5:
                                Assert.True(Op_Or(b2, b3, results[15][5]), " Verification Failed");
                                break;
                            case 6:
                                Assert.True(Op_Or(b3, b1, results[15][6]), " Verification Failed");
                                break;
                            case 7:
                                Assert.True(Op_Or(b3, b2, results[15][7]), " Verification Failed");
                                break;
                            case 8:
                                Assert.True(Op_Or(b3, b3, results[15][8]), " Verification Failed");
                                break;
                            default:
                                Console.WriteLine("Invalid bigInt selected");
                                ret = false;
                                break;
                        }
                        break;
                    case 16:
                        switch (order % 9)
                        {
                            case 0:
                                Assert.True(Op_And(b1, b1, results[16][0]), " Verification Failed");
                                break;
                            case 1:
                                Assert.True(Op_And(b1, b2, results[16][1]), " Verification Failed");
                                break;
                            case 2:
                                Assert.True(Op_And(b1, b3, results[16][2]), " Verification Failed");
                                break;
                            case 3:
                                Assert.True(Op_And(b2, b1, results[16][3]), " Verification Failed");
                                break;
                            case 4:
                                Assert.True(Op_And(b2, b2, results[16][4]), " Verification Failed");
                                break;
                            case 5:
                                Assert.True(Op_And(b2, b3, results[16][5]), " Verification Failed");
                                break;
                            case 6:
                                Assert.True(Op_And(b3, b1, results[16][6]), " Verification Failed");
                                break;
                            case 7:
                                Assert.True(Op_And(b3, b2, results[16][7]), " Verification Failed");
                                break;
                            case 8:
                                Assert.True(Op_And(b3, b3, results[16][8]), " Verification Failed");
                                break;
                            default:
                                Console.WriteLine("Invalid bigInt selected");
                                ret = false;
                                break;
                        }
                        break;
                    case 17:
                        switch (order % 9)
                        {
                            case 0:
                                Assert.True(Log(b1, b1, results[17][0]), " Verification Failed");
                                break;
                            case 1:
                                Assert.True(Log(b1, b2, results[17][1]), " Verification Failed");
                                break;
                            case 2:
                                Assert.True(Log(b1, b3, results[17][2]), " Verification Failed");
                                break;
                            case 3:
                                Assert.True(Log(b2, b1, results[17][3]), " Verification Failed");
                                break;
                            case 4:
                                Assert.True(Log(b2, b2, results[17][4]), " Verification Failed");
                                break;
                            case 5:
                                Assert.True(Log(b2, b3, results[17][5]), " Verification Failed");
                                break;
                            case 6:
                                Assert.True(Log(b3, b1, results[17][6]), " Verification Failed");
                                break;
                            case 7:
                                Assert.True(Log(b3, b2, results[17][7]), " Verification Failed");
                                break;
                            case 8:
                                Assert.True(Log(b3, b3, results[17][8]), " Verification Failed");
                                break;
                            default:
                                Console.WriteLine("Invalid bigInt selected");
                                ret = false;
                                break;
                        }
                        break;
                    case 18:
                        switch (order % 9)
                        {
                            case 0:
                                Assert.True(GCD(b1, b1, results[18][0]), " Verification Failed");
                                break;
                            case 1:
                                Assert.True(GCD(b1, b2, results[18][1]), " Verification Failed");
                                break;
                            case 2:
                                Assert.True(GCD(b1, b3, results[18][2]), " Verification Failed");
                                break;
                            case 3:
                                Assert.True(GCD(b2, b1, results[18][3]), " Verification Failed");
                                break;
                            case 4:
                                Assert.True(GCD(b2, b2, results[18][4]), " Verification Failed");
                                break;
                            case 5:
                                Assert.True(GCD(b2, b3, results[18][5]), " Verification Failed");
                                break;
                            case 6:
                                Assert.True(GCD(b3, b1, results[18][6]), " Verification Failed");
                                break;
                            case 7:
                                Assert.True(GCD(b3, b2, results[18][7]), " Verification Failed");
                                break;
                            case 8:
                                Assert.True(GCD(b3, b3, results[18][8]), " Verification Failed");
                                break;
                            default:
                                Console.WriteLine("Invalid bigInt selected");
                                ret = false;
                                break;
                        }
                        break;
                    case 19:
                        switch (order % 9)
                        {
                            case 0:
                                Assert.True(Pow(b1, b1, results[19][0]), " Verification Failed");
                                break;
                            case 1:
                                Assert.True(Pow(b1, b2, results[19][1]), " Verification Failed");
                                break;
                            case 2:
                                Assert.True(Pow(b1, b3, results[19][2]), " Verification Failed");
                                break;
                            case 3:
                                Assert.True(Pow(b2, b1, results[19][3]), " Verification Failed");
                                break;
                            case 4:
                                Assert.True(Pow(b2, b2, results[19][4]), " Verification Failed");
                                break;
                            case 5:
                                Assert.True(Pow(b2, b3, results[19][5]), " Verification Failed");
                                break;
                            case 6:
                                Assert.True(Pow(b3, b1, results[19][6]), " Verification Failed");
                                break;
                            case 7:
                                Assert.True(Pow(b3, b2, results[19][7]), " Verification Failed");
                                break;
                            case 8:
                                Assert.True(Pow(b3, b3, results[19][8]), " Verification Failed");
                                break;
                            default:
                                Console.WriteLine("Invalid bigInt selected");
                                ret = false;
                                break;
                        }
                        break;
                    case 20:
                        switch (order % 9)
                        {
                            case 0:
                                Assert.True(DivRem(b1, b1, results[20][0]), " Verification Failed");
                                break;
                            case 1:
                                Assert.True(DivRem(b1, b2, results[20][1]), " Verification Failed");
                                break;
                            case 2:
                                Assert.True(DivRem(b1, b3, results[20][2]), " Verification Failed");
                                break;
                            case 3:
                                Assert.True(DivRem(b2, b1, results[20][3]), " Verification Failed");
                                break;
                            case 4:
                                Assert.True(DivRem(b2, b2, results[20][4]), " Verification Failed");
                                break;
                            case 5:
                                Assert.True(DivRem(b2, b3, results[20][5]), " Verification Failed");
                                break;
                            case 6:
                                Assert.True(DivRem(b3, b1, results[20][6]), " Verification Failed");
                                break;
                            case 7:
                                Assert.True(DivRem(b3, b2, results[20][7]), " Verification Failed");
                                break;
                            case 8:
                                Assert.True(DivRem(b3, b3, results[20][8]), " Verification Failed");
                                break;
                            default:
                                Console.WriteLine("Invalid bigInt selected");
                                ret = false;
                                break;
                        }
                        break;
                    case 21:
                        switch (order % 9)
                        {
                            case 0:
                                Assert.True(Remainder(b1, b1, results[21][0]), " Verification Failed");
                                break;
                            case 1:
                                Assert.True(Remainder(b1, b2, results[21][1]), " Verification Failed");
                                break;
                            case 2:
                                Assert.True(Remainder(b1, b3, results[21][2]), " Verification Failed");
                                break;
                            case 3:
                                Assert.True(Remainder(b2, b1, results[21][3]), " Verification Failed");
                                break;
                            case 4:
                                Assert.True(Remainder(b2, b2, results[21][4]), " Verification Failed");
                                break;
                            case 5:
                                Assert.True(Remainder(b2, b3, results[21][5]), " Verification Failed");
                                break;
                            case 6:
                                Assert.True(Remainder(b3, b1, results[21][6]), " Verification Failed");
                                break;
                            case 7:
                                Assert.True(Remainder(b3, b2, results[21][7]), " Verification Failed");
                                break;
                            case 8:
                                Assert.True(Remainder(b3, b3, results[21][8]), " Verification Failed");
                                break;
                            default:
                                Console.WriteLine("Invalid bigInt selected");
                                ret = false;
                                break;
                        }
                        break;
                    case 22:
                        switch (order % 9)
                        {
                            case 0:
                                Assert.True(Op_Modulus(b1, b1, results[22][0]), " Verification Failed");
                                break;
                            case 1:
                                Assert.True(Op_Modulus(b1, b2, results[22][1]), " Verification Failed");
                                break;
                            case 2:
                                Assert.True(Op_Modulus(b1, b3, results[22][2]), " Verification Failed");
                                break;
                            case 3:
                                Assert.True(Op_Modulus(b2, b1, results[22][3]), " Verification Failed");
                                break;
                            case 4:
                                Assert.True(Op_Modulus(b2, b2, results[22][4]), " Verification Failed");
                                break;
                            case 5:
                                Assert.True(Op_Modulus(b2, b3, results[22][5]), " Verification Failed");
                                break;
                            case 6:
                                Assert.True(Op_Modulus(b3, b1, results[22][6]), " Verification Failed");
                                break;
                            case 7:
                                Assert.True(Op_Modulus(b3, b2, results[22][7]), " Verification Failed");
                                break;
                            case 8:
                                Assert.True(Op_Modulus(b3, b3, results[22][8]), " Verification Failed");
                                break;
                            default:
                                Console.WriteLine("Invalid bigInt selected");
                                ret = false;
                                break;
                        }
                        break;
                    case 23:
                        switch (order % 9)
                        {
                            case 0:
                                Assert.True(Divide(b1, b1, results[23][0]), " Verification Failed");
                                break;
                            case 1:
                                Assert.True(Divide(b1, b2, results[23][1]), " Verification Failed");
                                break;
                            case 2:
                                Assert.True(Divide(b1, b3, results[23][2]), " Verification Failed");
                                break;
                            case 3:
                                Assert.True(Divide(b2, b1, results[23][3]), " Verification Failed");
                                break;
                            case 4:
                                Assert.True(Divide(b2, b2, results[23][4]), " Verification Failed");
                                break;
                            case 5:
                                Assert.True(Divide(b2, b3, results[23][5]), " Verification Failed");
                                break;
                            case 6:
                                Assert.True(Divide(b3, b1, results[23][6]), " Verification Failed");
                                break;
                            case 7:
                                Assert.True(Divide(b3, b2, results[23][7]), " Verification Failed");
                                break;
                            case 8:
                                Assert.True(Divide(b3, b3, results[23][8]), " Verification Failed");
                                break;
                            default:
                                Console.WriteLine("Invalid bigInt selected");
                                ret = false;
                                break;
                        }
                        break;
                    case 24:
                        switch (order % 9)
                        {
                            case 0:
                                Assert.True(Op_Divide(b1, b1, results[24][0]), " Verification Failed");
                                break;
                            case 1:
                                Assert.True(Op_Divide(b1, b2, results[24][1]), " Verification Failed");
                                break;
                            case 2:
                                Assert.True(Op_Divide(b1, b3, results[24][2]), " Verification Failed");
                                break;
                            case 3:
                                Assert.True(Op_Divide(b2, b1, results[24][3]), " Verification Failed");
                                break;
                            case 4:
                                Assert.True(Op_Divide(b2, b2, results[24][4]), " Verification Failed");
                                break;
                            case 5:
                                Assert.True(Op_Divide(b2, b3, results[24][5]), " Verification Failed");
                                break;
                            case 6:
                                Assert.True(Op_Divide(b3, b1, results[24][6]), " Verification Failed");
                                break;
                            case 7:
                                Assert.True(Op_Divide(b3, b2, results[24][7]), " Verification Failed");
                                break;
                            case 8:
                                Assert.True(Op_Divide(b3, b3, results[24][8]), " Verification Failed");
                                break;
                            default:
                                Console.WriteLine("Invalid bigInt selected");
                                ret = false;
                                break;
                        }
                        break;
                    case 25:
                        switch (order % 9)
                        {
                            case 0:
                                Assert.True(Multiply(b1, b1, results[25][0]), " Verification Failed");
                                break;
                            case 1:
                                Assert.True(Multiply(b1, b2, results[25][1]), " Verification Failed");
                                break;
                            case 2:
                                Assert.True(Multiply(b1, b3, results[25][2]), " Verification Failed");
                                break;
                            case 3:
                                Assert.True(Multiply(b2, b1, results[25][3]), " Verification Failed");
                                break;
                            case 4:
                                Assert.True(Multiply(b2, b2, results[25][4]), " Verification Failed");
                                break;
                            case 5:
                                Assert.True(Multiply(b2, b3, results[25][5]), " Verification Failed");
                                break;
                            case 6:
                                Assert.True(Multiply(b3, b1, results[25][6]), " Verification Failed");
                                break;
                            case 7:
                                Assert.True(Multiply(b3, b2, results[25][7]), " Verification Failed");
                                break;
                            case 8:
                                Assert.True(Multiply(b3, b3, results[25][8]), " Verification Failed");
                                break;
                            default:
                                Console.WriteLine("Invalid bigInt selected");
                                ret = false;
                                break;
                        }
                        break;
                    case 26:
                        switch (order % 9)
                        {
                            case 0:
                                Assert.True(Op_Multiply(b1, b1, results[26][0]), " Verification Failed");
                                break;
                            case 1:
                                Assert.True(Op_Multiply(b1, b2, results[26][1]), " Verification Failed");
                                break;
                            case 2:
                                Assert.True(Op_Multiply(b1, b3, results[26][2]), " Verification Failed");
                                break;
                            case 3:
                                Assert.True(Op_Multiply(b2, b1, results[26][3]), " Verification Failed");
                                break;
                            case 4:
                                Assert.True(Op_Multiply(b2, b2, results[26][4]), " Verification Failed");
                                break;
                            case 5:
                                Assert.True(Op_Multiply(b2, b3, results[26][5]), " Verification Failed");
                                break;
                            case 6:
                                Assert.True(Op_Multiply(b3, b1, results[26][6]), " Verification Failed");
                                break;
                            case 7:
                                Assert.True(Op_Multiply(b3, b2, results[26][7]), " Verification Failed");
                                break;
                            case 8:
                                Assert.True(Op_Multiply(b3, b3, results[26][8]), " Verification Failed");
                                break;
                            default:
                                Console.WriteLine("Invalid bigInt selected");
                                ret = false;
                                break;
                        }
                        break;
                    case 27:
                        switch (order % 9)
                        {
                            case 0:
                                Assert.True(Subtract(b1, b1, results[27][0]), " Verification Failed");
                                break;
                            case 1:
                                Assert.True(Subtract(b1, b2, results[27][1]), " Verification Failed");
                                break;
                            case 2:
                                Assert.True(Subtract(b1, b3, results[27][2]), " Verification Failed");
                                break;
                            case 3:
                                Assert.True(Subtract(b2, b1, results[27][3]), " Verification Failed");
                                break;
                            case 4:
                                Assert.True(Subtract(b2, b2, results[27][4]), " Verification Failed");
                                break;
                            case 5:
                                Assert.True(Subtract(b2, b3, results[27][5]), " Verification Failed");
                                break;
                            case 6:
                                Assert.True(Subtract(b3, b1, results[27][6]), " Verification Failed");
                                break;
                            case 7:
                                Assert.True(Subtract(b3, b2, results[27][7]), " Verification Failed");
                                break;
                            case 8:
                                Assert.True(Subtract(b3, b3, results[27][8]), " Verification Failed");
                                break;
                            default:
                                Console.WriteLine("Invalid bigInt selected");
                                ret = false;
                                break;
                        }
                        break;
                    case 28:
                        switch (order % 9)
                        {
                            case 0:
                                Assert.True(Op_Subtract(b1, b1, results[28][0]), " Verification Failed");
                                break;
                            case 1:
                                Assert.True(Op_Subtract(b1, b2, results[28][1]), " Verification Failed");
                                break;
                            case 2:
                                Assert.True(Op_Subtract(b1, b3, results[28][2]), " Verification Failed");
                                break;
                            case 3:
                                Assert.True(Op_Subtract(b2, b1, results[28][3]), " Verification Failed");
                                break;
                            case 4:
                                Assert.True(Op_Subtract(b2, b2, results[28][4]), " Verification Failed");
                                break;
                            case 5:
                                Assert.True(Op_Subtract(b2, b3, results[28][5]), " Verification Failed");
                                break;
                            case 6:
                                Assert.True(Op_Subtract(b3, b1, results[28][6]), " Verification Failed");
                                break;
                            case 7:
                                Assert.True(Op_Subtract(b3, b2, results[28][7]), " Verification Failed");
                                break;
                            case 8:
                                Assert.True(Op_Subtract(b3, b3, results[28][8]), " Verification Failed");
                                break;
                            default:
                                Console.WriteLine("Invalid bigInt selected");
                                ret = false;
                                break;
                        }
                        break;
                    case 29:
                        switch (order % 9)
                        {
                            case 0:
                                Assert.True(Add(b1, b1, results[29][0]), " Verification Failed");
                                break;
                            case 1:
                                Assert.True(Add(b1, b2, results[29][1]), " Verification Failed");
                                break;
                            case 2:
                                Assert.True(Add(b1, b3, results[29][2]), " Verification Failed");
                                break;
                            case 3:
                                Assert.True(Add(b2, b1, results[29][3]), " Verification Failed");
                                break;
                            case 4:
                                Assert.True(Add(b2, b2, results[29][4]), " Verification Failed");
                                break;
                            case 5:
                                Assert.True(Add(b2, b3, results[29][5]), " Verification Failed");
                                break;
                            case 6:
                                Assert.True(Add(b3, b1, results[29][6]), " Verification Failed");
                                break;
                            case 7:
                                Assert.True(Add(b3, b2, results[29][7]), " Verification Failed");
                                break;
                            case 8:
                                Assert.True(Add(b3, b3, results[29][8]), " Verification Failed");
                                break;
                            default:
                                Console.WriteLine("Invalid bigInt selected");
                                ret = false;
                                break;
                        }
                        break;
                    case 30:
                        switch (order % 9)
                        {
                            case 0:
                                Assert.True(Op_Add(b1, b1, results[30][0]), " Verification Failed");
                                break;
                            case 1:
                                Assert.True(Op_Add(b1, b2, results[30][1]), " Verification Failed");
                                break;
                            case 2:
                                Assert.True(Op_Add(b1, b3, results[30][2]), " Verification Failed");
                                break;
                            case 3:
                                Assert.True(Op_Add(b2, b1, results[30][3]), " Verification Failed");
                                break;
                            case 4:
                                Assert.True(Op_Add(b2, b2, results[30][4]), " Verification Failed");
                                break;
                            case 5:
                                Assert.True(Op_Add(b2, b3, results[30][5]), " Verification Failed");
                                break;
                            case 6:
                                Assert.True(Op_Add(b3, b1, results[30][6]), " Verification Failed");
                                break;
                            case 7:
                                Assert.True(Op_Add(b3, b2, results[30][7]), " Verification Failed");
                                break;
                            case 8:
                                Assert.True(Op_Add(b3, b3, results[30][8]), " Verification Failed");
                                break;
                            default:
                                Console.WriteLine("Invalid bigInt selected");
                                ret = false;
                                break;
                        }
                        break;
                    case 31:
                        switch (order % 27)
                        {
                            case 0:
                                Assert.True(ModPow(b1, b1, b1, results[31][0]), " Verification Failed");
                                break;
                            case 1:
                                Assert.True(ModPow(b1, b1, b2, results[31][1]), " Verification Failed");
                                break;
                            case 2:
                                Assert.True(ModPow(b1, b1, b3, results[31][2]), " Verification Failed");
                                break;
                            case 3:
                                Assert.True(ModPow(b1, b2, b1, results[31][3]), " Verification Failed");
                                break;
                            case 4:
                                Assert.True(ModPow(b1, b2, b2, results[31][4]), " Verification Failed");
                                break;
                            case 5:
                                Assert.True(ModPow(b1, b2, b3, results[31][5]), " Verification Failed");
                                break;
                            case 6:
                                Assert.True(ModPow(b1, b3, b1, results[31][6]), " Verification Failed");
                                break;
                            case 7:
                                Assert.True(ModPow(b1, b3, b2, results[31][7]), " Verification Failed");
                                break;
                            case 8:
                                Assert.True(ModPow(b1, b3, b3, results[31][8]), " Verification Failed");
                                break;
                            case 9:
                                Assert.True(ModPow(b2, b1, b1, results[31][9]), " Verification Failed");
                                break;
                            case 10:
                                Assert.True(ModPow(b2, b1, b2, results[31][10]), " Verification Failed");
                                break;
                            case 11:
                                Assert.True(ModPow(b2, b1, b3, results[31][11]), " Verification Failed");
                                break;
                            case 12:
                                Assert.True(ModPow(b2, b2, b1, results[31][12]), " Verification Failed");
                                break;
                            case 13:
                                Assert.True(ModPow(b2, b2, b2, results[31][13]), " Verification Failed");
                                break;
                            case 14:
                                Assert.True(ModPow(b2, b2, b3, results[31][14]), " Verification Failed");
                                break;
                            case 15:
                                Assert.True(ModPow(b2, b3, b1, results[31][15]), " Verification Failed");
                                break;
                            case 16:
                                Assert.True(ModPow(b2, b3, b2, results[31][16]), " Verification Failed");
                                break;
                            case 17:
                                Assert.True(ModPow(b2, b3, b3, results[31][17]), " Verification Failed");
                                break;
                            case 18:
                                Assert.True(ModPow(b3, b1, b1, results[31][18]), " Verification Failed");
                                break;
                            case 19:
                                Assert.True(ModPow(b3, b1, b2, results[31][19]), " Verification Failed");
                                break;
                            case 20:
                                Assert.True(ModPow(b3, b1, b3, results[31][20]), " Verification Failed");
                                break;
                            case 21:
                                Assert.True(ModPow(b3, b2, b1, results[31][21]), " Verification Failed");
                                break;
                            case 22:
                                Assert.True(ModPow(b3, b2, b2, results[31][22]), " Verification Failed");
                                break;
                            case 23:
                                Assert.True(ModPow(b3, b2, b3, results[31][23]), " Verification Failed");
                                break;
                            case 24:
                                Assert.True(ModPow(b3, b3, b1, results[31][24]), " Verification Failed");
                                break;
                            case 25:
                                Assert.True(ModPow(b3, b3, b2, results[31][25]), " Verification Failed");
                                break;
                            case 26:
                                Assert.True(ModPow(b3, b3, b3, results[31][26]), " Verification Failed");
                                break;
                            default:
                                Console.WriteLine("Invalid bigInt selected");
                                ret = false;
                                break;
                        }
                        break;
                    default:
                        Console.WriteLine("Invalid operation selected");
                        ret = false;
                        break;
                }
                if (!corrupt & !ret)
                {
                    Console.WriteLine("ERROR::: Cycle {0} corrupted with operation {1} on order {2}", id, op, order);
                    corrupt = true;
                    break;
                }
            }

            done = true;
        }

        public bool Sign(BigInteger a, BigInteger expected)
        {
            return (a.Sign == expected);
        }
        public bool Op_Not(BigInteger a, BigInteger expected)
        {
            return (~a == expected);
        }
        public bool Log10(BigInteger a, BigInteger expected)
        {
            return (MyBigIntImp.ApproximateBigInteger(BigInteger.Log10(a)) == expected);
        }
        public bool Log(BigInteger a, BigInteger expected)
        {
            return (MyBigIntImp.ApproximateBigInteger(BigInteger.Log(a)) == expected);
        }
        public bool Abs(BigInteger a, BigInteger expected)
        {
            return (BigInteger.Abs(a) == expected);
        }
        public bool Op_Decrement(BigInteger a, BigInteger expected)
        {
            return (--a == expected);
        }
        public bool Op_Increment(BigInteger a, BigInteger expected)
        {
            return (++a == expected);
        }
        public bool Negate(BigInteger a, BigInteger expected)
        {
            return (BigInteger.Negate(a) == expected);
        }
        public bool Op_Negate(BigInteger a, BigInteger expected)
        {
            return (-a == expected);
        }
        public bool Op_Plus(BigInteger a, BigInteger expected)
        {
            return (+a == expected);
        }
        public bool Min(BigInteger a, BigInteger b, BigInteger expected)
        {
            return (BigInteger.Min(a, b) == expected);
        }
        public bool Max(BigInteger a, BigInteger b, BigInteger expected)
        {
            return (BigInteger.Max(a, b) == expected);
        }
        public bool Op_RightShift(BigInteger a, BigInteger b, BigInteger expected)
        {
            return ((a >> Makeint(b)) == expected);
        }
        public bool Op_LeftShift(BigInteger a, BigInteger b, BigInteger expected)
        {
            return ((a << Makeint(b)) == expected);
        }
        public bool Op_Xor(BigInteger a, BigInteger b, BigInteger expected)
        {
            return ((a ^ b) == expected);
        }
        public bool Op_Or(BigInteger a, BigInteger b, BigInteger expected)
        {
            return ((a | b) == expected);
        }
        public bool Op_And(BigInteger a, BigInteger b, BigInteger expected)
        {
            return ((a & b) == expected);
        }
        public bool Log(BigInteger a, BigInteger b, BigInteger expected)
        {
            return (MyBigIntImp.ApproximateBigInteger(BigInteger.Log(a, (double)b)) == expected);
        }
        public bool GCD(BigInteger a, BigInteger b, BigInteger expected)
        {
            return (BigInteger.GreatestCommonDivisor(a, b) == expected);
        }
        public bool Pow(BigInteger a, BigInteger b, BigInteger expected)
        {
            b = Makeint(b);
            if (b < 0) b = -b;
            return (BigInteger.Pow(a, (int)b) == expected);
        }
        public bool DivRem(BigInteger a, BigInteger b, BigInteger expected)
        {
            BigInteger c = 0;
            return (BigInteger.DivRem(a, b, out c) == expected);
        }
        public bool Remainder(BigInteger a, BigInteger b, BigInteger expected)
        {
            return (BigInteger.Remainder(a, b) == expected);
        }
        public bool Op_Modulus(BigInteger a, BigInteger b, BigInteger expected)
        {
            return ((a % b) == expected);
        }
        public bool Divide(BigInteger a, BigInteger b, BigInteger expected)
        {
            return (BigInteger.Divide(a, b) == expected);
        }
        public bool Op_Divide(BigInteger a, BigInteger b, BigInteger expected)
        {
            return ((a / b) == expected);
        }
        public bool Multiply(BigInteger a, BigInteger b, BigInteger expected)
        {
            return (BigInteger.Multiply(a, b) == expected);
        }
        public bool Op_Multiply(BigInteger a, BigInteger b, BigInteger expected)
        {
            return ((a * b) == expected);
        }
        public bool Subtract(BigInteger a, BigInteger b, BigInteger expected)
        {
            return (BigInteger.Subtract(a, b) == expected);
        }
        public bool Op_Subtract(BigInteger a, BigInteger b, BigInteger expected)
        {
            return ((a - b) == expected);
        }
        public bool Add(BigInteger a, BigInteger b, BigInteger expected)
        {
            return (BigInteger.Add(a, b) == expected);
        }
        public bool Op_Add(BigInteger a, BigInteger b, BigInteger expected)
        {
            return ((a + b) == expected);
        }
        public bool ModPow(BigInteger a, BigInteger b, BigInteger c, BigInteger expected)
        {
            if (b < 0) b = -b;
            return (BigInteger.ModPow(a, b, c) == expected);
        }

        private int Makeint(BigInteger input)
        {
            int output;

            if (input < 0) input = -input;
            input = input + int.MaxValue;

            byte[] temp = input.ToByteArray();
            temp[1] = 0;
            temp[2] = 0;
            temp[3] = 0;

            output = BitConverter.ToInt32(temp, 0);
            if (output == 0) output = 1;

            return output;
        }
    }
}

