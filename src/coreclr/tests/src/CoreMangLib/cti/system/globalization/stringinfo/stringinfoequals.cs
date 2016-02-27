// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

/// <summary>
/// System.Globalization.StringInfo.Equals(Object)
/// </summary>
public class StringInfoEquals
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
        retVal = PosTest5() && retVal;
        retVal = PosTest6() && retVal;
        retVal = PosTest7() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Compare two equal StringInfo");

        try
        {
            string str = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            StringInfo stringInfo1 = new StringInfo(str);
            StringInfo stringInfo2 = new StringInfo(str);
            if (!stringInfo1.Equals(stringInfo2))
            {
                TestLibrary.TestFramework.LogError("001", "The result is not the value as expected");
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: The two stringinfos reference to one object ");

        try
        {
            string str = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            StringInfo stringInfo1 = new StringInfo(str);
            StringInfo stringInfo2 = stringInfo1;
            if (!stringInfo1.Equals(stringInfo2))
            {
                TestLibrary.TestFramework.LogError("003", "The result is not the value as expected");
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Using default constructor to create two equal instance");

        try
        {
            StringInfo stringInfo1 = new StringInfo();
            StringInfo stringInfo2 = new StringInfo();
            if (!stringInfo1.Equals(stringInfo2))
            {
                TestLibrary.TestFramework.LogError("005", "The result is not the value as expected");
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

        TestLibrary.TestFramework.BeginScenario("PosTest4: Compare two instance with different string value");

        try
        {
            StringInfo stringInfo1 = new StringInfo("stringinfo1");
            StringInfo stringInfo2 = new StringInfo("stringinfo2");
            if (stringInfo1.Equals(stringInfo2))
            {
                TestLibrary.TestFramework.LogError("007", "The result is not the value as expected");
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

    public bool PosTest5()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest5: Compare with a different kind of type");

        try
        {
            StringInfo stringInfo1 = new StringInfo("stringinfo1");
            string str = "stringinfo1";
            if (stringInfo1.Equals(str))
            {
                TestLibrary.TestFramework.LogError("009", "The result is not the value as expected");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest6()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest6: The argument is a null reference");

        try
        {
            StringInfo stringInfo1 = new StringInfo("stringinfo1");
            object ob = null;
            if (stringInfo1.Equals(ob))
            {
                TestLibrary.TestFramework.LogError("011", "The result is not the value as expected");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest7()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest7: The argument is value type");

        try
        {
            StringInfo stringInfo1 = new StringInfo("123");
            int i = 123;
            if (stringInfo1.Equals(i))
            {
                TestLibrary.TestFramework.LogError("013", "The result is not the value as expected");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("014", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases
    #endregion
    #endregion

    public static int Main()
    {
        StringInfoEquals test = new StringInfoEquals();

        TestLibrary.TestFramework.BeginTestCase("StringInfoEquals");

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
