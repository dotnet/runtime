// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.Decimal.ctor(System.UInt32)
/// </summary>
public class DecimalCtor7
{

    public static int Main()
    {
        DecimalCtor7 dCtor7 = new DecimalCtor7();
        TestLibrary.TestFramework.BeginTestCase("for Constructor:System.Decimal.Ctor(System.UInt32)");
        
        if (dCtor7.RunTests())
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

    #region PositiveTesting
    public bool PosTest1()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest1:Verify the param is a random UInt32 ";
        const string c_TEST_ID = "P001";

        System.UInt32 uint32Value = Convert.ToUInt32(TestLibrary.Generator.GetInt32(-55));

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            Decimal decimalValue = new Decimal(uint32Value);
            if (decimalValue != Convert.ToDecimal(uint32Value))
            {
                string errorDesc = "Value is not " + decimalValue.ToString() + " as expected: param is " + uint32Value.ToString();
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
        const string c_TEST_DESC = "PosTest2:Verify the param is UInt32.MinValue(0) ";
        const string c_TEST_ID = "P002";

        UInt32 dValue = UInt32.MinValue;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            Decimal decimalValue = new Decimal(dValue);
            if (decimalValue != Convert.ToDecimal(dValue))
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

     public bool PosTest3()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest3:Verify the param is UInt32.MaxValue ";
        const string c_TEST_ID = "P003";

        UInt32 dValue = UInt32.MaxValue;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            Decimal decimalValue = new Decimal(dValue);
            if (decimalValue != Convert.ToDecimal(dValue))
            {
                string errorDesc = "Value is not " + decimalValue.ToString() + " as expected: param is " + dValue.ToString();
                TestLibrary.TestFramework.LogError("005" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006" + " TestId-" + c_TEST_ID + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
            retVal = false;
        }


        return retVal;
    }
    #endregion

}
