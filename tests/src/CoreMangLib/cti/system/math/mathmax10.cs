// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

/// <summary>
/// System.Math.Max(System.UInt32,System.UInt32)
/// </summary>
public class MathMax10
{
    public static int Main(string[] args)
    {
        MathMax10 max10 = new MathMax10();
        TestLibrary.TestFramework.BeginTestCase("Testing System.Math.Max(System.UInt32,System.UInt32)...");

        if (max10.RunTests())
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

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify the return value of max function...");

        try
        {
            UInt32 var1 = (UInt32)TestLibrary.Generator.GetInt32(-55);
            UInt32 var2 = (UInt32)TestLibrary.Generator.GetInt32(-55);

            if (var1 < var2)
            {
                if (Math.Max(var1, var2) != var2)
                {
                    TestLibrary.TestFramework.LogError("001", "The return value should be var2!");
                    retVal = false;
                }
            }
            else
            {
                if (Math.Max(var1, var2) != var1)
                {
                    TestLibrary.TestFramework.LogError("002", "The return value should be var1!");
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
}
