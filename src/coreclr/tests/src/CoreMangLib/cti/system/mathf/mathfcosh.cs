// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.MathF.Cosh(System.Single)
/// </summary>
public class MathFCosh
{
    public static int Main(string[] args)
    {
        MathFCosh cosh = new MathFCosh();
        TestLibrary.TestFramework.BeginTestCase("Testing System.MathF.Cosh(System.Single)...");

        if (cosh.RunTests())
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
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify the monotonicity of cosh function...");

        try
        {
            float variable = TestLibrary.Generator.GetSingle(-55);
            float value1 = MathF.Cosh(variable);
            float positiveoffset = MathF.Exp(-15);
            float value2 = MathF.Cosh(variable + positiveoffset);

            if (value2 <= value1)
            {
                TestLibrary.TestFramework.LogError("001", "the monotonicity of cosh function [x>=y -> cosh(x)>=cosh(y)]");
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify the evenness of cosh function [cosh(x)=cosh(-x)]");

        try
        {
            float variable = TestLibrary.Generator.GetSingle(-55);
            float value1 = MathF.Cosh(variable);
            float value2 = MathF.Cosh(-variable);

            if (!MathFTestLib.SingleIsWithinEpsilon(value1, value2))
            {
                TestLibrary.TestFramework.LogError("003", "The parity of cosh should be even function!");
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
        TestLibrary.TestFramework.BeginScenario("PosTest3: Verify the value of cosh(0) = 1...");

        try
        {
            float zero = 0;
            float value = MathF.Cosh(zero);
            if (!MathFTestLib.SingleIsWithinEpsilon(value, 1))
            {
                TestLibrary.TestFramework.LogError("005", "The value of cosh(0) should be 1, actual: " + value.ToString());
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
        TestLibrary.TestFramework.BeginScenario("PosTest4: Verify the cosh(+inf)=+inf...");

        try
        {
            float variable = float.MaxValue;
            float value = MathF.Cosh(variable);
            if (value != float.PositiveInfinity)
            {
                TestLibrary.TestFramework.LogError("007", "The value should be float.PositiveInfinity, actual: " + value.ToString());
                retVal = false;
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
        TestLibrary.TestFramework.BeginScenario("PosTest5: Verify the value of cosh(float.MinValue)=+inf");

        try
        {
            float variable = float.MinValue;
            float value = MathF.Cosh(variable);
            if (value != float.PositiveInfinity)
            {
                TestLibrary.TestFramework.LogError("009", "The value should be float.PositiveInfinity, actual: " +
                    value.ToString());
                retVal = false;
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
