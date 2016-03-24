// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;
using TestLibrary;

/// <summary>
/// String.Compare Method (String, String) 
/// The comparison uses the current culture to obtain culture-specific information such as 
/// casing rules and the alphabetic order of individual characters. 
/// The comparison is performed using word sort rules. 
/// </summary>
class StringCompare5
{
    private const int c_MIN_STRING_LEN = 8;
    private const int c_MAX_STRING_LEN = 256;

    private const int c_MAX_SHORT_STR_LEN = 31;//short string (<32 chars)
    private const int c_MIN_LONG_STR_LEN = 257;//long string ( >256 chars)
    private const int c_MAX_LONG_STR_LEN = 65535;

    private const string c_POS_TEST_PREFIX = "PosTest";
    private const string c_NEG_TEST_PREFIX = "NegTest";

    private int totalTestCount;
    private int posTestCount ;
    private int negTestCount;
    private int passedTestCount;
    private int failedTestCount;

    private enum TestType { PositiveTest = 1, NegativeTest = 2 };
    private enum TestResult {NotRun = 0, PassedTest = 1, FailedTest = 2};
    internal struct Parameters
    {
        public string strA, strB;

        public string DataString
        {
            get 
            {
                string str = string.Format("\n\tFirst string: {0}, Second string: {1}", strA, strB);
                str += string.Format("\n\tFirst string length:{0}, second string length:{1}", strA.Length, strB.Length);
                return str;
            }
        }
    }

    //Default constructor to ninitial all kind of test counts
    public StringCompare5()
    {
        totalTestCount = posTestCount = negTestCount = 0;
        passedTestCount = failedTestCount = 0;
    }

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

    //Generate standard error number string
    //i.e "9", "12" is not proper. Instead they should be "009", "012"
    private string GenerateErrorNum(int errorNum)
    {
        string temp = errorNum.ToString();
        string errorNumStr = new string('0', 3 - temp.Length) + temp;
        return errorNumStr;
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

  public static int Main()
    {
        StringCompare5 sc = new StringCompare5();

        TestLibrary.TestFramework.BeginTestCase("for method: String.Compare Method (String, String)");
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
        
        //Negative test scenarios
        TestLibrary.TestFramework.LogInformation("[Negative]");
        //retVal = NegTest1() && retVal;

        return retVal;
    }

    #region Positive test scenarioes

    public bool PosTest1()
    {
        Parameters paras;

        const string c_testDesc = "Two null comparison.";
        const int c_expectedValue = 0;

        paras.strA = null;
        paras.strB = null;

        return ExecutePosTestZero(paras, c_expectedValue, c_testDesc);
    }

    public bool PosTest2()
    {
        Parameters paras;

        const string c_testDesc = "Null vs String.Empty";
        const bool c_expectedValue = true;

        paras.strA = null;
        paras.strB = string.Empty;

        return ExecutePosTestLesser(paras, c_expectedValue, c_testDesc);
    }

    public bool PosTest3()
    {
        Parameters paras;

        const string c_testDesc = "Long string(>256 chars) vs long string (>256 chars)";
        const bool c_expectedValue = true;

        string strBasic1 = TestLibrary.Generator.GetString(-55, false, c_MIN_LONG_STR_LEN, c_MAX_LONG_STR_LEN - 16);
        string strBasic2 = TestLibrary.Generator.GetString(-55, false, 8, 8);
        char ch = TestLibrary.Generator.GetChar(-55);
        paras.strA = strBasic1 + strBasic2;
        paras.strB = strBasic1 + strBasic2 + strBasic2;

        return ExecutePosTestLesser(paras, c_expectedValue, c_testDesc);
    }

    public bool PosTest4()
    {
        Parameters paras;

        const string c_testDesc = "Short string (<32 chars) vs long string(>256 chars)";
        const bool c_expectedValue = true;

        string strBasic = TestLibrary.Generator.GetString(-55, false, 20920, 20920);
        paras.strA = strBasic.Substring(0,this.GetInt32(1,c_MAX_SHORT_STR_LEN));
        paras.strB = strBasic;

        return ExecutePosTestLesser(paras, c_expectedValue, c_testDesc);
    }

    #endregion

    #region Helper methods for positive test scenarioes

    //Zero value returned from test method means that two substrings equal    
    private bool ExecutePosTestZero(Parameters paras, int expectedValue, string testDesc)
    {
        bool retVal = true;
        UpdateCounts(TestType.PositiveTest);

        string testInfo = c_POS_TEST_PREFIX + this.posTestCount.ToString() +": " + testDesc;
        int actualValue;

        TestResult testResult = TestResult.NotRun;

        TestLibrary.TestFramework.BeginScenario(testInfo);
        try
        {
            actualValue = this.CallTestMethod(paras);
            if (paras.strA != null && paras.strB != null) expectedValue = GlobLocHelper.OSCompare(paras.strA, paras.strB);
            if (expectedValue != actualValue)
            {
                string errorDesc = "Value is not " + expectedValue + " as expected: Actual(" + actualValue + ")";
                TestLibrary.TestFramework.LogError(GenerateErrorNum(this.totalTestCount), errorDesc);
                testResult = TestResult.FailedTest;
                retVal = false;
            }
            testResult = TestResult.PassedTest;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError(GenerateErrorNum(this.totalTestCount + 1), "Unexpected exception: " + e);
            testResult = TestResult.FailedTest;
            retVal = false;
        }

        UpdateCounts(testResult);
        return retVal;
    }

    // True value returned from test method means that two substrings do not equal
    private bool ExecutePosTestNonzero(Parameters paras, bool expectedValue, string testDesc)
    {
        bool retVal = true;
        UpdateCounts(TestType.PositiveTest);

        string testInfo = c_POS_TEST_PREFIX + this.posTestCount.ToString() + ": " + testDesc;
        bool actualValue;

        TestResult testResult = TestResult.NotRun;

        TestLibrary.TestFramework.BeginScenario(testInfo);
        try
        {
            actualValue = (0 != this.CallTestMethod(paras));
            if (paras.strA != null && paras.strB != null) expectedValue = (0 != GlobLocHelper.OSCompare(paras.strA, paras.strB));
            if (expectedValue != actualValue)
            {
                string errorDesc = "Value is not " + expectedValue + " as expected: Actual(" + actualValue + ")";
                TestLibrary.TestFramework.LogError(GenerateErrorNum(this.totalTestCount), errorDesc);
                testResult = TestResult.FailedTest;
                retVal = false;
            }
            testResult = TestResult.PassedTest;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError(GenerateErrorNum(this.totalTestCount + 1), "Unexpected exception: " + e);
            testResult = TestResult.FailedTest;
            retVal = false;
        }

        UpdateCounts(testResult);
        return retVal;
    }

    // True value returned from test method means that the first substring lesser than the second
    private bool ExecutePosTestLesser(Parameters paras, bool expectedValue, string testDesc)
    {
        bool retVal = true;
        UpdateCounts(TestType.PositiveTest);

        string testInfo = c_POS_TEST_PREFIX + this.posTestCount.ToString() + ": " + testDesc;
        bool actualValue;

        TestResult testResult = TestResult.NotRun;

        TestLibrary.TestFramework.BeginScenario(testInfo);
        try
        {
            actualValue = (0 > this.CallTestMethod(paras));
            if (paras.strA != null && paras.strB != null) expectedValue = (0 > GlobLocHelper.OSCompare(paras.strA, paras.strB));
            if (expectedValue != actualValue)
            {
                string errorDesc = "Value is not " + expectedValue + " as expected: Actual(" + actualValue + ")";
                errorDesc += paras.DataString;
                TestLibrary.TestFramework.LogError(GenerateErrorNum(this.totalTestCount), errorDesc);
                testResult = TestResult.FailedTest;
                retVal = false;
            }
            testResult = TestResult.PassedTest;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError(GenerateErrorNum(this.totalTestCount + 1), "Unexpected exception: " + e);
            testResult = TestResult.FailedTest;
            retVal = false;
        }

        UpdateCounts(testResult);
        return retVal;
    }

    #endregion

    #region Helper methods for negative test scenarioes
    //Test ArgumentOutOfRangeException
    private bool ExeNegTest_AOORE(Parameters paras, string testDesc, string errorDesc)
    {
        bool retVal = true;
        UpdateCounts(TestType.NegativeTest);

        string testInfo = c_NEG_TEST_PREFIX + this.negTestCount.ToString() + ": " + testDesc;

        TestResult testResult = TestResult.NotRun;

        TestLibrary.TestFramework.BeginScenario(testInfo);
        try
        {
            this.CallTestMethod(paras);
            TestLibrary.TestFramework.LogError(GenerateErrorNum((this.totalTestCount << 1) - 1), errorDesc);
            testResult = TestResult.FailedTest;
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
            testResult = TestResult.PassedTest;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError(GenerateErrorNum(this.totalTestCount << 1), "Unexpected exception: " + e);
            testResult = TestResult.FailedTest;
            retVal = false;
        }

        UpdateCounts(testResult);
        return retVal;
    }

    //Test ArgumentNullException
    private bool ExeNegTest_ANE(Parameters paras, string testDesc, string errorDesc)
    {
        bool retVal = true;
        UpdateCounts(TestType.NegativeTest);

        string testInfo = c_NEG_TEST_PREFIX + this.negTestCount.ToString() + ": " + testDesc;

        TestResult testResult = TestResult.NotRun;

        TestLibrary.TestFramework.BeginScenario(testInfo);
        try
        {
            this.CallTestMethod(paras);
            TestLibrary.TestFramework.LogError(GenerateErrorNum((this.totalTestCount << 1) - 1), errorDesc);
            testResult = TestResult.FailedTest;
            retVal = false;
        }
        catch (ArgumentNullException)
        {
            testResult = TestResult.PassedTest;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError(GenerateErrorNum(this.totalTestCount <<1), "Unexpected exception: " + e);
            testResult = TestResult.FailedTest;
            retVal = false;
        }

        UpdateCounts(testResult);
        return retVal;
    }
#endregion

    //Involke the test method
    //In this test case this method is System.String.Compare() with 7 parameters
    private int CallTestMethod(Parameters paras)
    {
        return string.Compare(paras.strA, paras.strB);
    }

    #region helper methods for generating test data
    private bool GetBoolean()
    {
        Int32 i = this.GetInt32(1,2);
        return (i == 1) ? true : false;
    }

    //Get a non-negative integer between minValue and maxValue
    private Int32 GetInt32(Int32 minValue, Int32 maxValue)
    {
        return minValue + TestLibrary.Generator.GetInt32(-55) % (maxValue - minValue);
    }

    private Int32 Min(Int32 i1, Int32 i2)
    {
        return (i1 <= i2) ? i1 : i2;
    }

    private Int32 Max(Int32 i1, Int32 i2)
    {
        return (i1 >= i2) ? i1 : i2;
    }
    #endregion
}

