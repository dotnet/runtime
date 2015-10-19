using System;

/// <summary>
/// ToInt64(System.Object)
/// </summary>

public class ConvertToInt64_9
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
        retVal = PosTest7() && retVal;
        
        //
        // TODO: Add your negative test cases here
        //
         TestLibrary.TestFramework.LogInformation("[Negative]");
         retVal = NegTest1() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify methos ToInt64((object)random).");

        try
        {

            object random = TestLibrary.Generator.GetInt64(-55);

            long actual = Convert.ToInt64(random);
            long expected = (long)random;

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
        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify method ToInt64((object)0)");

        try
        {
            object obj = 0;

            long actual = Convert.ToInt64(obj);
            long expected = 0;

            if (actual != expected)
            {
                TestLibrary.TestFramework.LogError("002.1", "Method ToInt64 Err.");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] actual = " + actual + ", expected = " + expected);
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

    public bool PosTest3()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest3: Verify method ToInt64((object)int64.max)");

        try
        {
            object obj = Int64.MaxValue;

            long actual = Convert.ToInt64(obj);
            long expected = Int64.MaxValue;

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
        TestLibrary.TestFramework.BeginScenario("PosTest4: Verify method ToInt64((object)int64.min)");

        try
        {
            object obj = Int64.MinValue;

            long actual = Convert.ToInt64(obj);
            long expected = Int64.MinValue;

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
        TestLibrary.TestFramework.BeginScenario("PosTest5: Verify method ToInt64(true)");

        try
        {
            object obj = true;

            long actual = Convert.ToInt64(obj);
            long expected = 1;

            if (actual != expected)
            {
                TestLibrary.TestFramework.LogError("005.1", "Method ToInt64 Err.");
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

    public bool PosTest6()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest6: Verify method ToInt64(false)");

        try
        {
            object obj = false;

            long actual = Convert.ToInt64(obj);
            long expected = 0;

            if (actual != expected)
            {
                TestLibrary.TestFramework.LogError("006.1", "Method ToInt64 Err.");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] actual = " + actual + ", expected = " + expected);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest7()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest7: Verify method ToInt64(null)");

        try
        {
            object obj = null;

            long actual = Convert.ToInt64(obj);
            long expected = 0;

            if (actual != expected)
            {
                TestLibrary.TestFramework.LogError("007.1", "Method ToInt16 Err.");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] actual = " + actual + ", expected = " + expected);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("007.2", "Unexpected exception: " + e);
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

        TestLibrary.TestFramework.BeginScenario("NegTest1: InvalidCastException is not thrown.");

        try
        {
            object obj = new object();

            long r = Convert.ToInt64(obj);

            TestLibrary.TestFramework.LogError("101.1", "InvalidCastException is not thrown.");
            retVal = false;
        }
        catch (InvalidCastException)
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
        ConvertToInt64_9 test = new ConvertToInt64_9();

        TestLibrary.TestFramework.BeginTestCase("ConvertToInt64_9");

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
