// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

/// <summary>
/// System.Math.Max(System.Double,System.Double)
/// </summary>
public class MathMax3
{
    public static int Main(string[] args)
    {
        MathMax3 max3 = new MathMax3();
        TestLibrary.TestFramework.BeginTestCase("Testing System.Math.Max(System.Double,System.Double)");

        if (max3.RunTests())
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
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify the return value of max function when double parameters are not NaN...");

        try
        {
            double var1 = TestLibrary.Generator.GetDouble(-55);
            double var2 = TestLibrary.Generator.GetDouble(-55);
            
            if (var1 > var2 && !double.IsNaN(var1) && !double.IsNaN(var2))
            {
                if (Math.Max(var1, var2) != var1)
                {
                    TestLibrary.TestFramework.LogError("001","The value should equal to var1!");
                    retVal = false;
                }
            }
            else if (!double.IsNaN(var1) && !double.IsNaN(var2))
            {
                if (Math.Max(var1, var2) != var2)
                {
                    TestLibrary.TestFramework.LogError("002", "The value should equal to var2!");
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002","Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify var1 is double.NaN...");

        try
        {
            double var1 = double.NaN;
            double var2 = double.PositiveInfinity;

            if (!double.IsNaN(Math.Max(var1, var2)))
            {
                TestLibrary.TestFramework.LogError("003","The value should be var1!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004","Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }
}
