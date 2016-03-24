// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// Convert.ToBoolean(Int32)
/// Converts the value of the specified 32-bit signed integer to an equivalent Boolean value. 
/// </summary>
public class ConvertToBoolean
{
    public static int Main()
    {
        ConvertToBoolean testObj = new ConvertToBoolean();

        TestLibrary.TestFramework.BeginTestCase("for method: Convert.ToBoolean(Int32)");
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

        Int32 b;
        bool expectedValue;
        bool actualValue;

        b = TestLibrary.Generator.GetInt32(-55);

        TestLibrary.TestFramework.BeginScenario("PosTest1: Random Int32 value between 0 and Int32.MaxValue.");
        try
        {
            actualValue = Convert.ToBoolean(b);
            expectedValue = 0 != b;

            if (actualValue != expectedValue)
            {
                errorDesc = "The boolean value of Int32 value " + b + " is not the value " + expectedValue + 
                                 " as expected: actual(" + actualValue + ")";
                TestLibrary.TestFramework.LogError("001", errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpect exception:" + e;
            errorDesc += "\nThe Int32 value is " + b;
            TestLibrary.TestFramework.LogError("002", errorDesc);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        string errorDesc;

        Int32 b;
        bool expectedValue;
        bool actualValue;

        b = Int32.MaxValue;

        TestLibrary.TestFramework.BeginScenario("PosTest2: value is Int32.MaxValue.");
        try
        {
            actualValue = Convert.ToBoolean(b);
            expectedValue = 0 != b;

            if (actualValue != expectedValue)
            {
                errorDesc = "The boolean value of Int32 value " + b + " is not the value " + expectedValue +
                                 " as expected: actual(" + actualValue + ")";
                TestLibrary.TestFramework.LogError("003", errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpect exception:" + e;
            errorDesc += "\nThe byte value is " + b;
            TestLibrary.TestFramework.LogError("004", errorDesc);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        string errorDesc;

        Int32 b;
        bool expectedValue;
        bool actualValue;

        b = Int32.MinValue;

        TestLibrary.TestFramework.BeginScenario("PosTest3: value is Int32.MinValue.");
        try
        {
            actualValue = Convert.ToBoolean(b);
            expectedValue = 0 != b;

            if (actualValue != expectedValue)
            {
                errorDesc = "The boolean value of Int32 integer " + b + " is not the value " + expectedValue +
                                 " as expected: actual(" + actualValue + ")";
                TestLibrary.TestFramework.LogError("005", errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpect exception:" + e;
            errorDesc += "\nThe Int32 value is " + b;
            TestLibrary.TestFramework.LogError("006", errorDesc);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;
        string errorDesc;

        Int32 b;
        bool expectedValue;
        bool actualValue;

        b = (Int32)(-1 * TestLibrary.Generator.GetInt32(-55) - 1);

        TestLibrary.TestFramework.BeginScenario("PosTest4: Random Int32 value between Int32.MinValue and -1.");
        try
        {
            actualValue = Convert.ToBoolean(b);
            expectedValue = 0 != b;

            if (actualValue != expectedValue)
            {
                errorDesc = "The boolean value of Int32 value " + b + " is not the value " + expectedValue +
                                 " as expected: actual(" + actualValue + ")";
                TestLibrary.TestFramework.LogError("007", errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpect exception:" + e;
            errorDesc += "\nThe Int32 value is " + b;
            TestLibrary.TestFramework.LogError("008", errorDesc);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;
        string errorDesc;

        Int32 b;
        bool expectedValue;
        bool actualValue;

        b = 0;

        TestLibrary.TestFramework.BeginScenario("PosTest5: Int32 value is zero.");
        try
        {
            actualValue = Convert.ToBoolean(b);
            expectedValue = 0 != b;

            if (actualValue != expectedValue)
            {
                errorDesc = "The boolean value of Int32 value " + b + " is not the value " + expectedValue +
                                 " as expected: actual(" + actualValue + ")";
                TestLibrary.TestFramework.LogError("009", errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpect exception:" + e;
            errorDesc += "\nThe Int32 value is " + b;
            TestLibrary.TestFramework.LogError("010", errorDesc);
            retVal = false;
        }
        return retVal;
    }
    #endregion
}
