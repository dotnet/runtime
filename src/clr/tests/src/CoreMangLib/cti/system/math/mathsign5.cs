// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

/// <summary>
/// Sign(System.Int64)
/// </summary>

public class MathSign5
{
    public static int Main(string[] args)
    {
        MathSign5 test = new MathSign5();
        TestLibrary.TestFramework.BeginTestCase("Testing System.Math.Sign(System.Int64).");

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
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;
        retVal = PosTest5() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify Int64.MaxValue is a positive number.");

        try
        {
            Int64 i = Int64.MaxValue;
            if (Math.Sign(i) != 1)
            {
                TestLibrary.TestFramework.LogError("P01.1", "Int64.MaxValue is not a positive number!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("P01.2", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify Int64.MinValue is a negative number.");

        try
        {
            Int64 i = Int64.MinValue;
            if (Math.Sign(i) != -1)
            {
                TestLibrary.TestFramework.LogError("P02.1", "Int64.MinValue is not a negative number!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("P02.2", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest3: Verify the return value should be 1 when the Int64 is positive.");

        try
        {
            Int64 i = TestLibrary.Generator.GetInt64(-55);
            while (i <= 0)
            {
                i = TestLibrary.Generator.GetInt64(-55);
            }

            if (Math.Sign(i) != 1)
            {
                TestLibrary.TestFramework.LogError("P03.1", "The return value is not 1 when the Int64 is positive!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("P03.2", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest4: Verify the return value should be -1 when the Int64 is negative.");

        try
        {
            Int64 i = TestLibrary.Generator.GetInt64(-55);
            while (i <= 0)
            {
                i = TestLibrary.Generator.GetInt64(-55);
            }

            if (Math.Sign(-i) != -1)
            {
                TestLibrary.TestFramework.LogError("P04.1", "The return value is not -1 when the Int64 is negative!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("P04.2", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest5: Verify the return value should be 0 when the Int64 is zero.");

        try
        {
            Int64 i = 0;

            if (Math.Sign(i) != 0)
            {
                TestLibrary.TestFramework.LogError("P05.1", "The return value is not -1 when the Int64 is negative!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("P05.2", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }
}
