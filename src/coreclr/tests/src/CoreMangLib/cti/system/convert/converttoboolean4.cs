// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// Convert.ToBoolean(Decimal)
/// Converts the value of the specified decimal value to an equivalent Boolean value. 
/// </summary>
public class ConvertToBoolean
{
    public static int Main()
    {
        ConvertToBoolean testObj = new ConvertToBoolean();

        TestLibrary.TestFramework.BeginTestCase("for method: Convert.ToBoolean(Decimal)");
        if(testObj.RunTests())
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

        return retVal;
    }

    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;
        string errorDesc;

        decimal d;
        bool expectedValue;
        bool actualValue;

        d = (decimal)TestLibrary.Generator.GetSingle(-55);

        TestLibrary.TestFramework.BeginScenario("PosTest1: Random decimal value between 0.0 and 1.0.");
        try
        {
            actualValue = Convert.ToBoolean(d);
            expectedValue = 0 != d;

            if (actualValue != expectedValue)
            {
                errorDesc = "The boolean value of decimal value " + d + " is not the value " + expectedValue + 
                                 " as expected: actual(" + actualValue + ")";
                TestLibrary.TestFramework.LogError("001", errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpect exception:" + e;
            errorDesc += "\nThe decimal value is " + d;
            TestLibrary.TestFramework.LogError("002", errorDesc);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        string errorDesc;

        decimal d;
        bool expectedValue;
        bool actualValue;

        d = decimal.MaxValue;

        TestLibrary.TestFramework.BeginScenario("PosTest2: value is decimal.MaxValue.");
        try
        {
            actualValue = Convert.ToBoolean(d);
            expectedValue = 0 != d;

            if (actualValue != expectedValue)
            {
                errorDesc = "The boolean value of decimal value " + d + " is not the value " + expectedValue +
                                 " as expected: actual(" + actualValue + ")";
                TestLibrary.TestFramework.LogError("003", errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpect exception:" + e;
            errorDesc += "\nThe decimal value is " + d;
            TestLibrary.TestFramework.LogError("004", errorDesc);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        string errorDesc;

        decimal d;
        bool expectedValue;
        bool actualValue;

        d = decimal.MinValue;

        TestLibrary.TestFramework.BeginScenario("PosTest3: value is decimal.MinValue.");
        try
        {
            actualValue = Convert.ToBoolean(d);
            expectedValue = 0 != d;

            if (actualValue != expectedValue)
            {
                errorDesc = "The boolean value of decimal integer " + d + " is not the value " + expectedValue +
                                 " as expected: actual(" + actualValue + ")";
                TestLibrary.TestFramework.LogError("005", errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpect exception:" + e;
            errorDesc += "\nThe decimal value is " + d;
            TestLibrary.TestFramework.LogError("006", errorDesc);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;
        string errorDesc;

        decimal d;
        bool expectedValue;
        bool actualValue;

        d = decimal.Zero;

        TestLibrary.TestFramework.BeginScenario("PosTest4: value is decimal.Zero.");
        try
        {
            actualValue = Convert.ToBoolean(d);
            expectedValue = 0 != d;

            if (actualValue != expectedValue)
            {
                errorDesc = "The boolean value of decimal integer " + d + " is not the value " + expectedValue +
                                 " as expected: actual(" + actualValue + ")";
                TestLibrary.TestFramework.LogError("007", errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpect exception:" + e;
            errorDesc += "\nThe decimal value is " + d;
            TestLibrary.TestFramework.LogError("008", errorDesc);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;
        string errorDesc;

        decimal d;
        bool expectedValue;
        bool actualValue;

        d = -1 * (decimal)TestLibrary.Generator.GetSingle(-55);

        TestLibrary.TestFramework.BeginScenario("PosTest5: Random decimal value between -0.0 and -1.0.");
        try
        {
            actualValue = Convert.ToBoolean(d);
            expectedValue = 0 != d;

            if (actualValue != expectedValue)
            {
                errorDesc = "The boolean value of decimal integer " + d + " is not the value " + expectedValue +
                                 " as expected: actual(" + actualValue + ")";
                TestLibrary.TestFramework.LogError("009", errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpect exception:" + e;
            errorDesc += "\nThe decimal value is " + d;
            TestLibrary.TestFramework.LogError("010", errorDesc);
            retVal = false;
        }
        return retVal;
    }
    #endregion
}
