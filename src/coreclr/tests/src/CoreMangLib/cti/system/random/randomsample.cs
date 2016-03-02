// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

public class TestRandom : Random
{
    public TestRandom(int seed) : base(seed)
    {
    }

    public double CallSample()
    {
        return Sample();
    }

    protected override double Sample()
    {
        return base.Sample() / 2.0;
    }
}

/// <summary>
/// Sample
/// </summary>
public class RandomSample
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call NextDouble to check the Sample works");

        try
        {
            TestRandom random = new TestRandom(-55);
            double value = random.NextDouble();

            if ((value < 0.0) || (value >= 1.0))
            {
                TestLibrary.TestFramework.LogError("001.1", "NextDouble returns a value less than 0 or equal to 1.0");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] value = " + value);
                retVal = false;
            }

            value = random.CallSample();

            if ((value < 0.0) || (value >= 1.0))
            {
                TestLibrary.TestFramework.LogError("001.2", "NextDouble returns a value less than 0 or equal to 1.0");
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
    #endregion
    #endregion

    public static int Main()
    {
        RandomSample test = new RandomSample();

        TestLibrary.TestFramework.BeginTestCase("RandomSample");

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
