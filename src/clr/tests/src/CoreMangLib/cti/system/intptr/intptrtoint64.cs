// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Text;

namespace ToInt64
{
    class IntPtrToInt64
    {
        public static int Main()
        {
            IntPtrToInt64 dek = new IntPtrToInt64();
            TestLibrary.TestFramework.BeginTestCase(" IntPtrToInt64");
            if (dek.RunTests())
            {
                TestLibrary.TestFramework.EndTestCase();
                TestLibrary.TestFramework.LogInformation("PASS");
                return 100;
            }
            else
            {
                TestLibrary.TestFramework.EndTestCase();
                TestLibrary.TestFramework.LogInformation("FAIL");
                return 0;
            }
        }

        public bool RunTests()
        {
            bool retVal = true;
            TestLibrary.TestFramework.LogInformation("[Positive]");
            retVal = PosTest1() && retVal;
            retVal = PosTest2() && retVal;
            retVal = PosTest3() && retVal;

            TestLibrary.TestFramework.LogInformation("[Negative]");
            retVal = NegTest1() && retVal;

            return retVal;
        }

        public bool PosTest1()
        {
            bool retVal = true;
            TestLibrary.TestFramework.BeginScenario("PosTest1: int32.minvalue");
            try
            {
                IntPtr ip = new IntPtr(Int32.MinValue);
                if (ip.ToInt64() != Int32.MinValue)
                {
                    TestLibrary.TestFramework.LogError("001", "value is not int32.minvalue as expected");

                    retVal = false;
                }
            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
                retVal = false;
            }
            return retVal;
        }

        public bool PosTest2()
        {
            bool retVal = true;
            TestLibrary.TestFramework.BeginScenario("PosTest2: int32.maxvalue");
            try
            {
                IntPtr ip = new IntPtr(Int32.MaxValue);
                if (ip.ToInt64() != Int32.MaxValue)
                {
                    TestLibrary.TestFramework.LogError("003", "value is not int32.maxvalue as expected");

                    retVal = false;
                }
            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
                retVal = false;

            }
            return retVal;
        }

        public bool PosTest3()
        {
            bool retVal = true;
            TestLibrary.TestFramework.BeginScenario("PosTest3:  random int32 ");
            try
            {
                int i = TestLibrary.Generator.GetInt32(-55);
                IntPtr ip = new IntPtr(i);
                if (ip.ToInt64() != i)
                {
                    TestLibrary.TestFramework.LogError("005", "value is not the random number as expected");

                    retVal = false;
                }
            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e);
                retVal = false;
            }
            return retVal;
        }

        public bool NegTest1()
        {
            bool retVal = true;
            TestLibrary.TestFramework.BeginScenario("NegTest1: int64.maxvalue");
            try
            {
                long i = Int64.MaxValue;
                System.IntPtr ip = new IntPtr(i);
            }
            catch (System.OverflowException)
            {
                if (IntPtr.Size == 8)
                {
                    TestLibrary.TestFramework.LogError("007", "64bit machine would not be Overflowed! ");
                    retVal = false;
                }
            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("008", "Unexpected exception: " + e);
                retVal = false;
            }

            return retVal;
        }
    }
}

