// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
﻿using System;
using System.Globalization;
/// <summary>
/// RegionInfo.IsMetric
/// </summary>
public class RegionInfoIsMetric
{
    public static int Main()
    {
        RegionInfoIsMetric regInfoIsMetric = new RegionInfoIsMetric();
        TestLibrary.TestFramework.BeginTestCase("RegionInfoIsMetric");
        if (regInfoIsMetric.RunTests())
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
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1:Return the property IsMetric in RegionInfo object 1");
        try
        {
            RegionInfo regionInfo = new RegionInfo("en-US");
            bool boolVal = regionInfo.IsMetric;
            if (boolVal)
            {
                TestLibrary.TestFramework.LogError("001", "the ExpectResult is false but the ActualResult is " + boolVal.ToString());
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest2:Return the property IsMetric in RegionInfo object 2");
        try
        {
            RegionInfo regionInfo = new RegionInfo("zh-CN");
            bool boolVal = regionInfo.IsMetric;
            if (!boolVal)
            {
                TestLibrary.TestFramework.LogError("003", "the ExpectResult is false but the ActualResult is " + boolVal.ToString());
                retVal = false;
            }
        }
		catch (ArgumentException)
		{
			TestLibrary.TestFramework.LogInformation("The East Asian Languages are not installed. Skipping test(s)");
			retVal = true;
		}
		catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
}