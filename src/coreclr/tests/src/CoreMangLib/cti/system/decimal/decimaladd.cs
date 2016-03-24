// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.Decimal.Add(Decimal,Decimal)
/// </summary>
public class DecimalAdd
{

    public static int Main()
    {
        DecimalAdd dAdd = new DecimalAdd();
        TestLibrary.TestFramework.BeginTestCase("for Method:System.Decimal.Add(Decimal,Decimal)");

        if (dAdd.RunTests())
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

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        return retVal;
    }

    #region PositiveTesting
    public bool PosTest1()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest1:Verify two params both are plus...";
        const string c_TEST_ID = "P001";

        Decimal dec1 = 12345.678m;
        Decimal dec2 = 87654.321m;
        Decimal resDec = 99999.999m;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            Decimal decimalValue = Decimal.Add(dec1, dec2); ;
            if (decimalValue != resDec)
            {
                string errorDesc = "Value is not " + resDec.ToString() + " as expected: param is " + decimalValue.ToString();
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
        const string c_TEST_DESC = "PosTest2:Verify a  param is zero...";
        const string c_TEST_ID = "P003";

        Decimal dec1 = 623512345.678m;
        Decimal dec2 = 0m;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            Decimal decimalValue = Decimal.Add(dec1, dec2); ;
            if (decimalValue != dec1)
            {
                string errorDesc = "Value is not " + dec1.ToString() + " as expected: param is " + decimalValue.ToString();
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
        const string c_TEST_DESC = "PosTest3:Verify a  param is negative  ";
        const string c_TEST_ID = "P003";

        Decimal dec1 = 87654.321234m;
        Decimal dec2 = -12345.678321m;
        Decimal resDec = 75308.642913m;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            Decimal decimalValue = Decimal.Add(dec1, dec2); ;
            if (decimalValue != resDec)
            {
                string errorDesc = "Value is not " + resDec.ToString() + " as expected: param is " + decimalValue.ToString();
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

    public bool PosTest4()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest4:Verify two params both are negative...";
        const string c_TEST_ID = "P004";

        Decimal dec1 = -87654.321234m;
        Decimal dec2 = -12345.678321m;
        Decimal resDec = -99999.999555m;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            Decimal decimalValue = Decimal.Add(dec1, dec2); ;
            if (decimalValue != resDec)
            {
                string errorDesc = "Value is not " + resDec.ToString() + " as expected: param is " + decimalValue.ToString();
                TestLibrary.TestFramework.LogError("007" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008" + " TestId-" + c_TEST_ID + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
            retVal = false;
        }


        return retVal;
    }
    #endregion

    #region NegativeTesting
    public bool NegTest1()
    {
        bool retVal = true;
        const string c_TEST_DESC = "NegTest1:Verify the sum of two  larger than Decimal.MaxValue";
        const string c_TEST_ID = "N001";

        Decimal dec1 = Decimal.MaxValue;
        Decimal dec2 = 12345.678321m;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);


        try
        {
            Decimal decimalValue = Decimal.Add(dec1,dec2);
            TestLibrary.TestFramework.LogError("009" + " TestId-" + c_TEST_ID, "OverflowException is not thrown as expected ." + "\n parame value is " + dec1.ToString() + " and " + dec2.ToString());
            retVal = false;
        }
        catch (OverflowException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    #endregion
}