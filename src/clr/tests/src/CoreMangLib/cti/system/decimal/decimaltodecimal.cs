// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;
/// <summary>
///System.IConvertible.ToDecimal(System.IFormatProvider)
/// </summary>
public class DecimalToDecimal
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
      
        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Check a random Decimal.");

        try
        {
            Decimal i1 = new decimal(TestLibrary.Generator.GetSingle(-55));
            CultureInfo myCulture = CultureInfo.InvariantCulture;
            Decimal expectVaule = i1;
            Decimal actualValue = ((IConvertible)i1).ToDecimal(myCulture);
            if (actualValue != expectVaule)
            {
                TestLibrary.TestFramework.LogError("001.1", "ToDecimal should  return " + expectVaule);
                retVal = false;
            }


            i1 = new decimal(TestLibrary.Generator.GetInt32(-55));
            expectVaule = i1;
            actualValue = ((IConvertible)i1).ToDecimal(myCulture);
            if (actualValue != expectVaule)
            {
                TestLibrary.TestFramework.LogError("001.2", "ToDecimal should  return " + expectVaule);
                retVal = false;
            }


            i1 = new decimal(TestLibrary.Generator.GetSingle(-55));
            expectVaule = i1;
            actualValue = ((IConvertible)i1).ToDecimal(myCulture);
            if (actualValue != expectVaule)
            {
                TestLibrary.TestFramework.LogError("001.3", "ToDecimal should  return " + expectVaule);
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Check a Decimal which is  byte.MaxValue and byte.MinValue.");

        try
        {
            Decimal i1 = Decimal.MaxValue;
            CultureInfo myCulture = CultureInfo.InvariantCulture;
            Decimal expectVaule = Decimal.MaxValue;
            Decimal actualValue = ((IConvertible)i1).ToDecimal(myCulture);
            if (actualValue != expectVaule)
            {
                TestLibrary.TestFramework.LogError("002.1", "ToDecimal should  return " + expectVaule);
                retVal = false;
            }

            i1 = Decimal.MinValue;
            expectVaule = Decimal.MinValue;
            actualValue = ((IConvertible)i1).ToDecimal(myCulture);
            if (actualValue != expectVaule)
            {
                TestLibrary.TestFramework.LogError("002.2", "ToDecimal should  return " + expectVaule);
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

   
    #endregion

    #endregion

    public static int Main()
    {
        DecimalToDecimal test = new DecimalToDecimal();

        TestLibrary.TestFramework.BeginTestCase("DecimalToDecimal");

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
    #region private method
    private bool VerifyHelper(Decimal i1, string errorno)
    {
        bool retVal = true;

        try
        {
            CultureInfo myCulture = CultureInfo.InvariantCulture;
            Decimal actualValue = ((IConvertible)i1).ToDecimal(myCulture);
            TestLibrary.TestFramework.LogError(errorno, "OverflowException should be caught. ");
            retVal = false;

        }
        catch (OverflowException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError(errorno + ".0", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion

}
