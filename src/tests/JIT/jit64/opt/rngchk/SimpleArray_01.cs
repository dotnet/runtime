// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            RngTest[] Tests ={  new RngTest(Test.Test1),
                        new RngTest(Test.Test2),
                        new RngTest(Test.Test3),
                        new RngTest(Test.Test4),
                        new RngTest(Test.Test5),
                        new RngTest(Test.Test6)};
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
		* RngChk shall not be eliminated when direct access to an out bound element within the loop 
		*********************************************************************************************/
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Test1()
        {
            int[] numbers = new int[100];
            int index = 0;
            for (index = 0; index < numbers.Length; index++)
            {
                numbers[101] = index * index;
            }
        }

        /********************************************************************************************
		* RngChk shall not be eliminated if the loop upper limit is larger than the array bound
		********************************************************************************************/
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Test2()
        {
            int[] numbers = new int[100];
            int index = 0;
            for (index = 0; index < 101; index++)
            {
                numbers[index] = index * index;
            }
        }
        /********************************************************************************************
		* RngChk is eliminated properly when reverse iterate through the array
		********************************************************************************************/
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Test3()
        {
            int[] numbers = new int[100];
            int index = 0;

            for (index = numbers.Length; index >= 0; index--)
            {
                numbers[index] = index * index;
            }
        }
        /********************************************************************************************
		* RngChk is not eliminated if the array is modified
		********************************************************************************************/
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Test4()
        {
            int[] numbers = new int[100];
            int[] numbers2 = new int[99];
            int index = 0;
            for (index = 0; index < numbers.Length; index++)
            {
                if (index > 98)
                    numbers = numbers2;
                numbers[index] = index * index;
            }
        }
        /********************************************************************************************
		* RngChk is not eliminated if the upper limit of the array is modified
		********************************************************************************************/
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Test5()
        {
            int[] numbers;

            int index = 0;
            numbers = new int[100];
            int upper = 99;
            for (index = 0; index < upper; index++, upper++)
            {
                numbers[index] = index * index;
            }
        }
        /********************************************************************************************
		* RngChk is not eliminated if induction variable is modified
		********************************************************************************************/
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Test6()
        {
            int[] numbers;

            int index = 0;
            numbers = new int[101];
            for (index = 0; index++ < numbers.Length; index++)
            {
                numbers[index] = index * index;
            }
        }
    }
}
