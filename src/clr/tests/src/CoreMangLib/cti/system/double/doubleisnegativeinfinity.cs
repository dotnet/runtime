// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// IsNegativeInfinity(System.Double)
/// </summary>

public class DoubleIsNegativeInfinity
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest2() && retVal;


        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify Method IsNegativeInfinity when Double is Negative value.");

        try
        {
            Double d;
            do
                d = -TestLibrary.Generator.GetDouble(-55);
            while (d == 0);

            if (!Double.IsNegativeInfinity(d / 0))
            {
                TestLibrary.TestFramework.LogError("001.1", "Method IsNegativeInfinity(System.Double) Err.");
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
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify Method IsNegativeInfinity when Double is Positive value.");

        try
        {
            Double d;
            do
                d = TestLibrary.Generator.GetDouble(-55);
            while (d == 0);

            if (Double.IsNegativeInfinity(d / 0))
            {
                TestLibrary.TestFramework.LogError("002.1", "Method IsNegativeInfinity(System.Double) Err.");
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
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify Method IsNegativeInfinity when Double is zero value.");

        try
        {
            Double d = 0.00D;

            if (Double.IsNegativeInfinity(d / 0))
            {
                TestLibrary.TestFramework.LogError("003.1", "Method IsNegativeInfinity(System.Double) Err.");
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
    #endregion

    #region Nagetive Test Cases
    //public bool NegTest1()
    //{
    //    bool retVal = true;

    //    TestLibrary.TestFramework.BeginScenario("NegTest1: ");

    //    try
    //    {
    //          //
    //          // Add your test logic here
    //          //
    //    }
    //    catch (Exception e)
    //    {
    //        TestLibrary.TestFramework.LogError("101", "Unexpected exception: " + e);
    //        TestLibrary.TestFramework.LogInformation(e.StackTrace);
    //        retVal = false;
    //    }

    //    return retVal;
    //}
    #endregion
    #endregion

    public static int Main()
    {
        DoubleIsNegativeInfinity test = new DoubleIsNegativeInfinity();

        TestLibrary.TestFramework.BeginTestCase("DoubleIsNegativeInfinity");

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
