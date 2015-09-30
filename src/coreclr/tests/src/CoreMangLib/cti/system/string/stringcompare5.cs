using System;

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

        public string DataString //new update
        {
            get
            {
                string str1, str2, str;
                int lenA, lenB;

                if (null == strA)
                {
                    str1 = "null";
                    lenA = 0;
                }
                else
                {
                    str1 = strA;
                    lenA = str1.Length;
                }
                if (null == strB)
                {
                    str2 = "null";
                    lenB = 0;
                }
                else
                {
                    str2 = strB;
                    lenB = str2.Length;
                }
                str = string.Format("\n[String value]\nFirst: \"{0}\"\nSecond: \"{1}\"", str1, str2);
                str += string.Format("\n[String's length]\nFirst: {0}\nSecond: {1}", lenA, lenB);

                return str;
            }
        }
    }

    //Default constructor to ininitial all kind of test counts
    public StringCompare5()
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
    // new update: add method to generate testId automatically (8-2-2006 by v-yaduoj)
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
        StringCompare5 sc = new StringCompare5();

        TestLibrary.TestFramework.BeginTestCase("for method: System.String.Compare(String, String)");
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
        retVal = PosTest12() && retVal;
        retVal = PosTest13() && retVal;
        retVal = PosTest14() && retVal;
        retVal = PosTest15() && retVal;
        retVal = PosTest16() && retVal;
        retVal = PosTest17() && retVal;
        retVal = PosTest18() && retVal;
        retVal = PosTest19() && retVal;
        retVal = PosTest20() && retVal;
        retVal = PosTest21() && retVal;
        
        //Negative test scenarios
        TestLibrary.TestFramework.LogInformation("[Negative]");
        //retVal = NegTest1() && retVal;

        return retVal;
    }

    #region Positive test scenarioes

    #region Null and String.Empty testing

    public bool PosTest1()
    {
        Parameters paras;

        const string c_TEST_DESC = "Two nulls comparison.";
        int expectedValue = 0;

        paras.strA = null;
        paras.strB = null;

        return ExecutePosTestZero(paras, expectedValue, c_TEST_DESC);
    }

    public bool PosTest2()
    {
        Parameters paras;

        const string c_TEST_DESC = "Null vs String.Empty";
        bool expectedValue = true;

        paras.strA = null;
        paras.strB = string.Empty;

        return ExecutePosTestLesser(paras, expectedValue, c_TEST_DESC);
    }

    //String.Empty == ""
    public bool PosTest3()
    {
        Parameters paras;

        const string c_TEST_DESC = "String.Empty vs \"\"";
        int expectedValue = 0;

        paras.strA = string.Empty;
        paras.strB = "";

        return ExecutePosTestZero(paras, expectedValue, c_TEST_DESC);
    }

    //String.Empty == "\0"
    public bool PosTest4()
    {
        Parameters paras;

        const string c_TEST_DESC = "String.Empty vs \"\\0\"";
        int expectedValue = TestLibrary.Utilities.IsWindows?0:(-1);

        paras.strA = string.Empty;
        paras.strB = "\0";

        return ExecutePosTestZero(paras, expectedValue, c_TEST_DESC);
    }

    public bool PosTest5()
    {
        Parameters paras;

        const string c_TEST_DESC = "Null vs unempty string";
        bool expectedValue = true;

        paras.strA = null;
        paras.strB = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);

        return ExecutePosTestLesser(paras, expectedValue, c_TEST_DESC);
    }

    //Embedded "\0"s (important for Interop layer)
    //such as ¡°This\0String\0Is\0Valid¡±
    public bool PosTest21()
    {
        Parameters paras;
        const string c_testDesc = @"Embedded '\0' (important for Interop layer)";
        const int c_expectedValue = 0;

        string strBasic1 = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        string strBasic2 = TestLibrary.Generator.GetString(-55, false, 2, 10);
        paras.strA = strBasic1 + "\0" + strBasic2 + "\0";
        paras.strB = strBasic1 + "\0" + strBasic2 + "\0";

        return ExecutePosTestZero(paras, c_expectedValue, c_testDesc);
    }

    #endregion

    #region Long string (> 256 chars), short string (< 32 chars)

    public bool PosTest6()
    {
        Parameters paras;

        const string c_TEST_DESC = "Long string(>256 chars) vs long string (>256 chars)";
        bool expectedValue = true;

        int i = this.GetInt32(c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        string strBasic2 = TestLibrary.Generator.GetString(-55, false, i, i);
        string strBasic1 = TestLibrary.Generator.GetString(-55, false, c_MIN_LONG_STR_LEN - i, c_MAX_LONG_STR_LEN - (i << 1));
        paras.strA = strBasic1 + strBasic2;
        paras.strB = strBasic1 + strBasic2 + strBasic2;

        return ExecutePosTestLesser(paras, expectedValue, c_TEST_DESC);
    }

    public bool PosTest7()
    {
        Parameters paras;

        const string c_TEST_DESC = "Short string (<32 chars) vs long string(>256 chars)";
        bool expectedValue = true;

        string strBasic = TestLibrary.Generator.GetString(-55, false, 1, c_MAX_SHORT_STR_LEN);
        paras.strA = strBasic;
        paras.strB = strBasic + TestLibrary.Generator.GetString(-55, false, c_MIN_LONG_STR_LEN - strBasic.Length, c_MAX_LONG_STR_LEN - strBasic.Length);

        return ExecutePosTestLesser(paras, expectedValue, c_TEST_DESC);
    }

    public bool PosTest8()
    {
        Parameters paras;

        const string c_TEST_DESC = "Short string (<32 chars) vs short string(<32 chars)";
        bool expectedValue = true;

        int i = this.GetInt32(1, c_MAX_SHORT_STR_LEN - 8);
        string strBasic = TestLibrary.Generator.GetString(-55, false, 1, i);
        paras.strA = strBasic;
        paras.strB = strBasic  + TestLibrary.Generator.GetString(-55, false, 1, c_MAX_SHORT_STR_LEN -strBasic.Length);

        return ExecutePosTestLesser(paras, expectedValue, c_TEST_DESC);
    }

    #endregion

    #region Single char string

    public bool PosTest9()
    {
        Parameters paras;

        const string c_TEST_DESC = "Single char string comparison.";
        int expectedValue = 0;

        char ch = TestLibrary.Generator.GetChar(-55);

        paras.strA = new string(ch, 1);
        paras.strB = new string(ch, 1);

        return ExecutePosTestZero(paras, expectedValue, c_TEST_DESC);
    }

    #endregion

    #region String with spaces

    public bool PosTest10()
    {
        Parameters paras;

        const string c_TEST_DESC = @"Space definition: ' ' == '\x0020' ";
        int expectedValue = 0;

        paras.strA = new string(' ', 1);
        paras.strB = new string('\x0020', 1);

        return ExecutePosTestZero(paras, expectedValue, c_TEST_DESC);
    }

    public bool PosTest11()
    {
        Parameters paras;

        const string c_TEST_DESC = "Two strings consist of different number spaces";
        bool expectedValue = true;

        char ch = '\x0020';
        paras.strA = new string(ch, this.GetInt32(1,8));
        paras.strB = new string(ch, this.GetInt32(9,16));

        return ExecutePosTestNonzero(paras, expectedValue, c_TEST_DESC);
    }

    public bool PosTest12()
    {
        Parameters paras;

        const string c_TEST_DESC = "Two strings different number spaces embedded";
        bool expectedValue = true;

        char ch = '\x0020'; // ' ' == '\x0020'
        string strBasic1 = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        string strBasic2 = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        paras.strA = strBasic1 + new string(ch, this.GetInt32(1, 8)) + strBasic2;
        paras.strB = strBasic1 + new string(ch, this.GetInt32(9, 16)) + strBasic2;

        return ExecutePosTestNonzero(paras, expectedValue, c_TEST_DESC);
    }

    #endregion

    #region String with tabs

    public bool PosTest13()
    {
        Parameters paras;

        const string c_TEST_DESC = @"Tab definiton '\t' == '\x0009' ";
        int expectedValue = 0;

        paras.strA = new string('\t', 1);
        paras.strB = new string('\x0009', 1);

        return ExecutePosTestZero(paras, expectedValue, c_TEST_DESC);
    }

    public bool PosTest14()
    {
        Parameters paras;

        const string c_TEST_DESC = "Two strings consist of different number tabs";
        bool expectedValue = true;

        char ch = '\t';
        paras.strA = new string(ch, this.GetInt32(1, 8));
        paras.strB = new string(ch, this.GetInt32(9, 16));

        return ExecutePosTestNonzero(paras, expectedValue, c_TEST_DESC);
    }

    public bool PosTest15()
    {
        Parameters paras;

        const string c_TEST_DESC = "Two strings different number spaces embedded";
        bool expectedValue = true;

        char ch = '\t';
        string strBasic1 = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        string strBasic2 = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        paras.strA = strBasic1 + new string(ch, this.GetInt32(1, 8)) + strBasic2;
        paras.strB = strBasic1 + new string(ch, this.GetInt32(9, 16)) + strBasic2;

        return ExecutePosTestNonzero(paras, expectedValue, c_TEST_DESC);
    }

    public bool PosTest16()
    {
        Parameters paras;

        const string c_TEST_DESC = "Tab vs 4 spaces";
        bool expectedValue = true;

        paras.strA = new string('\t', 1);
        paras.strB = new string('\x0020', 4);

        return ExecutePosTestNonzero(paras, expectedValue, c_TEST_DESC);
    }

    public bool PosTest17()
    {
        Parameters paras;

        const string c_TEST_DESC = "Tab vs 8 spaces";
        bool expectedValue = true;

        paras.strA = new string('\t', 1);
        paras.strB = new string('\x0020', 8);

        return ExecutePosTestNonzero(paras, expectedValue, c_TEST_DESC);
    }

    #endregion 

    #region String with newlines

    public bool PosTest18()
    {
        Parameters paras;

        const string c_TEST_DESC = @"Newline definiton '\n' == '\x000A' ";
        int expectedValue = 0;

        paras.strA = new string('\n', 1);
        paras.strB = new string('\x000A', 1);

        return ExecutePosTestZero(paras, expectedValue, c_TEST_DESC);
    }

    public bool PosTest19()
    {
        Parameters paras;

        const string c_TEST_DESC = "Two strings consist of different number newlines";
        bool expectedValue = true;

        char ch = '\n';
        paras.strA = new string(ch, this.GetInt32(1, 8));
        paras.strB = new string(ch, this.GetInt32(9, 16));

        return ExecutePosTestNonzero(paras, expectedValue, c_TEST_DESC);
    }

    public bool PosTest20()
    {
        Parameters paras;

        const string c_TEST_DESC = "Two strings different number spaces embedded";
        bool expectedValue = true;

        char ch = '\n';
        string strBasic1 = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        string strBasic2 = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        paras.strA = strBasic1 + new string(ch, this.GetInt32(1, 8)) + strBasic2;
        paras.strB = strBasic1 + new string(ch, this.GetInt32(9, 16)) + strBasic2;

        return ExecutePosTestNonzero(paras, expectedValue, c_TEST_DESC);
    }

    #endregion

    #endregion //end for positive test scenarioes

    #region Helper methods for positive test scenarioes

    //Zero value returned from test method means that two substrings equal    
    private bool ExecutePosTestZero(Parameters paras, int expectedValue, string testDesc)
    {
        bool retVal = true;
        UpdateCounts(TestType.PositiveTest);
        string testId = GenerateTestId(TestType.PositiveTest); // new update (8-2-2006 by v-yaduoj)

        string testInfo = c_POS_TEST_PREFIX + this.posTestCount.ToString() + ": " + testDesc;
        int actualValue;

        TestResult testResult = TestResult.NotRun;

        TestLibrary.TestFramework.BeginScenario(testInfo);
        try
        {
            actualValue = this.CallTestMethod(paras);
            if (paras.strA != null && paras.strB != null) expectedValue = (TestLibrary.GlobLocHelper.OSCompare(paras.strA, paras.strB));
            if (expectedValue != actualValue)
            {
                string errorDesc = "Value is not " + expectedValue + " as expected: Actual(" + actualValue + ")";
                errorDesc += paras.DataString;
                // new update: modify the error number and add testId information (8-3-2006 by v-yaduoj)
                TestLibrary.TestFramework.LogError(GenerateErrorNum(totalTestCount << 1 - 1) + " TestId -" + testId, errorDesc);
                testResult = TestResult.FailedTest;
                retVal = false;
            }
            testResult = TestResult.PassedTest;
        }
        catch (Exception e)
        {
            // new update: modify the error number and add testId information (8-3-2006 by v-yaduoj)
            TestLibrary.TestFramework.LogError(GenerateErrorNum(totalTestCount << 1) + " TestId -" + testId, "Unexpected exception: " + e + paras.DataString);
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
        string testId = GenerateTestId(TestType.PositiveTest);// new update (8-2-2006 by v-yaduoj)

        string testInfo = c_POS_TEST_PREFIX + this.posTestCount.ToString() + ": " + testDesc;
        bool actualValue;

        TestResult testResult = TestResult.NotRun;

        TestLibrary.TestFramework.BeginScenario(testInfo);
        try
        {
            actualValue = (0 != this.CallTestMethod(paras));
            if (paras.strA != null && paras.strB != null) expectedValue =  (0 != TestLibrary.GlobLocHelper.OSCompare(paras.strA, paras.strB));
            if (expectedValue != actualValue)
            {
                string errorDesc = "Value is not " + expectedValue + " as expected: Actual(" + actualValue + ")";
                errorDesc += paras.DataString;
                // new update: modify the error number and add testId information (8-3-2006 by v-yaduoj)
                TestLibrary.TestFramework.LogError(GenerateErrorNum(totalTestCount << 1 - 1) + " TestId -" + testId, errorDesc);
                testResult = TestResult.FailedTest;
                retVal = false;
            }
            testResult = TestResult.PassedTest;
        }
        catch (Exception e)
        {
            // new update: modify the error number and add testId information (8-3-2006 by v-yaduoj)
            TestLibrary.TestFramework.LogError(GenerateErrorNum(totalTestCount << 1) + " TestId -" + testId, "Unexpected exception: " + e + paras.DataString);
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
        string testId = GenerateTestId(TestType.PositiveTest);// new update

        string testInfo = c_POS_TEST_PREFIX + this.posTestCount.ToString() + ": " + testDesc;
        bool actualValue;

        TestResult testResult = TestResult.NotRun;

        TestLibrary.TestFramework.BeginScenario(testInfo);
        try
        {
            actualValue = (0 > this.CallTestMethod(paras));
            if (paras.strA != null && paras.strB != null) expectedValue = (0 > TestLibrary.GlobLocHelper.OSCompare(paras.strA, paras.strB));
            if (expectedValue != actualValue)
            {
                string errorDesc = "Value is not " + expectedValue + " as expected: Actual(" + actualValue + ")";
                errorDesc += paras.DataString;
                // new update: modify the error number and add testId information (8-3-2006 by v-yaduoj)
                TestLibrary.TestFramework.LogError(GenerateErrorNum(totalTestCount << 1 - 1) + " TestId -" + testId, errorDesc);
                testResult = TestResult.FailedTest;
                retVal = false;
            }
            testResult = TestResult.PassedTest;
        }
        catch (Exception e)
        {
            // new update: modify the error number and add testId information (8-3-2006 by v-yaduoj)
            TestLibrary.TestFramework.LogError(GenerateErrorNum(totalTestCount << 1) + " TestId -" + testId, "Unexpected exception: " + e + paras.DataString);
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
        string testId = GenerateTestId(TestType.NegativeTest);// new update: (8-2-2006 by v-yaduoj)
        string testInfo = c_NEG_TEST_PREFIX + this.negTestCount.ToString() + ": " + testDesc;
        TestResult testResult = TestResult.NotRun;

        TestLibrary.TestFramework.BeginScenario(testInfo);
        try
        {
            this.CallTestMethod(paras);
            // new update: modify the error number and add testId information (8-2-2006 by v-yaduoj)
            TestLibrary.TestFramework.LogError(GenerateErrorNum((this.totalTestCount << 1) - 1) + " TestId -" + testId, errorDesc + paras.DataString);
            testResult = TestResult.FailedTest;
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
            testResult = TestResult.PassedTest;
        }
        catch (Exception e)
        {
            // new update: modify the error number and add testId information (8-2-2006 by v-yaduoj)
            TestLibrary.TestFramework.LogError(GenerateErrorNum(this.totalTestCount << 1) + " TestId -" + testId, "Unexpected exception: " + e + paras.DataString);
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
        string testId = GenerateTestId(TestType.NegativeTest);// new update: (8-2-2006 by v-yaduoj)
        string testInfo = c_NEG_TEST_PREFIX + this.negTestCount.ToString() + ": " + testDesc;
        TestResult testResult = TestResult.NotRun;

        TestLibrary.TestFramework.BeginScenario(testInfo);
        try
        {
            this.CallTestMethod(paras);
            // new update: modify the error number and add testId information (8-2-2006 by v-yaduoj)
            TestLibrary.TestFramework.LogError(GenerateErrorNum((this.totalTestCount << 1) - 1) + " TestId -" + testId, errorDesc + paras.DataString);
            testResult = TestResult.FailedTest;
            retVal = false;
        }
        catch (ArgumentNullException)
        {
            testResult = TestResult.PassedTest;
        }
        catch (Exception e)
        {
            // new update: modify the error number and add testId information (8-2-2006 by v-yaduoj)
            TestLibrary.TestFramework.LogError(GenerateErrorNum(this.totalTestCount << 1) + " TestId -" + testId, "Unexpected exception: " + e + paras.DataString);
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
    #endregion
}

