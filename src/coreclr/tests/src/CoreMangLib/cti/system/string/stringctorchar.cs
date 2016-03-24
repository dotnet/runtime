// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;

[assembly:System.Security.SecurityCritical]
namespace StringTest
{
    public class StringCtorChar
    {
        #region const define
        const int c_MaxLenth = 256;
        const int c_MinLenth = 1;
        #endregion
        #region Main function
        public static int Main()
        {
            StringCtorChar sCtorChar = new StringCtorChar();

            TestLibrary.TestFramework.BeginTestCase("StringCtorChar");

            if (sCtorChar.RunTests())
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
        #endregion
        #region public method
        public bool RunTests()
        {
            bool retVal = true;
            TestLibrary.TestFramework.LogInformation("[Positive1]");
            retVal = this.PosTest1() && retVal;
            TestLibrary.TestFramework.LogInformation("[Positive2]");
            retVal = this.PosTest2() && retVal;
            return retVal;
        }
        #endregion
        #region Positive Test Cases
        // Returns true if the expected result is right
        // Returns false if the expected result is wrong
        unsafe public bool PosTest1()
        {
            bool retVal = true;

            TestLibrary.TestFramework.BeginScenario("PosTest1:  value is a null pointer, an Empty instance is initialized");

            try
            {
                char* pointer = null;
                string teststring = GetTestString(pointer);
                if (teststring != string.Empty)
                {
                    TestLibrary.TestFramework.LogError("001a", "Comarison incorrect. TestString != String.Empty");
                    return false;
                }

            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("001", "Unexpected exception: " + e);
                retVal = false;
            }

            return retVal;
        }
        // Returns true if the expected result is right
        // Returns false if the expected result is wrong
        unsafe public bool PosTest2()
        {
            bool retVal = true;
            string testString = TestLibrary.Generator.GetString(-55, true, c_MinLenth, c_MaxLenth);
         
            TestLibrary.TestFramework.BeginScenario("PosTest2:  value is constructed and correct");

            try
            {

                char[] chartest = new char[c_MaxLenth];
                testString.CopyTo(0, chartest, 0, testString.Length);
                string teststring1;

                fixed (char* pointer = &(chartest[0]))
                {
                    teststring1 = GetTestString(pointer);
                }

                if (testString != teststring1)
                {
                    if (teststring1.Length < testString.Length)
                    {
                        TestLibrary.TestFramework.LogError("002", "The current process does not have read access to all the addressed characters");
                        retVal = false;

                    }
                    else
                    {
                        TestLibrary.TestFramework.LogError("002a", "Strings not the same");
                        retVal = false;
                    }
                 
                }

            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("003", "Unexpected exception: " + e);
                retVal = false;
            }

            return retVal;
        }
        #endregion
        #region constuctor function
        unsafe public static string  GetTestString(char* lPointer)
        {
            return new string(lPointer);
        }
        #endregion
    }

}
