// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

public class MathRound3
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;

        retVal = NegTest1() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify Round(System.double) when decimal part of arg d < =0.5 .");

        try
        {
            int tempIntVar = TestLibrary.Generator.GetInt32(-55);

            double tempDoubleVar;
            do
                tempDoubleVar = TestLibrary.Generator.GetDouble(-55);
            while (tempDoubleVar > 0.5);

            double d = tempIntVar + tempDoubleVar;

            if (Math.Round(d) != tempIntVar)
            {
                Console.WriteLine("actual value = " + tempIntVar);
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify Round(System.double) when decimal part of arg d > 0.5 .");

        try
        {
            int tempIntVar = TestLibrary.Generator.GetInt32(-55);

            double tempDoubleVar;
            do
                tempDoubleVar = TestLibrary.Generator.GetDouble(-55);
            while (tempDoubleVar <= 0.5);

            double d = tempIntVar + tempDoubleVar;

            if (Math.Round(d) != tempIntVar + 1)
            {
                Console.WriteLine("actual value = " + (tempIntVar + 1));
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
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: OverflowException is not thrown.");

        try
        {
            double result = Math.Round(double.Parse(double.MaxValue.ToString() + "1"));
            TestLibrary.TestFramework.LogError("101.1", " OverflowException is not thrown.");
            retVal = false;
        }
        catch (OverflowException)
        { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("101.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        MathRound3 test = new MathRound3();

        TestLibrary.TestFramework.BeginTestCase("MathRound3");

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
