// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;
/// <summary>
/// RegionInfo.TwoLetterISORegionName [v-minch]
/// </summary>
public class RegionInfoTwoLetterISORegionName
{
    public static int Main()
    {
        RegionInfoTwoLetterISORegionName regInfoTwoLetterISOName = new RegionInfoTwoLetterISORegionName();
        TestLibrary.TestFramework.BeginTestCase("RegionInfoTwoLetterISORegionName");
        if (regInfoTwoLetterISOName.RunTests())
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
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1:Return the property TwoLetterISORegionName in RegionInfo object 1");
        try
        {
            RegionInfo regionInfo = new RegionInfo("en-US");
            string strTwoLetterName = regionInfo.TwoLetterISORegionName;
            if (strTwoLetterName != "US")
            {
                TestLibrary.TestFramework.LogError("001", "the ExpectResult is US but the ActualResult is " + strTwoLetterName);
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
        TestLibrary.TestFramework.BeginScenario("PosTest2:Return the property TwoLetterISORegionName in RegionInfo object 2");
        try
        {
            RegionInfo regionInfo = new RegionInfo("zh-CN");
            string strTwoLetterName = regionInfo.TwoLetterISORegionName;
            if (strTwoLetterName != "CN")
            {
                TestLibrary.TestFramework.LogError("003", "the ExpectResult is CN but the ActualResult is " + strTwoLetterName);
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
    public bool PosTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest3:Return the property TwoLetterISORegionName in RegionInfo object 3");
        try
        {
            RegionInfo regionInfo = new RegionInfo("de-DE");
            string strTwoLetterName = regionInfo.TwoLetterISORegionName;
            if (strTwoLetterName != "DE")
            {
                TestLibrary.TestFramework.LogError("005", "the ExpectResult is DE but the ActualResult is " + strTwoLetterName);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest4()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest4:Return the property TwoLetterISORegionName in RegionInfo object 4");
        try
        {
            RegionInfo regionInfo = new RegionInfo("it-IT");
            string strTwoLetterName = regionInfo.TwoLetterISORegionName;
            if (strTwoLetterName != "IT")
            {
                TestLibrary.TestFramework.LogError("007", "the ExpectResult is IT but the ActualResult is " + strTwoLetterName);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
}