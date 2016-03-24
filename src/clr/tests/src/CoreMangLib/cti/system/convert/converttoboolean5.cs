// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// Convert.ToBoolean(Double)
/// Converts the value of the specified double float value to an equivalent Boolean value. 
/// </summary>
public class ConvertToBoolean
{
    public static int Main()
    {
        ConvertToBoolean testObj = new ConvertToBoolean();

        TestLibrary.TestFramework.BeginTestCase("for method: Convert.ToBoolean(Double)");
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
        retVal = PosTest6() && retVal;
        retVal = PosTest7() && retVal;
        retVal = PosTest8() && retVal;
        retVal = PosTest9() && retVal;

        return retVal;
    }

    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;
        string errorDesc;

        double d;
        bool expectedValue;
        bool actualValue;

        d = TestLibrary.Generator.GetDouble(-55);

        TestLibrary.TestFramework.BeginScenario("PosTest1: Random double value between 0.0 and 1.0.");
        try
        {
            actualValue = Convert.ToBoolean(d);
            expectedValue = 0 != d;

            if (actualValue != expectedValue)
            {
                errorDesc = "The boolean value of double value " + d + " is not the value " + expectedValue + 
                                 " as expected: actual(" + actualValue + ")";
                TestLibrary.TestFramework.LogError("001", errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpect exception:" + e;
            errorDesc += "\nThe double value is " + d;
            TestLibrary.TestFramework.LogError("002", errorDesc);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        string errorDesc;

        double d;
        bool expectedValue;
        bool actualValue;

        d = double.MaxValue;

        TestLibrary.TestFramework.BeginScenario("PosTest2: value is double.MaxValue.");
        try
        {
            actualValue = Convert.ToBoolean(d);
            expectedValue = 0 != d;

            if (actualValue != expectedValue)
            {
                errorDesc = "The boolean value of double value " + d + " is not the value " + expectedValue +
                                 " as expected: actual(" + actualValue + ")";
                TestLibrary.TestFramework.LogError("003", errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpect exception:" + e;
            errorDesc += "\nThe double value is " + d;
            TestLibrary.TestFramework.LogError("004", errorDesc);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        string errorDesc;

        double d;
        bool expectedValue;
        bool actualValue;

        d = double.MinValue;

        TestLibrary.TestFramework.BeginScenario("PosTest3: value is double.MinValue.");
        try
        {
            actualValue = Convert.ToBoolean(d);
            expectedValue = 0 != d;

            if (actualValue != expectedValue)
            {
                errorDesc = "The boolean value of double value " + d + " is not the value " + expectedValue +
                                 " as expected: actual(" + actualValue + ")";
                TestLibrary.TestFramework.LogError("005", errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpect exception:" + e;
            errorDesc += "\nThe double value is " + d;
            TestLibrary.TestFramework.LogError("006", errorDesc);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;
        string errorDesc;

        double d;
        bool expectedValue;
        bool actualValue;

        d = double.Epsilon;

        TestLibrary.TestFramework.BeginScenario("PosTest4: value is double.Epsilon.");
        try
        {
            actualValue = Convert.ToBoolean(d);
            expectedValue = 0 != d;

            if (actualValue != expectedValue)
            {
                errorDesc = "The boolean value of double value " + d + " is not the value " + expectedValue +
                                 " as expected: actual(" + actualValue + ")";
                TestLibrary.TestFramework.LogError("007", errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpect exception:" + e;
            errorDesc += "\nThe double value is " + d;
            TestLibrary.TestFramework.LogError("008", errorDesc);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;
        string errorDesc;

        double d;
        bool expectedValue;
        bool actualValue;

        d = -1 * TestLibrary.Generator.GetDouble(-55);

        TestLibrary.TestFramework.BeginScenario("PosTest5: Random double value between -0.0 and -1.0.");
        try
        {
            actualValue = Convert.ToBoolean(d);
            expectedValue = 0 != d;

            if (actualValue != expectedValue)
            {
                errorDesc = "The boolean value of double value " + d + " is not the value " + expectedValue +
                                 " as expected: actual(" + actualValue + ")";
                TestLibrary.TestFramework.LogError("009", errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpect exception:" + e;
            errorDesc += "\nThe double value is " + d;
            TestLibrary.TestFramework.LogError("010", errorDesc);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest6()
    {
        bool retVal = true;
        string errorDesc;

        double d;
        bool expectedValue;
        bool actualValue;

        d = double.NaN;

        TestLibrary.TestFramework.BeginScenario("PosTest6: value is double.NaN.");
        try
        {
            actualValue = Convert.ToBoolean(d);
            expectedValue = 0 != d;

            if (actualValue != expectedValue)
            {
                errorDesc = "The boolean value of double value " + d + " is not the value " + expectedValue +
                                 " as expected: actual(" + actualValue + ")";
                TestLibrary.TestFramework.LogError("011", errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpect exception:" + e;
            errorDesc += "\nThe double value is " + d;
            TestLibrary.TestFramework.LogError("012", errorDesc);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest7()
    {
        bool retVal = true;
        string errorDesc;

        double d;
        bool expectedValue;
        bool actualValue;

        d = double.NegativeInfinity;

        TestLibrary.TestFramework.BeginScenario("PosTest7: value is double.NegativeInfinity.");
        try
        {
            actualValue = Convert.ToBoolean(d);
            expectedValue = 0 != d;

            if (actualValue != expectedValue)
            {
                errorDesc = "The boolean value of double value " + d + " is not the value " + expectedValue +
                                 " as expected: actual(" + actualValue + ")";
                TestLibrary.TestFramework.LogError("013", errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpect exception:" + e;
            errorDesc += "\nThe double value is " + d;
            TestLibrary.TestFramework.LogError("014", errorDesc);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest8()
    {
        bool retVal = true;
        string errorDesc;

        double d;
        bool expectedValue;
        bool actualValue;

        d = double.PositiveInfinity;

        TestLibrary.TestFramework.BeginScenario("PosTest8: value is double.PositiveInfinity.");
        try
        {
            actualValue = Convert.ToBoolean(d);
            expectedValue = 0 != d;

            if (actualValue != expectedValue)
            {
                errorDesc = "The boolean value of double value " + d + " is not the value " + expectedValue +
                                 " as expected: actual(" + actualValue + ")";
                TestLibrary.TestFramework.LogError("015", errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpect exception:" + e;
            errorDesc += "\nThe double value is " + d;
            TestLibrary.TestFramework.LogError("016", errorDesc);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest9()
    {
        bool retVal = true;
        string errorDesc;

        double d;
        bool expectedValue;
        bool actualValue;

        d = -1 * double.Epsilon;

        TestLibrary.TestFramework.BeginScenario("PosTest9: value is negative double.Epsilon.");
        try
        {
            actualValue = Convert.ToBoolean(d);
            expectedValue = 0 != d;

            if (actualValue != expectedValue)
            {
                errorDesc = "The boolean value of double value " + d + " is not the value " + expectedValue +
                                 " as expected: actual(" + actualValue + ")";
                TestLibrary.TestFramework.LogError("017", errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpect exception:" + e;
            errorDesc += "\nThe double value is " + d;
            TestLibrary.TestFramework.LogError("018", errorDesc);
            retVal = false;
        }
        return retVal;
    }
    #endregion
}
