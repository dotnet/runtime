// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

/// <summary>
/// System.DateTime.ToLocalTime
/// </summary>
public class DateTimeToLocalTime
{
    public static int Main(string[] args)
    {
        DateTimeToLocalTime toLocalTime = new DateTimeToLocalTime();
        TestLibrary.TestFramework.BeginTestCase("Testing DateTime.ToLocalTime...");

        if (toLocalTime.RunTests())
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
        TestLibrary.TestFramework.BeginScenario("Verify convvert DateTime to utc then convert back is equal to original...");

        try
        {
            DateTime myTime = DateTime.Now;
            DateTime myUtcTime = myTime.ToUniversalTime();
            DateTime utcToLocal = myUtcTime.ToLocalTime();
            
            if (myTime != utcToLocal)
            {
                TestLibrary.TestFramework.LogError("001","The DateTime change back to local should be equal to original!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002","Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }
}
