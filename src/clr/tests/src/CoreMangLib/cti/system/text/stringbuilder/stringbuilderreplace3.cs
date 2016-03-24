// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.Text.StringBuilder.Replace(oldChar,newChar,startIndex,count)
/// </summary>
class StringBuilderReplace3
{
    private const int c_MIN_STRING_LEN = 8;
    private const int c_MAX_STRING_LEN = 256;

    private const int c_MAX_SHORT_STR_LEN = 31;//short string (<32 chars)
    private const int c_MIN_LONG_STR_LEN = 257;//long string ( >256 chars)
    private const int c_MAX_LONG_STR_LEN = 65535;

    public static int Main()
    {
        StringBuilderReplace3 test = new StringBuilderReplace3();

        TestLibrary.TestFramework.BeginTestCase("for Method:System.Text.StringBuilder.Replace(String,String)");

        if (test.RunTests())
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

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal  = NegTest1() && retVal;
        retVal  = NegTest2() && retVal;

        return retVal;
    }

    #region Positive test scenarios
    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest1: Old string does not exist in source StringBuilder, random new string. ";
        const string c_TEST_ID = "P001";

        string strSrc = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(strSrc);

        string newString = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        string oldString = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        while(-1 < strSrc.IndexOf(oldString))
        {
            oldString = TestLibrary.Generator.GetString(-55, false,c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        }

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        System.Text.StringBuilder newSB = new System.Text.StringBuilder(strSrc.Replace(oldString, newString));


        try
        {
            stringBuilder = stringBuilder.Replace(oldString, newString);

            if (0 != string.Compare(newSB.ToString(), stringBuilder.ToString()))
            {
                string errorDesc = "Value is not " + newSB.ToString() + " as expected: Actual(" + stringBuilder.ToString() + ")";
                errorDesc += GetDataString(strSrc, oldString, newString);
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID,errorDesc);
                retVal = false;
            }
        }
        catch(Exception e)
        {
            TestLibrary.TestFramework.LogError("002" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(strSrc, oldString, newString));
            retVal = false;  
        }


        return retVal;
    }


    public bool PosTest2()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest1: Old string  exist in source StringBuilder, random new string. ";
        const string c_TEST_ID = "P002";

        string strSrc = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(strSrc);

        string  newString = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
	// 0 <= startIndex < strSrc.Length ( == startIndex is a valid index into strSrc)
        int     startIndex  = TestLibrary.Generator.GetInt32(-55) % strSrc.Length;
	
	// we require that: substrSize >= 1
	//                  substrSize + startIndex <= strSrc.Length
	// which is implied by 1 <= substrSize <= strSrc.Length-startIndex
	// now for any Int32 k, 0 <= k % strSrc.Length-startIndex <= (strSrcLength-startIndex)-1
	//        and so        1 <= 1 + (k % strSrc.Length-startIndex) <= strSrcLength-startIndex
	// so generate k randomly and let substrSize := 1 + (k % strSrc.Length - startIndex).
	int substrSize = (TestLibrary.Generator.GetInt32(-55) % (strSrc.Length - startIndex)) + 1;
        string oldString = strSrc.Substring(startIndex, substrSize);
        System.Text.StringBuilder newSB = new System.Text.StringBuilder(strSrc.Replace(oldString, newString));

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            stringBuilder = stringBuilder.Replace(oldString, newString);
            if (0 != string.Compare(newSB.ToString(), stringBuilder.ToString()))
            {
                string errorDesc = "Value is not " + newSB.ToString() + " as expected: Actual(" + stringBuilder.ToString() + ")";
                errorDesc += GetDataString(strSrc, oldString, newString);
                TestLibrary.TestFramework.LogError("003" + " TestId-" + c_TEST_ID,errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(strSrc, oldString, newString));
            retVal = false;
        }


        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;


        const string c_TEST_DESC = "PosTest1:  source StringBuilder is empty, random new string. ";
        const string c_TEST_ID = "P003";

        string strSrc = string.Empty;
        System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(strSrc);

        string newString = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        string oldString = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            stringBuilder = stringBuilder.Replace(oldString, newString);
            if (string.Empty != stringBuilder.ToString())
            {
                string errorDesc = "Value is not string of empty as expected: Actual(" + stringBuilder.ToString() + ")";
                errorDesc += GetDataString(strSrc, oldString, newString);
                TestLibrary.TestFramework.LogError("005" + " TestId-" + c_TEST_ID+errorDesc,errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(strSrc, oldString, newString));
            retVal = false;
        }


        return retVal;
    }
    #endregion

    #region Negative test scenarios
    public bool NegTest1()
    {
        bool retVal = true;

        const string c_TEST_DESC = "NegTest1: old string value is null reference.";
        const string c_TEST_ID = "N001";

        string strSrc = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        string oldStr = null;
        string newStr = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);

        System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(strSrc);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            stringBuilder.Replace(oldStr, newStr);
            TestLibrary.TestFramework.LogError("007" + " TestId-" + c_TEST_ID, "ArgumentNullException is not thrown as expected." + GetDataString(strSrc, oldStr, newStr));
            retVal = false;
        }
        catch (ArgumentNullException)
        { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(strSrc, oldStr, newStr));
            retVal = false;
        }

        return retVal;

    }
    public bool NegTest2()
    {
        bool retVal = true;

        const string c_TEST_DESC = "NegTest1: old string value is string of empty.";
        const string c_TEST_ID = "N002";

        string strSrc = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        string oldStr = string.Empty;
        string newStr = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);

        System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(strSrc);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            stringBuilder.Replace(oldStr, newStr);
            TestLibrary.TestFramework.LogError("009" + " TestId-" + c_TEST_ID, "ArgumentNullException is not thrown as expected." + GetDataString(strSrc, oldStr, newStr));
            retVal = false;
        }
        catch (ArgumentException)
        { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(strSrc, oldStr, newStr));
            retVal = false;
        }

        return retVal;

    }
   

  
    #endregion

    private string GetDataString(string strSrc, string oldStr, string newStr)
    {
        string str1, str;
        int len1;

        if (null == strSrc)
        {
            str1 = "null";
            len1 = 0;
        }
        else
        {
            str1 = strSrc;
            len1 = strSrc.Length;
        }

        str = "\n [Source string value] \n \" " + str1 + " \n \" [Length of source string] \n " + len1.ToString()
                + "\n[Old string]\n \" " + oldStr + "\" \n[New string]\n \"" + newStr + "\"";
       

        return str;
    }
}
