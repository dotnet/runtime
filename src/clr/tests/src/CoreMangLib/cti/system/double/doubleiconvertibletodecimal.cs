// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.IConvertible.ToDecimal(System.IFormatProvider)
/// </summary>

public class DoubleIConvertibleToDecimal
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest1: Convert a Double to Decimal.");

        try
        {
            Double d = TestLibrary.Generator.GetDouble();
            IConvertible i = (IConvertible)d;
            Decimal dm = i.ToDecimal(null);
            
            String dmString = dm.ToString();
            //truncate one more precision digit to get rid off precision difference
            dmString = dmString.Substring(0, dmString.Length - 1);

            if (!d.ToString().Contains(dmString))
            {
                TestLibrary.TestFramework.LogError("001.1", "The result " + dmString + " is not correct as expected: " + d);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Convert zero to decimal");

        try
        {
            Double d = 0;
            IConvertible i = (IConvertible)d;
            decimal dm = i.ToDecimal(null);
            if (d.ToString() != dm.ToString())
            {
                TestLibrary.TestFramework.LogError("002.1", "The result is not correct as expected");
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

    

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: Check OverflowException");

        try
        {
            Double d = Double.MaxValue;
            IConvertible i = (IConvertible)d;
            decimal dm = i.ToDecimal(null);

            TestLibrary.TestFramework.LogError("001.1", "The OverflowException is not thrown.");
            retVal = false;

        }
        catch (OverflowException)
        { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.2", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest2: Check OverflowException");

        try
        {
            Double d = Double.MinValue;
            IConvertible i = (IConvertible)d;
            decimal dm = i.ToDecimal(null);

            TestLibrary.TestFramework.LogError("002.1", "The OverflowException is not thrown.");
            retVal = false;

        }
        catch (OverflowException)
        { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002.2", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        DoubleIConvertibleToDecimal test = new DoubleIConvertibleToDecimal();

        TestLibrary.TestFramework.BeginTestCase("DoubleIConvertibleToDecimal");

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
