// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;

/// <summary>
/// System.String.Equals(Object)  
/// Determines whether this instance of String and a specified object, 
/// which must also be a String object, have the same value. 
/// This method performs an ordinal (case-sensitive and culture-insensitive) comparison.
/// </summary>
class StringEquals1
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
        public object obj;

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
                if (null == obj)
                {
                    strB = "null";
                    lenB = 0;
                }
                else
                {
                    strB = obj.ToString();
                    lenB = strB.Length;
                }

                str = string.Format("\n[String value]\nSource: \"{0}\"\nObj: \"{1}\"", strA, strB);
                str += string.Format("\n[String length]\nSource: {0}\nObj: {1}", lenA, lenB);
                
                return str;
            }
        }
    }

    //Default constructor to ininitial all kind of test counts
    public StringEquals1()
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
        StringEquals1 sc = new StringEquals1();

        TestLibrary.TestFramework.BeginTestCase("for method: System.String.Equals(Object obj)");
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
        retVal = NegTest1() && retVal;

        return retVal;
    }

    #region Positive test scenarioes

    #region null, String.Empty and "\0" testing

    public bool PosTest1()
    {
        Parameters paras;

        const string c_TEST_DESC = "The obj to compare with is null";
        bool expectedValue = false;

        paras.strSrc = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        paras.obj = null;

        return ExecutePosTest(paras, expectedValue, c_TEST_DESC);
    }

    // PosTest2 to PosTest6 are new tests added in 8-4-2006 by v-yaduoj
    public bool PosTest2()
    {
        Parameters paras;

        const string c_TEST_DESC = "String.Empty vs null";
        bool expectedValue = false;

        paras.strSrc = String.Empty;
        paras.obj = null;

        return ExecutePosTest(paras, expectedValue, c_TEST_DESC);
    }

    public bool PosTest3()
    {
        Parameters paras;

        const string c_TEST_DESC = "String.Empty vs \"\"";
        bool expectedValue = true;

        paras.strSrc = String.Empty;
        paras.obj = "";

        return ExecutePosTest(paras, expectedValue, c_TEST_DESC);
    }

    public bool PosTest4()
    {
        Parameters paras;

        const string c_TEST_DESC = "String.Empty vs \"\\0\"";
        bool expectedValue = false;

        paras.strSrc = String.Empty;
        paras.obj = "\0";

        return ExecutePosTest(paras, expectedValue, c_TEST_DESC);
    }

    public bool PosTest5()
    {
        Parameters paras;

        const string c_TEST_DESC = "String.Empty vs unempty string";
        bool expectedValue = false;

        paras.strSrc = String.Empty;
        paras.obj = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);

        return ExecutePosTest(paras, expectedValue, c_TEST_DESC);
    }

    public bool PosTest6()
    {
        Parameters paras;

        const string c_TEST_DESC = "Embedded '\\0' string ";
        bool expectedValue = true;

        StringBuilder sb = new StringBuilder("This\0String\0Is\0Valid");
        paras.strSrc = sb.ToString();
        paras.obj = sb.ToString();

        return ExecutePosTest(paras, expectedValue, c_TEST_DESC);
    }
    // PosTest2 to PosTest6 are new tests added in 8-4-2006 by Noter(v-yaduoj)

    #endregion

    //The following region is new updates in 8-4-2006 by Noter(v-yaduoj)
    #region Tab and space testing

    public bool PosTest7()
    {
        Parameters paras;

        const string c_TEST_DESC = "Tab vs 4 spaces";
        bool expectedValue = false;

        paras.strSrc = "\t";
        paras.obj = new string('\x0020', 4); // new update 8-8-2006 Noter(v-yaduoj)

        return ExecutePosTest(paras, expectedValue, c_TEST_DESC);
    }

    public bool PosTest8()
    {
        Parameters paras;

        const string c_TEST_DESC = "Tab vs 8 spaces";
        bool expectedValue = false;

        paras.strSrc = "\t";
        paras.obj = new string('\x0020', 8); // new update 8-8-2006 Noter(v-yaduoj)

        return ExecutePosTest(paras, expectedValue, c_TEST_DESC);
    }

    #endregion

    #region Non-string type obj

    public bool PosTest9()
    {
        Parameters paras;

        const string c_TEST_DESC = "The type of obj to compare with is Int32";
        bool expectedValue = false;

        paras.obj = TestLibrary.Generator.GetInt32(-55);
        paras.strSrc = paras.obj.ToString();

        return ExecutePosTest(paras, expectedValue, c_TEST_DESC);
    }

    public bool PosTest10()
    {
        Parameters paras;

        const string c_TEST_DESC = "The type of obj to compare with is Int16";
        bool expectedValue = false;

        paras.obj = TestLibrary.Generator.GetInt16(-55);
        paras.strSrc = paras.obj.ToString();

        return ExecutePosTest(paras, expectedValue, c_TEST_DESC);
    }

    public bool PosTest11()
    {
        Parameters paras;

        const string c_TEST_DESC = "The type of obj to compare with is Int64";
        bool expectedValue = false;

        paras.obj = TestLibrary.Generator.GetInt64(-55);
        paras.strSrc = paras.obj.ToString();

        return ExecutePosTest(paras, expectedValue, c_TEST_DESC);
    }

    public bool PosTest12()
    {
        Parameters paras;

        const string c_TEST_DESC = "The type of obj to compare with is Double";
        bool expectedValue = false;

        paras.obj = TestLibrary.Generator.GetDouble(-55);
        paras.strSrc = paras.obj.ToString();

        return ExecutePosTest(paras, expectedValue, c_TEST_DESC);
    }

    public bool PosTest13()
    {
        Parameters paras;

        const string c_TEST_DESC = "The type of obj to compare with is single";
        bool expectedValue = false;

        paras.obj = TestLibrary.Generator.GetSingle(-55);
        paras.strSrc = paras.obj.ToString();

        return ExecutePosTest(paras, expectedValue, c_TEST_DESC);
    }

    public bool PosTest14()
    {
        Parameters paras;

        const string c_TEST_DESC = "The type of obj to compare with is string[]";
        bool expectedValue = false;

        paras.obj = TestLibrary.Generator.GetStrings(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        paras.strSrc = paras.obj.ToString();

        return ExecutePosTest(paras, expectedValue, c_TEST_DESC);
    }

    public bool PosTest15()
    {
        Parameters paras;

        const string c_TEST_DESC = "The type of obj to compare with is byte";
        bool expectedValue = false;

        paras.obj = TestLibrary.Generator.GetByte(-55);
        paras.strSrc = paras.obj.ToString();

        return ExecutePosTest(paras, expectedValue, c_TEST_DESC);
    }

    public bool PosTest16()
    {
        Parameters paras;

        const string c_TEST_DESC = "The type of obj to compare with is Byte[]";
        bool expectedValue = false;

        paras.obj = new byte[] {1,2,3};
        paras.strSrc = paras.obj.ToString();

        return ExecutePosTest(paras, expectedValue, c_TEST_DESC);
    }

    public bool PosTest17()
    {
        Parameters paras;

        const string c_TEST_DESC = "The type of obj to compare with is char";
        bool expectedValue = false;

        paras.obj = TestLibrary.Generator.GetChar(-55);
        paras.strSrc = paras.obj.ToString();

        return ExecutePosTest(paras, expectedValue, c_TEST_DESC);
    }

    public bool PosTest18()
    {
        Parameters paras;

        const string c_TEST_DESC = "The type of obj to compare with is char[]";
        bool expectedValue = false;

        paras.obj = new char[] {'1', 'a', '\x098A'};
        paras.strSrc = paras.obj.ToString();

        return ExecutePosTest(paras, expectedValue, c_TEST_DESC);
    }

    #endregion

    #region Case sensitive testing

    public bool PosTest19()
    {
        Parameters paras;

        const string c_TEST_DESC = "Case sensitive testing";
        bool expectedValue = false;

        char ch = this.GetUpperChar();
        paras.obj = ch.ToString();
        paras.strSrc = char.ToLower(ch).ToString();

        return ExecutePosTest(paras, expectedValue, c_TEST_DESC);
    }

    //Greek Sigma: 
    //Two lower case Sigma: (0x03C2), (0x03C3) 
    //One upper case Sigma: (0x03A3) 
    //where 2 lower case characters have the same upper case character.
    public bool PosTest20()
    {
        Parameters paras;

        const string c_TEST_DESC = "Asymmetric casing: Greek Sigma character, different case";
        bool expectedValue = false;
        
        paras.obj = c_GREEK_SIGMA_STR_A;
        paras.strSrc = c_GREEK_SIGMA_STR_B;

        return ExecutePosTest(paras, expectedValue, c_TEST_DESC);
    }

    public bool PosTest21()
    {
        Parameters paras;

        const string c_TEST_DESC = "Asymmetric casing: Greek Sigma character, both upper case";
        bool expectedValue = true;
        
        string str1 = c_GREEK_SIGMA_STR_A;
        string str2 = c_GREEK_SIGMA_STR_B;
        paras.obj = str1.ToUpper();
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
                // new updates 8-6-2006 Noter(v-yaduoj)
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

    #region Negative test scenarioes

    public bool NegTest1()
    {
        Parameters paras;

        const string c_TEST_DESC = "The instance of source string is null";
        const string c_ERROR_DESC = "NullReferenceException is not thrown as expected";

        paras.strSrc = null;
        paras.obj = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);

        return ExeNegTest_NRE(paras, c_TEST_DESC, c_ERROR_DESC);
    }

    #endregion //end for negative test scenarioes

    #region Helper methods for negative test scenarioes

    //Test NullReferenceException
    private bool ExeNegTest_NRE(Parameters paras, string testDesc, string errorDesc)
    {
        bool retVal = true;
        UpdateCounts(TestType.NegativeTest);
        string testId = GenerateTestId(TestType.NegativeTest);
        string testInfo = c_NEG_TEST_PREFIX + this.negTestCount.ToString() + ": " + testDesc;
        TestResult testResult = TestResult.NotRun;

        TestLibrary.TestFramework.BeginScenario(testInfo);
        try
        {
            this.CallTestMethod(paras);
            TestLibrary.TestFramework.LogError(GenerateErrorNum((this.totalTestCount << 1) - 1) + " TestId -" + testId, errorDesc + paras.DataString);
            testResult = TestResult.FailedTest;
            retVal = false;
        }
        catch (NullReferenceException)
        {
            testResult = TestResult.PassedTest;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError(GenerateErrorNum(this.totalTestCount << 1) + " TestId -" + testId, "Unexpected exception: " + e + paras.DataString);
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
        return paras.strSrc.Equals(paras.obj);
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

