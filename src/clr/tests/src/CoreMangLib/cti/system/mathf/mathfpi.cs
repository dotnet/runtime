// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.MathF.PI
/// </summary>
public class MathFPI
{
    public static int Main(string[] args)
    {
        MathFPI mathPI = new MathFPI();
        TestLibrary.TestFramework.BeginTestCase("Testing System.MathF.PI...");

        if (mathPI.RunTests())
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify the value of MathF.PI...");

        try
        {
            float mathPI = MathF.PI;
            if (mathPI != 3.14159265f)
            {
                TestLibrary.TestFramework.LogError("001", "The value of MathF.PI is not correct!");
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
}
