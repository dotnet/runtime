// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;
/// <summary>
///Floor(System.Decimal)
/// </summary>
public class DecimaFloor
{
    #region const
    private const int SEEDVALUE = 2;
    private const int EQUALVALUE = 1;
    private const int ZEROVALUE = 0;
    private const Double RAND_DOUBLE_POSITIVE = 101.111;
    private const Double RAND_DOUBLE_NEGTIVE = -101.111;
    private const int RAND_DOUBLE_INT = 101;
    #endregion
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;
        retVal = PosTest5() && retVal;
        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1:  If d doesn't have a fractional part, d is returned unchanged.");

        try
        {
            int myInt = TestLibrary.Generator.GetInt32(-55);
            Decimal myDecimal1 = new decimal(myInt);
            Decimal returnValue = Decimal.Floor(myDecimal1);
            if (returnValue != myDecimal1)
            {
                TestLibrary.TestFramework.LogError("001.1", "Calling Floor method should return " + myDecimal1);
                retVal = false;
            }

         }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.0", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2:  If d has a fractional part, the next whole Decimal number toward negative infinity that is less than d.and the decimal comes from single.");

        try
        {
            Single mySingle = TestLibrary.Generator.GetSingle(-55);
            Decimal myDecimal1 = new decimal(mySingle);
            Decimal returnValue = Decimal.Floor(myDecimal1);
            if (returnValue != ZEROVALUE)
            {
                TestLibrary.TestFramework.LogError("002.1", "Calling Floor method should return " + ZEROVALUE);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002.0", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3:  If d has a fractional part, the next whole Decimal number toward negative infinity that is less than d.and the decimal comes from double.");

        try
        {
            Double myDouble = TestLibrary.Generator.GetDouble(-55);
            Decimal myDecimal1 = new decimal(myDouble);
            Decimal returnValue = Decimal.Floor(myDecimal1);
            if (returnValue != ZEROVALUE)
            {
                TestLibrary.TestFramework.LogError("003.1", "Calling Floor method should return " + ZEROVALUE);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003.0", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest4:  If d has a fractional part, the next whole Decimal number toward negative infinity that is less than d.Positive Value.");

        try
        {
          
            Decimal myDecimal1 = new decimal(RAND_DOUBLE_POSITIVE);
            Decimal returnValue = Decimal.Floor(myDecimal1);
            if (returnValue != RAND_DOUBLE_INT)
            {
                TestLibrary.TestFramework.LogError("004.1", "Calling Floor method should return " + RAND_DOUBLE_INT);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004.0", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest5:  If d has a fractional part, the next whole Decimal number toward negative infinity that is less than d. Negitive value ");

        try
        {
          
            Decimal myDecimal1 = new decimal(RAND_DOUBLE_NEGTIVE);
            Decimal returnValue = Decimal.Floor(myDecimal1);
            if (returnValue != -RAND_DOUBLE_INT-1)
            {
                TestLibrary.TestFramework.LogError("005.1", "Calling Floor method should return " + (-RAND_DOUBLE_INT - 1));
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("005.0", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }


   
    #endregion

    #endregion

    public static int Main()
    {
        DecimaFloor test = new DecimaFloor();

        TestLibrary.TestFramework.BeginTestCase("DecimaFloor");

        if (test.RunTests())
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

   

}
