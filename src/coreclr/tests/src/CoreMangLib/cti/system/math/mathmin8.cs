// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

/// <summary>
/// System.Math.Min(System.Single,System.Single)
/// </summary>
public class MathMin8
{
    public static int Main(string[] args)
    {
        MathMin8 min8 = new MathMin8();
        TestLibrary.TestFramework.BeginTestCase("Testing System.Math.Min(System.Single,System.Single)...");

        if (min8.RunTests())
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
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify the return value of min function...");

        try
        {
            Single var1 = TestLibrary.Generator.GetSingle(-55);
            Single var2 = TestLibrary.Generator.GetSingle(-55);

            if (var1 < var2 && !Single.IsNaN(var1) && !Single.IsNaN(var2))
            {
                if (Math.Min(var1, var2) != var1)
                {
                    TestLibrary.TestFramework.LogError("001", "The return value should be var1!");
                    retVal = false;
                }
            }
            else if (!Single.IsNaN(var1) && !Single.IsNaN(var2))
            {
                if (Math.Min(var1, var2) != var2)
                {
                    TestLibrary.TestFramework.LogError("002","The return value should be var2!");
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003","Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify the min value of Single.NaN and Single.NegativeInfinity...");

        try
        {
            Single var1 = Single.NaN;
            Single var2 = Single.NegativeInfinity;

            if (!Single.IsNaN(Math.Min(var1, var2)))
            {
                TestLibrary.TestFramework.LogError("004", "The return value should be Single.NaN!");
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
