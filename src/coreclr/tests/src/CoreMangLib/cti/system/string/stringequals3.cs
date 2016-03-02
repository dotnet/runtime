// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;
using System.Globalization;

/// <summary>
/// System.String.Equals(String, String)  
/// Determines whether two specified String objects have the same value.
/// This method performs an ordinal (case-sensitive and culture-insensitive) comparison.
/// </summary>
class StringEquals3
{
    private const int c_MIN_STRING_LEN = 8;
    private const int c_MAX_STRING_LEN = 256;

    private const int c_MAX_SHORT_STR_LEN = 31;//short string (<32 chars)
    private const int c_MIN_LONG_STR_LEN = 257;//long string ( >256 chars)
    private const int c_MAX_LONG_STR_LEN = 65535;

    private const string c_POS_TEST_PREFIX = "PosTest";
    private const string c_NEG_TEST_PREFIX = "NegTest";

    private const string c_GREEK_SIGMA_STR_A = "\x03C2\x03C3\x03A3\x03C2\x03C3";
    private const string c_GREEK_SIGMA_STR_B = "\x03A3\x03A3\x03A3\x03C3\x03C2";

    private int totalTestCount;
    private int posTestCount;
    private int negTestCount;
    private int passedTestCount;
    private int failedTestCount;

    private enum TestType { PositiveTest = 1, NegativeTest = 2 };
    private enum TestResult { NotRun = 0, PassedTest = 1, FailedTest = 2 };
    internal struct Parameters
    {
        public string strSrc;
        public string strDes;

        public string DataString
        {
            get
            {
                string str, strA, strB;
                int lenA, lenB;

                if (null == strSrc)
                {
                    strA = "null";
                    lenA = 0;
                }
                else
                {
                    strA = strSrc;
                    lenA = strSrc.Length;
                }
                if (null == strDes)
                {
                    strB = "null";
                    lenB = 0;
                }
                else
                {
                    strB = strDes;
                    lenB = strB.Length;
                }

                str = string.Format("\n[String value]\nSource: \"{0}\"\nDestination: \"{1}\"", strA, strB);
                str += string.Format("\n[String length]\nSource: {0}\nDestination: {1}", lenA, lenB);

                return str;
            }
        }
    }

    //Default constructor to ininitial all kind of test counts
    public StringEquals3()
    {
        totalTestCount = posTestCount = negTestCount = 0;
        passedTestCount = failedTestCount = 0;
    }

    #region Methods for all test scenarioes

    //Update (postive or negative) and total test count at the beginning of test scenario
    private void UpdateCounts(TestType testType)
    {
        if (TestType.PositiveTest == testType)
        {
            posTestCount++;
            totalTestCount++;
            return;
        }

        if (TestType.NegativeTest == testType)
        {
            negTestCount++;
            totalTestCount++;
            return;
        }
    }

    //Update failed or passed test counts at the end of test scenario
    private void UpdateCounts(TestResult testResult)
    {
        if (TestResult.PassedTest == testResult)
        {
            passedTestCount++;
            return;
        }

        if (TestResult.FailedTest == testResult)
        {
            failedTestCount++;
            return;
        }
    }

    //Generate standard error number string
    //i.e "9", "12" is not proper. Instead they should be "009", "012"
    private string GenerateErrorNum(int errorNum)
    {
        string temp = errorNum.ToString();
        string errorNumStr = new string('0', 3 - temp.Length) + temp;
        return errorNumStr;
    }

    //Generate testId string
    //i.e "P9", "N12" is not proper. Instead they should be "P009", "N012"
    private string GenerateTestId(TestType testType)
    {
        string temp, testId;

        if (testType == TestType.PositiveTest)
        {
            temp = this.posTestCount.ToString();
            testId = "P" + new string('0', 3 - temp.Length) + temp;
        }
        else
        {
            temp = this.negTestCount.ToString();
            testId = "N" + new string('0', 3 - temp.Length) + temp;
        }

        return testId;
    }

    #endregion

    public static int Main()
    {
        StringEquals3 sc = new StringEquals3();

        TestLibrary.TestFramework.BeginTestCase("for method: System.String.Equals(String, String)");
        if (sc.RunTests())
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

        //Postive test scenarios
        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;
        retVal = PosTest5() && retVal;
        retVal = PosTest6() && retVal;
        retVal = PosTest7() && retVal;
        retVal = PosTest8() && retVal;
        retVal = PosTest9() && retVal;
        retVal = PosTest10() && retVal;
        retVal = PosTest11() && retVal;

        //Negative test scenarios
        //TestLibrary.TestFramework.LogInformation("[Negative]");

        return retVal;
    }

    #region Positive test scenarioes

    #region null, String.Empty and "\0" testing

    public bool PosTest1()
    {
        Parameters paras;

        const string c_TEST_DESC = "The string to compare with is null";
        bool expectedValue = false;

        paras.strSrc = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        paras.strDes = null;

        return ExecutePosTest(paras, expectedValue, c_TEST_DESC);
    }

    public bool PosTest2()
    {
        Parameters paras;

        const string c_TEST_DESC = "String.Empty vs null";
        bool expectedValue = false;

        paras.strSrc = String.Empty;
        paras.strDes = null;

        return ExecutePosTest(paras, expectedValue, c_TEST_DESC);
    }

    public bool PosTest3()
    {
        Parameters paras;

        const string c_TEST_DESC = "String.Empty vs \"\"";
        bool expectedValue = true;

        paras.strSrc = String.Empty;
        paras.strDes = "";

        return ExecutePosTest(paras, expectedValue, c_TEST_DESC);
    }

    public bool PosTest4()
    {
        Parameters paras;

        const string c_TEST_DESC = "String.Empty vs \"\\0\"";
        bool expectedValue = false;

        paras.strSrc = String.Empty;
        paras.strDes = "\0";

        return ExecutePosTest(paras, expectedValue, c_TEST_DESC);
    }

    public bool PosTest5()
    {
        Parameters paras;

        const string c_TEST_DESC = "String.Empty vs unempty string";
        bool expectedValue = false;

        paras.strSrc = String.Empty;
        paras.strDes = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);

        return ExecutePosTest(paras, expectedValue, c_TEST_DESC);
    }

    public bool PosTest6()
    {
        Parameters paras;

        const string c_TEST_DESC = "Embedded '\\0' string ";
        bool expectedValue = true;

        StringBuilder sb = new StringBuilder("This\0String\0Is\0Valid");
        paras.strSrc = sb.ToString();
        paras.strDes = sb.ToString();

        return ExecutePosTest(paras, expectedValue, c_TEST_DESC);
    }

    #endregion

    #region Tab and space testing

    public bool PosTest7()
    {
        Parameters paras;

        const string c_TEST_DESC = "Tab vs 4 spaces";
        bool expectedValue = false;

        paras.strSrc = "\t";
        paras.strDes = new string('\x0020', 4); // new update 8-8-2006 Noter(v-yaduoj)

        return ExecutePosTest(paras, expectedValue, c_TEST_DESC);
    }

    public bool PosTest8()
    {
        Parameters paras;

        const string c_TEST_DESC = "Tab vs 8 spaces";
        bool expectedValue = false;

        paras.strSrc = "\t";
        paras.strDes = new string('\x0020', 8); // new update 8-8-2006 Noter(v-yaduoj)

        return ExecutePosTest(paras, expectedValue, c_TEST_DESC);
    }

    #endregion

    #region Case sensitive testing

    public bool PosTest9()
    {
        Parameters paras;

        const string c_TEST_DESC = "Case sensitive testing";
        bool expectedValue = false;

        char ch = this.GetUpperChar();
        paras.strDes = ch.ToString();
        paras.strSrc = char.ToLower(ch).ToString();

        return ExecutePosTest(paras, expectedValue, c_TEST_DESC);
    }

    //Greek Sigma: 
    //Two lower case Sigma: (0x03C2), (0x03C3) 
    //One upper case Sigma: (0x03A3) 
    //where 2 lower case characters have the same upper case character.
    public bool PosTest10()
    {
        Parameters paras;

        const string c_TEST_DESC = "Asymmetric casing: Greek Sigma character, different case";
        bool expectedValue = false;

        paras.strDes = c_GREEK_SIGMA_STR_A;
        paras.strSrc = c_GREEK_SIGMA_STR_B;

        return ExecutePosTest(paras, expectedValue, c_TEST_DESC);
    }

    public bool PosTest11()
    {
        Parameters paras;

        const string c_TEST_DESC = "Asymmetric casing: Greek Sigma character, both upper case";
        bool expectedValue = true;
                
        string str1 = c_GREEK_SIGMA_STR_A;
        string str2 = c_GREEK_SIGMA_STR_B;
        paras.strDes = str1.ToUpper();
        paras.strSrc = str2.ToUpper();
       
        return ExecutePosTest(paras, expectedValue, c_TEST_DESC);
    }

    #endregion

    #endregion //end for positive test scenarioes

    #region Helper methods for positive test scenarioes

    private bool ExecutePosTest(Parameters paras, bool expectedValue, string testDesc)
    {
        bool retVal = true;
        UpdateCounts(TestType.PositiveTest);
        string testId = GenerateTestId(TestType.PositiveTest);
        TestResult testResult = TestResult.NotRun;

        string testInfo = c_POS_TEST_PREFIX + this.posTestCount.ToString() + ": " + testDesc;
        bool actualValue;

        TestLibrary.TestFramework.BeginScenario(testInfo);
        try
        {
            actualValue = this.CallTestMethod(paras);
            if (expectedValue != actualValue)
            {
                string errorDesc = "Value is not " + expectedValue + " as expected: Actual(" + actualValue + ")";
                errorDesc += paras.DataString + "\nTest scenario Id: " + testId;
                // new update: 8-6-2006 Noter(v-yaduoj)
                TestLibrary.TestFramework.LogError(GenerateErrorNum((totalTestCount << 1) - 1) + " TestId -" + testId, errorDesc);
                testResult = TestResult.FailedTest;
                retVal = false;
            }

            testResult = TestResult.PassedTest;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError(GenerateErrorNum(totalTestCount << 1) + " TestId -" + testId, "Unexpected exception: " + e + paras.DataString);
            testResult = TestResult.FailedTest;
            retVal = false;
        }

        UpdateCounts(testResult);
        return retVal;
    }

    #endregion

    //Involke the test method
    private bool CallTestMethod(Parameters paras)
    {
        return string.Equals(paras.strSrc, paras.strDes);
    }

    #region helper methods for generating test data

    private bool GetBoolean()
    {
        Int32 i = this.GetInt32(1, 2);
        return (i == 1) ? true : false;
    }

    //Get a non-negative integer between minValue and maxValue
    private Int32 GetInt32(Int32 minValue, Int32 maxValue)
    {
        try
        {
            if (minValue == maxValue)
            {
                return minValue;
            }
            if (minValue < maxValue)
            {
                return minValue + TestLibrary.Generator.GetInt32(-55) % (maxValue - minValue);
            }
        }
        catch
        {
            throw;
        }

        return minValue;
    }

    private Int32 Min(Int32 i1, Int32 i2)
    {
        return (i1 <= i2) ? i1 : i2;
    }

    private Int32 Max(Int32 i1, Int32 i2)
    {
        return (i1 >= i2) ? i1 : i2;
    }

    private char GetUpperChar()
    {
        Char c;
        //  Grab an ASCII letter
        c = Convert.ToChar(TestLibrary.Generator.GetInt16(-55) % 26 + 'A');
        return c;
    }

    #endregion
}

