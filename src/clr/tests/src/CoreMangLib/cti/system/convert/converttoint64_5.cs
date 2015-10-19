using System;

/// <summary>
/// ToInt64(System.Double)
/// </summary>

public class ConvertToInt64_5
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

        //
        // TODO: Add your negative test cases here
        //
        // TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify method ToInt64(0<double<0.5)");

        try
        {
            double d;
            do
                d = TestLibrary.Generator.GetDouble(-55);
            while (d >= 0.5);

            long actual = Convert.ToInt64(d);
            long expected = 0;

            if (actual != expected)
            {
                TestLibrary.TestFramework.LogError("001.1", "Method ToInt64 Err.");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] actual = " + actual + ", expected = " + expected);
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify method ToInt64(1>double>=0.5)");

        try
        {
            double d;
            do
                d = TestLibrary.Generator.GetDouble(-55);
            while (d < 0.5);

            long actual = Convert.ToInt64(d);
            long expected = 1;

            if (actual != expected)
            {
                TestLibrary.TestFramework.LogError("002.1", "Method ToInt64 Err.");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] actual = " + actual + ", expected = " + expected);
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

    public bool PosTest3()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest3: Verify method ToInt64(0)");

        try
        {
            double d = 0d;

            long actual = Convert.ToInt64(d);
            long expected = 0;

            if (actual != expected)
            {
                TestLibrary.TestFramework.LogError("003.1", "Method ToInt64 Err.");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] actual = " + actual + ", expected = " + expected);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest4: Verify method ToInt64(int64.max)");

        try
        {
            long actual = Convert.ToInt64(Int64.MaxValue);
            long expected = Int64.MaxValue;

            if (actual != expected)
            {
                TestLibrary.TestFramework.LogError("004.1", "Method ToInt64 Err.");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] actual = " + actual + ", expected = " + expected);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest5: Verify method ToInt64(int64.min)");

        try
        {
            long actual = Convert.ToInt64(Int64.MinValue);
            long expected = Int64.MinValue;

            if (actual != expected)
            {
                TestLibrary.TestFramework.LogError("005.1", "Method ToInt32 Err.");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] actual = " + actual + ", expected = " + expected);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("005.2", "Unexpected exception: " + e);
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

        TestLibrary.TestFramework.BeginScenario("NegTest1: OverflowException is thrown for value > Int64.MaxValue.");

        try
        {
            double d = (double)Int64.MaxValue + 1;

            long i = Convert.ToInt64(d);

            TestLibrary.TestFramework.LogError("101.1", "OverflowException is thrown.");
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

    public bool NegTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest2: OverflowException is thrown for value < Int64.MinValue");

        try
        {
            // with the length of the min value being around 17 digits and the double going out to 15 or 16 digits
            // before rounding, then subtracting one simply isn't enough to force the value to be less than Int64.MaxValue.
            // If it is possible that the last two or 3 digits can get dropped and rounding can occur on the thousands, then
            // subtracting 10000 is safe bet to be lower
            double d = (double)Int64.MinValue - 10000;

            long i = Convert.ToInt64(d);

            TestLibrary.TestFramework.LogError("102.1", "OverflowException is not thrown.");
            retVal = false;
        }
        catch (OverflowException)
        { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("102.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    #endregion
    #endregion

    public static int Main()
    {
        ConvertToInt64_5 test = new ConvertToInt64_5();

        TestLibrary.TestFramework.BeginTestCase("ConvertToInt64_5");

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
