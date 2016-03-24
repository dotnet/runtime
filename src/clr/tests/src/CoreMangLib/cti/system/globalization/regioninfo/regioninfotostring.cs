// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;
/// <summary>
/// RegionInfo.ToString()
/// </summary>
public class RegionInfoToString
{
    public static int Main()
    {
        RegionInfoToString regInfoToString = new RegionInfoToString();
        TestLibrary.TestFramework.BeginTestCase("RegionInfoToString");
        if (regInfoToString.RunTests())
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
        retVal = PosTest3() && retVal;
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1:Invoke the method ToString in RegionInfo object 1");
		try
		{
			RegionInfo regionInfo = new RegionInfo("zh-CN");
			string strVal = regionInfo.ToString();
			if (strVal != regionInfo.Name)
			{
				TestLibrary.TestFramework.LogError("001", "the ExpectResult is" + regionInfo.Name + "but the ActualResult is" + strVal);
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
			TestLibrary.TestFramework.LogError("002", "Unexpect exception:" + e);
			retVal = false;
		}
        return retVal;
    }
    public bool PosTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest2:Invoke the method ToString in RegionInfo object 2");
        try
        {
            RegionInfo regionInfo = new RegionInfo("en-US");
            string strVal = regionInfo.ToString();
            if (strVal != regionInfo.Name)
            {
                TestLibrary.TestFramework.LogError("003", "the ExpectResult is" + regionInfo.Name + "but the ActualResult is" + strVal);
                retVal = false;
            }
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
        TestLibrary.TestFramework.BeginScenario("PosTest3:Invoke the method ToString in RegionInfo object 3");
        try
        {
            RegionInfo regionInfo = new RegionInfo("en-IE");
            string strVal = regionInfo.ToString();
            if (strVal != regionInfo.Name)
            {
                TestLibrary.TestFramework.LogError("005", "the ExpectResult is" + regionInfo.Name + "but the ActualResult is" + strVal);
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
    #endregion
}