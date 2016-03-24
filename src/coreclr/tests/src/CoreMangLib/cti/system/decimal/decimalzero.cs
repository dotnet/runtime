// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.Decimal.Zero
/// </summary>
public class DecimalZero
{

    public static int Main()
    {
        DecimalZero dZero = new DecimalZero();
        TestLibrary.TestFramework.BeginTestCase("for Field:System.Decimal.Zero");

        if (dZero.RunTests())
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
        const string c_TEST_DESC = "PosTest1:Verify the Decimal Zero is equal to 0";
        const string c_TEST_ID = "P001";

        Decimal dZero = Convert.ToDecimal(0);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {

            if (Decimal.Zero != dZero)
            {
                string errorDesc = "Value is not " + dZero.ToString() + " as expected: Actual(" + Decimal.Zero.ToString() + ")";
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

