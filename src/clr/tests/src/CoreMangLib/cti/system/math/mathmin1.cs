// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

/// <summary>
/// System.Math.Min(System.Byte,System.Byte)
/// </summary>
public class MathMin1
{
    public static int Main(string[] args)
    {
        MathMin1 min1 = new MathMin1();
        TestLibrary.TestFramework.BeginTestCase("Testing System.Math.Min(System.Byte,System.Byte)...");

        if (min1.RunTests())
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
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify the return value of min function...");

        try
        {
            Byte var1 = TestLibrary.Generator.GetByte(-55);
            Byte var2 = TestLibrary.Generator.GetByte(-55);

            if (var1 > var2)
            {
                if (Math.Min(var1, var2) != var2)
                {
                    TestLibrary.TestFramework.LogError("001", "The return value should be var2!");
                    retVal = false;
                }
            }
            else
            {
                if (Math.Min(var1, var2) != var1)
                {
                    TestLibrary.TestFramework.LogError("002","The return value should be var1!");
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
