// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;
/// <summary>
///System.IConvertible.ToBoolean(System.IFormatProvider)
/// </summary>
public class DecimalToBoolean
{
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
        retVal = PosTest6() && retVal;
        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Check a random Decimal.");

        try
        {
            Decimal i1 =new decimal( TestLibrary.Generator.GetSingle(-55));
            CultureInfo myCulture = new CultureInfo("en-us");
            bool actualValue = ((IConvertible)i1).ToBoolean(myCulture);
            if (!actualValue)
            {
                TestLibrary.TestFramework.LogError("001.1", "ToBoolean  should return " + actualValue);
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: check Decimal which is the MaxValue and MinValue.");

        try
        {
            Decimal i1 = Decimal.MaxValue;
            CultureInfo myCulture = new CultureInfo("en-us");
            bool actualValue = ((IConvertible)i1).ToBoolean(myCulture);
            if (!actualValue)
            {
                TestLibrary.TestFramework.LogError("002.1", "ToBoolean  should return " + actualValue);
                retVal = false;
            }
            i1 = Decimal.MinValue;
            actualValue = ((IConvertible)i1).ToBoolean(myCulture);
            if (!actualValue)
            {
                TestLibrary.TestFramework.LogError("002.2", "ToBoolean  should return " + actualValue);
                retVal = false;
            }
          
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002.4", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Check a decimal which is  -123456789.");

        try
        {
            Decimal i1 = -123456789m;
            CultureInfo myCulture = new CultureInfo("en-us");
            bool actualValue = ((IConvertible)i1).ToBoolean(myCulture);
            if (!actualValue)
            {
                TestLibrary.TestFramework.LogError("003.1", "ToBoolean  should return " + actualValue);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003.2", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    public bool PosTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest4: Check a Decimal which is  123.45e+6.");

        try
        {
            Decimal i1 = (Decimal)123.45e+6;
           CultureInfo myCulture = new CultureInfo("en-us");
            bool actualValue = ((IConvertible)i1).ToBoolean(myCulture);
            if (!actualValue)
            {
                TestLibrary.TestFramework.LogError("004.1", "ToBoolean  should return " + actualValue);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004.2", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    public bool PosTest5()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest5: Check a Decimal which is  -.123.");

        try
        {
            Decimal i1 = (Decimal)(-.123);
           CultureInfo myCulture = new CultureInfo("en-us");
            bool actualValue = ((IConvertible)i1).ToBoolean(myCulture);
            if (!actualValue)
            {
                TestLibrary.TestFramework.LogError("005.1", "ToBoolean return failed. ");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("005.2", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest6()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest6: Check a Decimal which is  +123.");

        try
        {
            Decimal i1 = (Decimal)(+123);
            CultureInfo myCulture = new CultureInfo("en-us");
            bool actualValue = ((IConvertible)i1).ToBoolean(myCulture);
            if (!actualValue)
            {
                TestLibrary.TestFramework.LogError("006.1", "ToBoolean return failed. ");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006.2", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    #endregion

    #endregion

    public static int Main()
    {
        DecimalToBoolean test = new DecimalToBoolean();

        TestLibrary.TestFramework.BeginTestCase("DecimalToBoolean");

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
