using System;

/// <summary>
/// String.Split(params Char[])  
/// Returns a String array containing the substrings in this 
/// instance that are delimited by elements of a specified Char array.
/// </summary>
public class StringSplit1
{
    private const int c_MIN_STRING_LEN = 8;
    private const int c_MAX_STRING_LEN = 256;

    private const int c_MAX_SHORT_STR_LEN = 31;//short string (<32 chars)
    private const int c_MIN_LONG_STR_LEN = 257;//long string ( >256 chars)
    private const int c_MAX_LONG_STR_LEN = 65535;

    public static int Main()
    {
        StringSplit1 sr = new StringSplit1();

        TestLibrary.TestFramework.BeginTestCase("for method: System.String.Split(params Char[])");
        if (sr.RunTests())
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

        //while (sr.RunTests() != false)
        //{ }
        //return 0;
    }

    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;

        //TestLibrary.TestFramework.LogInformation("[Negative]");

        return retVal;
    }

    #region Positive test scenarios

    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest1: Random old char random new char.";
        const string c_TEST_ID = "P001";

        string strSrc, delimiterStr;
        char[] separator;
        bool condition = false;
        bool expectedValue = true;
        bool actualValue = false;

        strSrc = TestLibrary.Generator.GetString(-55, false, 10, 10);
        //strSrc = "?œ¿»°?????a§T";
        delimiterStr = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        //delimiterStr = new string('a', 5);
        separator = new char[delimiterStr.Length];
        for (int i = 0; i < delimiterStr.Length; i++)
        {
            separator[i] = delimiterStr[i];
        }

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            string[] strs = strSrc.Split(separator);

            string subStrSrc;
            int indexSrc;
            int indexStrsArray = 0;

            condition = true; //
            // compare string array after splitting and source string
            for (indexSrc = 0; indexSrc < strSrc.Length; )
            {
                if (delimiterStr.Contains(strSrc[indexSrc].ToString())) //Begin if
                {
                    bool adjacent = 
                        ((0 < indexSrc) 
                        && (indexSrc < strSrc.Length - 1) 
                        && (delimiterStr.Contains(strSrc[indexSrc + 1].ToString())));

                    if ((0 == indexSrc) || ((strSrc.Length - 1) == indexSrc))
                    {
                        condition = (0 == string.CompareOrdinal(strs[indexStrsArray], string.Empty)) && condition;
                        indexSrc++;
                        indexStrsArray++;
                        continue;
                    }
                    if (adjacent)
                    {
                        condition = (0 == string.CompareOrdinal(strs[indexStrsArray], string.Empty)) && condition;
                        indexSrc += 2;
                        indexStrsArray++;
                        continue;
                    }
                    indexSrc++;
                }
                else
                {
                    subStrSrc = strSrc.Substring(indexSrc, strs[indexStrsArray].Length); //error
                    condition = (0 == string.CompareOrdinal(subStrSrc, strs[indexStrsArray])) && condition;
                    indexSrc += strs[indexStrsArray].Length;
                    indexStrsArray++;
                } //End if
            }

            actualValue = condition;
            if (actualValue != expectedValue)
            {
                string errorDesc = "Value is not " + expectedValue + " as expected: Actual(" + actualValue + ")";
                errorDesc += GetDataString(strSrc, separator);
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(strSrc, separator));
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest2: Adjacent delimiters exist in the source string";
        const string c_TEST_ID = "P002";

        string strSrc, delimiterStr;
        int j;
        char[] separator;
        bool condition = false;
        bool expectedValue = true;
        bool actualValue = false;

        strSrc = TestLibrary.Generator.GetString(-55, false, 10, 10);
        j = GetInt32(1, strSrc.Length - 2);
        delimiterStr = strSrc.Substring(j, 2);
        //delimiterStr = new string('a', 5);
        separator = new char[delimiterStr.Length];
        for (int i = 0; i < delimiterStr.Length; i++)
        {
            separator[i] = delimiterStr[i];
        }

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            string[] strs = strSrc.Split(separator);

            string subStrSrc;
            int indexSrc;
            int indexStrsArray = 0;

            condition = true; //
            // compare string array after splitting and source string
            for (indexSrc = 0; indexSrc < strSrc.Length; )
            {
                if (delimiterStr.Contains(strSrc[indexSrc].ToString())) //Begin if
                {
                    bool adjacent =
                        ((0 < indexSrc)
                        && (indexSrc < strSrc.Length - 1)
                        && (delimiterStr.Contains(strSrc[indexSrc + 1].ToString())));

                    if ((0 == indexSrc) || ((strSrc.Length - 1) == indexSrc))
                    {
                        condition = (0 == string.CompareOrdinal(strs[indexStrsArray], string.Empty)) && condition;
                        indexSrc++;
                        indexStrsArray++;
                        continue;
                    }
                    if (adjacent)
                    {
                        condition = (0 == string.CompareOrdinal(strs[indexStrsArray], string.Empty)) && condition;
                        indexSrc += 2;
                        indexStrsArray++;
                        continue;
                    }
                    indexSrc++;
                }
                else
                {
                    subStrSrc = strSrc.Substring(indexSrc, strs[indexStrsArray].Length); //error
                    condition = (0 == string.CompareOrdinal(subStrSrc, strs[indexStrsArray])) && condition;
                    indexSrc += strs[indexStrsArray].Length;
                    indexStrsArray++;
                } //End if
            }

            actualValue = condition;
            if (actualValue != expectedValue)
            {
                string errorDesc = "Value is not " + expectedValue + " as expected: Actual(" + actualValue + ")";
                errorDesc += GetDataString(strSrc, separator);
                TestLibrary.TestFramework.LogError("003" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(strSrc, separator));
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest3: Separator char array is a null reference";
        const string c_TEST_ID = "P003";

        string strSrc, delimiterStr;
        char[] separator;
        bool condition = false;
        bool expectedValue = true;
        bool actualValue = false;

        strSrc = "\tWEre\t\rasdfsdf\n\nweer\r23123\t456\v\x0020678op~";
        delimiterStr = null;
        separator = null;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            string[] strs = strSrc.Split(separator);

            delimiterStr = "\n\r\t\v\x0020";
            separator = new char[delimiterStr.Length];
            for (int i = 0; i < delimiterStr.Length; i++)
            {
                separator[i] = delimiterStr[i];
            }

            string subStrSrc;
            int indexSrc;
            int indexStrsArray = 0;

            condition = true; //
            // compare string array after splitting and source string
            for (indexSrc = 0; indexSrc < strSrc.Length; )
            {
                if (delimiterStr.Contains(strSrc[indexSrc].ToString())) //Begin if
                {
                    bool adjacent =
                        ((0 < indexSrc)
                        && (indexSrc < strSrc.Length - 1)
                        && (delimiterStr.Contains(strSrc[indexSrc + 1].ToString())));

                    if ((0 == indexSrc) || ((strSrc.Length - 1) == indexSrc))
                    {
                        condition = (0 == string.CompareOrdinal(strs[indexStrsArray], string.Empty)) && condition;
                        indexSrc++;
                        indexStrsArray++;
                        continue;
                    }
                    if (adjacent)
                    {
                        condition = (0 == string.CompareOrdinal(strs[indexStrsArray], string.Empty)) && condition;
                        indexSrc += 2;
                        indexStrsArray++;
                        continue;
                    }
                    indexSrc++;
                }
                else
                {
                    subStrSrc = strSrc.Substring(indexSrc, strs[indexStrsArray].Length); //error
                    condition = (0 == string.CompareOrdinal(subStrSrc, strs[indexStrsArray])) && condition;
                    indexSrc += strs[indexStrsArray].Length;
                    indexStrsArray++;
                } //End if
            }

            actualValue = condition;
            if (actualValue != expectedValue)
            {
                string errorDesc = "Value is not " + expectedValue + " as expected: Actual(" + actualValue + ")";
                errorDesc += GetDataString(strSrc, separator);
                TestLibrary.TestFramework.LogError("005" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(strSrc, separator));
            retVal = false;
        }

        return retVal;
    }

    #endregion

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

    #endregion

    private string GetDataString(string strSrc, char[] separator)
    {
        string str1, str, str2;
        int len1, len2;

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

        if (null == separator)
        {
            str2 = "white-space";
            len2 = 0;
        }
        else
        {
            str2 = string.Empty;
            for (int i = 0; i < separator.Length; i++)
            {
                str2 += separator[i].ToString();
            }
            len2 = separator.Length;
        }

        str = string.Format("\n[Source string value]\n \"{0}\"", str1);
        str += string.Format("\n[Length of source string]\n {0}", len1);
        str += string.Format("\n[Sepator char array]\n{0}", str2);
        str += string.Format("\n[Sepator char array's length]\n{0}", len2);

        return str;
    }
}