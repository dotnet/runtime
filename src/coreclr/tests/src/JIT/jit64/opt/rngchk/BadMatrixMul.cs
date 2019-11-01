// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

namespace SimpleArray_01
{
    public delegate void RngTest();
    internal class Class1
    {
        private static int Main()
        {
            int retVal = 100;
            int testNum = 0;
            RngTest[] Tests ={  new RngTest(Test.BadMatrixMul1),
                                new RngTest (Test.BadMatrixMul2),
                                new RngTest (Test.BadMatrixMul3)};

            foreach (RngTest test in Tests)
            {
                testNum++;
                if (DoTest(test))
                {
                    Console.WriteLine("Test {0} Passed", testNum);
                }
                else
                {
                    Console.WriteLine("Test {0} Failed", testNum);
                    retVal = 1;
                }
            }
            return retVal;
        }

        //Test shall throw IndexOutOfRangeException if rangecheck is inserted properly
        private static bool DoTest(RngTest Test)
        {
            bool bResult = false;
            try
            {
                Test();
            }
            catch (System.IndexOutOfRangeException)
            {
                bResult = true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return bResult;
        }
    }
    internal class Test
    {
        public static void BadMatrixMul1()
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
                            new int[] {13,14},
                            new int[] {15,16}
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

        public static void BadMatrixMul2()
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
                            new int[] {13,14},
                        };
            int[][] resultMatrix = new int[matrixA.Length][];

            int i, j, k;
            for (i = 0; i < matrixA.Length; i++)
            {
                resultMatrix[i] = new int[matrixB.Length];
                for (j = 0; j < matrixB.Length; j++)
                {
                    for (k = 0; k < matrixB[i].Length; k++)
                    {
                        resultMatrix[i][j] += matrixA[i][k] * matrixB[k][j];
                    }
                }
            }
        }

        public static void BadMatrixMul3()
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
                        j++;
                    }
                }
            }
        }
    }
}
