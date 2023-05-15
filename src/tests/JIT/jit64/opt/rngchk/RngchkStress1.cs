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
            RngTest[] Tests = { new RngTest(Test.Test1) };

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
		* Stress Test 1
		*********************************************************************************************/
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Test1()
        {
            int index = 0;
            int[] array = new int[100];
            int[] array1 = new int[100 + 1];
            int[] array2 = new int[100 + 2];
            int[] array3 = new int[100 + 3];
            int[] array4 = new int[100 + 4];
            int[] array5 = new int[100 + 5];
            int[] smallArray = new int[99];
            for (index = 0; index < array.Length; index++)
            {
                array1[index] = index;
                array2[index] = index;
                array3[index] = index;
                array4[index] = index;
                array5[index] = index;
                smallArray[index] = index;
            }
        }
    }
}
