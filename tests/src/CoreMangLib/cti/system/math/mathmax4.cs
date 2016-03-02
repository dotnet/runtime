// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

/// <summary>
/// System.Math.Max(System.Int16,System.Int16)
/// </summary>
public class MathMax4
{
    public static int Main(string[] args)
    {
        MathMax4 max4 = new MathMax4();
        TestLibrary.TestFramework.BeginTestCase("Testing System.Math.Max(System.Int16,System.Int16)...");

        if (max4.RunTests())
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
            Int16 var1 = TestLibrary.Generator.GetInt16(-55);
            Int16 var2 = TestLibrary.Generator.GetInt16(-55);

            if (var1 < var2)
            {
                if (Math.Max(var1, var2) != var2)
                {
                    TestLibrary.TestFramework.LogError("001", "The value should be var1!");
                    retVal = false;
                }
            }
            else
            {
                if (Math.Max(var1, var2) != var1)
                {
                    TestLibrary.TestFramework.LogError("002", "The value should be var2!");
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
}

