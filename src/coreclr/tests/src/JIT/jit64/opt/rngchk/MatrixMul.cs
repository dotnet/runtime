// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace MatrixMul
{
    internal class Test
    {
        //Test shall throw IndexOutOfRangeException if rangecheck is inserted properly
        private static int Main()
        {
            int retVal = 100;
            try
            {
                MatrixMul();
            }
            catch (System.IndexOutOfRangeException)
            {
                retVal = 1;
            }
            return retVal;
        }

        private static void MatrixMul()
        {
            int[][] matrixA = new int[][]
                        {
                            new int[] {1,2,3},
                            new int[] {4,5,6}
                        };
            int[][] matrixB = new int[][]
                        {
                            new int[] {9,10},
                            new int[] {11,12},
                            new int[] {13,14}
                        };
            int[][] resultMatrix = new int[matrixA.Length][];

            int i, j, k;
            for (i = 0; i < matrixA.Length; i++)
            {
                resultMatrix[i] = new int[matrixB.Length];
                for (j = 0; j < matrixB[i].Length; j++)
                {
                    for (k = 0; k < matrixB.Length; k++)
                    {
                        resultMatrix[i][j] += matrixA[i][k] * matrixB[k][j];
                    }
                }
            }
        }
    }
}
