// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// v-jinfw
// Does not build

using System;
using System.Collections;

/// <summary>
/// MinValue
/// </summary>
public class DoubleMinValue
{
    public static int Main()
    {
        DoubleMinValue test = new DoubleMinValue();
        TestLibrary.TestFramework.BeginTestCase("DoubleMinValue");

        if (test.RunTests())
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
        TestLibrary.TestFramework.BeginScenario("PosTest1: Ensure MinValue equals -1.79769313486232e308");

        try
        {
            if (Double.MinValue.CompareTo(-1.7976931348623157E+308) != 0)
            {
                TestLibrary.TestFramework.LogError("001.1", "MinValue does not equal -1.79769313486232e308!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
}
