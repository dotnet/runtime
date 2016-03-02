// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// Round(System.Decimal)
/// </summary>

public class mathRound1
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        
        //
        // TODO: Add your negative test cases here
        //
        // TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify Round(System.Decimal) when decimal part of arg d < 0.5 .");

        try
        {
            Int64 tempInt64Var = TestLibrary.Generator.GetInt64(-55);

            double tempDoubleVar;
            do
                tempDoubleVar = TestLibrary.Generator.GetDouble(-55);
            while (tempDoubleVar >= 0.5);

            decimal d = decimal.Parse(tempInt64Var.ToString() + tempDoubleVar.ToString().Substring(1));

            if (Math.Round(d) != decimal.Parse(tempInt64Var.ToString()))
            {
                Console.WriteLine("actual value = " + decimal.Parse(tempInt64Var.ToString()));
                Console.WriteLine("expected value = " + Math.Round(d));
                TestLibrary.TestFramework.LogError("001.1", "Return value is wrong!");
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

    public bool PosTest2()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify Round(System.Decimal) when decimal part of arg d >= 0.5 .");

        try
        {
            Int64 tempInt64Var;
            do
                tempInt64Var = TestLibrary.Generator.GetInt64(-55);
            while (tempInt64Var == Int64.MaxValue);

            double tempDoubleVar;
            do
                tempDoubleVar = TestLibrary.Generator.GetDouble(-55);
            while (tempDoubleVar < 0.5);

            decimal d = decimal.Parse(tempInt64Var.ToString() + tempDoubleVar.ToString().Substring(1));

            if (Math.Round(d) != decimal.Parse((tempInt64Var + 1).ToString()))
            {
                Console.WriteLine("actual value = " + decimal.Parse((tempInt64Var + 1).ToString()));
                Console.WriteLine("expected value = " + Math.Round(d));
                TestLibrary.TestFramework.LogError("002.1", "Return value is wrong!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: OverflowException is not thrown.");
        
        try
        {
            decimal result = Math.Round(decimal.Parse(decimal.MaxValue.ToString() + "1"));
        }
        catch (OverflowException)
        { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("101", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        mathRound1 test = new mathRound1();

        TestLibrary.TestFramework.BeginTestCase("mathRound1");

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
