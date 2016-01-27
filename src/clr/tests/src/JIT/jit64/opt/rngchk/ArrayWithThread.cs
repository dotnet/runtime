// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Runtime.CompilerServices;

namespace ArrayWithThread
{
    public delegate void RngTest(ref int a);
    internal class Class1
    {
        public static int val = 0;
        public static AutoResetEvent myResetEvent = new AutoResetEvent(false);
        private static int Main()
        {
            int retVal = 100;
            int testNum = 0;
            RngTest[] Tests ={  new RngTest(Test.Test1),
                        new RngTest(Test.Test2)};
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

        private static bool DoTest(RngTest Test)
        {
            bool bResult = false;
            myResetEvent.Reset();
            try
            {
                Thread t = new Thread(new ThreadStart(Class1.ThreadFunc));
                t.Start();
                Test(ref val);
                t.Join();
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
        private static void ThreadFunc()
        {
            myResetEvent.WaitOne();
            Class1.val = 101;
            return;
        }
    }
    internal class Test
    {
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Test1(ref int index)
        {
            int[] numbers = new int[100];
            for (; index < numbers.Length; index++)
            {
                Class1.myResetEvent.Set();
                Thread.Sleep(1);
                numbers[index] = index * index;
            }
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Test2(ref int upper)
        {
            int[] numbers = new int[100];
            int index = 0;
            upper = numbers.Length;
            for (; index < upper; index++)
            {
                Class1.myResetEvent.Set();
                Thread.Sleep(1);
                numbers[index] = index * index;
            }
        }
    }
}
