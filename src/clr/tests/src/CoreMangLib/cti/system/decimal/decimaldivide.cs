// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;
/// <summary>
/// Divide(System.Decimal,System.Decimal)
/// </summary>
public class DecimalDivide
{
    #region const
    private const int SEEDVALUE = 2;
    private const int EQUALVALUE = 1;
    private const int ZEROVALUE = 0;
    #endregion
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;
        TestLibrary.TestFramework.LogInformation("[Negtive]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Calling Devide method and the dividend is a random decimal,divisor is defined as Seed.");

        try
        {

            Decimal myDecimal1 = new decimal(TestLibrary.Generator.GetInt32(-55) / SEEDVALUE);
            Decimal myDecimal2 = new decimal(SEEDVALUE);
            Decimal returnValue = Decimal.Divide(myDecimal1 * SEEDVALUE, myDecimal2);
            if (returnValue != myDecimal1)
            {
                TestLibrary.TestFramework.LogError("001.2", "Calling Devide method should return " + myDecimal1);
                retVal = false;
            }
          

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.2", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Calling Devide method and the dividend 0.");

        try
        {

            Decimal myDecimal1 = new decimal(ZEROVALUE / SEEDVALUE);
            Decimal myDecimal2 = new decimal(SEEDVALUE);
            Decimal returnValue = Decimal.Divide(ZEROVALUE, myDecimal2);
            if (returnValue != ZEROVALUE)
            {
                TestLibrary.TestFramework.LogError("002.2", "Calling Devide method should return " + ZEROVALUE);
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002.2", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Calling Devide method and the dividend is Int32.MaxValue or Int32.MinValue.");

        try
        {

            Decimal myDecimal1 = new decimal(Int32.MaxValue / SEEDVALUE);
            Decimal myDecimal2 = new decimal(SEEDVALUE);
            Decimal returnValue = Decimal.Divide(myDecimal1 * SEEDVALUE, myDecimal2);
            if (returnValue != myDecimal1)
            {
                TestLibrary.TestFramework.LogError("003.1", "Calling Devide method should return " + myDecimal1);
                retVal = false;
            }
            myDecimal1 = new decimal(Int32.MinValue / SEEDVALUE);
            myDecimal2 = new decimal(SEEDVALUE);
            returnValue = Decimal.Divide(myDecimal1 * SEEDVALUE, myDecimal2);
            if (returnValue != myDecimal1)
            {
                TestLibrary.TestFramework.LogError("003.2", "Calling Devide method should return " + myDecimal1);
                retVal = false;
            }

            myDecimal1 = new decimal(Int32.MinValue);
            myDecimal2 = new decimal(Int32.MinValue);
            returnValue = Decimal.Divide(myDecimal1 , myDecimal2);
            if (returnValue != EQUALVALUE)
            {
                TestLibrary.TestFramework.LogError("003.3", "Calling Devide method should return " + EQUALVALUE);
                retVal = false;
            }

            myDecimal1 = new decimal(Int32.MaxValue);
            myDecimal2 = new decimal(Int32.MaxValue);
            returnValue = Decimal.Divide(myDecimal1, myDecimal2);
            if (returnValue != EQUALVALUE)
            {
                TestLibrary.TestFramework.LogError("003.4", "Calling Devide method should return " + EQUALVALUE);
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
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: d2 is zero.");

        try
        {

            Decimal myDecimal1 = new decimal(TestLibrary.Generator.GetInt32(-55) );
            Decimal myDecimal2 = new decimal(ZEROVALUE);
            Decimal returnValue = Decimal.Divide(ZEROVALUE, myDecimal2);
            TestLibrary.TestFramework.LogError("101.1", "DivideByZeroException should be caught.");
            retVal = false;
         }
        catch (DivideByZeroException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("101.2", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    public bool NegTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest2: The return value (that is, the quotient) is  greater than MaxValue.");

        try
        {

            Decimal myDecimal1 = new decimal(Int32.MaxValue);
            Decimal myDecimal2 = new decimal(1e-020);
            Decimal returnValue = Decimal.Divide(myDecimal1, myDecimal2);
            TestLibrary.TestFramework.LogError("102.1", "OverflowException should be caught.");
            retVal = false;
        }
        catch (OverflowException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("102.2", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest3: The return value (that is, the quotient) is less than MinValue .");

        try
        {

            Decimal myDecimal1 = new decimal(Int32.MinValue);
            Decimal myDecimal2 = new decimal(1e-020);
            Decimal returnValue = Decimal.Divide(myDecimal1, myDecimal2);
            TestLibrary.TestFramework.LogError("103.1", "OverflowException should be caught.");
            retVal = false;
        }
        catch (OverflowException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("103.2", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
   
    #endregion

    #endregion

    public static int Main()
    {
        DecimalDivide test = new DecimalDivide();

        TestLibrary.TestFramework.BeginTestCase("DecimalDivide");

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
