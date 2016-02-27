// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;
/// <summary>
/// RegionInfo.Equals(Object)
/// </summary>
public class RegionInfoEquals
{
    public static int Main()
    {
        RegionInfoEquals regInfoEquals = new RegionInfoEquals();
        TestLibrary.TestFramework.BeginTestCase("RegionInfoEquals");
        if (regInfoEquals.RunTests())
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
		// The RegionInfo constructor will disallow partial names
        retVal = PosTest4() && retVal;
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1:Compare two RegionInfo object 1");
        try
        {
            RegionInfo regionInfo1 = new RegionInfo("en-US");
            RegionInfo regionInfo2 = new RegionInfo("en-US");
            bool boolVal = regionInfo1.Equals(regionInfo2);
            if (!boolVal)
            {
                TestLibrary.TestFramework.LogError("001", "the ExpectResult is true but the ActualResult is " + boolVal.ToString());
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
        TestLibrary.TestFramework.BeginScenario("PosTest2:Compare two RegionInfo object 2");
        try
        {
            RegionInfo regionInfo1 = new RegionInfo("en-US");
            RegionInfo regionInfo2 = new RegionInfo("zh-CN");
            bool boolVal = regionInfo1.Equals(regionInfo2);
            if (boolVal)
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
	public bool PosTest3() // The RegionInfo constructor will disallow partial names
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest3:Compare two RegionInfo object 3");
        try
        {
            RegionInfo regionInfo1 = new RegionInfo("US");
            RegionInfo regionInfo2 = new RegionInfo("en-US");
            bool boolVal = regionInfo1.Equals(regionInfo2);
            if (boolVal)
            {
                TestLibrary.TestFramework.LogError("005", "the ExpectResult is false but the ActualResult is " + boolVal.ToString());
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
        TestLibrary.TestFramework.BeginScenario("PosTest4:RegionInfo object compared with not RegionInfo object");
        try
        {
            RegionInfo regionInfo1 = new RegionInfo("en-US");
            object objVal = new object();
            bool boolVal = regionInfo1.Equals(objVal);
            if (boolVal)
            {
                TestLibrary.TestFramework.LogError("007", "the ExpectResult is false but the ActualResult is " + boolVal.ToString());
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