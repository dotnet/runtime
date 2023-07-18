// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace ArrayBound
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
                                 new RngTest(Test.Test3)};

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
		* Index is type sbyte
		*********************************************************************************************/
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Test1()
        {
            int[] numbers = new int[128];
            sbyte index;
            for (index = 0; index < numbers.Length; index++)
            {
                numbers[index] = 1;
            }
        }

        /********************************************************************************************
		* Index is type short
		********************************************************************************************/
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Test2()
        {
            int[] numbers = new int[Int16.MaxValue + 1];
            short index = Int16.MaxValue - 1;
            for (; index < numbers.Length; index++)
            {
                numbers[index] = 1;
            }
        }
        /********************************************************************************************
		* upper bound is on the edge of the short
		********************************************************************************************/
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Test3()
        {
            int[] numbers = new int[100];
            int upper = int.MinValue;
            int index;
            for (index = 0; index < upper - 1; index++)
            {
                numbers[index] = 1;
            }
        }
    }
}
