// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// ToInt16(System.Object)
/// </summary>

public class ConvertToInt16_9
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
        // TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify methos ToInt16((object)random).");

        try
        {

            object random = TestLibrary.Generator.GetInt16(-55);

            Int16 actual = Convert.ToInt16(random);
            Int16 expected = (Int16)random;

            if (actual != expected)
            {
                TestLibrary.TestFramework.LogError("001.1", "Method ToInt16 Err.");
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify method ToInt16((object)0)");

        try
        {
            object obj = 0;

            Int16 actual = Convert.ToInt16(obj);
            Int16 expected = 0;

            if (actual != expected)
            {
                TestLibrary.TestFramework.LogError("002.1", "Method ToInt16 Err.");
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
        TestLibrary.TestFramework.BeginScenario("PosTest3: Verify method ToInt16((object)int16.max)");

        try
        {
            object obj = Int16.MaxValue;

            Int16 actual = Convert.ToInt16(obj);
            Int16 expected = Int16.MaxValue;

            if (actual != expected)
            {
                TestLibrary.TestFramework.LogError("003.1", "Method ToInt16 Err.");
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
        TestLibrary.TestFramework.BeginScenario("PosTest4: Verify method ToInt16((object)int16.min)");

        try
        {
            object obj = Int16.MinValue;

            Int16 actual = Convert.ToInt16(obj);
            Int16 expected = Int16.MinValue;

            if (actual != expected)
            {
                TestLibrary.TestFramework.LogError("004.1", "Method ToInt16 Err.");
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
        TestLibrary.TestFramework.BeginScenario("PosTest5: Verify method ToInt16(true)");

        try
        {
            object obj = true;

            Int16 actual = Convert.ToInt16(obj);
            Int16 expected = 1;

            if (actual != expected)
            {
                TestLibrary.TestFramework.LogError("005.1", "Method ToInt16 Err.");
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
        TestLibrary.TestFramework.BeginScenario("PosTest6: Verify method ToInt16(false)");

        try
        {
            object obj = false;

            Int16 actual = Convert.ToInt16(obj);
            Int16 expected = 0;

            if (actual != expected)
            {
                TestLibrary.TestFramework.LogError("006.1", "Method ToInt16 Err.");
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
        TestLibrary.TestFramework.BeginScenario("PosTest7: Verify method ToInt16(null)");

        try
        {
            object obj = null;

            Int16 actual = Convert.ToInt16(obj);
            Int16 expected = 0;

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

            Int16 r = Convert.ToInt16(obj);

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
        ConvertToInt16_9 test = new ConvertToInt16_9();

        TestLibrary.TestFramework.BeginTestCase("ConvertToInt16_9");

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
