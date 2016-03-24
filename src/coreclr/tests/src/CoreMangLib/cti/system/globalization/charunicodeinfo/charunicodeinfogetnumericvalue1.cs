// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

///<summary>
///System.Globalization.CharUnicodeInfo.GetNumericValue(Char)
///</summary>

public class CharUnicodeInfoGetNumericValue
{

    public static int Main()
    {
        CharUnicodeInfoGetNumericValue testObj = new CharUnicodeInfoGetNumericValue();
        TestLibrary.TestFramework.BeginTestCase("for method of System.Globalization.CharUnicodeInfo.GetNumericValue");
        if (testObj.RunTests())
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
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;
        retVal = PosTest5() && retVal;
        retVal = PosTest6() && retVal;
        return retVal;
    }


    #region Test Logic
    public bool PosTest1()
    {
        bool retVal = true;

        Char ch = '\0';

        Double expectedValue = -1;

        Double actualValue;

        TestLibrary.TestFramework.BeginScenario(@"PosTest1:Test the method with '\0'");
        try
        {
            actualValue = CharUnicodeInfo.GetNumericValue(ch);
            if (expectedValue != actualValue)
            {
                TestLibrary.TestFramework.LogError("001", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        Char ch = '0';

        Double expectedValue = 0;

        Double actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest1:Test the method with char '0'");
        try
        {
            actualValue = CharUnicodeInfo.GetNumericValue(ch);
            if (expectedValue != actualValue)
            {
                TestLibrary.TestFramework.LogError("003", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        Char ch = '9';

        Double expectedValue = 9;

        Double actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest1:Test the method with char '9'");
        try
        {
            actualValue = CharUnicodeInfo.GetNumericValue(ch);
            if (expectedValue != actualValue)
            {
                TestLibrary.TestFramework.LogError("005", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }


    public bool PosTest4()
    {
        bool retVal = true;

        Char ch = '\u3289';

        Double expectedValue = 10;

        Double actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest1:Test the method with char '\\u3289'");
        try
        {
            actualValue = CharUnicodeInfo.GetNumericValue(ch);
            if (expectedValue != actualValue)
            {
                TestLibrary.TestFramework.LogError("007", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }


    public bool PosTest5()
    {
        bool retVal = true;

        Char ch = 'a';

        Double expectedValue = -1;

        Double actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest1:Test the method with char 'a'");
        try
        {
            actualValue = CharUnicodeInfo.GetNumericValue(ch);
            if (expectedValue != actualValue)
            {
                TestLibrary.TestFramework.LogError("009", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }


    public bool PosTest6()
    {
        bool retVal = true;

        Char ch = 'Z';

        Double expectedValue = -1;

        Double actualValue;

        TestLibrary.TestFramework.BeginScenario("PosTest1:Test the method with char 'Z'");
        try
        {
            actualValue = CharUnicodeInfo.GetNumericValue(ch);
            if (expectedValue != actualValue)
            {
                TestLibrary.TestFramework.LogError("011", "ExpectedValue(" + expectedValue + ") !=ActualValue(" + actualValue + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }


    #endregion
}
