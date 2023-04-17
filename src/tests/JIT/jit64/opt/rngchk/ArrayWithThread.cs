// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Runtime.CompilerServices;
using Xunit;

namespace ArrayWithThread
{
    public delegate void RngTest(ref int a);
    public class Class1
    {
        public static int val = 0;
        public static AutoResetEvent myResetEvent1 = new AutoResetEvent(false);
        public static ManualResetEvent myResetEvent2 = new ManualResetEvent(false);
        [Fact]
        public static int TestEntryPoint()
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
            myResetEvent1.Reset();
            myResetEvent2.Reset();
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
            myResetEvent1.WaitOne();
            Class1.val = 101;
            myResetEvent2.Set();
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
                Class1.myResetEvent1.Set();
                Class1.myResetEvent2.WaitOne();
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
                Class1.myResetEvent1.Set();
                Class1.myResetEvent2.WaitOne();
                numbers[index] = index * index;
            }
        }
    }
}
