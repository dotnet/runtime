// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.Decimal.ctor(Double)
/// </summary>
public class DecimalCtor1
{

    public static int Main()
    {
        DecimalCtor1 dCtor1 = new DecimalCtor1();
        TestLibrary.TestFramework.BeginTestCase("for Constructor:System.Decimal.Ctor(Double)");

        if (dCtor1.RunTests())
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

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;
        retVal = NegTest5() && retVal;

        return retVal;
    }

    #region PositiveTesting
    public bool PosTest1()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest1:Verify the param is a random double ";
        const string c_TEST_ID = "P001";

        Double dValue = TestLibrary.Generator.GetDouble(-55);
        while (dValue > Convert.ToDouble(Decimal.MaxValue) || dValue < Convert.ToDouble(Decimal.MinValue))
        {
            dValue = TestLibrary.Generator.GetDouble(-55);
        }

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            Decimal decimalValue = new Decimal(dValue);
            if (decimalValue != Convert.ToDecimal(dValue))
            {
                string errorDesc = "Value is not " + decimalValue.ToString() + " as expected: param is "+ dValue.ToString() ;
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

    #region Nagetive Testing
    public bool NegTest1()
    {
        bool retVal = true;
        const string c_TEST_DESC = "NegTest1:Verify the param is larger than Decimal.MaxValue";
        const string c_TEST_ID = "N001";

        double dValue = 1.2345678901234567E+35;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);


        try
        {
            Decimal decimalValue = new Decimal(dValue);
            TestLibrary.TestFramework.LogError("005" + " TestId-" + c_TEST_ID, "ArgumentException is not thrown as expected ." + "\n arame value is "+dValue.ToString());
            retVal = false;
        }
        catch(OverflowException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

     public bool NegTest2()
    {
        bool retVal = true;
        const string c_TEST_DESC = "NegTest2:Verify the param is less than Decimal.MinValue";
        const string c_TEST_ID = "N002";

        double dValue = -1.2345678901234567E+35;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);


        try
        {
            Decimal decimalValue = new Decimal(dValue);
            TestLibrary.TestFramework.LogError("007" + " TestId-" + c_TEST_ID, "ArgumentException is not thrown as expected ." + "\n arame value is "+dValue.ToString());
            retVal = false;
        }
        catch(OverflowException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest3()
    {
        bool retVal = true;
        const string c_TEST_DESC = "NegTest3:Verify the param is Double.NaN";
        const string c_TEST_ID = "N003";

        double dValue = Double.NaN;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);


        try
        {
            Decimal decimalValue = new Decimal(dValue);
            TestLibrary.TestFramework.LogError("009" + " TestId-" + c_TEST_ID, "ArgumentException is not thrown as expected ." + "\n arame value is " + dValue.ToString());
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

    public bool NegTest4()
    {
        bool retVal = true;
        const string c_TEST_DESC = "NegTest4:Verify the param is Double.PositiveInfinity";
        const string c_TEST_ID = "N004";

        double dValue = Double.PositiveInfinity;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);


        try
        {
            Decimal decimalValue = new Decimal(dValue);
            TestLibrary.TestFramework.LogError("011" + " TestId-" + c_TEST_ID, "ArgumentException is not thrown as expected ." + "\n arame value is " + dValue.ToString());
            retVal = false;
        }
        catch (OverflowException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest5()
    {
        bool retVal = true;
        const string c_TEST_DESC = "NegTest5:Verify the param is Double.NegativeInfinity";
        const string c_TEST_ID = "N005";

        double dValue = Double.NegativeInfinity;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);


        try
        {
            Decimal decimalValue = new Decimal(dValue);
            TestLibrary.TestFramework.LogError("013" + " TestId-" + c_TEST_ID, "ArgumentException is not thrown as expected ." + "\n arame value is " + dValue.ToString());
            retVal = false;
        }
        catch (OverflowException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("014" + " TestId-" + c_TEST_ID, "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion
}
