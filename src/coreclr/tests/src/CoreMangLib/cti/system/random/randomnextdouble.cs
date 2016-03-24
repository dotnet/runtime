// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// NextDouble
/// </summary>
public class RandomNextDouble
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

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call NextDouble when the seed is default value");

        try
        {
            Random random = new Random(-55);
            double value = random.NextDouble();

            if ((value < 0.0) || (value >= 1.0))
            {
                TestLibrary.TestFramework.LogError("001.1", "NextDouble returns a value less than 0 or equal to 1.0");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] value = " + value);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Call NextDouble when the seed is MaxValue");

        try
        {
            Random random = new Random(Int32.MaxValue);
            double value = random.NextDouble();

            if ((value < 0.0) || (value >= 1.0))
            {
                TestLibrary.TestFramework.LogError("002.1", "NextDouble returns a value less than 0 or equal to 1.0");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] value = " + value);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Call NextDouble when the seed is 0");

        try
        {
            Random random = new Random(0);
            double value = random.NextDouble();

            if ((value < 0.0) || (value >= 1.0))
            {
                TestLibrary.TestFramework.LogError("003.1", "NextDouble returns a value less than 0 or equal to 1.0");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] value = " + value);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest4: Call NextDouble when the seed is a negative value");

        try
        {
            int randValue = 0;
            do
            {
                randValue = TestLibrary.Generator.GetInt32(-55);
            } while ((randValue == Int32.MinValue) || (randValue == 0));

            if (randValue > 0)
            {
                randValue *= -1;
            }

            Random random = new Random(randValue);
            double value = random.NextDouble();

            if ((value < 0.0) || (value >= 1.0))
            {
                TestLibrary.TestFramework.LogError("004.1", "NextDouble returns a value less than 0 or equal to 1.0");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] value = " + value);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion


    public static int Main()
    {
        RandomNextDouble test = new RandomNextDouble();

        TestLibrary.TestFramework.BeginTestCase("RandomNextDouble");

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
