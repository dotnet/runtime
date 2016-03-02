// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;
/// <summary>
/// RegionInfo.Name [v-minch]
/// </summary>
public class RegionInfoName
{
    public static int Main()
    {
        RegionInfoName regInfoName = new RegionInfoName();
        TestLibrary.TestFramework.BeginTestCase("RegionInfoName");
        if (regInfoName.RunTests())
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
        retVal = PosTest1() && retVal;
		// The constructor will disallow partial names
		//retVal = PosTest3() && retVal;
		//retVal = PosTest4() && retVal;
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1:Return the property Name in RegionInfo object 1");
        try
        {
            RegionInfo regionInfo = new RegionInfo("en-US");
            string strName = regionInfo.Name;
            if (strName != "US")
            {
                TestLibrary.TestFramework.LogError("001", "the ExpectResult is en-US but the ActualResult is " + strName);
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
        TestLibrary.TestFramework.BeginScenario("PosTest2:Return the property Name in RegionInfo object 2");
        try
        {
            RegionInfo regionInfo = new RegionInfo("zh-CN");
            string strName = regionInfo.Name;
            if (strName != "CN")
            {
                TestLibrary.TestFramework.LogError("003", "the ExpectResult is ZH-CN but the ActualResult is " + strName);
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
	public bool PosTest3() // The constructor will disallow partial names
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest3:Return the property Name in RegionInfo object 3");
        try
        {
            RegionInfo regionInfo = new RegionInfo("US");
            string strName = regionInfo.Name;
            if (strName != "US")
            {
                TestLibrary.TestFramework.LogError("005", "the ExpectResult is US but the ActualResult is " + strName);
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
	public bool PosTest4() // The constructor will disallow partial names
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest4:Return the property Name in RegionInfo object 4");
        try
        {
            RegionInfo regionInfo = new RegionInfo("CN");
            string strName = regionInfo.Name;
            if (strName != "CN")
            {
                TestLibrary.TestFramework.LogError("007", "the ExpectResult is CN but the ActualResult is " + strName);
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