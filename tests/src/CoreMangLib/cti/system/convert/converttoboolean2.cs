// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// Convert.ToBoolean(Byte)
/// Converts the value of the specified 8-bit unsigned integer to an equivalent Boolean value. 
/// </summary>
public class ConvertToBoolean
{
    public static int Main()
    {
        ConvertToBoolean testObj = new ConvertToBoolean();

        TestLibrary.TestFramework.BeginTestCase("for method: Convert.ToBoolean(Byte)");
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

        return retVal;
    }

    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;
        string errorDesc;

        byte b;
        bool expectedValue;
        bool actualValue;

        b = TestLibrary.Generator.GetByte(-55);

        TestLibrary.TestFramework.BeginScenario("PosTest1: Random byte value between 0 and byte.MaxValue.");
        try
        {
            actualValue = Convert.ToBoolean(b);
            expectedValue = 0 != b;

            if (actualValue != expectedValue)
            {
                errorDesc = "The boolean value of byte value " + b + " is not the value " + expectedValue + 
                                 " as expected: actual(" + actualValue + ")";
                TestLibrary.TestFramework.LogError("001", errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpect exception:" + e;
            errorDesc += "\nThe byte value is " + b;
            TestLibrary.TestFramework.LogError("002", errorDesc);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        string errorDesc;

        byte b;
        bool expectedValue;
        bool actualValue;

        b = byte.MaxValue;

        TestLibrary.TestFramework.BeginScenario("PosTest2: value is byte.MaxValue.");
        try
        {
            actualValue = Convert.ToBoolean(b);
            expectedValue = 0 != b;

            if (actualValue != expectedValue)
            {
                errorDesc = "The boolean value of byte value " + b + " is not the value " + expectedValue +
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

        byte b;
        bool expectedValue;
        bool actualValue;

        b = byte.MinValue;

        TestLibrary.TestFramework.BeginScenario("PosTest3: value is byte.MinValue.");
        try
        {
            actualValue = Convert.ToBoolean(b);
            expectedValue = 0 != b;

            if (actualValue != expectedValue)
            {
                errorDesc = "The boolean value of byte integer " + b + " is not the value " + expectedValue +
                                 " as expected: actual(" + actualValue + ")";
                TestLibrary.TestFramework.LogError("005", errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpect exception:" + e;
            errorDesc += "\nThe byte value is " + b;
            TestLibrary.TestFramework.LogError("006", errorDesc);
            retVal = false;
        }
        return retVal;
    }
    #endregion
}
