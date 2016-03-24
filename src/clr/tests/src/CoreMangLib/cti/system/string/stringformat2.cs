// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;

/// <summary>
/// String.Format(String, params Object[])  
/// Replaces the format item in a specified String with 
/// the text equivalent of the value of a corresponding Object instance in a specified array.
/// </summary>
class StringFormat2
{
    private const int c_MIN_STRING_LEN = 8;
    private const int c_MAX_STRING_LEN = 256;

    public static int Main()
    {
        StringFormat2 sf = new StringFormat2();

        TestLibrary.TestFramework.BeginTestCase("for method: String.Format(String, params Object[])");
        if (sf.RunTests())
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
        retVal = PosTest4() && retVal;
        retVal = PosTest5() && retVal;
        retVal = PosTest6() && retVal;
        retVal = PosTest7() && retVal;
        retVal = PosTest8() && retVal;

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

        const string c_TEST_ID = "P001";
        const string c_TEST_DESC = "PosTest1: Default format string for Int32[]";

        bool expectedValue = true;
        bool actualValue = false;
        string errorDesc;

        string format;
        Object[] args;

        int[] iArray = new int[TestLibrary.Generator.GetInt32(-55) % c_MIN_STRING_LEN + 1];
        args = new Object[iArray.Length];
        //boxing
        for (int j = 0; j < iArray.Length; j++)
        {
            iArray[j] = TestLibrary.Generator.GetInt32(-55);
            args[j] = iArray[j];
        }
        int index = TestLibrary.Generator.GetInt32(-55) % iArray.Length;
        format = "The Int32 {" + index.ToString() + "}";

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            string str = string.Format(format, args);
            str = str.Substring(format.IndexOf('{'));
            actualValue = (int.Parse(str) == iArray[index]);
            if (actualValue != expectedValue)
            {
                errorDesc = "Value is not " + expectedValue + " as expected: Actual(" + actualValue + ")";
                //errorDesc += GetDataString(strA, indexA, strB, indexB, length, comparisonType);
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch(Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("002" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    #region alignment testing

    public bool PosTest2()
    {
        bool retVal = true;

        const string c_TEST_ID = "P002";
        const string c_TEST_DESC = "PosTest2: the length of the formatted Int32 value is less than alignment";

        bool expectedValue = true;
        bool actualValue = false;
        string errorDesc;

        string format;
        Object[] args;

        int[] iArray = new int[TestLibrary.Generator.GetInt32(-55) % c_MIN_STRING_LEN + 1];
        args = new Object[iArray.Length];
        //boxing
        for (int j = 0; j < iArray.Length; j++)
        {
            iArray[j] = TestLibrary.Generator.GetInt32(-55);
            args[j] = iArray[j];
        }
        int index = TestLibrary.Generator.GetInt32(-55) % iArray.Length;
        int alignment = iArray[index].ToString().Length + TestLibrary.Generator.GetInt32(-55) % c_MAX_STRING_LEN + 1;
        format = "The Int32 {" + index.ToString() + "," + alignment.ToString() + "}";

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            string str = string.Format(format, args);
            str = str.Substring(format.IndexOf('{'));
            //Validate the formatted string against primitive integer
            actualValue = (int.Parse(str) == iArray[index]);
            int lastIndex = str.LastIndexOf('\u0020');
            actualValue = (lastIndex >= 0) && actualValue;
            for (int j = 0; j <= lastIndex; j++)
            {
                actualValue = ('\u0020' == str[j]) && actualValue;
            }
            actualValue = (alignment == str.Length) && actualValue;

            if (actualValue != expectedValue)
            {
                errorDesc = "Value is not " + expectedValue + " as expected: Actual(" + actualValue + ")";
                //errorDesc += GetDataString(strA, indexA, strB, indexB, length, comparisonType);
                TestLibrary.TestFramework.LogError("003" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("004" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        const string c_TEST_ID = "P003";
        const string c_TEST_DESC = "PosTest3: alignment is positive, the length of the formatted Int32 value is greater than alignment";

        bool expectedValue = true;
        bool actualValue = false;
        string errorDesc;

        string format;
        Object[] args;

        int[] iArray = new int[TestLibrary.Generator.GetInt32(-55) % c_MIN_STRING_LEN + 1];
        args = new Object[iArray.Length];
        //boxing
        for (int j = 0; j < iArray.Length; j++)
        {
            iArray[j] = TestLibrary.Generator.GetInt32(-55) % (Int32.MaxValue - 10) + 10;
            args[j] = iArray[j];
        }
        int index = TestLibrary.Generator.GetInt32(-55) % iArray.Length;
        int alignment =TestLibrary.Generator.GetInt32(-55) % (iArray[index].ToString().Length - 1) + 1;
        format = "The Int32 {" + index.ToString() + "," + alignment.ToString() + "}";

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            string str = string.Format(format, args);
            str = str.Substring(format.IndexOf('{'));
            actualValue = (int.Parse(str) == iArray[index]) && (str.Length == iArray[index].ToString().Length);
            if (actualValue != expectedValue)
            {
                errorDesc = "Value is not " + expectedValue + " as expected: Actual(" + actualValue + ")";
                //errorDesc += GetDataString(strA, indexA, strB, indexB, length, comparisonType);
                TestLibrary.TestFramework.LogError("005" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("006" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        const string c_TEST_ID = "P004";
        const string c_TEST_DESC = "PosTest4: alignment is zero";

        bool expectedValue = true;
        bool actualValue = false;
        string errorDesc;

        string format;
        Object[] args;

        int[] iArray = new int[TestLibrary.Generator.GetInt32(-55) % c_MIN_STRING_LEN + 1];
        args = new Object[iArray.Length];
        //boxing
        for (int j = 0; j < iArray.Length; j++)
        {
            iArray[j] = TestLibrary.Generator.GetInt32(-55) % (Int32.MaxValue - 10) + 10;
            args[j] = iArray[j];
        }
        int index = TestLibrary.Generator.GetInt32(-55) % iArray.Length;
        int alignment = 0;
        format = "The Int32 {" + index.ToString() + "," + alignment.ToString() + "}";

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            string str = string.Format(format, args);
            str = str.Substring(format.IndexOf('{'));
            actualValue = (int.Parse(str) == iArray[index]) && (str.Length == iArray[index].ToString().Length);
            if (actualValue != expectedValue)
            {
                errorDesc = "Value is not " + expectedValue + " as expected: Actual(" + actualValue + ")";
                //errorDesc += GetDataString(strA, indexA, strB, indexB, length, comparisonType);
                TestLibrary.TestFramework.LogError("007" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("008" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;

        const string c_TEST_ID = "P005";
        const string c_TEST_DESC = "PosTest5: alignment is negative, and its absolute value is greater than length of formatted object";

        bool expectedValue = true;
        bool actualValue = false;
        string errorDesc;

        string format;
        Object[] args;

        int[] iArray = new int[TestLibrary.Generator.GetInt32(-55) % c_MIN_STRING_LEN + 1];
        args = new Object[iArray.Length];
        //boxing
        for (int j = 0; j < iArray.Length; j++)
        {
            iArray[j] = TestLibrary.Generator.GetInt32(-55) % (Int32.MaxValue - 10) + 10;
            args[j] = iArray[j];
        }
        int index = TestLibrary.Generator.GetInt32(-55) % iArray.Length;
        int alignment = -1 * (iArray[index].ToString().Length + TestLibrary.Generator.GetInt32(-55) % c_MAX_STRING_LEN + 1);
        format = "The Int32 {" + index.ToString() + "," + alignment.ToString() + "}";

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            string str = string.Format(format, args);
            str = str.Substring(format.IndexOf('{'));

            //
            int firstIndex = str.IndexOf('\u0020');
            actualValue = (int.Parse(str) == iArray[index]);
            for (int j = firstIndex; j < str.Length; j++ )
            {
                actualValue = ('\u0020' == str[j]) && actualValue;
            }
            actualValue = (str.Length == (-1 * alignment)) && actualValue;
            if (actualValue != expectedValue)
            {
                errorDesc = "Value is not " + expectedValue + " as expected: Actual(" + actualValue + ")";
                //errorDesc += GetDataString(strA, indexA, strB, indexB, length, comparisonType);
                TestLibrary.TestFramework.LogError("009" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("010" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest6()
    {
        bool retVal = true;

        const string c_TEST_ID = "P006";
        const string c_TEST_DESC = "PosTest6: alignment is negative, and its absolute value is less than or equal length of formatted object";

        bool expectedValue = true;
        bool actualValue = false;
        string errorDesc;

        string format;
        Object[] args;

        int[] iArray = new int[TestLibrary.Generator.GetInt32(-55) % c_MIN_STRING_LEN + 1];
        args = new Object[iArray.Length];
        //boxing
        for (int j = 0; j < iArray.Length; j++)
        {
            iArray[j] = TestLibrary.Generator.GetInt32(-55) % (Int32.MaxValue - 10) + 10;
            args[j] = iArray[j];
        }
        int index = TestLibrary.Generator.GetInt32(-55) % iArray.Length;
        int alignment = -1 * (TestLibrary.Generator.GetInt32(-55) % (iArray[index].ToString().Length + 1));
        format = "The Int32 {" + index.ToString() + "," + alignment.ToString() + "}";

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            string str = string.Format(format, args);
            str = str.Substring(format.IndexOf('{'));
            actualValue = (int.Parse(str) == iArray[index]);
            actualValue = (str.Length == iArray[index].ToString().Length) && actualValue;
            if (actualValue != expectedValue)
            {
                errorDesc = "Value is not " + expectedValue + " as expected: Actual(" + actualValue + ")";
                //errorDesc += GetDataString(strA, indexA, strB, indexB, length, comparisonType);
                TestLibrary.TestFramework.LogError("011" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("012" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    #endregion

    public bool PosTest7()
    {
        bool retVal = true;

        const string c_TEST_ID = "P007";
        const string c_TEST_DESC = "PosTest7: Formatted object is null";

        bool expectedValue = true;
        bool actualValue = false;
        string errorDesc;

        string format;
        Object[] args;

        format = "The object is {0}";
        args = new object[TestLibrary.Generator.GetInt32(-55) % c_MAX_STRING_LEN + 1];
        for (int i = 0; i < args.Length; i++)
        {
            args[i] = null;
        }

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            string str = string.Format(format, args);
            str = str.Substring(format.IndexOf('{'));
            actualValue = (0 == string.CompareOrdinal(str, string.Empty));
            if (actualValue != expectedValue)
            {
                errorDesc = "Value is not " + expectedValue + " as expected: Actual(" + actualValue + ")";
                //errorDesc += GetDataString(strA, indexA, strB, indexB, length, comparisonType);
                TestLibrary.TestFramework.LogError("013" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("014" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest8()
    {
        bool retVal = true;

        const string c_TEST_ID = "P008";
        const string c_TEST_DESC = @"PosTest8: format string contains { literal";

        bool expectedValue = true;
        bool actualValue = false;
        string errorDesc;

        string format;
        Object[] args;

        int[] iArray = new int[TestLibrary.Generator.GetInt32(-55) % c_MIN_STRING_LEN + 1];
        args = new object[iArray.Length];
        format = "{{0}}";
        //boxing
        for (int j = 0; j < iArray.Length; j++)
        {
            args[j] = iArray[j];
        }

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            string str = string.Format(format, args);
            
            actualValue = (0 == string.CompareOrdinal(str, "{0}"));
            if (actualValue != expectedValue)
            {
                errorDesc = "Value is not " + expectedValue + " as expected: Actual(" + actualValue + ")";
                //errorDesc += GetDataString(strA, indexA, strB, indexB, length, comparisonType);
                TestLibrary.TestFramework.LogError("015" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("016" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    #endregion

    #region Negative test scenarios

    //ArgumentNullException
    public bool NegTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "N001";
        const string c_TEST_DESC = "NegTest1: format is a null reference ";
        string errorDesc;

        string format;
        Object[] args;

        format = null;
        args = new object[TestLibrary.Generator.GetInt32(-55) % c_MIN_STRING_LEN]; //boxing

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            string.Format(format, args);
            errorDesc = "ArgumentNullException is not thrown as expected";
            //errorDesc += GetDataString(strA, indexA, strB, indexB, length, comparisonType);
            TestLibrary.TestFramework.LogError("017" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }
        catch (ArgumentNullException)
        { }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("018" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    //FormatException
    public bool NegTest2()
    {
        bool retVal = true;

        const string c_TEST_ID = "N002";
        const string c_TEST_DESC = "NegTest2: The format item in format is invalid ";
        string errorDesc;

        string format;
        Object[] args;

        char ch = TestLibrary.Generator.GetChar(-55);
        format = "The object {" + ch.ToString() + "}";
        args = new object[TestLibrary.Generator.GetInt32(-55) % c_MIN_STRING_LEN]; //boxing

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            string.Format(format, args);
            errorDesc = "FormatException is not thrown as expected";
            //errorDesc += GetDataString(strA, indexA, strB, indexB, length, comparisonType);
            TestLibrary.TestFramework.LogError("019" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }
        catch (FormatException)
        { }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("020" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest3()
    {
        bool retVal = true;

        const string c_TEST_ID = "N003";
        const string c_TEST_DESC = "NegTest3: The index is greater than number of formatted objects ";
        string errorDesc;

        string format;
        Object[] args;

        args = new object[TestLibrary.Generator.GetInt32(-55) % c_MIN_STRING_LEN]; //boxing
        int i = TestLibrary.Generator.GetInt32(-55) % c_MAX_STRING_LEN + 1 + args.Length;
        format = "The object {" + i.ToString() + "}";

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            string.Format(format, args);
            errorDesc = "FormatException is not thrown as expected";
            //errorDesc += GetDataString(strA, indexA, strB, indexB, length, comparisonType);
            TestLibrary.TestFramework.LogError("021" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }
        catch (FormatException)
        { }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("022" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest4()
    {
        bool retVal = true;

        const string c_TEST_ID = "N004";
        const string c_TEST_DESC = "NegTest4: The index is negative ";
        string errorDesc;

        string format;
        Object[] args;

        int i = -1 * TestLibrary.Generator.GetInt32(-55) - 1;
        format = "The object {" + i.ToString() + "}";
        args = new object[TestLibrary.Generator.GetInt32(-55) % c_MAX_STRING_LEN]; //boxing

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            string.Format(format, args);
            errorDesc = "FormatException is not thrown as expected";
            //errorDesc += GetDataString(strA, indexA, strB, indexB, length, comparisonType);
            TestLibrary.TestFramework.LogError("023" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }
        catch (FormatException)
        { }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("024" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    //ArgumentNullException
    public bool NegTest5()
    {
        bool retVal = true;

        const string c_TEST_ID = "N005";
        const string c_TEST_DESC = "NegTest5: The formatted object array is negative ";
        string errorDesc;

        string format;
        Object[] args;

        int i = -1 * TestLibrary.Generator.GetInt32(-55) - 1;
        format = "The object {" + i.ToString() + "}";
        args = null;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            string.Format(format, args);
            errorDesc = "ArgumentNullException is not thrown as expected";
            //errorDesc += GetDataString(strA, indexA, strB, indexB, length, comparisonType);
            TestLibrary.TestFramework.LogError("023" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }
        catch (ArgumentNullException)
        { }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            TestLibrary.TestFramework.LogError("024" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }
    
    #endregion

}
