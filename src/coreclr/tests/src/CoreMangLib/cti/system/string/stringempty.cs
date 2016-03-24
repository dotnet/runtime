// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;
namespace StringTest
{
    public class StringEmpty
    {
        #region main method
        public static int Main()
        {
            StringEmpty stringempty = new StringEmpty();

            TestLibrary.TestFramework.BeginTestCase("StringEmpty");

            if (stringempty.RunTests())
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

            TestLibrary.TestFramework.LogInformation("[Positive]");
            retVal = PosTest1() && retVal;
           
          

            return retVal;
        }
        #endregion
        #region Positive Test Cases
        // Returns true if the expected result is right
        // Returns false if the expected result is wrong
        public bool PosTest1()
        {
            bool retVal = true;

            TestLibrary.TestFramework.BeginScenario("PosTest1: Compare a string with System.Empty");

            try
            {
                string teststring = string.Empty;
                string teststring1 = "";
                if (!teststring.Equals(teststring1))
                {
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
        #endregion


    }

}