// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.Decimal.MinValue
/// </summary>
public class DecimalMinValue
{
    private const string c_STRMIN = "-79228162514264337593543950335";

    public static int Main()
    {
        DecimalMinValue dMinValue = new DecimalMinValue();
        TestLibrary.TestFramework.BeginTestCase("for Field:System.Decimal.MinValue");

        if (dMinValue.RunTests())
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
        return retVal;
    }

    #region PositiveTesting
    public bool PosTest1()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest1:Verify the Decimal.MinValue  is equal to -79228162514264337593543950335";
        const string c_TEST_ID = "P001";

        Decimal dMin = Convert.ToDecimal(c_STRMIN);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {

            if (Decimal.MinValue != dMin)
            {
                string errorDesc = "Value is not " + dMin.ToString() + " as expected: Actual(" + Decimal.MinValue.ToString() + ")";
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
            retVal = false;
        }


        return retVal;
    }


    #endregion
}

