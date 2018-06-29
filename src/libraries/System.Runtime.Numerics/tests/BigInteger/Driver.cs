// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
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
                Worker worker = new Worker(new Random(s_random.Next()), i);
                worker.DoWork();
                Assert.True(worker.Valid, "Verification Failed");
            }
        }

        private static byte[] GetRandomByteArray(Random random)
        {
            return MyBigIntImp.GetNonZeroRandomByteArray(random, random.Next(1, 18));
        }

        private static int Makeint(BigInteger input)
        {
            int output;

            if (input < 0)
            {
                input = -input;
            }
            input = input + int.MaxValue;

            byte[] temp = input.ToByteArray();
            temp[1] = 0;
            temp[2] = 0;
            temp[3] = 0;

            output = BitConverter.ToInt32(temp, 0);
            if (output == 0)
            {
                output = 1;
            }

            return output;
        }
    }

    public class Worker
    {
        private Random random;
        private int id;

        public bool Valid
        {
            get;
            set;
        }

        public Worker(Random r, int i)
        {
            random = r;
            id = i;
        }

        public void DoWork()
        {
            Valid = true;

            BigInteger b1 = Driver.b1;
            BigInteger b2 = Driver.b2;
            BigInteger b3 = Driver.b3;
            BigInteger[][] results = Driver.results;
            
            var threeOrderOperations = new Action<BigInteger, BigInteger>[] { 
                new Action<BigInteger, BigInteger>((a, expected) => { Sign(a, expected); }),
                new Action<BigInteger, BigInteger>((a, expected) => { Op_Not(a, expected); }),
                new Action<BigInteger, BigInteger>((a, expected) => { Log10(a, expected); }),
                new Action<BigInteger, BigInteger>((a, expected) => { Log(a, expected); }),
                new Action<BigInteger, BigInteger>((a, expected) => { Abs(a, expected); }),
                new Action<BigInteger, BigInteger>((a, expected) => { Op_Decrement(a, expected); }),
                new Action<BigInteger, BigInteger>((a, expected) => { Op_Increment(a, expected); }),
                new Action<BigInteger, BigInteger>((a, expected) => { Negate(a, expected); }),
                new Action<BigInteger, BigInteger>((a, expected) => { Op_Negate(a, expected); }),
                new Action<BigInteger, BigInteger>((a, expected) => { Op_Plus(a, expected); })
            };

            var nineOrderOperations = new Action<BigInteger, BigInteger, BigInteger>[] { 
                new Action<BigInteger, BigInteger, BigInteger>((a, b, expected) => { Min(a, b, expected); }),
                new Action<BigInteger, BigInteger, BigInteger>((a, b, expected) => { Max(a, b, expected); }),
                new Action<BigInteger, BigInteger, BigInteger>((a, b, expected) => { Op_RightShift(a, b, expected); }),
                new Action<BigInteger, BigInteger, BigInteger>((a, b, expected) => { Op_LeftShift(a, b, expected); }),
                new Action<BigInteger, BigInteger, BigInteger>((a, b, expected) => { Op_Xor(a, b, expected); }),
                new Action<BigInteger, BigInteger, BigInteger>((a, b, expected) => { Op_Or(a, b, expected); }),
                new Action<BigInteger, BigInteger, BigInteger>((a, b, expected) => { Op_And(a, b, expected); }),
                new Action<BigInteger, BigInteger, BigInteger>((a, b, expected) => { Log(a, b, expected); }),
                new Action<BigInteger, BigInteger, BigInteger>((a, b, expected) => { GCD(a, b, expected); }),
                new Action<BigInteger, BigInteger, BigInteger>((a, b, expected) => { Pow(a, b, expected); }),
                new Action<BigInteger, BigInteger, BigInteger>((a, b, expected) => { DivRem(a, b, expected); }),
                new Action<BigInteger, BigInteger, BigInteger>((a, b, expected) => { Remainder(a, b, expected); }),
                new Action<BigInteger, BigInteger, BigInteger>((a, b, expected) => { Op_Modulus(a, b, expected); }),
                new Action<BigInteger, BigInteger, BigInteger>((a, b, expected) => { Divide(a, b, expected); }),
                new Action<BigInteger, BigInteger, BigInteger>((a, b, expected) => { Op_Divide(a, b, expected); }),
                new Action<BigInteger, BigInteger, BigInteger>((a, b, expected) => { Multiply(a, b, expected); }),
                new Action<BigInteger, BigInteger, BigInteger>((a, b, expected) => { Op_Multiply(a, b, expected); }),
                new Action<BigInteger, BigInteger, BigInteger>((a, b, expected) => { Subtract(a, b, expected); }),
                new Action<BigInteger, BigInteger, BigInteger>((a, b, expected) => { Op_Subtract(a, b, expected); }),
                new Action<BigInteger, BigInteger, BigInteger>((a, b, expected) => { Add(a, b, expected); }),
                new Action<BigInteger, BigInteger, BigInteger>((a, b, expected) => { Op_Add(a, b, expected); })
            };

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            while (stopWatch.ElapsedMilliseconds < 10000)
            {
                // Remove the Pow operation for performance reasons
                int op;
                do
                {
                    op = random.Next(0, 32);
                }
                while (op == 19);


                int order = random.Next(0, 27);
                switch (op)
                {
                    case 0: // Sign
                    case 1: // Op_Not
                    case 2: // Log10
                    case 3: // Log
                    case 4: // Abs
                    case 5: // Op_Decrement
                    case 6: // Op_Increment
                    case 7: // Negate
                    case 8: // Op_Negate
                    case 9: // Op_Plus
                        switch (order % 3)
                        {
                            case 0:
                                threeOrderOperations[op](b1, results[op][0]);
                                break;
                            case 1:
                                threeOrderOperations[op](b2, results[op][1]);
                                break;
                            case 2:
                                threeOrderOperations[op](b3, results[op][2]);
                                break;
                            default:
                                Valid = false;
                                break;
                        }
                        break;
                    case 10: // Min
                    case 11: // Max
                    case 12: // Op_RightShift
                    case 13: // Op_LeftShift
                    case 14: // Op_Xor
                    case 15: // Op_Or
                    case 16: // Op_And
                    case 17: // Log
                    case 18: // GCD
                    case 19: // Pow
                    case 20: // DivRem
                    case 21: // Remainder
                    case 22: // Op_Modulus
                    case 23: // Divide
                    case 24: // Op_Divide
                    case 25: // Multiply
                    case 26: // Op_Multiply
                    case 27: // Subtract
                    case 28: // Op_Subtract
                    case 29: // Add
                    case 30: // Op_Add
                        switch (order % 9)
                        {
                            case 0:
                                nineOrderOperations[op-10](b1, b1, results[op][0]);
                                break;
                            case 1:
                                nineOrderOperations[op-10](b1, b2, results[op][1]);
                                break;
                            case 2:
                                nineOrderOperations[op-10](b1, b3, results[op][2]);
                                break;
                            case 3:
                                nineOrderOperations[op-10](b2, b1, results[op][3]);
                                break;
                            case 4:
                                nineOrderOperations[op-10](b2, b2, results[op][4]);
                                break;
                            case 5:
                                nineOrderOperations[op-10](b2, b3, results[op][5]);
                                break;
                            case 6:
                                nineOrderOperations[op-10](b3, b1, results[op][6]);
                                break;
                            case 7:
                                nineOrderOperations[op-10](b3, b2, results[op][7]);
                                break;
                            case 8:
                                nineOrderOperations[op-10](b3, b3, results[op][8]);
                                break;
                            default:
                                Valid = false;
                                break;
                        }
                        break;
                    case 31:
                        switch (order % 27)
                        {
                            case 0:
                                ModPow(b1, b1, b1, results[31][0]);
                                break;
                            case 1:
                                ModPow(b1, b1, b2, results[31][1]);
                                break;
                            case 2:
                                ModPow(b1, b1, b3, results[31][2]);
                                break;
                            case 3:
                                ModPow(b1, b2, b1, results[31][3]);
                                break;
                            case 4:
                                ModPow(b1, b2, b2, results[31][4]);
                                break;
                            case 5:
                                ModPow(b1, b2, b3, results[31][5]);
                                break;
                            case 6:
                                ModPow(b1, b3, b1, results[31][6]);
                                break;
                            case 7:
                                ModPow(b1, b3, b2, results[31][7]);
                                break;
                            case 8:
                                ModPow(b1, b3, b3, results[31][8]);
                                break;
                            case 9:
                                ModPow(b2, b1, b1, results[31][9]);
                                break;
                            case 10:
                                ModPow(b2, b1, b2, results[31][10]);
                                break;
                            case 11:
                                ModPow(b2, b1, b3, results[31][11]);
                                break;
                            case 12:
                                ModPow(b2, b2, b1, results[31][12]);
                                break;
                            case 13:
                                ModPow(b2, b2, b2, results[31][13]);
                                break;
                            case 14:
                                ModPow(b2, b2, b3, results[31][14]);
                                break;
                            case 15:
                                ModPow(b2, b3, b1, results[31][15]);
                                break;
                            case 16:
                                ModPow(b2, b3, b2, results[31][16]);
                                break;
                            case 17:
                                ModPow(b2, b3, b3, results[31][17]);
                                break;
                            case 18:
                                ModPow(b3, b1, b1, results[31][18]);
                                break;
                            case 19:
                                ModPow(b3, b1, b2, results[31][19]);
                                break;
                            case 20:
                                ModPow(b3, b1, b3, results[31][20]);
                                break;
                            case 21:
                                ModPow(b3, b2, b1, results[31][21]);
                                break;
                            case 22:
                                ModPow(b3, b2, b2, results[31][22]);
                                break;
                            case 23:
                                ModPow(b3, b2, b3, results[31][23]);
                                break;
                            case 24:
                                ModPow(b3, b3, b1, results[31][24]);
                                break;
                            case 25:
                                ModPow(b3, b3, b2, results[31][25]);
                                break;
                            case 26:
                                ModPow(b3, b3, b3, results[31][26]);
                                break;
                            default:
                                Valid = false;
                                break;
                        }
                        break;
                    default:
                        Valid = false;
                        break;
                }

                Assert.True(Valid, string.Format("Cycle {0} corrupted with operation {1} on order {2}", id, op, order));
            }
        }

        private void Sign(BigInteger a, BigInteger expected)
        {
            Assert.Equal(a.Sign, expected);
        }

        private void Op_Not(BigInteger a, BigInteger expected)
        {
            Assert.Equal(~a, expected);
        }

        private void Log10(BigInteger a, BigInteger expected)
        {
            Assert.Equal(MyBigIntImp.ApproximateBigInteger(BigInteger.Log10(a)), expected);
        }

        private void Log(BigInteger a, BigInteger expected)
        {
            Assert.Equal(MyBigIntImp.ApproximateBigInteger(BigInteger.Log(a)), expected);
        }

        private void Abs(BigInteger a, BigInteger expected)
        {
            Assert.Equal(BigInteger.Abs(a), expected);
        }

        private void Op_Decrement(BigInteger a, BigInteger expected)
        {
            Assert.Equal(--a, expected);
        }

        private void Op_Increment(BigInteger a, BigInteger expected)
        {
            Assert.Equal(++a, expected);
        }

        private void Negate(BigInteger a, BigInteger expected)
        {
            Assert.Equal(BigInteger.Negate(a), expected);
        }

        private void Op_Negate(BigInteger a, BigInteger expected)
        {
            Assert.Equal(-a, expected);
        }

        private void Op_Plus(BigInteger a, BigInteger expected)
        {
            Assert.Equal(+a, expected);
        }

        private void Min(BigInteger a, BigInteger b, BigInteger expected)
        {
            Assert.Equal(BigInteger.Min(a, b), expected);
        }

        private void Max(BigInteger a, BigInteger b, BigInteger expected)
        {
            Assert.Equal(BigInteger.Max(a, b), expected);
        }

        private void Op_RightShift(BigInteger a, BigInteger b, BigInteger expected)
        {
            Assert.Equal((a >> MakeInt32(b)), expected);
        }

        private void Op_LeftShift(BigInteger a, BigInteger b, BigInteger expected)
        {
            Assert.Equal((a << MakeInt32(b)), expected);
        }

        private void Op_Xor(BigInteger a, BigInteger b, BigInteger expected)
        {
            Assert.Equal((a ^ b), expected);
        }

        private void Op_Or(BigInteger a, BigInteger b, BigInteger expected)
        {
            Assert.Equal((a | b), expected);
        }

        private void Op_And(BigInteger a, BigInteger b, BigInteger expected)
        {
            Assert.Equal((a & b), expected);
        }

        private void Log(BigInteger a, BigInteger b, BigInteger expected)
        {
            Assert.Equal(MyBigIntImp.ApproximateBigInteger(BigInteger.Log(a, (double)b)), expected);
        }

        private void GCD(BigInteger a, BigInteger b, BigInteger expected)
        {
            Assert.Equal(BigInteger.GreatestCommonDivisor(a, b), expected);
        }

        public bool Pow(BigInteger a, BigInteger b, BigInteger expected)
        {
            b = MakeInt32(b);
            if (b < 0)
            {
                b = -b;
            }
            return (BigInteger.Pow(a, (int)b) == expected);
        }

        public bool DivRem(BigInteger a, BigInteger b, BigInteger expected)
        {
            BigInteger c = 0;
            return (BigInteger.DivRem(a, b, out c) == expected);
        }

        private void Remainder(BigInteger a, BigInteger b, BigInteger expected)
        {
            Assert.Equal(BigInteger.Remainder(a, b), expected);
        }

        private void Op_Modulus(BigInteger a, BigInteger b, BigInteger expected)
        {
            Assert.Equal((a % b), expected);
        }

        private void Divide(BigInteger a, BigInteger b, BigInteger expected)
        {
            Assert.Equal(BigInteger.Divide(a, b), expected);
        }

        private void Op_Divide(BigInteger a, BigInteger b, BigInteger expected)
        {
            Assert.Equal((a / b), expected);
        }

        private void Multiply(BigInteger a, BigInteger b, BigInteger expected)
        {
            Assert.Equal(BigInteger.Multiply(a, b), expected);
        }

        private void Op_Multiply(BigInteger a, BigInteger b, BigInteger expected)
        {
            Assert.Equal((a * b), expected);
        }

        private void Subtract(BigInteger a, BigInteger b, BigInteger expected)
        {
            Assert.Equal(BigInteger.Subtract(a, b), expected);
        }

        private void Op_Subtract(BigInteger a, BigInteger b, BigInteger expected)
        {
            Assert.Equal((a - b), expected);
        }

        private void Add(BigInteger a, BigInteger b, BigInteger expected)
        {
            Assert.Equal(BigInteger.Add(a, b), expected);
        }

        private void Op_Add(BigInteger a, BigInteger b, BigInteger expected)
        {
            Assert.Equal((a + b), expected);
        }

        public bool ModPow(BigInteger a, BigInteger b, BigInteger c, BigInteger expected)
        {
            if (b < 0)
            {
                b = -b;
            }
            return (BigInteger.ModPow(a, b, c) == expected);
        }

        private int MakeInt32(BigInteger input)
        {
            int output;

            if (input < 0)
            {
                input = -input;
            }
            input = input + int.MaxValue;

            byte[] temp = input.ToByteArray();
            temp[1] = 0;
            temp[2] = 0;
            temp[3] = 0;

            output = BitConverter.ToInt32(temp, 0);
            if (output == 0)
            {
                output = 1;
            }

            return output;
        }
    }
}

