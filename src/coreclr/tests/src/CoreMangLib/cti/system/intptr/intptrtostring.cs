// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Text;

namespace IntPtrToString
{
    class IntPtrToString
    {
        public static int Main()
        {
            IntPtrToString dek = new IntPtrToString();
            TestLibrary.TestFramework.BeginTestCase(" IntPtrToString");
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
            return retVal;


        }
       public bool PosTest1()
        {
            bool retVal = true;
            TestLibrary.TestFramework.BeginScenario("PosTest1: test ToString()");
            try
            {
                int i = 0;
                IntPtr ip = new IntPtr(i);
                if (ip.ToString() != i.ToString())
                {
                    TestLibrary.TestFramework.LogError("001", "not equal!");
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
            TestLibrary.TestFramework.BeginScenario("PosTest1: check the value of string");
            try
            {
                long long1 = 123456789;
                
                if (long1.ToString() != "123456789" )
                {

                    TestLibrary.TestFramework.LogError("003", "ToString() did not works correctly");
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
            TestLibrary.TestFramework.BeginScenario("PosTest1: check ToString() is equal to Int64.ToString()  ");
            try
            {
                int ip = 010234;
                IntPtr ptr = new IntPtr(ip);
                if ( ptr.ToInt64().ToString() != ip.ToString())
                {

                    TestLibrary.TestFramework.LogError("005", "Int64.ToString() is not equivalent to IntPtr.ToString()");
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
    }
}
