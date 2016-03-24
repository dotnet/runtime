// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.Decimal.MaxValue
/// </summary>
public class DecimalMaxValue
{
    private const string c_STRMAX = "79228162514264337593543950335";

    public static int Main()
    {
        DecimalMaxValue dMaxValue = new DecimalMaxValue();
        TestLibrary.TestFramework.BeginTestCase("for Field:System.Decimal.MaxValue");

        if (dMaxValue.RunTests())
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
        const string c_TEST_DESC = "PosTest1:Verify the Decimal.MaxValue  is equal to 79228162514264337593543950335";
        const string c_TEST_ID = "P001";

        Decimal dMax = Convert.ToDecimal(c_STRMAX);
        
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {

            if (Decimal.MaxValue != dMax)
            {
                string errorDesc = "Value is not " + dMax.ToString() + " as expected: Actual(" + Decimal.MaxValue.ToString() + ")";
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

