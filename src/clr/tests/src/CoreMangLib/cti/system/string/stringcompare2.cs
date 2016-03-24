// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;
using TestLibrary;

namespace DictionaryEntryCtor
{
    class DictionaryEntryCtor
    {
        const string str1 = "HELLOWORLD";
        const string str2 = "helloworld";
        public static int Main(string[] args)
        {
            DictionaryEntryCtor dec = new DictionaryEntryCtor();
            TestLibrary.TestFramework.BeginTestCase("Compare(System.String,System.Int32,System.String,System.Int32,System.Int32,System.StringComparision)");

            if (dec.RunTests())
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

        /// <summary>
        /// Compare(System.String,System.Int32,System.String,System.Int32,System.Int32,System.Boolean)
        /// </summary>
        /// <returns></returns>
        public bool RunTests()
        {
            bool retVal = true;

            TestLibrary.TestFramework.LogInformation("[Positive]");
            retVal = PosTest1() && retVal;
            retVal = PosTest2() && retVal;
            retVal = PosTest3() && retVal;
            retVal = PosTest4() && retVal;

            TestLibrary.TestFramework.LogInformation("");

            TestLibrary.TestFramework.LogInformation("[Negative]");
            retVal = NegTest1() && retVal;
            retVal = NegTest2() && retVal;
            retVal = NegTest3() && retVal;
            retVal = NegTest4() && retVal;
            retVal = NegTest5() && retVal;
            retVal = NegTest6() && retVal;
            retVal = NegTest7() && retVal;
            retVal = NegTest8() && retVal;

            return retVal;

        }

        /// <summary>
        /// Compare the same string with different cases when ignore the case
        /// </summary>
        /// <returns></returns>
        public bool PosTest1()
        {
            bool retVal = true;

            try
            {
                TestLibrary.TestFramework.BeginScenario("Compare the same string with different cases when ignore the case...");
                int expected = GlobLocHelper.OSCompare(str1, 2, str2, 2, 3, true); // 0;
                if (String.Compare(str1, 2, str2, 2, 3, StringComparison.CurrentCultureIgnoreCase) != expected)
                {
                    TestLibrary.TestFramework.LogError("001", "LLOWO is not equal to llowo when ignore the case...");
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

        /// <summary>
        /// Compare the same string with different cases when the case is considered
        /// </summary>
        /// <returns></returns>
        public bool PosTest2()
        {
            bool retVal = true;

            try
            {
                TestLibrary.TestFramework.BeginScenario("Compare the same string with different cases when the case is considered...");
                int expected = GlobLocHelper.OSCompare(str1, 2, str2, 2, 3, false); // 1;
                if (String.Compare(str1, 2, str2, 2, 3, StringComparison.CurrentCulture) != expected)
                {
                    TestLibrary.TestFramework.LogError("009", "LLOWO is not larger than llowo when the case is considered!");
                    retVal = false;
                }
            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("010", "Unexpected exception: " + e);
                retVal = false;
            }


            return retVal;
        }

        /// <summary>
        /// Compare two null strings with indexs and length as zero
        /// </summary>
        /// <returns></returns>
        public bool PosTest3()
        {
            bool retVal = true;

            TestLibrary.TestFramework.BeginScenario("Compare two null strings with indexs and length as zero...");
            try
            {
                if (String.Compare(null, 0, null, 0, 0, StringComparison.CurrentCultureIgnoreCase) != 0)
                {
                    TestLibrary.TestFramework.LogError("011", "Copmare failed!");
                    retVal = false;
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                TestLibrary.TestFramework.LogError("012", "ArgumentOutOfRangeException is thrown when a given index is out of range!");
            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("013", "Unexpected exception: " + e);
                retVal = false;
            }

            return retVal;
        }

        /// <summary>
        /// Compare two strings which one of them is null
        /// </summary>
        /// <returns></returns>
        public bool PosTest4()
        {
            bool retVal = true;

            TestLibrary.TestFramework.BeginScenario("Compare two strings which one of them is null...");
            try
            {
                if (String.Compare(null, 0, str1, 0, 0, StringComparison.CurrentCultureIgnoreCase) >= 0)
                {
                    TestLibrary.TestFramework.LogError("023", "Compare failed!");
                    retVal = false;
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                TestLibrary.TestFramework.LogError("024", "ArgumentOutOfRangeException is thrown when one of strings is null!");
            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("025", "Unexpected exception: " + e);
                retVal = false;
            }

            return retVal;
        }

        /// <summary>
        /// Compare the strings with index out of range
        /// </summary>
        /// <returns></returns>
        public bool NegTest1()
        {
            bool retVal = true;

            TestLibrary.TestFramework.BeginScenario("Compare the strings with index out of range...");
            try
            {
                if (String.Compare(str1, 11, str2, 11, 3, StringComparison.CurrentCultureIgnoreCase) != 0)
                {
                    retVal = false;
                }
            }
            catch (ArgumentOutOfRangeException)
            {
   //             TestLibrary.TestFramework.LogError("003", "ArgumentOutOfRangeException is thrown when a given index is out of range!");
            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
                retVal = false;
            }

            return retVal;
        }

        /// <summary>
        /// Compare the strings with indexes are negative
        /// </summary>
        /// <returns></returns>
        public bool NegTest2()
        {
            bool retVal = true;

            TestLibrary.TestFramework.BeginScenario("Compare the strings with indexes are negative...");
            try
            {                
                if (String.Compare(str1, -1, str2, -1, 3, StringComparison.CurrentCultureIgnoreCase) != 0)
                {
                    retVal = false;
                }
            }
            catch (ArgumentOutOfRangeException)
            {
     //           TestLibrary.TestFramework.LogError("005", "ArgumentOutOfRangeException is thrown when a given indexes are negative!");
            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e);
                retVal = false;
            }

            return retVal;
        }

        /// <summary>
        /// Compare the strings with length is negative
        /// </summary>
        /// <returns></returns>
        public bool NegTest3()
        {
            bool retVal = true;

            TestLibrary.TestFramework.BeginScenario("Compare the strings with length is negative...");
            try
            {
                if (String.Compare(str1, 0, str2, 0, -1, StringComparison.CurrentCultureIgnoreCase) != 0)
                {
                    retVal = false;
                }
            }
            catch (ArgumentOutOfRangeException)
            {
  //              TestLibrary.TestFramework.LogError("007", "ArgumentOutOfRangeException is thrown when a given length is negative!");
            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("008", "Unexpected exception: " + e);
                retVal = false;
            }

            return retVal;
        }

        /// <summary>
        /// Compare two null strings with length out of range
        /// </summary>
        /// <returns></returns>
        public bool NegTest4()
        {
            bool retVal = true;

            TestLibrary.TestFramework.BeginScenario("Compare two null strings with length out of range...");
            try
            {
                if (String.Compare(null, 0, null, 0, 1, StringComparison.CurrentCultureIgnoreCase) != 0)
                {
                    TestLibrary.TestFramework.LogError("014","Compare failed!");
                    retVal = false;
                }
            }
            catch (ArgumentOutOfRangeException)
            {
   //             TestLibrary.TestFramework.LogError("015", "ArgumentOutOfRangeException is thrown when a given length is out of range!");
            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("016", "Unexpected exception: " + e);
                retVal = false;
            }

            return retVal;
        }

        /// <summary>
        /// Compare two null strings with indexs out of range
        /// </summary>
        /// <returns></returns>
        public bool NegTest5()
        {
            bool retVal = true;

            TestLibrary.TestFramework.BeginScenario("Compare two null strings with indexs out of range...");
            try
            {
                if (String.Compare(null, 1, null, 1, 0, StringComparison.CurrentCultureIgnoreCase) != 0)
                {
                    TestLibrary.TestFramework.LogError("017","Compare failed!");
                    retVal = false;
                }
            }
            catch (ArgumentOutOfRangeException)
            {
   //             TestLibrary.TestFramework.LogError("018", "ArgumentOutOfRangeException is thrown when a given index is out of range!");
            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("019", "Unexpected exception: " + e);
                retVal = false;
            }

            return retVal;
        }

        /// <summary>
        /// Compare two null strings with indexs are negative
        /// </summary>
        /// <returns></returns>
        public bool NegTest6()
        {
            bool retVal = true;

            TestLibrary.TestFramework.BeginScenario("Compare two null strings with indexs are negative...");
            try
            {
                if (String.Compare(null, -1, null, -1, 0, StringComparison.CurrentCultureIgnoreCase) != 0)
                {
                    TestLibrary.TestFramework.LogError("020","Compare failed!");
                    retVal = false;
                }
            }
            catch (ArgumentOutOfRangeException)
            {
 //               TestLibrary.TestFramework.LogError("021", "ArgumentOutOfRangeException is thrown when a given indexes are negative!");
            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("022", "Unexpected exception: " + e);
                retVal = false;
            }

            return retVal;
        }

        /// <summary>
        /// Compare two strings which one of them is null and the length is not zero
        /// </summary>
        /// <returns></returns>
        public bool NegTest7()
        {
            bool retVal = true;

            TestLibrary.TestFramework.BeginScenario("Compare two strings which one of them is null and the length is not zero...");
            try
            {
                if (String.Compare(null, 0, str1, 0, 1, StringComparison.CurrentCultureIgnoreCase) >= 0)
                {
                    TestLibrary.TestFramework.LogError("026","Compare failed!");
                    retVal = false;
                }
            }
            catch (ArgumentOutOfRangeException)
            {
   //             TestLibrary.TestFramework.LogError("027", "ArgumentOutOfRangeException is thrown when a given length is out of range!");
            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("028", "Unexpected exception: " + e);
                retVal = false;
            }

            return retVal;
        }

        /// <summary>
        /// Compare two strings which one of them is null and the length is negative
        /// </summary>
        /// <returns></returns>
        public bool NegTest8()
        {
            bool retVal = true;

            TestLibrary.TestFramework.BeginScenario("Compare two strings which one of them is null and the length is negative...");
            try
            {
                if (String.Compare(null, 0, str1, 0, -1, StringComparison.CurrentCultureIgnoreCase) >= 0)
                {
                    TestLibrary.TestFramework.LogError("029","Compare failed!");
                    retVal = false;
                }
            }
            catch (ArgumentOutOfRangeException)
            {
    //            TestLibrary.TestFramework.LogError("030", "ArgumentOutOfRangeException is thrown when a given length is ength is negative!");
            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("031", "Unexpected exception: " + e);
                retVal = false;
            }

            return retVal;
        }
    }
}
