// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// String.GetHashCode()
/// Returns the hash code for this string.
/// </summary>
class StringGetHashCode
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

        public string DataString
        {
            get
            {
                string str, strA;
                int lenA;

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

                str = string.Format("\n[String value]\nSource: \"{0}\"\n[String length]\n {1}", strA, lenA);

                return str;
            }
        }
    }

    //Default constructor to ininitial all kind of test counts
    public StringGetHashCode()
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
        StringGetHashCode sge = new StringGetHashCode();

        TestLibrary.TestFramework.BeginTestCase("for method: System.String.GetHashCode()");
        if (sge.RunTests())
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

    #region Positive test scenarioes

    #region Normal tests

    public bool PosTest1()
    {
        Parameters paras;

        const string c_TEST_DESC = "Random string";

        paras.strSrc = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);

        return ExecutePosTest(paras, c_TEST_DESC);
    }

    #endregion

    #region String.Empty, "\0" and null

    public bool PosTest2()
    {
        Parameters paras;

        const string c_TEST_DESC = "String.Empty";

        paras.strSrc = String.Empty;

        return ExecutePosTest(paras, c_TEST_DESC);
    }

    public bool PosTest3()
    {
        Parameters paras;

        const string c_TEST_DESC = "\"\0\"";

        paras.strSrc = "\0";

        return ExecutePosTest(paras, c_TEST_DESC);
    }

    #endregion

    #endregion // end for positive test scenarioes

    #region Helper methods for positive test scenarioes

    private bool ExecutePosTest(Parameters paras, string testDesc)
    {
        bool retVal = true;
        UpdateCounts(TestType.PositiveTest);
        string testId = GenerateTestId(TestType.PositiveTest);
        TestResult testResult = TestResult.NotRun;

        string testInfo = c_POS_TEST_PREFIX + this.posTestCount.ToString() + ": " + testDesc;
        object actualValue = null;

        TestLibrary.TestFramework.BeginScenario(testInfo);
        try
        {
            actualValue = this.CallTestMethod(paras);
            if (null == actualValue)
            {
                string errorDesc = "Enumerator is not retrieved as expected, actually it is null";
                errorDesc += paras.DataString + "\nTest scenario Id: " + testId;
                TestLibrary.TestFramework.LogError(GenerateErrorNum(totalTestCount << 1 - 1) + " TestId -" + testId, errorDesc);
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
    private int CallTestMethod(Parameters paras)
    {
        return paras.strSrc.GetHashCode();
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
