// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;
/// <summary>
///ToByte(System.Decimal)
/// </summary>
public class DecimalToByte1
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        TestLibrary.TestFramework.LogInformation("[Negtive]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Check a random Decimal.");

        try
        {
            byte b = (byte)(TestLibrary.Generator.GetInt32(-55) % 256);
            Decimal i1 = (Decimal)(b + new decimal(TestLibrary.Generator.GetSingle(-55)));
            CultureInfo myCulture = CultureInfo.InvariantCulture;
            byte expectVaule = b;
            byte actualValue = Decimal.ToByte(i1);
            if (actualValue != expectVaule)
            {
                TestLibrary.TestFramework.LogError("001.1", "ToByte should  return " + expectVaule + " Actual: " + actualValue);
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Check a Decimal which is  byte.MaxValue and byte.MinValue.");

        try
        {
            Decimal i1 = byte.MaxValue;
            CultureInfo myCulture = CultureInfo.InvariantCulture;
            byte expectVaule = byte.MaxValue;
            byte actualValue = Decimal.ToByte(i1);
            if (actualValue != expectVaule)
            {
                TestLibrary.TestFramework.LogError("002.1", "ToByte should  return " + expectVaule);
                retVal = false;
            }

            i1 = byte.MinValue;
            expectVaule = byte.MinValue;
            actualValue = Decimal.ToByte(i1);
            if (actualValue != expectVaule)
            {
                TestLibrary.TestFramework.LogError("002.2", "ToByte should  return " + expectVaule);
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

    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: value is  greater than Byte.MaxValue.");

        try
        {
            Decimal i1 = byte.MaxValue + 1m;
            CultureInfo myCulture = CultureInfo.InvariantCulture;
            byte actualValue = Decimal.ToByte(i1);
            TestLibrary.TestFramework.LogError("101.1", "OverflowException should be caught. ");
            retVal = false;

        }
        catch (OverflowException)
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

        TestLibrary.TestFramework.BeginScenario("NegTest2: value is less than Byte.MinValue.");

        try
        {
            Decimal i1 = byte.MinValue - 1m;
            CultureInfo myCulture = CultureInfo.InvariantCulture;
            byte actualValue = Decimal.ToByte(i1);
            TestLibrary.TestFramework.LogError("102.1", "OverflowException should be caught. ");
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
    #endregion

    #endregion

    public static int Main()
    {
        DecimalToByte1 test = new DecimalToByte1();

        TestLibrary.TestFramework.BeginTestCase("DecimalToByte1");

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
            byte actualValue = Decimal.ToByte(i1);
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
