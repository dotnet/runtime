// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

/// <summary>
/// System.Math.Max(System.Byte,System.Byte)
/// </summary>
public class MathMax1
{
    public static int Main(string[] args)
    {
        MathMax1 max1 = new MathMax1();
        TestLibrary.TestFramework.BeginTestCase("Testing System.Math.Max(System.Byte,System.Byte)...");

        if (max1.RunTests())
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
            byte byte1 = TestLibrary.Generator.GetByte(-55);
            byte byte2 = TestLibrary.Generator.GetByte(-55);

            if (byte1 < byte2)
            {
                if (Math.Max(byte1, byte2) != byte2)
                {
                    TestLibrary.TestFramework.LogError("001", "the max value should be byte2!");
                    retVal = false;
                }
            }
            else
            {
                if (Math.Max(byte1, byte2) != byte1)
                {
                    TestLibrary.TestFramework.LogError("002","the max value should be byte1!");
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

