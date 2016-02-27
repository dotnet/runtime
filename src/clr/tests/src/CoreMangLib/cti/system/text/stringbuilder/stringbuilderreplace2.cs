// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.Text.StringBuilder.Replace(oldChar,newChar,startIndex,count)
/// </summary>
class StringBuilderReplace2
{
    private const int c_MIN_STRING_LEN = 8;
    private const int c_MAX_STRING_LEN = 256;

    private const int c_MAX_SHORT_STR_LEN = 31;//short string (<32 chars)
    private const int c_MIN_LONG_STR_LEN = 257;//long string ( >256 chars)
    private const int c_MAX_LONG_STR_LEN = 65535;

    public static int Main()
    {
        StringBuilderReplace2 test = new StringBuilderReplace2();

        TestLibrary.TestFramework.BeginTestCase("for Method:System.Text.StringBuilder.Replace(oldChar,newChar,indexStart,count)");

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
        retVal = posTest2() && retVal;
        retVal = PosTest3() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;

        return retVal;
    }

    #region Positive test scenarios
    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest1: Old char does not exist in source StringBuilder, random new char. ";
        const string c_TEST_ID = "P001";

        char oldChar = TestLibrary.Generator.GetChar(-55);
        int length = c_MIN_STRING_LEN+TestLibrary.Generator.GetInt32(-55) %(c_MAX_STRING_LEN-c_MIN_STRING_LEN);

        int index = 0;
        char ch;
        string oldString = string.Empty;
        while (index < length)
        {
            ch = TestLibrary.Generator.GetChar(-55);
            if (oldChar == ch)
            {
                continue;
            }
            oldString += ch.ToString();
            index++;
        }
        
        int     startIndex  = TestLibrary.Generator.GetInt32(-55) % oldString.Length;
        int     count       = TestLibrary.Generator.GetInt32(-55) % (oldString.Length - startIndex);
        int     indexChar   = oldString.IndexOf(oldChar, startIndex, count);
        char    newChar     = TestLibrary.Generator.GetChar(-55);
        while (oldString.IndexOf(newChar, startIndex, count) > -1)
        {
            newChar = TestLibrary.Generator.GetChar(-55);
        }
        
        System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(oldString);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            stringBuilder.Replace(oldChar, newChar,startIndex,count);
          
            
            if ((indexChar != stringBuilder.ToString().IndexOf(newChar,startIndex,count)) || (-1 != stringBuilder.ToString().IndexOf(oldChar,startIndex,count)))
            {
                TestLibrary.TestFramework.LogError("001", "StringBuilder\"" + oldString + "\" can't corrently Replace \"" + oldChar.ToString() + "\" to \"" + newChar + "\" from index of StringBuilder :" + startIndex.ToString() + "to count " + count.ToString());
                retVal = false;
            }
            
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e );
            retVal = false;
        }

        return retVal;
    }

    public bool posTest2()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest2: Old char exists in source StringBuilder, random new char. ";
        const string c_TEST_ID = "P002";

        string oldString = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(oldString);

        int     startIndex  = TestLibrary.Generator.GetInt32(-55) % oldString.Length;
        int     count       = TestLibrary.Generator.GetInt32(-55) % (oldString.Length - startIndex);

        char oldChar;
        if (count == 0)
        {
            oldChar = oldString[0];
        }
        else 
        {
            oldChar = oldString[startIndex + TestLibrary.Generator.GetInt32(-55) % count];
        }
        
        int     indexChar   = oldString.IndexOf(oldChar, startIndex, count);

        char newChar = TestLibrary.Generator.GetChar(-55);
        while (oldString.IndexOf(newChar, startIndex, count) > -1)
        {
            newChar = TestLibrary.Generator.GetChar(-55);
        }

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            stringBuilder.Replace(oldChar, newChar, startIndex, count);


            if ((indexChar != stringBuilder.ToString().IndexOf(newChar, startIndex, count)) || (-1 != stringBuilder.ToString().IndexOf(oldChar, startIndex, count)))
            {
                TestLibrary.TestFramework.LogError("003", "StringBuilder\"" + oldString + "\" can't corrently Replace \"" + oldChar.ToString() + "\" to \"" + newChar + "\" from index of StringBuilder :" + startIndex.ToString() + "to count " + count.ToString());
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e );
            retVal = false;
        }

        return retVal;     
    }

    public bool PosTest3()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest3: Verify  StringBuilder Replace char that stringBuilder includes ";
        const string c_TEST_ID = "P003";

        string oldString = TestLibrary.Generator.GetString(-55, false, 0, 0);
        System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(oldString);

        int startIndex = 0;
        int count = 0;
        char oldChar = TestLibrary.Generator.GetChar(-55);
        char newChar = TestLibrary.Generator.GetChar(-55);
        int indexChar = oldString.IndexOf(oldString, startIndex, count);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

       
        try
        {
            
            if (-1 != stringBuilder.ToString().IndexOf(newChar, startIndex, count))
            {
                TestLibrary.TestFramework.LogError("005", "StringBuilder\"" + oldString + "\" can't corrently Replace \"" + oldChar.ToString() + "\" to \"" + newChar + "\" from index of StringBuilder :" + startIndex.ToString() + "to count " + count.ToString());
                retVal = false;
            }
           
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e );
            retVal = false;
        }

        return retVal;

    }
    #endregion

    #region NegitiveTesting

    public bool NegTest1()
    {
        bool retVal = true;

        const string c_TEST_DESC = "NegTest1: Start index is  larger than source StringBuilder's length";
        const string c_TEST_ID = "N001";

        string strSrc   = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        char   oldChar  = TestLibrary.Generator.GetChar(-55);
        char   newChar  = TestLibrary.Generator.GetChar(-55);
        int startIndex  = strSrc.Length +TestLibrary.Generator.GetInt32(-55) % (Int32.MaxValue - strSrc.Length);
        int count       = TestLibrary.Generator.GetInt32(-55);

        System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(strSrc);

       

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            stringBuilder.Replace(oldChar, newChar, startIndex, count);
            TestLibrary.TestFramework.LogError("007" + " TestId-" + c_TEST_ID, "ArgumentOutOfRangeException is not thrown as expected." + GetDataString(strSrc, startIndex,count,oldChar,newChar));
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(strSrc, startIndex,count,oldChar,newChar));
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;

        const string c_TEST_DESC = "NegTest1: Replace count  is  larger than source StringBuilder's length";
        const string c_TEST_ID = "N002";

        string strSrc = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        char oldChar = TestLibrary.Generator.GetChar(-55);
        char newChar = TestLibrary.Generator.GetChar(-55);
        int startIndex = TestLibrary.Generator.GetInt32(-55) % strSrc.Length;
        int count = strSrc.Length - startIndex + TestLibrary.Generator.GetInt32(-55);

        System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(strSrc);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            stringBuilder.Replace(oldChar, newChar, startIndex, count);
            TestLibrary.TestFramework.LogError("009" + " TestId-" + c_TEST_ID, "ArgumentOutOfRangeException is not thrown as expected." + GetDataString(strSrc, startIndex,count,oldChar,newChar));
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(strSrc, startIndex,count,oldChar,newChar));
            retVal = false;
        }

        return retVal;
    }

    #endregion

    #region Helper methords for testing
    
    private string GetDataString(string strSrc, int startIndex, int count,char oldChar,char newChar)
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

        str = string.Format("\n[Source StingBulider value]\n \"{0}\"", str1);
        str += string.Format("\n[Length of source StringBuilder]\n {0}", len1);
        str += string.Format("\n[Start index ]\n{0}", startIndex);
        str += string.Format("\n[Replace count]\n{0}", count);
        str += string.Format("\n[Old char]\n{0}", oldChar);
        str += string.Format("\n[New char]\n{0}", newChar);

        return str;
    }
    #endregion

}

