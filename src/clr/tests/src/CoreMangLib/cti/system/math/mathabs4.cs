// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

/// <summary>
/// System.Math.Abs(System.Int32)
/// </summary>
public class MathAbs4
{
    public static int Main(string[] args)
    {
        MathAbs4 abs4 = new MathAbs4();
        TestLibrary.TestFramework.BeginTestCase("Testing System.Math.Abs(System.Int32)...");

        if (abs4.RunTests())
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

        TestLibrary.TestFramework.LogInformation("[Negtive]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify the max value of Int32 should be equal to its Abs value...");

        try
        {
            Int32 max = Int32.MaxValue;
            if (Math.Abs(max) != max)
            {
                TestLibrary.TestFramework.LogError("001", "The Abs of max value should be equal to itself!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify Abs value of zero should be equal to both itself and its contrary value...");

        try
        {
            Int32 zero = 0;
            if (Math.Abs(zero) != zero || Math.Abs(zero) != -zero)
            {
                TestLibrary.TestFramework.LogError("003", "Abs value of zero should be equal to both itself and its contrary value!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest1: Verify the Abs of min value of Int32...");

        try
        {
            Int32 min = Int32.MinValue;
            if (Math.Abs(min) != -min)
            {
                TestLibrary.TestFramework.LogError("101", "The Abs of min value should be equal to it's contrary value!");
                retVal = false;
            }
        }
        catch (OverflowException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("102", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }
}
