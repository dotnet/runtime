// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.Decimal.ctor(System.Int32)
/// </summary>
public class DecimalCtor2
{

    public static int Main()
    {
        DecimalCtor2 dCtor2 = new DecimalCtor2();
        TestLibrary.TestFramework.BeginTestCase("for Constructor:System.Decimal.Ctor(System.Int32)");

        if (dCtor2.RunTests())
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

        return retVal;
    }

    #region PositiveTesting
    public bool PosTest1()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest1:Verify the param is a random Int32 ";
        const string c_TEST_ID = "P001";

        System.Int32  int32Value = TestLibrary.Generator.GetInt32(-55);
        
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            Decimal decimalValue = new Decimal(int32Value);
            if (decimalValue != Convert.ToDecimal(int32Value))
            {
                string errorDesc = "Value is not " + decimalValue.ToString() + " as expected: param is " + int32Value.ToString();
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002" + " TestId-" + c_TEST_ID + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
            retVal = false;
        }


        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest2:Verify the param is 0 ";
        const string c_TEST_ID = "P002";

        Double dValue = 0;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            Decimal decimalValue = new Decimal(dValue);
            if (decimalValue != 0m)
            {
                string errorDesc = "Value is not " + decimalValue.ToString() + " as expected: param is " + dValue.ToString();
                TestLibrary.TestFramework.LogError("003" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004" + " TestId-" + c_TEST_ID + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
            retVal = false;
        }


        return retVal;
    }
    #endregion

   
}
