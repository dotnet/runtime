// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

/// <summary>
/// System.Globalization.StringInfo.string
/// </summary>
public class StringInfoString
{
    private const int c_MINI_STRING_LENGTH = 8;
    private const int c_MAX_STRING_LENGTH = 256;

    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: The string property in stringinfo object is a random string argument");

        try
        {
            string str = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            StringInfo stringInfo = new StringInfo(str);
            if (stringInfo.String != str)
            {
                TestLibrary.TestFramework.LogError("001", "The constructor does not work correctly,the str is: " + str);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: The string property in stringinfo object is an empty string argument");

        try
        {
            string str = string.Empty;
            StringInfo stringInfo = new StringInfo(str);
            if (stringInfo.String != string.Empty)
            {
                TestLibrary.TestFramework.LogError("003", "The constructor does not work correctly,the str is: " + str);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3:  Check an instance with a string of white space");

        try
        {
            string str = " ";
            StringInfo stringInfo = new StringInfo(str);
            if (stringInfo.String != " ")
            {
                TestLibrary.TestFramework.LogError("005", "The constructor does not work correctly,the str is: " + str);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest4:  Set the property with a random string value");

        try
        {
            string str = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            StringInfo stringInfo = new StringInfo();
            stringInfo.String = str;
            if (stringInfo.String != str)
            {
                TestLibrary.TestFramework.LogError("007", "The constructor does not work correctly,the str is: " + str);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: The property was set with a value of null");

        try
        {
            string str = null;
            StringInfo stringInfo = new StringInfo();
            stringInfo.String = str;
            TestLibrary.TestFramework.LogError("101", "The ArgumentNullException was not thrown as expected");
            retVal = false;
        }
        catch (ArgumentNullException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("102", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        StringInfoString test = new StringInfoString();

        TestLibrary.TestFramework.BeginTestCase("StringInfoString");

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
}
