// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace SimpleArray_01
{
    public delegate void RngTest();
    public class Class1
    {
        [Fact]
        public static int TestEntryPoint()
        {
            int retVal = 100;
            int testNum = 0;
            RngTest[] Tests ={  new RngTest(Test.Test1),
                                 new RngTest(Test.Test2),
                                 new RngTest(Test.Test3),
                                 new RngTest(Test.Test4),
                                 new RngTest(Test.Test5)};
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
        /********************************************************************************************
		* RngChk shall not be eliminated if directly access jaggedArray elements
		*********************************************************************************************/
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Test1()
        {
            int[][] myJaggedArray = new int[3][]
                        {
                            new int[5],
                            new int[3],
                            new int[4]
                        };
            int i, j;

            for (i = 0; i < myJaggedArray.Length; i++)
            {
                for (j = 0; j < myJaggedArray[i].Length; i++)
                {
                    myJaggedArray[2][j] = 1;
                }
            }
        }

        /********************************************************************************************
		* RngChk shall not be eliminated if the loop upper limit is larger than the array bound
		********************************************************************************************/
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Test2()
        {
            int[][] myJaggedArray = new int[3][]
                        {
                            new int[5],
                            new int[3],
                            new int[4]
                        };
            int i, j;
            int innerUpper = myJaggedArray[2].Length;
            for (i = 0; i < myJaggedArray.Length; i++)
            {
                for (j = 0; j < innerUpper; j++)
                {
                    myJaggedArray[i][j] = 1;
                }
            }
        }

        /********************************************************************************************
		* RngChk is not eliminated if the array is modified
		********************************************************************************************/
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Test3()
        {
            int[][] myJaggedArray = new int[3][]
                        {
                            new int[5],
                            new int[3],
                            new int[4]
                        };
            int i, j;
            int[][] dummy = new int[2][]
                {
                    new int[5],
                    new int[3]
                };
            int upper = myJaggedArray.Length;
            for (i = 0; i < upper; i++)
            {
                for (j = 0; j < myJaggedArray[i].Length; j++)
                {
                    myJaggedArray[i][j] = 1;
                    myJaggedArray = dummy;
                }
                myJaggedArray[i][0] = i;
            }
        }
        /********************************************************************************************
		* RngChk is not eliminated if the upper limit of the array is modified
		********************************************************************************************/
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Test4()
        {
            int[][] myJaggedArray = new int[3][]
                        {
                            new int[5],
                            new int[3],
                            new int[4]
                        };
            int i, j;
            int innerUpper = myJaggedArray[2].Length;
            for (i = 0; i < myJaggedArray.Length; i++)
            {
                for (j = 0; j < innerUpper; j++, innerUpper++)
                {
                    myJaggedArray[i][j] = 1;
                }
            }
        }
        /********************************************************************************************
		* RngChk is not eliminated if induction variable is modified
		********************************************************************************************/
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Test5()
        {
            int[][] myJaggedArray = new int[3][]
                        {
                            new int[5],
                            new int[3],
                            new int[4]
                        };
            int i, j;
            int innerUpper = myJaggedArray[2].Length;
            for (i = 0; i < myJaggedArray.Length; i++)
            {
                for (j = 0; j < innerUpper; j++)
                {
                    myJaggedArray[i][++j] = 1;
                }
            }
        }
    }
}
