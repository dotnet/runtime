// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;
/// <summary>
///System.IConvertible.ToInt32(System.IFormatProvider)
/// </summary>
public class DecimalToInt32
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Check a  random Decimal.");

        try
        {
            Decimal i1 = new decimal(TestLibrary.Generator.GetSingle(-55));
            int expectValue = 0;
            if (i1 > 0.5m)
                expectValue = 1;
            else
                expectValue = 0;
            CultureInfo myCulture = new CultureInfo("en-us");
            int actualValue = ((IConvertible)i1).ToInt32(myCulture);
            if (actualValue != expectValue)
            {
                TestLibrary.TestFramework.LogError("001.1", "ToInt32 should return " + expectValue);
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Check a Decimal which is  -123.");

        try
        {
            Decimal i1 = int.MaxValue;
            CultureInfo myCulture = CultureInfo.InvariantCulture;
            int actualValue = ((IConvertible)i1).ToInt32(myCulture);
            if (actualValue != int.MaxValue)
            {
                TestLibrary.TestFramework.LogError("002.1", "ToInt32  return failed. ");
                retVal = false;
            }

            i1 = int.MinValue;
            actualValue = ((IConvertible)i1).ToInt32(myCulture);
            if (actualValue != int.MinValue)
            {
                TestLibrary.TestFramework.LogError("002.2", "ToInt32  return failed. ");
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

        TestLibrary.TestFramework.BeginScenario("NegTest1: Check a Decimal which is  >Int32.MaxValue.");

        try
        {
            Decimal i1 = Int32.MaxValue +1m;
            CultureInfo myCulture = new CultureInfo("en-us");
            int actualValue = ((IConvertible)i1).ToInt32(myCulture);
            TestLibrary.TestFramework.LogError("101.1", "OverflowException should be caught.");
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

        TestLibrary.TestFramework.BeginScenario("NegTest2: Check a Decimal which is  <Int32.MinValue.");

        try
        {
            Decimal i1 = Int32.MinValue - 1m;
            CultureInfo myCulture = new CultureInfo("en-us");
            int actualValue = ((IConvertible)i1).ToInt32(myCulture);
            TestLibrary.TestFramework.LogError("102.1", "ToInt32  return failed. ");
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
        DecimalToInt32 test = new DecimalToInt32();

        TestLibrary.TestFramework.BeginTestCase("DecimalToInt32");

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
