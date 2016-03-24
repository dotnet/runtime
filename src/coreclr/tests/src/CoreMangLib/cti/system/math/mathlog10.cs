// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

/// <summary>
/// System.Math.Log10(System.Double)
/// </summary>
public class MathLog10
{
    public static int Main(string[] args)
    {
        MathLog10 log10 = new MathLog10();
        TestLibrary.TestFramework.BeginTestCase("Testing System.Math.Log10(System.Double)...");

        if (log10.RunTests())
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
        retVal = PosTest6() && retVal;
        retVal = PosTest7() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify the monotonicity of Log10(var)...");

        try
        {
            double var1 = 100.1 * TestLibrary.Generator.GetDouble(-55);
            double var2 = 100.1 * TestLibrary.Generator.GetDouble(-55);

            if (var1 < var2)
            {
                if (Math.Log10(var1) >= Math.Log10(var2))
                {
                    TestLibrary.TestFramework.LogError("001", "The value of Log10(var1) should be less than In(var2)...");
                    retVal = false;
                }
            }
            else if (var1 > var2)
            {
                if (Math.Log10(var1) <= Math.Log10(var2))
                {
                    TestLibrary.TestFramework.LogError("002", "The value of Log10(var1) should be larger than In(var2)...");
                    retVal = false;
                }
            }
            else
            {
                if (Math.Log10(var1) != Math.Log10(var2))
                {
                    TestLibrary.TestFramework.LogError("003", "The value of Log10(var1) should be equal to In(var2)...");
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify the value is negative when var is between o and 1...");

        try
        {
            double var = 0;
            while (var <= 0 && var >= 1)
            {
                var = TestLibrary.Generator.GetDouble(-55);
            }

            if (Math.Log10(var) >= 0)
            {
                TestLibrary.TestFramework.LogError("005", "The value should be negative!");
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

    public bool PosTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest3: Verify the value of Log10(1) is 0...");

        try
        {
            double var = 1;
            if (Math.Log10(var) != 0)
            {
                TestLibrary.TestFramework.LogError("007", "The value of Log10(1) should be zero!");
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

    public bool PosTest4()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest4: Verify the value of Log10(var) is larger than zero...");

        try
        {
            double var = TestLibrary.Generator.GetDouble(-55);
            while (var <= 1)
            {
                var *= 10;
            }

            if (Math.Log10(var) < 0)
            {
                TestLibrary.TestFramework.LogError("009", "The value should be larger than zero!");
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

    public bool PosTest5()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest5: Verify the value of Log10(0)...");

        try
        {
            double var = 0;
            if (!double.IsNegativeInfinity(Math.Log10(var)))
            {
                TestLibrary.TestFramework.LogError("011", "the value of Log10(0) should be negativeInfinity!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest6()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest6: Verify the value of Log10(var) when var is negative...");

        try
        {
            double var = -TestLibrary.Generator.GetDouble(-55);
            while (var >= 0)
            {
                var = TestLibrary.Generator.GetDouble(-55);
            }

            if (!double.IsNaN(Math.Log10(var)))
            {
                TestLibrary.TestFramework.LogError("013", "The value should be NaN!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("014", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest7()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest7: Verify the value of Log10(10)...");

        try
        {
            double var = 10;
            if (Math.Log10(var) != 1)
            {
                TestLibrary.TestFramework.LogError("015","the value should be equal to 1!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("016", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }
}
