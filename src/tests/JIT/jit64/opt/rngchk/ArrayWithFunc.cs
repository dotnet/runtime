// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace ArrayWithFunc
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
    internal class Modifier
    {
        public static void ModValue(ref int a)
        {
            a++;
            return;
        }
        public static int ModValue2(ref int a)
        {
            return ++a;
        }
        public static void ModArray(ref int[] array)
        {
            int[] array2 = new int[97];
            array = array2;
            return;
        }
    }

    internal class Test
    {
        /********************************************************************************************
		* Index is modified through a function call 
		*********************************************************************************************/
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Test1()
        {
            int index = 0;
            int[] numbers = new int[101];
            for (index = 0; index < numbers.Length; index++)
            {
                Modifier.ModValue(ref index);
                numbers[index] = index * index;
            }
        }

        /********************************************************************************************
		* Array is modified through a function call
		********************************************************************************************/
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Test2()
        {
            int[] numbers = new int[100];
            int index = 0;
            int upper = numbers.Length - 1;
            for (index = 0; index < upper; index++)
            {
                Modifier.ModArray(ref numbers);
                numbers[index] = index * index;
            }
        }
        /********************************************************************************************
		* Loop upper bound is modified through a function call
		********************************************************************************************/
        public static void Test3()
        {
            int index = 0;
            int[] numbers = new int[9];
            int upper = numbers.Length - 1;
            for (index = 0; index < upper; index++, Modifier.ModValue(ref upper))
            {
                numbers[index] = index * index;
            }
        }
        /********************************************************************************************
		* Another way to modifier loop induction variable
		********************************************************************************************/
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Test4()
        {
            int index = 0;
            int[] numbers = new int[9];
            for (index = 0; index < numbers.Length; index++)
            {
                numbers[Modifier.ModValue2(ref index)] = index * index;
            }
        }
    }
}
