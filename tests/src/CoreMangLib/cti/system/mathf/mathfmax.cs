// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.MathF.Max(System.Single, System.Single)
/// </summary>
public class MathFMax8
{
    public static int Main(string[] args)
    {
        MathFMax8 max8 = new MathFMax8();
        TestLibrary.TestFramework.BeginTestCase("Testing System.MathF.Max(System.Single,System.Single)...");

        if (max8.RunTests())
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
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify the return value of max function...");

        try
        {
            float var1 = TestLibrary.Generator.GetSingle(-55);
            float var2 = TestLibrary.Generator.GetSingle(-55);

            if (var1 > var2 && !float.IsNaN(var1) && !float.IsNaN(var2))
            {
                if (MathF.Max(var1, var2) != var1)
                {
                    TestLibrary.TestFramework.LogError("001", "The return value should be var1!");
                    retVal = false;
                }
            }
            else if (!float.IsNaN(var1) && !float.IsNaN(var2))
            {
                if (MathF.Max(var1, var2) != var2)
                {
                    TestLibrary.TestFramework.LogError("002", "The return value should be var2!");
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify the max value of float.NaN and float.PositiveInfinity...");

        try
        {
            float var1 = float.NaN;
            float var2 = float.PositiveInfinity;

            if (!float.IsNaN(MathF.Max(var1, var2)))
            {
                TestLibrary.TestFramework.LogError("004", "The return value should be single.NaN!");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("005", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }
}
