// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.MathF.Exp(System.Single)
/// </summary>
public class MathFExp
{
    public static int Main(string[] args)
    {
        MathFExp exp = new MathFExp();
        TestLibrary.TestFramework.BeginTestCase("Testing System.MathF.Exp(System.Single)...");

        if (exp.RunTests())
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
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify monotonicity of Exp function");

        try
        {
            float variable = TestLibrary.Generator.GetSingle(-55);
            float value1 = MathF.Exp(variable);
            float positiveoffset = MathF.Exp(-15);
            float value2 = MathF.Exp(variable + positiveoffset);

            if (value2 <= value1)
            {
                TestLibrary.TestFramework.LogError("001", "The monotonicity of chx function should be increase by degree!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify Exp is neither odd function nor even function...");

        try
        {
            float variable = TestLibrary.Generator.GetSingle(-55);
            float value1 = MathF.Exp(variable);
            float value2 = MathF.Exp(-variable);

            if (value1 == value2 || value1 == -value2)
            {
                TestLibrary.TestFramework.LogError("003", "The exp function should not be symmetry!");
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

    public bool PosTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest3: Verify the value of Exp(0) is 1...");

        try
        {
            float value = MathF.Exp(0);

            if (value != 1)
            {
                TestLibrary.TestFramework.LogError("005", "The value of Exp(0) is 1!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest4: Verify the value of Exp(float.MaxValue)...");

        try
        {
            float variable = float.MaxValue;
            float value = MathF.Exp(variable);

            if (value != float.PositiveInfinity)
            {
                TestLibrary.TestFramework.LogError("007", "The value of Exp(float.MaxValue) should be PositiveInfinity!");
                retVal = true;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest5: Verify the value of Exp(float.MaxValue)");

        try
        {
            float variable = float.MinValue;
            float value = MathF.Exp(variable);

            if (value != 0)
            {
                TestLibrary.TestFramework.LogError("009", "The value of Exp(float.MaxValue) should be PositiveInfinity!");
                retVal = true;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }
}
