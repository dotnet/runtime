// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.Decimal.ctor(Single)
/// </summary>
public class DecimalCtor6
{

    public static int Main()
    {
        DecimalCtor6 dCtor6 = new DecimalCtor6();
        TestLibrary.TestFramework.BeginTestCase("for Constructor:System.Decimal.Ctor(Single)");

        if (dCtor6.RunTests())
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
        const string c_TEST_DESC = "PosTest1:Verify the param is a random float ";
        const string c_TEST_ID = "P001";

        float dValue = TestLibrary.Generator.GetSingle(-55);
        while (dValue > Convert.ToSingle(Decimal.MaxValue) || dValue < Convert.ToSingle(Decimal.MinValue))
        {
            dValue = TestLibrary.Generator.GetSingle(-55);
        }

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            Decimal decimalValue = new Decimal(dValue);
            if (decimalValue != Convert.ToDecimal(dValue))
            {
                string errorDesc = "Value is not " + decimalValue.ToString() + " as expected: param is " + dValue.ToString();
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
        const string c_TEST_DESC = "PosTest2:Verify the param is 0.00 ";
        const string c_TEST_ID = "P002";

        float dValue = 0.00F;

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

    public bool PosTest3()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest3:Verify the param is 1.23456789E-25F ";
        const string c_TEST_ID = "P003";

        float dValue = 1.23456789E-25F;
        Decimal resDec = 0.0000000000000000000000001235m;
        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);

        try
        {
            Decimal decimalValue = new Decimal(dValue);
            if (decimalValue != resDec)
            {
                string errorDesc = "Value is not " + decimalValue.ToString() + " as expected: param is " + resDec.ToString();
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

        float dValue = Convert.ToSingle(Decimal.MaxValue);

        float addValue = TestLibrary.Generator.GetSingle(-55);
        while (addValue <= 0)
        {
            addValue = TestLibrary.Generator.GetSingle(-55);
        }
        dValue += addValue;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);


        try
        {
            Decimal decimalValue = new Decimal(dValue);
            TestLibrary.TestFramework.LogError("005" + " TestId-" + c_TEST_ID, "ArgumentException is not thrown as expected ." + "\n parame value is " + dValue.ToString());
            retVal = false;
        }
        catch (OverflowException)
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

        float dValue = Convert.ToSingle(Decimal.MinValue);

        float addValue = TestLibrary.Generator.GetSingle(-55);
        while (addValue <= 0)
        {
            addValue = TestLibrary.Generator.GetSingle(-55);
        }
        dValue -= addValue;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);


        try
        {
            Decimal decimalValue = new Decimal(dValue);
            TestLibrary.TestFramework.LogError("007" + " TestId-" + c_TEST_ID, "ArgumentException is not thrown as expected ." + "\n arame value is " + dValue.ToString());
            retVal = false;
        }
        catch (OverflowException)
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
        const string c_TEST_DESC = "NegTest3:Verify the param is Single.NaN";
        const string c_TEST_ID = "N003";

        Single dValue = Single.NaN;

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
        const string c_TEST_DESC = "NegTest4:Verify the param is Single.PositiveInfinity";
        const string c_TEST_ID = "N004";

        Single dValue = Single.PositiveInfinity;

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
        const string c_TEST_DESC = "NegTest5:Verify the param is Single.NegativeInfinity";
        const string c_TEST_ID = "N005";

        Single dValue = Single.NegativeInfinity;

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
