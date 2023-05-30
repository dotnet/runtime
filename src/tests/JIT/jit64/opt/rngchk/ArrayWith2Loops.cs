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
                                 new RngTest(Test.Test4)};

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
		* RngChk shall not be eliminated inner loop uppbound is modified in the outer loop
		*********************************************************************************************/
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Test1()
        {
            // Declare the array of two elements:
            int[] myArray = new int[100];
            int upper = 50;
            for (int i = 0; i < myArray.Length; i++)
            {
                for (int j = 0; j < upper; j++)
                {
                    myArray[j] = j;
                }
                upper++;
            }
        }

        /********************************************************************************************
		* RngChk shall not be eliminated if induction vairable is modified through function call in the outer loop
		********************************************************************************************/
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Test2()
        {
            // Declare the array of two elements:
            int[] myArray = new int[101];
            int index = 99;
            for (int i = 0; i < myArray.Length; i++)
            {
                for (int j = 0; j < myArray.Length; j++)
                {
                    myArray[index] = j;
                }
                foo(ref index);
            }
        }
        /********************************************************************************************
		* RngChk shall not be eliminated if induction vairable is modified through function call in the inner loop
		********************************************************************************************/
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Test3()
        {
            int[] myArray = new int[100];
            for (int i = 0; i < myArray.Length; i++)
            {
                for (int j = 0; j < myArray.Length; j++)
                {
                    myArray[j] = j + i;
                    foo(ref i);
                }
                myArray[i] = i;
            }
        }

        /********************************************************************************************
		* RngChk shall not be eliminated outer loop uppbound is modified in the inner loop
		*********************************************************************************************/
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Test4()
        {
            // Declare the array of two elements:
            int[] myArray = new int[100];
            int upperOuter = 99;
            int upperInner = 99;
            for (int i = 0; i < upperOuter; i++)
            {
                myArray[i] = i;
                for (int j = 0; j < upperInner; j++)
                {
                    myArray[j] = j + i;
                    upperOuter += j;
                }
            }
        }

        private static void foo(ref int index)
        {
            index++;
        }
    }
}
