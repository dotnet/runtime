// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// Equals(System.Double)
/// </summary>

public class DoubleEquals1
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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify Double value equals with Equals(Double).");

        try
        {
            Double d1 = new Random(-55).NextDouble();
            Double d2 = d1;

            if (!d1.Equals(d2))
            {
                TestLibrary.TestFramework.LogError("001.1", "Method Double.Equals(Double) Err.");
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify Double value not equals with Equals(Double).");

        try
        {
            Random random = new Random(-55);
            Double d1,d2;
            do
            {
                d1 = random.NextDouble();
                d2 = random.NextDouble();
            }
            while (d1 == d2);

            if (d1.Equals(d2))
            {
                TestLibrary.TestFramework.LogError("002.1", "Method Double.Equals(Double) Err.");
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

    #endregion

    public static int Main()
    {
        DoubleEquals1 test = new DoubleEquals1();

        TestLibrary.TestFramework.BeginTestCase("DoubleEquals1");

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
