// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Text;



namespace ToInt32
{
    class IntPtrToInt32
    {
        public static int Main()
        {
            IntPtrToInt32 dek = new IntPtrToInt32();
            TestLibrary.TestFramework.BeginTestCase("IntPtrToInt32");
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
            retVal = PosTest4() && retVal;

            return retVal;


        }
        public bool PosTest1()
        {
            bool retVal = true;
            TestLibrary.TestFramework.BeginScenario("PosTest1: int32.minvalue");
            try
            {
                IntPtr ip = new IntPtr(Int32.MinValue);
                if (ip.ToInt32() != Int32.MinValue)
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
                if (ip.ToInt32() != Int32.MaxValue)
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
            TestLibrary.TestFramework.BeginScenario("PosTest3: int32.maxvalue-1");
            try
            {
                IntPtr ip = new IntPtr(Int32.MaxValue - 1);
                if (ip.ToInt32() != Int32.MaxValue - 1)
                {

                    TestLibrary.TestFramework.LogError("005", "value is not int32.maxvalue-1 as expected");

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

        public bool PosTest4()
        {
            bool retVal = true;
            TestLibrary.TestFramework.BeginScenario("PosTest4:  random int32 ");
            try
            {
                int i = TestLibrary.Generator.GetInt32(-55);
                IntPtr ip = new IntPtr(i);
                if (ip.ToInt32() != i)
                {

                    TestLibrary.TestFramework.LogError("007", "value is not the random number as expected");

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

