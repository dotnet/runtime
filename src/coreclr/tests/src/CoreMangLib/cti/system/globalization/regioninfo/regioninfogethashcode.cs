// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;
/// <summary>
/// RegionInfo.GetHashCode()
/// </summary>
public class RegionInfoGetHashCode
{
    public static int Main()
    {
        RegionInfoGetHashCode regInfoGetHashCode = new RegionInfoGetHashCode();
        TestLibrary.TestFramework.BeginTestCase("RegionInfoGetHashCode");
        if (regInfoGetHashCode.RunTests())
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
        TestLibrary.TestFramework.BeginScenario("PosTest1:Get the hash code of the RegionInfo object 1");
        try
        {
            RegionInfo regionInfo = new RegionInfo("zh-CN");
            int hashCode = regionInfo.GetHashCode();
            if (hashCode != regionInfo.Name.GetHashCode())
            {
                TestLibrary.TestFramework.LogError("001", "the ExpectResult is" + regionInfo.Name.GetHashCode() +"but the ActualResult is " + hashCode);
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
        TestLibrary.TestFramework.BeginScenario("PosTest2:Get the hash code of the RegionInfo object 2");
        try
        {
            RegionInfo regionInfo = new RegionInfo("en-US");
            int hashCode = regionInfo.GetHashCode();
            if (hashCode != regionInfo.Name.GetHashCode())
            {
                TestLibrary.TestFramework.LogError("003", "the ExpectResult is" + regionInfo.Name.GetHashCode() +"but the ActualResult is " + hashCode);
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
    #endregion
}
