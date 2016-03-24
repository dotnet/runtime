// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.IO;
using System.Reflection;

public class SeekOriginEnum
{
    public static int Main()
    {
        SeekOriginEnum ac = new SeekOriginEnum();

        TestLibrary.TestFramework.BeginTestCase("SeekOriginEnum");

        if (ac.RunTests())
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
        int  enumValue;

        TestLibrary.TestFramework.BeginScenario("PosTest1: SeekOriginEnum");

        try
        {
            enumValue = (int)SeekOrigin.Begin;

            if (0 != enumValue)
            {
                TestLibrary.TestFramework.LogError("001", "Unexpected value: Expected(0) Actual("+enumValue+")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
}

