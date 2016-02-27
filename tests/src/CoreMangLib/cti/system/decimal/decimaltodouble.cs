// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;
/// <summary>
///System.IConvertible.ToDouble(System.IFormatProvider)
/// </summary>
public class DecimalToDouble
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Check a  random Decimal.");

        try
        {
            Decimal d1 = new decimal(TestLibrary.Generator.GetSingle(-55));
            CultureInfo myCulture = CultureInfo.InvariantCulture;
            double actualValue = ((IConvertible)d1).ToDouble(myCulture);
            double expectValue = (double)d1;
            if (actualValue != expectValue)
            {
                TestLibrary.TestFramework.LogError("001.1", "ToDouble should return " + expectValue);
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Check a Decimal which is  the  Decimal.MaxValue and Decimal.MinValue.");

        try
        {
            Decimal i1 = Decimal.MaxValue;
            CultureInfo myCulture = CultureInfo.InvariantCulture;
            Double actualValue = ((IConvertible)i1).ToDouble(myCulture);
            Double expectValue = (double)i1;
            if (actualValue != expectValue)
            {
                TestLibrary.TestFramework.LogError("002.1", "ToDouble should return " + expectValue);
                retVal = false;
            }

            i1 = Decimal.MinValue;
            actualValue = ((IConvertible)i1).ToDouble(myCulture);
            expectValue = (double)i1;
            if (actualValue != expectValue)
            {
                TestLibrary.TestFramework.LogError("002.2", "ToDouble should return " + expectValue);
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
        DecimalToDouble test = new DecimalToDouble();

        TestLibrary.TestFramework.BeginTestCase("DecimalToDouble");

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
