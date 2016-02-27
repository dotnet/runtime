// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

/// <summary>
/// PositiveInfinity
/// </summary>
public class DoublePositiveInfinity
{
    public static int Main()
    {
        DoublePositiveInfinity test = new DoublePositiveInfinity();
        TestLibrary.TestFramework.BeginTestCase("DoublePositiveInfinity");

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

    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Ensure PositiveInfinity is positive infinity.");

        try
        {
            if (Double.IsPositiveInfinity(Double.PositiveInfinity) != true)
            {
                TestLibrary.TestFramework.LogError("001.1", "PositiveInfinity is not positive infinity!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: Ensure PositiveInfinity is positive infinity after adding with a double value.");

        Double randomDouble = new Random(-55).NextDouble();
        try
        {
            if (Double.IsPositiveInfinity(Double.PositiveInfinity + randomDouble) != true)
            {
                TestLibrary.TestFramework.LogError("001.1", "PositiveInfinity is not positive infinity after adding with a double value!");
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
}
