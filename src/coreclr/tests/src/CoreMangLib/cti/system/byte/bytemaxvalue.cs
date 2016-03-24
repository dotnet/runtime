// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

/// <summary>
/// System.Byte.MaxValue
/// </summary>
public class ByteMaxValue
{
    public static int Main(string[] args)
    {
        ByteMaxValue maxValue = new ByteMaxValue();
        TestLibrary.TestFramework.BeginTestCase("Testing System.Byte.MaxValue property...");

        if (maxValue.RunTests())
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

        TestLibrary.TestFramework.LogInformation("[Negtive]");
        retVal = NegTest1() && retVal;

        return retVal; 
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify the MaxValue of Byte is 255...");

        try
        {
            Byte max = Byte.MaxValue;

            if (max != 255)
            {
                TestLibrary.TestFramework.LogError("001","The MaxValue of Byte is not 255!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002","Unexpected exception occurs: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify using number larger than MaxValue to create Byte instance...");

        try
        {
            int beyondMax = 256;
            Byte beyondByte = (Byte)beyondMax;

            if (beyondByte != beyondMax % 256)
            {
                TestLibrary.TestFramework.LogError("003","The converting is wrong!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004","Unexpected exception occurs: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            return retVal;
        }

        return retVal;
    }
}
