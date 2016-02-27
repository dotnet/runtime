// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// ctor(System.Int32)
/// </summary>
public class RandomCtor2
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = PosTest2() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call ctor to construct a new instance");

        try
        {
            int randValue = 0;

            do
            {
                randValue = TestLibrary.Generator.GetInt32(-55);
            } while (randValue == Int32.MinValue);

            Random random = new Random(randValue);

            if (null == random)
            {
                TestLibrary.TestFramework.LogError("001.1", "Call ctor returns null reference");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] randValue = " + randValue);
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

        //Dev10 Bug#738987 && Bug#738988: Random throwed OverflowException because of  Math.Abs limitations. That has been fixed.
        TestLibrary.TestFramework.BeginScenario("PosTest2: OverflowException should NOT be thrown when Seed is Int32.MinValue, Dev10 Bug#738987 && Bug#738988");

        try
        {
            Random rand = new Random(Int32.MinValue);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("101.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        RandomCtor2 test = new RandomCtor2();

        TestLibrary.TestFramework.BeginTestCase("RandomCtor2");

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
