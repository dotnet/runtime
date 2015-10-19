using System;

/// <summary>
/// String.Remove(Int32, Int32)  
/// Deletes a specified number of characters from this instance 
/// beginning at a specified position.  
/// </summary>
public class StringRemove2
{
    private const int c_MIN_STRING_LEN = 8;
    private const int c_MAX_STRING_LEN = 256;

    private const int c_MAX_SHORT_STR_LEN = 31;//short string (<32 chars)
    private const int c_MIN_LONG_STR_LEN = 257;//long string ( >256 chars)
    private const int c_MAX_LONG_STR_LEN = 65535;

    public static int Main()
    {
        StringRemove2 sr = new StringRemove2();

        TestLibrary.TestFramework.BeginTestCase("for method: System.String.Remove(Int32, Int32)");
        if(sr.RunTests())
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
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;
        retVal = NegTest5() && retVal;

        return retVal;
    }

    #region Positive test scenarios

    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest1: Valid start index between 0, source string's length minus 1";
        const string c_TEST_ID = "P001";

        string strSrc;
        int startIndex, count;
        bool expectedValue = true;
        bool actualValue = false;

        strSrc = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        startIndex = GetInt32(0, strSrc.Length - 1);
        count = GetInt32(1, strSrc.Length - startIndex);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            string strReserved = strSrc.Remove(startIndex, count);

            actualValue = (0 == string.CompareOrdinal(strReserved, strSrc.Substring(0, startIndex) + strSrc.Substring(startIndex + count)));
            if (actualValue != expectedValue)
            {
                string errorDesc = "Value is not " + expectedValue + " as expected: Actual(" + actualValue + ")";
                errorDesc += GetDataString(strSrc, startIndex, count);
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(strSrc, startIndex, count));
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest2: Start index is zero, count equals whole string length";
        const string c_TEST_ID = "P002";

        string strSrc;
        int startIndex, count;
        bool expectedValue = true;
        bool actualValue = false;

        strSrc = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        startIndex = 0;
        count = strSrc.Length;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            string strReserved = strSrc.Remove(startIndex, count);

            actualValue = (0 == string.CompareOrdinal(strReserved, String.Empty));
            if (actualValue != expectedValue)
            {
                string errorDesc = "Value is not " + expectedValue + " as expected: Actual(" + actualValue + ")";
                errorDesc += GetDataString(strSrc, startIndex, count);
                TestLibrary.TestFramework.LogError("003" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(strSrc, startIndex, count));
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        const string c_TEST_DESC = "PosTest3: Count is zero, valid start index";
        const string c_TEST_ID = "P003";

        string strSrc;
        int startIndex, count;
        bool expectedValue = true;
        bool actualValue = false;

        strSrc = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        startIndex = GetInt32(0, strSrc.Length - 1);
        count = 0;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            string strReserved = strSrc.Remove(startIndex, count);

            actualValue = (0 == string.CompareOrdinal(strReserved, strSrc));
            if (actualValue != expectedValue)
            {
                string errorDesc = "Value is not " + expectedValue + " as expected: Actual(" + actualValue + ")";
                errorDesc += GetDataString(strSrc, startIndex, count);
                TestLibrary.TestFramework.LogError("005" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(strSrc, startIndex, count));
            retVal = false;
        }

        return retVal;
    }

    #endregion

    #region Negative test Scenarios

    //ArgumentOutOfRangeException
    public bool NegTest1()
    {
        bool retVal = true;

        const string c_TEST_DESC = "NegTest1: Start index is too greater than source string's length";
        const string c_TEST_ID = "N001";

        string strSrc;
        int startIndex, count;

        strSrc = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        startIndex = GetInt32(strSrc.Length, Int32.MaxValue);
        count = GetInt32(0, strSrc.Length);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            strSrc.Remove(startIndex, count);
            TestLibrary.TestFramework.LogError("007" + " TestId-" + c_TEST_ID, "ArgumentOutOfRangeException is not thrown as expected." + GetDataString(strSrc, startIndex, count));
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch(Exception e)
        {
            TestLibrary.TestFramework.LogError("008" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(strSrc, startIndex, count));
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;

        const string c_TEST_DESC = "NegTest2: Valid start index plus count is greater than string length";
        const string c_TEST_ID = "N002";

        string strSrc;
        int startIndex, count;

        strSrc = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        startIndex = GetInt32(0, strSrc.Length - 1);
        count = GetInt32(strSrc.Length - startIndex + 1, Int32.MaxValue);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            strSrc.Remove(startIndex, count);
            TestLibrary.TestFramework.LogError("009" + " TestId-" + c_TEST_ID, "ArgumentOutOfRangeException is not thrown as expected." + GetDataString(strSrc, startIndex, count));
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(strSrc, startIndex, count));
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest3()
    {
        bool retVal = true;

        const string c_TEST_DESC = "NegTest3: Valid count plus start index is greater than string length";
        const string c_TEST_ID = "N003";

        string strSrc;
        int startIndex, count;

        strSrc = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        count = GetInt32(0, strSrc.Length);
        startIndex = GetInt32(strSrc.Length - count + 1, Int32.MaxValue);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            strSrc.Remove(startIndex, count);
            TestLibrary.TestFramework.LogError("011" + " TestId-" + c_TEST_ID, "ArgumentOutOfRangeException is not thrown as expected." + GetDataString(strSrc, startIndex, count));
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(strSrc, startIndex, count));
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest4()
    {
        bool retVal = true;

        const string c_TEST_DESC = "NegTest4: Start index is negative, valid number of characters to delete";
        const string c_TEST_ID = "N004";

        string strSrc;
        int startIndex, count;

        strSrc = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        startIndex = -1 * TestLibrary.Generator.GetInt32(-55) - 1;
        count = GetInt32(0, strSrc.Length);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            strSrc.Remove(startIndex, count);
            TestLibrary.TestFramework.LogError("013" + " TestId-" + c_TEST_ID, "ArgumentOutOfRangeException is not thrown as expected." + GetDataString(strSrc, startIndex, count));
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("014" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(strSrc, startIndex, count));
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest5()
    {
        bool retVal = true;

        const string c_TEST_DESC = "NegTest5: Valid start index, negative count of characters to delete";
        const string c_TEST_ID = "N005";

        string strSrc;
        int startIndex, count;

        strSrc = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
        startIndex = GetInt32(0, strSrc.Length - 1);
        count = -1 * TestLibrary.Generator.GetInt32(-55) - 1;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            strSrc.Remove(startIndex, count);
            TestLibrary.TestFramework.LogError("015" + " TestId-" + c_TEST_ID, "ArgumentOutOfRangeException is not thrown as expected." + GetDataString(strSrc, startIndex, count));
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("016" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e + GetDataString(strSrc, startIndex, count));
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

    private string GetDataString(string strSrc, int startIndex, int count)
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

        str = string.Format("\n[Source string value]\n \"{0}\"", str1);
        str += string.Format("\n[Length of source string]\n {0}", len1);
        str += string.Format("\n[Start index]\n{0}", startIndex);
        str += string.Format("\n[Start index]\n{0}", count);

        return str;
    }
}
